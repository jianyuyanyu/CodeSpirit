using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.LLM;
using CodeSpirit.Shared.Dtos.AI;
using CodeSpirit.Shared.Services;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 支持异步处理的问卷AI生成服务
/// </summary>
public class SurveyAiGeneratorService : BaseAiGeneratorService<GenerateSurveyRequest, GeneratedSurveyDto>, IScopedDependency
{
    private readonly ISurveyLLMGeneratorService _llmGeneratorService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 初始化问卷AI生成服务
    /// </summary>
    /// <param name="aiTaskService">AI任务服务</param>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="serviceScopeFactory">服务范围工厂</param>
    /// <param name="currentUser">当前用户服务</param>
    public SurveyAiGeneratorService(
        IAiTaskService aiTaskService,
        ISurveyLLMGeneratorService llmGeneratorService,
        ILogger<SurveyAiGeneratorService> logger,
        IServiceScopeFactory serviceScopeFactory,
        ICurrentUser currentUser)
        : base(aiTaskService, logger, serviceScopeFactory)
    {
        _llmGeneratorService = llmGeneratorService ?? throw new ArgumentNullException(nameof(llmGeneratorService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// 获取任务类型名称
    /// </summary>
    /// <returns>任务类型</returns>
    protected override string GetTaskType() => "问卷生成";

    /// <summary>
    /// 重写异步生成方法，确保在Task.Run之前捕获租户上下文
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>任务ID</returns>
    public override async Task<string> GenerateAsync(GenerateSurveyRequest request)
    {
        // 在Task.Run之前捕获当前的租户上下文
        var capturedTenantId = _currentUser.TenantId;
        var capturedUserId = _currentUser.Id;
        var capturedUserName = _currentUser.UserName;
        
        _logger.LogDebug("捕获租户上下文：TenantId={TenantId}, UserId={UserId}, UserName={UserName}", 
            capturedTenantId, capturedUserId, capturedUserName);

        string taskId = await _aiTaskService.CreateTaskAsync(GetTaskType(), request);
        
        // 在后台执行生成任务，使用独立的服务范围，并传递捕获的上下文
        _ = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            try
            {
                // 在新的服务范围中设置租户上下文
                SetTenantContextInScope(scope.ServiceProvider, capturedTenantId, capturedUserId, capturedUserName);
                
                await ExecuteGenerationTaskAsyncWithScope(scope.ServiceProvider, taskId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成任务执行失败：{TaskId}", taskId);
                
                // 使用独立的服务范围来处理失败任务
                try
                {
                    var aiTaskService = scope.ServiceProvider.GetRequiredService<IAiTaskService>();
                    await aiTaskService.FailTaskAsync(taskId, ex.Message);
                }
                catch (Exception failEx)
                {
                    _logger.LogError(failEx, "更新任务失败状态时出错：{TaskId}", taskId);
                }
            }
        });

        return taskId;
    }

    /// <summary>
    /// 在新的服务范围中设置租户上下文
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    private void SetTenantContextInScope(IServiceProvider serviceProvider, string? tenantId, long? userId, string? userName)
    {
        try
        {
            // 设置当前用户的租户上下文
            var scopedCurrentUser = serviceProvider.GetRequiredService<ICurrentUser>();
            if (scopedCurrentUser is ISettableCurrentUser settableCurrentUser)
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    settableCurrentUser.SetTenantId(tenantId);
                    _logger.LogDebug("已在新服务范围中设置租户ID: {TenantId}", tenantId);
                }
                
                if (userId.HasValue)
                {
                    settableCurrentUser.SetUserId(userId.Value);
                    _logger.LogDebug("已在新服务范围中设置用户ID: {UserId}", userId.Value);
                }
                
                if (!string.IsNullOrEmpty(userName))
                {
                    settableCurrentUser.SetUserName(userName);
                    _logger.LogDebug("已在新服务范围中设置用户名: {UserName}", userName);
                }
            }
            else
            {
                _logger.LogWarning("无法设置租户上下文：当前用户服务未实现ISettableCurrentUser接口");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置租户上下文时发生异常：TenantId={TenantId}, UserId={UserId}", tenantId, userId);
        }
    }

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
        
        progressCallback?.Invoke(1.0, "问卷生成完成");
        
        return result;
    }

    /// <summary>
    /// 执行具体的AI生成逻辑（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="request">生成请求</param>
    /// <param name="progressCallback">进度回调</param>
    /// <returns>生成结果</returns>
    protected override async Task<GeneratedSurveyDto> DoGenerateAsyncWithScope(IServiceProvider serviceProvider, GenerateSurveyRequest request, Action<double, string>? progressCallback = null)
    {
        // 从独立的服务范围获取所需的服务
        var llmGeneratorService = serviceProvider.GetRequiredService<ISurveyLLMGeneratorService>();
        
        // 验证租户上下文是否正确设置
        var scopedCurrentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        _logger.LogDebug("问卷生成开始，当前租户上下文：TenantId={TenantId}, UserId={UserId}, UserName={UserName}", 
            scopedCurrentUser.TenantId, scopedCurrentUser.Id, scopedCurrentUser.UserName);
        
        progressCallback?.Invoke(0.1, "正在分析问卷主题...");
        await Task.Delay(500); // 模拟处理时间
        
        progressCallback?.Invoke(0.3, "正在生成问卷结构...");
        await Task.Delay(500);
        
        progressCallback?.Invoke(0.6, "正在生成题目内容...");
        var result = await llmGeneratorService.GenerateSurveyAsync(request);
        
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
