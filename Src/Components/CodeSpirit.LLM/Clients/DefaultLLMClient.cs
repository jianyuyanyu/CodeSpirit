using CodeSpirit.LLM.Settings;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CodeSpirit.LLM.Clients;

/// <summary>
/// 默认LLM客户端实现
/// </summary>
public class DefaultLLMClient : ILLMClient
{
    private readonly ILogger<DefaultLLMClient> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 获取LLM设置
    /// </summary>
    public LLMSettings Settings { get; }

    /// <summary>
    /// 初始化默认LLM客户端
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="settings">LLM设置</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    public DefaultLLMClient(
        ILogger<DefaultLLMClient> logger,
        LLMSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        Settings = settings;
        _httpClient = CreateHttpClient(settings, httpClientFactory);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        return await GenerateContentAsync(prompt, Settings.MaxTokens);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateContentAsync(string prompt, int maxTokens)
    {
        return await GenerateContentAsync(
            "你是一个专业的AI助手，请根据用户的提示提供专业的回答。",
            prompt,
            maxTokens);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
    {
        return await GenerateContentAsync(systemPrompt, userPrompt, Settings.MaxTokens);
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    /// <param name="systemPrompt">系统提示词</param>
    /// <param name="userPrompt">用户提示词</param>
    /// <param name="maxTokens">最大令牌数</param>
    /// <returns>生成的内容</returns>
    public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt, int maxTokens)
    {
        _logger.LogDebug("开始生成内容");

        try
        {
            // 构建请求URL
            string requestUrl = DetermineRequestUrl(Settings.ApiBaseUrl, Settings.ModelName);

            // 构建请求体
            var requestBody = BuildRequestBody(Settings.ModelName, systemPrompt, userPrompt, maxTokens);

            _logger.LogDebug("请求体: {RequestBody}", JsonSerializer.Serialize(requestBody));

            // 发送请求
            var httpContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("发送请求到大模型API: {Url}", requestUrl);
            
            // 使用流式处理
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = httpContent
            };
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            // 确保请求成功
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("API响应状态码: {StatusCode}", response.StatusCode);
            
            // 读取流式响应
            var streamResult = new StringBuilder();
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
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
                        string content = ExtractContentFromStreamChunk(chunk, Settings.ModelName);
                        if (!string.IsNullOrEmpty(content))
                        {
                            streamResult.Append(content);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "解析流式数据块失败: {Line}", line);
                        // 继续处理下一个数据块
                    }
                }
            }
            
            string generatedContent = streamResult.ToString();
            
            if (string.IsNullOrEmpty(generatedContent))
            {
                _logger.LogWarning("提取的生成内容为空");
                throw new InvalidOperationException("提取的生成内容为空");
            }

            return generatedContent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP请求失败: {Message}", ex.Message);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON格式错误: {Message}", ex.Message);
            throw new FormatException("解析生成内容时出现JSON格式错误", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成内容时发生错误: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 从流式响应块中提取内容
    /// </summary>
    private string ExtractContentFromStreamChunk(JsonElement chunk, string modelName)
    {
        try
        {
            // 标准OpenAI流式响应格式
            if (chunk.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta) && 
                    delta.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "";
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
        catch (Exception)
        {
            // 记录错误但不抛出，以保持流式处理的连续性
            return "";
        }
    }
    
    /// <summary>
    /// 创建HTTP客户端
    /// </summary>
    private HttpClient CreateHttpClient(LLMSettings settings, IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient("LLMClient");
        
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
                _logger.LogError(ex, "配置代理失败: {ProxyAddress}, {ErrorMessage}", 
                    settings.ProxyAddress, ex.Message);
            }
        }
        
        return client;
    }
    
    /// <summary>
    /// 确定请求URL
    /// </summary>
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
    private object BuildRequestBody(string modelName, string systemPrompt, string userPrompt, int maxTokens)
    {
        // OpenAI格式
        if (modelName.Contains("gpt") || Settings.ApiBaseUrl.Contains("openai.com"))
        {
            return new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens,
                temperature = 0.7,
                stream = true
            };
        }
        
        // 阿里云格式
        if (modelName.StartsWith("qwen") || Settings.ApiBaseUrl.Contains("dashscope"))
        {
            return new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens,
                temperature = 0.7,
                stream = true
            };
        }
        
        // 默认格式
        return new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxTokens,
            temperature = 0.7,
            stream = true
        };
    }
}
