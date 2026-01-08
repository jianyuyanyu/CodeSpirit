namespace CodeSpirit.ConfigCenter.Sdk.Models;

/// <summary>
/// 配置版本DTO（轻量级，用于轮询检测变更）
/// </summary>
public class ConfigVersionDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 配置版本号
    /// </summary>
    public long Version { get; set; }
}
