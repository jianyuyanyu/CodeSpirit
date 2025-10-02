using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.MultiTenant.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Data.Models;

/// <summary>
/// 部门信息
/// </summary>
public class Department : IFullAuditable, IMultiTenant, IIsActive
{
    /// <summary>
    /// 部门ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; }

    /// <summary>
    /// 父部门ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 父部门
    /// </summary>
    public Department Parent { get; set; }

    /// <summary>
    /// 子部门集合
    /// </summary>
    public ICollection<Department> Children { get; set; }

    /// <summary>
    /// 部门负责人ID
    /// </summary>
    public long? ManagerId { get; set; }

    /// <summary>
    /// 部门负责人
    /// </summary>
    public Employee Manager { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 部门描述
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 职工集合
    /// </summary>
    public ICollection<Employee> Employees { get; set; }

    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}

