using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.User;

/// <summary>
/// 租户用户统计DTO
/// </summary>
public class TenantUserStatisticsDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [DisplayName("租户名称")]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户显示名称
    /// </summary>
    [DisplayName("租户显示名称")]
    public string TenantDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 总用户数
    /// </summary>
    [DisplayName("总用户数")]
    public int TotalUsers { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    [DisplayName("活跃用户数")]
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 禁用用户数
    /// </summary>
    [DisplayName("禁用用户数")]
    public int InactiveUsers { get; set; }

    /// <summary>
    /// 管理员用户数
    /// </summary>
    [DisplayName("管理员用户数")]
    public int AdminUsers { get; set; }

    /// <summary>
    /// 普通用户数
    /// </summary>
    [DisplayName("普通用户数")]
    public int NormalUsers { get; set; }

    /// <summary>
    /// 本月新增用户数
    /// </summary>
    [DisplayName("本月新增用户数")]
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// 本月活跃用户数
    /// </summary>
    [DisplayName("本月活跃用户数")]
    public int ActiveUsersThisMonth { get; set; }

    /// <summary>
    /// 最后活跃时间
    /// </summary>
    [DisplayName("最后活跃时间")]
    public DateTimeOffset? LastActiveTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 用户增长率（%）
    /// </summary>
    [DisplayName("用户增长率")]
    public decimal GrowthRate { get; set; }

    /// <summary>
    /// 用户活跃度（%）
    /// </summary>
    [DisplayName("用户活跃度")]
    public decimal ActivityRate { get; set; }
} 