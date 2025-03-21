using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 学生DTO
/// </summary>
public class StudentDto
{
    /// <summary>
    /// ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID（关联到身份系统）
    /// </summary>
    [DisplayName("用户ID")]
    public long UserId { get; set; }

    /// <summary>
    /// 学生姓名
    /// </summary>
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// 身份证号码
    /// </summary>
    [DisplayName("身份证号码")]

    public string IdNo { get; set; }
    /// <summary>
    /// 性别
    /// </summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>
    /// 准考证
    /// </summary>
    [DisplayName("准考证")]
    public string AdmissionTicket { get; set; }
    /// <summary>
    /// 学生学号/工号
    /// </summary>
    [DisplayName("学号/工号")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    [AmisColumn(QuickEdit = false)]
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// 分组列表
    /// </summary>
    [DisplayName("分组列表")]
    public List<string> StudentGroups { get; set; }

    /// <summary>
    /// 分组ID列表
    /// </summary>
    [IgnoreColumn]
    public List<long> StudentGroupIds { get; set; } = new List<long>();

}