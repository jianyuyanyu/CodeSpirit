using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用查询参数
/// </summary>
public class AppQueryDto : QueryDtoBase
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    [DisplayName("应用名称")]
    public string Name { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? Enabled { get; set; }
}