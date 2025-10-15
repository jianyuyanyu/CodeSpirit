namespace CodeSpirit.ExamApi.Dtos.Client;

/// <summary>
/// 考试基本信息缓存DTO
/// </summary>
public class ExamBasicInfoCacheDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 考试名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 考试描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 允许的切屏次数
    /// </summary>
    public int AllowedScreenSwitchCount { get; set; }

    /// <summary>
    /// 是否允许查看结果
    /// </summary>
    public bool EnableViewResult { get; set; }

    /// <summary>
    /// 最小考试时间（分钟）
    /// </summary>
    public int? MinExamTime { get; set; }

    /// <summary>
    /// 允许考试次数
    /// </summary>
    public int AllowedAttempts { get; set; }

    /// <summary>
    /// 是否启用题目乱序
    /// </summary>
    public bool EnableRandomQuestionOrder { get; set; }

    /// <summary>
    /// 是否启用选项乱序
    /// </summary>
    public bool EnableRandomOptionOrder { get; set; }
}

/// <summary>
/// 用户考试记录缓存DTO
/// </summary>
public class UserExamRecordCacheDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    public long RecordId { get; set; }

    /// <summary>
    /// 当前切屏次数
    /// </summary>
    public int ScreenSwitchCount { get; set; }
}

/// <summary>
/// 考试题目数据缓存DTO
/// </summary>
public class ExamQuestionsDataCacheDto
{
    /// <summary>
    /// 题目数据字典（题目ID -> 题目DTO）
    /// </summary>
    public Dictionary<long, ClientExamQuestionDto> QuestionsData { get; set; } = new();
}