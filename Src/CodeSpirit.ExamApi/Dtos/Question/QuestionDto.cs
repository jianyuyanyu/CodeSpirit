using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 题目DTO
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public long Id { get; set; }

    [DisplayName("题目内容")]
    public string Content { get; set; } = string.Empty;

    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    [DisplayName("难度")]
    public QuestionDifficulty Difficulty { get; set; }

    [DisplayName("选项")]
    [AmisColumn(Type = "json")]
    [AmisFormField(Type = "json")]
    public List<string> Options { get; set; } = [];

    [DisplayName("正确答案")]
    [AmisColumn(Copyable = true)]
    public string CorrectAnswer { get; set; } = string.Empty;

    [DisplayName("解析")]
    [AmisColumn(Copyable = true, Toggled = false)]
    public string? Analysis { get; set; }

    [DisplayName("知识点")]
    [AmisColumn(Type = "json", Toggled = false)]
    [AmisFormField(Type = "json")]
    public string? KnowledgePoints { get; set; }

    [DisplayName("分类")]
    public string CategoryName { get; set; } = string.Empty;

    [IgnoreColumn]
    public long CategoryId { get; set; }

    [DisplayName("分值")]
    public int DefaultScore { get; set; }

    [DisplayName("版本")]
    public int Version { get; set; }

    [DisplayName("使用次数")]
    public int UsageCount { get; set; }

    [DisplayName("正确率")]
    //[AmisColumn(Type = "progress", ShowCounter = true)]
    public decimal CorrectRate { get; set; }

    [DisplayName("标签")]
    [TagsColumn(Color = "info")]
    public List<string>? Tags { get; set; }

    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }

    [DisplayName("更新人")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string? UpdatedBy { get; set; }
}