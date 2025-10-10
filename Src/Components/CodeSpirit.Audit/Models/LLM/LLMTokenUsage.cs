using System.ComponentModel;

namespace CodeSpirit.Audit.Models.LLM;

/// <summary>
/// Token使用统计
/// </summary>
public class LLMTokenUsage
{
    /// <summary>
    /// 输入Token数
    /// </summary>
    [DisplayName("输入Token")]
    public int InputTokens { get; set; }
    
    /// <summary>
    /// 输出Token数
    /// </summary>
    [DisplayName("输出Token")]
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// 总Token数
    /// </summary>
    [DisplayName("总Token")]
    public int TotalTokens { get; set; }
}

