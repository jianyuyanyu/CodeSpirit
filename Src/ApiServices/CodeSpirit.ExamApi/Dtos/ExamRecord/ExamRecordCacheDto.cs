using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 考试记录缓存DTO（轻量级，仅包含答题过程中需要验证的字段）
/// </summary>
public class ExamRecordCacheDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// 考生ID
    /// </summary>
    public long StudentId { get; set; }
    
    /// <summary>
    /// 考试状态
    /// </summary>
    public ExamRecordStatus Status { get; set; }
    
    /// <summary>
    /// 考试设置ID
    /// </summary>
    public long ExamSettingId { get; set; }
}

