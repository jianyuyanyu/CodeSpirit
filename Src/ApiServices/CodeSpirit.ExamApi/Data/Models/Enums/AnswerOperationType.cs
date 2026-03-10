using System.ComponentModel.DataAnnotations;
using CodeSpirit.ExamApi.Resources;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 答题操作类型
/// </summary>
public enum AnswerOperationType
{
    /// <summary>
    /// 提交答案
    /// </summary>
    [Display(Name = "AnswerOperationType.Submit", ResourceType = typeof(ExamDisplayResources))]
    Submit = 1,

    /// <summary>
    /// 修改答案
    /// </summary>
    [Display(Name = "AnswerOperationType.Modify", ResourceType = typeof(ExamDisplayResources))]
    Modify = 2
}
