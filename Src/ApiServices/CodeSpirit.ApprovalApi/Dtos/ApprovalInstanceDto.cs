using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 审批实例DTO
/// </summary>
public class ApprovalInstanceDto
{
    /// <summary>
    /// 实例ID
    /// </summary>
    [DisplayName("实例ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 审批标题
    /// </summary>
    [DisplayName("审批标题")]
    [AmisColumn]
    public string Title { get; set; } = string.Empty;
    
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
    
    /// <summary>
    /// 申请人ID
    /// </summary>
    [DisplayName("申请人ID")]
    [AggregateField("/IdentityApi/Users/{value}", "{RealName}")]
    public string ApplicantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 申请人姓名
    /// </summary>
    [DisplayName("申请人姓名")]
    [AmisColumn]
    public string ApplicantName { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前状态
    /// </summary>
    [DisplayName("当前状态")]
    [AmisColumn(Type = "mapping")]
    public ApprovalStatus Status { get; set; }
    
    /// <summary>
    /// 申请时间
    /// </summary>
    [DisplayName("申请时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime ApplyTime { get; set; }
    
    /// <summary>
    /// 完成时间
    /// </summary>
    [DisplayName("完成时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime? CompletedTime { get; set; }
    
    /// <summary>
    /// 工作流名称
    /// </summary>
    [DisplayName("工作流名称")]
    public string WorkflowName { get; set; } = string.Empty;
}

/// <summary>
/// 审批实例查询DTO
/// </summary>
public class ApprovalInstanceQueryDto : QueryDtoBase
{
    /// <summary>
    /// 审批标题（模糊查询）
    /// </summary>
    [DisplayName("审批标题")]
    public string? Title { get; set; }
    
    /// <summary>
    /// 申请人ID
    /// </summary>
    [DisplayName("申请人ID")]
    public string? ApplicantId { get; set; }
    
    /// <summary>
    /// 审批状态
    /// </summary>
    [DisplayName("审批状态")]
    public ApprovalStatus? Status { get; set; }
    
    /// <summary>
    /// 业务实体类型
    /// </summary>
    [DisplayName("业务实体类型")]
    public string? EntityType { get; set; }
    
    /// <summary>
    /// 申请开始时间
    /// </summary>
    [DisplayName("申请开始时间")]
    public DateTime? ApplyTimeStart { get; set; }
    
    /// <summary>
    /// 申请结束时间
    /// </summary>
    [DisplayName("申请结束时间")]
    public DateTime? ApplyTimeEnd { get; set; }
}

/// <summary>
/// 发起审批DTO
/// </summary>
public class StartApprovalDto
{
    /// <summary>
    /// 工作流代码
    /// </summary>
    [Required]
    [DisplayName("工作流代码")]
    public string WorkflowCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体类型
    /// </summary>
    [Required]
    [DisplayName("业务实体类型")]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [Required]
    [DisplayName("业务实体ID")]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批标题
    /// </summary>
    [Required]
    [DisplayName("审批标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务数据
    /// </summary>
    [DisplayName("业务数据")]
    public object? BusinessData { get; set; }
}

/// <summary>
/// 审批实例详情DTO
/// </summary>
public class ApprovalInstanceDetailDto : ApprovalInstanceDto
{
    /// <summary>
    /// 业务数据
    /// </summary>
    [DisplayName("业务数据")]
    public string BusinessData { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批任务列表
    /// </summary>
    [DisplayName("审批任务列表")]
    public List<ApprovalTaskDto> Tasks { get; set; } = new();
    
    /// <summary>
    /// 审批日志列表
    /// </summary>
    [DisplayName("审批日志列表")]
    public List<ApprovalLogDto> Logs { get; set; } = new();
    
    /// <summary>
    /// 风险评估结果
    /// </summary>
    [DisplayName("风险评估结果")]
    public string? RiskAssessmentResult { get; set; }
    
    /// <summary>
    /// 智能审批建议
    /// </summary>
    [DisplayName("智能审批建议")]
    public string? IntelligentSuggestion { get; set; }
}
