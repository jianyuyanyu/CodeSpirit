using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 题型分布规则
/// </summary>
[DisplayName("题型分布规则")]
public class QuestionTypeRule
{
    /// <summary>
    /// 题型
    /// </summary>
    [DisplayName("题型")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 数量
    /// </summary>
    [DisplayName("数量")]
    [Range(1, 100, ErrorMessage = "题目数量必须在1-100之间")]
    public int Count { get; set; }
    
    /// <summary>
    /// 每题分数
    /// </summary>
    [DisplayName("每题分数")]
    [Range(1, 100, ErrorMessage = "每题分数必须在1-100之间")]
    public int ScorePerQuestion { get; set; }
}
