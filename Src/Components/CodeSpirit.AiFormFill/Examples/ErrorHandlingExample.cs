using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.AiFormFill.Examples;

/// <summary>
/// 错误处理示例 - 演示各种错误情况下的日志输出
/// </summary>
public class ErrorHandlingExample
{
    /// <summary>
    /// 测试无效API密钥的错误处理
    /// </summary>
    [AiFormFill(
        TriggerField = nameof(Topic),
        UseIndependentLLM = true,
        LLMSettingsKey = "InvalidApiKeyLLM", // 使用无效的API密钥配置
        DisableThinking = true,
        ResponseFormatType = "json_object")]
    public class InvalidApiKeyDto
    {
        [Required]
        [DisplayName("主题")]
        public string Topic { get; set; } = string.Empty;

        [DisplayName("描述")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 测试无效模型名称的错误处理
    /// </summary>
    [AiFormFill(
        TriggerField = nameof(Topic),
        UseIndependentLLM = true,
        LLMSettingsKey = "InvalidModelLLM", // 使用无效的模型名称配置
        DisableThinking = true,
        ResponseFormatType = "json_object")]
    public class InvalidModelDto
    {
        [Required]
        [DisplayName("主题")]
        public string Topic { get; set; } = string.Empty;

        [DisplayName("内容")]
        public string? Content { get; set; }
    }

    /// <summary>
    /// 测试网络超时的错误处理
    /// </summary>
    [AiFormFill(
        TriggerField = nameof(Topic),
        UseIndependentLLM = true,
        LLMSettingsKey = "TimeoutLLM", // 使用超时配置（超时时间设置为1秒）
        DisableThinking = true,
        ResponseFormatType = "json_object")]
    public class TimeoutTestDto
    {
        [Required]
        [DisplayName("主题")]
        public string Topic { get; set; } = string.Empty;

        [DisplayName("结果")]
        public string? Result { get; set; }
    }
}

/// <summary>
/// 错误配置示例
/// 在 appsettings.json 中添加以下配置来测试不同的错误情况：
/// </summary>
public static class ErrorConfigurationExamples
{
    /// <summary>
    /// 无效API密钥配置示例
    /// </summary>
    public const string InvalidApiKeyConfig = @"
{
  ""InvalidApiKeyLLM"": {
    ""ApiBaseUrl"": ""https://dashscope.aliyuncs.com/compatible-mode/v1"",
    ""ApiKey"": ""invalid-api-key-12345"",
    ""ModelName"": ""qwq-plus"",
    ""TimeoutSeconds"": 30,
    ""MaxTokens"": 1000,
    ""DisableThinking"": true,
    ""ResponseFormatType"": ""json_object""
  }
}";

    /// <summary>
    /// 无效模型名称配置示例
    /// </summary>
    public const string InvalidModelConfig = @"
{
  ""InvalidModelLLM"": {
    ""ApiBaseUrl"": ""https://dashscope.aliyuncs.com/compatible-mode/v1"",
    ""ApiKey"": ""your-valid-api-key"",
    ""ModelName"": ""non-existent-model"",
    ""TimeoutSeconds"": 30,
    ""MaxTokens"": 1000,
    ""DisableThinking"": true,
    ""ResponseFormatType"": ""json_object""
  }
}";

    /// <summary>
    /// 超时测试配置示例
    /// </summary>
    public const string TimeoutConfig = @"
{
  ""TimeoutLLM"": {
    ""ApiBaseUrl"": ""https://dashscope.aliyuncs.com/compatible-mode/v1"",
    ""ApiKey"": ""your-valid-api-key"",
    ""ModelName"": ""qwq-plus"",
    ""TimeoutSeconds"": 1,
    ""MaxTokens"": 1000,
    ""DisableThinking"": true,
    ""ResponseFormatType"": ""json_object""
  }
}";

    /// <summary>
    /// 无效API地址配置示例
    /// </summary>
    public const string InvalidUrlConfig = @"
{
  ""InvalidUrlLLM"": {
    ""ApiBaseUrl"": ""https://invalid-api-endpoint.com/v1"",
    ""ApiKey"": ""your-valid-api-key"",
    ""ModelName"": ""qwq-plus"",
    ""TimeoutSeconds"": 30,
    ""MaxTokens"": 1000,
    ""DisableThinking"": true,
    ""ResponseFormatType"": ""json_object""
  }
}";
}

/// <summary>
/// 预期的错误日志输出示例
/// </summary>
public static class ExpectedErrorLogs
{
    /// <summary>
    /// 401 Unauthorized 错误日志
    /// </summary>
    public const string UnauthorizedError = @"
[Information] 使用独立AI表单填充LLM配置，设置键：InvalidApiKeyLLM
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Information] AI表单填充API响应状态码: Unauthorized
[Error] AI表单填充API请求失败，状态码: Unauthorized, 错误内容: {""error"":{""message"":""Invalid API key"",""type"":""authentication_error""}}
[Error] AI表单填充HTTP请求失败: AI表单填充API请求失败，状态码: Unauthorized, 错误: {""error"":{""message"":""Invalid API key"",""type"":""authentication_error""}}
";

    /// <summary>
    /// 400 Bad Request 错误日志
    /// </summary>
    public const string BadRequestError = @"
[Information] 使用独立AI表单填充LLM配置，设置键：InvalidModelLLM
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Information] AI表单填充API响应状态码: BadRequest
[Error] AI表单填充API请求失败，状态码: BadRequest, 错误内容: {""error"":{""message"":""Invalid model name: non-existent-model"",""type"":""invalid_request_error""}}
[Error] AI表单填充HTTP请求失败: AI表单填充API请求失败，状态码: BadRequest, 错误: {""error"":{""message"":""Invalid model name: non-existent-model"",""type"":""invalid_request_error""}}
";

    /// <summary>
    /// 超时错误日志
    /// </summary>
    public const string TimeoutError = @"
[Information] 使用独立AI表单填充LLM配置，设置键：TimeoutLLM
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Error] AI表单填充请求超时: The operation was canceled.
";

    /// <summary>
    /// 网络连接错误日志
    /// </summary>
    public const string NetworkError = @"
[Information] 使用独立AI表单填充LLM配置，设置键：InvalidUrlLLM
[Information] 发送AI表单填充请求到: https://invalid-api-endpoint.com/v1/chat/completions
[Error] AI表单填充HTTP请求失败: No such host is known. (invalid-api-endpoint.com:443)
";
}
