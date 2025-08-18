using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Question;

/// <summary>
/// AI生成题目请求DTO
/// </summary>
public class AIGenerateQuestionDto
{
    /// <summary>
    /// 题目主题或知识领域
    /// </summary>
    [Required(ErrorMessage = "主题不能为空")]
    [StringLength(100, ErrorMessage = "主题最多100字符")]
    [DisplayName("主题")]
    [AmisFormField(type: "input-text", Required = true)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 需要生成的题目数量
    /// </summary>
    [Required(ErrorMessage = "请指定题目数量")]
    [Range(1, 10, ErrorMessage = "题目数量范围为1-10题")]
    [DisplayName("题目数量")]
    [AmisNumberField(Min = 1, Max = 10, Step = 1)]
    public int Count { get; set; } = 1;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required(ErrorMessage = "请选择题目类型")]
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;

    /// <summary>
    /// 题目难度
    /// </summary>
    [Required(ErrorMessage = "请选择题目难度")]
    [DisplayName("难度")]
    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;
    
    /// <summary>
    /// 分类ID
    /// </summary>
    [Required(ErrorMessage = "请选择题目分类")]
    [DisplayName("分类")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
        Multiple = false,
        Cascade = true,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id"
    )]
    public long CategoryId { get; set; }
    
    /// <summary>
    /// 生成要求
    /// </summary>
    [DisplayName("生成要求")]
    [StringLength(500, ErrorMessage = "生成要求最多500字符")]
    [AmisTextareaField(MaxLength = 500, ShowCounter = true, Placeholder = "请输入对生成题目的特定要求，例如：围绕某个特定概念、包含具体知识点等")]
    public string? Requirements { get; set; }
} 