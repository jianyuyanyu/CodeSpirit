using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;

/// <summary>
/// 题目版本DTO
/// </summary>
public class QuestionVersionDto
{
    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本")]
    public int Version { get; set; }

    /// <summary>
    /// 题目内容
    /// </summary>
    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("选项")]
    [AmisColumn(Type = "json")]
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 正确答案
    /// </summary>
    [DisplayName("正确答案")]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    [DisplayName("解析")]
    public string? Analysis { get; set; }

    /// <summary>
    /// 知识点
    /// </summary>
    [DisplayName("知识点")]
    [AmisColumn(Type = "json")]
    public string? KnowledgePoints { get; set; }

    /// <summary>
    /// 题目分值
    /// </summary>
    [DisplayName("分值")]
    public int DefaultScore { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    [AmisColumn(Type = "json")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 修改原因
    /// </summary>
    [DisplayName("修改原因")]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [DisplayName("修改时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    [DisplayName("修改人")]
    [AggregateField(dataSource: "http://identity/api/identity/users/{value}.data.name", template: "{field}")]
    public string CreatedBy { get; set; } = string.Empty;
} 