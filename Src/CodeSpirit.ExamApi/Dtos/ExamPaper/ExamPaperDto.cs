using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷DTO
/// </summary>
[DisplayName("试卷")]
public class ExamPaperDto
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long Id { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    public ExamPaperType Type { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }

    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    public int PassScore { get; set; }

    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    public int Duration { get; set; }

    /// <summary>
    /// 随机试卷规则
    /// </summary>
    [DisplayName("随机试卷规则")]
    [AmisColumn(Type = "json", Toggled = true)]
    public string? RandomRules { get; set; }

    /// <summary>
    /// 试卷难度系数
    /// </summary>
    [DisplayName("试卷难度系数")]
    public int DifficultyLevel { get; set; }

    /// <summary>
    /// 试卷版本
    /// </summary>
    [DisplayName("试卷版本")]
    public int Version { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    [DisplayName("使用次数")]
    public int UsageCount { get; set; }

    /// <summary>
    /// 平均分
    /// </summary>
    [DisplayName("平均分")]
    public decimal AverageScore { get; set; }

    /// <summary>
    /// 通过率
    /// </summary>
    [DisplayName("通过率")]
    public decimal PassRate { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public ExamPaperStatus Status { get; set; }

    /// <summary>
    /// 试卷包含的题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<ExamPaperQuestionDto> Questions { get; set; } = [];

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
}