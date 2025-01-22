using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public class UserDto
{
    public string Id { get; set; }

    [DisplayName("姓名")]
    public string Name { get; set; }

    [DisplayName("头像")]
    public string AvatarUrl { get; set; }

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

    [DisplayName("身份证")]
    public string IdNo { get; set; }


    [DisplayName("最后登录时间")]
    public DateTimeOffset? LastLoginTime { get; set; }

    [DisplayName("角色")]
    public List<string> Roles { get; set; }

    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; }

    [DisplayName("性别")]
    public Gender Gender { get; set; }

    public bool LockoutEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }
}
