using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.IdentityApi.Data.Models;
using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class UserDto
{
    public long Id { get; set; }

    [DisplayName("姓名")]
    [TplColumn(template: "${name}")]
    [Badge(VisibleOn = "accessFailedCount > 0", Level = "warning", Mode = "text", Text = "${accessFailedCount}")]
    public string Name { get; set; }

    [DisplayName("头像")]
    [AvatarColumn(Text = "${name}")]
    [Badge(Animation = true, VisibleOn = "isActive", Level = "info")]
    public string AvatarUrl { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [DisplayName("用户名")]
    public string UserName { get; set; }

    [Required]
    [EmailAddress]
    [DisplayName("电子邮箱")]
    public string Email { get; set; }

    [DisplayName("是否激活")]
    public bool IsActive { get; set; }

    [DisplayName("身份证")]
    public string IdNo { get; set; }


    [DisplayName("最后登录时间")]
    [DateColumn(FromNow = true)]
    public DateTimeOffset? LastLoginTime { get; set; }

    [DisplayName("角色")]
    public List<string> Roles { get; set; }

    [DisplayName("手机号码")]
    public string PhoneNumber { get; set; }

    [DisplayName("性别")]
    public Gender Gender { get; set; }

    [DisplayName("启用锁定")]
    public bool LockoutEnabled { get; set; }

    [DisplayName("锁定结束时间")]
    public DateTimeOffset? LockoutEnd { get; set; }

    [DisplayName("访问失败次数")]
    public int AccessFailedCount { get; set; }
}
