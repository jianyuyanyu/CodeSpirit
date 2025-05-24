using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志详情DTO
/// </summary>
public class AuditLogDetailDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [DisplayName("日志ID")]
    public string Id { get; set; } = string.Empty;
    
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
    /// 地理位置信息
    /// </summary>
    [DisplayName("地理位置")]
    public GeoLoc Location { get; set; } = new GeoLoc();
    
    /// <summary>
    /// 用户代理
    /// </summary>
    [DisplayName("用户代理")]
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    public DateTime OperationTime { get; set; }
    
    /// <summary>
    /// 服务名称
    /// </summary>
    [DisplayName("服务名称")]
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    [DisplayName("控制器名称")]
    public string ControllerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    [DisplayName("操作名称")]
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作显示名称
    /// </summary>
    [DisplayName("操作显示名称")]
    public string OperationName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作描述
    /// </summary>
    [DisplayName("操作描述")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求路径
    /// </summary>
    [DisplayName("请求路径")]
    public string RequestPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求方法
    /// </summary>
    [DisplayName("请求方法")]
    public string RequestMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求参数
    /// </summary>
    [DisplayName("请求参数")]
    public string RequestParams { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体名称
    /// </summary>
    [DisplayName("业务实体名称")]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [DisplayName("业务实体ID")]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作前数据
    /// </summary>
    [DisplayName("操作前数据")]
    public string BeforeData { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作后数据
    /// </summary>
    [DisplayName("操作后数据")]
    public string AfterData { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行时长(毫秒)
    /// </summary>
    [DisplayName("执行时长(毫秒)")]
    public long ExecutionDuration { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("HTTP状态码")]
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 特性属性
    /// </summary>
    [DisplayName("特性属性")]
    public Dictionary<string, string> AttributeProperties { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// 附加数据
    /// </summary>
    [DisplayName("附加数据")]
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
} 