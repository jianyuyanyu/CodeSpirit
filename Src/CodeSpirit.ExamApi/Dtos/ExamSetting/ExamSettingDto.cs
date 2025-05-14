using System.ComponentModel;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.StudentGroup;

namespace CodeSpirit.ExamApi.Dtos.ExamSetting;

/// <summary>
/// 考试设置DTO
/// </summary>
[DisplayName("考试设置")]
public class ExamSettingDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
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
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    [IgnoreColumn]
    public long ExamPaperId { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    public string ExamPaperName { get; set; } = string.Empty;

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
    [DisplayName("考试时长（分钟）")]
    public int Duration { get; set; }

    /// <summary>
    /// 允许考试次数
    /// </summary>
    [DisplayName("允许考试次数")]
    public int AllowedAttempts { get; set; }

    /// <summary>
    /// 是否启用题目乱序
    /// </summary>
    [DisplayName("是否启用题目乱序")]
    public bool EnableRandomQuestionOrder { get; set; }

    /// <summary>
    /// 是否启用选项乱序
    /// </summary>
    [DisplayName("是否启用选项乱序")]
    public bool EnableRandomOptionOrder { get; set; }

    /// <summary>
    /// 允许切屏次数
    /// </summary>
    [DisplayName("允许切屏次数")]
    public int AllowedScreenSwitchCount { get; set; }

    /// <summary>
    /// 提交后是否可以查看考试结果
    /// </summary>
    [DisplayName("提交后是否可以查看考试结果")]
    public bool EnableViewResult { get; set; }

    /// <summary>
    /// 考试状态
    /// </summary>
    [DisplayName("考试状态")]
    public ExamSettingStatus Status { get; set; }

    /// <summary>
    /// 参加考试的学生分组
    /// </summary>
    [DisplayName("参加考试的学生分组")]
    [ListColumn(title: "name", subTitle: "description")]
    [AmisTableField()]
    public List<StudentGroupDto> StudentGroups { get; set; } = [];

    /// <summary>
    /// 分组ID列表
    /// </summary>
    [IgnoreColumn]
    public List<long> StudentGroupIds { get; set; } = new List<long>();

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 通过率
    /// </summary>
    [DisplayName("通过率")]
    public decimal? PassRate { get; set; }

    /// <summary>
    /// 参考人数
    /// </summary>
    [DisplayName("参考人数")]
    public int TotalParticipants { get; set; }

    /// <summary>
    /// 通过人数
    /// </summary>
    [DisplayName("通过人数")]
    public int PassedParticipants { get; set; }
}