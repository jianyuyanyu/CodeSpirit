using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Audit.Models.LLM;

/// <summary>
/// LLM交互类型枚举
/// </summary>
public enum LLMInteractionType
{
    /// <summary>
    /// 内容生成
    /// </summary>
    [Display(Name = "内容生成")]
    ContentGeneration = 1,
    
    /// <summary>
    /// 批量生成
    /// </summary>
    [Display(Name = "批量生成")]
    BatchGeneration = 2,
    
    /// <summary>
    /// 格式修正
    /// </summary>
    [Display(Name = "格式修正")]
    FormatCorrection = 3,
    
    /// <summary>
    /// 内容审核
    /// </summary>
    [Display(Name = "内容审核")]
    ContentAudit = 4,
    
    /// <summary>
    /// 批量审核
    /// </summary>
    [Display(Name = "批量审核")]
    BatchAudit = 5,
    
    /// <summary>
    /// 结构化任务处理
    /// </summary>
    [Display(Name = "结构化任务")]
    StructuredTask = 6,
    
    /// <summary>
    /// 重试操作
    /// </summary>
    [Display(Name = "重试操作")]
    Retry = 7,
    
    /// <summary>
    /// 其他
    /// </summary>
    [Display(Name = "其他")]
    Other = 99
}

