namespace CodeSpirit.Audit.Services.LLM.Dtos;

/// <summary>
/// LLM使用统计DTO
/// </summary>
public class LLMUsageStatsDto
{
    /// <summary>
    /// 总交互次数
    /// </summary>
    public long TotalInteractions { get; set; }
    
    /// <summary>
    /// 成功交互次数
    /// </summary>
    public long SuccessfulInteractions { get; set; }
    
    /// <summary>
    /// 失败交互次数
    /// </summary>
    public long FailedInteractions { get; set; }
    
    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// 总Token使用量
    /// </summary>
    public long TotalTokensUsed { get; set; }
    
    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime { get; set; }
    
    /// <summary>
    /// 按交互类型统计
    /// </summary>
    public Dictionary<string, long> InteractionsByType { get; set; } = new();
    
    /// <summary>
    /// 按模型统计
    /// </summary>
    public Dictionary<string, long> InteractionsByModel { get; set; } = new();
    
    /// <summary>
    /// 按业务场景统计
    /// </summary>
    public Dictionary<string, long> InteractionsByScenario { get; set; } = new();
    
    /// <summary>
    /// 使用趋势（时间 → 交互次数）
    /// </summary>
    public Dictionary<DateTime, long> UsageTrend { get; set; } = new();
}

