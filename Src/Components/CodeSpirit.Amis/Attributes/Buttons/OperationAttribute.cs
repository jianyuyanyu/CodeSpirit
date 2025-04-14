[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class OperationAttribute : Attribute
{
    public string Label { get; }
    public string ActionType { get; }
    public string Api { get; }
    public string ConfirmText { get; }
    public string VisibleOn { get; }
    
    /// <summary>
    /// 按钮图标，支持 Font Awesome 图标，如: fa fa-plus
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 请求成功后，跳转至某个页面
    /// </summary>
    public string Redirect { get; set; }

    /// <summary>
    /// 是否批量操作
    /// </summary>
    public bool IsBulkOperation { get; set; }

    /// <summary>
    /// 数据映射配置
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// 仅ActionType为link时可用，如果为 true 将在新 tab 页面打开。
    /// </summary>
    public bool Blank { get; set; }
    
    /// <summary>
    /// 操作完成后反馈弹框标题，设置此属性将显示操作结果弹框
    /// </summary>
    public string FeedbackTitle { get; set; }

    /// <summary>
    /// 操作完成后反馈弹框内容，支持 Amis 渲染
    /// </summary>
    public string FeedbackBodyTpl { get; set; }

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