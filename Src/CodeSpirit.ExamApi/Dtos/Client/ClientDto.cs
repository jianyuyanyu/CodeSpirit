using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Client;

/// <summary>
/// 客户端考试DTO
/// </summary>
[DisplayName("可参加考试")]
public class ClientExamDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否已有考试结果
    /// </summary>
    [DisplayName("已有结果")]
    public bool HasResult { get; set; }
}

/// <summary>
/// 客户端考试历史DTO
/// </summary>
[DisplayName("考试历史")]
public class ClientExamHistoryDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("记录ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long ExamId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? SubmitTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    [DisplayName("得分")]
    public double? Score { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    [DisplayName("是否通过")]
    public bool IsPassed { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 客户端考试详情DTO
/// </summary>
[DisplayName("考试详情")]
public class ClientExamDetailDto
{
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("记录ID")]
    public long RecordId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考试描述
    /// </summary>
    [DisplayName("考试描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 尝试次数
    /// </summary>
    [DisplayName("尝试次数")]
    public int AttemptNumber { get; set; }
    
    /// <summary>
    /// 允许尝试次数
    /// </summary>
    [DisplayName("允许尝试次数")]
    public int AllowedAttempts { get; set; }
    
    /// <summary>
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<ClientExamQuestionDto> Questions { get; set; } = new List<ClientExamQuestionDto>();
}

/// <summary>
/// 客户端考试问题DTO
/// </summary>
[DisplayName("考试题目")]
public class ClientExamQuestionDto
{
    /// <summary>
    /// 试卷题目ID
    /// </summary>
    [DisplayName("试卷题目ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目版本ID
    /// </summary>
    [DisplayName("题目版本ID")]
    public long QuestionVersionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 选项（JSON格式）
    /// </summary>
    [DisplayName("选项")]
    public string? Options { get; set; }
    
    /// <summary>
    /// 分值
    /// </summary>
    [DisplayName("分值")]
    public int Score { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// 是否必答
    /// </summary>
    [DisplayName("是否必答")]
    public bool IsRequired { get; set; }
}

/// <summary>
/// 客户端考试答案DTO
/// </summary>
[DisplayName("考试答案")]
public class ClientExamAnswerDto
{
    /// <summary>
    /// 试卷题目ID
    /// </summary>
    [DisplayName("试卷题目ID")]
    [Required(ErrorMessage = "题目ID不能为空")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 答案内容
    /// </summary>
    [DisplayName("答案内容")]
    public string Answer { get; set; } = string.Empty;
}

/// <summary>
/// 客户端考试结果DTO
/// </summary>
[DisplayName("考试结果")]
public class ClientExamResultDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("记录ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 考试ID
    /// </summary>
    [DisplayName("考试ID")]
    public long ExamId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? SubmitTime { get; set; }
    
    /// <summary>
    /// 考试时长（分钟）
    /// </summary>
    [DisplayName("考试时长")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    [DisplayName("得分")]
    public double? Score { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    [DisplayName("是否通过")]
    public bool IsPassed { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 评语
    /// </summary>
    [DisplayName("评语")]
    public string? Comments { get; set; }
    
    /// <summary>
    /// 答题详情
    /// </summary>
    [DisplayName("答题详情")]
    public List<ClientExamAnswerResultDto> Answers { get; set; } = new List<ClientExamAnswerResultDto>();
}

/// <summary>
/// 客户端考试答案结果DTO
/// </summary>
[DisplayName("答题结果")]
public class ClientExamAnswerResultDto
{
    /// <summary>
    /// 试卷题目ID
    /// </summary>
    [DisplayName("试卷题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目分值
    /// </summary>
    [DisplayName("题目分值")]
    public int Score { get; set; }
    
    /// <summary>
    /// 用户答案
    /// </summary>
    [DisplayName("用户答案")]
    public string UserAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool IsCorrect { get; set; }
    
    /// <summary>
    /// 获得分数
    /// </summary>
    [DisplayName("获得分数")]
    public double? ObtainedScore { get; set; }
} 