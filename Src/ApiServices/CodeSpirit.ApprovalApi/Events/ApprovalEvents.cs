using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.EventBus.Events;

namespace CodeSpirit.ApprovalApi.Events;

/// <summary>
/// 审批事件基类（租户感知）
/// </summary>
public abstract class ApprovalEvent : TenantAwareEventBase
{
    /// <summary>
    /// 审批实例ID
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// 业务实体类型（微服务名称.实体类型）
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 业务实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
}

/// <summary>
/// 审批启动事件
/// </summary>
public class ApprovalStartedEvent : ApprovalEvent
{
    /// <summary>
    /// 申请人ID
    /// </summary>
    public string ApplicantId { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码
    /// </summary>
    public string WorkflowCode { get; set; } = string.Empty;

    /// <summary>
    /// 审批标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务数据快照
    /// </summary>
    public string BusinessDataSnapshot { get; set; } = string.Empty;
}

/// <summary>
/// 审批完成事件
/// </summary>
public class ApprovalCompletedEvent : ApprovalEvent
{
    /// <summary>
    /// 审批结果
    /// </summary>
    public ApprovalStatus Result { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedTime { get; set; }

    /// <summary>
    /// 最终审批意见
    /// </summary>
    public string FinalComment { get; set; } = string.Empty;
}

/// <summary>
/// 任务分配事件
/// </summary>
public class TaskAssignedEvent : ApprovalEvent
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 审批人ID
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 分配时间
    /// </summary>
    public DateTime AssignedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 任务完成事件
/// </summary>
public class TaskCompletedEvent : ApprovalEvent
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 审批人ID
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// 审批结果
    /// </summary>
    public ApprovalResult Result { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime ProcessedTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 业务数据状态更新事件（由业务微服务发布）
/// </summary>
public class BusinessDataStatusChangedEvent : TenantAwareEventBase
{
    /// <summary>
    /// 业务实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 业务实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// 新状态
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// 更新的业务数据
    /// </summary>
    public string UpdatedBusinessData { get; set; } = string.Empty;

    /// <summary>
    /// 更新原因
    /// </summary>
    public string UpdateReason { get; set; } = string.Empty;
}
