using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志查询参数
/// </summary>
public class AuditLogQueryDto : QueryDtoBase
{
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [StringLength(50, ErrorMessage = "租户ID长度不能超过50个字符")]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    [StringLength(50, ErrorMessage = "用户ID长度不能超过50个字符")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    [StringLength(100, ErrorMessage = "用户名长度不能超过100个字符")]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
    
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    public AuditOperationTypeEnum? OperationType { get; set; }
    
    /// <summary>
    /// HTTP请求方法
    /// </summary>
    [DisplayName("请求方法")]
    public HttpRequestMethod? RequestMethod { get; set; }
    
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// 操作名称（模糊搜索）
    /// </summary>
    [DisplayName("操作名称")]
    [StringLength(200, ErrorMessage = "操作名称长度不能超过200个字符")]
    public string OperationName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称（模糊搜索）
    /// </summary>
    [DisplayName("控制器")]
    [StringLength(100, ErrorMessage = "控制器名称长度不能超过100个字符")]
    public string ApiController { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体名称
    /// </summary>
    [DisplayName("实体名称")]
    [StringLength(100, ErrorMessage = "实体名称长度不能超过100个字符")]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作交互类型
    /// </summary>
    [DisplayName("交互类型")]
    public OperationInteractionType? OperationActionType { get; set; }
    
    /// <summary>
    /// 是否批量操作
    /// </summary>
    [DisplayName("批量操作")]
    public bool? IsBulkOperation { get; set; }
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("HTTP状态码")]
    public CommonHttpStatusCode? StatusCode { get; set; }
    
    /// <summary>
    /// 自定义HTTP状态码（当StatusCode为null时使用）
    /// </summary>
    [DisplayName("自定义状态码")]
    [Range(100, 599, ErrorMessage = "HTTP状态码必须在100-599之间")]
    public int? CustomStatusCode { get; set; }
    
    /// <summary>
    /// 最小执行时长(毫秒)
    /// </summary>
    [DisplayName("最小执行时长")]
    [Range(0, long.MaxValue, ErrorMessage = "执行时长必须大于等于0")]
    public long? MinExecutionDuration { get; set; }
    
    /// <summary>
    /// 最大执行时长(毫秒)
    /// </summary>
    [DisplayName("最大执行时长")]
    [Range(0, long.MaxValue, ErrorMessage = "执行时长必须大于等于0")]
    public long? MaxExecutionDuration { get; set; }
} 