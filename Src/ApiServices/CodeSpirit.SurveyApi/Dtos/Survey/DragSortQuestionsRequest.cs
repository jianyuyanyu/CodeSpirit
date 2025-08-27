using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 拖拽排序题目请求
/// </summary>
[DisplayName("拖拽排序题目")]
public class DragSortQuestionsRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 题目排序列表
    /// </summary>
    [Required]
    [DisplayName("题目排序")]
    public List<QuestionSortItem> Questions { get; set; } = new();
}

/// <summary>
/// 题目排序项
/// </summary>
[DisplayName("题目排序项")]
public class QuestionSortItem
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    [DisplayName("题目ID")]
    public int Id { get; set; }

    /// <summary>
    /// 新的排序索引
    /// </summary>
    [Required]
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }
}
