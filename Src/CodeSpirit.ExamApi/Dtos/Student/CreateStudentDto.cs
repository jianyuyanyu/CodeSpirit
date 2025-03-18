namespace CodeSpirit.ExamApi.Dtos.Student;

/// <summary>
/// 创建学生DTO
/// </summary>
public class CreateStudentDto
{
    /// <summary>
    /// 用户ID（关联到身份系统）
    /// </summary>
    [Required(ErrorMessage = "用户ID不能为空")]
    [DisplayName("用户ID")]
    public long UserId { get; set; }

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
    [Required(ErrorMessage = "学号/工号不能为空")]
    [StringLength(50, ErrorMessage = "学号/工号长度不能超过50个字符")]
    [DisplayName("学号/工号")]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required(ErrorMessage = "手机号码不能为空")]
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
    [Phone(ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 是否激活
    /// </summary>
    [DisplayName("是否激活")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 学生组ID列表
    /// </summary>
    [DisplayName("所属分组")]
    public List<long> StudentGroupIds { get; set; } = new List<long>();
} 