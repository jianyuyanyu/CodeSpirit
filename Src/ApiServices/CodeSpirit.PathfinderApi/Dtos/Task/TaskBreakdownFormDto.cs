using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.PathfinderApi.Dtos.Task;

/// <summary>
/// 任务拆解表单DTO（支持AI表单填充）
/// </summary>
[AiFormFill(
    CustomPromptTemplate = @"你是一个专业的项目管理助手，负责将用户的目标拆解为可执行的任务。

目标信息:
- 标题: {GoalTitle}
- 描述: {GoalDescription}
- 类别: {GoalCategory}
- 目标日期: {GoalTargetDate}

用户拆解说明: {BreakdownInstructions}

拆解要求:
1. 拆解粒度: {Granularity}/5 (数字越大拆解越细)
2. 每个任务必须具体、可执行、可衡量
3. 合理设置任务优先级(1-5)
4. 估算每个任务的开始和完成时间
5. 识别任务之间的依赖关系（使用序号，如 '1,2'）
6. 如果用户提供了拆解说明，务必遵循用户的要求调整拆解策略

注意:
- 任务数量应根据拆解粒度合理控制(粒度1约3-5个任务，粒度5约10-15个任务)
- 确保时间估算合理，符合目标日期
- dependsOn 字段表示当前任务依赖哪些前置任务（使用任务在数组中的序号，从1开始）
- 如果用户提供了特殊要求，务必遵循

请直接返回 Tasks 数组，包含所有拆解后的任务。",
    EnableCache = true,
    CacheExpirationMinutes = 60,
    DisableThinking = false,
    Temperature = 0.3)]
public class TaskBreakdownFormDto
{
    /// <summary>
    /// 目标ID（隐藏字段）
    /// </summary>
    [Required(ErrorMessage = "目标ID不能为空")]
    [DisplayName("目标ID")]
    [AiFieldFill(Enabled = false)]
    [AmisFormField(Hidden = true)]
    public Guid GoalId { get; set; }
    
    /// <summary>
    /// 目标标题（静态展示）
    /// </summary>
    [DisplayName("目标标题")]
    [AiFieldFill(Enabled = false)]
    [AmisInputTextField(Static = true)]
    public string? GoalTitle { get; set; }
    
    /// <summary>
    /// 目标描述（静态展示）
    /// </summary>
    [DisplayName("目标描述")]
    [AiFieldFill(Enabled = false)]
    [AmisTextareaField(Static = true, MinRows = 2, MaxRows = 4)]
    public string? GoalDescription { get; set; }
    
    /// <summary>
    /// 目标类别（静态展示）
    /// </summary>
    [DisplayName("目标类别")]
    [AiFieldFill(Enabled = false)]
    [AmisInputTextField(Static = true)]
    public string? GoalCategory { get; set; }
    
    /// <summary>
    /// 目标日期（静态展示）
    /// </summary>
    [DisplayName("目标日期")]
    [AiFieldFill(Enabled = false)]
    [AmisDateField(Static = true)]
    public DateTime? GoalTargetDate { get; set; }
    
    /// <summary>
    /// 拆解粒度（1-5，数字越大拆解越细）
    /// </summary>
    [Range(1, 5, ErrorMessage = "拆解粒度必须在1-5之间")]
    [DisplayName("拆解粒度")]
    [AiFieldFill(Enabled = false)]
    [AmisNumberField(Min = 1, Max = 5, DefaultValue = 5)]
    public int Granularity { get; set; } = 5;
    
    /// <summary>
    /// 拆解后的任务列表（AI自动填充）
    /// </summary>
    [Required(ErrorMessage = "任务列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要生成一个任务")]
    [DisplayName("任务列表")]
    [AiFieldFill(
        Priority = 10,
        CustomDescription = "根据目标、拆解粒度和用户说明，生成合理的任务列表。每个任务包含标题、描述、优先级、预计时间和依赖关系")]
    [AmisTableField(
        Addable = true,
        Removable = true,
        Draggable = true,
        AddButtonText = "添加任务",
        ShowIndex = true,
        Editable = true,
        Copyable = true,
        QuickEdit = true,
        VisibleOn = "this.tasks && this.tasks.length > 0")]
    public List<TaskItemDto> Tasks { get; set; } = new();
}

/// <summary>
/// 任务项DTO（用于AI表单填充）
/// </summary>
public class TaskItemDto
{
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
    /// 依赖任务序号（逗号分隔，如"1,2"）
    /// </summary>
    [DisplayName("依赖任务")]
    public string? DependsOn { get; set; }
}

