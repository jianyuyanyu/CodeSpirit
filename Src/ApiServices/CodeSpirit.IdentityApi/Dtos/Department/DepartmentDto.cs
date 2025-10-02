using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Department;

/// <summary>
/// 部门数据传输对象
/// </summary>
public class DepartmentDto
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [DisplayName("部门名称")]
    public string Name { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [DisplayName("部门编码")]
    public string Code { get; set; }

    /// <summary>
    /// 父部门ID
    /// </summary>
    [DisplayName("父部门ID")]
    [AmisColumn(Hidden = true)]
    public long? ParentId { get; set; }

    /// <summary>
    /// 父部门名称
    /// </summary>
    [DisplayName("父部门")]
    public string ParentName { get; set; }

    /// <summary>
    /// 部门负责人ID
    /// </summary>
    [DisplayName("负责人ID")]
    [AmisColumn(Hidden = true)]
    public long? ManagerId { get; set; }

    /// <summary>
    /// 部门负责人姓名
    /// </summary>
    [DisplayName("负责人")]
    public string ManagerName { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    [DisplayName("排序号")]
    public int SortOrder { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 子部门集合
    /// </summary>
    [IgnoreColumn]
    public List<DepartmentDto> Children { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    [DateColumn(FromNow = true)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(FromNow = true)]
    public DateTime? UpdatedAt { get; set; }
}

