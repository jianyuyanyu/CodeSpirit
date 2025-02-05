using System.Collections.Generic;

/// <summary>
/// 权限节点类，用于描述权限树中的一个节点（既可以表示控制器，也可以表示动作）。
/// 新增 RequestMethod 属性，用于记录动作所支持的 HTTP 请求方法。
/// </summary>
public class PermissionNode
{
    /// <summary>
    /// 节点名称（控制器名称或动作名称）
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 节点描述（可以通过 DisplayNameAttribute 或 PermissionAttribute 指定）
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 父节点名称，如果是动作则指向所属控制器；如果为空，则表示根节点
    /// </summary>
    public string Parent { get; set; }

    /// <summary>
    /// 请求路径（仅对动作节点有效，通过 RouteAttribute 获取）
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 请求方法（例如 GET、POST、PUT、DELETE 等，仅对动作节点有效）
    /// </summary>
    public string RequestMethod { get; set; }

    /// <summary>
    /// 子节点集合
    /// </summary>
    public List<PermissionNode> Children { get; set; } = new List<PermissionNode>();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="description">节点描述</param>
    /// <param name="parent">父节点名称</param>
    /// <param name="path">请求路径</param>
    /// <param name="requestMethod">请求方法</param>
    public PermissionNode(string name, string description, string parent = "", string path = "", string requestMethod = "")
    {
        Name = name;
        Description = description;
        Parent = parent;
        Path = path;
        RequestMethod = requestMethod;
    }
}