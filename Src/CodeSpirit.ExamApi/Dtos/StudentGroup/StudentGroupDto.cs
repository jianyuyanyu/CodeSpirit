using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.StudentGroup;

/// <summary>
/// 考生组DTO
/// </summary>
public class StudentGroupDto
{
    /// <summary>
    /// ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 分组名称
    /// </summary>
    [DisplayName("分组名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分组描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 考生数量
    /// </summary>
    [DisplayName("考生数量")]
    public int StudentCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [DisplayName("更新人")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string? UpdatedBy { get; set; }
} 