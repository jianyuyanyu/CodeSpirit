namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 知识点分布规则
/// </summary>
[DisplayName("知识点分布规则")]
public class KnowledgePointRule
{
    /// <summary>
    /// 知识点
    /// </summary>
    [DisplayName("知识点")]
    public string KnowledgePoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    public int Percentage { get; set; }
}
