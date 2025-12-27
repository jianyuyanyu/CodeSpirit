using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto
{
    public long Id { get; set; }

    [Display(Name = nameof(Name), ResourceType = typeof(IdentityDisplayResources))]
    [TplColumn(template: "${name}")]
    [Badge(VisibleOn = "accessFailedCount > 0", Level = "warning", Mode = "text", Text = "${accessFailedCount}")]
    public string Name { get; set; }

    [Display(Name = nameof(AvatarUrl), ResourceType = typeof(IdentityDisplayResources))]
    [AvatarColumn(Text = "${name}",Src = "${avatarUrl}")]
    [Badge(Animation = true, VisibleOn = "isActive", Level = "info")]
    public string AvatarUrl { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = nameof(UserName), ResourceType = typeof(IdentityDisplayResources))]
    public string UserName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = nameof(Email), ResourceType = typeof(IdentityDisplayResources))]
    public string Email { get; set; }

    [Display(Name = nameof(IsActive), ResourceType = typeof(IdentityDisplayResources))]
    [AmisColumn(Type = "switch")]
    public bool IsActive { get; set; }

    [Display(Name = nameof(IdNo), ResourceType = typeof(IdentityDisplayResources))]
    public string IdNo { get; set; }

    [Display(Name = nameof(LastLoginTime), ResourceType = typeof(IdentityDisplayResources))]
    [DateColumn(FromNow = true)]
    public DateTimeOffset? LastLoginTime { get; set; }

    [Display(Name = nameof(Roles), ResourceType = typeof(IdentityDisplayResources))]
    public List<string> Roles { get; set; }

    [Display(Name = nameof(PhoneNumber), ResourceType = typeof(IdentityDisplayResources))]
    public string PhoneNumber { get; set; }

    [Display(Name = nameof(Gender), ResourceType = typeof(IdentityDisplayResources))]
    public Gender Gender { get; set; }

    [Display(Name = nameof(LockoutEnabled), ResourceType = typeof(IdentityDisplayResources))]
    public bool LockoutEnabled { get; set; }

    [Display(Name = nameof(LockoutEnd), ResourceType = typeof(IdentityDisplayResources))]
    public DateTimeOffset? LockoutEnd { get; set; }

    [Display(Name = nameof(AccessFailedCount), ResourceType = typeof(IdentityDisplayResources))]
    public int AccessFailedCount { get; set; }
}
