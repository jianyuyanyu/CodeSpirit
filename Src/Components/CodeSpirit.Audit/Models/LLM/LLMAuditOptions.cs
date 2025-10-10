namespace CodeSpirit.Audit.Models.LLM;

/// <summary>
/// LLM审计配置选项
/// </summary>
public class LLMAuditOptions
{
    /// <summary>
    /// 是否启用LLM审计
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 是否记录提示词
    /// </summary>
    public bool LogPrompts { get; set; } = true;
    
    /// <summary>
    /// 是否记录LLM响应
    /// </summary>
    public bool LogResponses { get; set; } = true;
    
    /// <summary>
    /// 是否记录处理后的数据
    /// </summary>
    public bool LogProcessedData { get; set; } = false;
    
    /// <summary>
    /// 提示词最大长度（超过则截断）
    /// </summary>
    public int MaxPromptLength { get; set; } = 10000;
    
    /// <summary>
    /// 响应最大长度（超过则截断）
    /// </summary>
    public int MaxResponseLength { get; set; } = 50000;
    
    /// <summary>
    /// RabbitMQ配置
    /// </summary>
    public LLMRabbitMQOptions RabbitMQ { get; set; } = new LLMRabbitMQOptions();
    
    /// <summary>
    /// Elasticsearch配置
    /// </summary>
    public LLMElasticsearchOptions Elasticsearch { get; set; } = new LLMElasticsearchOptions();
    
    /// <summary>
    /// GreptimeDB配置
    /// </summary>
    public LLMGreptimeDbOptions GreptimeDB { get; set; } = new LLMGreptimeDbOptions();
    
    /// <summary>
    /// 敏感数据配置
    /// </summary>
    public LLMSensitiveDataOptions SensitiveData { get; set; } = new LLMSensitiveDataOptions();
    
    /// <summary>
    /// 成本计算配置
    /// </summary>
    public CostCalculationOptions CostCalculation { get; set; } = new CostCalculationOptions();
    
    /// <summary>
    /// 业务场景配置（用于自动识别业务场景）
    /// </summary>
    public Dictionary<string, string> ScenarioMapping { get; set; } = new Dictionary<string, string>
    {
        { "QuestionGeneration", "题目生成" },
        { "QuestionAudit", "题目审核" },
        { "QuestionCorrection", "题目校正" },
        { "ContentGeneration", "内容生成" }
    };
}

/// <summary>
/// LLM RabbitMQ配置选项
/// </summary>
public class LLMRabbitMQOptions
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; set; } = "llm.audit.exchange";
    
    /// <summary>
    /// 队列名称
    /// </summary>
    public string QueueName { get; set; } = "llm.audit.queue";
    
    /// <summary>
    /// 路由键
    /// </summary>
    public string RoutingKey { get; set; } = "llm.audit.log";
}

/// <summary>
/// LLM Elasticsearch配置选项
/// </summary>
public class LLMElasticsearchOptions
{
    /// <summary>
    /// 索引名称
    /// </summary>
    public string IndexName { get; set; } = "llm_audit_logs";
    
    /// <summary>
    /// 索引前缀
    /// </summary>
    public string IndexPrefix { get; set; } = "codespirit";
    
    /// <summary>
    /// 索引分片数
    /// </summary>
    public int NumberOfShards { get; set; } = 3;
    
    /// <summary>
    /// 索引副本数
    /// </summary>
    public int NumberOfReplicas { get; set; } = 1;
}

/// <summary>
/// LLM GreptimeDB配置选项
/// </summary>
public class LLMGreptimeDbOptions
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = "llm_audit_logs";
    
    /// <summary>
    /// 表前缀
    /// </summary>
    public string TablePrefix { get; set; } = "codespirit";
    
    /// <summary>
    /// 批量插入的批次大小
    /// </summary>
    public int BatchSize { get; set; } = 500;
}

/// <summary>
/// LLM敏感数据配置选项
/// </summary>
public class LLMSensitiveDataOptions
{
    /// <summary>
    /// 是否启用敏感数据脱敏
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 敏感字段模式列表
    /// </summary>
    public List<string> SensitiveFieldPatterns { get; set; } = new List<string>
    {
        "password", "pwd", "secret", "token", "apiKey", "key",
        "personalInfo", "idCard", "phone", "email", "address"
    };
    
    /// <summary>
    /// 掩码字符
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
}

/// <summary>
/// 成本计算配置选项
/// </summary>
public class CostCalculationOptions
{
    /// <summary>
    /// 是否启用成本计算
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// 模型价格配置（模型名 → 价格信息）
    /// </summary>
    public Dictionary<string, ModelPricing> ModelPricing { get; set; } = new Dictionary<string, ModelPricing>
    {
        { "gpt-4", new ModelPricing { InputPer1K = 0.03m, OutputPer1K = 0.06m } },
        { "gpt-3.5-turbo", new ModelPricing { InputPer1K = 0.0015m, OutputPer1K = 0.002m } },
        { "qwen-plus", new ModelPricing { InputPer1K = 0.004m, OutputPer1K = 0.012m } },
        { "qwen-turbo", new ModelPricing { InputPer1K = 0.002m, OutputPer1K = 0.006m } }
    };
}

/// <summary>
/// 模型定价
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// 输入每1000 tokens价格（USD）
    /// </summary>
    public decimal InputPer1K { get; set; }
    
    /// <summary>
    /// 输出每1000 tokens价格（USD）
    /// </summary>
    public decimal OutputPer1K { get; set; }
}

