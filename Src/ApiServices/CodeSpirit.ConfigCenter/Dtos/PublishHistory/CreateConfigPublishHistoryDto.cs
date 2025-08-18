using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 创建配置发布历史DTO
/// </summary>
public class CreateConfigPublishHistoryDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(200)]
    [DisplayName("发布说明")]
    public string Description { get; set; }

    /// <summary>
    /// 待发布的配置项列表
    /// </summary>
    [Required]
    public List<ConfigItemForPublishDto> ConfigItems { get; set; } = new();


}
