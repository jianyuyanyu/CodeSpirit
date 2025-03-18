using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷DTO
/// </summary>
[DisplayName("试卷")]
public class ExamPaperDto
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
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    public ExamPaperType Type { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 随机试卷规则
    /// </summary>
    [DisplayName("随机试卷规则")]
    public string? RandomRules { get; set; }
    
    /// <summary>
    /// 试卷难度系数
    /// </summary>
    [DisplayName("试卷难度系数")]
    public int DifficultyLevel { get; set; }
    
    /// <summary>
    /// 试卷版本
    /// </summary>
    [DisplayName("试卷版本")]
    public int Version { get; set; }
    
    /// <summary>
    /// 使用次数
    /// </summary>
    [DisplayName("使用次数")]
    public int UsageCount { get; set; }
    
    /// <summary>
    /// 平均分
    /// </summary>
    [DisplayName("平均分")]
    public decimal AverageScore { get; set; }
    
    /// <summary>
    /// 通过率
    /// </summary>
    [DisplayName("通过率")]
    public decimal PassRate { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public ExamPaperStatus Status { get; set; }
    
    /// <summary>
    /// 试卷包含的题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<ExamPaperQuestionDto> Questions { get; set; } = [];
    
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

/// <summary>
/// 试卷创建DTO
/// </summary>
[DisplayName("创建试卷")]
public class CreateExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    [Required(ErrorMessage = "试卷类型不能为空")]
    public ExamPaperType Type { get; set; } = ExamPaperType.Fixed;
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    public int TotalScore { get; set; } = 100;
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    public int PassScore { get; set; } = 60;
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; } = 120;
    
    /// <summary>
    /// 随机试卷规则
    /// </summary>
    [DisplayName("随机试卷规则")]
    [StringLength(2000, ErrorMessage = "随机规则不能超过2000个字符")]
    public string? RandomRules { get; set; }
    
    /// <summary>
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<CreateExamPaperQuestionDto>? Questions { get; set; }
}

/// <summary>
/// 试卷更新DTO
/// </summary>
[DisplayName("更新试卷")]
public class UpdateExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; }
    
    /// <summary>
    /// 随机试卷规则
    /// </summary>
    [DisplayName("随机试卷规则")]
    [StringLength(2000, ErrorMessage = "随机规则不能超过2000个字符")]
    public string? RandomRules { get; set; }
    
    /// <summary>
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<CreateExamPaperQuestionDto>? Questions { get; set; }
}

/// <summary>
/// 随机试卷生成DTO
/// </summary>
[DisplayName("生成随机试卷")]
public class GenerateRandomExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    public int TotalScore { get; set; } = 100;
    
    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    public int PassScore { get; set; } = 60;
    
    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; } = 120;
    
    /// <summary>
    /// 题型分布规则
    /// </summary>
    [DisplayName("题型分布规则")]
    [Required(ErrorMessage = "题型分布规则不能为空")]
    public List<QuestionTypeRule> QuestionTypeRules { get; set; } = [];
    
    /// <summary>
    /// 难度分布规则
    /// </summary>
    [DisplayName("难度分布规则")]
    public List<DifficultyRule>? DifficultyRules { get; set; }
    
    /// <summary>
    /// 知识点分布规则
    /// </summary>
    [DisplayName("知识点分布规则")]
    public List<KnowledgePointRule>? KnowledgePointRules { get; set; }
    
    /// <summary>
    /// 分类ID限制
    /// </summary>
    [DisplayName("分类ID限制")]
    public List<long>? CategoryIds { get; set; }
}

/// <summary>
/// 题型分布规则
/// </summary>
[DisplayName("题型分布规则")]
public class QuestionTypeRule
{
    /// <summary>
    /// 题型
    /// </summary>
    [DisplayName("题型")]
    public QuestionType QuestionType { get; set; }
    
    /// <summary>
    /// 数量
    /// </summary>
    [DisplayName("数量")]
    [Range(1, 100, ErrorMessage = "题目数量必须在1-100之间")]
    public int Count { get; set; }
    
    /// <summary>
    /// 每题分数
    /// </summary>
    [DisplayName("每题分数")]
    [Range(1, 100, ErrorMessage = "每题分数必须在1-100之间")]
    public int ScorePerQuestion { get; set; }
}

/// <summary>
/// 难度分布规则
/// </summary>
[DisplayName("难度分布规则")]
public class DifficultyRule
{
    /// <summary>
    /// 难度
    /// </summary>
    [DisplayName("难度")]
    public QuestionDifficulty Difficulty { get; set; }
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    public int Percentage { get; set; }
}

/// <summary>
/// 知识点分布规则
/// </summary>
[DisplayName("知识点分布规则")]
public class KnowledgePointRule
{
    /// <summary>
    /// 知识点
    /// </summary>
    [DisplayName("知识点")]
    public string KnowledgePoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 比例（百分比）
    /// </summary>
    [DisplayName("比例（百分比）")]
    [Range(0, 100, ErrorMessage = "比例必须在0-100之间")]
    public int Percentage { get; set; }
}

/// <summary>
/// 试卷题目DTO
/// </summary>
[DisplayName("试卷题目")]
public class ExamPaperQuestionDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
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
    public QuestionType Type { get; set; }
    
    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<string> Options { get; set; } = [];
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;
    
    /// <summary>
    /// 题目解析
    /// </summary>
    [DisplayName("题目解析")]
    public string? Analysis { get; set; }
    
    /// <summary>
    /// 分值
    /// </summary>
    [DisplayName("分值")]
    public int Score { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    public int OrderNumber { get; set; }
    
    /// <summary>
    /// 是否必答
    /// </summary>
    [DisplayName("是否必答")]
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// 创建试卷题目DTO
/// </summary>
[DisplayName("创建试卷题目")]
public class CreateExamPaperQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    [Required(ErrorMessage = "题目ID不能为空")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目版本ID
    /// </summary>
    [DisplayName("题目版本ID")]
    [Required(ErrorMessage = "题目版本ID不能为空")]
    public long QuestionVersionId { get; set; }
    
    /// <summary>
    /// 分值
    /// </summary>
    [DisplayName("分值")]
    [Required(ErrorMessage = "分值不能为空")]
    [Range(0, 100, ErrorMessage = "分值必须在0-100之间")]
    public int Score { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    [Required(ErrorMessage = "题目序号不能为空")]
    public int OrderNumber { get; set; }
    
    /// <summary>
    /// 是否必答
    /// </summary>
    [DisplayName("是否必答")]
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// 试卷查询DTO
/// </summary>
[DisplayName("查询试卷")]
public class ExamPaperQueryDto : QueryDtoBase
{
    /// <summary>
    /// 关键词
    /// </summary>
    [DisplayName("关键词")]
    public string? Keywords { get; set; }
    
    /// <summary>
    /// 试卷类型
    /// </summary>
    [DisplayName("试卷类型")]
    public ExamPaperType? Type { get; set; }
    
    /// <summary>
    /// 试卷状态
    /// </summary>
    [DisplayName("试卷状态")]
    public ExamPaperStatus? Status { get; set; }
    
    /// <summary>
    /// 难度级别最小值
    /// </summary>
    [DisplayName("难度级别最小值")]
    public int? MinDifficultyLevel { get; set; }
    
    /// <summary>
    /// 难度级别最大值
    /// </summary>
    [DisplayName("难度级别最大值")]
    public int? MaxDifficultyLevel { get; set; }
} 