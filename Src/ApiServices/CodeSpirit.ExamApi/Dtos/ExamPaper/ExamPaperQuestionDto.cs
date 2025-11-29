using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷题目DTO
/// </summary>
[DisplayName("试卷题目")]
public class ExamPaperQuestionDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目版本ID
    /// </summary>
    [DisplayName("题目版本ID")]
    public long QuestionVersionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }
    
    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<string> Options { get; set; } = [];
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目解析
    /// </summary>
    [DisplayName("题目解析")]
    public string? Analysis { get; set; }
    
    /// <summary>
    /// 分值
    /// </summary>
    [DisplayName("分值")]
    public int Score { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    public int OrderNumber { get; set; }
    
    /// <summary>
    /// 是否必答
    /// </summary>
    [DisplayName("是否必答")]
    public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// 题目标签（JSON数组格式）
    /// </summary>
    [DisplayName("题目标签")]
    public string? Tags { get; set; }
}
