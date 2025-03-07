using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 配置发布历史DTO
/// </summary>
public class ConfigPublishHistoryDto
{
    /// <summary>
    /// 发布历史ID
    /// </summary>
    [DisplayName("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    [DisplayName("应用名称")]
    public string AppName { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("发布说明")]
    public string Description { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本")]
    public long Version { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [DisplayName("发布时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 发布人（通过CreatedBy属性获取发布人信息）
    /// TODO: 应提供聚合器独立的内部接口
    /// </summary>
    [DisplayName("发布人")]
    [AggregateField(dataSource: "http://identity/api/identity/users/{value}.data.name", template: "用户: {field}")]
    public string CreatedBy { get; set; }
}
