using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 难度分布规则
/// </summary>
[DisplayName("难度分布规则")]
public class DifficultyRule
{
    /// <summary>
    /// 难度
    /// </summary>
    [DisplayName("难度")]
    public QuestionDifficulty Difficulty { get; set; }
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    public int Percentage { get; set; }
}
