using CodeSpirit.ExamApi.Data.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 题型分布规则
/// 说明：定义试卷中各题型的数量和分值。所有题型的总分之和必须等于试卷总分。
/// </summary>
[DisplayName("题型分布规则")]
public class QuestionTypeRule
{
    /// <summary>
    /// 题型
    /// </summary>
    [DisplayName("题型")]
    [Description("选择题目类型（单选题、多选题、判断题等）")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 数量
    /// </summary>
    [DisplayName("数量")]
    [Range(1, 100, ErrorMessage = "题目数量必须在1-100之间")]
    [Description("该题型的题目数量")]
    public int Count { get; set; }
    
    /// <summary>
    /// 每题分数
    /// </summary>
    [DisplayName("每题分数")]
    [Range(1, 100, ErrorMessage = "每题分数必须在1-100之间")]
    [Description("该题型每道题的分值")]
    public int ScorePerQuestion { get; set; }
}
