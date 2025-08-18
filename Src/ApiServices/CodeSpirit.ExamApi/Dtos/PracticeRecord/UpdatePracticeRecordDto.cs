using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 更新练习记录DTO
/// </summary>
public class UpdatePracticeRecordDto
{
    /// <summary>
    /// 考生回答
    /// </summary>
    [DisplayName("考生回答")]
    public string? Answer { get; set; }
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool? IsCorrect { get; set; }
    
    /// <summary>
    /// 耗时（秒）
    /// </summary>
    [Range(0, int.MaxValue)]
    [DisplayName("耗时（秒）")]
    public int? TimeSpent { get; set; }
    
    /// <summary>
    /// 练习时间
    /// </summary>
    [DisplayName("练习时间")]
    public DateTime? PracticeTime { get; set; }
} 