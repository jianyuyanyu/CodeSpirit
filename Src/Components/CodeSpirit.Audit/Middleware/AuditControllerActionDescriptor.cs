namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 控制器操作描述符
/// </summary>
public class AuditControllerActionDescriptor
{
    /// <summary>
    /// 控制器名称
    /// </summary>
    public string ControllerName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string ActionName { get; set; }

    /// <summary>
    /// 控制器类型信息
    /// </summary>
    public TypeInfo ControllerTypeInfo { get; set; }

    /// <summary>
    /// 方法信息
    /// </summary>
    public MethodInfo MethodInfo { get; set; }
}
