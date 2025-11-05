using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 更新任务DTO
/// </summary>
public class UpdateTaskDto
{
    /// <summary>
    /// 任务标题
    /// </summary>
    [StringLength(200, ErrorMessage = "任务标题长度不能超过200个字符")]
    [DisplayName("任务标题")]
    public string? Title { get; set; }
    
    /// <summary>
    /// 任务描述
    /// </summary>
    [StringLength(2000, ErrorMessage = "任务描述长度不能超过2000个字符")]
    [DisplayName("任务描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus? Status { get; set; }
    
    /// <summary>
    /// 优先级
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级必须在1-5之间")]
    [DisplayName("优先级")]
    public int? Priority { get; set; }
    
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
    /// 依赖任务ID
    /// </summary>
    [DisplayName("依赖任务")]
    public string? DependsOn { get; set; }
}

