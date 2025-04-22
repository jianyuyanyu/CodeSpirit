using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Settings;

/// <summary>
/// 考试系统设置
/// </summary>
public class ExamSettings
{
    /// <summary>
    /// 是否启用AI题目生成
    /// </summary>
    [DisplayName("启用AI题目生成")]
    [Description("是否启用AI大模型自动生成题目功能")]
    public bool EnableAIQuestionGeneration { get; set; } = true;

    /// <summary>
    /// 单次生成题目数量上限
    /// </summary>
    [DisplayName("单次生成题目数量上限")]
    [Range(1, 100)]
    [Description("单次最多可生成的题目数量")]
    public int MaxGeneratedQuestionsPerRequest { get; set; } = 10;

    /// <summary>
    /// 保存AI生成题目时是否自动审核
    /// </summary>
    [DisplayName("自动审核AI生成题目")]
    [Description("保存AI生成的题目时是否自动标记为已审核")]
    public bool AutoApproveGeneratedQuestions { get; set; } = false;
} 