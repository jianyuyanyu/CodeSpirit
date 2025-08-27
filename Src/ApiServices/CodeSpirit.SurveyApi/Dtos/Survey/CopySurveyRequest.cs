using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 复制问卷请求
/// </summary>
public class CopySurveyRequest
{
    /// <summary>
    /// 新问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;
}
