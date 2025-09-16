using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.ApprovalApi.Events;

/// <summary>
/// 审批启动事件处理器
/// </summary>
public class ApprovalStartedEventHandler : ITenantAwareEventHandler<ApprovalStartedEvent>
{
    private readonly ILogger<ApprovalStartedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ApprovalStartedEventHandler(ILogger<ApprovalStartedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public async Task<bool> CanHandleEventAsync(ApprovalStartedEvent @event)
    {
        // 对于审批启动事件，一般都允许处理
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(ApprovalStartedEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理审批启动事件: 实例ID={InstanceId}, 工作流={WorkflowCode}, 租户={TenantId}",
                @event.InstanceId, @event.WorkflowCode, tenantContext.TenantId);

            // 这里可以添加审批启动后的业务逻辑
            // 例如：发送通知、更新业务状态等

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批启动事件失败: 实例ID={InstanceId}", @event.InstanceId);
            throw;
        }
    }

    /// <summary>
    /// 处理事件（保留向后兼容性）
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ApprovalStartedEvent @event)
    {
        _logger.LogWarning("使用无租户上下文的事件处理，建议升级到租户感知版本");
        await Task.CompletedTask;
    }
}

/// <summary>
/// 审批完成事件处理器
/// </summary>
public class ApprovalCompletedEventHandler : ITenantAwareEventHandler<ApprovalCompletedEvent>
{
    private readonly ILogger<ApprovalCompletedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ApprovalCompletedEventHandler(ILogger<ApprovalCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public async Task<bool> CanHandleEventAsync(ApprovalCompletedEvent @event)
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(ApprovalCompletedEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理审批完成事件: 实例ID={InstanceId}, 结果={Result}, 租户={TenantId}",
                @event.InstanceId, @event.Result, tenantContext.TenantId);

            // 这里可以添加审批完成后的业务逻辑
            // 例如：发送通知、更新业务状态、触发后续流程等

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审批完成事件失败: 实例ID={InstanceId}", @event.InstanceId);
            throw;
        }
    }

    /// <summary>
    /// 处理事件（保留向后兼容性）
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(ApprovalCompletedEvent @event)
    {
        _logger.LogWarning("使用无租户上下文的事件处理，建议升级到租户感知版本");
        await Task.CompletedTask;
    }
}

/// <summary>
/// 任务分配事件处理器
/// </summary>
public class TaskAssignedEventHandler : ITenantAwareEventHandler<TaskAssignedEvent>
{
    private readonly ILogger<TaskAssignedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public TaskAssignedEventHandler(ILogger<TaskAssignedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public async Task<bool> CanHandleEventAsync(TaskAssignedEvent @event)
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(TaskAssignedEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理任务分配事件: 任务ID={TaskId}, 审批人={ApproverId}, 租户={TenantId}",
                @event.TaskId, @event.ApproverId, tenantContext.TenantId);

            // 这里可以添加任务分配后的业务逻辑
            // 例如：发送通知给审批人、更新任务状态等

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理任务分配事件失败: 任务ID={TaskId}", @event.TaskId);
            throw;
        }
    }

    /// <summary>
    /// 处理事件（保留向后兼容性）
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(TaskAssignedEvent @event)
    {
        _logger.LogWarning("使用无租户上下文的事件处理，建议升级到租户感知版本");
        await Task.CompletedTask;
    }
}

/// <summary>
/// 任务完成事件处理器
/// </summary>
public class TaskCompletedEventHandler : ITenantAwareEventHandler<TaskCompletedEvent>
{
    private readonly ILogger<TaskCompletedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public TaskCompletedEventHandler(ILogger<TaskCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public async Task<bool> CanHandleEventAsync(TaskCompletedEvent @event)
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(TaskCompletedEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理任务完成事件: 任务ID={TaskId}, 结果={Result}, 租户={TenantId}",
                @event.TaskId, @event.Result, tenantContext.TenantId);

            // 这里可以添加任务完成后的业务逻辑
            // 例如：推进工作流、发送通知等

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理任务完成事件失败: 任务ID={TaskId}", @event.TaskId);
            throw;
        }
    }

    /// <summary>
    /// 处理事件（保留向后兼容性）
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(TaskCompletedEvent @event)
    {
        _logger.LogWarning("使用无租户上下文的事件处理，建议升级到租户感知版本");
        await Task.CompletedTask;
    }
}

/// <summary>
/// 业务数据状态变更事件处理器
/// </summary>
public class BusinessDataStatusChangedEventHandler : ITenantAwareEventHandler<BusinessDataStatusChangedEvent>
{
    private readonly ILogger<BusinessDataStatusChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public BusinessDataStatusChangedEventHandler(ILogger<BusinessDataStatusChangedEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证事件的租户权限
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <returns>是否有权限处理该事件</returns>
    public async Task<bool> CanHandleEventAsync(BusinessDataStatusChangedEvent @event)
    {
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 处理租户感知事件（带上下文）
    /// </summary>
    /// <param name="event">事件实例</param>
    /// <param name="tenantContext">租户上下文</param>
    /// <returns>处理任务</returns>
    public async Task HandleWithTenantContextAsync(BusinessDataStatusChangedEvent @event, ITenantEventContext tenantContext)
    {
        try
        {
            _logger.LogInformation("处理业务数据状态变更事件: 实体类型={EntityType}, 实体ID={EntityId}, 状态={Status}, 租户={TenantId}",
                @event.EntityType, @event.EntityId, @event.NewStatus, tenantContext.TenantId);

            // 这里可以添加业务数据状态变更后的逻辑
            // 例如：同步外部系统、更新缓存、发送通知等

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理业务数据状态变更事件失败: 实体类型={EntityType}, 实体ID={EntityId}",
                @event.EntityType, @event.EntityId);
            throw;
        }
    }

    /// <summary>
    /// 处理事件（保留向后兼容性）
    /// </summary>
    /// <param name="event">事件</param>
    /// <returns>处理任务</returns>
    public async Task HandleAsync(BusinessDataStatusChangedEvent @event)
    {
        _logger.LogWarning("使用无租户上下文的事件处理，建议升级到租户感知版本");
        await Task.CompletedTask;
    }
}