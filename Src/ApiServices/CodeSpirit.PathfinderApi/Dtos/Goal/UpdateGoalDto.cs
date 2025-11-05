using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos.Goal;

/// <summary>
/// 更新目标数据传输对象
/// </summary>
public class UpdateGoalDto
{
    /// <summary>
    /// 目标标题
    /// </summary>
    [MaxLength(200, ErrorMessage = "目标标题不能超过200字符")]
    [DisplayName("目标标题")]
    public string? Title { get; set; }
    
    /// <summary>
    /// 目标描述
    /// </summary>
    [MaxLength(2000, ErrorMessage = "目标描述不能超过2000字符")]
    [DisplayName("目标描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 目标类型
    /// </summary>
    [MaxLength(50, ErrorMessage = "目标类型不能超过50字符")]
    [DisplayName("目标类型")]
    public string? Category { get; set; }
    
    /// <summary>
    /// 目标日期
    /// </summary>
    [DisplayName("目标日期")]
    public DateTime? TargetDate { get; set; }
    
    /// <summary>
    /// 目标状态
    /// </summary>
    [DisplayName("目标状态")]
    public GoalStatus? Status { get; set; }
    
    /// <summary>
    /// 目标进度
    /// </summary>
    [Range(0, 100, ErrorMessage = "进度必须在0-100之间")]
    [DisplayName("目标进度")]
    public int? Progress { get; set; }
}

