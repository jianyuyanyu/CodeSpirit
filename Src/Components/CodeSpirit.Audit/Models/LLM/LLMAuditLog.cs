using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core;

namespace CodeSpirit.Audit.Models.LLM;

/// <summary>
/// LLM审计日志模型
/// </summary>
public class LLMAuditLog : IMultiTenant
{
    /// <summary>
    /// 审计ID
    /// </summary>
    [DisplayName("审计ID")]
    [Key]
    [StringLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [StringLength(50)]
    [Required]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间（UTC）
    /// </summary>
    [DisplayName("操作时间")]
    [Required]
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// LLM提供商
    /// </summary>
    [DisplayName("LLM提供商")]
    [StringLength(50)]
    public string LLMProvider { get; set; } = string.Empty;
    
    /// <summary>
    /// LLM模型名称
    /// </summary>
    [DisplayName("模型名称")]
    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// 交互类型
    /// </summary>
    [DisplayName("交互类型")]
    [StringLength(50)]
    public string InteractionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务场景
    /// </summary>
    [DisplayName("业务场景")]
    [StringLength(100)]
    public string BusinessScenario { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统提示词
    /// </summary>
    [DisplayName("系统提示词")]
    public string SystemPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户提示词
    /// </summary>
    [DisplayName("用户提示词")]
    public string UserPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// LLM原始响应
    /// </summary>
    [DisplayName("LLM响应")]
    public string LLMResponse { get; set; } = string.Empty;
    
    /// <summary>
    /// 处理后的数据
    /// </summary>
    [DisplayName("处理后数据")]
    public string ProcessedData { get; set; } = string.Empty;
    
    /// <summary>
    /// Token使用统计
    /// </summary>
    [DisplayName("Token使用")]
    public LLMTokenUsage TokenUsage { get; set; } = new LLMTokenUsage();
    
    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    [DisplayName("处理耗时(ms)")]
    [Range(0, long.MaxValue)]
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// 成本（美元）
    /// </summary>
    [DisplayName("成本(USD)")]
    public decimal? CostUsd { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    [DisplayName("重试次数")]
    [Range(0, int.MaxValue)]
    public int RetryCount { get; set; }
    
    /// <summary>
    /// 是否进行了JSON修复
    /// </summary>
    [DisplayName("JSON修复")]
    public bool WasJsonRepaired { get; set; }
    
    /// <summary>
    /// 质量评分（1-10）
    /// </summary>
    [DisplayName("质量评分")]
    [Range(1, 10)]
    public int? QualityScore { get; set; }
    
    /// <summary>
    /// 批次ID（用于关联同一批次的多个LLM调用）
    /// </summary>
    [DisplayName("批次ID")]
    [StringLength(50)]
    public string? BatchId { get; set; }
    
    /// <summary>
    /// 批次序号（在批次中的序号）
    /// </summary>
    [DisplayName("批次序号")]
    public int? BatchSequence { get; set; }
    
    /// <summary>
    /// 父审计ID（用于关联重试和修正操作）
    /// </summary>
    [DisplayName("父审计ID")]
    [StringLength(50)]
    public string? ParentAuditId { get; set; }
    
    /// <summary>
    /// 关联的业务实体ID（如题目ID、试卷ID等）
    /// </summary>
    [DisplayName("业务实体ID")]
    [StringLength(50)]
    public string? BusinessEntityId { get; set; }
    
    /// <summary>
    /// 关联的业务实体类型（Question、Exam等）
    /// </summary>
    [DisplayName("业务实体类型")]
    [StringLength(50)]
    public string? BusinessEntityType { get; set; }
    
    /// <summary>
    /// 处理的数据量（如生成的题目数、审核的题目数）
    /// </summary>
    [DisplayName("数据量")]
    public int? DataCount { get; set; }
    
    /// <summary>
    /// 附加元数据
    /// </summary>
    [DisplayName("元数据")]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

