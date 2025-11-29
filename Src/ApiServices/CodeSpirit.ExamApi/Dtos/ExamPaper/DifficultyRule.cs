using CodeSpirit.ExamApi.Data.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 难度分布规则
/// 说明：控制试卷中不同难度题目的比例分布。可以不配置难度规则，系统将随机选择题目。
/// </summary>
[DisplayName("难度分布规则")]
public class DifficultyRule
{
    /// <summary>
    /// 难度
    /// </summary>
    [DisplayName("难度")]
    [Description("选择题目难度等级（简单、中等、困难）")]
    public QuestionDifficulty Difficulty { get; set; }
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    [Description("该难度题目占比。难度规则可选，比例总和可以不为100%")]
    public int Percentage { get; set; }
}
