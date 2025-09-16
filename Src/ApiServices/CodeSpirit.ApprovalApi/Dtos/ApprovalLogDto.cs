using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 审批日志DTO
/// </summary>
public class ApprovalLogDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [DisplayName("日志ID")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    [AmisColumn(Type = "mapping")]
    public ApprovalLogType LogType { get; set; }
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    [DisplayName("操作人ID")]
    [AggregateField("/IdentityApi/Users/{value}", "{RealName}")]
    public string OperatorId { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作人姓名
    /// </summary>
    [DisplayName("操作人姓名")]
    [AmisColumn]
    public string OperatorName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime OperationTime { get; set; }
    
    /// <summary>
    /// 操作结果
    /// </summary>
    [DisplayName("操作结果")]
    [AmisColumn(Type = "mapping")]
    public ApprovalResult? Result { get; set; }
    
    /// <summary>
    /// 操作内容/意见
    /// </summary>
    [DisplayName("操作内容")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 审批日志查询DTO
/// </summary>
public class ApprovalLogQueryDto : QueryDtoBase
{
    /// <summary>
    /// 审批实例ID
    /// </summary>
    [DisplayName("审批实例ID")]
    public long? ApprovalInstanceId { get; set; }
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    [DisplayName("操作人ID")]
    public string? OperatorId { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    public ApprovalLogType? LogType { get; set; }
    
    /// <summary>
    /// 操作开始时间
    /// </summary>
    [DisplayName("操作开始时间")]
    public DateTime? OperationTimeStart { get; set; }
    
    /// <summary>
    /// 操作结束时间
    /// </summary>
    [DisplayName("操作结束时间")]
    public DateTime? OperationTimeEnd { get; set; }
}
