using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.LLM;
using CodeSpirit.Shared.Dtos.AI;
using CodeSpirit.Shared.Services;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 支持异步处理的问卷AI生成服务
/// </summary>
public class SurveyAiGeneratorService : BaseAiGeneratorService<GenerateSurveyRequest, GeneratedSurveyDto>, IScopedDependency
{
    private readonly ISurveyLLMGeneratorService _llmGeneratorService;

    /// <summary>
    /// 初始化问卷AI生成服务
    /// </summary>
    /// <param name="aiTaskService">AI任务服务</param>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveyAiGeneratorService(
        IAiTaskService aiTaskService,
        ISurveyLLMGeneratorService llmGeneratorService,
        ILogger<SurveyAiGeneratorService> logger)
        : base(aiTaskService, logger)
    {
        _llmGeneratorService = llmGeneratorService ?? throw new ArgumentNullException(nameof(llmGeneratorService));
    }

    /// <summary>
    /// 获取任务类型名称
    /// </summary>
    /// <returns>任务类型</returns>
    protected override string GetTaskType() => "问卷生成";

    /// <summary>
    /// 执行具体的AI生成逻辑
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="progressCallback">进度回调</param>
    /// <returns>生成结果</returns>
    protected override async Task<GeneratedSurveyDto> DoGenerateAsync(GenerateSurveyRequest request, Action<double, string>? progressCallback = null)
    {
        // 这里调用原有的同步生成方法
        // 可以根据需要在适当的位置调用 progressCallback 来报告进度
        
        progressCallback?.Invoke(0.1, "正在分析问卷主题...");
        await Task.Delay(500); // 模拟处理时间
        
        progressCallback?.Invoke(0.3, "正在生成问卷结构...");
        await Task.Delay(500);
        
        progressCallback?.Invoke(0.6, "正在生成题目内容...");
        var result = await _llmGeneratorService.GenerateSurveyAsync(request);
        
        progressCallback?.Invoke(0.9, "正在优化问卷格式...");
        await Task.Delay(300);
        
        progressCallback?.Invoke(1.0, "问卷生成完成");
        
        return result;
    }

    /// <summary>
    /// 生成开始前的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    protected override async Task OnGenerationStarted(GenerateSurveyRequest request)
    {
        await base.OnGenerationStarted(request);
        _logger.LogInformation("开始生成问卷，主题：{Topic}，题目数量：{QuestionCount}", request.Topic, request.QuestionCount);
    }

    /// <summary>
    /// 结果处理阶段
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected override async Task OnResultProcessing(GenerateSurveyRequest request, GeneratedSurveyDto result)
    {
        await base.OnResultProcessing(request, result);
        _logger.LogInformation("问卷生成完成，包含 {QuestionCount} 个题目", result.Questions?.Count ?? 0);
    }

    /// <summary>
    /// 获取详情页面URL
    /// </summary>
    /// <param name="result">生成结果</param>
    /// <returns>详情页面URL</returns>
    protected override async Task<string?> GetDetailUrl(GeneratedSurveyDto result)
    {
        await Task.CompletedTask;
        
        // 生成的问卷暂时返回问卷列表页面
        // 如果需要，可以在这里保存生成的问卷到数据库并返回具体的详情页面URL
        return "/survey/surveys";
    }
}
