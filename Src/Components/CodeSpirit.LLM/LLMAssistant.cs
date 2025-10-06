using CodeSpirit.LLM.Factories;
using CodeSpirit.LLM.Processors;
using CodeSpirit.LLM.Prompts;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM;

/// <summary>
/// LLM助手，简化LLM服务的使用
/// </summary>
public class LLMAssistant
{
    private readonly ILLMClientFactory _llmClientFactory;
    private readonly ILogger<LLMAssistant> _logger;
    private readonly ILLMJsonProcessor _jsonProcessor;
    private readonly ILLMBatchProcessor _batchProcessor;
    private readonly ILLMPromptBuilder _promptBuilder;

    /// <summary>
    /// 初始化LLM助手
    /// </summary>
    /// <param name="llmClientFactory">LLM客户端工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="jsonProcessor">JSON处理器</param>
    /// <param name="batchProcessor">批量处理器</param>
    /// <param name="promptBuilder">提示词构建器</param>
    public LLMAssistant(
        ILLMClientFactory llmClientFactory,
        ILogger<LLMAssistant> logger,
        ILLMJsonProcessor jsonProcessor,
        ILLMBatchProcessor batchProcessor,
        ILLMPromptBuilder promptBuilder)
    {
        _llmClientFactory = llmClientFactory;
        _logger = logger;
        _jsonProcessor = jsonProcessor;
        _batchProcessor = batchProcessor;
        _promptBuilder = promptBuilder;
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(prompt);
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string prompt, int maxTokens)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(prompt, maxTokens);
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <returns>生成的内容</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
    {
        var client = await GetClientAsync();
        return await client.GenerateContentAsync(systemPrompt, userPrompt);
    }

