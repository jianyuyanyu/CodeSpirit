[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class HeaderOperationAttribute : OperationAttribute
{
    /// <summary>
    /// 使用枚举类型的构造函数（推荐）
    /// </summary>
    /// <param name="label">按钮标签</param>
    /// <param name="actionType">操作类型枚举</param>
    /// <param name="api">API地址</param>
    /// <param name="confirmText">确认文本</param>
    /// <param name="visibleOn">显示条件</param>
    /// <param name="isBulkOperation">是否批量操作</param>
    public HeaderOperationAttribute(string label, OperationActionType actionType = OperationActionType.Ajax, string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false) : base(label, actionType, api, confirmText, visibleOn, isBulkOperation)
    {
    }

    /// <summary>
    /// 使用字符串类型的构造函数（向后兼容）
    /// </summary>
    /// <param name="label">按钮标签</param>
    /// <param name="actionType">操作类型字符串</param>
    /// <param name="api">API地址</param>
    /// <param name="confirmText">确认文本</param>
    /// <param name="visibleOn">显示条件</param>
    /// <param name="isBulkOperation">是否批量操作</param>
    public HeaderOperationAttribute(string label, string actionType = "ajax", string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false) : base(label, actionType, api, confirmText, visibleOn, isBulkOperation)
    {
    }
}