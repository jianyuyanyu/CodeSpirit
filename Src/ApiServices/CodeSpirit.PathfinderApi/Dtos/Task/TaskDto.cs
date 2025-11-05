using CodeSpirit.PathfinderApi.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 任务数据传输对象
/// </summary>
public class TaskDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// 目标ID
    /// </summary>
    [DisplayName("目标ID")]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 任务标题
    /// </summary>
    [DisplayName("任务标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 任务描述
    /// </summary>
    [DisplayName("任务描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public TaskStatus Status { get; set; }
    
    /// <summary>
    /// 优先级
    /// </summary>
    [DisplayName("优先级")]
    public int Priority { get; set; }
    
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
    /// 实际开始时间
    /// </summary>
    [DisplayName("实际开始时间")]
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// 实际完成时间
    /// </summary>
    [DisplayName("实际完成时间")]
    public DateTime? ActualEndTime { get; set; }
    
    /// <summary>
    /// 依赖任务ID（逗号分隔）
    /// </summary>
    [DisplayName("依赖任务")]
    public string? DependsOn { get; set; }
    
    /// <summary>
    /// 任务结果（JSON）
    /// </summary>
    [DisplayName("任务结果")]
    public string? Result { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}

