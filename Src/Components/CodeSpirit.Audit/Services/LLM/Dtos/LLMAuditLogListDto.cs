using System.ComponentModel;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Audit.Models.LLM;
using Newtonsoft.Json;

namespace CodeSpirit.Audit.Services.LLM.Dtos;

/// <summary>
/// LLM审计日志列表DTO（用于列表展示，优化显示）
/// </summary>
public class LLMAuditLogListDto
{
    /// <summary>
    /// 审计ID
    /// </summary>
    [DisplayName("审计ID")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    public string? UserName { get; set; }
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    public DateTime OperationTime { get; set; }
    
    /// <summary>
    /// LLM提供商
    /// </summary>
    [DisplayName("LLM提供商")]
    public string? LLMProvider { get; set; }
    
    /// <summary>
    /// 模型名称
    /// </summary>
    [DisplayName("模型名称")]
    public string? ModelName { get; set; }
    
    /// <summary>
    /// 交互类型
    /// </summary>
    [DisplayName("交互类型")]
    public string? InteractionType { get; set; }
    
    /// <summary>
    /// 业务场景
    /// </summary>
    [DisplayName("业务场景")]
    public string? BusinessScenario { get; set; }
    
    /// <summary>
    /// 用户提示词（截断显示）
    /// </summary>
    [DisplayName("用户提示词")]
    [LongTextColumn()]
    public string? UserPromptSummary { get; set; }
    
    /// <summary>
    /// LLM响应（截断显示）
    /// </summary>
    [DisplayName("LLM响应")]
    [LongTextColumn()]
    [JsonProperty("llmResponseSummary")]
    public string? LLMResponseSummary { get; set; }
    
    /// <summary>
    /// Token使用情况（格式化显示）
    /// </summary>
    [DisplayName("Token使用")]
    public string? TokenUsageSummary { get; set; }
    
    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    [DisplayName("处理耗时(ms)")]
    public long ProcessingTimeMs { get; set; }
    
    /// <summary>
    /// 成本（美元）
    /// </summary>
    [DisplayName("成本(USD)")]
    public string? CostUsdDisplay { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public string? IsSuccessDisplay { get; set; }
    
    /// <summary>
    /// 错误信息（截断显示）
    /// </summary>
    [DisplayName("错误信息")]
    [LongTextColumn()]
    public string? ErrorMessageSummary { get; set; }
    
    /// <summary>
    /// 是否进行了JSON修复
    /// </summary>
    [DisplayName("JSON修复")]
    public bool WasJsonRepaired { get; set; }
    
    /// <summary>
    /// 批次ID
    /// </summary>
    [DisplayName("批次ID")]
    public string? BatchId { get; set; }
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [DisplayName("业务实体ID")]
    public string? BusinessEntityId { get; set; }
    
    /// <summary>
    /// 业务实体类型
    /// </summary>
    [DisplayName("业务实体类型")]
    public string? BusinessEntityType { get; set; }
    
    /// <summary>
    /// 从LLMAuditLog创建列表DTO
    /// </summary>
    /// <param name="log">审计日志</param>
    /// <param name="maxTextLength">文本截断的最大长度</param>
    /// <returns>列表DTO</returns>
    public static LLMAuditLogListDto FromAuditLog(LLMAuditLog log, int maxTextLength = 500)
    {
        // 调试：确保字段不为null
        var userPrompt = log.UserPrompt ?? "";
        var llmResponse = log.LLMResponse ?? "";
        var systemPrompt = log.SystemPrompt ?? "";
        
        return new LLMAuditLogListDto
        {
            Id = log.Id,
            TenantId = log.TenantId,
            UserId = log.UserId,
            UserName = log.UserName,
            OperationTime = log.OperationTime,
            LLMProvider = log.LLMProvider,
            ModelName = log.ModelName,
            InteractionType = log.InteractionType,
            BusinessScenario = log.BusinessScenario,
            UserPromptSummary = TruncateText(userPrompt, maxTextLength),
            LLMResponseSummary = TruncateText(llmResponse, maxTextLength),
            TokenUsageSummary = log.TokenUsage != null 
                ? $"输入:{log.TokenUsage.InputTokens} / 输出:{log.TokenUsage.OutputTokens} / 总计:{log.TokenUsage.TotalTokens}"
                : "-",
            ProcessingTimeMs = log.ProcessingTimeMs,
            CostUsdDisplay = log.CostUsd.HasValue && log.CostUsd.Value > 0
                ? $"${log.CostUsd.Value:F6}" 
                : "$0.000000",
            IsSuccessDisplay = log.IsSuccess ? "成功" : "失败",
            ErrorMessageSummary = TruncateText(log.ErrorMessage, maxTextLength),
            WasJsonRepaired = log.WasJsonRepaired,
            BatchId = log.BatchId,
            BusinessEntityId = log.BusinessEntityId,
            BusinessEntityType = log.BusinessEntityType
        };
    }
    
    /// <summary>
    /// 截断文本用于显示
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>截断后的文本</returns>
    private static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "-";
        }
        
        if (text.Length <= maxLength)
        {
            return text;
        }
        
        var omittedLength = text.Length - maxLength;
        return $"{text.Substring(0, maxLength)}...(省略{omittedLength}字符)";
    }
}

