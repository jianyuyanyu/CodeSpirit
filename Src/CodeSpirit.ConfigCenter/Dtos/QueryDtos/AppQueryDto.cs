using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Dtos.QueryDtos;

/// <summary>
/// 应用查询参数
/// </summary>
public class AppQueryDto(string appId = null, string name = null, bool? enabled = null) : QueryDtoBase
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; } = appId;

    /// <summary>
    /// 应用名称
    /// </summary>
    [DisplayName("应用名称")]
    public string Name { get; set; } = name;

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? Enabled { get; set; } = enabled;
} 