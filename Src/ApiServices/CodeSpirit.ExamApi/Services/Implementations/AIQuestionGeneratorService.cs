using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Helpers;
using CodeSpirit.LLM;
using CodeSpirit.LLM.Factories;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Audit.LLM;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// AI题目生成服务实现
/// </summary>
public class AIQuestionGeneratorService : IAIQuestionGeneratorService
{
    private readonly ILogger<AIQuestionGeneratorService> _logger;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IQuestionParser _questionParser;
    private readonly AuditableLLMAssistant _auditableLLM;

    /// <summary>
    /// 初始化AI题目生成服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="promptBuilder">提示词构建器</param>
    /// <param name="questionParser">题目解析器</param>
    /// <param name="auditableLLM">可审计的LLM助手</param>
    public AIQuestionGeneratorService(
        ILogger<AIQuestionGeneratorService> logger,
        IPromptBuilder promptBuilder,
        IQuestionParser questionParser,
        AuditableLLMAssistant auditableLLM)
    {
        _logger = logger;
        _promptBuilder = promptBuilder;
        _questionParser = questionParser;
        _auditableLLM = auditableLLM;
    }

    /// <inheritdoc/>
    public async Task<List<CreateQuestionDto>> GenerateQuestionsAsync(AIGenerateQuestionDto request, string? sessionId = null, IGeneratorNotificationService? notificationService = null)
    {
        _logger.LogInformation("开始生成题目: 主题={Topic}, 数量={Count}, 类型={Type}, 难度={Difficulty}", 
            request.Topic, request.Count, request.Type, request.Difficulty);

        // 如果提供了通知服务和会话ID，则发送构建提示词开始通知
        if (notificationService != null && !string.IsNullOrEmpty(sessionId))
        {
            await notificationService.NotifyGenerationProgressAsync(sessionId, "preparing", "正在构建提示词...", 20);
        }

        // 生成批次ID用于关联审计记录
        var batchId = Guid.NewGuid().ToString();
        
        // 重试策略
        int maxRetries = 3;
        int retryCount = 0;
        int backoffMs = 1000; // 初始重试延迟

        while (true)
        {
            try
            {
                // 构建提示词
                string prompt = _promptBuilder.BuildGenerationPrompt(request);
                _logger.LogTrace("生成的提示词: {Prompt}", prompt);
                
                // 如果提供了通知服务和会话ID，则发送提示词构建完成通知
                if (notificationService != null && !string.IsNullOrEmpty(sessionId))
                {
                    await notificationService.NotifyGenerationProgressAsync(sessionId, "prompt_ready", "提示词构建完成，开始生成题目...", 30);
                }

                // 使用可审计的LLM助手生成内容，配置审计上下文
                var generatedContent = await _auditableLLM
                    .WithBusinessScenario("QuestionGeneration")
                    .WithInteractionType("Generation")
                    .WithBatch(batchId)
                    .WithBusinessEntity("Question", request.CategoryId.ToString(), request.Count)
                    .WithMetadata("topic", request.Topic)
                    .WithMetadata("difficulty", request.Difficulty.ToString())
                    .WithMetadata("type", request.Type.ToString())
                    .WithMetadata("sessionId", sessionId ?? "unknown")
                    .GenerateContentAsync(prompt);
                
                // 如果提供了通知服务和会话ID，则发送内容生成完成通知
                if (notificationService != null && !string.IsNullOrEmpty(sessionId))
                {
                    await notificationService.NotifyGenerationProgressAsync(sessionId, "content_generated", "内容生成完成，开始解析题目...", 60);
                }
                
                if (string.IsNullOrEmpty(generatedContent))
                {
                    _logger.LogWarning("提取的生成内容为空");
                    throw new InvalidOperationException("提取的生成内容为空");
                }
                
                _logger.LogInformation(generatedContent);
                // 解析生成的题目
                var questions = _questionParser.ParseQuestions(generatedContent, request);
                
                // 如果提供了通知服务和会话ID，则发送题目解析完成通知
                if (notificationService != null && !string.IsNullOrEmpty(sessionId))
                {
                    await notificationService.NotifyGenerationProgressAsync(sessionId, "parsing_completed", $"成功解析 {questions.Count} 道题目", 90);
                }
                
                if (questions.Count == 0)
                {
                    _logger.LogWarning("未能解析出任何题目");
                    throw new InvalidOperationException("未能解析出任何题目，请检查生成内容格式");
                }
                
                _logger.LogInformation("成功生成 {Count} 道题目", questions.Count);
                return questions;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "格式错误: {Message}", ex.Message);
                
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogWarning("检测到格式错误，尝试请求修正");
                    
                    try
                    {
                        // 请求LLM修正格式
                        string correctedContent = await RequestFormatCorrectionAsync(request, ex.Message, batchId);
                        
                        // 使用修正后的内容尝试重新解析
                        _logger.LogInformation("获取到修正内容，尝试重新解析");
                        var questions = _questionParser.ParseQuestions(correctedContent, request);
                        
                        if (questions.Count > 0)
                        {
                            _logger.LogInformation("成功从修正内容中解析出 {Count} 道题目", questions.Count);
                            return questions;
                        }
                        else
                        {
                            _logger.LogWarning("从修正内容中未能解析出任何题目，继续重试");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "尝试修正格式失败，将进行常规重试");
                    }
                    
                    await ApplyRetryDelay(retryCount, maxRetries, backoffMs);
                    backoffMs *= 2; // 指数退避
                    continue;
                }
                
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP请求失败: {Message}", ex.Message);
                
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    await ApplyRetryDelay(retryCount, maxRetries, backoffMs);
                    backoffMs *= 2; // 指数退避
                    continue;
                }
                
