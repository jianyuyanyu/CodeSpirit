using CodeSpirit.Audit.Models.LLM;
using CodeSpirit.Audit.Services.LLM;
using CodeSpirit.LLM;
using CodeSpirit.LLM.Factories;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace CodeSpirit.Audit.LLM;

/// <summary>
/// 可审计的LLM助手（装饰器模式）
/// </summary>
public class AuditableLLMAssistant
{
    private readonly LLMAssistant _innerAssistant;
    private readonly ILLMAuditService _auditService;
    private readonly ITenantContext? _tenantContext;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ILogger<AuditableLLMAssistant> _logger;
    private readonly ILLMClientFactory _llmClientFactory;
    
    // 审计上下文
    private string? _currentBusinessScenario;
    private string? _currentInteractionType;
    private string? _currentBatchId;
    private int? _currentBatchSequence;
    private string? _currentParentAuditId;
    private string? _currentBusinessEntityId;
    private string? _currentBusinessEntityType;
    private int? _currentDataCount;
    private Dictionary<string, object> _currentMetadata = new();
    
    /// <summary>
    /// 初始化可审计的LLM助手
    /// </summary>
    public AuditableLLMAssistant(
        LLMAssistant innerAssistant,
        ILLMAuditService auditService,
        ITenantContext? tenantContext,
        IHttpContextAccessor? httpContextAccessor,
        ILogger<AuditableLLMAssistant> logger,
        ILLMClientFactory llmClientFactory)
    {
        _innerAssistant = innerAssistant;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _llmClientFactory = llmClientFactory;
    }
    
    #region 审计上下文配置
    
    /// <summary>
    /// 设置业务场景
    /// </summary>
    public AuditableLLMAssistant WithBusinessScenario(string scenario)
    {
        _currentBusinessScenario = scenario;
        return this;
    }
    
    /// <summary>
    /// 设置交互类型
    /// </summary>
    public AuditableLLMAssistant WithInteractionType(string interactionType)
    {
        _currentInteractionType = interactionType;
        return this;
    }
    
    /// <summary>
    /// 设置批次信息
    /// </summary>
    public AuditableLLMAssistant WithBatch(string batchId, int? sequence = null)
    {
        _currentBatchId = batchId;
        _currentBatchSequence = sequence;
        return this;
    }
    
    /// <summary>
    /// 设置父审计ID（用于关联重试和修正操作）
    /// </summary>
    public AuditableLLMAssistant WithParentAuditId(string parentAuditId)
    {
        _currentParentAuditId = parentAuditId;
        return this;
    }
    
    /// <summary>
    /// 设置业务实体信息
    /// </summary>
    public AuditableLLMAssistant WithBusinessEntity(string entityType, string entityId, int? dataCount = null)
    {
        _currentBusinessEntityType = entityType;
        _currentBusinessEntityId = entityId;
        _currentDataCount = dataCount;
        return this;
    }
    
    /// <summary>
    /// 添加元数据
    /// </summary>
    public AuditableLLMAssistant WithMetadata(string key, object value)
    {
        _currentMetadata[key] = value;
        return this;
    }
    
    /// <summary>
    /// 清除审计上下文
    /// </summary>
    public AuditableLLMAssistant ResetAuditContext()
    {
        _currentBusinessScenario = null;
        _currentInteractionType = null;
        _currentBatchId = null;
        _currentBatchSequence = null;
        _currentParentAuditId = null;
        _currentBusinessEntityId = null;
        _currentBusinessEntityType = null;
        _currentDataCount = null;
        _currentMetadata = new Dictionary<string, object>();
        return this;
    }
    
    #endregion
    
    #region 审计包装方法
    
    /// <summary>
    /// 生成内容（带审计）
    /// </summary>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        _logger.LogInformation("AuditableLLMAssistant.GenerateContentAsync 被调用，提示词长度: {PromptLength}", prompt?.Length ?? 0);
        
        var stopwatch = Stopwatch.StartNew();
        string response = string.Empty;
        bool success = false;
        string errorMessage = string.Empty;
        
