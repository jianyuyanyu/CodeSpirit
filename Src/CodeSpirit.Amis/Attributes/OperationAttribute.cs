[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class OperationAttribute : Attribute
{
    public string Label { get; }
    public string ActionType { get; }
    public string Api { get; }
    public string ConfirmText { get; }

    public string VisibleOn { get; }

    /// <summary>
    /// 请求成功后，跳转至某个页面
    /// </summary>
    public string Redirect { get; set; }

    /// <summary>
    /// 是否批量操作
    /// </summary>
    public bool IsBulkOperation {  get; set; }

    public OperationAttribute(string label, string actionType = "ajax", string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false)
    {
        Label = label;
        ActionType = actionType;
        Api = api;
        ConfirmText = confirmText;
        VisibleOn = visibleOn;
        IsBulkOperation = isBulkOperation;
    }
}