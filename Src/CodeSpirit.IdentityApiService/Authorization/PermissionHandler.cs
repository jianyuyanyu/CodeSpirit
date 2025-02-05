//// Authorization/PermissionHandler.cs
//using CodeSpirit.IdentityApi.Data;
//using CodeSpirit.IdentityApi.Data.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Distributed;
//using System.Text.Json;

//namespace CodeSpirit.IdentityApi.Authorization
//{
//    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly ApplicationDbContext _context;
//        private readonly IDistributedCache _cache;

//        // 定义缓存过期时间
//        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

//        public PermissionHandler(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IDistributedCache cache)
//        {
//            _userManager = userManager;
//            _context = context;
//            _cache = cache;
//        }

//        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
//        {
//            // 获取当前用户
//            string userId = _userManager.GetUserId(context.User);
//            if (userId == null)
//            {
//                return;
//            }

//            // 定义缓存键
//            string cacheKey = $"UserPermissions_{userId}";

//            // 尝试从缓存中获取用户权限
//            string cachedPermissions = await _cache.GetStringAsync(cacheKey);
//            List<RolePermission> userPermissions;

//            if (!string.IsNullOrEmpty(cachedPermissions))
//            {
//                // 从缓存中反序列化权限列表
//                userPermissions = JsonSerializer.Deserialize<List<RolePermission>>(cachedPermissions);
//            }
//            else
//            {
//                // 缓存未命中，从数据库获取
//                ApplicationUser user = await _userManager.Users
//                    .Include(u => u.UserRoles)
//                    .FirstOrDefaultAsync(u => u.Id == userId);

//                if (user == null)
//                {
//                    return;
//                }

//                List<ApplicationRole> roles = await _context.Roles
//                    .Where(r => user.UserRoles.Any(ur => ur.RoleId == r.Id))
//                    .Include(r => r.RolePermissions)  // 加载角色的权限关系
//                        .ThenInclude(rp => rp.Permission)  // 加载角色权限的具体权限
//                                                           //.ThenInclude(p => p.Children)  // 加载权限的子权限
//                    .ToListAsync();

//                // 获取所有权限，包括继承的权限
//                userPermissions = roles
//                    .SelectMany(rp => rp.RolePermissions)  // 获取权限及其继承的所有权限
//                    .Distinct()
//                    .ToList();

//                // 序列化并设置缓存
//                string serializedPermissions = JsonSerializer.Serialize(userPermissions);
//                DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions
//                {
//                    AbsoluteExpirationRelativeToNow = CacheExpiration
//                };
//                await _cache.SetStringAsync(cacheKey, serializedPermissions, cacheOptions);
//            }

//            // 检查是否存在允许权限
//            bool hasAllow = userPermissions.Any(p => p.Permission.Name == requirement.PermissionName);
//            if (hasAllow)
//            {
//                context.Succeed(requirement);
//            }
//            else
//            {
//                return;
//            }
//        }
//    }
//}
