using CodeSpirit.SurveyApi.Dtos.Survey;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷LLM生成服务接口
/// </summary>
public interface ISurveyLLMGeneratorService
{
    /// <summary>
    /// 根据主题生成问卷
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成的问卷</returns>
    Task<GeneratedSurveyDto> GenerateSurveyAsync(GenerateSurveyRequest request);

    /// <summary>
    /// 优化现有问卷
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="optimizationGoals">优化目标</param>
    /// <returns>优化建议</returns>
    Task<SurveyOptimizationResult> OptimizeSurveyAsync(int surveyId, string optimizationGoals);

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>洞察分析结果</returns>
    Task<SurveyInsightResult> GenerateInsightsAsync(int surveyId);

    /// <summary>
    /// 验证提示词长度
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>验证结果</returns>
    Task<PromptValidationResult> ValidatePromptAsync(string prompt);

    /// <summary>
    /// 压缩提示词
    /// </summary>
    /// <param name="prompt">原始提示词</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>压缩后的提示词</returns>
    Task<string> CompressPromptAsync(string prompt, int maxLength);
}

/// <summary>
/// 生成问卷请求
/// </summary>
public class GenerateSurveyRequest
{
    /// <summary>
    /// 问卷主题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 问卷类型
    /// </summary>
    [StringLength(100)]
    public string? SurveyType { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    [Range(1, 50)]
    public int QuestionCount { get; set; } = 10;

    /// <summary>
    /// 目标受众
    /// </summary>
    [StringLength(500)]
    public string? TargetAudience { get; set; }

    /// <summary>
    /// 调查目标
    /// </summary>
    [StringLength(1000)]
    public string? Goals { get; set; }

    /// <summary>
    /// 自定义提示词
    /// </summary>
    [StringLength(4000)]
    public string? CustomPrompt { get; set; }
}

/// <summary>
/// 生成的问卷DTO
/// </summary>
public class GeneratedSurveyDto
{
    /// <summary>
    /// 问卷标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 生成的题目列表
    /// </summary>
    public List<GeneratedQuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// 使用的提示词
    /// </summary>
    public string? UsedPrompt { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 生成质量评分（1-10）
    /// </summary>
    public int QualityScore { get; set; }
}

/// <summary>
/// 生成的题目DTO
/// </summary>
public class GeneratedQuestionDto
{
    /// <summary>
    /// 题目标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    public List<GeneratedQuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// 生成的题目选项DTO
/// </summary>
public class GeneratedQuestionOptionDto
{
    /// <summary>
    /// 选项文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; }
}

/// <summary>
/// 问卷优化结果
/// </summary>
public class SurveyOptimizationResult
{
    /// <summary>
    /// 优化建议列表
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 整体评分（1-10）
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// 优化后预期提升
    /// </summary>
    public string? ExpectedImprovement { get; set; }
}

/// <summary>
/// 优化建议
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// 建议类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 建议内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 影响的题目ID（可选）
    /// </summary>
    public int? QuestionId { get; set; }
}

/// <summary>
/// 问卷洞察结果
/// </summary>
public class SurveyInsightResult
{
    /// <summary>
    /// 洞察列表
    /// </summary>
    public List<SurveyInsight> Insights { get; set; } = new();

    /// <summary>
    /// 数据质量评分
    /// </summary>
    public int DataQualityScore { get; set; }

    /// <summary>
    /// 关键发现
    /// </summary>
    public List<string> KeyFindings { get; set; } = new();

    /// <summary>
    /// 建议行动
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// 问卷洞察
/// </summary>
public class SurveyInsight
{
    /// <summary>
    /// 洞察类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 洞察内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 相关题目ID
    /// </summary>
    public List<int> RelatedQuestionIds { get; set; } = new();
}

/// <summary>
/// 提示词验证结果
/// </summary>
public class PromptValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 提示词长度
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// 预估Token数
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 是否需要压缩
    /// </summary>
    public bool NeedsCompression { get; set; }
}
