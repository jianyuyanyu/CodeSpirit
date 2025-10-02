using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Employee;

/// <summary>
/// 职工数据传输对象
/// </summary>
public class EmployeeDto
{
    /// <summary>
    /// 职工ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    [DisplayName("工号")]
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [DisplayName("姓名")]
    [TplColumn(template: "${name}")]
    public string Name { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [DisplayName("头像")]
    [AvatarColumn(Text = "${name}", Src = "${avatarUrl}")]
    [Badge(Animation = true, VisibleOn = "isActive", Level = "info")]
    public string AvatarUrl { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    [DisplayName("身份证号")]
    public string IdNo { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    [DisplayName("出生日期")]
    [DateColumn(Format = "YYYY-MM-DD")]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [DisplayName("电子邮箱")]
    public string Email { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [DisplayName("部门ID")]
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [DisplayName("部门")]
    public string DepartmentName { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    [DisplayName("职位")]
    public string Position { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [DisplayName("职级")]
    public string JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    [DisplayName("入职日期")]
    [DateColumn(Format = "YYYY-MM-DD")]
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// 离职日期
    /// </summary>
    [DisplayName("离职日期")]
    [DateColumn(Format = "YYYY-MM-DD")]
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// 在职状态
    /// </summary>
    [DisplayName("在职状态")]
    public EmploymentStatus EmploymentStatus { get; set; }

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public long? UserId { get; set; }

    /// <summary>
    /// 关联的用户名
    /// </summary>
    [DisplayName("用户账号")]
    public string UserName { get; set; }

    /// <summary>
    /// 紧急联系人
    /// </summary>
    [DisplayName("紧急联系人")]
    public string EmergencyContact { get; set; }

    /// <summary>
    /// 紧急联系电话
    /// </summary>
    [DisplayName("紧急联系电话")]
    public string EmergencyPhone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [DisplayName("地址")]
    public string Address { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [DisplayName("备注")]
    public string Remarks { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool IsActive { get; set; }

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

