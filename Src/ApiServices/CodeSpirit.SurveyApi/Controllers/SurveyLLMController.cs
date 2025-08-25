using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷LLM生成控制器
/// </summary>
[DisplayName("AI问卷生成")]
[Navigation(Icon = "fa-solid fa-robot", PlatformType = PlatformType.Tenant)]
public class SurveyLLMController : ApiControllerBase
{
    private readonly ISurveyLLMGeneratorService _llmGeneratorService;
    private readonly ILogger<SurveyLLMController> _logger;

    /// <summary>
    /// 初始化LLM控制器
    /// </summary>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveyLLMController(
        ISurveyLLMGeneratorService llmGeneratorService,
        ILogger<SurveyLLMController> logger)
    {
        ArgumentNullException.ThrowIfNull(llmGeneratorService);
        ArgumentNullException.ThrowIfNull(logger);

        _llmGeneratorService = llmGeneratorService;
        _logger = logger;
    }

    /// <summary>
    /// 根据主题生成问卷
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成的问卷</returns>
    [HttpPost("generate")]
    [Operation("AI生成问卷", "form")]
    [DisplayName("AI生成问卷")]
    public async Task<ActionResult<ApiResponse<GeneratedSurveyDto>>> GenerateSurvey([FromBody] GenerateSurveyRequest request)
    {
        var result = await _llmGeneratorService.GenerateSurveyAsync(request);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 优化现有问卷
    /// </summary>
    /// <param name="request">优化请求</param>
    /// <returns>优化建议</returns>
    [HttpPost("optimize")]
    [Operation("AI优化问卷", "form")]
    [DisplayName("AI优化问卷")]
    public async Task<ActionResult<ApiResponse<SurveyOptimizationResult>>> OptimizeSurvey([FromBody] OptimizeSurveyRequest request)
    {
        var result = await _llmGeneratorService.OptimizeSurveyAsync(request.SurveyId, request.OptimizationGoals);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>洞察分析结果</returns>
    [HttpPost("{surveyId}/insights")]
    [Operation("AI洞察分析", "ajax", null, "确定要生成AI洞察分析吗？")]
    [DisplayName("AI洞察分析")]
    public async Task<ActionResult<ApiResponse<SurveyInsightResult>>> GenerateInsights(int surveyId)
    {
        var result = await _llmGeneratorService.GenerateInsightsAsync(surveyId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 验证提示词
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-prompt")]
    [DisplayName("验证提示词")]
    public async Task<ActionResult<ApiResponse<PromptValidationResult>>> ValidatePrompt([FromBody] ValidatePromptRequest request)
    {
        var result = await _llmGeneratorService.ValidatePromptAsync(request.Prompt);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 压缩提示词
    /// </summary>
    /// <param name="request">压缩请求</param>
    /// <returns>压缩结果</returns>
    [HttpPost("compress-prompt")]
    [Operation("压缩提示词", "form")]
    [DisplayName("压缩提示词")]
    public async Task<ActionResult<ApiResponse<CompressPromptResult>>> CompressPrompt([FromBody] CompressPromptRequest request)
    {
        var compressedPrompt = await _llmGeneratorService.CompressPromptAsync(request.Prompt, request.MaxLength);
        
        var result = new CompressPromptResult
        {
            OriginalPrompt = request.Prompt,
            CompressedPrompt = compressedPrompt,
            OriginalLength = request.Prompt.Length,
            CompressedLength = compressedPrompt.Length,
            CompressionRatio = (double)compressedPrompt.Length / request.Prompt.Length
        };

        return SuccessResponse(result);
    }
}

/// <summary>
/// 优化问卷请求
/// </summary>
public class OptimizeSurveyRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 优化目标
    /// </summary>
    [Required]
    [StringLength(1000)]
    [DisplayName("优化目标")]
    public string OptimizationGoals { get; set; } = string.Empty;
}

/// <summary>
/// 验证提示词请求
/// </summary>
public class ValidatePromptRequest
{
    /// <summary>
    /// 提示词
    /// </summary>
    [Required]
    [StringLength(10000)]
    [DisplayName("提示词")]
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>
/// 压缩提示词请求
/// </summary>
public class CompressPromptRequest
{
    /// <summary>
    /// 原始提示词
    /// </summary>
    [Required]
    [StringLength(10000)]
    [DisplayName("原始提示词")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// 最大长度
    /// </summary>
    [Required]
    [Range(100, 5000)]
    [DisplayName("最大长度")]
    public int MaxLength { get; set; } = 2000;
}

/// <summary>
/// 压缩提示词结果
/// </summary>
public class CompressPromptResult
{
    /// <summary>
    /// 原始提示词
    /// </summary>
    [DisplayName("原始提示词")]
    public string OriginalPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 压缩后的提示词
    /// </summary>
    [DisplayName("压缩后的提示词")]
    public string CompressedPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 原始长度
    /// </summary>
    [DisplayName("原始长度")]
    public int OriginalLength { get; set; }

    /// <summary>
    /// 压缩后长度
    /// </summary>
    [DisplayName("压缩后长度")]
    public int CompressedLength { get; set; }

    /// <summary>
    /// 压缩比率
    /// </summary>
    [DisplayName("压缩比率")]
    public double CompressionRatio { get; set; }
}
