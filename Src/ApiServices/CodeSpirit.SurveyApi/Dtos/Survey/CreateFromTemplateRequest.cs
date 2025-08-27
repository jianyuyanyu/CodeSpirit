using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 从模板创建问卷请求
/// </summary>
public class CreateFromTemplateRequest
{
    /// <summary>
    /// 新问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;
}
