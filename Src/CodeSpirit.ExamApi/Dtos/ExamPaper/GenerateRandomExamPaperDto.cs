using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.ExamPaper;

/// <summary>
/// 随机试卷生成DTO
/// </summary>
[DisplayName("生成随机试卷")]
public class GenerateRandomExamPaperDto
{
    /// <summary>
    /// 试卷名称
    /// </summary>
    [DisplayName("试卷名称")]
    [Required(ErrorMessage = "试卷名称不能为空")]
    [StringLength(100, ErrorMessage = "试卷名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 试卷描述
    /// </summary>
    [DisplayName("试卷描述")]
    [StringLength(500, ErrorMessage = "试卷描述不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    [Required(ErrorMessage = "总分不能为空")]
    [Range(1, 1000, ErrorMessage = "总分必须在1-1000之间")]
    public int TotalScore { get; set; } = 100;

    /// <summary>
    /// 及格分数
    /// </summary>
    [DisplayName("及格分数")]
    [Range(0, 1000, ErrorMessage = "及格分数必须在0-1000之间")]
    public int PassScore { get; set; } = 60;

    /// <summary>
    /// 时长（分钟）
    /// </summary>
    [DisplayName("时长（分钟）")]
    [Range(1, 1440, ErrorMessage = "考试时长必须在1-1440分钟之间")]
    public int Duration { get; set; } = 120;

    /// <summary>
    /// 题型分布规则
    /// </summary>
    [DisplayName("题型分布规则")]
    [Required(ErrorMessage = "题型分布规则不能为空")]
    [AmisTableField(Addable = true, Removable = true, Draggable = true)]
    public List<QuestionTypeRule> QuestionTypeRules { get; set; } = [];

    /// <summary>
    /// 难度分布规则
    /// </summary>
    [DisplayName("难度分布规则")]
    [AmisTableField(Addable = true, Removable = true, Draggable = true)]
    public List<DifficultyRule>? DifficultyRules { get; set; }

    ///// <summary>
    ///// 知识点分布规则
    ///// </summary>
    //[DisplayName("知识点分布规则")]
    //public List<KnowledgePointRule>? KnowledgePointRules { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("题目标签")]
    [AmisArrayField(
        Items = "{ \"type\":\"input-text\" }",
        Addable = true,
        Removable = true,
        Draggable = true,
        MaxLength = 5
    )]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 分类ID限制
    /// </summary>
    [DisplayName("题目分类限制")]
    [AmisTreeSelectField(
        DataSource = "${ROOT_API}/api/exam/QuestionCategories/tree",
        Multiple = true,
        Cascade = true,
        ShowOutline = true,
        Required = true,
        LabelField = "name",
        ValueField = "id",
        JoinValues = false,
        ExtractValue = true
    )]
    public List<long> CategoryIds { get; set; }
}
