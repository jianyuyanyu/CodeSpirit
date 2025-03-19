using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习记录批量导入DTO
/// </summary>
public class PracticeRecordBatchImportDto
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    [Required(ErrorMessage = "考生ID不能为空")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    [Required(ErrorMessage = "题目ID不能为空")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 练习类型
    /// </summary>
    [DisplayName("练习类型")]
    [Required(ErrorMessage = "练习类型不能为空")]
    public PracticeType PracticeType { get; set; }
    
    /// <summary>
    /// 考生回答
    /// </summary>
    [DisplayName("考生回答")]
    [Required(ErrorMessage = "考生回答不能为空")]
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 耗时（秒）
    /// </summary>
    [DisplayName("耗时(秒)")]
    [Required(ErrorMessage = "耗时不能为空")]
    [Range(0, int.MaxValue, ErrorMessage = "耗时必须为正整数")]
    public int TimeSpent { get; set; }
    
    /// <summary>
    /// 练习时间
    /// </summary>
    [DisplayName("练习时间")]
    public DateTime PracticeTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 模拟考试ID
    /// </summary>
    [DisplayName("模拟考试ID")]
    public long? MockExamId { get; set; }
} 