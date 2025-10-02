using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Department;

/// <summary>
/// 部门查询数据传输对象
/// </summary>
public class DepartmentQueryDto : QueryDtoBase
{
    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// 父部门ID
    /// </summary>
    [DisplayName("父部门")]
    public long? ParentId { get; set; }

    /// <summary>
    /// 是否仅查询根部门
    /// </summary>
    [DisplayName("仅根部门")]
    public bool OnlyRootDepartments { get; set; }
}

