using System.Text.Json;

namespace CodeSpirit.LLM.Processors;

/// <summary>
/// LLM JSON响应处理器接口
/// </summary>
public interface ILLMJsonProcessor
{
    /// <summary>
    /// 解析结构化响应
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="aiResponse">AI响应内容</param>
    /// <returns>解析结果</returns>
    Task<JsonProcessingResult<T>> ParseStructuredResponseAsync<T>(string aiResponse) where T : class;

    /// <summary>
    /// 从AI响应中提取JSON内容
    /// </summary>
    /// <param name="aiResponse">AI响应内容</param>
    /// <returns>清理后的JSON字符串</returns>
    string ExtractJsonFromResponse(string aiResponse);

    /// <summary>
    /// 修复损坏的JSON
    /// </summary>
    /// <param name="brokenJson">损坏的JSON字符串</param>
    /// <returns>修复后的JSON字符串</returns>
    string RepairJson(string brokenJson);

    /// <summary>
    /// 验证JSON格式是否有效
    /// </summary>
    /// <param name="json">JSON字符串</param>
    /// <returns>是否有效</returns>
    bool ValidateJsonStructure(string json);
}

/// <summary>
/// JSON处理结果
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class JsonProcessingResult<T> where T : class
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 解析结果
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 原始响应
    /// </summary>
    public string RawResponse { get; set; } = string.Empty;

    /// <summary>
    /// 清理后的JSON
    /// </summary>
    public string CleanedJson { get; set; } = string.Empty;

    /// <summary>
    /// 是否经过修复
    /// </summary>
    public bool WasRepaired { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="result">解析结果</param>
    /// <param name="rawResponse">原始响应</param>
    /// <param name="cleanedJson">清理后的JSON</param>
    /// <param name="wasRepaired">是否经过修复</param>
    /// <returns>成功结果</returns>
    public static JsonProcessingResult<T> Success(T result, string rawResponse, string cleanedJson, bool wasRepaired = false)
    {
        return new JsonProcessingResult<T>
        {
            IsSuccess = true,
            Result = result,
            RawResponse = rawResponse,
            CleanedJson = cleanedJson,
            WasRepaired = wasRepaired
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="errors">错误信息</param>
    /// <param name="rawResponse">原始响应</param>
    /// <returns>失败结果</returns>
    public static JsonProcessingResult<T> Failure(List<string> errors, string rawResponse = "")
    {
        return new JsonProcessingResult<T>
        {
            IsSuccess = false,
            Errors = errors,
            RawResponse = rawResponse
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    /// <param name="error">错误信息</param>
    /// <param name="rawResponse">原始响应</param>
    /// <returns>失败结果</returns>
    public static JsonProcessingResult<T> Failure(string error, string rawResponse = "")
    {
        return Failure(new List<string> { error }, rawResponse);
    }
}
