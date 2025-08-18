namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 题型统计信息
/// </summary>
public class QuestionTypeStatistics
{
    /// <summary>
    /// 题型
    /// </summary>
    [DisplayName("题型")]
    public string Type { get; set; }
    
    /// <summary>
    /// 题型名称
    /// </summary>
    [DisplayName("题型名称")]
    public string TypeName { get; set; }
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; }
    
    /// <summary>
    /// 得分
    /// </summary>
    [DisplayName("得分")]
    public int Score { get; set; }
    
    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public int TotalScore { get; set; }
    
    /// <summary>
    /// 正确题数
    /// </summary>
    [DisplayName("正确题数")]
    public int CorrectCount { get; set; }
} 