using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class UserDto
{
    public string Id { get; set; }

    [DisplayName("姓名")]
    public string Name { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [DisplayName("用户名")]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [DisplayName("电子邮箱")]
    public string Email { get; set; }

    [DisplayName("是否激活")]
    public bool IsActive { get; set; }

    public string IdNo { get; set; }
    public string AvatarUrl { get; set; }
    public DateTimeOffset? LastLoginTime { get; set; }
    public List<string> Roles { get; set; }

    // 新增字段
    public string PhoneNumber { get; set; }
    public Gender Gender { get; set; }
}
