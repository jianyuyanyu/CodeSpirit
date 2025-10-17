using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 试卷检查结果
/// </summary>
[DisplayName("试卷检查结果")]
public class ExamPaperCheckResult
{
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long ExamPaperId { get; set; }
    
    /// <summary>
    /// 试卷级别错误列表
    /// </summary>
    [DisplayName("试卷级别错误列表")]
    public List<string> PaperErrors { get; set; } = new();
    
    /// <summary>
    /// 试卷级别警告列表
    /// </summary>
    [DisplayName("试卷级别警告列表")]
    public List<string> PaperWarnings { get; set; } = new();
    
    /// <summary>
    /// 题目检查结果列表
    /// </summary>
    [DisplayName("题目检查结果列表")]
    public Dictionary<long, QuestionCheckResult> QuestionValidations { get; set; } = new();
    
    /// <summary>
    /// 是否有试卷级别错误
    /// </summary>
    [DisplayName("是否有试卷级别错误")]
    public bool HasPaperErrors => PaperErrors.Count > 0;
    
    /// <summary>
    /// 是否有试卷级别警告
    /// </summary>
    [DisplayName("是否有试卷级别警告")]
    public bool HasPaperWarnings => PaperWarnings.Count > 0;
    
    /// <summary>
    /// 是否有任何问题
    /// </summary>
    [DisplayName("是否有任何问题")]
    public bool HasIssues => HasPaperErrors || HasPaperWarnings || QuestionValidations.Values.Any(q => q.HasIssues);
}

