using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置发布历史查询DTO
/// </summary>
public class ConfigPublishHistoryQueryDto : QueryDtoBase
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [StringLength(36)]
    [DisplayName("应用")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择应用"
    )]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public EnvironmentType? Environment { get; set; }
} 