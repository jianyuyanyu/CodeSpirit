using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Data.Models.Enums;
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
    [Required(ErrorMessage = "题目内容不能为空")]
    [StringLength(2000, ErrorMessage = "题目内容最多2000字符")]
    [DisplayName("题目内容")]
    [AmisFormField(type: "editor", AdditionalConfig = "{\"language\":\"markdown\",\"size\":\"xl\"}", Required = true)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required(ErrorMessage = "请选择题目类型")]
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 题目难度
    /// </summary>
    [Required(ErrorMessage = "请选择题目难度")]
    [DisplayName("难度")]
    public QuestionDifficulty Difficulty { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [Required(ErrorMessage = "请添加题目选项")]
    [DisplayName("选项")]
    [Description("根据题目内容生成合适的选项，单选题通常4个选项，多选题可以有更多选项")]
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
    [Required(ErrorMessage = "请填写正确答案")]
    [StringLength(1000)]
    [DisplayName("正确答案")]
    [Description("多选题请用逗号分隔，判断题答案必须是True或False。")]
    [AiFieldFill(Weight = 3, Priority = 2)]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 解析
    /// </summary>
    [DisplayName("解析")]
    [StringLength(2000)]
    [Description("详细解释正确答案的原因，帮助学生理解知识点")]
    [AiFieldFill(Weight = 2, Priority = 3)]
    [AmisTextareaField(MaxLength = 2000, ShowCounter = true)]
    public string? Analysis { get; set; }

    /// <summary>
    /// 知识点
    /// </summary>
    [DisplayName("知识点")]
    [Description("列出该题目涉及的主要知识点，用逗号分隔")]
    [AiFieldFill(Weight = 2, Priority = 4)]
    public string? KnowledgePoints { get; set; }

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
    /// 题目分值
    /// </summary>
    [Range(0, 100, ErrorMessage = "分值范围为0-100")]
    [DisplayName("分值")]
    [AmisNumberField(Min = 0, Max = 100)]
    public int DefaultScore { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    [Description("为题目添加相关标签，便于分类和搜索")]
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