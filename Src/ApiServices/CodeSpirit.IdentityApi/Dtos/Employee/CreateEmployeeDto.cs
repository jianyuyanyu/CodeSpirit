using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Employee;

/// <summary>
/// 创建职工数据传输对象
/// </summary>
[FormGroup("basic", "基本信息", "EmployeeNo,Name,Gender,IdNo,BirthDate", Order = 1, Mode = FormGroupMode.Normal)]
[FormGroup("contact", "联系方式", "PhoneNumber,Email,Address", Order = 2, Mode = FormGroupMode.Normal)]
[FormGroup("work", "工作信息", "DepartmentId,Position,JobLevel,HireDate,EmploymentStatus", Order = 3, Mode = FormGroupMode.Normal)]
[FormGroup("relation", "关联信息", "UserId", Order = 4, Mode = FormGroupMode.Normal)]
[FormGroup("emergency", "紧急联系人", "EmergencyContact,EmergencyPhone", Order = 5, Mode = FormGroupMode.Normal)]
[FormGroup("other", "其他信息", "AvatarUrl,Remarks,IsActive", Order = 6, Mode = FormGroupMode.Normal)]
public class CreateEmployeeDto
{
    /// <summary>
    /// 工号
    /// </summary>
    [Required(ErrorMessage = "工号不能为空")]
    [MaxLength(50, ErrorMessage = "工号长度不能超过50个字符")]
    [DisplayName("工号")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [MaxLength(100, ErrorMessage = "姓名长度不能超过100个字符")]
    [DisplayName("姓名")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string Name { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    [AmisFormField(ColumnRatio = 6)]
    public Gender Gender { get; set; }

    /// <summary>
    /// 身份证号码
    /// </summary>
    [MaxLength(18, ErrorMessage = "身份证号码长度不能超过18个字符")]
    [DisplayName("身份证号")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string IdNo { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    [DisplayName("出生日期")]
    [AmisDateFieldAttribute(ColumnRatio = 6)]
    public DateTime? BirthDate { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [MaxLength(15, ErrorMessage = "手机号码长度不能超过15个字符")]
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 电子邮箱
    /// </summary>
    [MaxLength(100, ErrorMessage = "电子邮箱长度不能超过100个字符")]
    [EmailAddress(ErrorMessage = "电子邮箱格式不正确")]
    [DisplayName("电子邮箱")]
    [AmisInputTextField(ColumnRatio = 6)]
    [Required]
    public string Email { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [DisplayName("部门")]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/identity/Departments/tree",
        LabelField = "name",
        ValueField = "id",
        Multiple = false,
        JoinValues = true,
        ExtractValue = false,
        Searchable = true,
        Clearable = true,
        ShowIcon = true,
        ShowOutline = true,
        HeightAuto = true,
        ColumnRatio = 12
    )]
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 职位
    /// </summary>
    [MaxLength(100, ErrorMessage = "职位长度不能超过100个字符")]
    [DisplayName("职位")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string Position { get; set; }

    /// <summary>
    /// 职级
    /// </summary>
    [MaxLength(50, ErrorMessage = "职级长度不能超过50个字符")]
    [DisplayName("职级")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string JobLevel { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    [DisplayName("入职日期")]
    [AmisDateFieldAttribute(ColumnRatio = 6)]
    public DateTime? HireDate { get; set; }

    /// <summary>
    /// 在职状态
    /// </summary>
    [DisplayName("在职状态")]
    [AmisFormField(ColumnRatio = 6)]
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [DisplayName("关联用户")]
    [Description("选择已有用户进行关联，如果为空系统会自动创建新用户")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/identity/Users",
        ValueField = "id",
        LabelField = "name",
        Multiple = false,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择关联用户或留空自动创建",
        ColumnRatio = 12
    )]
    public long? UserId { get; set; }

    /// <summary>
    /// 紧急联系人
    /// </summary>
    [MaxLength(100, ErrorMessage = "紧急联系人长度不能超过100个字符")]
    [DisplayName("紧急联系人")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string EmergencyContact { get; set; }

    /// <summary>
    /// 紧急联系电话
    /// </summary>
    [MaxLength(15, ErrorMessage = "紧急联系电话长度不能超过15个字符")]
    [Phone(ErrorMessage = "紧急联系电话格式不正确")]
    [DisplayName("紧急联系电话")]
    [AmisInputTextField(ColumnRatio = 6)]
    public string EmergencyPhone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    [MaxLength(500, ErrorMessage = "地址长度不能超过500个字符")]
    [DisplayName("地址")]
    [AmisTextareaField(ColumnRatio = 12)]
    public string Address { get; set; }

    /// <summary>
    /// 头像地址
    /// </summary>
    [MaxLength(255, ErrorMessage = "头像地址长度不能超过255个字符")]
    [DisplayName("头像")]
    [AmisInputImageField(
        Receiver = "/file/api/file/images/upload?BucketName=avatar",
        Accept = "image/png,image/jpeg,image/jpg",
        MaxSize = 2097152,
        Multiple = false,
        ColumnRatio = 12
    )]
    public string AvatarUrl { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
    [DisplayName("备注")]
    [AmisTextareaField(ColumnRatio = 12)]
    public string Remarks { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    [AmisFormField(ColumnRatio = 6)]
    public bool IsActive { get; set; } = true;
}

