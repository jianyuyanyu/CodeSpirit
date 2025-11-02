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
            _logger.LogDebug("[HasPermissionService] 开始权限检查: 权限代码={PermissionCode}", permissionCode);
            
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogDebug("[HasPermissionService] 用户未认证，权限检查失败");
                return false;
            }

            _logger.LogDebug("[HasPermissionService] 用户已认证: 用户名={UserName}, 角色=[{Roles}]", 
                _currentUser.UserName, string.Join(",", _currentUser.Roles ?? Array.Empty<string>()));

            // 管理员角色直接通过
            if (_currentUser.Roles.Contains("Admin"))
            {
                _logger.LogDebug("[HasPermissionService] 用户拥有Admin角色，权限检查通过");
                return true;
            }

            _logger.LogDebug("[HasPermissionService] 用户权限数量: {PermissionCount}", _currentUser.Permissions?.Count ?? 0);
            if (_currentUser.Permissions?.Count > 0)
            {
                _logger.LogDebug("[HasPermissionService] 用户权限列表: [{Permissions}]", string.Join(",", _currentUser.Permissions));
            }

            // 使用权限服务检查具体权限
            var hasPermission = _permissionService.HasPermission(permissionCode, _currentUser.Permissions);

            _logger.LogDebug("[HasPermissionService] 权限检查结果: {HasPermission}", hasPermission);

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
            var hasPermission = _permissionService.HasPermission(permissionCode, navigationPermissions);

            // 详细调试日志
            _logger.LogDebug("导航权限检查详情: 权限代码={PermissionCode}, 用户原始权限=[{UserPermissions}], 提取的导航权限=[{NavigationPermissions}], 检查结果={HasPermission}",
                permissionCode,
                string.Join(",", _currentUser.Permissions ?? new HashSet<string>()),
                string.Join(",", navigationPermissions),
                hasPermission);

            return hasPermission;
        }

        /// <summary>
        /// 从用户权限列表中提取导航权限（只保留明确的二级权限和通配权限）
        /// </summary>
        /// <param name="permissions">用户权限集合</param>
        /// <returns>导航权限集合</returns>
        private HashSet<string> ExtractNavigationPermissions(ISet<string> permissions)
        {
            if (permissions == null || !permissions.Any())
            {
                return new HashSet<string>();
            }

            var navigationPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions)
            {
                if (string.IsNullOrEmpty(permission)) continue;

                // 检查是否为通配权限（以 _* 结尾）
                if (permission.EndsWith("_*", StringComparison.OrdinalIgnoreCase))
                {
                    // 通配权限：保留一级通配和二级通配
                    var parts = permission.Split('_');
                    
                    // parts.Length <= 3 表示：
                    // - module_* (2部分：module 和 *)
                    // - module_controller_* (3部分：module、controller 和 *)
                    if (parts.Length <= 3)
                    {
                        navigationPermissions.Add(permission);
                        _logger.LogDebug("提取通配导航权限: {Permission}", permission);
                    }
                }
                else
                {
                    // 具体权限：只保留二级权限（module_controller）
                    var parts = permission.Split('_');
                    if (parts.Length == 2)
                    {
                        navigationPermissions.Add(permission);
                        _logger.LogDebug("提取二级导航权限: {Permission}", permission);
                    }
                    // 三级权限（module_controller_action）不会自动提升为二级导航权限
                    // 一级权限（module）也不会被提取，除非是通配权限 module_*
                }
            }

            _logger.LogDebug("从 {PermissionCount} 个用户权限中提取出 {NavigationPermissionCount} 个导航权限",
                permissions.Count,
                navigationPermissions.Count);

            return navigationPermissions;
        }
    }
}