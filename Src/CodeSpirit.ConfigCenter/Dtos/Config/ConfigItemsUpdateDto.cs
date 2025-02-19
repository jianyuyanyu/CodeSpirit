using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class ConfigItemsUpdateDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    public string Environment { get; set; }

    /// <summary>
    /// 配置项集合，Key为配置键，Value为配置值
    /// </summary>
    [Required]
    public Dictionary<string, string> Configs { get; set; }
} 