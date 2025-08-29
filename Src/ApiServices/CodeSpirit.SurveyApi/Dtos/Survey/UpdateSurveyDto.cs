using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 更新问卷DTO
/// </summary>
[DisplayName("更新问卷")]
public class UpdateSurveyDto
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
    public SurveyAccessType AccessType { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    [DisplayName("是否为模板")]
    public bool IsTemplate { get; set; }

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
