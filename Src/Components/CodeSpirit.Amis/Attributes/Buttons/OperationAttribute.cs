using System.ComponentModel.DataAnnotations;

/// <summary>
/// 对话框大小枚举
/// </summary>
public enum DialogSize
{
    /// <summary>
    /// 超小尺寸
    /// </summary>
    [Display(Name = "超小尺寸")]
    XS = 1,

    /// <summary>
    /// 小尺寸
    /// </summary>
    [Display(Name = "小尺寸")]
    SM = 2,

    /// <summary>
    /// 中等尺寸（默认）
    /// </summary>
    [Display(Name = "中等尺寸")]
    MD = 3,

    /// <summary>
    /// 大尺寸
    /// </summary>
    [Display(Name = "大尺寸")]
    LG = 4,

    /// <summary>
    /// 超大尺寸
    /// </summary>
    [Display(Name = "超大尺寸")]
    XL = 5,

    /// <summary>
    /// 全屏
    /// </summary>
    [Display(Name = "全屏")]
    Full = 6,

    /// <summary>
    /// 自定义尺寸
    /// </summary>
    [Display(Name = "自定义尺寸")]
    Custom = 7
}

/// <summary>
/// 操作类型枚举
/// </summary>
public enum OperationActionType
{
    /// <summary>
    /// AJAX请求
    /// </summary>
    [Display(Name = "AJAX请求")]
    Ajax = 1,

    /// <summary>
    /// 表单操作
    /// </summary>
    [Display(Name = "表单操作")]
    Form = 2,

    /// <summary>
    /// 链接跳转
    /// </summary>
    [Display(Name = "链接跳转")]
    Link = 3,

    /// <summary>
    /// 服务调用
    /// </summary>
    [Display(Name = "服务调用")]
    Service = 4,

    /// <summary>
    /// AI表单操作
    /// </summary>
    [Display(Name = "AI表单操作")]
    AiForm = 5,

    /// <summary>
    /// CRUD对话框（弹窗显示列表数据）
    /// </summary>
    [Display(Name = "CRUD对话框")]
    CrudDialog = 6
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class OperationAttribute : Attribute
{
    public string Label { get; }
    
    /// <summary>
    /// 操作类型（枚举）
    /// </summary>
    public OperationActionType ActionTypeEnum { get; }
    
    /// <summary>
    /// 操作类型（字符串，用于向后兼容）
    /// </summary>
    public string ActionType => ActionTypeEnum switch
    {
        OperationActionType.Ajax => "ajax",
        OperationActionType.Form => "form",
        OperationActionType.Link => "link",
        OperationActionType.Service => "service",
        OperationActionType.AiForm => "aiForm",
        OperationActionType.CrudDialog => "crudDialog",
        _ => "ajax"
    };
    
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
    /// 对话框大小（枚举）
    /// </summary>
    public DialogSize DialogSize { get; set; } = DialogSize.MD;

    /// <summary>
    /// 指定 dialog 大小（字符串，用于向后兼容）
    /// </summary>
    public string DialogSizeString => DialogSize switch
    {
        DialogSize.XS => "xs",
        DialogSize.SM => "sm",
        DialogSize.MD => "md",
        DialogSize.LG => "lg",
        DialogSize.XL => "xl",
        DialogSize.Full => "full",
        DialogSize.Custom => "custom",
        _ => "md"
    };

    /// <summary>
    /// 仅当 ActionType 为 form 时可用，用于表单数据初始化
    /// </summary>
    public string InitApi { get; set; }

    /// <summary>
    /// 仅当 ActionType 为 aiForm 时可用，用于获取AI任务状态的轮询API
    /// 默认使用 /api/web/TaskStatus/{taskId}
    /// </summary>
    public string StatusApi { get; set; } = "/api/common/TaskStatus";

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

    /// <summary>
    /// 自定义底部按钮配置，JSON格式的字符串数组
    /// 如果为null，则使用默认按钮；如果为空数组，则不显示底部按钮
    /// 示例：[{"type":"button","label":"确定","actionType":"submit"},{"type":"button","label":"取消","actionType":"close"}]
    /// </summary>
    public string Actions { get; set; }

    /// <summary>
    /// 按钮标签的资源键（用于多语言）
    /// 如果指定，将从资源文件中读取按钮文本
    /// </summary>
    public string LabelResourceKey { get; set; }

    /// <summary>
    /// 按钮标签的资源类型（用于多语言）
    /// 如：typeof(SharedResources) 或 typeof(IdentityDisplayResources)
    /// </summary>
    public Type LabelResourceType { get; set; }

    /// <summary>
    /// 确认文本的资源键（用于多语言）
    /// </summary>
    public string ConfirmTextResourceKey { get; set; }

    /// <summary>
    /// 确认文本的资源类型（用于多语言）
    /// </summary>
    public Type ConfirmTextResourceType { get; set; }

    /// <summary>
    /// 反馈标题的资源键（用于多语言）
    /// </summary>
    public string FeedbackTitleResourceKey { get; set; }

    /// <summary>
    /// 反馈标题的资源类型（用于多语言）
    /// </summary>
    public Type FeedbackTitleResourceType { get; set; }

    /// <summary>
    /// 使用枚举类型的构造函数（推荐）
    /// </summary>
    /// <param name="label">按钮标签</param>
    /// <param name="actionType">操作类型枚举</param>
    /// <param name="api">API地址</param>
    /// <param name="confirmText">确认文本</param>
    /// <param name="visibleOn">显示条件</param>
    /// <param name="isBulkOperation">是否批量操作</param>
    public OperationAttribute(string label, OperationActionType actionType = OperationActionType.Ajax, string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false)
    {
        Label = label;
        ActionTypeEnum = actionType;
        Api = api;
        ConfirmText = confirmText;
        VisibleOn = visibleOn;
        IsBulkOperation = isBulkOperation;
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
    public OperationAttribute(string label, string actionType = "ajax", string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false)
    {
        Label = label;
        ActionTypeEnum = actionType?.ToLower() switch
        {
            "ajax" => OperationActionType.Ajax,
            "form" => OperationActionType.Form,
            "link" => OperationActionType.Link,
            "service" => OperationActionType.Service,
            "aiform" => OperationActionType.AiForm,
            "cruddialog" or "crudDialog" => OperationActionType.CrudDialog,
            _ => OperationActionType.Ajax
        };
        Api = api;
        ConfirmText = confirmText;
        VisibleOn = visibleOn;
        IsBulkOperation = isBulkOperation;
    }
}