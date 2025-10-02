using CodeSpirit.IdentityApi.Data.Models;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Employee;

/// <summary>
/// 职工批量导入项数据传输对象
/// </summary>
public class EmployeeBatchImportItemDto
{
    /// <summary>
    /// 工号
    /// </summary>
    [Required(ErrorMessage = "工号不能为空")]
    [MaxLength(50, ErrorMessage = "工号长度不能超过50个字符")]
    [DisplayName("工号")]
    [JsonProperty("工号")]
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [MaxLength(100, ErrorMessage = "姓名长度不能超过100个字符")]
    [DisplayName("姓名")]
    [JsonProperty("姓名")]
    public string Name { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    [JsonProperty("性别")]
    public Gender Gender { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    [MaxLength(18, ErrorMessage = "身份证号码长度不能超过18个字符")]
    [DisplayName("身份证号")]
    [JsonProperty("身份证号")]
    public string IdNo { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    [DisplayName("出生日期")]
    [JsonProperty("出生日期")]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [MaxLength(15, ErrorMessage = "手机号码长度不能超过15个字符")]
    [DisplayName("手机号码")]
    [JsonProperty("手机号码")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [MaxLength(100, ErrorMessage = "电子邮箱长度不能超过100个字符")]
    [EmailAddress(ErrorMessage = "电子邮箱格式不正确")]
    [DisplayName("电子邮箱")]
    [JsonProperty("电子邮箱")]
    public string Email { get; set; }

    /// <summary>
    /// 部门编码
    /// </summary>
    [DisplayName("部门编码")]
    [JsonProperty("部门编码")]
    public string DepartmentCode { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    [MaxLength(100, ErrorMessage = "职位长度不能超过100个字符")]
    [DisplayName("职位")]
    [JsonProperty("职位")]
    public string Position { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [MaxLength(50, ErrorMessage = "职级长度不能超过50个字符")]
    [DisplayName("职级")]
    [JsonProperty("职级")]
    public string JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    [DisplayName("入职日期")]
    [JsonProperty("入职日期")]
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// 在职状态
    /// </summary>
    [DisplayName("在职状态")]
    [JsonProperty("在职状态")]
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    /// <summary>
    /// 紧急联系人
    /// </summary>
    [MaxLength(100, ErrorMessage = "紧急联系人长度不能超过100个字符")]
    [DisplayName("紧急联系人")]
    [JsonProperty("紧急联系人")]
    public string EmergencyContact { get; set; }

    /// <summary>
    /// 紧急联系电话
    /// </summary>
    [MaxLength(15, ErrorMessage = "紧急联系电话长度不能超过15个字符")]
    [DisplayName("紧急联系电话")]
    [JsonProperty("紧急联系电话")]
    public string EmergencyPhone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [MaxLength(500, ErrorMessage = "地址长度不能超过500个字符")]
    [DisplayName("地址")]
    [JsonProperty("地址")]
    public string Address { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
    [DisplayName("备注")]
    [JsonProperty("备注")]
    public string Remarks { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    [JsonProperty("是否激活")]
    public bool IsActive { get; set; } = true;
}

