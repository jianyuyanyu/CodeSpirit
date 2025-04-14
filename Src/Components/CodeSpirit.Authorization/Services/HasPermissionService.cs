using CodeSpirit.Core;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Authorization.Services
{
    /// <summary>
    /// 权限检查服务实现
    /// </summary>
    public class HasPermissionService : IHasPermissionService, IScopedDependency
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
        public bool HasPermission(string permissionCode)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogDebug("用户未认证，权限检查失败");
                return false;
            }
            
            // 日志记录当前用户信息和角色
            _logger.LogDebug("执行权限检查: 用户={UserName}, 角色={Roles}, 权限代码={PermissionCode}", 
                _currentUser.UserName, 
                string.Join(",", _currentUser.Roles ?? Array.Empty<string>()), 
                permissionCode);
            
            // 管理员角色直接通过
            if (_currentUser.Roles.Contains("Admin"))
            {
                _logger.LogDebug("用户拥有Admin角色，权限检查通过");
                return true;
            }
            
            // 使用权限服务检查具体权限
            return _permissionService.HasPermission(permissionCode, _currentUser.Permissions);
        }
    }
} 