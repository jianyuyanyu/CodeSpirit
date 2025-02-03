using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using CodeSpirit.IdentityApi.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CodeSpirit.IdentityApi.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleRepository(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }
        public async Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto)
        {
            var query = _roleManager.Roles
                                        .Include(r => r.RolePermissions)
                                        .AsQueryable();
            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                var searchLower = queryDto.Keywords.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Description.ToLower().Contains(searchLower)
                    );
            }

            var count = await query.CountAsync();

            // Sorting logic
            query = query.ApplySorting(queryDto);
            query = query.ApplyPaging(queryDto);
            var list = await query.ToListAsync();
            return (list, count);
        }

        public async Task<ApplicationRole> GetRoleByIdAsync(string id)
        {
            return await _roleManager.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ThenInclude(p => p.Children)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task CreateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            var permissions = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            role.RolePermissions = permissions.Select(p => new RolePermission
            {
                PermissionId = p.Id,
                IsAllowed = true
            }).ToList();

            await _roleManager.CreateAsync(role);
        }

        public async Task<List<string>> GetUserIdsByRoleId(string id)
        {
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == id)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
            return userIds;
        }

        public async Task UpdateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            var currentPermissions = role.RolePermissions.Select(rp => rp.PermissionId).ToList();
            var permissionsToAdd = permissionIds.Except(currentPermissions).ToList();
            var permissionsToRemove = currentPermissions.Except(permissionIds).ToList();

            if (permissionsToAdd.Any())
            {
                var newPermissions = await _context.Permissions
                    .Where(p => permissionsToAdd.Contains(p.Id))
                    .ToListAsync();

                foreach (var permission in newPermissions)
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        IsAllowed = true,
                        Permission = permission
                    });
                }
            }

            if (permissionsToRemove.Any())
            {
                var removeList = role.RolePermissions
                    .Where(rp => permissionsToRemove.Contains(rp.PermissionId))
                    .ToList();

                foreach (var rp in removeList)
                {
                    role.RolePermissions.Remove(rp);
                }
            }

            await _roleManager.UpdateAsync(role);
        }

        public async Task DeleteRoleAsync(ApplicationRole role)
        {
            await _roleManager.DeleteAsync(role);
        }

        public async Task AssignPermissionsToRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            var permissions = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (!role.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        IsAllowed = true,
                        Permission = permission
                    });
                }
            }

            await _roleManager.UpdateAsync(role);
        }

        public async Task RemovePermissionsFromRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            // 先获取需要移除的权限列表
            var removeList = role.RolePermissions
                .Where(rp => permissionIds.Contains(rp.PermissionId))
                .ToList();

            // 遍历移除的权限
            foreach (var rp in removeList)
            {
                // 如果权限是子权限，需要先检查并重新赋权
                var permission = rp.Permission;
                if (permission.ParentId != null) // 权限是子权限
                {
                    // 重新赋权父权限
                    var parentPermission = permission.Parent;
                    // 只移除当前权限，保留父权限
                    var parentRolePermission = role.RolePermissions
                        .FirstOrDefault(rp => rp.PermissionId == parentPermission.Id);

                    if (parentRolePermission == null)
                    {
                        // 如果父权限不存在角色权限列表中，则添加父权限
                        role.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = parentPermission.Id,
                            IsAllowed = rp.IsAllowed
                        });
                    }
                }

                // 从角色中移除当前权限
                role.RolePermissions.Remove(rp);
            }

            // 更新角色
            await _roleManager.UpdateAsync(role);
        }
    }

}
