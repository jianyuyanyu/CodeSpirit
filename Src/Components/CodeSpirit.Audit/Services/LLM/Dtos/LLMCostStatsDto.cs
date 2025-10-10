namespace CodeSpirit.Audit.Services.LLM.Dtos;

/// <summary>
/// LLM成本统计DTO
/// </summary>
public class LLMCostStatsDto
{
    /// <summary>
    /// 总成本（USD）
    /// </summary>
    public decimal TotalCost { get; set; }
    
    /// <summary>
    /// 按模型统计成本
    /// </summary>
    public Dictionary<string, decimal> CostByModel { get; set; } = new();
    
    /// <summary>
    /// 按业务场景统计成本
    /// </summary>
    public Dictionary<string, decimal> CostByScenario { get; set; } = new();
    
    /// <summary>
    /// 成本趋势
    /// </summary>
    public Dictionary<DateTime, decimal> CostTrend { get; set; } = new();
}

