using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeSpirit.Core;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Events;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// 租户感知事件总线实现
/// 在事件发布和订阅时自动处理租户逻辑
/// </summary>
public class TenantAwareEventBus : ITenantAwareEventBus
{
    private readonly IEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantAwareEventBus> _logger;
    private readonly string _defaultTenantId;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="eventBus">基础事件总线</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    /// <param name="logger">日志记录器</param>
    public TenantAwareEventBus(
        IEventBus eventBus,
        ICurrentUser currentUser,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantAwareEventBus> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _currentUser = currentUser;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _httpContextAccessor = httpContextAccessor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTenantId = "default"; // 默认租户ID，可以从配置读取
    }
    
    /// <summary>
    /// 发布租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>发布任务</returns>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }
        
        try
        {
            // 验证和设置租户ID
            await EnsureTenantIdAsync(@event);
            
            // 验证租户权限
            await ValidateTenantPermissionAsync(@event);
            
            // 验证事件数据完整性
            ValidateEventData(@event);
            
            // 记录事件日志
            _logger.LogInformation("发布租户事件: {EventType}, 租户: {TenantId}, 事件ID: {EventId}, 用户: {UserId}", 
                typeof(TEvent).Name, @event.TenantId, @event.EventId, _currentUser?.Id);
            
            // 添加事件元数据
            AddEventMetadata(@event);
            
            // 发布事件
            await _eventBus.PublishAsync(@event);
            
            _logger.LogDebug("租户事件发布成功: {EventType}, 租户: {TenantId}, 事件ID: {EventId}", 
                typeof(TEvent).Name, @event.TenantId, @event.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布租户事件失败: {EventType}, 租户: {TenantId}, 事件ID: {EventId}", 
                typeof(TEvent).Name, @event.TenantId, @event.EventId);
            throw;
        }
    }
    
    /// <summary>
    /// 发布普通事件（非租户感知）
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>发布任务</returns>
    public async Task PublishNonTenantEventAsync<TEvent>(TEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }
        
        _logger.LogInformation("发布非租户事件: {EventType}", typeof(TEvent).Name);
        await _eventBus.PublishAsync(@event);
    }
    
    /// <summary>
    /// 订阅租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <returns>订阅任务</returns>
    public async Task SubscribeAsync<TEvent, THandler>() 
        where TEvent : ITenantAwareEvent
        where THandler : ITenantAwareEventHandler<TEvent>
    {
        _logger.LogInformation("订阅租户感知事件: {EventType} -> {HandlerType}", 
            typeof(TEvent).Name, typeof(THandler).Name);
            
        await _eventBus.Subscribe<TEvent, THandler>();
    }
    
    /// <summary>
    /// 取消订阅租户感知事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    public void Unsubscribe<TEvent, THandler>()
        where TEvent : ITenantAwareEvent
        where THandler : ITenantAwareEventHandler<TEvent>
    {
        _logger.LogInformation("取消订阅租户感知事件: {EventType} -> {HandlerType}", 
            typeof(TEvent).Name, typeof(THandler).Name);
            
        _eventBus.Unsubscribe<TEvent, THandler>();
    }
    
    /// <summary>
    /// 获取当前租户ID
    /// 参考ApplicationDbContext.GetCurrentTenantId的实现
    /// </summary>
    /// <returns>租户ID</returns>
    private string GetCurrentTenantId()
    {
        try
        {
            // 设计时检查 - 如果是设计时上下文，返回默认值
            if (_currentUser == null && _httpContextAccessor == null)
            {
                return _defaultTenantId;
            }

            // 优先从CurrentUser获取租户ID（更安全，避免异步调用）
            var tenantId = _currentUser?.TenantId;
            
            // 如果CurrentUser中没有，尝试从HttpContext获取
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _httpContextAccessor?.HttpContext?.Items["TenantId"] as string;
            }
            
            // 如果仍然没有，使用默认租户ID
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = _defaultTenantId;
            }
            
            return tenantId;
        }
        catch (Exception ex)
        {
            // 异常情况下，返回默认值
            _logger.LogWarning(ex, "获取租户ID时发生异常，使用默认值");
            return _defaultTenantId;
        }
    }

    /// <summary>
    /// 确保事件包含有效的租户ID
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>处理任务</returns>
    private Task EnsureTenantIdAsync<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent
    {
        if (string.IsNullOrEmpty(@event.TenantId))
        {
            // 自动设置当前租户ID
            @event.TenantId = GetCurrentTenantId();
            _logger.LogDebug("自动设置事件租户ID: {TenantId}", @event.TenantId);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 验证租户权限
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <returns>验证任务</returns>
    private Task ValidateTenantPermissionAsync<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent
    {
        // 验证当前用户是否有权限操作该租户的数据
        if (_currentUser != null && 
            !_currentUser.IsInTenant(@event.TenantId) && 
            !_currentUser.IsInRole("SystemAdmin"))
        {
            var exception = new UnauthorizedAccessException($"用户 {_currentUser.UserName} 无权限访问租户 {@event.TenantId} 的数据");
            _logger.LogWarning(exception, "租户权限验证失败: 用户={UserId}, 租户={TenantId}", 
                _currentUser.Id, @event.TenantId);
            throw exception;
        }
        
        // 简化租户验证 - 只要租户ID不为空就认为有效
        if (string.IsNullOrEmpty(@event.TenantId))
        {
            var exception = new AppServiceException(400, "租户ID不能为空");
            _logger.LogWarning(exception, "租户验证失败: 租户ID为空");
            throw exception;
        }
        
        _logger.LogDebug("租户权限验证通过: 租户={TenantId}, 用户={UserId}", 
            @event.TenantId, _currentUser?.Id);
            
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 验证事件数据的完整性
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    private static void ValidateEventData<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent
    {
        // 检查事件是否实现了验证方法
        if (@event is TenantAwareEventBase eventBase && !eventBase.IsValid())
        {
            throw new ArgumentException($"事件数据不完整或无效: {typeof(TEvent).Name}");
        }
        
        // 基础验证
        if (string.IsNullOrEmpty(@event.TenantId))
        {
            throw new ArgumentException("事件的租户ID不能为空");
        }
        
        if (string.IsNullOrEmpty(@event.EventId))
        {
            throw new ArgumentException("事件ID不能为空");
        }
        
        if (@event.Timestamp == default)
        {
            throw new ArgumentException("事件时间戳无效");
        }
    }
    
    /// <summary>
    /// 添加事件元数据
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    private void AddEventMetadata<TEvent>(TEvent @event) where TEvent : ITenantAwareEvent
    {
        // 添加时间戳（如果未设置）
        if (@event.Timestamp == default)
        {
            @event.Timestamp = DateTime.UtcNow;
        }
        
        // 添加事件来源（如果未设置）
        if (string.IsNullOrEmpty(@event.Source))
        {
            @event.Source = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
        }
        
        // 添加版本信息（如果未设置）
        if (string.IsNullOrEmpty(@event.Version))
        {
            @event.Version = "1.0";
        }
        
        _logger.LogDebug("已添加事件元数据: EventId={EventId}, Source={Source}, Version={Version}", 
            @event.EventId, @event.Source, @event.Version);
    }
}