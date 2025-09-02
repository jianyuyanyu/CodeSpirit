using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 随机试卷生成DTO
/// </summary>
[DisplayName("生成随机试卷")]
[AiFormFill(TriggerField = nameof(Name), ApiEndpoint = "ai-fill")]
public class GenerateRandomExamPaperDto
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
    [Description("详细描述试卷的内容、适用范围和考试要求")]
    [AiFieldFill(Weight = 2, Priority = 1)]
    public string? Description { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    [Description("试卷的总分值，通常为100分")]
    [AiFieldFill(Weight = 1, Priority = 2)]
    [AmisNumberField(DefaultValue = 100)]
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    [Description("考试及格所需的最低分数，通常为总分的60%")]
    [AiFieldFill(Weight = 1, Priority = 3)]
    [AmisNumberField(DefaultValue = 60)]
    public int PassScore { get; set; } = 60;

    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    [Description("考试的时间限制，单位为分钟，根据题目数量和难度合理设置")]
    [AiFieldFill(Weight = 1, Priority = 4)]
    [AmisNumberField(DefaultValue = 120)]
    public int Duration { get; set; } = 120;

    /// <summary>
    /// 题型分布规则
    /// </summary>
    [DisplayName("题型分布规则")]
    [Required(ErrorMessage = "题型分布规则不能为空")]
    [AmisTableField(Addable = true, Removable = true, Draggable = true, Editable = true)]
    public List<QuestionTypeRule> QuestionTypeRules { get; set; } = [];

    /// <summary>
    /// 难度分布规则
    /// </summary>
    [DisplayName("难度分布规则")]
    [AmisTableField(Addable = true, Removable = true, Draggable = true)]
    public List<DifficultyRule>? DifficultyRules { get; set; }

    ///// <summary>
    ///// 知识点分布规则
    ///// </summary>
    //[DisplayName("知识点分布规则")]
    //public List<KnowledgePointRule>? KnowledgePointRules { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("题目标签")]
    [AmisArrayField(
        Items = "{ \"type\":\"input-text\" }",
        Addable = true,
        Removable = true,
        Draggable = true,
        MaxLength = 5
    )]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 分类ID限制
    /// </summary>
    [DisplayName("题目分类限制")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
        Multiple = true,
        Cascade = true,
        ShowOutline = true,
        Required = true,
        LabelField = "name",
        ValueField = "id",
        JoinValues = false,
        ExtractValue = true
    )]
    public List<long> CategoryIds { get; set; }

    /// <summary>
    /// 是否启用成绩换算
    /// </summary>
    [DisplayName("启用成绩换算")]
    [AmisSwitchField("启用成绩换算", false)]
    public bool EnableScoreConversion { get; set; } = false;

    /// <summary>
    /// 换算目标满分（通常为100）
    /// </summary>
    [DisplayName("换算目标满分")]
    [Required(ErrorMessage = "换算目标满分不能为空")]
    [Range(1, 1000, ErrorMessage = "换算目标满分必须在1-1000之间")]
    [AmisNumberField("换算目标满分", Min = 1, Max = 1000, DefaultValue = 100, VisibleOn = "enableScoreConversion")]
    public int ConversionTargetFullScore { get; set; } = 100;

    /// <summary>
    /// 换算目标及格分（通常为60，会自动更新到PassScore字段）
    /// </summary>
    [DisplayName("换算目标及格分")]
    [Range(0, 1000, ErrorMessage = "换算目标及格分必须在0-1000之间")]
    [AmisNumberField("换算目标及格分", Min = 0, Max = 1000, DefaultValue = 60, VisibleOn = "enableScoreConversion")]
    public int ConversionTargetPassScore { get; set; } = 60;

    /// <summary>
    /// 小数保留位数
    /// </summary>
    [DisplayName("小数保留位数")]
    [Range(0, 2, ErrorMessage = "小数保留位数必须在0-2之间")]
    [AmisNumberField("小数保留位数", Min = 0, Max = 2, DefaultValue = 1, VisibleOn = "enableScoreConversion")]
    public int ConversionDecimalPlaces { get; set; } = 1;
}
