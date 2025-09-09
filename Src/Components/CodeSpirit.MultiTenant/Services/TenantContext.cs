using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeSpirit.MultiTenant.Services;

/// <summary>
/// 租户上下文服务实现
/// 提供统一的租户信息获取方式，支持登录和免登录场景
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<TenantContext> _logger;
    private readonly TenantOptions _options;

    // 缓存当前请求的租户信息，避免重复解析
    private string? _cachedTenantId;
    private string? _cachedTenantName;
    private ITenantInfo? _cachedTenantInfo;
    private bool _tenantInfoLoaded = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="tenantResolver">租户解析器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">租户配置选项</param>
    public TenantContext(
        IHttpContextAccessor httpContextAccessor,
        ICurrentUser currentUser,
        ITenantResolver tenantResolver,
        ILogger<TenantContext> logger,
        IOptions<TenantOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _tenantResolver = tenantResolver;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 获取当前租户ID
    /// 优先级：JWT Claims -> HTTP上下文 -> 默认租户
    /// </summary>
    public string? TenantId
    {
        get
        {
            if (_cachedTenantId != null)
            {
                return _cachedTenantId;
            }

            _cachedTenantId = ResolveTenantId();
            return _cachedTenantId;
        }
    }

    /// <summary>
    /// 获取当前租户名称
    /// </summary>
    public string? TenantName
    {
        get
        {
            if (_cachedTenantName != null)
            {
                return _cachedTenantName;
            }

            // 优先从JWT Claims获取
            if (_currentUser.IsAuthenticated)
            {
                var tenantNameClaim = _currentUser.Claims
                    .FirstOrDefault(c => c.Type == "TenantName")?.Value;
                
                if (!string.IsNullOrEmpty(tenantNameClaim))
                {
                    _cachedTenantName = tenantNameClaim;
                    _logger.LogDebug("从JWT Claims获取租户名称: {TenantName}", _cachedTenantName);
                    return _cachedTenantName;
                }
            }

            // 从HTTP上下文获取
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue("TenantInfo", out var tenantInfoObj) == true 
                && tenantInfoObj is ITenantInfo tenantInfo)
            {
                _cachedTenantName = tenantInfo.Name;
                _logger.LogDebug("从HTTP上下文获取租户名称: {TenantName}", _cachedTenantName);
                return _cachedTenantName;
            }

            _logger.LogDebug("无法获取租户名称");
            return null;
        }
    }

    /// <summary>
    /// 获取当前租户信息
    /// </summary>
    public async Task<ITenantInfo?> GetCurrentTenantInfoAsync()
    {
        if (_tenantInfoLoaded && _cachedTenantInfo != null)
        {
            return _cachedTenantInfo;
        }

        var tenantId = TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogDebug("租户ID为空，无法获取租户信息");
            _tenantInfoLoaded = true;
            return null;
        }

        try
        {
            _cachedTenantInfo = await _tenantResolver.GetTenantInfoAsync(tenantId);
            _tenantInfoLoaded = true;
            
            if (_cachedTenantInfo != null)
            {
                // 缓存租户名称
                _cachedTenantName = _cachedTenantInfo.Name;
                _logger.LogDebug("成功获取租户信息: {TenantId} - {TenantName}", tenantId, _cachedTenantName);
            }
            else
            {
                _logger.LogWarning("无法获取租户信息: {TenantId}", tenantId);
            }

            return _cachedTenantInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户信息时发生异常: {TenantId}", tenantId);
            _tenantInfoLoaded = true;
            return null;
        }
    }

    /// <summary>
    /// 判断是否为指定租户
    /// </summary>
    public bool IsInTenant(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return false;
        }

        var currentTenantId = TenantId;
        return string.Equals(currentTenantId, tenantId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断当前是否有有效的租户上下文
    /// </summary>
    public bool HasTenant => !string.IsNullOrEmpty(TenantId);

    /// <summary>
    /// 强制刷新租户上下文
    /// </summary>
    public async Task RefreshTenantContextAsync()
    {
        _logger.LogDebug("刷新租户上下文");
        
        // 清除缓存
        _cachedTenantId = null;
        _cachedTenantName = null;
        _cachedTenantInfo = null;
        _tenantInfoLoaded = false;

        // 重新加载租户信息
        await GetCurrentTenantInfoAsync();
        
        _logger.LogDebug("租户上下文刷新完成: TenantId={TenantId}, TenantName={TenantName}", 
            TenantId, TenantName);
    }

    /// <summary>
    /// 解析租户ID
    /// 优先级：JWT Claims -> HTTP上下文 -> 默认租户
    /// </summary>
    private string? ResolveTenantId()
    {
        // 1. 优先从JWT Claims获取（登录场景）
        if (_currentUser.IsAuthenticated)
        {
            var tenantIdClaim = _currentUser.Claims
                .FirstOrDefault(c => c.Type == "TenantId")?.Value;
            
            if (!string.IsNullOrEmpty(tenantIdClaim))
            {
                _logger.LogDebug("从JWT Claims获取租户ID: {TenantId}", tenantIdClaim);
                return tenantIdClaim;
            }

            // 也尝试从ICurrentUser接口获取（如果已经实现了多租户扩展）
            var currentUserTenantId = _currentUser.TenantId;
            if (!string.IsNullOrEmpty(currentUserTenantId))
            {
                _logger.LogDebug("从ICurrentUser获取租户ID: {TenantId}", currentUserTenantId);
                return currentUserTenantId;
            }
        }

        // 2. 从HTTP上下文获取（免登录场景，由中间件设置）
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("TenantId", out var tenantIdObj) == true 
            && tenantIdObj is string tenantId 
            && !string.IsNullOrEmpty(tenantId))
        {
            _logger.LogDebug("从HTTP上下文获取租户ID: {TenantId}", tenantId);
            return tenantId;
        }

        // 3. 使用默认租户（如果配置了）
        if (!string.IsNullOrEmpty(_options.DefaultTenantId))
        {
            _logger.LogDebug("使用默认租户ID: {TenantId}", _options.DefaultTenantId);
            return _options.DefaultTenantId;
        }

        _logger.LogDebug("无法解析租户ID");
        return null;
    }

    /// <summary>
    /// 验证当前租户是否有效（存在且活跃）
    /// 用于按需验证场景
    /// </summary>
    public async Task<bool> ValidateCurrentTenantAsync()
    {
        var tenantId = TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogDebug("租户ID为空，验证失败");
            return false;
        }

        try
        {
            var tenantInfo = await _tenantResolver.GetTenantInfoAsync(tenantId);
            if (tenantInfo == null)
            {
                _logger.LogWarning("租户不存在: {TenantId}", tenantId);
                return false;
            }

            if (!tenantInfo.IsActive)
            {
                _logger.LogWarning("租户已禁用: {TenantId}", tenantId);
                return false;
            }

            _logger.LogDebug("租户验证成功: {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证租户时发生异常: {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// 获取当前租户信息并验证其有效性
    /// 如果租户无效，根据配置的失败策略处理
    /// </summary>
    public async Task<ITenantInfo?> GetValidatedCurrentTenantInfoAsync()
    {
        var tenantId = TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            return await HandleTenantValidationFailure("无法解析租户ID");
        }

        try
        {
            var tenantInfo = await _tenantResolver.GetTenantInfoAsync(tenantId);
            if (tenantInfo == null)
            {
                return await HandleTenantValidationFailure($"租户不存在: {tenantId}");
            }

            if (!tenantInfo.IsActive)
            {
                return await HandleTenantValidationFailure($"租户已禁用: {tenantId}");
            }

            // 缓存有效的租户信息
            _cachedTenantInfo = tenantInfo;
            _cachedTenantName = tenantInfo.Name;
            _tenantInfoLoaded = true;

            _logger.LogDebug("获取并验证租户信息成功: {TenantId} - {TenantName}", tenantId, tenantInfo.Name);
            return tenantInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户信息时发生异常: {TenantId}", tenantId);
            return await HandleTenantValidationFailure($"租户验证异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理租户验证失败
    /// </summary>
    /// <param name="message">失败消息</param>
    /// <returns>根据策略返回的租户信息或null</returns>
    private async Task<ITenantInfo?> HandleTenantValidationFailure(string message)
    {
        _logger.LogWarning("租户验证失败: {Message}", message);

        switch (_options.FailureStrategy)
        {
            case TenantResolutionFailureStrategy.UseDefault:
                if (!string.IsNullOrEmpty(_options.DefaultTenantId))
                {
                    _logger.LogInformation("使用默认租户: {DefaultTenantId}", _options.DefaultTenantId);
                    return await _tenantResolver.GetTenantInfoAsync(_options.DefaultTenantId);
                }
                return null;

            case TenantResolutionFailureStrategy.ThrowException:
                var exception = new InvalidOperationException(message);
                _logger.LogError(exception, "租户验证失败，抛出异常");
                throw exception;

            case TenantResolutionFailureStrategy.Return404:
            default:
                _logger.LogWarning("租户验证失败，返回null");
                return null;
        }
    }
}
