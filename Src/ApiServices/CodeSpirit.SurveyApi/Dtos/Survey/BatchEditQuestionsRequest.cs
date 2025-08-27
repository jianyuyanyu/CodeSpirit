using CodeSpirit.SurveyApi.Dtos.Question;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 批量编辑题目请求
/// </summary>
[DisplayName("批量编辑题目")]
public class BatchEditQuestionsRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 要批量编辑的题目列表
    /// </summary>
    [Required]
    [DisplayName("题目列表")]
    public List<BatchEditQuestionItem> Questions { get; set; } = new();
}

/// <summary>
/// 批量编辑题目项
/// </summary>
[DisplayName("批量编辑题目项")]
public class BatchEditQuestionItem
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    [DisplayName("题目ID")]
    public int Id { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [StringLength(500)]
    [DisplayName("题目标题")]
    public string? Title { get; set; }

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int? OrderIndex { get; set; }

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    [StringLength(2000)]
    [DisplayName("验证规则")]
    public string? Validation { get; set; }

    /// <summary>
    /// 题目设置（JSON格式）
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目设置")]
    public string? Settings { get; set; }
}
