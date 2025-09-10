using CodeSpirit.Shared.Dtos.AI;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// AI生成服务基类
/// </summary>
/// <remarks>
/// 提供标准化的AI生成流程，包括任务创建、状态管理、日志记录等功能。
/// 子类只需要实现具体的生成逻辑即可。
/// </remarks>
/// <typeparam name="TRequest">请求类型</typeparam>
/// <typeparam name="TResult">结果类型</typeparam>
public abstract class BaseAiGeneratorService<TRequest, TResult>
    where TRequest : class
    where TResult : class
{
    protected readonly IAiTaskService _aiTaskService;
    protected readonly ILogger _logger;

    /// <summary>
    /// 初始化AI生成服务基类
    /// </summary>
    /// <param name="aiTaskService">AI任务服务</param>
    /// <param name="logger">日志记录器</param>
    protected BaseAiGeneratorService(IAiTaskService aiTaskService, ILogger logger)
    {
        _aiTaskService = aiTaskService ?? throw new ArgumentNullException(nameof(aiTaskService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 生成内容（异步版本，返回任务ID）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>任务ID</returns>
    public virtual async Task<string> GenerateAsync(TRequest request)
    {
        string taskId = await _aiTaskService.CreateTaskAsync(GetTaskType(), request);
        
        // 在后台执行生成任务
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteGenerationTaskAsync(taskId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI生成任务执行失败：{TaskId}", taskId);
                await _aiTaskService.FailTaskAsync(taskId, ex.Message);
            }
        });

        return taskId;
    }


    /// <summary>
    /// 生成内容（同步版本，直接返回结果）
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成结果</returns>
    public virtual async Task<TResult> GenerateDirectAsync(TRequest request)
    {
        try
        {
            await OnGenerationStarted(request);
            var result = await DoGenerateAsync(request);
            await OnGenerationCompleted(request, result);
            return result;
        }
        catch (Exception ex)
        {
            await OnGenerationFailed(request, ex);
            throw;
        }
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态</returns>
    public virtual async Task<AiTaskStatusDto?> GetTaskStatusAsync(string taskId)
    {
        return await _aiTaskService.GetTaskStatusAsync(taskId);
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public virtual async Task CancelTaskAsync(string taskId)
    {
        await _aiTaskService.CancelTaskAsync(taskId);
    }

    /// <summary>
    /// 执行生成任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="request">生成请求</param>
    protected virtual async Task ExecuteGenerationTaskAsync(string taskId, TRequest request)
    {
        try
        {
            // 步骤 1: 准备阶段
            await _aiTaskService.UpdateTaskStatusAsync(taskId, AiTaskStatus.Running, 1, 10, "正在初始化AI生成环境...");
            await _aiTaskService.AddTaskLogAsync(taskId, "开始初始化生成环境");
            
            await OnGenerationStarted(request);
            await OnTaskStep(taskId, 1, "初始化完成");

            // 步骤 2: AI处理中
            await _aiTaskService.UpdateTaskStatusAsync(taskId, AiTaskStatus.Running, 2, 30, "AI正在分析您的需求并生成内容...");
            await _aiTaskService.AddTaskLogAsync(taskId, "开始AI内容生成");

            var result = await DoGenerateAsync(request, (progress, message) => 
            {
                _ = Task.Run(async () =>
                {
                    var actualProgress = 30 + (int)(progress * 0.5); // 30% + 50% for generation
                    await _aiTaskService.UpdateTaskStatusAsync(taskId, AiTaskStatus.Running, 2, actualProgress, message);
                    if (!string.IsNullOrEmpty(message))
                    {
                        await _aiTaskService.AddTaskLogAsync(taskId, message);
                    }
                });
            });

            // 步骤 3: 结果处理
            await _aiTaskService.UpdateTaskStatusAsync(taskId, AiTaskStatus.Running, 3, 85, "正在处理生成结果...");
            await _aiTaskService.AddTaskLogAsync(taskId, "开始处理生成结果");

            await OnResultProcessing(request, result);
            await OnTaskStep(taskId, 3, "结果处理完成");

            // 步骤 4: 完成
            string? detailUrl = await GetDetailUrl(result);
            await _aiTaskService.CompleteTaskAsync(taskId, result, detailUrl);
            await OnGenerationCompleted(request, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI生成任务失败：{TaskId}", taskId);
            await _aiTaskService.FailTaskAsync(taskId, ex.Message);
            await OnGenerationFailed(request, ex);
        }
    }

    /// <summary>
    /// 任务步骤完成回调
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="step">步骤号</param>
    /// <param name="message">消息</param>
    protected virtual async Task OnTaskStep(string taskId, int step, string message)
    {
        await _aiTaskService.AddTaskLogAsync(taskId, message);
    }

    #region 抽象方法 - 子类必须实现

    /// <summary>
    /// 获取任务类型名称
    /// </summary>
    /// <returns>任务类型</returns>
    protected abstract string GetTaskType();

    /// <summary>
    /// 执行具体的AI生成逻辑
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="progressCallback">进度回调（进度0-1，消息）</param>
    /// <returns>生成结果</returns>
    protected abstract Task<TResult> DoGenerateAsync(TRequest request, Action<double, string>? progressCallback = null);

    #endregion

    #region 虚方法 - 子类可选择重写

    /// <summary>
    /// 生成开始前的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    protected virtual Task OnGenerationStarted(TRequest request)
    {
        _logger.LogInformation("AI生成开始：{RequestType}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 结果处理阶段
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected virtual Task OnResultProcessing(TRequest request, TResult result)
    {
        _logger.LogInformation("AI生成结果处理中：{ResultType}", typeof(TResult).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成完成后的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="result">生成结果</param>
    protected virtual Task OnGenerationCompleted(TRequest request, TResult result)
    {
        _logger.LogInformation("AI生成完成：{RequestType} -> {ResultType}", typeof(TRequest).Name, typeof(TResult).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 生成失败后的处理
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="exception">异常信息</param>
    protected virtual Task OnGenerationFailed(TRequest request, Exception exception)
    {
        _logger.LogError(exception, "AI生成失败：{RequestType}", typeof(TRequest).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取详情页面URL
    /// </summary>
    /// <param name="result">生成结果</param>
    /// <returns>详情页面URL，如果没有返回null</returns>
    protected virtual Task<string?> GetDetailUrl(TResult result)
    {
        return Task.FromResult<string?>(null);
    }

    #endregion
}
