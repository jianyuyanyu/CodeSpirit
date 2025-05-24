using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计日志模型
/// </summary>
public class AuditLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [DisplayName("日志ID")]
    [Key]
    [StringLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 地理位置信息
    /// </summary>
    [DisplayName("地理位置")]
    public GeoLocation Location { get; set; } = new GeoLocation();
    
    /// <summary>
    /// 用户代理（浏览器/设备信息）
    /// </summary>
    [DisplayName("用户代理")]
    [StringLength(500)]
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    [Required]
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 服务名称
    /// </summary>
    [DisplayName("服务名称")]
    [StringLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    [DisplayName("控制器名称")]
    [StringLength(100)]
    public string ControllerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    [DisplayName("操作名称")]
    [StringLength(100)]
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作显示名称
    /// </summary>
    [DisplayName("操作显示名称")]
    [StringLength(200)]
    public string OperationName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    [StringLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作描述
    /// </summary>
    [DisplayName("操作描述")]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求路径
    /// </summary>
    [DisplayName("请求路径")]
    [StringLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求方法
    /// </summary>
    [DisplayName("请求方法")]
    [StringLength(10)]
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
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [DisplayName("业务实体ID")]
    [StringLength(100)]
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
    [Range(0, long.MaxValue)]
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
    /// 特性属性
    /// </summary>
    [DisplayName("特性属性")]
    public Dictionary<string, string> AttributeProperties { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// 附加数据
    /// </summary>
    [DisplayName("附加数据")]
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("HTTP状态码")]
    [Range(100, 599, ErrorMessage = "HTTP状态码必须在100-599之间")]
    public int StatusCode { get; set; }

    /// <summary>
    /// 设置附加数据
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SetAdditionalData(string key, object value)
    {
        if (AdditionalData.ContainsKey(key))
        {
            AdditionalData[key] = value;
        }
        else
        {
            AdditionalData.Add(key, value);
        }
    }
    
    /// <summary>
    /// 将对象转换为JSON
    /// </summary>
    /// <param name="obj">要转换的对象</param>
    /// <returns>JSON字符串</returns>
    public string ToJson(object obj)
    {
        if (obj == null) return null;
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
