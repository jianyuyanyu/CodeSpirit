using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 批量创建任务请求DTO
/// </summary>
public class BatchCreateTasksRequest
{
    /// <summary>
    /// 目标ID
    /// </summary>
    [Required(ErrorMessage = "目标ID不能为空")]
    [DisplayName("目标ID")]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 任务列表
    /// </summary>
    [Required(ErrorMessage = "任务列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要提供一个任务")]
    [DisplayName("任务列表")]
    public List<CreateTaskDto> Tasks { get; set; } = new();
}

