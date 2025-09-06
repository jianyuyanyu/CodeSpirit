using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 题目重新排序请求DTO
/// </summary>
[DisplayName("题目排序")]
public class QuestionReorderRequest
{
    /// <summary>
    /// 问卷ID（隐藏字段，不允许编辑）
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    [AmisFormField(Hidden = true)]
    public int SurveyId { get; set; }

    /// <summary>
    /// 排序说明文字
    /// </summary>
    [DisplayName("温馨提示")]
    [AmisInputTextField(
        Static = true,
        AdditionalConfig = "{\"staticInputClassName\": \"text-info font-bold\"}"
    )]
    public string SortInstructions { get; set; } = string.Empty;

    /// <summary>
    /// 题目列表（仅支持拖拽排序）
    /// </summary>
    [Required]
    [DisplayName("题目列表")]
    [AmisTableField(
        Addable = false,
        Removable = false,
        Editable = false,
        Draggable = true,
        Copyable = false,
        ShowIndex = true,
        AddButtonText = ""
    )]
    public List<QuestionSortDto> Questions { get; set; } = new();
}
