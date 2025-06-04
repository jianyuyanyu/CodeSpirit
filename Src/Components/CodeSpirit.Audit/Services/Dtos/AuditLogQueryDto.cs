using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志查询参数
/// </summary>
public class AuditLogQueryDto : QueryDtoBase
{
    /// <summary>
    /// 页码
    /// </summary>
    [DisplayName("页码")]
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int PageIndex 
    { 
        get => Page; 
        set => Page = value; 
    }
    
    /// <summary>
    /// 页大小
    /// </summary>
    [DisplayName("页大小")]
    [Range(1, 100, ErrorMessage = "页大小必须在1-100之间")]
    public int PageSize 
    { 
        get => PerPage; 
        set => PerPage = value; 
    }
    
    /// <summary>
    /// 排序字段
    /// </summary>
    [DisplayName("排序字段")]
    [StringLength(50, ErrorMessage = "排序字段长度不能超过50个字符")]
    public string SortField 
    { 
        get => OrderBy ?? "OperationTime"; 
        set => OrderBy = value; 
    }
    
    /// <summary>
    /// 排序方向
    /// </summary>
    [DisplayName("排序方向")]
    [RegularExpression("^(asc|desc)$", ErrorMessage = "排序方向只能是asc或desc")]
    public string SortDirection 
    { 
        get => OrderDir; 
        set => OrderDir = value; 
    }
    
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
    /// 服务名称
    /// </summary>
    [DisplayName("服务名称")]
    [StringLength(100, ErrorMessage = "服务名称长度不能超过100个字符")]
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    [DisplayName("控制器名称")]
    [StringLength(100, ErrorMessage = "控制器名称长度不能超过100个字符")]
    public string ControllerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    [DisplayName("操作名称")]
    [StringLength(100, ErrorMessage = "操作名称长度不能超过100个字符")]
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    [StringLength(50, ErrorMessage = "操作类型长度不能超过50个字符")]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体名称
    /// </summary>
    [DisplayName("实体名称")]
    [StringLength(100, ErrorMessage = "实体名称长度不能超过100个字符")]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体ID
    /// </summary>
    [DisplayName("实体ID")]
    [StringLength(100, ErrorMessage = "实体ID长度不能超过100个字符")]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool? IsSuccess { get; set; }
} 