        try
        {
            response = await _innerAssistant.GenerateContentAsync(prompt);
            success = true;
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "生成内容失败");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await CreateAuditLogAsync(
                systemPrompt: string.Empty,
                userPrompt: prompt,
                llmResponse: response,
                processedData: string.Empty,
                processingTimeMs: stopwatch.ElapsedMilliseconds,
                isSuccess: success,
                errorMessage: errorMessage,
                wasJsonRepaired: false
            );
        }
    }
    
    /// <summary>
    /// 生成内容（带审计）
    /// </summary>
    public async Task<string> GenerateContentAsync(string prompt, int maxTokens)
    {
        var stopwatch = Stopwatch.StartNew();
        string response = string.Empty;
        bool success = false;
        string errorMessage = string.Empty;
        
        try
        {
            response = await _innerAssistant.GenerateContentAsync(prompt, maxTokens);
            success = true;
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "生成内容失败");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await CreateAuditLogAsync(
                systemPrompt: string.Empty,
                userPrompt: prompt,
                llmResponse: response,
                processedData: string.Empty,
                processingTimeMs: stopwatch.ElapsedMilliseconds,
                isSuccess: success,
                errorMessage: errorMessage,
                wasJsonRepaired: false
            );
        }
    }
    
    /// <summary>
    /// 生成内容（带审计）
    /// </summary>
    public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
    {
        var stopwatch = Stopwatch.StartNew();
        string response = string.Empty;
        bool success = false;
        string errorMessage = string.Empty;
        
        try
        {
            response = await _innerAssistant.GenerateContentAsync(systemPrompt, userPrompt);
            success = true;
            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "生成内容失败");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await CreateAuditLogAsync(
                systemPrompt: systemPrompt,
                userPrompt: userPrompt,
                llmResponse: response,
                processedData: string.Empty,
                processingTimeMs: stopwatch.ElapsedMilliseconds,
                isSuccess: success,
                errorMessage: errorMessage,
                wasJsonRepaired: false
            );
        }
    }
    
    /// <summary>
    /// 处理结构化任务（带审计）
    /// </summary>
    public async Task<StructuredTaskResult<T>> ProcessStructuredTaskAsync<T>(
        string prompt, 
        StructuredTaskOptions? options = null) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        StructuredTaskResult<T>? result = null;
        
        try
        {
            result = await _innerAssistant.ProcessStructuredTaskAsync<T>(prompt, options);
            return result;
        }
        finally
        {
            stopwatch.Stop();
            
            if (result != null)
            {
                await CreateAuditLogAsync(
                    systemPrompt: string.Empty,
                    userPrompt: prompt,
                    llmResponse: result.RawResponse,
                    processedData: result.CleanedJson,
                    processingTimeMs: stopwatch.ElapsedMilliseconds,
                    isSuccess: result.IsSuccess,
                    errorMessage: result.IsSuccess ? string.Empty : string.Join("; ", result.Errors),
                    wasJsonRepaired: result.WasRepaired
                );
            }
        }
    }
    
    /// <summary>
    /// 使用模板处理结构化任务（带审计）
    /// </summary>
    public async Task<StructuredTaskResult<T>> ProcessStructuredTaskWithTemplateAsync<T>(
        string templateName,
        Dictionary<string, object> parameters,
        StructuredTaskOptions? options = null) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        StructuredTaskResult<T>? result = null;
        
        try
        {
            result = await _innerAssistant.ProcessStructuredTaskWithTemplateAsync<T>(
                templateName, parameters, options);
            return result;
        }
        finally
        {
            stopwatch.Stop();
            
            if (result != null)
            {
                await CreateAuditLogAsync(
                    systemPrompt: $"Template: {templateName}",
                    userPrompt: System.Text.Json.JsonSerializer.Serialize(parameters),
                    llmResponse: result.RawResponse,
                    processedData: result.CleanedJson,
                    processingTimeMs: stopwatch.ElapsedMilliseconds,
                    isSuccess: result.IsSuccess,
                    errorMessage: result.IsSuccess ? string.Empty : string.Join("; ", result.Errors),
                    wasJsonRepaired: result.WasRepaired
                );
            }
        }
    }
    
    #endregion
    
    #region 私有辅助方法
    
    /// <summary>
    /// 创建审计日志
    /// </summary>
    private async Task CreateAuditLogAsync(
        string systemPrompt,
        string userPrompt,
        string llmResponse,
        string processedData,
        long processingTimeMs,
        bool isSuccess,
        string errorMessage,
        bool wasJsonRepaired)
    {
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
            
            // 获取LLM提供商和模型信息
            string llmProvider = "Unknown";
            string modelName = "Unknown";
            try
            {
                var client = await _llmClientFactory.CreateClientAsync();
                if (client?.Settings != null)
                {
                    modelName = client.Settings.ModelName ?? "Unknown";
                    
                    // 根据API URL推断提供商
                    llmProvider = InferProviderFromUrl(client.Settings.ApiBaseUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取LLM客户端信息失败");
            }
            
            var auditLog = new LLMAuditLog
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                UserId = userId,
                UserName = userName,
                OperationTime = DateTime.UtcNow,
                LLMProvider = llmProvider,
                ModelName = modelName,
                InteractionType = _currentInteractionType ?? "ContentGeneration",
                BusinessScenario = _currentBusinessScenario ?? "General",
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                LLMResponse = llmResponse,
                ProcessedData = processedData,
                ProcessingTimeMs = processingTimeMs,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                WasJsonRepaired = wasJsonRepaired,
                BatchId = _currentBatchId,
                BatchSequence = _currentBatchSequence,
                ParentAuditId = _currentParentAuditId,
                BusinessEntityId = _currentBusinessEntityId,
                BusinessEntityType = _currentBusinessEntityType,
                DataCount = _currentDataCount,
                Metadata = new Dictionary<string, object>(_currentMetadata)
            };
            
            // 估算Token使用（如果LLM响应中没有提供）
            // 注意：这是一个粗略估算，实际应从LLM API响应中提取
            auditLog.TokenUsage = new LLMTokenUsage
            {
                InputTokens = EstimateTokens(systemPrompt + userPrompt),
                OutputTokens = EstimateTokens(llmResponse),
                TotalTokens = 0
            };
            auditLog.TokenUsage.TotalTokens = auditLog.TokenUsage.InputTokens + auditLog.TokenUsage.OutputTokens;
            
            _logger.LogInformation("准备记录LLM审计日志，审计ID: {AuditId}, 业务场景: {BusinessScenario}", auditLog.Id, auditLog.BusinessScenario);
            await _auditService.LogLLMInteractionAsync(auditLog);
            _logger.LogInformation("LLM审计日志记录完成，审计ID: {AuditId}", auditLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建LLM审计日志失败");
        }
    }
    
    /// <summary>
    /// 根据API URL推断提供商
    /// </summary>
    /// <param name="apiUrl">API基础URL</param>
    /// <returns>提供商名称</returns>
    private string InferProviderFromUrl(string apiUrl)
    {
        if (string.IsNullOrEmpty(apiUrl))
        {
            return "Unknown";
        }
        
        apiUrl = apiUrl.ToLowerInvariant();
        
        if (apiUrl.Contains("openai.com") || apiUrl.Contains("api.openai"))
        {
            return "OpenAI";
        }
        else if (apiUrl.Contains("dashscope.aliyuncs.com") || apiUrl.Contains("qwen"))
        {
            return "Alibaba-Qwen";
        }
        else if (apiUrl.Contains("anthropic.com") || apiUrl.Contains("claude"))
        {
            return "Anthropic";
        }
        else if (apiUrl.Contains("gemini") || apiUrl.Contains("google"))
        {
            return "Google";
        }
        else if (apiUrl.Contains("huggingface"))
        {
            return "HuggingFace";
        }
        else if (apiUrl.Contains("localhost") || apiUrl.Contains("127.0.0.1"))
        {
            return "Local";
        }
        
        return "Custom";
    }
    
    /// <summary>
    /// 估算文本的Token数量
    /// 这是一个粗略估算，实际应从LLM API响应中提取
    /// 英文大约 1 token ≈ 4 字符
    /// 中文大约 1 token ≈ 1.5-2 字符
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
        // 这个估算适用于中英文混合文本
        return (int)Math.Ceiling(text.Length / 2.5);
    }
    
    #endregion
}

