namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计选项配置
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// 是否启用审计
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 是否记录请求参数
    /// </summary>
    public bool LogRequestParams { get; set; } = true;
    
    /// <summary>
    /// 是否记录响应数据
    /// </summary>
    public bool LogResponseData { get; set; } = false;
    
    /// <summary>
    /// 是否记录未经授权的请求
    /// </summary>
    public bool LogUnauthorizedRequests { get; set; } = true;
    
    /// <summary>
    /// 是否记录匿名请求
    /// </summary>
    public bool LogAnonymousRequests { get; set; } = false;
    
    /// <summary>
    /// 是否记录健康检查请求
    /// </summary>
    public bool LogHealthChecks { get; set; } = false;
    
    /// <summary>
    /// 是否启用操作类型自动推断
    /// </summary>
    public bool EnableOperationTypeInference { get; set; } = true;
    
    /// <summary>
    /// 是否启用地理位置查询
    /// </summary>
    public bool EnableGeoLocation { get; set; } = false;
    
    /// <summary>
    /// 地理位置API URL格式，使用{0}作为IP地址占位符
    /// </summary>
    public string GeoLocationApiUrl { get; set; } = "http://ip-api.com/json/{0}?fields=status,message,country,countryCode,region,regionName,city,lat,lon,isp,org";
    
    /// <summary>
    /// 地理位置API类型（支持：ipapi, ipinfo, ipstack）
    /// </summary>
    public string GeoLocationApiType { get; set; } = "ipapi";
    
    /// <summary>
    /// 脱敏配置
    /// </summary>
    public SensitiveDataOptions SensitiveData { get; set; } = new SensitiveDataOptions();
    
    /// <summary>
    /// 操作类型推断配置
    /// </summary>
    public OperationInferenceOptions OperationInference { get; set; } = new OperationInferenceOptions();
    
    /// <summary>
    /// 要排除的路径前缀
    /// </summary>
    public List<string> ExcludedPathPrefixes { get; set; } = new List<string>
    {
        "/swagger",
        "/healthz",
        "/favicon.ico"
    };
    
    /// <summary>
    /// RabbitMQ配置
    /// </summary>
    public RabbitMQOptions RabbitMQ { get; set; } = new RabbitMQOptions();
    
    /// <summary>
    /// Elasticsearch配置
    /// </summary>
    public ElasticsearchOptions Elasticsearch { get; set; } = new ElasticsearchOptions();
}

/// <summary>
/// 敏感数据配置选项
/// </summary>
public class SensitiveDataOptions
{
    /// <summary>
    /// 是否启用敏感数据脱敏
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 敏感字段名称模式（区分大小写）
    /// </summary>
    public List<string> SensitiveFieldPatterns { get; set; } = new List<string>
    {
        "password",
        "pwd",
        "secret",
        "token",
        "apiKey",
        "key",
        "auth",
        "credential",
        "creditCard",
        "cardNumber",
        "cvv",
        "ssn",
        "idCard"
    };
    
    /// <summary>
    /// 默认敏感数据掩码字符
    /// </summary>
    public string MaskCharacter { get; set; } = "*";
    
    /// <summary>
    /// 前面保留的字符数
    /// </summary>
    public int KeepFirstChars { get; set; } = 0;
    
    /// <summary>
    /// 末尾保留的字符数
    /// </summary>
    public int KeepLastChars { get; set; } = 0;
    
    /// <summary>
    /// 完全排除的敏感字段（不记录）
    /// </summary>
    public List<string> ExcludedFields { get; set; } = new List<string>
    {
        "password",
        "newPassword",
        "confirmPassword",
        "currentPassword"
    };
}

/// <summary>
/// RabbitMQ配置选项
/// </summary>
public class RabbitMQOptions
{
    /// <summary>
    /// 主机
    /// </summary>
    public string HostName { get; set; } = "localhost";
    
    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; } = 5672;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = "guest";
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = "guest";
    
    /// <summary>
    /// 虚拟主机
    /// </summary>
    public string VirtualHost { get; set; } = "/";
    
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; set; } = "audit.exchange";
    
    /// <summary>
    /// 队列名称
    /// </summary>
    public string QueueName { get; set; } = "audit.queue";
    
    /// <summary>
    /// 路由键
    /// </summary>
    public string RoutingKey { get; set; } = "audit.log";
}

/// <summary>
/// Elasticsearch配置选项
/// </summary>
public class ElasticsearchOptions
{
    /// <summary>
    /// Elasticsearch URLs
    /// </summary>
    public List<string> Urls { get; set; } = new List<string> { "http://localhost:9200" };
    
    /// <summary>
    /// 索引名称
    /// </summary>
    public string IndexName { get; set; } = "auditlogs";
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 索引分片数
    /// </summary>
    public int NumberOfShards { get; set; } = 1;
    
    /// <summary>
    /// 索引副本数
    /// </summary>
    public int NumberOfReplicas { get; set; } = 1;
}

/// <summary>
/// 操作类型推断配置选项
/// </summary>
public class OperationInferenceOptions
{
    /// <summary>
    /// 查询操作的关键词
    /// </summary>
    public List<string> QueryKeywords { get; set; } = new List<string>
    {
        "get", "query", "find", "search", "list", "select", "fetch", "read", "retrieve"
    };
    
    /// <summary>
    /// 创建操作的关键词
    /// </summary>
    public List<string> CreateKeywords { get; set; } = new List<string>
    {
        "create", "add", "insert", "new", "register", "sign", "post"
    };
    
    /// <summary>
    /// 更新操作的关键词
    /// </summary>
    public List<string> UpdateKeywords { get; set; } = new List<string>
    {
        "update", "edit", "modify", "change", "set", "put", "patch"
    };
    
    /// <summary>
    /// 删除操作的关键词
    /// </summary>
    public List<string> DeleteKeywords { get; set; } = new List<string>
    {
        "delete", "remove", "clear", "drop", "erase", "purge"
    };
    
    /// <summary>
    /// HTTP方法到操作类型的映射
    /// </summary>
    public Dictionary<string, string> HttpMethodMappings { get; set; } = new Dictionary<string, string>
    {
        { "GET", "Query" },
        { "POST", "Create" },
        { "PUT", "Update" },
        { "PATCH", "Update" },
        { "DELETE", "Delete" },
        { "HEAD", "Query" },
        { "OPTIONS", "Query" }
    };
    
    /// <summary>
    /// 常见的ID参数名称
    /// </summary>
    public List<string> CommonIdParameterNames { get; set; } = new List<string>
    {
        "id", "entityId", "{entityName}Id", "key", "code"
    };
} 