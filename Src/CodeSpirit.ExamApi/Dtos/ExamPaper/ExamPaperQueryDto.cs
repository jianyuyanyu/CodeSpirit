using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷查询DTO
/// </summary>
[DisplayName("查询试卷")]
public class ExamPaperQueryDto : QueryDtoBase
{
    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    public ExamPaperType? Type { get; set; }
    
    /// <summary>
    /// 试卷状态
    /// </summary>
    [DisplayName("试卷状态")]
    public ExamPaperStatus? Status { get; set; }
    
    /// <summary>
    /// 难度级别最小值
    /// </summary>
    [DisplayName("难度级别最小值")]
    public int? MinDifficultyLevel { get; set; }
    
    /// <summary>
    /// 难度级别最大值
    /// </summary>
    [DisplayName("难度级别最大值")]
    public int? MaxDifficultyLevel { get; set; }
} 