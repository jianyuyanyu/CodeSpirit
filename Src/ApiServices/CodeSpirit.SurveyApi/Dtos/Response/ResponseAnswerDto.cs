using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.SurveyApi.Dtos.Response;

/// <summary>
/// 回答详情DTO
/// </summary>
[DisplayName("回答详情")]
public class ResponseAnswerDto
{
    /// <summary>
    /// 回答详情ID
    /// </summary>
    [DisplayName("回答详情ID")]
    public int Id { get; set; }

    /// <summary>
    /// 回答ID
    /// </summary>
    [DisplayName("回答ID")]
    public int ResponseId { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public int QuestionId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [DisplayName("题目标题")]
    [AggregateField(dataSource: "/api/questions/{value}.title")]
    public string? QuestionTitle { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    [AggregateField(dataSource: "/api/questions/{value}.type")]
    public string? QuestionType { get; set; }

    /// <summary>
    /// 回答文本
    /// </summary>
    [DisplayName("回答文本")]
    public string? AnswerText { get; set; }

    /// <summary>
    /// 回答值
    /// </summary>
    [DisplayName("回答值")]
    public string? AnswerValue { get; set; }

    /// <summary>
    /// 回答时间
    /// </summary>
    [DisplayName("回答时间")]
    public DateTime AnsweredAt { get; set; }
}
