using System.ComponentModel;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.SurveyApi.Dtos.App;

/// <summary>
/// App端问卷查询DTO
/// </summary>
[DisplayName("App问卷查询")]
public class AppSurveyQueryDto : QueryDtoBase
{
    /// <summary>
    /// 问卷标题（模糊搜索）
    /// </summary>
    [DisplayName("问卷标题")]
    public string? Title { get; set; }

    /// <summary>
    /// 问卷分类名称
    /// </summary>
    [DisplayName("问卷分类")]
    public string? CategoryName { get; set; }
}
