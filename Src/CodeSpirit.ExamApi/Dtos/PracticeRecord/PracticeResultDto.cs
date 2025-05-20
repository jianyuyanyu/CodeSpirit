using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习结果DTO
/// </summary>
[DisplayName("练习结果")]
public class PracticeResultDto
{
    /// <summary>
    /// 练习记录ID
    /// </summary>
    [DisplayName("记录ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 练习设置ID
    /// </summary>
    [DisplayName("练习设置ID")]
    public long PracticeSettingId { get; set; }
    
    /// <summary>
    /// 练习名称
    /// </summary>
    [DisplayName("练习名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 练习模式
    /// </summary>
    [DisplayName("练习模式")]
    public PracticeMode PracticeMode { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    [DisplayName("得分")]
    public decimal Score { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public decimal TotalScore { get; set; }
    
    /// <summary>
    /// 正确题目数
    /// </summary>
    [DisplayName("正确题目数")]
    public int CorrectCount { get; set; }
    
    /// <summary>
    /// 题目总数
    /// </summary>
    [DisplayName("题目总数")]
    public int TotalCount { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public PracticeSessionStatus Status { get; set; }
    
    /// <summary>
    /// 答题详情
    /// </summary>
    [DisplayName("答题详情")]
    public List<PracticeAnswerResultDto> Answers { get; set; } = new List<PracticeAnswerResultDto>();
} 