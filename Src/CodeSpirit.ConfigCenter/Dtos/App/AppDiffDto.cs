using System.Text.Json.Serialization;

namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用差异数据传输对象
/// </summary>
public class AppDiffDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 启用状态
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// 自动发布状态
    /// </summary>
    public bool? AutoPublish { get; set; }
} 