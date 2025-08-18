using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 错题DTO
/// </summary>
public class WrongQuestionDto
{
    /// <summary>
    /// 答题记录ID
    /// </summary>
    [DisplayName("答题记录ID")]
    public long ExamAnswerRecordId { get; set; }
    
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [DisplayName("考试记录ID")]
    public long ExamRecordId { get; set; }
    
    /// <summary>
    /// 考试名称
    /// </summary>
    [DisplayName("考试名称")]
    public string ExamName { get; set; }
    
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
    public string QuestionContent { get; set; }
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public string QuestionType { get; set; }
    
    /// <summary>
    /// 题目分值
    /// </summary>
    [DisplayName("题目分值")]
    public int QuestionScore { get; set; }
    
    /// <summary>
    /// 考生答案
    /// </summary>
    [DisplayName("考生答案")]
    public string Answer { get; set; }
    
    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; }
    
    /// <summary>
    /// 错误分析
    /// </summary>
    [DisplayName("错误分析")]
    public string Analysis { get; set; }
    
    /// <summary>
    /// 考试时间
    /// </summary>
    [DisplayName("考试时间")]
    public DateTime ExamTime { get; set; }
} 