using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.Columns;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志数据传输对象
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [DisplayName("日志ID")]
    [AmisColumn(Hidden = true)]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [AmisColumn(Fixed = "left")]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm:ss", FromNow = true)]
    public DateTime OperationTime { get; set; }
    
    
    /// <summary>
    /// 操作显示名称
    /// </summary>
    [DisplayName("操作显示名称")]
    public string OperationName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    [AmisStatusColumn(StatusMapping.AuditOperationType)]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作描述
    /// </summary>
    [DisplayName("操作描述")]
    [AmisColumn(Hidden = true)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求路径
    /// </summary>
    [DisplayName("请求路径")]
    public string RequestPath { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP请求方法
    /// </summary>
    [DisplayName("请求方法")]
    [AmisStatusColumn(StatusMapping.HttpMethod, Remark = "GET/POST/PUT/DELETE等")]
    public string RequestMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行时长(毫秒)
    /// </summary>
    [DisplayName("执行时长(毫秒)")]
    [AmisColumn(
            BackgroundScaleMin = 0,
            BackgroundScaleMax = 200,
            BackgroundScaleColors = new[] { "#FFEF9C", "#FF7127" })]
    public long ExecutionDuration { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    [AmisStatusColumn(StatusMapping.Boolean)]
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    [AmisColumn(Hidden = true)]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("HTTP状态码")]
    [AmisStatusColumn(StatusMapping.HttpStatusCode)]
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 用户代理（浏览器/设备信息）
    /// </summary>
    [DisplayName("用户代理")]
    [AmisColumn(Hidden = true, Remark = "浏览器和设备信息")]
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// 地理位置信息
    /// </summary>
    [DisplayName("地理位置")]
    [AmisColumn(Hidden = true, Remark = "用户操作的地理位置")]
    public string LocationInfo { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求参数
    /// </summary>
    [DisplayName("请求参数")]
    [AmisColumn(Hidden = true, Type = "json", Remark = "HTTP请求参数")]
    public string RequestParams { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作前数据
    /// </summary>
    [DisplayName("操作前数据")]
    [AmisColumn(Hidden = true, Type = "json", Remark = "操作前的数据状态")]
    public string BeforeData { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作后数据
    /// </summary>
    [DisplayName("操作后数据")]
    [AmisColumn(Hidden = true, Type = "json", Remark = "操作后的数据状态")]
    public string AfterData { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体名称
    /// </summary>
    [DisplayName("实体名称")]
    [AmisColumn(Remark = "操作的业务实体")]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    [DisplayName("控制器")]
    [AmisColumn(Remark = "API控制器名称")]
    public string ApiController { get; set; } = string.Empty;
    
    /// <summary>
    /// 方法名称
    /// </summary>
    [DisplayName("方法")]
    [AmisColumn(Remark = "API方法名称")]
    public string ApiAction { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作按钮标签
    /// </summary>
    [DisplayName("操作按钮")]
    [AmisColumn(Remark = "用户点击的按钮标签")]
    public string OperationLabel { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作交互类型
    /// </summary>
    [DisplayName("交互类型")]
    [AmisStatusColumn(StatusMapping.CommonStatus, Remark = "ajax/form/link等")]
    public string OperationActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作图标
    /// </summary>
    [DisplayName("操作图标")]
    [AmisColumn(Hidden = true, Remark = "按钮图标")]
    public string OperationIcon { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否批量操作
    /// </summary>
    [DisplayName("批量操作")]
    [AmisStatusColumn(StatusMapping.YesNo, Remark = "是否为批量操作")]
    public bool IsBulkOperation { get; set; }
    
    /// <summary>
    /// 记录请求参数配置
    /// </summary>
    [DisplayName("记录请求参数")]
    [AmisStatusColumn(StatusMapping.YesNo, Hidden = true)]
    public bool LogRequestParams { get; set; }
    
    /// <summary>
    /// 记录响应数据配置
    /// </summary>
    [DisplayName("记录响应数据")]
    [AmisStatusColumn(StatusMapping.YesNo, Hidden = true)]
    public bool LogResponseData { get; set; }
    
    /// <summary>
    /// 确认文本
    /// </summary>
    [DisplayName("确认文本")]
    [AmisColumn(Hidden = true, Remark = "操作确认提示文本")]
    public string OperationConfirmText { get; set; } = string.Empty;
    
    /// <summary>
    /// API地址
    /// </summary>
    [DisplayName("API地址")]
    [AmisColumn(Hidden = true, Remark = "实际调用的API地址")]
    public string OperationApi { get; set; } = string.Empty;
} 