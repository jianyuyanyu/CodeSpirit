namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项 DTO
/// </summary>
public class ConfigItemDto
{
    /// <summary>
    /// 配置ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用")]
    public string AppId { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    [DisplayName("配置项")]
    [TplColumn(template: "${key}")]
    //[Badge(VisibleOn = "onlineStatus", Level = "success", Text = "已发布")]
    [Badge(VisibleOn = "!onlineStatus", Level = "warning", Text = "未发布")]
    public string Key { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [DisplayName("配置值")]
    public string Value { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    [Badge(Level = "info")]
    public string Environment { get; set; }

    /// <summary>
    /// 配置组
    /// </summary>
    [DisplayName("分组")]
    public string Group { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; }

    /// <summary>
    /// 是否已发布
    /// </summary>
    public bool OnlineStatus { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [DisplayName("类型")]
    public string Type { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本")]
    public long Version { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("发布状态")]
    //[StatusColumn(
    //    StatusMap = new Dictionary<string, string> {
    //        { "init", "准备中" },
    //        { "edit", "编辑中" },
    //        { "released", "已发布" }
    //    },
    //    StatusLabelMap = new Dictionary<string, string> {
    //        { "init", "warning" },
    //        { "edit", "info" },
    //        { "released", "success" }
    //    }
    //)]
    public string Status { get; set; }
} 