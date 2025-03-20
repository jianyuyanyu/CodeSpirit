using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Services.TextParsers;

/// <summary>
/// 题目文本解析结果
/// </summary>
public class QuestionParseResult
{
    /// <summary>
    /// 题目内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    public QuestionType Type { get; set; }

    /// <summary>
    /// 题目选项（单选题、多选题有效）
    /// </summary>
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 正确答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    public string? Analysis { get; set; }

    /// <summary>
    /// 分值
    /// </summary>
    public int Score { get; set; } = 1;

    /// <summary>
    /// 标签（JSON格式存储）
    /// </summary>
    public List<string>? Tags { get; set; }
}
