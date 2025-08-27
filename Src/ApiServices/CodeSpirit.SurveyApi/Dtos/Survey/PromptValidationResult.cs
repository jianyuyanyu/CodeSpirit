using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 提示词验证结果
/// </summary>
[DisplayName("提示词验证结果")]
public class PromptValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    [DisplayName("是否有效")]
    public bool IsValid { get; set; }

    /// <summary>
    /// 提示词长度
    /// </summary>
    [DisplayName("提示词长度")]
    public int Length { get; set; }

    /// <summary>
    /// 预估Token数
    /// </summary>
    [DisplayName("预估Token数")]
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// 验证消息
    /// </summary>
    [DisplayName("验证消息")]
    public string? Message { get; set; }

    /// <summary>
    /// 是否需要压缩
    /// </summary>
    [DisplayName("是否需要压缩")]
    public bool NeedsCompression { get; set; }
}
