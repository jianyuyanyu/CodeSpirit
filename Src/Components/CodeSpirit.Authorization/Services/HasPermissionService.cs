using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Authorization.Services
{
    /// <summary>
    /// 权限检查服务实现
    /// </summary>
    public class HasPermissionService : BaseHasPermissionService, IScopedDependency
    {
        private readonly ILogger<HasPermissionService> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICurrentUser _currentUser;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="permissionService">权限服务</param>
        /// <param name="currentUser">当前用户</param>
        public HasPermissionService(
            ILogger<HasPermissionService> logger,
            IPermissionService permissionService,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _permissionService = permissionService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// 检查用户是否拥有指定权限
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public override bool HasPermission(string permissionCode)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogDebug("用户未认证，权限检查失败");
                return false;
            }

            // 管理员角色直接通过
            if (_currentUser.Roles.Contains("Admin"))
            {
                _logger.LogDebug("用户拥有Admin角色，权限检查通过");
                return true;
            }

            // 使用权限服务检查具体权限
            var hasPermission = _permissionService.HasPermission(permissionCode, _currentUser.Permissions);

            // 日志记录当前用户信息和角色
            _logger.LogDebug("执行权限检查: 用户={UserName}, 角色={Roles}, 权限代码={PermissionCode}, 检查结果={HasPermission}",
                _currentUser.UserName,
                string.Join(",", _currentUser.Roles ?? Array.Empty<string>()),
                permissionCode,
                hasPermission);

            return hasPermission;
        }

        /// <summary>
        /// 检查用户是否拥有指定导航权限
        /// </summary>
        /// <param name="permissionCode">权限代码</param>
        /// <returns>true 表示权限存在，false 表示权限不存在</returns>
        public override bool HasNavigationPermission(string permissionCode)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogDebug("用户未认证，导航权限检查失败");
                return false;
            }

            // 日志记录当前用户信息和角色
            _logger.LogDebug("执行导航权限检查: 用户={UserName}, 角色={Roles}, 权限代码={PermissionCode}",
                _currentUser.UserName,
                string.Join(",", _currentUser.Roles ?? Array.Empty<string>()),
                permissionCode);

            // 管理员角色直接通过
            if (_currentUser.Roles.Contains("Admin"))
            {
                _logger.LogDebug("用户拥有Admin角色，导航权限检查通过");
                return true;
            }

            // 检查权限代码是否为空
            if (string.IsNullOrEmpty(permissionCode))
            {
                _logger.LogDebug("导航权限代码为空，检查失败");
                return false;
            }

            // 获取用户权限中的导航权限（只保留二级权限）
            var navigationPermissions = ExtractNavigationPermissions(_currentUser.Permissions);

            // 使用权限服务检查导航权限
            return _permissionService.HasPermission(permissionCode, navigationPermissions);
        }

        /// <summary>
        /// 从用户权限列表中提取导航权限（仅二级以及已有的一级权限）
        /// </summary>
        /// <param name="permissions">用户权限集合</param>
        /// <returns>导航权限集合</returns>
        private HashSet<string> ExtractNavigationPermissions(ISet<string> permissions)
        {
            if (permissions == null || !permissions.Any())
            {
                return new HashSet<string>();
            }

            var navigationPermissions = new HashSet<string>();

            foreach (var permission in permissions)
            {
                var parts = permission.Split('_');
                // 只添加一级权限（模块级）
                if (parts.Length == 1)
                {
                    navigationPermissions.Add(permission);
                }
                // 添加二级权限（控制器级）
                else if (parts.Length > 1)
                {
                    navigationPermissions.Add($"{parts[0]}_{parts[1]}");
                }
            }

            _logger.LogDebug("从 {PermissionCount} 个用户权限中提取出 {NavigationPermissionCount} 个导航权限",
                permissions.Count,
                navigationPermissions.Count);

            return navigationPermissions;
        }
    }
}