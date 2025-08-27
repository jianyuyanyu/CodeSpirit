using System.ComponentModel;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 题目查询DTO
/// </summary>
[DisplayName("题目查询")]
public class QuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷")]
    [AmisListSelectField(Source = "/survey/api/survey/surveys/options", ValueField = "id", LabelField = "title", Clearable = true, Searchable = true)]
    [PageAside]
    public int? SurveyId { get; set; }

    /// <summary>
    /// 题目标题（模糊搜索）
    /// </summary>
    [DisplayName("题目标题")]
    public string? Title { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType? Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// 是否由LLM生成
    /// </summary>
    [DisplayName("LLM生成")]
    public bool? LLMGenerated { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [DisplayName("创建者")]
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 最小排序索引
    /// </summary>
    [DisplayName("最小排序")]
    public int? MinOrderIndex { get; set; }

    /// <summary>
    /// 最大排序索引
    /// </summary>
    [DisplayName("最大排序")]
    public int? MaxOrderIndex { get; set; }
}
