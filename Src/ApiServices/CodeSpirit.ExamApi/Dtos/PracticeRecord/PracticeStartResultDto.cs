using System.ComponentModel;
using CodeSpirit.ExamApi.Dtos.Question;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习开始结果DTO
/// </summary>
public class PracticeStartResultDto
{
    /// <summary>
    /// 练习记录ID
    /// </summary>
    [DisplayName("练习记录ID")]
    public long RecordId { get; set; }

    /// <summary>
    /// 练习设置ID
    /// </summary>
    [DisplayName("练习设置ID")]
    public long PracticeId { get; set; }

    /// <summary>
    /// 练习名称
    /// </summary>
    [DisplayName("练习名称")]
    public string PracticeName { get; set; } = string.Empty;

    /// <summary>
    /// 练习描述
    /// </summary>
    [DisplayName("练习描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 题目列表
    /// </summary>
    [DisplayName("题目列表")]
    public List<QuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 是否允许查看答案
    /// </summary>
    [DisplayName("是否允许查看答案")]
    public bool AllowViewAnswer { get; set; }

    /// <summary>
    /// 是否允许查看解析
    /// </summary>
    [DisplayName("是否允许查看解析")]
    public bool AllowViewExplanation { get; set; }

    /// <summary>
    /// 练习模式
    /// </summary>
    [DisplayName("练习模式")]
    public int PracticeMode { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [DisplayName("总分")]
    public decimal TotalScore { get; set; }
} 