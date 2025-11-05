using System.ComponentModel;
using Newtonsoft.Json;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using CodeSpirit.PathfinderApi.Dtos;

namespace CodeSpirit.PathfinderApi.Dtos.Goal;

/// <summary>
/// 创建目标数据传输对象（支持AI优化和可行性评估）
/// </summary>
[AiFormFill(
    TriggerField = nameof(Description),
    CustomPromptTemplate = @"你是一个目标管理专家，需要完成以下任务：

**任务1：优化目标描述**
用户输入：{Description}
- 使目标更明确、具体、可执行
- 补充必要的细节
- 建议合理的完成日期

**任务2：提取目标信息**
- 从目标描述中提取简短标题（10字以内）
- 识别目标类型（个人成长/工作项目/生活管理/学习提升/健康管理等）

**任务3：可行性评估**
从三个维度评分（0-10分）：
- 明确性：是否清晰明确？有具体成功标准？
- 可执行性：能否拆解为具体行动？在能力范围内？
- 完整性：包含时间约束、范围、预期结果？

评分标准：7-10分可直接执行，4-6分需补充细节，0-3分需重新定义。
",
    UseIndependentLLM = true,
    LLMSettingsKey = "AiFormFillLLM",
    DisableThinking = true,
    ResponseFormatType = "json_object")]