    /// <summary>
    /// 处理结构化任务
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="prompt">提示词</param>
    /// <param name="options">结构化任务选项</param>
    /// <returns>结构化处理结果</returns>
    public async Task<StructuredTaskResult<T>> ProcessStructuredTaskAsync<T>(
        string prompt, 
        StructuredTaskOptions? options = null) where T : class
    {
        options ??= new StructuredTaskOptions();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("开始处理结构化任务，目标类型: {Type}", typeof(T).Name);

            // 如果启用重试，使用批量处理器的重试功能
            string aiResponse;
            if (options.EnableRetry)
            {
                var retryOptions = new RetryOptions
                {
                    MaxRetries = options.MaxRetries,
                    RetryDelay = options.RetryDelay,
                    UseIncrementalDelay = true
                };

                aiResponse = await _batchProcessor.ProcessWithRetryAsync(
                    () => GenerateContentAsync(prompt), retryOptions);
            }
            else
            {
                aiResponse = await GenerateContentAsync(prompt);
            }

            // 使用JSON处理器解析响应
            var parseResult = await _jsonProcessor.ParseStructuredResponseAsync<T>(aiResponse);
            var endTime = DateTime.UtcNow;

            if (parseResult.IsSuccess)
            {
                _logger.LogInformation("结构化任务处理成功，类型: {Type}，耗时: {Duration}ms，是否修复: {WasRepaired}",
                    typeof(T).Name, (endTime - startTime).TotalMilliseconds, parseResult.WasRepaired);

                return StructuredTaskResult<T>.Success(
                    parseResult.Result!, aiResponse, parseResult.CleanedJson, 
                    parseResult.WasRepaired, startTime, endTime);
            }
            else
            {
                _logger.LogWarning("结构化任务处理失败，类型: {Type}，错误: {Errors}",
                    typeof(T).Name, string.Join("; ", parseResult.Errors));

                return StructuredTaskResult<T>.Failure(
                    parseResult.Errors, aiResponse, startTime, endTime);
            }
        }
        catch (Exception ex)
        {
            var endTime = DateTime.UtcNow;
            _logger.LogError(ex, "结构化任务处理异常，类型: {Type}", typeof(T).Name);

            return StructuredTaskResult<T>.Failure(
                new List<string> { ex.Message }, "", startTime, endTime);
        }
    }

    /// <summary>
    /// 批量处理结构化任务
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="items">输入项目列表</param>
    /// <param name="promptGenerator">提示词生成器</param>
    /// <param name="options">批量处理选项</param>
    /// <returns>批量处理结果</returns>
    public async Task<BatchProcessingResult<StructuredTaskResult<TResult>>> ProcessBatchStructuredTaskAsync<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, string> promptGenerator,
        BatchProcessingOptions? options = null) where TResult : class
    {
        options ??= new BatchProcessingOptions();

        _logger.LogInformation("开始批量处理结构化任务，输入类型: {InputType}，结果类型: {ResultType}，项目数: {Count}",
            typeof(TInput).Name, typeof(TResult).Name, items.Count);

        // 定义批量处理器
        async Task<List<StructuredTaskResult<TResult>>> ProcessBatch(List<TInput> batch)
        {
            var prompt = promptGenerator(batch);
            var taskOptions = new StructuredTaskOptions
            {
                EnableRetry = true,
                MaxRetries = options.MaxRetries,
                RetryDelay = options.RetryDelay
            };

            var result = await ProcessStructuredTaskAsync<TResult>(prompt, taskOptions);
            return new List<StructuredTaskResult<TResult>> { result };
        }

        return await _batchProcessor.ProcessBatchWithRetryAsync(items, ProcessBatch, options);
    }

    /// <summary>
    /// 使用模板处理结构化任务
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    /// <param name="templateName">模板名称</param>
    /// <param name="parameters">模板参数</param>
    /// <param name="options">结构化任务选项</param>
    /// <returns>结构化处理结果</returns>
    public async Task<StructuredTaskResult<T>> ProcessStructuredTaskWithTemplateAsync<T>(
        string templateName,
        object parameters,
        StructuredTaskOptions? options = null) where T : class
    {
        try
        {
            var prompt = _promptBuilder
                .Reset()
                .WithTemplate(templateName, parameters)
                .WithOutputFormat<T>()
                .Build();

            return await ProcessStructuredTaskAsync<T>(prompt, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用模板处理结构化任务失败，模板: {TemplateName}", templateName);
            throw;
        }
    }

    /// <summary>
    /// 获取LLM客户端
    /// </summary>
    /// <returns>LLM客户端</returns>
    /// <exception cref="InvalidOperationException">无法创建LLM客户端时抛出</exception>
    private async Task<Clients.ILLMClient> GetClientAsync()
    {
        try
        {
            var client = await _llmClientFactory.CreateClientAsync();
            if (client == null)
            {
                _logger.LogError("无法创建LLM客户端，请检查LLM设置是否正确");
                throw new InvalidOperationException("无法创建LLM客户端，请检查LLM设置是否正确");
            }
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM客户端时发生错误: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("获取LLM客户端时发生错误", ex);
        }
    }
}

/// <summary>
/// 结构化任务选项
/// </summary>
public class StructuredTaskOptions
{
    /// <summary>
    /// 是否启用重试
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// 重试延迟
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// 结构化任务结果
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class StructuredTaskResult<T> where T : class
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 结果对象
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 原始AI响应
    /// </summary>
    public string RawResponse { get; set; } = string.Empty;

    /// <summary>
    /// 清理后的JSON
    /// </summary>
    public string CleanedJson { get; set; } = string.Empty;

    /// <summary>
    /// 是否经过修复
    /// </summary>
    public bool WasRepaired { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 处理时长
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="result">结果对象</param>
    /// <param name="rawResponse">原始响应</param>
    /// <param name="cleanedJson">清理后的JSON</param>
    /// <param name="wasRepaired">是否经过修复</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>成功结果</returns>
    public static StructuredTaskResult<T> Success(
        T result, 
        string rawResponse, 
        string cleanedJson, 
        bool wasRepaired,
        DateTime startTime,
        DateTime endTime)
    {
        return new StructuredTaskResult<T>
        {
            IsSuccess = true,
            Result = result,
            RawResponse = rawResponse,
            CleanedJson = cleanedJson,
            WasRepaired = wasRepaired,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errors">错误信息列表</param>
    /// <param name="rawResponse">原始响应</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>失败结果</returns>
    public static StructuredTaskResult<T> Failure(
        List<string> errors, 
        string rawResponse,
        DateTime startTime,
        DateTime endTime)
    {
        return new StructuredTaskResult<T>
        {
            IsSuccess = false,
            Errors = errors,
            RawResponse = rawResponse,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}
