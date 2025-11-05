using System.ComponentModel;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 任务拆解结果DTO
/// </summary>
public class TaskBreakdownResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool Success { get; set; }
    
    /// <summary>
    /// 拆解后的任务列表
    /// </summary>
    [DisplayName("任务列表")]
    public List<TaskDto> Tasks { get; set; } = new();
    
    /// <summary>
    /// 拆解分析说明
    /// </summary>
    [DisplayName("拆解说明")]
    public string? Analysis { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [DisplayName("错误信息")]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 建议和注意事项
    /// </summary>
    [DisplayName("建议")]
    public List<string> Suggestions { get; set; } = new();
}

