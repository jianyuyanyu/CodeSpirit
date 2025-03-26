using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 考试记录查询DTO
/// </summary>
public class ExamRecordQueryDto : QueryDtoBase
{
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [DisplayName("考试设置ID")]
    public long? ExamSettingId { get; set; }
    
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// 考试状态
    /// </summary>
    [DisplayName("考试状态")]
    public ExamRecordStatus? Status { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    [DisplayName("是否通过")]
    public bool? IsPassed { get; set; }
    
    /// <summary>
    /// 开始时间范围
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime[]? StartTimeRange { get; set; }
    
    /// <summary>
    /// 提交时间范围
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime[]? SubmitTimeRange { get; set; }
    
    /// <summary>
    /// 作弊嫌疑等级(最小值)
    /// </summary>
    [DisplayName("作弊嫌疑等级(最小值)")]
    public int? MinCheatingSuspicionLevel { get; set; }
} 