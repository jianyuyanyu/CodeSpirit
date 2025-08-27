using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷查询DTO
/// </summary>
[DisplayName("问卷查询")]
public class SurveyQueryDto : QueryDtoBase
{
    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    public string? Title { get; set; }

    /// <summary>
    /// 问卷状态
    /// </summary>
    [DisplayName("问卷状态")]
    public SurveyStatus? Status { get; set; }

    /// <summary>
    /// 访问类型
    /// </summary>
    [DisplayName("访问类型")]
    public SurveyAccessType? AccessType { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    [DisplayName("是否为模板")]
    public bool? IsTemplate { get; set; }

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


}
