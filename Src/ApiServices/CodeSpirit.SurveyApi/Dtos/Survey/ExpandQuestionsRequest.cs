using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;
using Newtonsoft.Json;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// AI扩题请求
/// </summary>
[DisplayName("AI扩题请求")]
public class ExpandQuestionsRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    [AmisFormField(Type = "hidden")]
    public int Id { get; set; }

    /// <summary>
    /// 扩展题目数量
    /// </summary>
    [Required]
    [Range(1, 20)]
    [DisplayName("扩展题目数量")]
    [Description("指定要扩展的题目数量，建议1-10题为佳")]
    [AmisNumberField(DefaultValue = 5, Min = 1, Max = 20)]
    public int ExpandCount { get; set; } = 5;

    /// <summary>
    /// 扩展方向
    /// </summary>
    [StringLength(500)]
    [DisplayName("扩展方向")]
    [Description("指定题目扩展的方向，如：深入了解用户体验、增加满意度细分、添加建议收集等")]
    [AmisTextareaField(Placeholder = "请输入扩展方向，如：深入了解用户体验、增加满意度细分等", MinRows = 2, MaxRows = 4)]
    public string? ExpandDirection { get; set; }

    /// <summary>
    /// 题目类型偏好
    /// </summary>
    [DisplayName("题目类型偏好")]
    [Description("选择希望生成的题目类型，不选择则由AI自动决定")]
    [AmisSelectField(Source = "/survey/api/survey/surveys/question-type-options", ValueField = "value", LabelField = "label", 
        Multiple = true, 
        JoinValues = false, 
        ExtractValue = true)]
    public List<string>? PreferredQuestionTypes { get; set; }

    /// <summary>
    /// 自定义提示词
    /// </summary>
    [StringLength(1000)]
    [DisplayName("自定义提示词")]
    [Description("可选：提供额外的提示词来指导AI生成更符合需求的题目")]
    [AmisTextareaField(Placeholder = "可选：输入自定义提示词来指导AI生成", MinRows = 3, MaxRows = 6)]
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// 是否保持问卷风格一致
    /// </summary>
    [DisplayName("保持风格一致")]
    [Description("是否让AI分析现有题目风格并保持一致")]
    [AmisSwitchField(DefaultValue = true)]
    public bool MaintainStyle { get; set; } = true;

    /// <summary>
    /// 插入位置
    /// </summary>
    [DisplayName("插入位置")]
    [Description("选择新题目的插入位置")]
    [AmisSelectField(Source = "/survey/api/survey/surveys/insert-position-options", ValueField = "value", LabelField = "label", DefaultValue = "end")]
    public string InsertPosition { get; set; } = "end";
}
