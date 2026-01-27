using System.Text;
using System.Text.Json;
using CodeSpirit.Audit.Models;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 敏感数据处理器
/// </summary>
/// <remarks>
/// 专门负责敏感数据的脱敏处理
/// </remarks>
public class SensitiveDataProcessor
{
    private readonly AuditOptions _options;
    private readonly ILogger<SensitiveDataProcessor> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SensitiveDataProcessor(
        IOptions<AuditOptions> options,
        ILogger<SensitiveDataProcessor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 对敏感数据进行脱敏处理
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>脱敏后的数据</returns>
    public string Sanitize(string data)
    {
        if (string.IsNullOrEmpty(data) || !_options.SensitiveData.Enabled)
        {
            return data;
        }

        try
        {
            // 尝试解析为JSON
            var (isValidJson, jsonDoc) = TryParseJson(data);
            if (isValidJson && jsonDoc != null)
            {
                using (jsonDoc) // 确保JsonDocument被正确释放
                {
                    return SanitizeJson(jsonDoc);
                }
            }

            // 对查询字符串参数进行脱敏
            if (data.Contains('=') && data.Contains('&'))
            {
                return SanitizeQueryString(data);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "敏感数据脱敏处理失败");
            return data;
        }
    }

    /// <summary>
    /// 处理JSON中的敏感数据
    /// </summary>
    private string SanitizeJson(JsonDocument jsonDoc)
    {
        try
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                SanitizeJsonElement(jsonDoc.RootElement, writer);
                writer.Flush();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JSON脱敏处理失败");
            return "[JSON脱敏失败]";
        }
    }

    /// <summary>
    /// 脱敏JSON元素
    /// </summary>
    private void SanitizeJsonElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                foreach (var property in element.EnumerateObject())
                {
                    var propertyName = property.Name.ToLowerInvariant();

                    // 检查是否为要排除的字段
                    if (_options.SensitiveData.ExcludedFields.Any(p =>
                        propertyName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WritePropertyName(property.Name);
                        writer.WriteStringValue("[已移除]");
                        continue;
                    }

                    // 检查是否需要脱敏
                    bool isSensitive = _options.SensitiveData.SensitiveFieldPatterns.Any(p =>
                        propertyName.Contains(p, StringComparison.OrdinalIgnoreCase));

                    writer.WritePropertyName(property.Name);

                    if (isSensitive && property.Value.ValueKind == JsonValueKind.String)
                    {
                        writer.WriteStringValue(MaskSensitiveValue(property.Value.GetString()));
                    }
                    else
                    {
                        SanitizeJsonElement(property.Value, writer);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in element.EnumerateArray())
                {
                    SanitizeJsonElement(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                WriteJsonValue(element, writer);
                break;
        }
    }

    /// <summary>
    /// 写入JSON值
    /// </summary>
    private void WriteJsonValue(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    writer.WriteNumberValue(intValue);
                }
                else if (element.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    writer.WriteNumberValue(doubleValue);
                }
                else
                {
                    writer.WriteStringValue(element.GetRawText());
                }
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteStringValue(element.GetRawText());
                break;
        }
    }

    /// <summary>
    /// 对查询字符串参数进行脱敏
    /// </summary>
    private string SanitizeQueryString(string queryString)
    {
        try
        {
            var resultParts = new List<string>();
            var parts = queryString.Split('&');

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part) || !part.Contains('='))
                {
                    resultParts.Add(part);
                    continue;
                }

                var keyValue = part.Split('=', 2);
                var key = keyValue[0];
                var value = keyValue.Length > 1 ? keyValue[1] : string.Empty;

                // 检查是否为要排除的字段
                if (_options.SensitiveData.ExcludedFields.Any(p =>
                    key.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    resultParts.Add($"{key}=[已移除]");
                    continue;
                }

                // 检查是否需要脱敏
                bool isSensitive = _options.SensitiveData.SensitiveFieldPatterns.Any(p =>
                    key.Contains(p, StringComparison.OrdinalIgnoreCase));

                if (isSensitive)
                {
                    resultParts.Add($"{key}={MaskSensitiveValue(value)}");
                }
                else
                {
                    resultParts.Add(part);
                }
            }

            return string.Join("&", resultParts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询字符串脱敏处理失败");
            return queryString;
        }
    }

    /// <summary>
    /// 掩码敏感值
    /// </summary>
    private string MaskSensitiveValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int keepFirstChars = _options.SensitiveData.KeepFirstChars;
        int keepLastChars = _options.SensitiveData.KeepLastChars;
        string maskChar = _options.SensitiveData.MaskCharacter;

        // 如果值太短，直接全部掩码
        if (value.Length <= keepFirstChars + keepLastChars)
        {
            return new string(maskChar[0], value.Length);
        }

        // 保留前几位和后几位，中间部分掩码
        var result = new StringBuilder();

        // 保留前面的字符
        if (keepFirstChars > 0)
        {
            result.Append(value.Substring(0, keepFirstChars));
        }

        // 中间部分掩码
        int maskLength = value.Length - keepFirstChars - keepLastChars;
        result.Append(new string(maskChar[0], maskLength));

        // 保留后面的字符
        if (keepLastChars > 0)
        {
            result.Append(value.Substring(value.Length - keepLastChars));
        }

        return result.ToString();
    }

    /// <summary>
    /// 检查是否为有效的JSON并返回解析结果
    /// </summary>
    private (bool IsValid, JsonDocument? Document) TryParseJson(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (false, null);
        }

        input = input.Trim();
        if (!((input.StartsWith("{") && input.EndsWith("}")) ||
              (input.StartsWith("[") && input.EndsWith("]"))))
        {
            return (false, null);
        }

        try
        {
            var document = JsonDocument.Parse(input);
            return (true, document);
        }
        catch (JsonException)
        {
            return (false, null);
        }
    }
}
