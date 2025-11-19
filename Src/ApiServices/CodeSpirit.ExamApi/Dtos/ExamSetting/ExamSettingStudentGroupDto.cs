using CodeSpirit.Amis.Attributes.Columns;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamSetting;

/// <summary>
/// 考试设置中的学生分组DTO（简化版，仅用于考试设置显示）
/// </summary>
public class ExamSettingStudentGroupDto
{
    /// <summary>
    /// 分组名称
    /// </summary>
    [DisplayName("分组名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分组描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 考生数量
    /// </summary>
    [DisplayName("考生数量")]
    public int StudentCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }
}

