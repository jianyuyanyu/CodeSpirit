using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 保存问卷编辑器请求
/// </summary>
[DisplayName("保存问卷编辑器")]
public class SaveSurveyEditorRequest
{
    /// <summary>
    /// 问卷ID（0表示新建）
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("问卷描述")]
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
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<EditorQuestionDto>? Questions { get; set; }
}
