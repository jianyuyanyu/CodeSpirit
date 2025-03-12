using System.Text.Json;

namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计日志模型
/// </summary>
public class AuditLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 地理位置信息
    /// </summary>
    public GeoLocation Location { get; set; } = new GeoLocation();
    
    /// <summary>
    /// 用户代理（浏览器/设备信息）
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    public string ControllerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求方法
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// 请求参数
    /// </summary>
    public string RequestParams { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体名称
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作前数据
    /// </summary>
    public string BeforeData { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作后数据
    /// </summary>
    public string AfterData { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行时长(毫秒)
    /// </summary>
    public long ExecutionDuration { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 特性属性
    /// </summary>
    public Dictionary<string, string> AttributeProperties { get; set; } = new Dictionary<string, string>();
    
    /// <summary>
    /// 附加数据
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 设置附加数据
    /// </summary>
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

/// <summary>
/// 地理位置信息
/// </summary>
public class GeoLocation
{
    /// <summary>
    /// 国家
    /// </summary>
    public string Country { get; set; } = string.Empty;
    
    /// <summary>
    /// 国家代码
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 省/州
    /// </summary>
    public string Region { get; set; } = string.Empty;
    
    /// <summary>
    /// 城市
    /// </summary>
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }
    
    /// <summary>
    /// 纬度
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// 互联网服务提供商
    /// </summary>
    public string ISP { get; set; } = string.Empty;
} 