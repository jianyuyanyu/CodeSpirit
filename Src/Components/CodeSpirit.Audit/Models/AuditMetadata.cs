using Newtonsoft.Json;

namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计元数据模型
/// </summary>
/// <remarks>
/// 用于在分布式环境中通过响应头传递审计元数据
/// </remarks>
public class AuditMetadata
{
    /// <summary>
    /// 操作名称
    /// </summary>
    [JsonProperty("operationName")]
    public string? OperationName { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [JsonProperty("operationType")]
    public string? OperationType { get; set; }
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    [JsonProperty("controller")]
    public string? Controller { get; set; }
    
    /// <summary>
    /// 操作方法名称
    /// </summary>
    [JsonProperty("action")]
    public string? Action { get; set; }
    
    /// <summary>
    /// 描述信息
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否记录请求参数
    /// </summary>
    [JsonProperty("logRequestParams")]
    public bool LogRequestParams { get; set; }
    
    /// <summary>
    /// 是否记录响应数据
    /// </summary>
    [JsonProperty("logResponseData")]
    public bool LogResponseData { get; set; }
    
    /// <summary>
    /// 实体名称
    /// </summary>
    [JsonProperty("entityName")]
    public string? EntityName { get; set; }
    
    /// <summary>
    /// 实体ID参数名
    /// </summary>
    [JsonProperty("entityIdParamName")]
    public string? EntityIdParamName { get; set; }
    
    /// <summary>
    /// 操作标签
    /// </summary>
    [JsonProperty("operationLabel")]
    public string? OperationLabel { get; set; }
    
    /// <summary>
    /// 操作动作类型
    /// </summary>
    [JsonProperty("operationActionType")]
    public string? OperationActionType { get; set; }
    
    /// <summary>
    /// 操作API
    /// </summary>
    [JsonProperty("operationApi")]
    public string? OperationApi { get; set; }
    
    /// <summary>
    /// 确认文本
    /// </summary>
    [JsonProperty("operationConfirmText")]
    public string? OperationConfirmText { get; set; }
    
    /// <summary>
    /// 操作图标
    /// </summary>
    [JsonProperty("operationIcon")]
    public string? OperationIcon { get; set; }
    
    /// <summary>
    /// 是否批量操作
    /// </summary>
    [JsonProperty("isBulkOperation")]
    public bool IsBulkOperation { get; set; }
}
