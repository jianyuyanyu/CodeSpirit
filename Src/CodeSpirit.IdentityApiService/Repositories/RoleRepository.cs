using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using CodeSpirit.IdentityApi.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Repositories
{
    /// <summary>
    /// 角色管理仓储类，负责角色及其权限的数据库操作
    /// </summary>
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleRepository(
            ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        /// <summary>
        /// 分页获取角色列表（带权限信息）
        /// </summary>
        /// <param name="queryDto">包含分页、排序和搜索条件的查询对象</param>
        /// <returns>(角色列表, 总记录数)</returns>
        public async Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto)
        {
            // 基础查询包含权限信息
            var query = _roleManager.Roles
                .Include(r => r.RolePermissions)
                .AsQueryable();

            // 应用关键词过滤
            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                var searchLower = queryDto.Keywords.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Description.ToLower().Contains(searchLower));
            }

            var count = await query.CountAsync();

            // 应用排序和分页
            query = query
                .ApplySorting(queryDto)  // 自定义排序扩展方法
                .ApplyPaging(queryDto);  // 自定义分页扩展方法

            return (await query.ToListAsync(), count);
        }

        /// <summary>
        /// 根据ID获取角色详细信息（包含完整权限树）
        /// </summary>
        public async Task<ApplicationRole> GetRoleByIdAsync(string id)
        {
            return await _roleManager.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// 创建新角色并关联权限
        /// </summary>
        /// <remarks>需要事务保证角色和权限的原子性操作</remarks>
        public async Task CreateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            // 验证权限是否存在
            var permissions = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync();

            // 构建角色-权限关联关系
            role.RolePermissions = permissions.Select(p => new RolePermission
            {
                PermissionId = p.Id,
                IsAllowed = true
            }).ToList();

            // 创建角色（自动级联创建关联的RolePermissions）
            await _roleManager.CreateAsync(role);
        }

        /// <summary>
        /// 更新角色信息及其权限配置
        /// </summary>
        /// <param name="permissionIds">更新后的完整权限ID集合</param>
        public async Task UpdateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            // 获取当前权限ID集合
            var currentPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToList();

            // 计算需要添加和移除的权限
            var permissionsToAdd = permissionIds.Except(currentPermissionIds).ToList();
            var permissionsToRemove = currentPermissionIds.Except(permissionIds).ToList();

            // 添加新权限
            if (permissionsToAdd.Any())
            {
                var newPermissions = await GetPermissionsByIdsAsync(permissionsToAdd);
                foreach (var permission in newPermissions)
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        IsAllowed = true,
                        Permission = permission
                    });
                }
            }

            // 移除旧权限
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

        /// <summary>
        /// 获取拥有指定角色的用户ID列表
        /// </summary>
        public async Task<List<string>> GetUserIdsByRoleId(string id)
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == id)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// 删除角色（同时级联删除关联的RolePermissions）
        /// </summary>
        public async Task DeleteRoleAsync(ApplicationRole role)
        {
            await _roleManager.DeleteAsync(role);
        }

        /// <summary>
        /// 为角色批量添加权限
        /// </summary>
        public async Task AssignPermissionsToRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            var existingPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            var permissions = await GetPermissionsByIdsAsync(permissionIds);

            foreach (var permission in permissions)
            {
                if (!existingPermissionIds.Contains(permission.Id))
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

        /// <summary>
        /// 从角色中移除指定权限（处理父子权限关系）
        /// </summary>
        /// <remarks>
        /// 当移除子权限时，需要确保父权限保持有效状态：
        /// 1. 如果父权限不存在于角色权限中，则自动添加父权限
        /// 2. 保留原有权限的IsAllowed状态
        /// </remarks>
        public async Task RemovePermissionsFromRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds)
        {
            var removeList = role.RolePermissions
                .Where(rp => permissionIds.Contains(rp.PermissionId))
                .ToList();

            foreach (var rp in removeList)
            {
                var permission = rp.Permission;

                // 处理子权限的特殊情况
                if (permission.ParentId.HasValue)
                {
                    var parentPermission = await _context.Permissions
                        .FindAsync(permission.ParentId.Value);

                    // 确保父权限存在且未被包含
                    if (!role.RolePermissions.Any(x => x.PermissionId == parentPermission.Id))
                    {
                        role.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = parentPermission.Id,
                            IsAllowed = rp.IsAllowed  // 继承原有状态
                        });
                    }
                }

                role.RolePermissions.Remove(rp);
            }

            await _roleManager.UpdateAsync(role);
        }

        #region Private Methods

        /// <summary>
        /// 根据ID集合获取权限实体（带缓存校验）
        /// </summary>
        private async Task<List<Permission>> GetPermissionsByIdsAsync(IEnumerable<int> ids)
        {
            var uniqueIds = ids.Distinct().ToList();
            return await _context.Permissions
                .Where(p => uniqueIds.Contains(p.Id))
                .ToListAsync();
        }

        #endregion
    }
}