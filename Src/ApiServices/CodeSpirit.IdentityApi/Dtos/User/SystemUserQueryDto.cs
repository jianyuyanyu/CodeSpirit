using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 系统平台用户查询条件DTO
/// </summary>
public class SystemUserQueryDto : QueryDtoBase
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Display(Name = nameof(UserName), ResourceType = typeof(IdentityDisplayResources))]
    public string? UserName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [Display(Name = nameof(Email), ResourceType = typeof(IdentityDisplayResources))]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [Display(Name = nameof(PhoneNumber), ResourceType = typeof(IdentityDisplayResources))]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [Display(Name = nameof(IsActive), ResourceType = typeof(IdentityDisplayResources))]
    public bool? IsActive { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Display(Name = nameof(Tenant), ResourceType = typeof(IdentityDisplayResources))]
    [AmisSelectField(
        Source = "${ROOT_API}/api/identity/Tenants/active",
        ValueField = "tenantId",
        LabelField = "displayName",
        Multiple = false,
        JoinValues = false,
        ExtractValue = true,
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择租户"
    )]
    public string? TenantId { get; set; }

    /// <summary>
    /// 创建时间开始
    /// </summary>
    [Display(Name = "CreatedAt", ResourceType = typeof(IdentityDisplayResources))]
    [AmisDatetimeFieldAttribute(
        DisplayFormat = "YYYY-MM-DD HH:mm:ss",
        Clearable = true,
        InputPlaceholder = "请选择开始时间"
    )]
    public DateTimeOffset? CreatedAtStart { get; set; }

    /// <summary>
    /// 创建时间结束
    /// </summary>
    [Display(Name = "Separator", ResourceType = typeof(IdentityDisplayResources))]
    [AmisDatetimeFieldAttribute(
        DisplayFormat = "YYYY-MM-DD HH:mm:ss",
        Clearable = true,
        InputPlaceholder = "请选择结束时间"
    )]
    public DateTimeOffset? CreatedAtEnd { get; set; }

    /// <summary>
    /// 最后登录时间开始
    /// </summary>
    [Display(Name = "LastLoginTime", ResourceType = typeof(IdentityDisplayResources))]
    [AmisDatetimeFieldAttribute(
        DisplayFormat = "YYYY-MM-DD HH:mm:ss",
        Clearable = true,
        InputPlaceholder = "请选择开始时间"
    )]
    public DateTimeOffset? LastLoginStart { get; set; }

    /// <summary>
    /// 最后登录时间结束
    /// </summary>
    [Display(Name = "Separator", ResourceType = typeof(IdentityDisplayResources))]
    [AmisDatetimeFieldAttribute(
        DisplayFormat = "YYYY-MM-DD HH:mm:ss",
        Clearable = true,
        InputPlaceholder = "请选择结束时间"
    )]
    public DateTimeOffset? LastLoginEnd { get; set; }
} 