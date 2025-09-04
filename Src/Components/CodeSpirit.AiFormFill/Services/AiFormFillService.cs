
using CodeSpirit.LLM;
using Microsoft.Extensions.Caching.Memory;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="llmAssistant">LLM助手</param>
    /// <param name="promptBuilder">提示词构建器</param>
    /// <param name="responseParser">响应解析器</param>
    /// <param name="cache">内存缓存</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="aiFormFillLLMClientFactory">AI表单填充LLM客户端工厂</param>
    public AiFormFillService(
        LLMAssistant llmAssistant,
        AiFormPromptBuilder promptBuilder,
        AiFormResponseParser responseParser,
        IMemoryCache cache,
        ILogger<AiFormFillService> logger,
        AiFormFillLLMClientFactory aiFormFillLLMClientFactory)
    {
        _llmAssistant = llmAssistant;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _cache = cache;
        _logger = logger;
        _aiFormFillLLMClientFactory = aiFormFillLLMClientFactory;
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
            var prompt = _promptBuilder.BuildPrompt<T>(triggerValue, aiFormFillAttr.CustomPromptTemplate);
            
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
                llmResponse = await _llmAssistant.GenerateContentAsync(prompt, aiFormFillAttr.MaxTokens);
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
}
