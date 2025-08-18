using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Settings.Enums;

namespace CodeSpirit.ExamApi.Dtos.Question;

/// <summary>
/// 题目设置DTO
/// </summary>
[DisplayName("题目设置")]
public class QuestionSettingsDto
{
    /// <summary>
    /// 题目唯一性校验模式
    /// </summary>
    [DisplayName("唯一性校验模式")]
    [Required(ErrorMessage = "请选择唯一性校验模式")]
    [Description("题目的唯一性校验当前支持三种模式：不校验；全局唯一校验；分类唯一校验。选择不同的校验模式将影响题目的添加和导入，请谨慎选择。")]
    public QuestionUniquenessMode UniquenessMode { get; set; }
} 