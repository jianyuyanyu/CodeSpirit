using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 更新配置发布历史DTO
/// </summary>
public class UpdateConfigPublishHistoryDto
{
    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(200)]
    [DisplayName("发布说明")]
    public string Description { get; set; }
}