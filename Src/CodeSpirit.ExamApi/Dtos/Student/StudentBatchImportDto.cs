using CodeSpirit.Amis.Attributes.FormFields;
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
    [Required(ErrorMessage = "学号/工号不能为空")]
    [DisplayName("学号/工号")]
    [JsonProperty("学号/工号")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    [JsonProperty("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;
} 