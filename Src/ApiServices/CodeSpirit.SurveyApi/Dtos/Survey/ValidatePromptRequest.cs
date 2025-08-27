using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 验证提示词请求
/// </summary>
public class ValidatePromptRequest
{
    /// <summary>
    /// 提示词
    /// </summary>
    [Required]
    [StringLength(10000)]
    [DisplayName("提示词")]
    public string Prompt { get; set; } = string.Empty;
}
