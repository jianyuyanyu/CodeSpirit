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
    public List<OptionDisplayDto> Options { get; set; }
    
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

    /// <summary>
    /// 答案
    /// </summary>
    public string? Answer { get; set; }
    public int TypeValue { get; internal set; }
}

public class OptionDisplayDto
{
    public string Label { get; set; }

    public string Value { get; set; }
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
    
    /// <summary>
    /// 原始成绩（换算前）
    /// </summary>
    [DisplayName("原始成绩")]
    public double? OriginalScore { get; set; }
    
    /// <summary>
    /// 是否应用了成绩换算
    /// </summary>
    [DisplayName("已应用换算")]
    public bool IsScoreConverted { get; set; }
    
    /// <summary>
    /// 换算比例（记录当时的换算比例）
    /// </summary>
    [DisplayName("换算比例")]
    public decimal? ScoreConversionRatio { get; set; }
    
    /// <summary>
    /// 试卷信息（包含换算配置）
    /// </summary>
    [DisplayName("试卷信息")]
    public ClientExamPaperInfoDto? Exam { get; set; }
    
    /// <summary>
    /// 是否在结果页显示题目分析
    /// </summary>
    [DisplayName("显示题目分析")]
    public bool EnableQuestionAnalysis { get; set; }
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
    
    /// <summary>
    /// 是否已作答
    /// </summary>
    [DisplayName("是否已作答")]
    public bool IsAnswered { get; set; }
}

/// <summary>
/// 考生个人信息DTO
/// </summary>
[DisplayName("考生个人信息")]
public class ClientProfileDto
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public long UserId { get; set; }
    
    /// <summary>
    /// 姓名
    /// </summary>
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 学号
    /// </summary>
    [DisplayName("学号")]
    public string StudentNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 身份证号码
    /// </summary>
    [DisplayName("身份证号码")]
    public string IdNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    public string Gender { get; set; } = string.Empty;
    
    /// <summary>
    /// 准考证号
    /// </summary>
    [DisplayName("准考证号")]
    public string AdmissionTicket { get; set; } = string.Empty;
    
    /// <summary>
    /// 手机号码
    /// </summary>
    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 所属考生组
    /// </summary>
    [DisplayName("所属考生组")]
    public List<string> StudentGroups { get; set; } = new List<string>();
}

/// <summary>
/// 客户端试卷信息DTO（包含换算配置）
/// </summary>
[DisplayName("试卷信息")]
public class ClientExamPaperInfoDto
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long Id { get; set; }
    
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 及格分
    /// </summary>
    [DisplayName("及格分")]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 是否启用成绩换算
    /// </summary>
    [DisplayName("启用成绩换算")]
    public bool EnableScoreConversion { get; set; }
    
    /// <summary>
    /// 原始总分（换算前的满分）
    /// </summary>
    [DisplayName("原始总分")]
    public int? OriginalTotalScore { get; set; }
    
    /// <summary>
    /// 原始及格分（换算前的及格分）
    /// </summary>
    [DisplayName("原始及格分")]
    public int? OriginalPassScore { get; set; }
    
    /// <summary>
    /// 换算目标满分
    /// </summary>
    [DisplayName("换算目标满分")]
    public int? ConversionTargetFullScore { get; set; }
    
    /// <summary>
    /// 换算目标及格分
    /// </summary>
    [DisplayName("换算目标及格分")]
    public int? ConversionTargetPassScore { get; set; }
    
    /// <summary>
    /// 换算小数保留位数
    /// </summary>
    [DisplayName("小数保留位数")]
    public int ConversionDecimalPlaces { get; set; }
    
    /// <summary>
    /// 换算比例
    /// </summary>
    [DisplayName("换算比例")]
    public decimal? ConversionRatio { get; set; }
    
    /// <summary>
    /// 换算描述（自动生成，仅用于前端显示）
    /// </summary>
    [DisplayName("换算说明")]
    public string ConversionDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int? QuestionCount { get; set; }
    
    /// <summary>
    /// 题目总数（用于统计）
    /// </summary>
    [DisplayName("题目总数")]
    public int? TotalQuestions { get; set; }
} 