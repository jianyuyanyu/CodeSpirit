using CodeSpirit.AiFormFill.Models;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonDocument = System.Text.Json.JsonDocument;
using JsonElement = System.Text.Json.JsonElement;
using JsonValueKind = System.Text.Json.JsonValueKind;
using JsonException = System.Text.Json.JsonException;

namespace CodeSpirit.AiFormFill.Services;

/// <summary>
/// AI表单填充专用LLM客户端
/// </summary>
public class AiFormFillLLMClient : IScopedDependency
{
    private readonly ILogger<AiFormFillLLMClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly AiFormFillLLMSettings _settings;

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    public AiFormFillLLMSettings Settings => _settings;

    /// <summary>
    /// 初始化AI表单填充专用LLM客户端
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="settings">LLM设置</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    public AiFormFillLLMClient(
        ILogger<AiFormFillLLMClient> logger,
        AiFormFillLLMSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings;
        _httpClient = CreateHttpClient(settings, httpClientFactory);
    }

    /// <summary>
    /// 生成内容（使用专门的AI表单填充配置）
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="disableThinking">是否禁用思考</param>
    /// <param name="responseFormatType">响应格式类型</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topP">Top-p参数</param>
    /// <returns>生成的内容</returns>
    public async Task<string> GenerateContentAsync(
        string prompt,
        int? maxTokens = null,
        bool? disableThinking = null,
        string? responseFormatType = null,
        double? temperature = null,
        double? topP = null)
    {
        return await GenerateContentAsync(
            "你是一个专业的AI助手，专门用于智能填充表单内容。请严格按照用户要求生成JSON格式的响应，确保内容准确、相关且符合业务场景。",
            prompt,
            maxTokens,
            disableThinking,
            responseFormatType,
            temperature,
            topP);
    }

