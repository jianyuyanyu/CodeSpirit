
using CodeSpirit.Audit.Models.LLM;
using CodeSpirit.Audit.Services.LLM;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Security.Claims;

namespace CodeSpirit.AiFormFill.Services;

/// <summary>
/// AI表单填充服务实现
/// </summary>
public class AiFormFillService : IAiFormFillService, IScopedDependency
{
    private readonly LLMAssistant _llmAssistant;
    private readonly AiFormPromptBuilder _promptBuilder;
    private readonly AiFormResponseParser _responseParser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiFormFillService> _logger;
    private readonly AiFormFillLLMClientFactory _aiFormFillLLMClientFactory;
    private readonly ILLMAuditService? _auditService;
    private readonly ITenantContext? _tenantContext;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="llmAssistant">LLM助手</param>
    /// <param name="promptBuilder">提示词构建器</param>
    /// <param name="responseParser">响应解析器</param>
    /// <param name="cache">内存缓存</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="aiFormFillLLMClientFactory">AI表单填充LLM客户端工厂</param>
    /// <param name="auditService">LLM审计服务（可选）</param>
    /// <param name="tenantContext">租户上下文（可选）</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器（可选）</param>
    public AiFormFillService(
        LLMAssistant llmAssistant,
        AiFormPromptBuilder promptBuilder,
        AiFormResponseParser responseParser,
        IMemoryCache cache,
        ILogger<AiFormFillService> logger,
        AiFormFillLLMClientFactory aiFormFillLLMClientFactory,
        ILLMAuditService? auditService = null,
        ITenantContext? tenantContext = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _llmAssistant = llmAssistant;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _cache = cache;
        _logger = logger;
        _aiFormFillLLMClientFactory = aiFormFillLLMClientFactory;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 填充表单字段
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发值</param>
    /// <param name="existingData">现有数据</param>
    /// <returns>填充后的数据</returns>
    public async Task<T> FillFormAsync<T>(string triggerValue, T? existingData = null) where T : class, new()
    {
        try
        {
            var dtoType = typeof(T);
            var aiFormFillAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
            
            if (aiFormFillAttr == null)
            {
                throw new InvalidOperationException($"类型 {dtoType.Name} 未标记 AiFormFillAttribute 特性");
            }

            _logger.LogInformation("开始AI表单填充，类型：{Type}，触发值：{TriggerValue}", dtoType.Name, triggerValue);

            // 检查缓存
            if (aiFormFillAttr.EnableCache)
            {
                var cacheKey = GenerateCacheKey<T>(triggerValue);
                if (_cache.TryGetValue(cacheKey, out T? cachedResult))
                {
                    _logger.LogInformation("从缓存获取AI填充结果：{Type}", dtoType.Name);
                    return cachedResult!;
                }
            }

            // 构建提示词
            var prompt = _promptBuilder.BuildPrompt<T>(triggerValue, aiFormFillAttr.CustomPromptTemplate, existingData);
            
            _logger.LogDebug("AI填充提示词：{Prompt}", prompt);

            // 调用LLM（根据配置选择使用独立LLM还是全局LLM）
            string llmResponse;
            if (aiFormFillAttr.UseIndependentLLM)
            {
                _logger.LogInformation("使用独立AI表单填充LLM配置，设置键：{SettingsKey}", aiFormFillAttr.LLMSettingsKey);
                
                var aiFormFillClient = await _aiFormFillLLMClientFactory.CreateClientAsync(aiFormFillAttr.LLMSettingsKey);
                if (aiFormFillClient == null)
                {
                    throw new InvalidOperationException($"无法创建AI表单填充LLM客户端，设置键：{aiFormFillAttr.LLMSettingsKey}");
                }

                // 独立LLM客户端内部已经处理审计
                llmResponse = await aiFormFillClient.GenerateContentAsync(
                    prompt,
                    aiFormFillAttr.MaxTokens > 0 ? aiFormFillAttr.MaxTokens : null,
                    aiFormFillAttr.DisableThinking,
                    aiFormFillAttr.ResponseFormatType,
                    aiFormFillAttr.Temperature,
                    aiFormFillAttr.TopP);
            }
            else
            {
                _logger.LogInformation("使用全局LLM配置");
                
                // 使用全局LLM时，需要手动记录审计
                llmResponse = await GenerateContentWithAuditAsync(
                    prompt, 
                    aiFormFillAttr.MaxTokens,
                    dtoType.Name,
                    aiFormFillAttr.TriggerField,
                    triggerValue,
                    aiFormFillAttr.EnableCache);
            }
            
            _logger.LogInformation("AI表单填充LLM响应内容：{Response}", llmResponse);

            // 解析响应
            var result = await _responseParser.ParseResponseAsync<T>(llmResponse, existingData);
            
            _logger.LogInformation("AI表单填充解析后的结果：{Result}", System.Text.Json.JsonSerializer.Serialize(result));

            // 设置触发字段的值
            if (!aiFormFillAttr.IsGlobalMode)
            {
                var triggerProperty = dtoType.GetProperty(aiFormFillAttr.TriggerField);
                if (triggerProperty != null && triggerProperty.CanWrite)
                {
                    triggerProperty.SetValue(result, triggerValue);
                }
            }

            // 缓存结果
            if (aiFormFillAttr.EnableCache)
            {
                var cacheKey = GenerateCacheKey<T>(triggerValue);
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(aiFormFillAttr.CacheExpirationMinutes)
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            _logger.LogInformation("AI表单填充完成：{Type}", dtoType.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI表单填充失败，类型：{Type}，触发值：{TriggerValue}", typeof(T).Name, triggerValue);
            throw new BusinessException($"AI表单填充失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证DTO是否支持AI填充
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <returns>是否支持</returns>
    public bool IsAiFillSupported<T>() where T : class
    {
        return typeof(T).GetCustomAttribute<AiFormFillAttribute>() != null;
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <typeparam name="T">DTO类型</typeparam>
    /// <param name="triggerValue">触发值</param>
    /// <returns>缓存键</returns>
    private string GenerateCacheKey<T>(string triggerValue) where T : class
    {
        return $"AiFormFill:{typeof(T).Name}:{triggerValue.GetHashCode()}";
    }

    /// <summary>
    /// 使用全局LLM生成内容并记录审计
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="dtoTypeName">DTO类型名称</param>
    /// <param name="triggerField">触发字段</param>
    /// <param name="triggerValue">触发值</param>
    /// <param name="enableCache">是否启用缓存</param>
    /// <returns>LLM响应</returns>
    private async Task<string> GenerateContentWithAuditAsync(
        string prompt,
        int maxTokens,
        string dtoTypeName,
        string triggerField,
        string triggerValue,
        bool enableCache)
    {
        var stopwatch = Stopwatch.StartNew();
        string llmResponse = string.Empty;
        bool isSuccess = false;
        string errorMessage = string.Empty;

        try
        {
            llmResponse = await _llmAssistant.GenerateContentAsync(prompt, maxTokens);
            isSuccess = true;
            return llmResponse;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "使用全局LLM生成AI表单填充内容失败");
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // 记录审计日志
            await CreateAuditLogForGlobalLLMAsync(
                userPrompt: prompt,
                llmResponse: llmResponse,
                processingTimeMs: stopwatch.ElapsedMilliseconds,
                isSuccess: isSuccess,
                errorMessage: errorMessage,
                dtoTypeName: dtoTypeName,
                triggerField: triggerField,
                triggerValue: triggerValue,
                maxTokens: maxTokens,
                enableCache: enableCache);
        }
    }

    /// <summary>
    /// 为全局LLM创建审计日志
    /// </summary>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="llmResponse">LLM响应</param>
    /// <param name="processingTimeMs">处理时间（毫秒）</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="dtoTypeName">DTO类型名称</param>
    /// <param name="triggerField">触发字段</param>
    /// <param name="triggerValue">触发值</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="enableCache">是否启用缓存</param>
    private async Task CreateAuditLogForGlobalLLMAsync(
        string userPrompt,
        string llmResponse,
        long processingTimeMs,
        bool isSuccess,
        string errorMessage,
        string dtoTypeName,
        string triggerField,
        string triggerValue,
        int maxTokens,
        bool enableCache)
    {
        // 如果审计服务未注册，则跳过
        if (_auditService == null)
        {
            return;
        }

        try
        {
            var tenantId = _tenantContext?.TenantId ?? "default";
            var userId = string.Empty;
            var userName = string.Empty;

            // 从HttpContext获取用户信息
            if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var user = _httpContextAccessor.HttpContext.User;
                userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                userName = user.FindFirst(ClaimTypes.Name)?.Value
                          ?? user.FindFirst("name")?.Value
                          ?? string.Empty;
            }

            var auditLog = new LLMAuditLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                UserId = userId,
                UserName = userName,
                OperationTime = DateTime.UtcNow,
                LLMProvider = "Global",
                ModelName = "Global-LLM",
                InteractionType = "FormAutoFill",
                BusinessScenario = "AiFormFill",
                SystemPrompt = string.Empty,
                UserPrompt = userPrompt,
                LLMResponse = llmResponse,
                ProcessedData = string.Empty,
                ProcessingTimeMs = processingTimeMs,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                WasJsonRepaired = false,
                Metadata = new Dictionary<string, object>
                {
                    ["DtoType"] = dtoTypeName,
                    ["TriggerField"] = triggerField,
                    ["TriggerValue"] = triggerValue,
                    ["MaxTokens"] = maxTokens,
                    ["EnableCache"] = enableCache,
                    ["UseIndependentLLM"] = false
                }
            };

            // 估算Token使用
            auditLog.TokenUsage = new LLMTokenUsage
            {
                InputTokens = EstimateTokens(userPrompt),
                OutputTokens = EstimateTokens(llmResponse),
                TotalTokens = 0
            };
            auditLog.TokenUsage.TotalTokens = auditLog.TokenUsage.InputTokens + auditLog.TokenUsage.OutputTokens;

            _logger.LogDebug("记录AI表单填充（全局LLM）审计日志，审计ID: {AuditId}", auditLog.Id);
            await _auditService.LogLLMInteractionAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建AI表单填充（全局LLM）审计日志失败");
        }
    }

    /// <summary>
    /// 估算文本的Token数量
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <returns>估算的Token数量</returns>
    private int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // 简单估算：平均每2.5个字符为1个token
        return (int)Math.Ceiling(text.Length / 2.5);
    }
}
