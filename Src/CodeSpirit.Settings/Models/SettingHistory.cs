using System.ComponentModel;

namespace CodeSpirit.Settings.Models;

/// <summary>
/// 设置历史记录
/// </summary>
public class SettingHistory : AuditableEntityBase<long>
{
    /// <summary>
    /// 设置项ID
    /// </summary>
    [Required]
    [DisplayName]
    public long SettingId { get; set; }
    
    /// <summary>
    /// 设置项
    /// </summary>
    public SettingItem Setting { get; set; } = null!;
    
    /// <summary>
    /// 旧值
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string OldValue { get; set; } = string.Empty;
    
    /// <summary>
    /// 新值
    /// </summary>
    [Required]
    [StringLength(4000)]
    public string NewValue { get; set; } = string.Empty;
    
    /// <summary>
    /// 版本号
    /// </summary>
    public long Version { get; set; }
    
    /// <summary>
    /// 修改原因
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
} 