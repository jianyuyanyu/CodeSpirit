using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.PracticeSetting;

/// <summary>
/// 练习设置查询DTO
/// </summary>
public class PracticeSettingQueryDto : QueryDtoBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [DisplayName("名称")]
    public string? Name { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long? ExamPaperId { get; set; }
    
    /// <summary>
    /// 练习模式
    /// </summary>
    [DisplayName("练习模式")]
    public PracticeMode? PracticeMode { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public PracticeSettingStatus? Status { get; set; }
} 