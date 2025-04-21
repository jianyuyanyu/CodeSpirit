using CodeSpirit.ExamApi.Settings;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CodeSpirit.ExamApi.Services.LLM;

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
            "你是一个专业的考试题目生成助手，严格按照要求生成考试题目。",
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
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
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
            // OpenAI 流式响应格式
            if (chunk.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta) && 
                    delta.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "";
                }
            }
            
            // 阿里云灵积模型响应格式
            if (modelName.StartsWith("qwen") || modelName.Contains("tongyi"))
            {
                if (chunk.TryGetProperty("output", out var output))
                {
                    if (output.TryGetProperty("choices", out var qwenChoices) && 
                        qwenChoices.GetArrayLength() > 0)
                    {
                        var choice = qwenChoices[0];
                        if (choice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var content))
                        {
                            return content.GetString() ?? "";
                        }
                    }
                }
            }
            
            // 尝试其他可能的路径
            foreach (var propName in new[] { "content", "text", "delta" })
            {
                if (chunk.TryGetProperty(propName, out var prop))
                {
                    string value = prop.ValueKind == JsonValueKind.String 
                        ? prop.GetString() ?? "" 
                        : prop.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
            }
            
            return "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取流式数据块内容失败");
            return "";
        }
    }

    /// <summary>
    /// 创建HTTP客户端
    /// </summary>
    private HttpClient CreateHttpClient(LLMSettings settings, IHttpClientFactory httpClientFactory)
    {
        _logger.LogDebug("创建HTTP客户端");
        HttpClient client;

        try
        {
            if (settings.UseProxy && !string.IsNullOrEmpty(settings.ProxyAddress))
            {
                // 使用代理
                _logger.LogDebug("使用代理: {ProxyAddress}", settings.ProxyAddress);
                try
                {
                    var handler = new HttpClientHandler
                    {
                        Proxy = new WebProxy(settings.ProxyAddress),
                        UseProxy = true
                    };
                    client = new HttpClient(handler);
                    _logger.LogInformation("成功配置HTTP代理");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "配置HTTP代理时发生错误: {ErrorMessage}", ex.Message);
                    _logger.LogWarning("回退到不使用代理的客户端");
                    client = httpClientFactory.CreateClient();
                }
            }
            else
            {
                // 使用普通客户端
                _logger.LogDebug("使用默认HTTP客户端，不使用代理");
                client = httpClientFactory.CreateClient();
            }

            // 设置超时
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            _logger.LogDebug("HTTP客户端超时设置为 {Timeout} 秒", settings.TimeoutSeconds);

            // 设置认证头
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                string.IsNullOrEmpty(settings.ApiKey) ? string.Empty : settings.ApiKey);

            // 添加请求头
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "CodeSpirit.ExamApi/1.0");

            // 添加可能特定于供应商的头部
            if (settings.ApiBaseUrl.Contains("dashscope.aliyuncs.com"))
            {
                _logger.LogDebug("添加阿里云特定请求头");
                client.DefaultRequestHeaders.Add("X-DashScope-Client", "CodeSpirit.ExamApi");
            }

            _logger.LogInformation("HTTP客户端创建成功");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建HTTP客户端时发生错误: {ErrorMessage}", ex.Message);
            // 尝试创建一个基本客户端
            var fallbackClient = new HttpClient();
            fallbackClient.Timeout = TimeSpan.FromSeconds(120); // 默认超时
            return fallbackClient;
        }
    }

    /// <summary>
    /// 确定请求URL
    /// </summary>
    private string DetermineRequestUrl(string apiBaseUrl, string modelName)
    {
        // 所有API都使用相同的接口地址
        return $"{apiBaseUrl.TrimEnd('/')}/chat/completions";
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    private object BuildRequestBody(string modelName, string systemPrompt, string userPrompt, int maxTokens)
    {
        // 通用格式
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