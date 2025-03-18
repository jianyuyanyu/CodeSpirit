using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 学生查询DTO
/// </summary>
public class StudentQueryDto : QueryDtoBase
{    
    /// <summary>
    /// 学生组ID
    /// </summary>
    [DisplayName("所属分组")]
    public long? StudentGroupId { get; set; }
    
    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("状态")]
    public bool? IsActive { get; set; }
} 