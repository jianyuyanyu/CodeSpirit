using System.ComponentModel;
using CodeSpirit.ExamApi.Dtos.ExamPaper;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 考试试卷详情DTO
/// </summary>
public class ExamPaperDetailDto
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long ExamPaperId { get; set; }
    
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("考试记录ID")]
    public long ExamRecordId { get; set; }

    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    public string ExamName { get; set; }
    
    /// <summary>
    /// 学生ID
    /// </summary>
    [DisplayName("学生ID")]
    public long StudentId { get; set; }
    
    /// <summary>
    /// 学生姓名
    /// </summary>
    [DisplayName("学生姓名")]
    public string StudentName { get; set; }
    
    /// <summary>
    /// 考试开始时间
    /// </summary>
    [DisplayName("考试开始时间")]
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? SubmitTime { get; set; }
    
    /// <summary>
    /// 总得分
    /// </summary>
    [DisplayName("总得分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 最高分
    /// </summary>
    [DisplayName("最高分")]
    public int MaxScore { get; set; }
    
    /// <summary>
    /// 通过分数
    /// </summary>
    [DisplayName("通过分数")]
    public int PassScore { get; set; }
    
    /// <summary>
    /// 是否通过
    /// </summary>
    [DisplayName("是否通过")]
    public bool IsPassed { get; set; }
    
    /// <summary>
    /// 状态
    /// </summary>
    [DisplayName("状态")]
    public string Status { get; set; }
    
    /// <summary>
    /// 试卷题目详情
    /// </summary>
    [DisplayName("试卷题目")]
    public List<ExamPaperQuestionDto> Questions { get; set; }
    
    /// <summary>
    /// 考生答案列表
    /// </summary>
    [DisplayName("考生答案")]
    public List<ClientExamAnswerWithCorrectDto> Answers { get; set; }
    
    /// <summary>
    /// 各题型统计信息
    /// </summary>
    [DisplayName("题型统计")]
    public List<QuestionTypeStatistics> TypeStatistics { get; set; }
}