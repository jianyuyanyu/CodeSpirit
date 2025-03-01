using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 配置项批量发布请求DTO
/// </summary>
public class ConfigItemsBatchPublishDto
{
    /// <summary>
    /// 要发布的配置项ID集合
    /// </summary>
    [Required]
    public List<int> Ids { get; set; }

    /// <summary>
    /// 发布说明
    /// </summary>
    [StringLength(200)]
    public string Description { get; set; }
}