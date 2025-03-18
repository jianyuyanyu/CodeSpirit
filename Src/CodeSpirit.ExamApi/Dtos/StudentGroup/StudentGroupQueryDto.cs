using System.ComponentModel;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ExamApi.Dtos.StudentGroup;

/// <summary>
/// 考生组查询DTO
/// </summary>
public class StudentGroupQueryDto : QueryDtoBase
{
    /// <summary>
    /// 关键词（用于搜索名称和描述）
    /// </summary>
    [DisplayName("关键词")]
    public string? Keywords { get; set; }
} 