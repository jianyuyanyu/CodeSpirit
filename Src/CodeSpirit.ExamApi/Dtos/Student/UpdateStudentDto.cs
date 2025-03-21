using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using Humanizer.Localisation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 更新学生DTO
/// </summary>
public class UpdateStudentDto
{
    /// <summary>
    /// 学生姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 学生学号/工号
    /// </summary>
    //[Required(ErrorMessage = "学号/工号不能为空")]
    [StringLength(50, ErrorMessage = "学号/工号长度不能超过50个字符")]
    [DisplayName("学号/工号")]
    public string StudentNumber { get; set; } = string.Empty;
    /// <summary>
    /// 身份证号码
    /// </summary>
    [Required(ErrorMessage = "身份证号码不能为空")]
    [MaxLength(18)]
    [DisplayName("身份证")]
    [RegularExpression(@"^(\d{15}(|\d{2}[0-9Xx])$|\d{17}([0-9Xx]))$", ErrorMessageResourceName = "InvalidIdCardNumber", ErrorMessageResourceType = typeof(Resources))]

    public string IdNo { get; set; } = string.Empty;
    /// <summary>
    /// 准考证
    /// </summary>
    //[Required(ErrorMessage = "准考证不能为空")]
    [StringLength(20, ErrorMessage = "准考证长度不能超过20个字符")]
    [DisplayName("准考证")]
    public string AdmissionTicket { get; set; } = string.Empty;
    /// <summary>
    /// 手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;
    /// <summary>
    /// 学生组ID列表
    /// </summary>
    [DisplayName("所属分组")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/exam/StudentGroups",
        ValueField = "id",
        LabelField = "name",
                    Multiple = true,
            JoinValues = false,
            ExtractValue = true,
            Searchable = true,
            Clearable = true,
            Placeholder = "请选择学生组"
    )]
    public List<long> StudentGroupIds { get; set; } = new List<long>();
} 