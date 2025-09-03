namespace CodeSpirit.AiFormFill.Models;

/// <summary>
/// AI表单填充端点信息
/// </summary>
public class AiFormFillEndpointInfo
{
    /// <summary>
    /// DTO类型
    /// </summary>
    public Type DtoType { get; set; } = null!;

    /// <summary>
    /// 路由路径
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 触发字段名称
    /// </summary>
    public string TriggerField { get; set; } = string.Empty;

    /// <summary>
    /// AI填充特性
    /// </summary>
    public AiFormFillAttribute Attribute { get; set; } = null!;
}
