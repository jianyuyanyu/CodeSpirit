using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos;

/// <summary>
/// 可行性评估结果
/// </summary>
public class FeasibilityEvaluation
{
    /// <summary>
    /// 是否可行（可以进行AI任务拆解）
    /// </summary>
    [DisplayName("是否可行")]
    public bool IsFeasible { get; set; }
    
    /// <summary>
    /// 明确性评分 (0-10)，评估目标描述是否清晰明确
    /// </summary>
    [DisplayName("明确性评分")]
    public int ClarityScore { get; set; }
    
    /// <summary>
    /// 可执行性评分 (0-10)，评估目标是否可以被实际执行
    /// </summary>
    [DisplayName("可执行性评分")]
    public int ExecutabilityScore { get; set; }
    
    /// <summary>
    /// 完整性评分 (0-10)，评估目标信息是否完整
    /// </summary>
    [DisplayName("完整性评分")]
    public int CompletenessScore { get; set; }
    
    /// <summary>
    /// 发现的问题列表
    /// </summary>
    [DisplayName("发现的问题")]
    public List<string> Issues { get; set; } = new();
    
    /// <summary>
    /// 澄清问题列表（需要用户进一步明确的地方）
    /// </summary>
    [DisplayName("需要澄清的问题")]
    public List<string> ClarificationQuestions { get; set; } = new();
    
    /// <summary>
    /// 改进建议列表（如何优化目标）
    /// </summary>
    [DisplayName("改进建议")]
    public List<string> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 综合评分 (0-10)，计算属性
    /// </summary>
    [DisplayName("综合评分")]
    public decimal OverallScore => 
        Math.Round((ClarityScore + ExecutabilityScore + CompletenessScore) / 3.0m, 1);
}

