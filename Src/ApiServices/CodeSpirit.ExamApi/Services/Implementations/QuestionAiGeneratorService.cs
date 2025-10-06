using CodeSpirit.Core;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.AI;
using CodeSpirit.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 题目AI生成服务
/// </summary>
public class QuestionAiGeneratorService : BaseAiGeneratorService<AIGenerateQuestionDto, List<CreateQuestionDto>>
{
    private readonly IAIQuestionGeneratorService _aiQuestionGeneratorService;
    private readonly IQuestionService _questionService;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 初始化题目AI生成服务
    /// </summary>
    /// <param name="aiTaskService">AI任务服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="serviceScopeFactory">服务范围工厂</param>
    /// <param name="aiQuestionGeneratorService">AI题目生成服务</param>
    /// <param name="questionService">题目服务</param>
    /// <param name="currentUser">当前用户</param>
    public QuestionAiGeneratorService(
        IAiTaskService aiTaskService,
        ILogger<QuestionAiGeneratorService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IAIQuestionGeneratorService aiQuestionGeneratorService,
        IQuestionService questionService,
        ICurrentUser currentUser)
        : base(aiTaskService, logger, serviceScopeFactory)
    {
        _aiQuestionGeneratorService = aiQuestionGeneratorService ?? throw new ArgumentNullException(nameof(aiQuestionGeneratorService));
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// 重写异步生成方法，确保在Task.Run之前捕获租户上下文
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>任务ID</returns>
    public override async Task<string> GenerateAsync(AIGenerateQuestionDto request)
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
                // 在独立的服务范围中验证租户上下文
                var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                _logger.LogDebug("后台任务中的租户上下文：TenantId={TenantId}, UserId={UserId}, UserName={UserName}", 
                    scopedCurrentUser.TenantId, scopedCurrentUser.Id, scopedCurrentUser.UserName);

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
    /// 获取任务类型名称
    /// </summary>
    /// <returns>任务类型</returns>
    protected override string GetTaskType()
    {
        return "QuestionGeneration";
    }

    /// <summary>
    /// 执行具体的AI生成逻辑
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="progressCallback">进度回调</param>
    /// <returns>生成结果</returns>
    protected override async Task<List<CreateQuestionDto>> DoGenerateAsync(AIGenerateQuestionDto request, Action<double, string>? progressCallback = null)
    {
        // 这里调用原有的同步生成方法
        // 可以根据需要在适当的位置调用 progressCallback 来报告进度
        
        progressCallback?.Invoke(0.1, "正在分析题目主题...");
        await Task.Delay(500); // 模拟处理时间
        
        progressCallback?.Invoke(0.3, "正在生成题目结构...");
        await Task.Delay(500);
        
        progressCallback?.Invoke(0.6, "正在生成题目内容...");
        var result = await _aiQuestionGeneratorService.GenerateQuestionsAsync(request);
        
        progressCallback?.Invoke(1.0, "题目生成完成");
        
        return result;
    }

    /// <summary>
    /// 执行具体的AI生成逻辑（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="request">生成请求</param>
    /// <param name="progressCallback">进度回调</param>
    /// <returns>生成结果</returns>
    protected override async Task<List<CreateQuestionDto>> DoGenerateAsyncWithScope(IServiceProvider serviceProvider, AIGenerateQuestionDto request, Action<double, string>? progressCallback = null)
    {
        // 从独立的服务范围获取所需的服务
        var aiQuestionGeneratorService = serviceProvider.GetRequiredService<IAIQuestionGeneratorService>();
        
        // 验证租户上下文是否正确设置
        var scopedCurrentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        _logger.LogDebug("题目生成开始，当前租户上下文：TenantId={TenantId}, UserId={UserId}, UserName={UserName}", 
            scopedCurrentUser.TenantId, scopedCurrentUser.Id, scopedCurrentUser.UserName);
        
        progressCallback?.Invoke(0.1, "正在分析题目主题...");
        await Task.Delay(500); // 模拟处理时间
        
        progressCallback?.Invoke(0.3, "正在生成题目结构...");
        await Task.Delay(500);
        
        progressCallback?.Invoke(0.6, "正在生成题目内容...");
        var result = await aiQuestionGeneratorService.GenerateQuestionsAsync(request);
        
        progressCallback?.Invoke(0.9, "正在优化题目格式...");
        await Task.Delay(300);
        
        progressCallback?.Invoke(1.0, "题目生成完成");
        
        return result;
    }

    /// <summary>
    /// 生成开始前的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    protected override async Task OnGenerationStarted(AIGenerateQuestionDto request)
    {
        await base.OnGenerationStarted(request);
        _logger.LogInformation("开始生成题目，主题：{Topic}，题目数量：{Count}，类型：{Type}", 
            request.Topic, request.Count, request.Type);
    }

    /// <summary>
    /// 生成完成后的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected override async Task OnGenerationCompleted(AIGenerateQuestionDto request, List<CreateQuestionDto> result)
    {
        await base.OnGenerationCompleted(request, result);
        _logger.LogInformation("题目生成完成，共生成 {Count} 个题目", result.Count);
    }

    /// <summary>
    /// 结果处理阶段
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected override async Task OnResultProcessing(AIGenerateQuestionDto request, List<CreateQuestionDto> result)
    {
        await base.OnResultProcessing(request, result);
        
        // 这里可以添加额外的结果处理逻辑
        // 比如自动保存题目到数据库
        if (result.Any())
        {
            _logger.LogInformation("正在处理生成的 {Count} 个题目", result.Count);
            
            // 自动保存题目到数据库
            await SaveQuestionsToDatabase(result);
        }
    }

    /// <summary>
    /// 结果处理阶段（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected override async Task OnResultProcessingWithScope(IServiceProvider serviceProvider, AIGenerateQuestionDto request, List<CreateQuestionDto> result)
    {
        await base.OnResultProcessingWithScope(serviceProvider, request, result);
        
        if (result.Any())
        {
            _logger.LogInformation("正在处理生成的 {Count} 个题目", result.Count);
            
            // 使用独立的服务范围保存题目到数据库
            await SaveQuestionsToDatabaseWithScope(serviceProvider, result);
        }
    }

    /// <summary>
    /// 保存题目到数据库
    /// </summary>
    /// <param name="questions">生成的题目列表</param>
    private async Task SaveQuestionsToDatabase(List<CreateQuestionDto> questions)
    {
        int successCount = 0;
        List<string> failedItems = new();

        foreach (var question in questions)
        {
            try
            {
                await _questionService.CreateQuestionAsync(question);
                successCount++;
                _logger.LogDebug("成功保存题目: {Content}", question.Content.Substring(0, Math.Min(50, question.Content.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存题目失败: {Content}", question.Content.Substring(0, Math.Min(50, question.Content.Length)));
                failedItems.Add($"题目 [{question.Content.Substring(0, Math.Min(30, question.Content.Length))}...] 保存失败: {ex.Message}");
            }
        }

        _logger.LogInformation("题目保存完成: 成功 {SuccessCount} 个，失败 {FailedCount} 个", successCount, failedItems.Count);
        
        if (failedItems.Any())
        {
            _logger.LogWarning("以下题目保存失败: {FailedItems}", string.Join("; ", failedItems));
        }
    }

    /// <summary>
    /// 使用独立的服务范围保存题目到数据库
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="questions">生成的题目列表</param>
    private async Task SaveQuestionsToDatabaseWithScope(IServiceProvider serviceProvider, List<CreateQuestionDto> questions)
    {
        // 从独立的服务范围获取题目服务
        var questionService = serviceProvider.GetRequiredService<IQuestionService>();
        
        int successCount = 0;
        List<string> failedItems = new();

        foreach (var question in questions)
        {
            try
            {
                await questionService.CreateQuestionAsync(question);
                successCount++;
                _logger.LogDebug("成功保存题目: {Content}", question.Content.Substring(0, Math.Min(50, question.Content.Length)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存题目失败: {Content}", question.Content.Substring(0, Math.Min(50, question.Content.Length)));
                failedItems.Add($"题目 [{question.Content.Substring(0, Math.Min(30, question.Content.Length))}...] 保存失败: {ex.Message}");
            }
        }

        _logger.LogInformation("题目保存完成: 成功 {SuccessCount} 个，失败 {FailedCount} 个", successCount, failedItems.Count);
        
        if (failedItems.Any())
        {
            _logger.LogWarning("以下题目保存失败: {FailedItems}", string.Join("; ", failedItems));
        }
    }

    /// <summary>
    /// 获取详情页面URL
    /// </summary>
    /// <param name="result">生成结果</param>
    /// <returns>详情页面URL</returns>
    protected override Task<string?> GetDetailUrl(List<CreateQuestionDto> result)
    {
        // 返回题目管理页面，可以添加筛选条件显示最新生成的题目
        return Task.FromResult<string?>("/exam/Questions");
    }

    /// <summary>
    /// 获取详情页面URL（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="result">生成结果</param>
    /// <returns>详情页面URL</returns>
    protected override async Task<string?> GetDetailUrlWithScope(IServiceProvider serviceProvider, List<CreateQuestionDto> result)
    {
        // 可以在这里添加更复杂的逻辑，比如获取保存成功的题目ID等
        return await GetDetailUrl(result);
    }
}
