using System.ComponentModel;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 题目检查结果
/// </summary>
[DisplayName("题目检查结果")]
public class QuestionCheckResult
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目序号
    /// </summary>
    [DisplayName("题目序号")]
    public int QuestionIndex { get; set; }
    
    /// <summary>
    /// 错误列表
    /// </summary>
    [DisplayName("错误列表")]
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// 警告列表
    /// </summary>
    [DisplayName("警告列表")]
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// 是否有错误
    /// </summary>
    [DisplayName("是否有错误")]
    public bool HasErrors => Errors.Count > 0;
    
    /// <summary>
    /// 是否有警告
    /// </summary>
    [DisplayName("是否有警告")]
    public bool HasWarnings => Warnings.Count > 0;
    
    /// <summary>
    /// 是否有任何问题
    /// </summary>
    [DisplayName("是否有任何问题")]
    public bool HasIssues => HasErrors || HasWarnings;
}

