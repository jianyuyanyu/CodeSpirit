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
            var aiTaskService = scope.ServiceProvider.GetRequiredService<IAiTaskService>();
            
            try
            {
                // 在新的服务范围中设置租户上下文
                SetTenantContextInScope(scope.ServiceProvider, capturedTenantId, capturedUserId, capturedUserName);
                
                await ExecuteGenerationTaskAsyncWithScope(scope.ServiceProvider, taskId, request);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "AI生成任务参数错误：{TaskId} - {Error}", taskId, argEx.Message);
                await aiTaskService.FailTaskAsync(taskId, $"参数错误：{argEx.Message}");
            }
            catch (InvalidOperationException opEx)
            {
                _logger.LogWarning(opEx, "AI生成任务操作错误：{TaskId} - {Error}", taskId, opEx.Message);
                await aiTaskService.FailTaskAsync(taskId, $"操作错误：{opEx.Message}");
            }
            catch (TimeoutException timeEx)
            {
                _logger.LogWarning(timeEx, "AI生成任务超时：{TaskId} - {Error}", taskId, timeEx.Message);
                await aiTaskService.FailTaskAsync(taskId, "AI生成超时，请稍后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成任务执行失败：{TaskId} - {Error}", taskId, ex.Message);
                
                // 尝试提供更友好的错误信息
                var friendlyMessage = GetFriendlyErrorMessage(ex);
                await aiTaskService.FailTaskAsync(taskId, friendlyMessage);
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
        try
        {
            progressCallback?.Invoke(0.05, $"开始分析问卷主题：{request.Topic}");
            _logger.LogInformation("开始问卷生成，主题：{Topic}，题目数量：{QuestionCount}", request.Topic, request.QuestionCount);
            
            progressCallback?.Invoke(0.15, "正在验证生成参数...");
            await ValidateGenerationRequest(request);
            
            progressCallback?.Invoke(0.25, "正在构建AI提示词...");
            await Task.Delay(200); // 给用户一些视觉反馈
            
            progressCallback?.Invoke(0.35, "正在调用AI模型生成问卷...");
            var result = await _llmGeneratorService.GenerateSurveyAsync(request);
            
            progressCallback?.Invoke(0.75, "正在验证生成结果...");
            await ValidateGenerationResult(result, request);
            
            progressCallback?.Invoke(0.90, "正在优化问卷质量...");
            await OptimizeGenerationResult(result, request);
            
            progressCallback?.Invoke(1.0, $"问卷生成完成！共生成 {result.Questions?.Count ?? 0} 道题目");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "问卷生成过程中发生错误：{Error}", ex.Message);
            progressCallback?.Invoke(0.0, $"生成失败：{ex.Message}");
            throw;
        }
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
        try
        {
            // 从独立的服务范围获取所需的服务
            var llmGeneratorService = serviceProvider.GetRequiredService<ISurveyLLMGeneratorService>();
            
            // 验证租户上下文是否正确设置
            var scopedCurrentUser = serviceProvider.GetRequiredService<ICurrentUser>();
            _logger.LogDebug("问卷生成开始，当前租户上下文：TenantId={TenantId}, UserId={UserId}, UserName={UserName}", 
                scopedCurrentUser.TenantId, scopedCurrentUser.Id, scopedCurrentUser.UserName);
            
            progressCallback?.Invoke(0.05, $"开始分析问卷主题：{request.Topic}");
            _logger.LogInformation("开始问卷生成，主题：{Topic}，题目数量：{QuestionCount}，用户：{UserId}", 
                request.Topic, request.QuestionCount, scopedCurrentUser.Id);
            
            progressCallback?.Invoke(0.15, "正在验证生成参数...");
            await ValidateGenerationRequestWithScope(serviceProvider, request);
            
            progressCallback?.Invoke(0.25, "正在构建AI提示词...");
            await Task.Delay(200); // 给用户一些视觉反馈
            
            progressCallback?.Invoke(0.35, "正在调用AI模型生成问卷...");
            var result = await llmGeneratorService.GenerateSurveyAsync(request);
            
            progressCallback?.Invoke(0.65, "正在验证生成结果...");
            await ValidateGenerationResultWithScope(serviceProvider, result, request);
            
            progressCallback?.Invoke(0.80, "正在优化问卷质量...");
            await OptimizeGenerationResultWithScope(serviceProvider, result, request);
            
            progressCallback?.Invoke(0.95, "正在完成最终处理...");
            await Task.Delay(100);
            
            var questionCount = result.Questions?.Count ?? 0;
            progressCallback?.Invoke(1.0, $"问卷生成完成！共生成 {questionCount} 道题目");
            
            _logger.LogInformation("问卷生成成功完成，题目数量：{QuestionCount}，质量评分：{QualityScore}", 
                questionCount, result.QualityScore);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "问卷生成过程中发生错误：{Error}", ex.Message);
            progressCallback?.Invoke(0.0, $"生成失败：{ex.Message}");
            throw;
        }
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

    #region 私有辅助方法

    /// <summary>
    /// 验证生成请求参数
    /// </summary>
    /// <param name="request">生成请求</param>
    private async Task ValidateGenerationRequest(GenerateSurveyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ArgumentException("问卷主题不能为空");
        }

        if (request.QuestionCount < 1 || request.QuestionCount > 50)
        {
            throw new ArgumentException("题目数量必须在1-50之间");
        }

        _logger.LogDebug("生成请求参数验证通过：主题={Topic}, 题目数量={QuestionCount}", request.Topic, request.QuestionCount);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 验证生成请求参数（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="request">生成请求</param>
    private async Task ValidateGenerationRequestWithScope(IServiceProvider serviceProvider, GenerateSurveyRequest request)
    {
        await ValidateGenerationRequest(request);
    }

    /// <summary>
    /// 验证生成结果
    /// </summary>
    /// <param name="result">生成结果</param>
    /// <param name="request">原始请求</param>
    private async Task ValidateGenerationResult(GeneratedSurveyDto result, GenerateSurveyRequest request)
    {
        if (result == null)
        {
            throw new InvalidOperationException("AI生成结果为空");
        }

        if (string.IsNullOrWhiteSpace(result.Title))
        {
            result.Title = request.Topic ?? "未知主题";
            _logger.LogWarning("生成的问卷标题为空，已使用主题作为标题");
        }

        if (result.Questions == null || !result.Questions.Any())
        {
            _logger.LogError("AI生成的问卷没有包含任何题目，主题：{Topic}，请求题目数量：{QuestionCount}", request.Topic, request.QuestionCount);
            
            // 尝试创建基础题目作为备用方案
            result.Questions = await CreateFallbackQuestions(request);
            _logger.LogInformation("已创建 {Count} 个备用题目", result.Questions.Count);
        }

        // 验证题目内容
        var validQuestions = new List<GeneratedQuestionDto>();
        foreach (var question in result.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.Title))
            {
                _logger.LogWarning("发现空标题题目，已跳过");
                continue;
            }

            // 确保题目有合理的类型
            if (string.IsNullOrWhiteSpace(question.Type))
            {
                question.Type = "Text";
                _logger.LogDebug("题目 '{Title}' 类型为空，已设置为Text类型", question.Title);
            }

            validQuestions.Add(question);
        }

        result.Questions = validQuestions;
        _logger.LogInformation("生成结果验证完成，有效题目数量：{ValidCount}/{TotalCount}", validQuestions.Count, result.Questions.Count);
    }

    /// <summary>
    /// 验证生成结果（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="result">生成结果</param>
    /// <param name="request">原始请求</param>
    private async Task ValidateGenerationResultWithScope(IServiceProvider serviceProvider, GeneratedSurveyDto result, GenerateSurveyRequest request)
    {
        await ValidateGenerationResult(result, request);
    }

    /// <summary>
    /// 优化生成结果
    /// </summary>
    /// <param name="result">生成结果</param>
    /// <param name="request">原始请求</param>
    private async Task OptimizeGenerationResult(GeneratedSurveyDto result, GenerateSurveyRequest request)
    {
        // 确保题目有正确的排序索引
        for (int i = 0; i < result.Questions.Count; i++)
        {
            result.Questions[i].OrderIndex = i + 1;
        }

        // 优化题目类型映射
        foreach (var question in result.Questions)
        {
            question.Type = NormalizeQuestionType(question.Type);
        }

        _logger.LogDebug("生成结果优化完成，题目数量：{Count}", result.Questions.Count);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 优化生成结果（使用独立的服务范围）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="result">生成结果</param>
    /// <param name="request">原始请求</param>
    private async Task OptimizeGenerationResultWithScope(IServiceProvider serviceProvider, GeneratedSurveyDto result, GenerateSurveyRequest request)
    {
        await OptimizeGenerationResult(result, request);
    }

    /// <summary>
    /// 创建备用题目（当AI生成失败时）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>备用题目列表</returns>
    private async Task<List<GeneratedQuestionDto>> CreateFallbackQuestions(GenerateSurveyRequest request)
    {
        var fallbackQuestions = new List<GeneratedQuestionDto>();
        var questionCount = Math.Min(request.QuestionCount, 5); // 最多创建5个备用题目

        for (int i = 1; i <= questionCount; i++)
        {
            var question = new GeneratedQuestionDto
            {
                Title = $"关于{request.Topic}的问题{i}",
                Description = $"请就{request.Topic}相关内容提供您的意见",
                Type = i == 1 ? "SingleChoice" : (i == questionCount ? "Textarea" : "Text"),
                IsRequired = i <= 2, // 前两个题目设为必填
                OrderIndex = i,
                Options = new List<GeneratedQuestionOptionDto>()
            };

            // 为选择题添加基础选项
            if (question.Type == "SingleChoice")
            {
                question.Options.AddRange(new[]
                {
                    new GeneratedQuestionOptionDto { Text = "非常满意", Value = "5", OrderIndex = 1 },
                    new GeneratedQuestionOptionDto { Text = "满意", Value = "4", OrderIndex = 2 },
                    new GeneratedQuestionOptionDto { Text = "一般", Value = "3", OrderIndex = 3 },
                    new GeneratedQuestionOptionDto { Text = "不满意", Value = "2", OrderIndex = 4 },
                    new GeneratedQuestionOptionDto { Text = "非常不满意", Value = "1", OrderIndex = 5 }
                });
            }

            fallbackQuestions.Add(question);
        }

        _logger.LogInformation("已创建 {Count} 个备用题目作为AI生成失败的替代方案", fallbackQuestions.Count);
        await Task.CompletedTask;
        return fallbackQuestions;
    }

    /// <summary>
    /// 标准化题目类型
    /// </summary>
    /// <param name="questionType">原始题目类型</param>
    /// <returns>标准化后的题目类型</returns>
    private string NormalizeQuestionType(string questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
            return "Text";

        return questionType.ToLower() switch
        {
            "single" or "radio" or "singlechoice" => "SingleChoice",
            "multiple" or "checkbox" or "multiplechoice" => "MultipleChoice",
            "input" or "text" => "Text",
            "longtext" or "textarea" => "Textarea",
            "numeric" or "number" => "Number",
            "rate" or "rating" => "Rating",
            "date" => "Date",
            "time" => "Time",
            "datetime" => "DateTime",
            "matrix" => "Matrix",
            "rank" or "ranking" => "Ranking",
            _ => questionType // 保持原样，让后续验证处理
        };
    }

    /// <summary>
    /// 获取友好的错误信息
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>友好的错误信息</returns>
    private string GetFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "请求参数有误，请检查输入内容",
            InvalidOperationException => "当前操作无法执行，请稍后重试",
            TimeoutException => "AI服务响应超时，请稍后重试",
            HttpRequestException => "网络连接异常，请检查网络状态",
            UnauthorizedAccessException => "权限不足，请联系管理员",
            _ when exception.Message.Contains("Token") => "AI服务配额不足，请联系管理员",
            _ when exception.Message.Contains("Rate") => "请求过于频繁，请稍后重试",
            _ when exception.Message.Contains("JSON") => "AI响应格式异常，正在尝试修复",
            _ when exception.Message.Contains("Network") => "网络连接异常，请稍后重试",
            _ => $"生成过程中遇到问题：{exception.Message}"
        };
    }

    #endregion
}
