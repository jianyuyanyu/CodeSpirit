using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
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
                                        .Include(r => r.RolePermissions
                                            .Where(rp => rp.Permission.ParentId == null))
                                        .ThenInclude(rp => rp.Permission.Children)
                                        .AsQueryable();

            if (!string.IsNullOrEmpty(queryDto.Keywords))
            {
                query = query.Where(r =>
                    EF.Functions.Like(r.Name, $"%{queryDto.Keywords}%") ||
                    EF.Functions.Like(r.Description, $"%{queryDto.Keywords}%"));
            }

            var count = await query.CountAsync();
            // Sorting logic
            query = ApplySorting(query, queryDto);

            int skip = (queryDto.Page - 1) * queryDto.PerPage;
            query = query.Skip(skip).Take(queryDto.PerPage);

            return (await query.ToListAsync(), count);
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
            var removeList = role.RolePermissions
                .Where(rp => permissionIds.Contains(rp.PermissionId))
                .ToList();

            foreach (var rp in removeList)
            {
                role.RolePermissions.Remove(rp);
            }

            await _roleManager.UpdateAsync(role);
        }

        private IQueryable<ApplicationRole> ApplySorting(IQueryable<ApplicationRole> query, RoleQueryDto queryDto)
        {
            var orderBy = !string.IsNullOrEmpty(queryDto.OrderBy) ? queryDto.OrderBy : "Name";
            var orderDir = queryDto.OrderDir?.ToLower() == "desc" ? "desc" : "asc";

            var allowedOrderFields = new[] { "Name", "Description", "Id" };
            if (!allowedOrderFields.Contains(orderBy, StringComparer.OrdinalIgnoreCase))
            {
                orderBy = "Name";
            }

            return orderDir == "desc"
                ? query.OrderByDescending(e => EF.Property<object>(e, orderBy))
                : query.OrderBy(e => EF.Property<object>(e, orderBy));
        }
    }

}