    /// <summary>
    /// 生成内容（完整参数版本）
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="disableThinking">是否禁用思考</param>
    /// <param name="responseFormatType">响应格式类型</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topP">Top-p参数</param>
    /// <returns>生成的内容</returns>
    public async Task<string> GenerateContentAsync(
        string systemPrompt,
        string userPrompt,
        int? maxTokens = null,
        bool? disableThinking = null,
        string? responseFormatType = null,
        double? temperature = null,
        double? topP = null)
    {
        _logger.LogDebug("开始生成AI表单填充内容");

        try
        {
            // 使用传入参数或默认设置
            var actualMaxTokens = maxTokens ?? _settings.MaxTokens;
            var actualDisableThinking = disableThinking ?? _settings.DisableThinking;
            var actualResponseFormatType = responseFormatType ?? _settings.ResponseFormatType;
            var actualTemperature = temperature ?? _settings.Temperature;
            var actualTopP = topP ?? _settings.TopP;

            // 构建请求URL
            string requestUrl = DetermineRequestUrl(_settings.ApiBaseUrl, _settings.ModelName);

            // 构建请求体
            var requestBody = BuildRequestBody(
                _settings.ModelName,
                systemPrompt,
                userPrompt,
                actualMaxTokens,
                actualDisableThinking,
                actualResponseFormatType,
                actualTemperature,
                actualTopP);

            _logger.LogDebug("AI表单填充请求体: {RequestBody}", JsonSerializer.Serialize(requestBody));

            // 发送请求
            var httpContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"));

            _logger.LogInformation("发送AI表单填充请求到: {Url}", requestUrl);

            HttpResponseMessage response;
            if (_settings.EnableStreaming)
            {
                // 流式处理
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = httpContent
                };
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            else
            {
                // 非流式处理
                response = await _httpClient.PostAsync(requestUrl, httpContent);
            }

            _logger.LogInformation("AI表单填充API响应状态码: {StatusCode}", response.StatusCode);
            
            // 打印响应头信息
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var responseHeaders = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                _logger.LogDebug("AI表单填充API响应头: {ResponseHeaders}", responseHeaders);
            }

            // 如果响应不成功，先读取错误内容再抛出异常
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "";
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI表单填充API请求失败，状态码: {StatusCode}, 错误内容: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    
                    // 检查是否是"只支持流式模式"的错误（不区分大小写）
                    var errorContentLower = errorContent.ToLowerInvariant();
                    if (errorContentLower.Contains("only support stream mode") || 
                        errorContentLower.Contains("please enable the stream parameter") ||
                        errorContentLower.Contains("enable the stream parameter"))
                    {
                        _logger.LogWarning("检测到模型只支持流式模式，尝试启用流式响应重新请求");
                        
                        // 如果当前不是流式模式，尝试启用流式模式重新请求
                        if (!_settings.EnableStreaming)
                        {
                            return await RetryWithStreamingEnabled(systemPrompt, userPrompt, actualMaxTokens, 
                                actualDisableThinking, actualResponseFormatType, actualTemperature, actualTopP);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取AI表单填充API错误响应内容失败，状态码: {StatusCode}", response.StatusCode);
                }
                
                // 抛出包含错误内容的异常
                var errorMessage = string.IsNullOrEmpty(errorContent) 
                    ? $"AI表单填充API请求失败，状态码: {response.StatusCode}" 
                    : $"AI表单填充API请求失败，状态码: {response.StatusCode}, 错误: {errorContent}";
                throw new HttpRequestException(errorMessage);
            }

            // 处理响应
            if (_settings.EnableStreaming)
            {
                return await ProcessStreamingResponse(response);
            }
            else
            {
                return await ProcessNonStreamingResponse(response);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI表单填充HTTP请求失败: {Message}", ex.Message);
            
            // 尝试从异常消息中提取更多信息
            if (ex.Data.Count > 0)
            {
                _logger.LogError("AI表单填充HTTP请求异常详细信息: {ExceptionData}", 
                    string.Join(", ", ex.Data.Cast<System.Collections.DictionaryEntry>().Select(kvp => $"{kvp.Key}: {kvp.Value}")));
            }
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "AI表单填充JSON格式错误: {Message}", ex.Message);
            throw new FormatException("解析AI表单填充内容时出现JSON格式错误", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "AI表单填充请求超时: {Message}", ex.Message);
            throw new TimeoutException("AI表单填充请求超时", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI表单填充生成内容时发生未知错误: {Message}, 异常类型: {ExceptionType}", 
                ex.Message, ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// 处理流式响应
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <returns>生成的内容</returns>
    private async Task<string> ProcessStreamingResponse(HttpResponseMessage response)
    {
        // 对于流式响应，即使状态码不成功，也要尝试读取流内容以获取错误信息
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AI表单填充流式响应状态码不成功: {StatusCode}，尝试读取错误流内容", response.StatusCode);
        }
        
        var streamResult = new StringBuilder();
        var allStreamData = new StringBuilder(); // 用于记录完整的流式数据
        
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // 记录原始流式数据
                allStreamData.AppendLine(line);
                
                if (string.IsNullOrEmpty(line) || line == "data: [DONE]")
                {
                    continue;
                }

                if (line.StartsWith("data: "))
                {
                    line = line.Substring("data: ".Length);
                }

                try
                {
                    JsonElement chunk = JsonDocument.Parse(line).RootElement;
                    string content = ExtractContentFromStreamChunk(chunk, _settings.ModelName);
                    if (!string.IsNullOrEmpty(content))
                    {
                        streamResult.Append(content);
                        _logger.LogDebug("AI表单填充流式内容片段: {Content}", content);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "解析AI表单填充流式数据块失败: {Line}", line);
                    // 继续处理下一个数据块
                }
            }
        }
        
