using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.PathfinderApi.Models.Enums;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 创建任务DTO
/// </summary>
public class CreateTaskDto
{
    /// <summary>
    /// 目标ID
    /// </summary>
    [Required(ErrorMessage = "目标ID不能为空")]
    [DisplayName("目标ID")]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 任务标题
    /// </summary>
    [Required(ErrorMessage = "任务标题不能为空")]
    [MaxLength(200, ErrorMessage = "任务标题长度不能超过200个字符")]
    [DisplayName("任务标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 任务描述
    /// </summary>
    [MaxLength(2000, ErrorMessage = "任务描述长度不能超过2000个字符")]
    [DisplayName("任务描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级必须在1-5之间")]
    [DisplayName("优先级")]
    public int Priority { get; set; } = 3;
    
    /// <summary>
    /// 预计开始时间
    /// </summary>
    [DisplayName("预计开始时间")]
    public DateTime? EstimatedStartTime { get; set; }
    
    /// <summary>
    /// 预计完成时间
    /// </summary>
    [DisplayName("预计完成时间")]
    public DateTime? EstimatedEndTime { get; set; }
    
    /// <summary>
    /// 依赖任务（逗号分隔的任务ID或序号）
    /// </summary>
    [DisplayName("依赖任务")]
    public string? DependsOn { get; set; }
}
