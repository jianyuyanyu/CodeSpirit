using CodeSpirit.LLM.Caching;
using CodeSpirit.LLM.Models;
using CodeSpirit.LLM.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Plugins.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSpirit.LLM.Services.Implementations
{
    /// <summary>
    /// 基于Semantic Kernel的LLM服务实现
    /// </summary>
    public class SemanticKernelLLMService : ILLMService
    {
        private readonly Kernel _kernel;
        private readonly LLMOptions _options;
        private readonly ILogger<SemanticKernelLLMService> _logger;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly ILLMCacheService _cacheService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public SemanticKernelLLMService(
            Kernel kernel, 
            IOptions<LLMOptions> options,
            ILogger<SemanticKernelLLMService> logger,
            ILLMCacheService cacheService = null)
        {
            _kernel = kernel;
            _options = options.Value;
            _logger = logger;
            _cacheService = cacheService;
            
            // 获取聊天完成服务
            _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            
            // 获取嵌入服务（如果可用）
            try
            {
                _embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("无法初始化嵌入服务：{Message}", ex.Message);
            }
        }
        
        /// <inheritdoc/>
        public async Task<ChatResponse> ChatAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null)
        {
            try
            {
                // 应用默认选项
                options ??= new ChatOptions
                {
                    ModelId = _options.DefaultModel,
                    Temperature = _options.DefaultTemperature,
                    MaxTokens = _options.DefaultMaxTokens,
                    SystemPrompt = _options.DefaultSystemPrompt
                };
                
                // 检查缓存
                if (_cacheService != null)
                {
                    string cacheKey = _cacheService.GenerateKey(messages, options.ModelId, options.Temperature);
                    if (_cacheService.TryGetValue<ChatResponse>(cacheKey, out var cachedResponse))
                    {
                        _logger.LogDebug("从缓存中获取LLM响应");
                        return cachedResponse;
                    }
                }
                
                // 创建聊天历史
                var chatHistory = new ChatHistory();
                
                // 添加系统消息（如果有）
                if (!string.IsNullOrEmpty(options.SystemPrompt))
                {
                    chatHistory.AddSystemMessage(options.SystemPrompt);
                }
                
                // 添加消息历史
                foreach (var message in messages)
                {
                    switch (message.Role)
                    {
                        case ChatRole.User:
                            chatHistory.AddUserMessage(message.Content);
                            break;
                        case ChatRole.Assistant:
                            chatHistory.AddAssistantMessage(message.Content);
                            break;
                        case ChatRole.System:
                            chatHistory.AddSystemMessage(message.Content);
                            break;
                        case ChatRole.Tool:
                            // Tool messages aren't directly supported in this simple implementation
                            _logger.LogWarning("工具消息类型暂不支持，已跳过");
                            break;
                    }
                }
                
                // 设置生成选项
                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = options.MaxTokens,
                    Temperature = options.Temperature,
                    ModelId = options.ModelId ?? _options.DefaultModel
                };
                
                // 发送请求
                var response = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory, 
                    executionSettings,
                    _kernel);
                
                var chatResponse = new ChatResponse
                {
                    Content = response.Content,
                    Model = options.ModelId ?? _options.DefaultModel,
                    // TokensUsed is not directly available from Semantic Kernel API
                    TokensUsed = null
                };
                
                // 缓存响应
                if (_cacheService != null)
                {
                    string cacheKey = _cacheService.GenerateKey(messages, options.ModelId, options.Temperature);
                    _cacheService.SetValue(cacheKey, chatResponse);
                }
                
                return chatResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "聊天对话请求失败: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc/>
        public async Task<string> AskAsync(string userMessage, ChatOptions options = null)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Role = ChatRole.User, Content = userMessage }
            };
            
            var response = await ChatAsync(messages, options);
            return response.Content;
        }
        
        /// <inheritdoc/>
        public async Task<string> ExecutePromptTemplateAsync(string promptTemplate, Dictionary<string, string> parameters, ChatOptions options = null)
        {
            try
            {
                // 应用默认选项
                options ??= new ChatOptions
                {
                    ModelId = _options.DefaultModel,
                    Temperature = _options.DefaultTemperature,
                    MaxTokens = _options.DefaultMaxTokens
                };
                
                // 创建函数
                var function = _kernel.CreateFunctionFromPrompt(
                    promptTemplate,
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = options.MaxTokens,
                        Temperature = options.Temperature,
                        ModelId = options.ModelId ?? _options.DefaultModel
                    });
                
                // 执行函数
                var arguments = new KernelArguments();
                foreach (var parameter in parameters)
                {
                    arguments[parameter.Key] = parameter.Value;
                }
                
                var result = await _kernel.InvokeAsync(function, arguments);
                return result.GetValue<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行提示模板失败: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc/>
        public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text)
        {
            if (_embeddingService == null)
            {
                throw new InvalidOperationException("嵌入服务未初始化");
            }
            
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
                // 返回ReadOnlyMemory<float>转换为IReadOnlyList<float>
                return embedding.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成嵌入向量失败: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc/>
        public Kernel GetKernel()
        {
            return _kernel;
        }
    }
} 