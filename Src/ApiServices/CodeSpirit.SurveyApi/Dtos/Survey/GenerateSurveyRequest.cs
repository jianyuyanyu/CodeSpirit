using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 生成问卷请求
/// </summary>
[DisplayName("生成问卷请求")]
public class GenerateSurveyRequest
{
    /// <summary>
    /// 问卷主题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷主题")]
    [Description("请输入问卷的主题，例如：客户满意度调查、产品反馈收集等")]
    [AmisInputTextField("AI生成", "/survey/api/survey/Surveys/generate-suggestions", 
        Placeholder = "请输入问卷主题", 
        AddOnIcon = "fa fa-magic",
        AddOnLevel = "info",
        // AddOnSize = "sm",
        // AddOnConfirmText = "是否基于当前主题生成问卷建议？",
        AddOnLoadingText = "AI正在生成中...")]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("问卷描述")]
    [Description("详细描述问卷的目的和背景信息，帮助AI更好地生成相关题目")]
    [AmisTextareaField(Placeholder = "请输入问卷描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 问卷类型
    /// </summary>
    [StringLength(100)]
    [DisplayName("问卷类型")]
    [Description("指定问卷类型，如：满意度调查、市场调研、员工反馈等")]
    [AmisFormField(Type = "input-text", Placeholder = "请输入问卷类型")]
    public string? SurveyType { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    [Range(1, 50)]
    [DisplayName("题目数量")]
    [Description("指定要生成的题目数量，建议5-20题为佳")]
    [AmisNumberField(DefaultValue = 10)]
    public int QuestionCount { get; set; } = 10;

    /// <summary>
    /// 目标受众
    /// </summary>
    [StringLength(500)]
    [DisplayName("目标受众")]
    [Description("描述问卷的目标受众群体，如：企业员工、产品用户、学生等")]
    [AmisFormField(Type = "input-text", Placeholder = "请输入目标受众")]
    public string? TargetAudience { get; set; }

    /// <summary>
    /// 调查目标
    /// </summary>
    [StringLength(1000)]
    [DisplayName("调查目标")]
    [Description("明确说明通过此问卷希望达到的目标和获得的信息")]
    [AmisTextareaField(Placeholder = "请输入调查目标")]
    public string? Goals { get; set; }

    /// <summary>
    /// 自定义提示词
    /// </summary>
    [StringLength(4000)]
    [DisplayName("自定义提示词")]
    [Description("可选：提供自定义的AI提示词来指导问卷生成，留空则使用默认提示词")]
    [AmisTextareaField(Placeholder = "请输入自定义提示词（可选）")]
    public string? CustomPrompt { get; set; }
}
