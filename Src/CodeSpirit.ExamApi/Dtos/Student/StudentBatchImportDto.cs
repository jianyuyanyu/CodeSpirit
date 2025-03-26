using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ExamApi.Data.Models;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 学生批量导入DTO
/// </summary>
public class StudentBatchImportDto
{
    /// <summary>
    /// 学生姓名
    /// </summary>
    [Required(ErrorMessage = "姓名不能为空")]
    [DisplayName("姓名")]
    [JsonProperty("姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 学生学号/工号
    /// </summary>
    [DisplayName("学号（工号）")]
    [JsonProperty("学号（工号）")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    //[Required(ErrorMessage = "手机号码不能为空")]
    //[Phone(ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    [JsonProperty("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 身份证号码
    /// </summary>
    [Required(ErrorMessage = "身份证号码不能为空")]
    [DisplayName("身份证号码")]
    [JsonProperty("身份证号码")]
    public string IdNo { get; set; } = string.Empty;
    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    [JsonProperty("性别")]
    public string Gender { get; set; }

    /// <summary>
    /// 准考证
    /// </summary>
    //[Required(ErrorMessage = "准考证不能为空")]
    [StringLength(20, ErrorMessage = "准考证长度不能超过20个字符")]
    [DisplayName("准考证")]
    [JsonProperty("准考证")]
    public string AdmissionTicket { get; set; } = string.Empty;

}