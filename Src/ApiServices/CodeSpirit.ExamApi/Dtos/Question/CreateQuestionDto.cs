using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Resources;
using CodeSpirit.Localization.Resources;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>
/// 创建题目DTO
/// </summary>
[DisplayName("创建题目")]
[AiFormFill(TriggerField = nameof(Content), ApiEndpoint = "ai-fill")]
public class CreateQuestionDto
{
    /// <summary>
    /// 题目内容
    /// </summary>
    [Display(Name = "Content", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
    [StringLength(2000, 
        ErrorMessageResourceType = typeof(ValidationResources),
        ErrorMessageResourceName = "StringLengthMax")]
    [AmisFormField(type: "editor", AdditionalConfig = "{\"language\":\"markdown\",\"size\":\"xl\"}", Required = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Display(Name = "Type", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 题目难度
    /// </summary>
    [Display(Name = "Difficulty", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
    public QuestionDifficulty Difficulty { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [Display(Name = "Options", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
    [LocalizedDescription(
        "根据题目内容生成合适的选项，单选题通常4个选项，多选题可以有更多选项",
        ResourceKey = "Description.Question.Options",
        ResourceType = typeof(ExamDisplayResources)
    )]
    [AiFieldFill(Weight = 3, Priority = 1)]
    [AmisArrayField(
        Items = "{ \"type\":\"input-text\", \"required\":true }",
        Addable = true,
        Removable = true,
        Draggable = true,
        MaxLength = 10,
        VisibleOn = "type != 3"
    )]
    public List<string> Options { get; set; } = [];

    /// <summary>
    /// 正确答案
    /// </summary>
    [Display(Name = "CorrectAnswer", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
    [StringLength(1000,
        ErrorMessageResourceType = typeof(ValidationResources),
        ErrorMessageResourceName = "StringLengthMax")]
    [LocalizedDescription(
        "多选题请用逗号分隔，判断题答案必须是True或False。",
        ResourceKey = "Description.Question.CorrectAnswer",
        ResourceType = typeof(ExamDisplayResources)
    )]
    [AiFieldFill(Weight = 3, Priority = 2)]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    [Display(Name = "Analysis", ResourceType = typeof(DisplayResources))]
    [StringLength(2000,
        ErrorMessageResourceType = typeof(ValidationResources),
        ErrorMessageResourceName = "StringLengthMax")]
    [LocalizedDescription(
        "详细解释正确答案的原因，帮助学生理解知识点",
        ResourceKey = "Description.Question.Analysis",
        ResourceType = typeof(ExamDisplayResources)
    )]
    [AiFieldFill(Weight = 2, Priority = 3)]
    [AmisTextareaField(MaxLength = 2000, ShowCounter = true)]
    public string? Analysis { get; set; }

    /// <summary>
    /// 知识点
    /// </summary>
    [Display(Name = "KnowledgePoints", ResourceType = typeof(DisplayResources))]
    [LocalizedDescription(
        "列出该题目涉及的主要知识点，用逗号分隔",
        ResourceKey = "Description.Question.KnowledgePoints",
        ResourceType = typeof(ExamDisplayResources)
    )]
    [AiFieldFill(Weight = 2, Priority = 4)]
    public string? KnowledgePoints { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    [Display(Name = "CategoryId", ResourceType = typeof(DisplayResources))]
    [Required(ErrorMessageResourceType = typeof(ValidationResources), 
             ErrorMessageResourceName = "Required")]
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
    /// 题目分值
    /// </summary>
    [Display(Name = "DefaultScore", ResourceType = typeof(DisplayResources))]
    [Range(0, 100, 
        ErrorMessageResourceType = typeof(ValidationResources),
        ErrorMessageResourceName = "Range")]
    [AmisNumberField(Min = 0, Max = 100)]
    public int DefaultScore { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [Display(Name = "Tags", ResourceType = typeof(DisplayResources))]
    [LocalizedDescription(
        "为题目添加相关标签，便于分类和搜索",
        ResourceKey = "Description.Question.Tags",
        ResourceType = typeof(ExamDisplayResources)
    )]
    [AiFieldFill(Weight = 1, Priority = 5)]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Questions/tags",
        ValueField = "id",
        LabelField = "name",
        Multiple = true,
        Searchable = true,
        Clearable = true,
        JoinValues = false,
        ExtractValue = true,
        Placeholder = "请选择或输入标签"
    )]
    public List<string>? Tags { get; set; }
}