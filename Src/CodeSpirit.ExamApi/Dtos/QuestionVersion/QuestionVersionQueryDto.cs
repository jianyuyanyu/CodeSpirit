using System.ComponentModel;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ExamApi.Dtos.QuestionVersion;

/// <summary>
/// 题目版本查询DTO
/// </summary>
public class QuestionVersionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/Questions",
        ValueField = "id",
        LabelField = "content",
        Searchable = true,
        Multiple = false
    )]
    public long? QuestionId { get; set; }
    
    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本号")]
    public int? Version { get; set; }
    
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