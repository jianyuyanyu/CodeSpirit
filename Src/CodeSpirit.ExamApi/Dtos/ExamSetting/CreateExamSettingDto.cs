using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.ExamSetting;

/// <summary>
/// 创建考试设置DTO
/// </summary>
[DisplayName("创建考试设置")]
public class CreateExamSettingDto
{
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    [Required(ErrorMessage = "考试名称不能为空")]
    [StringLength(100, ErrorMessage = "考试名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    [StringLength(500, ErrorMessage = "考试描述不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷")]
    [Required(ErrorMessage = "请选择试卷")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/ExamPapers/select-published",
        ValueField = "id",
        LabelField = "name",
        Multiple = false,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择试卷"
    )]
    public long ExamPaperId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    [Required(ErrorMessage = "开始时间不能为空")]
    [AmisDatetimeField(Utc = true)]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    [Required(ErrorMessage = "结束时间不能为空")]
    [AmisDatetimeField(Utc = true)]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长（分钟）")]
    [Required(ErrorMessage = "考试时长不能为空")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; }

    /// <summary>
    /// 允许考试次数
    /// </summary>
    [DisplayName("允许考试次数")]
    [Required(ErrorMessage = "允许考试次数不能为空")]
    [Range(1, 10, ErrorMessage = "允许考试次数必须在1-10次之间")]
    public int AllowedAttempts { get; set; } = 1;

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
    [Range(0, 10, ErrorMessage = "允许切屏次数必须在0-10次之间")]
    public int AllowedScreenSwitchCount { get; set; } = 0;

    /// <summary>
    /// 提交后是否可以查看考试结果
    /// </summary>
    [DisplayName("提交后是否可以查看考试结果")]
    public bool EnableViewResult { get; set; } = false;

    /// <summary>
    /// 参加考试的学生分组ID列表
    /// </summary>
    [DisplayName("参加考试的学生分组")]
    [Required(ErrorMessage = "请选择参加考试的学生分组")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/StudentGroups/select",
        ValueField = "id",
        LabelField = "name",
        Multiple = true,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择参加考试的学生分组"
    )]
    public List<long> StudentGroupIds { get; set; } = [];
}