        // 打印完整的流式响应数据（仅在Debug级别）
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("AI表单填充完整流式响应数据:\n{StreamData}", allStreamData.ToString());
        }

        string generatedContent = streamResult.ToString();
        
        // 打印最终生成的内容
        _logger.LogInformation("AI表单填充流式响应最终生成内容: {GeneratedContent}", generatedContent);

        // 如果没有生成内容，检查是否有错误信息
        if (string.IsNullOrEmpty(generatedContent))
        {
            var allStreamContent = allStreamData.ToString();
            if (!string.IsNullOrEmpty(allStreamContent))
            {
                // 检查流式内容中是否包含错误信息
                if (allStreamContent.Contains("\"error\"") || allStreamContent.Contains("\"type\":\"error\""))
                {
                    _logger.LogError("AI表单填充流式响应包含错误信息，状态码: {StatusCode}, 完整流式内容: {StreamContent}", 
                        response.StatusCode, allStreamContent);
                    throw new HttpRequestException($"AI表单填充流式响应失败，状态码: {response.StatusCode}, 内容: {allStreamContent}");
                }
            }
        }

        if (string.IsNullOrEmpty(generatedContent))
        {
            _logger.LogWarning("AI表单填充提取的生成内容为空");
            throw new InvalidOperationException("AI表单填充提取的生成内容为空");
        }

        return generatedContent;
    }

    /// <summary>
    /// 处理非流式响应
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <returns>生成的内容</returns>
    private async Task<string> ProcessNonStreamingResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // 打印完整的响应内容
        _logger.LogInformation("AI表单填充API完整响应内容: {ResponseContent}", responseContent);

        var jsonResponse = JsonDocument.Parse(responseContent);
        var root = jsonResponse.RootElement;

        // 提取内容
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                var generatedContent = content.GetString();
                
                // 打印提取的生成内容
                _logger.LogInformation("AI表单填充非流式响应提取的生成内容: {GeneratedContent}", generatedContent);
                
                if (string.IsNullOrEmpty(generatedContent))
                {
                    _logger.LogWarning("AI表单填充生成内容为空");
                    throw new InvalidOperationException("AI表单填充生成内容为空");
                }
                return generatedContent;
            }
        }

        // 打印响应结构以便调试
        _logger.LogWarning("AI表单填充响应结构不符合预期，完整响应: {ResponseContent}", responseContent);
        throw new InvalidOperationException("无法从AI表单填充响应中提取内容");
    }

    /// <summary>
    /// 从流式响应块中提取内容
    /// </summary>
    /// <param name="chunk">JSON响应块</param>
    /// <param name="modelName">模型名称</param>
    /// <returns>提取的内容</returns>
    private string ExtractContentFromStreamChunk(JsonElement chunk, string modelName)
    {
        try
        {
            // 标准OpenAI流式响应格式
            if (chunk.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta))
                {
                    // 优先检查 content 字段（但只有在不为 null 时才返回）
                    if (delta.TryGetProperty("content", out var content) && 
                        content.ValueKind != JsonValueKind.Null)
                    {
                        return content.GetString() ?? "";
                    }
                    
                    // 检查 reasoning_content 字段（qwq-plus 等推理模型使用）
                    if (delta.TryGetProperty("reasoning_content", out var reasoningContent) &&
                        reasoningContent.ValueKind != JsonValueKind.Null)
                    {
                        var extractedContent = reasoningContent.GetString() ?? "";
                        if (!string.IsNullOrEmpty(extractedContent))
                        {
                            _logger.LogDebug("AI表单填充提取到推理内容: {Content}", extractedContent);
                        }
                        return extractedContent;
                    }
                }

                // 兼容阿里云灵积API的格式（dashscope compatible mode）
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var msgContent))
                {
                    return msgContent.GetString() ?? "";
                }
            }

            // 尝试其他可能的路径
            foreach (var propName in new[] { "content", "text", "delta" })
            {
                if (chunk.TryGetProperty(propName, out var content) &&
                    content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? "";
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "提取AI表单填充流式内容时出错");
            // 记录错误但不抛出，以保持流式处理的连续性
            return "";
        }
    }

    /// <summary>
    /// 创建HTTP客户端
    /// </summary>
    /// <param name="settings">LLM设置</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <returns>HTTP客户端</returns>
    private HttpClient CreateHttpClient(AiFormFillLLMSettings settings, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("AiFormFillLLMClient");

        // 设置超时
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

        // 配置请求头
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            if (settings.ApiBaseUrl.Contains("openai.com"))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            }
            else if (settings.ApiBaseUrl.Contains("dashscope"))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
            }
            else
            {
                client.DefaultRequestHeaders.Add("api-key", settings.ApiKey);
            }
        }

        // 配置代理
        if (settings.UseProxy && !string.IsNullOrEmpty(settings.ProxyAddress))
        {
            try
            {
                var webProxy = new WebProxy(settings.ProxyAddress)
                {
                    UseDefaultCredentials = true
                };

                var handler = new HttpClientHandler
                {
                    Proxy = webProxy,
                    UseProxy = true
                };

                client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

                if (!string.IsNullOrEmpty(settings.ApiKey))
                {
                    if (settings.ApiBaseUrl.Contains("openai.com"))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                    }
                    else if (settings.ApiBaseUrl.Contains("dashscope"))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
                    }
                    else
                    {
                        client.DefaultRequestHeaders.Add("api-key", settings.ApiKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置AI表单填充代理失败: {ProxyAddress}, {ErrorMessage}",
                    settings.ProxyAddress, ex.Message);
            }
        }

        return client;
    }

    /// <summary>
    /// 确定请求URL
    /// </summary>
    /// <param name="apiBaseUrl">API基础地址</param>
    /// <param name="modelName">模型名称</param>
    /// <returns>请求URL</returns>
    private string DetermineRequestUrl(string apiBaseUrl, string modelName)
    {
        if (apiBaseUrl.EndsWith("/"))
        {
            apiBaseUrl = apiBaseUrl.TrimEnd('/');
        }

        // 对于OpenAI和阿里云灵积API，都需要添加/chat/completions路径
        if (apiBaseUrl.Contains("openai.com") || apiBaseUrl.Contains("dashscope"))
        {
            return $"{apiBaseUrl}/chat/completions";
        }

        return apiBaseUrl;
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    /// <param name="modelName">模型名称</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="disableThinking">是否禁用思考</param>
    /// <param name="responseFormatType">响应格式类型</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topP">Top-p参数</param>
    /// <returns>请求体对象</returns>
    private object BuildRequestBody(
        string modelName,
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        bool disableThinking,
        string responseFormatType,
        double temperature,
        double topP)
    {
        // 基础请求体
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = modelName,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature,
            ["top_p"] = topP,
            ["stream"] = _settings.EnableStreaming
        };

        // 添加响应格式（如果指定）
        if (!string.IsNullOrEmpty(responseFormatType))
        {
            requestBody["response_format"] = new { type = responseFormatType };
        }

        // 添加禁用思考参数（针对支持的模型）
        if (disableThinking)
        {
            requestBody["enable_thinking"] = false;
        }

        return requestBody;
    }

    /// <summary>
    /// 启用流式模式重新请求
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="disableThinking">是否禁用思考</param>
    /// <param name="responseFormatType">响应格式类型</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topP">Top-p参数</param>
    /// <returns>生成的内容</returns>
    private async Task<string> RetryWithStreamingEnabled(
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        bool disableThinking,
        string responseFormatType,
        double temperature,
        double topP)
    {
        _logger.LogInformation("使用流式模式重新发送AI表单填充请求");

        try
        {
            // 构建请求URL
            string requestUrl = DetermineRequestUrl(_settings.ApiBaseUrl, _settings.ModelName);

            // 构建启用流式的请求体
            var requestBody = BuildRequestBody(
                _settings.ModelName,
                systemPrompt,
                userPrompt,
                maxTokens,
                disableThinking,
                responseFormatType,
                temperature,
                topP,
                true); // 强制启用流式

            _logger.LogDebug("AI表单填充流式重试请求体: {RequestBody}", JsonSerializer.Serialize(requestBody));

            // 发送流式请求
            var httpContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                new MediaTypeHeaderValue("application/json"));

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = httpContent
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            _logger.LogInformation("AI表单填充流式重试响应状态码: {StatusCode}", response.StatusCode);

            // 如果仍然失败，读取错误内容并抛出异常
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = "";
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI表单填充流式重试仍然失败，状态码: {StatusCode}, 错误内容: {ErrorContent}",
                        response.StatusCode, errorContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取AI表单填充流式重试错误响应失败，状态码: {StatusCode}", response.StatusCode);
                }

                var errorMessage = string.IsNullOrEmpty(errorContent)
                    ? $"AI表单填充流式重试失败，状态码: {response.StatusCode}"
                    : $"AI表单填充流式重试失败，状态码: {response.StatusCode}, 错误: {errorContent}";
                throw new HttpRequestException(errorMessage);
            }

            // 处理流式响应
            return await ProcessStreamingResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI表单填充流式重试过程中发生错误: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 构建请求体（重载版本，支持强制流式）
    /// </summary>
    /// <param name="modelName">模型名称</param>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <param name="disableThinking">是否禁用思考</param>
    /// <param name="responseFormatType">响应格式类型</param>
    /// <param name="temperature">温度参数</param>
    /// <param name="topP">Top-p参数</param>
    /// <param name="forceStreaming">是否强制启用流式</param>
    /// <returns>请求体对象</returns>
    private object BuildRequestBody(
        string modelName,
        string systemPrompt,
        string userPrompt,
        int maxTokens,
        bool disableThinking,
        string responseFormatType,
        double temperature,
        double topP,
        bool forceStreaming)
    {
        // 基础请求体
        var requestBody = new Dictionary<string, object>
        {
            ["model"] = modelName,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature,
            ["top_p"] = topP,
            ["stream"] = forceStreaming || _settings.EnableStreaming
        };

        // 添加响应格式（如果指定）
        if (!string.IsNullOrEmpty(responseFormatType))
        {
            requestBody["response_format"] = new { type = responseFormatType };
        }

        // 添加禁用思考参数（针对支持的模型）
        if (disableThinking)
        {
            requestBody["enable_thinking"] = false;
        }

        return requestBody;
    }
}
