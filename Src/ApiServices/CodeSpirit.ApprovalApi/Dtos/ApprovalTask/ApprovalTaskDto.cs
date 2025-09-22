using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ApprovalApi.Dtos.ApprovalTask;

/// <summary>
/// 审批任务DTO
/// </summary>
public class ApprovalTaskDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public long Id { get; set; }

    /// <summary>
    /// 审批实例ID
    /// </summary>
    [DisplayName("审批实例ID")]
    public long ApprovalInstanceId { get; set; }

    /// <summary>
    /// 审批标题
    /// </summary>
    [DisplayName("审批标题")]
    [AmisColumn]
    public string ApprovalTitle { get; set; } = string.Empty;

    /// <summary>
    /// 工作流节点ID
    /// </summary>
    [DisplayName("工作流节点ID")]
    public long WorkflowNodeId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [DisplayName("节点名称")]
    [AmisColumn]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 审批人ID
    /// </summary>
    [DisplayName("审批人ID")]
    [AggregateField("/IdentityApi/Users/{value}", "{RealName}")]
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// 审批人姓名
    /// </summary>
    [DisplayName("审批人姓名")]
    [AmisColumn]
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    [AmisColumn(Type = "mapping")]
    public ApprovalTaskStatus Status { get; set; }

    /// <summary>
    /// 审批结果
    /// </summary>
    [DisplayName("审批结果")]
    [AmisColumn(Type = "mapping")]
    public ApprovalResult? Result { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    [DisplayName("审批意见")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 分配时间
    /// </summary>
    [DisplayName("分配时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime AssignedTime { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    [DisplayName("处理时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime? ProcessedTime { get; set; }

    /// <summary>
    /// 是否为加签任务
    /// </summary>
    [DisplayName("是否为加签任务")]
    public bool IsAdditionalSign { get; set; }

    /// <summary>
    /// 加签发起人ID
    /// </summary>
    [DisplayName("加签发起人ID")]
    public string? AdditionalSignInitiatorId { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    [DisplayName("业务实体类型")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 业务实体ID
    /// </summary>
    [DisplayName("业务实体ID")]
    public string EntityId { get; set; } = string.Empty;
}

/// <summary>
/// 审批任务查询DTO
/// </summary>
public class ApprovalTaskQueryDto : QueryDtoBase
{
    /// <summary>
    /// 审批人ID
    /// </summary>
    [DisplayName("审批人ID")]
    public string? ApproverId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public ApprovalTaskStatus? Status { get; set; }

    /// <summary>
    /// 分配开始时间
    /// </summary>
    [DisplayName("分配开始时间")]
    public DateTime? AssignedTimeStart { get; set; }

    /// <summary>
    /// 分配结束时间
    /// </summary>
    [DisplayName("分配结束时间")]
    public DateTime? AssignedTimeEnd { get; set; }

    /// <summary>
    /// 是否为加签任务
    /// </summary>
    [DisplayName("是否为加签任务")]
    public bool? IsAdditionalSign { get; set; }
}

/// <summary>
/// 处理审批任务DTO
/// </summary>
public class ProcessApprovalTaskDto
{
    /// <summary>
    /// 审批结果
    /// </summary>
    [Required]
    [DisplayName("审批结果")]
    public ApprovalResult Result { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    [DisplayName("审批意见")]
    [StringLength(1000, ErrorMessage = "审批意见长度不能超过1000个字符")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// 转交目标用户ID（当结果为转交时）
    /// </summary>
    [DisplayName("转交目标用户ID")]
    public string? TransferToUserId { get; set; }

    /// <summary>
    /// 加签用户ID（当结果为加签时）
    /// </summary>
    [DisplayName("加签用户ID")]
    public string? AdditionalSignUserId { get; set; }
}