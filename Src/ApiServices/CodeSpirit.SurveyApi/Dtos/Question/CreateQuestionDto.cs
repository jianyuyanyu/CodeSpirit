using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 创建题目DTO
/// </summary>
[DisplayName("创建题目")]
[AiFormFill(GlobalFillPrompt = "智能生成问卷题目")]
public class CreateQuestionDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    [AmisSelectField(Source = "/survey/api/survey/surveys/options", ValueField = "id", LabelField = "title", Clearable = false, Searchable = true)]
    public int SurveyId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目描述")]
    public string? Description { get; set; }


    /// <summary>
    /// 是否由LLM生成
    /// </summary>
    [DisplayName("LLM生成")]
    [AmisFormField(Hidden = true)]
    public bool LLMGenerated { get; set; } = false;

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool IsRequired { get; set; } = false;

    ///// <summary>
    ///// 验证规则（JSON格式）
    ///// </summary>
    //[StringLength(2000)]
    //[DisplayName("验证规则")]
    //public string? Validation { get; set; }

    ///// <summary>
    ///// 题目设置（JSON格式）
    ///// </summary>
    //[StringLength(2000)]
    //[DisplayName("题目设置")]
    //public string? Settings { get; set; }

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    [StringLength(2000)]
    [DisplayName("验证规则")]
    [AmisFormField(Type = "json-editor")]
    public string? Validation { get; set; }

    /// <summary>
    /// 评分最小值（评分题专用）
    /// </summary>
    [DisplayName("最小值")]
    [AmisNumberField(Min = 1, Max = 10, VisibleOn = "this.type === 5")]
    public int? RatingMin { get; set; } = 1;

    /// <summary>
    /// 评分最大值（评分题专用）
    /// </summary>
    [DisplayName("最大值")]
    [AmisNumberField(Min = 1, Max = 10, VisibleOn = "this.type === 5")]
    public int? RatingMax { get; set; } = 5;

    /// <summary>
    /// 评分步长（评分题专用）
    /// </summary>
    [DisplayName("步长")]
    [AmisNumberField(Min = 0.1, Max = 1, Step = 0.1, VisibleOn = "this.type === 5")]
    public double? RatingStep { get; set; } = 1;

    /// <summary>
    /// 数字最小值（数字题专用）
    /// </summary>
    [DisplayName("最小值")]
    [AmisNumberField(VisibleOn = "this.type === 4")]
    public double? NumberMin { get; set; }

    /// <summary>
    /// 数字最大值（数字题专用）
    /// </summary>
    [DisplayName("最大值")]
    [AmisNumberField(VisibleOn = "this.type === 4")]
    public double? NumberMax { get; set; }

    /// <summary>
    /// 数字步长（数字题专用）
    /// </summary>
    [DisplayName("步长")]
    [AmisNumberField(Min = 0.01, Step = 0.01, VisibleOn = "this.type === 4")]
    public double? NumberStep { get; set; }

    /// <summary>
    /// 文本最小长度（文本题专用）
    /// </summary>
    [DisplayName("最小长度")]
    [AmisNumberField(Min = 0, VisibleOn = "this.type === 3 || this.type === 9")]
    public int? TextMinLength { get; set; }

    /// <summary>
    /// 文本最大长度（文本题专用）
    /// </summary>
    [DisplayName("最大长度")]
    [AmisNumberField(Min = 1, VisibleOn = "this.type === 3 || this.type === 9")]
    public int? TextMaxLength { get; set; }

    /// <summary>
    /// 文本输入模式（文本题专用）
    /// </summary>
    [DisplayName("输入模式")]
    [AmisSelectField(VisibleOn = "this.type === 3",AdditionalConfig = "{\"options\":[{\"label\":\"普通文本\",\"value\":\"text\"},{\"label\":\"邮箱\",\"value\":\"email\"},{\"label\":\"电话\",\"value\":\"tel\"},{\"label\":\"网址\",\"value\":\"url\"},{\"label\":\"密码\",\"value\":\"password\"}]}")]
    public string? TextInputMode { get; set; } = "text";

    /// <summary>
    /// 日期格式（日期题专用）
    /// </summary>
    [DisplayName("日期格式")]
    [AmisSelectField(VisibleOn = "this.type === 6 || this.type === 8",AdditionalConfig = "{\"options\":[{\"label\":\"YYYY-MM-DD\",\"value\":\"YYYY-MM-DD\"},{\"label\":\"YYYY/MM/DD\",\"value\":\"YYYY/MM/DD\"},{\"label\":\"MM/DD/YYYY\",\"value\":\"MM/DD/YYYY\"},{\"label\":\"DD/MM/YYYY\",\"value\":\"DD/MM/YYYY\"}]}")]
    public string? DateFormat { get; set; } = "YYYY-MM-DD";

    /// <summary>
    /// 时间格式（时间题专用）
    /// </summary>
    [DisplayName("时间格式")]
    [AmisSelectField(VisibleOn = "this.type === 7 || this.type === 8",AdditionalConfig = "{\"options\":[{\"label\":\"HH:mm\",\"value\":\"HH:mm\"},{\"label\":\"HH:mm:ss\",\"value\":\"HH:mm:ss\"},{\"label\":\"hh:mm A\",\"value\":\"hh:mm A\"}]}")]
    public string? TimeFormat { get; set; } = "HH:mm";

    /// <summary>
    /// 矩阵行选项（矩阵题专用）
    /// </summary>
    [DisplayName("行选项")]
    [AmisArrayField(
        Items = "{\"type\":\"input-text\",\"placeholder\":\"请输入行选项\"}",
        Addable = true,
        Removable = true,
        Draggable = true,
        AddButtonText = "添加行",
        VisibleOn = "this.type === 10"
    )]
    public List<string>? MatrixRows { get; set; } = new();

    /// <summary>
    /// 矩阵列选项（矩阵题专用）
    /// </summary>
    [DisplayName("列选项")]
    [AmisArrayField(
        Items = "{\"type\":\"input-text\",\"placeholder\":\"请输入列选项\"}",
        Addable = true,
        Removable = true,
        Draggable = true,
        AddButtonText = "添加列",
        VisibleOn = "this.type === 10"
    )]
    public List<string>? MatrixColumns { get; set; } = new();

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    [AmisTableField(
        Addable = true,
        Removable = true,
        Draggable = true,
        AddButtonText = "添加选项",
        ShowIndex = true,
        VisibleOn = "this.type === 1 || this.type === 2 || this.type === 11"
    )]
    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// 创建题目选项DTO
/// </summary>
[DisplayName("创建题目选项")]
public class CreateQuestionOptionDto
{
    /// <summary>
    /// 选项文本
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("选项文本")]
    [AmisFormField(Type = "input-text", Placeholder = "请输入选项文本")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    [StringLength(200)]
    [DisplayName("选项值")]
    [AmisFormField(Type = "input-text", Placeholder = "请输入选项值（可选）")]
    public string? Value { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    [AmisFormField(Hidden = true)]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否为其他选项
    /// </summary>
    [DisplayName("其他选项")]
    [AmisSwitchField]
    public bool IsOther { get; set; } = false;
}
