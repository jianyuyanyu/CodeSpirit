using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;

namespace CodeSpirit.ExamApi.Dtos.WrongQuestion;

/// <summary>
/// 错题DTO
/// </summary>
public class WrongQuestionDto
{
    /// <summary>
    /// ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 考生姓名
    /// </summary>
    [DisplayName("考生")]
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string QuestionContent { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public int QuestionType { get; set; }
    
    /// <summary>
    /// 题目类型名称
    /// </summary>
    [DisplayName("题目类型")]
    public string QuestionTypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误次数
    /// </summary>
    [DisplayName("错误次数")]
    public int WrongCount { get; set; }
    
    /// <summary>
    /// 最后一次错误答案
    /// </summary>
    [DisplayName("错误答案")]
    public string LastWrongAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 最后错误时间
    /// </summary>
    [DisplayName("最后错误时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime LastWrongTime { get; set; }
    
    /// <summary>
    /// 分类标签
    /// </summary>
    [DisplayName("标签")]
    public string? Tags { get; set; }
    
    /// <summary>
    /// 考生笔记
    /// </summary>
    [DisplayName("笔记")]
    public string? Notes { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [DisplayName("更新人")]
    public string? UpdatedBy { get; set; }
} 