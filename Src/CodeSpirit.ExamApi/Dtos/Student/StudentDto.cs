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
    [DisplayName("状态")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreationTime { get; set; }
} 