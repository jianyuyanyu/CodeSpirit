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
    [AmisColumn(Type = "status")]
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
    /// 请求方法
    /// </summary>
    [DisplayName("请求方法")]
    [AmisColumn(Type = "status")]
    public string RequestMethod { get; set; } = string.Empty;
    
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
    /// 执行时长(毫秒)
    /// </summary>
    [DisplayName("执行时长(毫秒)")]
    [AmisColumn(Type = "tpl", Remark = "请求执行时长")]
    public long ExecutionDuration { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    [AmisColumn(Type = "status")]
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
    [AmisColumn(Type = "status")]
    public int StatusCode { get; set; }
} 