using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 试卷类型
/// </summary>
public enum ExamPaperType
{
    /// <summary>
    /// 固定试卷（手动选题）
    /// </summary>
    [Display(Name = "固定试卷")]
    Fixed = 1,
    
    /// <summary>
    /// 随机试卷（根据规则自动选题）
    /// </summary>
    [Display(Name = "随机试卷")]
    Random = 2
}
