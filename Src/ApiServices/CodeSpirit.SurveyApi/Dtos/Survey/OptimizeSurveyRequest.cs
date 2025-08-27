using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 优化问卷请求
/// </summary>
public class OptimizeSurveyRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 优化目标
    /// </summary>
    [Required]
    [StringLength(1000)]
    [DisplayName("优化目标")]
    public string OptimizationGoals { get; set; } = string.Empty;
}
