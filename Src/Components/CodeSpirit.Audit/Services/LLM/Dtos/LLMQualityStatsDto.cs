namespace CodeSpirit.Audit.Services.LLM.Dtos;

/// <summary>
/// LLM质量统计DTO
/// </summary>
public class LLMQualityStatsDto
{
    /// <summary>
    /// 平均质量评分
    /// </summary>
    public double AverageQualityScore { get; set; }
    
    /// <summary>
    /// JSON修复率
    /// </summary>
    public double JsonRepairRate { get; set; }
    
    /// <summary>
    /// 平均重试次数
    /// </summary>
    public double AverageRetryCount { get; set; }
    
    /// <summary>
    /// 按模型统计质量评分
    /// </summary>
    public Dictionary<string, double> QualityScoreByModel { get; set; } = new();
    
    /// <summary>
    /// 按业务场景统计质量评分
    /// </summary>
    public Dictionary<string, double> QualityScoreByScenario { get; set; } = new();
}