public class CreateGoalDto
{
    /// <summary>
    /// 目标描述
    /// </summary>
    [Required(ErrorMessage = "目标描述不能为空")]
    [MaxLength(2000, ErrorMessage = "目标描述不能超过2000字符")]
    [DisplayName("目标描述")]
    [AiFieldFill(Priority = 1, CustomDescription = "明确、具体、可执行的目标描述")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 目标日期
    /// </summary>
    [DisplayName("目标日期")]
    [AiFieldFill(Priority = 2, CustomDescription = "基于目标复杂度建议的合理完成日期")]
    public DateTime? TargetDate { get; set; }
    
    /// <summary>
    /// 目标标题（AI自动提取）
    /// </summary>
    [DisplayName("目标标题")]
    [MaxLength(200)]
    [AiFieldFill(Priority = 9, CustomDescription = "从目标描述中提取的简短标题（10字以内）")]
    [Required(ErrorMessage = "目标标题不能为空")]
    public required string Title { get; set; }
    
    /// <summary>
    /// 目标类型（AI自动识别）
    /// </summary>
    [DisplayName("目标类型")]
    [MaxLength(50)]
    [AiFieldFill(Priority = 10, CustomDescription = "目标类型（个人成长/工作项目/生活管理/学习提升/健康管理等）")]
    [Required]
    public required string Category { get; set; }
    
    #region AI 评估结果（仅AI填充响应时返回，创建时忽略）
    
    /// <summary>
    /// 明确性评分 (0-10)
    /// </summary>
    [DisplayName("明确性评分")]
    [JsonProperty("clarityScore")]
    [AiFieldFill(
        Priority = 3,
        CustomDescription = "目标的明确性评分（0-10分）：目标是否清晰明确，是否有具体的成功标准")]
    [AmisFormField(
        Type = "static-progress",
        Label = "📝 明确性评分",
        AdditionalConfig = @"{
            ""name"": ""clarityScorePercent"",
            ""map"": {
                ""70"": ""bg-success"",
                ""40"": ""bg-warning"",
                ""*"": ""bg-danger""
            },
            ""showLabel"": true,
            ""visibleOn"": ""clarityScorePercent > 0"",
            ""placeholder"": ""暂无评分""
        }")]
    public int? ClarityScore { get; set; }
    
    /// <summary>
    /// 明确性评分百分比（用于进度条显示）
    /// </summary>
    [AmisFormField(
        Type = "formula",
        AdditionalConfig = @"{
            ""name"": ""clarityScorePercent"",
            ""formula"": ""clarityScore * 10"",
            ""initSet"": false
        }")]
    [AiFieldFill(Enabled = false)]
    public int? ClarityScorePercent { get; set; }
    
    /// <summary>
    /// 可执行性评分 (0-10)
    /// </summary>
    [DisplayName("可执行性评分")]
    [JsonProperty("executabilityScore")]
    [AiFieldFill(
        Priority = 4,
        CustomDescription = "目标的可执行性评分（0-10分）：目标是否可以拆解为具体行动，是否在能力范围内")]
    [AmisFormField(
        Type = "static-progress",
        Label = "⚙️ 可执行性评分",
        AdditionalConfig = @"{
            ""name"": ""executabilityScorePercent"",
            ""map"": {
                ""70"": ""bg-success"",
                ""40"": ""bg-warning"",
                ""*"": ""bg-danger""
            },
            ""showLabel"": true,
            ""visibleOn"": ""executabilityScorePercent > 0"",
            ""placeholder"": ""暂无评分""
        }")]
    public int? ExecutabilityScore { get; set; }
    
    /// <summary>
    /// 可执行性评分百分比（用于进度条显示）
    /// </summary>
    [AmisFormField(
        Type = "formula",
        AdditionalConfig = @"{
            ""name"": ""executabilityScorePercent"",
            ""formula"": ""executabilityScore * 10"",
            ""initSet"": false
        }")]
    [AiFieldFill(Enabled = false)]
    public int? ExecutabilityScorePercent { get; set; }
    
    /// <summary>
    /// 完整性评分 (0-10)
    /// </summary>
    [DisplayName("完整性评分")]
    [JsonProperty("completenessScore")]
    [AiFieldFill(
        Priority = 5,
        CustomDescription = "目标的完整性评分（0-10分）：是否包含时间约束、范围边界、预期结果")]
    [AmisFormField(
        Type = "static-progress",
        Label = "✅ 完整性评分",
        AdditionalConfig = @"{
            ""name"": ""completenessScorePercent"",
            ""map"": {
                ""70"": ""bg-success"",
                ""40"": ""bg-warning"",
                ""*"": ""bg-danger""
            },
            ""showLabel"": true,
            ""visibleOn"": ""completenessScorePercent > 0"",
            ""placeholder"": ""暂无评分""
        }")]
    public int? CompletenessScore { get; set; }
    
    /// <summary>
    /// 完整性评分百分比（用于进度条显示）
    /// </summary>
    [AmisFormField(
        Type = "formula",
        AdditionalConfig = @"{
            ""name"": ""completenessScorePercent"",
            ""formula"": ""completenessScore * 10"",
            ""initSet"": false
        }")]
    [AiFieldFill(Enabled = false)]
    public int? CompletenessScorePercent { get; set; }
    
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    [DisplayName("综合评分")]
    [JsonIgnore]
    [AmisFormField(
        Type = "static-progress",
        Label = "🎯 综合评分",
        AdditionalConfig = @"{
            ""name"": ""overallScorePercent"",
            ""map"": {
                ""70"": ""bg-success"",
                ""40"": ""bg-warning"",
                ""*"": ""bg-danger""
            },
            ""showLabel"": true,
            ""visibleOn"": ""overallScorePercent > 0"",
            ""placeholder"": ""暂无评分"",
            ""strokeWidth"": 15,
            ""gapDegree"": 0
        }")]
    public decimal? OverallScore => 
        ClarityScore.HasValue && ExecutabilityScore.HasValue && CompletenessScore.HasValue
            ? Math.Round((ClarityScore.Value + ExecutabilityScore.Value + CompletenessScore.Value) / 3.0m, 1)
            : null;
    
    /// <summary>
    /// 综合评分百分比（用于进度条显示）
    /// </summary>
    [AmisFormField(
        Type = "formula",
        AdditionalConfig = @"{
            ""name"": ""overallScorePercent"",
            ""formula"": ""${ROUND((clarityScore + executabilityScore + completenessScore) / 3 * 10)}"",
            ""initSet"": false
        }")]
    [AiFieldFill(Enabled = false)]
    public int? OverallScorePercent { get; set; }
    
    /// <summary>
    /// 是否可行（通过公式计算：综合评分>=7）
    /// </summary>
    [AmisFormField(
        Type = "formula",
        AdditionalConfig = @"{
            ""name"": ""isFeasible"",
            ""formula"": ""${clarityScore >= 7 && executabilityScore >= 7 && completenessScore >= 7}"",
            ""initSet"": false
        }")]
    [AiFieldFill(Enabled = false)]
    [JsonIgnore]
    public bool? IsFeasibleFormula { get; set; }
    
    /// <summary>
    /// 是否可行（综合评分>=7）- 显示字段
    /// </summary>
    [DisplayName("是否可行")]
    [AmisFormField(
        Type = "static-tpl",
        Label = "评估结果",
        Required = false,
        AdditionalConfig = @"{
            ""tpl"": ""<span class='antd-label ${isFeasible ? \""antd-label-success\"" : \""antd-label-warning\""}' style='padding: 4px 12px; border-radius: 4px; font-weight: 500;'>${isFeasible ? \""✅ 评估通过\"" : \""⚠️ 需要完善\""}</span>"",
            ""visibleOn"": ""clarityScore != null""
        }")]
    [AiFieldFill(Enabled = false)]
    [JsonIgnore]
    public bool? IsFeasible { get; set; }

    /// <summary>
    /// 发现的问题列表
    /// </summary>
    [DisplayName("发现的问题")]
    [JsonProperty("issues")]
    [AiFieldFill(
        Priority = 6,
        CustomDescription = "目标中发现的具体问题（数组），如表述不清、缺少细节、范围过大等")]
    [AmisFormField(
        Type = "static-list",
        Label = "⚠️ 发现的问题",
        AdditionalConfig = @"{
            ""listItem"": {
                ""title"": ""${item}"",
                ""titleClassName"": ""text-danger""
            },
            ""visibleOn"": ""${issues && issues.length > 0}"",
            ""placeholder"": ""<span class='text-muted'>暂无问题</span>""
        }")]
    public List<string>? Issues { get; set; }
    
    /// <summary>
    /// 需要澄清的问题列表
    /// </summary>
    [DisplayName("需要澄清的问题")]
    [JsonProperty("clarificationQuestions")]
    [AiFieldFill(
        Priority = 7,
        CustomDescription = "需要用户澄清的问题（数组），如时间范围、资源限制、成功标准等")]
    [AmisFormField(
        Type = "static-list",
        Label = "❓ 需要澄清的问题",
        AdditionalConfig = @"{
            ""listItem"": {
                ""title"": ""${item}"",
                ""titleClassName"": ""text-info""
            },
            ""visibleOn"": ""${clarificationQuestions && clarificationQuestions.length > 0}"",
            ""placeholder"": ""<span class='text-muted'>暂无需要澄清的问题</span>""
        }")]
    public List<string>? ClarificationQuestions { get; set; }
    
    /// <summary>
    /// 改进建议列表
    /// </summary>
    [DisplayName("改进建议")]
    [JsonProperty("suggestions")]
    [AiFieldFill(
        Priority = 8,
        CustomDescription = "具体的改进建议（数组），如如何细化目标、增加哪些约束条件、优化路径等")]
    [AmisFormField(
        Type = "static-list",
        Label = "💡 改进建议",
        AdditionalConfig = @"{
            ""listItem"": {
                ""title"": ""${item}"",
                ""titleClassName"": ""text-success""
            },
            ""visibleOn"": ""${suggestions && suggestions.length > 0}"",
            ""placeholder"": ""<span class='text-muted'>暂无建议</span>""
        }")]
    public List<string>? Suggestions { get; set; }
    
    #endregion
}

