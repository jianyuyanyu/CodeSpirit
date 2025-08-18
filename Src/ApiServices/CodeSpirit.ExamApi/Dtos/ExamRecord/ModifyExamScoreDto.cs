using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 重新批改考试分数DTO
/// </summary>
[DisplayName("重新批改")]
public class ModifyExamScoreDto
{    
    /// <summary>
    /// 目标分数
    /// </summary>
    [DisplayName("目标分数")]
    [Required(ErrorMessage = "目标分数不能为空")]
    [Range(0, 1000, ErrorMessage = "目标分数必须在0-1000之间")]
    public double TargetScore { get; set; }
} 