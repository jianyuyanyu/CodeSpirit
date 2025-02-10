using CodeSpirit.Authorization;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
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
        private readonly IPermissionService permissionService;

        public RoleRepository(
            ApplicationDbContext context,
            RoleManager<ApplicationRole> roleManager,
            IPermissionService permissionService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            this.permissionService = permissionService;
        }

        /// <summary>
        /// 分页获取角色列表（带权限信息）
        /// </summary>
        /// <param name="queryDto">包含分页、排序和搜索条件的查询对象</param>
        /// <returns>(角色列表, 总记录数)</returns>
        public async Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto)
        {
            // 基础查询包含权限信息
            IQueryable<ApplicationRole> query = _roleManager.Roles
                .Include(r => r.RolePermission)
                .AsQueryable();

            // 应用关键词过滤
            if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
            {
                string searchLower = queryDto.Keywords.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(searchLower) ||
                    u.Description.ToLower().Contains(searchLower));
            }

            int count = await query.CountAsync();

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
                .Include(r => r.RolePermission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// 创建新角色并关联权限
        /// </summary>
        /// <remarks>需要事务保证角色和权限的原子性操作</remarks>
        public async Task CreateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds)
        {
            // 验证权限是否存在
            var permissions = permissionService.GetPermissionTree()
                .Where(p => permissionIds.Contains(p.Code))
                .Select(p => p.Code)
                .ToArray();

            role.RolePermission = new RolePermission()
            {
                PermissionIds = permissions
            };

            // 创建角色（自动级联创建关联的RolePermissions）
            await _roleManager.CreateAsync(role);
        }

        /// <summary>
        /// 更新角色信息及其权限配置
        /// </summary>
        /// <param name="permissionIds">更新后的完整权限ID集合</param>
        public async Task UpdateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds)
        {
            role.RolePermission = new RolePermission()
            {
                PermissionIds = permissionIds.ToArray()
            };

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
        /// 批量插入角色信息（高性能实现）
        /// </summary>
        /// <param name="roles">待导入的角色集合</param>
        public async Task BulkInsertRolesAsync(IEnumerable<ApplicationRole> roles)
        {
            // 直接添加实体集合到 DbContext
            await _context.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ApplicationRole>> GetRolesByNamesAsync(List<string> roleNames)
        {
            return await _context.Roles
                .Where(role => roleNames.Contains(role.Name))
                .ToListAsync();
        }
    }
}