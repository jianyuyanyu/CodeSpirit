using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习会话DTO
/// </summary>
public class PracticeSessionDto
{
    /// <summary>
    /// 会话ID
    /// </summary>
    [Display(Name = "会话ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 学生ID
    /// </summary>
    [Display(Name = "学生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 学生姓名
    /// </summary>
    [Display(Name = "学生姓名")]
    public string StudentName { get; set; }
    
    /// <summary>
    /// 练习名称
    /// </summary>
    [Display(Name = "练习名称")]
    public string PracticeName { get; set; }
    
    /// <summary>
    /// 练习ID
    /// </summary>
    [Display(Name = "练习ID")]
    public long PracticeId { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [Display(Name = "开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [Display(Name = "结束时间")]
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 用时(分钟)
    /// </summary>
    [Display(Name = "用时(分钟)")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    [Display(Name = "得分")]
    public decimal Score { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [Display(Name = "总分")]
    public decimal TotalScore { get; set; }
    
    /// <summary>
    /// 总题目数
    /// </summary>
    [Display(Name = "总题目数")]
    public int TotalQuestions { get; set; }
    
    /// <summary>
    /// 正确题目数
    /// </summary>
    [Display(Name = "正确题目数")]
    public int CorrectCount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Display(Name = "创建时间")]
    public DateTime CreatedTime { get; set; }
} 