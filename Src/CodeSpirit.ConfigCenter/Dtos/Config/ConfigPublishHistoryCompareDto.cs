using CodeSpirit.Amis.Attributes.FormFields;
using Newtonsoft.Json;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置发布对比结果DTO
/// </summary>
public class ConfigPublishHistoryCompareDto
{
    /// <summary>
    /// 发布历史ID
    /// </summary>
    [DisplayName("发布历史ID")]
    public int Id { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 发布说明
    /// </summary>
    [DisplayName("发布说明")]
    public string Description { get; set; }

    /// <summary>
    /// 发布版本
    /// </summary>
    [DisplayName("发布版本")]
    public long Version { get; set; }

    /// <summary>
    /// 旧配置JSON
    /// </summary>
    [DisplayName("旧配置JSON")]
    [AmisFormField(type: "hidden")]
    public string OldConfigsJson { get; set; }

    /// <summary>
    /// 新配置JSON
    /// </summary>
    [DisplayName("配置对比")]
    [Description("注意：左侧为旧配置，右侧为新的配置。")]
    [AmisFormField(type: "diff-editor",AdditionalConfig = "{\"diffValue\":\"${oldConfigsJson}\",\"language\":\"json\"}")]
    public string NewConfigsJson { get; set; }
}