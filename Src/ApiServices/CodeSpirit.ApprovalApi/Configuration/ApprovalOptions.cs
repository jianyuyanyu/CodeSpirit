namespace CodeSpirit.ApprovalApi.Configuration;

/// <summary>
/// 审批配置选项
/// </summary>
public class ApprovalOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 默认超时时间（小时）
    /// </summary>
    public int DefaultTimeoutHours { get; set; } = 72;

    /// <summary>
    /// 是否启用自动提醒
    /// </summary>
    public bool EnableAutoReminder { get; set; } = true;

    /// <summary>
    /// 提醒间隔（小时）
    /// </summary>
    public int ReminderIntervalHours { get; set; } = 24;

    /// <summary>
    /// 是否启用审批日志
    /// </summary>
    public bool EnableApprovalLog { get; set; } = true;

    /// <summary>
    /// 是否启用智能审批
    /// </summary>
    public bool EnableIntelligentApproval { get; set; } = true;

    /// <summary>
    /// 缓存配置
    /// </summary>
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// 通知配置
    /// </summary>
    public NotificationOptions Notification { get; set; } = new();

    /// <summary>
    /// LLM配置
    /// </summary>
    public LLMOptions LLM { get; set; } = new();
}

/// <summary>
/// 缓存配置选项
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// 工作流定义缓存时间（分钟）
    /// </summary>
    public int WorkflowDefinitionCacheMinutes { get; set; } = 30;

    /// <summary>
    /// 风险评估缓存时间（分钟）
    /// </summary>
    public int RiskAssessmentCacheMinutes { get; set; } = 30;

    /// <summary>
    /// 智能建议缓存时间（分钟）
    /// </summary>
    public int IntelligentSuggestionCacheMinutes { get; set; } = 15;
}

/// <summary>
/// 通知配置选项
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool EnableEmailNotification { get; set; } = true;

    /// <summary>
    /// 是否启用短信通知
    /// </summary>
    public bool EnableSmsNotification { get; set; } = false;

    /// <summary>
    /// 是否启用站内信通知
    /// </summary>
    public bool EnableInternalNotification { get; set; } = true;

    /// <summary>
    /// 邮件模板配置
    /// </summary>
    public Dictionary<string, string> EmailTemplates { get; set; } = new();

    /// <summary>
    /// 短信模板配置
    /// </summary>
    public Dictionary<string, string> SmsTemplates { get; set; } = new();
}

/// <summary>
/// LLM配置选项
/// </summary>
public class LLMOptions
{
    /// <summary>
    /// 默认温度参数
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.1;

    /// <summary>
    /// 默认最大令牌数
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 1000;

    /// <summary>
    /// 风险评估最大令牌数
    /// </summary>
    public int RiskAssessmentMaxTokens { get; set; } = 1000;

    /// <summary>
    /// 审批建议最大令牌数
    /// </summary>
    public int ApprovalSuggestionMaxTokens { get; set; } = 1500;

    /// <summary>
    /// 异常检测最大令牌数
    /// </summary>
    public int AnomalyDetectionMaxTokens { get; set; } = 800;

    /// <summary>
    /// 合规检查最大令牌数
    /// </summary>
    public int ComplianceCheckMaxTokens { get; set; } = 1000;
}
