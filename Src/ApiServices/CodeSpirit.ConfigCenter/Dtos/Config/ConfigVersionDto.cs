using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置版本DTO（轻量级，用于轮询检测变更）
/// </summary>
public class ConfigVersionDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 配置版本号
    /// </summary>
    [DisplayName("版本号")]
    public long Version { get; set; }
}
