using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.Client;

/// <summary>
/// 共享的可用考试DTO（用于缓存）
/// </summary>
/// <remarks>
/// 此DTO用于缓存所有可用的考试信息，所有学生共享同一份缓存。
/// 包含考生组信息，便于在内存中进行个性化过滤。
/// </remarks>
[DisplayName("共享可用考试")]
public class SharedAvailableExamDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 考生组ID列表
    /// </summary>
    /// <remarks>
    /// 如果为空列表，表示该考试对所有学生开放（无组限制）
    /// </remarks>
    [DisplayName("考生组ID列表")]
    public List<long> StudentGroupIds { get; set; } = new List<long>();
    
    /// <summary>
    /// 是否对所有学生开放（无组限制）
    /// </summary>
    [DisplayName("无组限制")]
    public bool IsOpenToAll { get; set; }
}

