using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.AiFormFill.Examples;

/// <summary>
/// 流式重试功能测试示例
/// </summary>
public class StreamingRetryTest
{
    /// <summary>
    /// 测试自动流式重试功能的DTO
    /// 配置为非流式模式，让系统自动检测并重试
    /// </summary>
    [AiFormFill(
        TriggerField = nameof(Topic),
        UseIndependentLLM = true,
        LLMSettingsKey = "TestStreamingRetryLLM",
        DisableThinking = true,
        ResponseFormatType = "json_object",
        Temperature = 0.1,
        TopP = 0.9)]
    public class StreamingRetryTestDto
    {
        /// <summary>
        /// 触发字段
        /// </summary>
        [Required]
        [DisplayName("主题")]
        [Description("测试主题")]
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// AI生成的描述
        /// </summary>
        [DisplayName("描述")]
        [Description("基于主题生成的描述")]
        [AiFieldFill(Priority = 1)]
        public string? Description { get; set; }

        /// <summary>
        /// AI生成的分类
        /// </summary>
        [DisplayName("分类")]
        [Description("内容分类")]
        [AiFieldFill(Priority = 2)]
        public string? Category { get; set; }
    }
}

/// <summary>
/// 测试配置示例
/// 在 appsettings.json 中添加以下配置来测试流式重试功能
/// </summary>
public static class StreamingRetryTestConfiguration
{
    /// <summary>
    /// 测试配置 - 故意设置为非流式模式来触发重试
    /// </summary>
    public const string TestConfig = @"
{
  ""TestStreamingRetryLLM"": {
    ""ApiBaseUrl"": ""https://dashscope.aliyuncs.com/compatible-mode/v1"",
    ""ApiKey"": ""your-valid-api-key"",
    ""ModelName"": ""qwq-plus"",
    ""TimeoutSeconds"": 120,
    ""MaxTokens"": 1500,
    ""DisableThinking"": true,
    ""ResponseFormatType"": ""json_object"",
    ""Temperature"": 0.1,
    ""TopP"": 0.9,
    ""EnableStreaming"": false
  }
}";

    /// <summary>
    /// 预期的日志输出
    /// </summary>
    public const string ExpectedLogs = @"
[Information] 使用独立AI表单填充LLM配置，设置键：TestStreamingRetryLLM
[Debug] AI表单填充请求体: {""model"":""qwq-plus"",""stream"":false,...}
[Information] 发送AI表单填充请求到: https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions
[Information] AI表单填充API响应状态码: BadRequest
[Error] AI表单填充API请求失败，状态码: BadRequest, 错误内容: {""error"":{""message"":""This model only support stream mode, please enable the stream parameter to access the model.""}}
[Warning] 检测到模型只支持流式模式，尝试启用流式响应重新请求
[Information] 使用流式模式重新发送AI表单填充请求
[Debug] AI表单填充流式重试请求体: {""model"":""qwq-plus"",""stream"":true,...}
[Information] AI表单填充流式重试响应状态码: OK
[Debug] AI表单填充流式内容片段: {
[Debug] AI表单填充流式内容片段: ""description""
[Debug] AI表单填充流式内容片段: :
[Debug] AI表单填充流式内容片段: ""基于图片主题的智能描述""
[Information] AI表单填充流式响应最终生成内容: {""description"":""基于图片主题的智能描述"",""category"":""图像处理""}
[Information] AI表单填充LLM响应内容：{""description"":""基于图片主题的智能描述"",""category"":""图像处理""}
[Information] AI表单填充提取的JSON内容：{""description"":""基于图片主题的智能描述"",""category"":""图像处理""}
[Information] AI表单填充成功设置属性 Description = 基于图片主题的智能描述
[Information] AI表单填充成功设置属性 Category = 图像处理
[Information] AI表单填充解析后的结果：{""Topic"":""图片"",""Description"":""基于图片主题的智能描述"",""Category"":""图像处理""}
";
}

/// <summary>
/// 错误检测逻辑测试
/// </summary>
public static class ErrorDetectionTest
{
    /// <summary>
    /// 测试各种错误消息格式的检测
    /// </summary>
    public static void TestErrorDetection()
    {
        var testCases = new[]
        {
            // 阿里云 qwq-plus 的实际错误消息
            @"{""error"":{""message"":""This model only support stream mode, please enable the stream parameter to access the model.""}}",
            
            // 其他可能的错误消息格式
            @"{""error"":{""message"":""Model only support stream mode""}}",
            @"{""error"":{""message"":""Please enable the stream parameter""}}",
            @"{""error"":{""message"":""You must enable the stream parameter""}}",
            
            // 大小写变化
            @"{""error"":{""message"":""THIS MODEL ONLY SUPPORT STREAM MODE""}}",
            @"{""error"":{""message"":""Please Enable The Stream Parameter""}}",
        };

        foreach (var testCase in testCases)
        {
            var errorContentLower = testCase.ToLowerInvariant();
            var shouldRetry = errorContentLower.Contains("only support stream mode") || 
                             errorContentLower.Contains("please enable the stream parameter") ||
                             errorContentLower.Contains("enable the stream parameter");
            
            Console.WriteLine($"测试用例: {testCase}");
            Console.WriteLine($"是否触发重试: {shouldRetry}");
            Console.WriteLine();
        }
    }
}
