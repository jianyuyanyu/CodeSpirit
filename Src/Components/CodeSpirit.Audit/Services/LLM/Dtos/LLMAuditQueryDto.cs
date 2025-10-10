using System.ComponentModel;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.Audit.Services.LLM.Dtos;

/// <summary>
/// LLM审计查询DTO
/// </summary>
public class LLMAuditQueryDto : QueryDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// LLM提供商
    /// </summary>
    [DisplayName("LLM提供商")]
    public string? LLMProvider { get; set; }
    
    /// <summary>
    /// 模型名称
    /// </summary>
    [DisplayName("模型名称")]
    public string? ModelName { get; set; }
    
    /// <summary>
    /// 交互类型
    /// </summary>
    [DisplayName("交互类型")]
    public string? InteractionType { get; set; }
    
    /// <summary>
    /// 业务场景
    /// </summary>
    [DisplayName("业务场景")]
    public string? BusinessScenario { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 最小处理时间（毫秒）
    /// </summary>
    [DisplayName("最小处理时间")]
    public long? MinProcessingTime { get; set; }
    
    /// <summary>
    /// 最大处理时间（毫秒）
    /// </summary>
    [DisplayName("最大处理时间")]
    public long? MaxProcessingTime { get; set; }
    
    /// <summary>
    /// 关键词搜索（在提示词和响应中搜索）
    /// </summary>
    [DisplayName("关键词")]
    public string? Keyword { get; set; }
    
    /// <summary>
    /// 批次ID
    /// </summary>
    [DisplayName("批次ID")]
    public string? BatchId { get; set; }
    
    /// <summary>
    /// 业务实体类型
    /// </summary>
    [DisplayName("业务实体类型")]
    public string? BusinessEntityType { get; set; }
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [DisplayName("业务实体ID")]
    public string? BusinessEntityId { get; set; }
}

