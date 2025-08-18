namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习答案DTO
/// </summary>
public class PracticeAnswerDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public long QuestionId { get; set; }

    /// <summary>
    /// 答案内容
    /// </summary>
    public string Answer { get; set; }

    /// <summary>
    /// 用时（秒）
    /// </summary>
    public int TimeSpent { get; set; }

    /// <summary>
    /// 是否标记
    /// </summary>
    public bool IsMarked { get; set; }
} 