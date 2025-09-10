using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Shared.Dtos.AI;

/// <summary>
/// AI任务状态数据传输对象
/// </summary>
[DisplayName("AI任务状态")]
public class AiTaskStatusDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public AiTaskStatus Status { get; set; }

    /// <summary>
    /// 状态文字描述
    /// </summary>
    [DisplayName("状态描述")]
    public string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// 当前步骤（0-3）
    /// </summary>
    [DisplayName("当前步骤")]
    public int Step { get; set; }

    /// <summary>
    /// 进度百分比（0-100）
    /// </summary>
    [DisplayName("进度百分比")]
    public int Progress { get; set; }

    /// <summary>
    /// 已耗时（格式化字符串，如："2分30秒"）
    /// </summary>
    [DisplayName("已耗时")]
    public string ElapsedTime { get; set; } = string.Empty;

    /// <summary>
    /// 处理日志
    /// </summary>
    [DisplayName("处理日志")]
    public List<string> Logs { get; set; } = new();

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    [DisplayName("错误信息")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 生成结果（如果完成）
    /// </summary>
    [DisplayName("生成结果")]
    public object? Result { get; set; }

    /// <summary>
    /// 详情页面URL（如果有）
    /// </summary>
    [DisplayName("详情页面")]
    public string? DetailUrl { get; set; }

    /// <summary>
    /// 任务开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 任务结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// AI任务状态枚举
/// </summary>
public enum AiTaskStatus
{
    /// <summary>
    /// 待开始
    /// </summary>
    [Display(Name = "待开始")]
    Pending = 0,

    /// <summary>
    /// 进行中
    /// </summary>
    [Display(Name = "进行中")]
    Running = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    [Display(Name = "已完成")]
    Completed = 2,

    /// <summary>
    /// 失败
    /// </summary>
    [Display(Name = "失败")]
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 4
}
