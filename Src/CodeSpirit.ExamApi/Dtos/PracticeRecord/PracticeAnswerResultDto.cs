using CodeSpirit.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习答题结果DTO
/// </summary>
[DisplayName("答题结果")]
public class PracticeAnswerResultDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户答案
    /// </summary>
    [DisplayName("用户答案")]
    public string UserAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 题目分值
    /// </summary>
    [DisplayName("题目分值")]
    public decimal Score { get; set; }
    
    /// <summary>
    /// 获得分数
    /// </summary>
    [DisplayName("获得分数")]
    public decimal ObtainedScore { get; set; }
    
    /// <summary>
    /// 用时（秒）
    /// </summary>
    [DisplayName("用时")]
    public int TimeSpent { get; set; }
} 