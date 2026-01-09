using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 更新定时任务DTO
/// </summary>
public class UpdateScheduledTaskDto
{
    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    [Required(ErrorMessage = "任务名称不能为空")]
    [StringLength(100, ErrorMessage = "任务名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [DisplayName("任务描述")]
    [StringLength(500, ErrorMessage = "任务描述长度不能超过500个字符")]
    [AmisTextareaField(MinRows = 2, MaxRows = 5)]
    public string? Description { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [DisplayName("任务分组")]
    [StringLength(50, ErrorMessage = "任务分组长度不能超过50个字符")]
    public string? Group { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [DisplayName("任务类型")]
    [AmisSelectField]
    public TaskType Type { get; set; }

    /// <summary>
    /// Cron表达式
    /// </summary>
    [DisplayName("Cron表达式")]
    [StringLength(100, ErrorMessage = "Cron表达式长度不能超过100个字符")]
    [AmisInputTextField(Placeholder = "例如: 0 */5 * * * * (每5分钟执行)", VisibleOn = "type == 'Cron' || type == 0")]
    public string? CronExpression { get; set; }

    /// <summary>
    /// 延迟时间（秒）
    /// </summary>
    [DisplayName("延迟时间(秒)")]
    [Range(1, 86400, ErrorMessage = "延迟时间必须在1秒到24小时之间")]
    [AmisNumberField(Min = 1, Max = 86400, Step = 1, VisibleOn = "type == 'Delay' || type == 1")]
    public int? DelaySeconds { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    [DisplayName("执行时间")]
    [AmisDatetimeField(DisplayFormat = "YYYY-MM-DD HH:mm:ss", VisibleOn = "type == 'OneTime' || type == 2")]
    public DateTime? ExecuteAt { get; set; }

    /// <summary>
    /// 处理器类型
    /// </summary>
    [DisplayName("处理器类型")]
    [Required(ErrorMessage = "处理器类型不能为空")]
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// 任务参数（JSON格式）
    /// </summary>
    [DisplayName("任务参数")]
    [AmisTextareaField(MinRows = 3, MaxRows = 10, Placeholder = "请输入JSON格式的参数")]
    public string? Parameters { get; set; }

    /// <summary>
    /// 执行策略
    /// </summary>
    [DisplayName("执行策略")]
    [AmisSelectField]
    public ExecutionStrategy ExecutionStrategy { get; set; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [DisplayName("超时时间(秒)")]
    [Range(1, 3600, ErrorMessage = "超时时间必须在1秒到1小时之间")]
    [AmisNumberField(Min = 1, Max = 3600, Step = 1)]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [DisplayName("最大重试次数")]
    [Range(0, 10, ErrorMessage = "最大重试次数必须在0-10之间")]
    [AmisNumberField(Min = 0, Max = 10, Step = 1)]
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 重试间隔（秒）
    /// </summary>
    [DisplayName("重试间隔(秒)")]
    [Range(1, 3600, ErrorMessage = "重试间隔必须在1秒到1小时之间")]
    [AmisNumberField(Min = 1, Max = 3600, Step = 1, VisibleOn = "maxRetryCount > 0")]
    public int? RetryIntervalSeconds { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [DisplayName("优先级")]
    [Range(1, 10, ErrorMessage = "优先级必须在1-10之间")]
    [AmisNumberField(Min = 1, Max = 10, Step = 1)]
    public int Priority { get; set; }

    /// <summary>
    /// 目标服务
    /// </summary>
    [DisplayName("目标服务")]
    [StringLength(100, ErrorMessage = "目标服务长度不能超过100个字符")]
    public string? TargetService { get; set; }
}
