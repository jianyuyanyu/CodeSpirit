using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.AiFormFill.Models;

/// <summary>
/// AI表单填充专用LLM设置
/// </summary>
public class AiFormFillLLMSettings
{
    /// <summary>
    /// 设置键名
    /// </summary>
    [DisplayName("设置键名")]
    [Description("用于标识此LLM设置的键名")]
    public string? SettingsKey { get; set; }

    /// <summary>
    /// API基础地址
    /// </summary>
    [Required]
    [DisplayName("API地址")]
    [Description("大语言模型API的基础地址")]
    public string ApiBaseUrl { get; set; } = "https://dashscope.aliyuncs.com/compatible-mode/v1";

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
    public string ModelName { get; set; } = "qwq-plus";

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

    /// <summary>
    /// 是否禁用思考（enable_thinking）
    /// </summary>
    [DisplayName("禁用思考")]
    [Description("是否禁用模型的思考过程，设置为true时enable_thinking为false")]
    public bool DisableThinking { get; set; } = true;

    /// <summary>
    /// 响应格式类型
    /// </summary>
    [DisplayName("响应格式")]
    [Description("指定响应格式类型，如json_object")]
    public string ResponseFormatType { get; set; } = "json_object";

    /// <summary>
    /// 温度参数
    /// </summary>
    [DisplayName("温度参数")]
    [Range(0.0, 2.0)]
    [Description("控制生成内容的随机性，0.0表示确定性输出，2.0表示最大随机性")]
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Top-p参数
    /// </summary>
    [DisplayName("Top-p参数")]
    [Range(0.0, 1.0)]
    [Description("核采样参数，控制生成内容的多样性")]
    public double TopP { get; set; } = 0.9;

    /// <summary>
    /// 是否启用流式响应
    /// </summary>
    [DisplayName("启用流式响应")]
    [Description("是否启用流式响应处理")]
    public bool EnableStreaming { get; set; } = true;
}
