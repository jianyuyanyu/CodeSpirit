using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.MultiTenant.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Data.Models;

/// <summary>
/// 职工信息
/// </summary>
public class Employee : IFullAuditable, IMultiTenant, IIsActive
{
    /// <summary>
    /// 职工ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    [MaxLength(18)]
    public string IdNo { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [MaxLength(15)]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 所属部门
    /// </summary>
    public Department Department { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    [MaxLength(100)]
    public string Position { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [MaxLength(50)]
    public string JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// 离职日期
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// 在职状态
    /// </summary>
    public EmploymentStatus EmploymentStatus { get; set; }

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 关联的用户账号
    /// </summary>
    public ApplicationUser User { get; set; }

    /// <summary>
    /// 紧急联系人
    /// </summary>
    [MaxLength(100)]
    public string EmergencyContact { get; set; }

    /// <summary>
    /// 紧急联系电话
    /// </summary>
    [MaxLength(15)]
    public string EmergencyPhone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [MaxLength(500)]
    public string Address { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(1000)]
    public string Remarks { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [MaxLength(255)]
    [DataType(DataType.ImageUrl)]
    public string AvatarUrl { get; set; }

    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}

