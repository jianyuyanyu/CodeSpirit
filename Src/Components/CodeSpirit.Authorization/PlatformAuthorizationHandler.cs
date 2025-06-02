using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core.Enums;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core;
using System.Threading.Tasks;

namespace CodeSpirit.Authorization;

/// <summary>
/// 平台权限验证处理器
/// </summary>
public class PlatformAuthorizationHandler : AuthorizationHandler<PlatformRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PlatformAuthorizationHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="logger">日志记录器</param>
    public PlatformAuthorizationHandler(ICurrentUser currentUser, ILogger<PlatformAuthorizationHandler> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// 处理权限验证
    /// </summary>
    /// <param name="context">授权上下文</param>
    /// <param name="requirement">权限要求</param>
    /// <returns></returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformRequirement requirement)
    {
        var platformType = requirement.PlatformType;
        var currentUserTenantId = _currentUser.TenantId;
        
        // 如果用户未认证，直接拒绝
        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogWarning("用户未认证，无法访问 {PlatformType} 平台功能", platformType);
            return Task.CompletedTask;
        }
        
        // 判断用户租户类型
        var isSystemTenant = currentUserTenantId == "system";
        var isDefaultTenant = currentUserTenantId == "default";
        var isBusinessTenant = !string.IsNullOrEmpty(currentUserTenantId) && !isSystemTenant && !isDefaultTenant;

        var hasAccess = platformType switch
        {
            PlatformType.System => isSystemTenant,
            PlatformType.Tenant => isBusinessTenant,
            PlatformType.Both => isSystemTenant || isBusinessTenant,
            PlatformType.None => false,
            _ => false
        };

        if (hasAccess)
        {
            context.Succeed(requirement);
            _logger.LogDebug("用户 {UserId} (租户: {TenantId}) 成功访问 {PlatformType} 平台功能", 
                _currentUser.Id, currentUserTenantId, platformType);
        }
        else
        {
            _logger.LogWarning("用户 {UserId} (租户: {TenantId}) 无权访问 {PlatformType} 平台功能", 
                _currentUser.Id, currentUserTenantId, platformType);
        }

        return Task.CompletedTask;
    }
} 