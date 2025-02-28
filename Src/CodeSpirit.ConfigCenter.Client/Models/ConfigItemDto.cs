using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Client.Models;

/// <summary>
/// 配置项DTO
/// </summary>
public class ConfigItemsExportDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    [Required(ErrorMessage = "应用ID不能为空")]
    public string AppId { get; set; }
    
    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    [Required(ErrorMessage = "环境不能为空")]
    public string Environment { get; set; }
    
    /// <summary>
    /// 配置项集合，Key为配置键，Value为配置值
    /// </summary>
    [DisplayName("配置项集合")]
    public Dictionary<string, object> Configs { get; set; }
} 