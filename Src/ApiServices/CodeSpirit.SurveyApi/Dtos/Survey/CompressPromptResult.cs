using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 压缩提示词结果
/// </summary>
public class CompressPromptResult
{
    /// <summary>
    /// 原始提示词
    /// </summary>
    [DisplayName("原始提示词")]
    public string OriginalPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 压缩后的提示词
    /// </summary>
    [DisplayName("压缩后的提示词")]
    public string CompressedPrompt { get; set; } = string.Empty;

    /// <summary>
    /// 原始长度
    /// </summary>
    [DisplayName("原始长度")]
    public int OriginalLength { get; set; }

    /// <summary>
    /// 压缩后长度
    /// </summary>
    [DisplayName("压缩后长度")]
    public int CompressedLength { get; set; }

    /// <summary>
    /// 压缩比率
    /// </summary>
    [DisplayName("压缩比率")]
    public double CompressionRatio { get; set; }
}
