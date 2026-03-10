using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Resources;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 答题日志 DTO
/// </summary>
public class ExamAnswerLogDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [Display(Name = "ExamAnswerLog.Id", ResourceType = typeof(ExamDisplayResources))]
    public long Id { get; set; }

    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Display(Name = "ExamAnswerLog.ExamRecordId", ResourceType = typeof(ExamDisplayResources))]
    public long ExamRecordId { get; set; }

    /// <summary>
    /// 考试名称
    /// </summary>
    [Display(Name = "ExamAnswerLog.ExamName", ResourceType = typeof(ExamDisplayResources))]
    public string ExamName { get; set; } = string.Empty;

    /// <summary>
    /// 考生姓名
    /// </summary>
    [Display(Name = "ExamAnswerLog.StudentName", ResourceType = typeof(ExamDisplayResources))]
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// 准考证号
    /// </summary>
    [Display(Name = "ExamAnswerLog.AdmissionTicket", ResourceType = typeof(ExamDisplayResources))]
    public string AdmissionTicket { get; set; } = string.Empty;

    /// <summary>
    /// 题目序号
    /// </summary>
    [Display(Name = "ExamAnswerLog.OrderNumber", ResourceType = typeof(ExamDisplayResources))]
    public int OrderNumber { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    [Display(Name = "ExamAnswerLog.QuestionContent", ResourceType = typeof(ExamDisplayResources))]
    public string QuestionContent { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Display(Name = "ExamAnswerLog.QuestionType", ResourceType = typeof(ExamDisplayResources))]
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    [Display(Name = "ExamAnswerLog.OperationType", ResourceType = typeof(ExamDisplayResources))]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作时间
    /// </summary>
    [Display(Name = "ExamAnswerLog.OperationTime", ResourceType = typeof(ExamDisplayResources))]
    public DateTime OperationTime { get; set; }

    /// <summary>
    /// 考生答案
    /// </summary>
    [Display(Name = "ExamAnswerLog.Answer", ResourceType = typeof(ExamDisplayResources))]
    public string Answer { get; set; } = string.Empty;
}
