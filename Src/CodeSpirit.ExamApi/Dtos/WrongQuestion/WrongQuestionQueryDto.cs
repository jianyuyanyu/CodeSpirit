using System.ComponentModel;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ExamApi.Dtos.WrongQuestion;

/// <summary>
/// 错题查询DTO
/// </summary>
public class WrongQuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Students",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false
    )]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// 标签
    /// </summary>
    [DisplayName("标签")]
    public string? Tags { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
} 