using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.LLM.Examples;

/// <summary>
/// 题目预览DTO（示例）
/// </summary>
public class QuestionPreviewDto
{
    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 选项列表
    /// </summary>
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// 正确答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    public string? Analysis { get; set; }

    /// <summary>
    /// 难度
    /// </summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>
    /// 默认分值
    /// </summary>
    public decimal DefaultScore { get; set; }

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 知识点
    /// </summary>
    public string? KnowledgePoints { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Passed;

    /// <summary>
    /// 审核消息
    /// </summary>
    public string? AuditMessage { get; set; }

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 是否已修正
    /// </summary>
    public bool IsCorrected { get; set; }

    /// <summary>
    /// 修正说明
    /// </summary>
    public List<string>? CorrectionNotes { get; set; }

    /// <summary>
    /// 原始内容（修正前）
    /// </summary>
    public string? OriginalContent { get; set; }

    /// <summary>
    /// 原始选项（修正前）
    /// </summary>
    public List<string>? OriginalOptions { get; set; }

    /// <summary>
    /// 原始答案（修正前）
    /// </summary>
    public string? OriginalAnswer { get; set; }

    /// <summary>
    /// 原始解析（修正前）
    /// </summary>
    public string? OriginalAnalysis { get; set; }
}

/// <summary>
/// 审核状态枚举
/// </summary>
public enum AuditStatus
{
    /// <summary>
    /// 通过
    /// </summary>
    [Display(Name = "通过")]
    Passed = 1,

    /// <summary>
    /// 失败
    /// </summary>
    [Display(Name = "失败")]
    Failed = 2
}

/// <summary>
/// 批量审核结果
/// </summary>
public class BatchAuditResult
{
    /// <summary>
    /// 审核结果列表
    /// </summary>
    public List<QuestionAuditResult> Results { get; set; } = new();
}

/// <summary>
/// 单个题目审核结果
/// </summary>
public class QuestionAuditResult
{
    /// <summary>
    /// 题目索引
    /// </summary>
    public int QuestionIndex { get; set; }

    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 修正说明列表
    /// </summary>
    public List<string> Corrections { get; set; } = new();

    /// <summary>
    /// 修正后的题目内容
    /// </summary>
    public string? CorrectedContent { get; set; }

    /// <summary>
    /// 修正后的选项
    /// </summary>
    public List<string>? CorrectedOptions { get; set; }

    /// <summary>
    /// 修正后的答案
    /// </summary>
    public string? CorrectedAnswer { get; set; }

    /// <summary>
    /// 修正后的解析
    /// </summary>
    public string? CorrectedAnalysis { get; set; }
}
