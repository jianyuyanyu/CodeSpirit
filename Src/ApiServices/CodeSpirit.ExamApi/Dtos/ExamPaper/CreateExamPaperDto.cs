using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models.Enums;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷创建DTO
/// </summary>
[DisplayName("创建试卷")]
public class CreateExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    [Required(ErrorMessage = "试卷类型不能为空")]
    [AmisFormField(VisibleOn = "false")]
    public ExamPaperType Type { get; set; } = ExamPaperType.Fixed;

    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    [AmisNumberField(DefaultValue = 100)]
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    [AmisNumberField(DefaultValue = 60)]
    public int PassScore { get; set; } = 60;

    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    [AmisNumberField(DefaultValue = 120)]
    public int Duration { get; set; } = 120;

    /// <summary>
    /// 题目列表
    /// </summary>
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Questions/select-list",
        ValueField = "id",
        LabelField = "content",
        Multiple = true,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Required = true,
        Placeholder = "请选择题目"
    )]
    [DisplayName("题目列表")]
    public List<string> QuestionIds { get; set; }
}
