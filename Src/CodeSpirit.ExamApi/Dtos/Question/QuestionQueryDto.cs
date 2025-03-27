using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 题目查询DTO
/// </summary>
public class QuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType? Type { get; set; }
    
    /// <summary>
    /// 题目难度
    /// </summary>
    [DisplayName("难度")]
    public QuestionDifficulty? Difficulty { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [DisplayName("分类")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
        Multiple = false,
        Cascade = true,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Required = false,
        Clearable = true
    )]
    public long? CategoryId { get; set; }

    /// <summary>
    /// 知识点
    /// </summary>
    [DisplayName("知识点")]
    public string? KnowledgePoint { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    public string? Tag { get; set; }
} 