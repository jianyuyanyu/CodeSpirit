using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项批量发布请求DTO
/// </summary>
public class ConfigItemsBatchPublishDto
{
    /// <summary>
    /// 要发布的配置项ID列表
    /// </summary>
    [Required(ErrorMessage = "ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要一个ID")]
    public List<int> Ids { get; set; } = new();
} 