using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 创建问卷DTO
/// </summary>
[DisplayName("创建问卷")]
public class CreateSurveyDto
{
    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    [Required(ErrorMessage = "问卷标题不能为空")]
    [StringLength(200, ErrorMessage = "问卷标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [DisplayName("问卷描述")]
    [StringLength(2000, ErrorMessage = "问卷描述长度不能超过2000个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 访问类型
    /// </summary>
    [DisplayName("访问类型")]
    public SurveyAccessType AccessType { get; set; } = SurveyAccessType.Public;

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    [DisplayName("是否为模板")]
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// LLM生成提示词
    /// </summary>
    [DisplayName("LLM提示词")]
    [StringLength(4000, ErrorMessage = "LLM提示词长度不能超过4000个字符")]
    public string? LLMPrompt { get; set; }

    /// <summary>
    /// LLM原始输出内容
    /// </summary>
    [DisplayName("LLM原始输出")]
    [StringLength(8000, ErrorMessage = "LLM原始输出长度不能超过8000个字符")]
    public string? LLMRawOutput { get; set; }

    /// <summary>
    /// 题目数量（用于LLM生成）
    /// </summary>
    [DisplayName("题目数量")]
    [Range(1, 50, ErrorMessage = "题目数量必须在1-50之间")]
    public int? QuestionCount { get; set; }

    /// <summary>
    /// 问卷类型（用于LLM生成）
    /// </summary>
    [DisplayName("问卷类型")]
    public string? SurveyType { get; set; }

    /// <summary>
    /// 问卷分类ID
    /// </summary>
    [DisplayName("问卷分类")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/survey/SurveyCategories/tree",
        Multiple = false,
        Cascade = true,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Clearable = true
    )]
    public int? CategoryId { get; set; }
}
