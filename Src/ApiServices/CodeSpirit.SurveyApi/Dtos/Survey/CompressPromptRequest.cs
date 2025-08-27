using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 压缩提示词请求
/// </summary>
public class CompressPromptRequest
{
    /// <summary>
    /// 原始提示词
    /// </summary>
    [Required]
    [StringLength(10000)]
    [DisplayName("原始提示词")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// 最大长度
    /// </summary>
    [Required]
    [Range(100, 5000)]
    [DisplayName("最大长度")]
    public int MaxLength { get; set; } = 2000;
}
