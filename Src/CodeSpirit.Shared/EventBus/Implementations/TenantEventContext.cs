using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// 租户事件上下文实现
/// 提供事件处理过程中的租户信息和服务
/// </summary>
public class TenantEventContext : ITenantEventContext, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<TenantEventContext> _logger;
    private readonly Lazy<IServiceScope> _tenantScope;

    /// <summary>
    /// 当前租户ID
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// 是否允许跨租户操作
    /// </summary>
    public bool AllowCrossTenantAccess { get; private set; }

    /// <summary>
    /// 事件处理的用户ID
    /// </summary>
    public long? UserId => _currentUser?.Id;

    /// <summary>
    /// 事件处理的用户名
    /// </summary>
    public string? UserName => _currentUser?.UserName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="logger">日志记录器</param>
    public TenantEventContext(
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        string tenantId,
        ILogger<TenantEventContext> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _currentUser = currentUser;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));

        // 初始化跨租户访问权限
        AllowCrossTenantAccess = _currentUser?.IsInRole("SystemAdmin") ?? false;

        // 延迟创建租户作用域
        _tenantScope = new Lazy<IServiceScope>(CreateTenantScope);

        _logger.LogDebug("租户事件上下文初始化完成: 租户={TenantId},用户={UserId}, 跨租户访问={AllowCrossTenantAccess}",
            TenantId, UserId, AllowCrossTenantAccess);
    }

    /// <summary>
    /// 获取租户专用的服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public T GetTenantService<T>() where T : class
    {
        try
        {
            var scope = _tenantScope.Value;
            return scope.ServiceProvider.GetRequiredService<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户服务失败: 服务类型={ServiceType}, 租户={TenantId}",
                typeof(T).Name, TenantId);
            throw;
        }
    }

    /// <summary>
    /// 获取租户专用的作用域服务提供者
    /// </summary>
    /// <returns>服务提供者</returns>
    public IServiceProvider GetTenantServiceProvider()
    {
        return _tenantScope.Value.ServiceProvider;
    }

    /// <summary>
    /// 设置当前用户的额外信息（可选，仅在需要时调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    public void SetCurrentUserInfo(long? userId = null, string? userName = null)
    {
        var currentUser = GetTenantService<ICurrentUser>();
        if (currentUser is ISettableCurrentUser settableCurrentUser)
        {
            if (userId.HasValue)
            {
                settableCurrentUser.SetUserId(userId.Value);
                _logger.LogDebug("设置CurrentUser用户ID: {UserId}", userId.Value);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                settableCurrentUser.SetUserName(userName);
                _logger.LogDebug("设置CurrentUser用户名: {UserName}", userName);
            }
        }
        else
        {
            _logger.LogWarning("CurrentUser未实现ISettableCurrentUser接口，无法设置用户信息");
        }
    }

    /// <summary>
    /// 创建租户专用的服务作用域
    /// </summary>
    /// <returns>服务作用域</returns>
    private IServiceScope CreateTenantScope()
    {
        try
        {
            var scope = _serviceProvider.CreateScope();

            // 设置租户上下文到HttpContext（如果存在）
            var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
            if (httpContextAccessor?.HttpContext != null)
            {
                httpContextAccessor.HttpContext.Items["TenantId"] = TenantId;
                _logger.LogDebug("已设置租户上下文到HttpContext: 租户={TenantId}", TenantId);
            }

            // 自动设置CurrentUser的租户信息
            var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
            if (currentUser is ISettableCurrentUser settableCurrentUser)
            {
                settableCurrentUser.SetTenantId(TenantId);
                _logger.LogDebug("已自动设置CurrentUser租户ID: {TenantId}", TenantId);

                if (UserId != null)
                {
                    settableCurrentUser.SetUserId(UserId);
                    _logger.LogDebug("已自动设置UserId: {UserId}", UserId);
                }


                // 返回包装的作用域，在销毁时自动重置CurrentUser
                return new AutoResetCurrentUserScope(scope, settableCurrentUser, _logger);
            }
            else
            {
                _logger.LogWarning("CurrentUser未实现ISettableCurrentUser接口，无法自动设置租户上下文");
                return scope;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建租户服务作用域失败: 租户={TenantId}", TenantId);
            throw;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_tenantScope.IsValueCreated)
        {
            _tenantScope.Value.Dispose();
        }
    }

    /// <summary>
    /// 自动重置CurrentUser的服务作用域包装器
    /// </summary>
    internal class AutoResetCurrentUserScope : IServiceScope
    {
        private readonly IServiceScope _innerScope;
        private readonly ISettableCurrentUser _settableCurrentUser;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public AutoResetCurrentUserScope(IServiceScope innerScope, ISettableCurrentUser settableCurrentUser, ILogger logger)
        {
            _innerScope = innerScope ?? throw new ArgumentNullException(nameof(innerScope));
            _settableCurrentUser = settableCurrentUser ?? throw new ArgumentNullException(nameof(settableCurrentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IServiceProvider ServiceProvider => _innerScope.ServiceProvider;

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // 重置CurrentUser状态
                    _settableCurrentUser.Reset();
                    _logger.LogDebug("已自动重置CurrentUser状态");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "重置CurrentUser状态时发生异常");
                }
                finally
                {
                    _innerScope.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}