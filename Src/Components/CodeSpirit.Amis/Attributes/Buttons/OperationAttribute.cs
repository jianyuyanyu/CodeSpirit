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

    /// <summary>
    /// 指定 dialog 大小，支持: xs、sm、md、lg、xl、full、custom
    /// </summary>
    public string FeedBackSize { get; set; }

    /// <summary>
    /// 仅当 ActionType 为 form 时可用，用于表单数据初始化
    /// </summary>
    public string InitApi { get; set; }

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，用于获取AI任务状态的轮询API
    /// </summary>
    public string StatusApi { get; set; }

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，轮询间隔（毫秒），默认2000ms
    /// </summary>
    public int PollingInterval { get; set; } = 2000;

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，最大轮询时间（毫秒），默认300000ms（5分钟）
    /// </summary>
    public int MaxPollingTime { get; set; } = 300000;

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，AI任务完成后的跳转页面
    /// </summary>
    public string SuccessRedirect { get; set; }

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，步骤面板标题
    /// </summary>
    public string StepsTitle { get; set; } = "AI处理进度";

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，表单面板标题
    /// </summary>
    public string FormTitle { get; set; } = "参数配置";

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，日志面板标题
    /// </summary>
    public string LogTitle { get; set; } = "处理日志";

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，结果面板标题
    /// </summary>
    public string ResultTitle { get; set; } = "处理结果";

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