using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.LLM.Settings;

/// <summary>
/// 大语言模型设置
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// API基础地址
    /// </summary>
    [Required]
    [DisplayName("API地址")]
    [Description("大语言模型API的基础地址")]
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// API密钥
    /// </summary>
    [Required]
    [DisplayName("API密钥")]
    [Description("访问大语言模型API的密钥")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    [Required]
    [DisplayName("模型名称")]
    [Description("使用的大语言模型名称")]
    public string ModelName { get; set; } = "gpt-4o";

    /// <summary>
    /// 超时时间(秒)
    /// </summary>
    [DisplayName("超时时间(秒)")]
    [Range(10, 300)]
    [Description("API请求超时时间")]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 最大令牌数
    /// </summary>
    [DisplayName("最大令牌数")]
    [Range(100, 8000)]
    [Description("生成内容的最大令牌数")]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// 是否启用代理
    /// </summary>
    [DisplayName("启用代理")]
    [Description("是否启用HTTP代理")]
    public bool UseProxy { get; set; } = false;

    /// <summary>
    /// 代理地址
    /// </summary>
    [DisplayName("代理地址")]
    [Description("HTTP代理地址，如http://127.0.0.1:7890")]
    public string? ProxyAddress { get; set; }
}