                throw new Exception($"生成题目失败，HTTP请求错误: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                // 记录错误
                _logger.LogError(ex, "操作无效: {Message}", ex.Message);
                
                // 检查是否与格式相关
                bool isFormatRelated = ex.Message.Contains("格式") || 
                                       ex.Message.Contains("解析") || 
                                       ex.Message.Contains("提取") || 
                                       ex.Message.Contains("空");
                
                if (isFormatRelated && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogWarning("检测到内容格式问题，尝试请求修正");
                    
                    try 
                    {
                        // 请求LLM修正格式
                        string correctedContent = await RequestFormatCorrectionAsync(request, ex.Message, batchId);
                        
                        // 使用修正后的内容尝试重新解析
                        _logger.LogInformation("获取到修正内容，尝试重新处理");
                        var questions = _questionParser.ParseQuestions(correctedContent, request);
                        
                        if (questions.Count > 0)
                        {
                            _logger.LogInformation("成功从修正内容中解析出 {Count} 道题目", questions.Count);
                            return questions;
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "尝试修正格式失败");
                    }
                    
                    await ApplyRetryDelay(retryCount, maxRetries, backoffMs);
                    backoffMs *= 2; // 指数退避
                    continue;
                }
                
                // 这些是我们自己抛出的异常，直接向上传递
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成题目时发生未预期错误: {Message}", ex.Message);
                
                // 判断是否可能是格式问题
                bool isLikelyFormatIssue = ex.Message.Contains("JSON") || 
                                           ex.Message.Contains("格式") || 
                                           ex.Message.Contains("解析") || 
                                           ex.InnerException is System.Text.Json.JsonException;
                
                if (isLikelyFormatIssue && retryCount < maxRetries)
                {
                    retryCount++;
                    _logger.LogWarning("检测到可能的格式问题，尝试请求修正");
                    
                    try
                    {
                        // 请求LLM修正格式
                        string correctedContent = await RequestFormatCorrectionAsync(request, ex.Message, batchId);
                        
                        // 使用修正后的内容尝试重新解析
                        _logger.LogInformation("获取到修正内容，尝试重新处理");
                        var questions = _questionParser.ParseQuestions(correctedContent, request);
                        
                        if (questions.Count > 0)
                        {
                            _logger.LogInformation("成功从修正内容中解析出 {Count} 道题目", questions.Count);
                            return questions;
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "尝试修正格式失败");
                    }
                }
                
                if (retryCount < maxRetries)
                {
                    retryCount++;
                    int backoffDelay = backoffMs * (int)Math.Pow(2, retryCount - 1); // 指数退避
                    _logger.LogWarning("生成题目时发生错误，{RetryCount}/{MaxRetries} 次重试，将在 {Delay}ms 后重试", 
                        retryCount, maxRetries, backoffDelay);
                    
                    // 发送重试通知
                    if (notificationService != null && !string.IsNullOrEmpty(sessionId))
                    {
                        await notificationService.NotifyGenerationProgressAsync(
                            sessionId, 
                            "retrying", 
                            $"生成题目时遇到问题，正在进行第 {retryCount}/{maxRetries} 次尝试...", 
                            20 + retryCount * 10);
                    }
                    
                    await Task.Delay(backoffDelay);
                    continue;
                }
                
                // 已达到最大重试次数
                _logger.LogError("生成题目失败，已达到最大重试次数 {MaxRetries}", maxRetries);
                
                // 如果提供了通知服务和会话ID，则发送生成失败通知
                if (notificationService != null && !string.IsNullOrEmpty(sessionId))
                {
                    await notificationService.NotifyGenerationErrorAsync(sessionId, "生成题目失败，请稍后重试");
                }
                
                throw;
            }
        }
    }

    /// <summary>
    /// 应用重试延迟
    /// </summary>
    private async Task ApplyRetryDelay(int retryCount, int maxRetries, int delayMs)
    {
        _logger.LogWarning("尝试第 {RetryCount}/{MaxRetries} 次重试，延迟 {Delay}ms", 
            retryCount, maxRetries, delayMs);
        await Task.Delay(delayMs);
    }

    /// <summary>
    /// 请求LLM修正格式问题
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="batchId">批次ID，用于关联审计记录</param>
    /// <returns>修正后的内容</returns>
    private async Task<string> RequestFormatCorrectionAsync(AIGenerateQuestionDto request, string? errorMessage = null, string? batchId = null)
    {
        _logger.LogInformation("请求LLM修正格式问题");
        
        try
        {
            // 构建修正提示词，包含详细的错误信息
            string prompt = _promptBuilder.BuildCorrectionPrompt(request, errorMessage);
            
            _logger.LogDebug("发送格式修正请求");
            
            // 使用可审计的LLM助手发送请求，配置审计上下文
            var correctionBuilder = _auditableLLM
                .WithBusinessScenario("QuestionGeneration")
                .WithInteractionType("Correction")
                .WithBusinessEntity("Question", request.CategoryId.ToString(), request.Count)
                .WithMetadata("topic", request.Topic)
                .WithMetadata("difficulty", request.Difficulty.ToString())
                .WithMetadata("type", request.Type.ToString())
                .WithMetadata("errorMessage", errorMessage ?? "unknown");
            
            // 如果有批次ID，则关联到批次
            if (!string.IsNullOrEmpty(batchId))
            {
                correctionBuilder = correctionBuilder.WithBatch(batchId);
            }
            
            string correctedContent = await correctionBuilder.GenerateContentAsync(prompt);
            
            _logger.LogInformation("成功获取修正后的内容");
            return correctedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "请求格式修正失败: {ErrorMessage}", ex.Message);
            throw new Exception("请求LLM修正格式失败", ex);
        }
    }
} 