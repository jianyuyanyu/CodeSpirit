using CodeSpirit.IdentityApi.Resources;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 租户用户统计DTO
/// </summary>
public class TenantUserStatisticsDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Display(Name = nameof(TenantId), ResourceType = typeof(IdentityDisplayResources))]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [Display(Name = nameof(TenantName), ResourceType = typeof(IdentityDisplayResources))]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户显示名称
    /// </summary>
    [Display(Name = nameof(TenantDisplayName), ResourceType = typeof(IdentityDisplayResources))]
    public string TenantDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 总用户数
    /// </summary>
    [Display(Name = nameof(TotalUsers), ResourceType = typeof(IdentityDisplayResources))]
    public int TotalUsers { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    [Display(Name = nameof(ActiveUsers), ResourceType = typeof(IdentityDisplayResources))]
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 禁用用户数
    /// </summary>
    [Display(Name = nameof(InactiveUsers), ResourceType = typeof(IdentityDisplayResources))]
    public int InactiveUsers { get; set; }

    /// <summary>
    /// 管理员用户数
    /// </summary>
    [Display(Name = nameof(AdminUsers), ResourceType = typeof(IdentityDisplayResources))]
    public int AdminUsers { get; set; }

    /// <summary>
    /// 普通用户数
    /// </summary>
    [Display(Name = nameof(NormalUsers), ResourceType = typeof(IdentityDisplayResources))]
    public int NormalUsers { get; set; }

    /// <summary>
    /// 本月新增用户数
    /// </summary>
    [Display(Name = nameof(NewUsersThisMonth), ResourceType = typeof(IdentityDisplayResources))]
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// 本月活跃用户数
    /// </summary>
    [Display(Name = nameof(ActiveUsersThisMonth), ResourceType = typeof(IdentityDisplayResources))]
    public int ActiveUsersThisMonth { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    [Display(Name = nameof(LastActiveTime), ResourceType = typeof(IdentityDisplayResources))]
    public DateTimeOffset? LastActiveTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Display(Name = nameof(CreatedAt), ResourceType = typeof(IdentityDisplayResources))]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 用户增长率（%）
    /// </summary>
    [Display(Name = nameof(GrowthRate), ResourceType = typeof(IdentityDisplayResources))]
    public decimal GrowthRate { get; set; }

    /// <summary>
    /// 用户活跃度（%）
    /// </summary>
    [Display(Name = nameof(ActivityRate), ResourceType = typeof(IdentityDisplayResources))]
    public decimal ActivityRate { get; set; }
} 