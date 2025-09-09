using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.MultiTenant.Models;

/// <summary>
/// 多租户配置选项
/// </summary>
public class TenantOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "MultiTenant";

    /// <summary>
    /// 是否启用多租户
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 默认租户ID
    /// </summary>
    public string DefaultTenantId { get; set; } = "default";

    /// <summary>
    /// 是否从Header解析租户
    /// </summary>
    public bool ResolveFromHeader { get; set; } = true;

    /// <summary>
    /// 租户Header名称
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    /// <summary>
    /// 是否从Query参数解析租户
    /// </summary>
    public bool ResolveFromQuery { get; set; } = false;

    /// <summary>
    /// 租户Query参数名称
    /// </summary>
    public string TenantQueryName { get; set; } = "tenantId";

    /// <summary>
    /// 是否从子域名解析租户
    /// </summary>
    public bool ResolveFromSubdomain { get; set; } = false;

    /// <summary>
    /// 是否从路径解析租户
    /// </summary>
    public bool ResolveFromPath { get; set; } = false;

    /// <summary>
    /// 租户路径前缀
    /// </summary>
    public string TenantPathPrefix { get; set; } = "tenant-";

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 是否启用租户验证
    /// </summary>
    public bool EnableTenantValidation { get; set; } = true;

    /// <summary>
    /// 是否启用租户缓存
    /// </summary>
    public bool EnableTenantCache { get; set; } = true;

    /// <summary>
    /// 租户解析失败时的处理策略
    /// </summary>
    public TenantResolutionFailureStrategy FailureStrategy { get; set; } = TenantResolutionFailureStrategy.Return404;

    /// <summary>
    /// 是否在中间件中进行租户验证
    /// 设为false时，中间件只解析租户ID，验证由各服务按需进行
    /// </summary>
    public bool ValidateInMiddleware { get; set; } = false;

    /// <summary>
    /// 是否在中间件中缓存租户信息
    /// 设为false时，中间件只设置租户ID，租户信息由各服务按需获取
    /// </summary>
    public bool CacheTenantInfoInMiddleware { get; set; } = false;

    /// <summary>
    /// 中间件跳过的路径模式列表
    /// 支持通配符匹配，如：/api/health/*、/swagger/*
    /// </summary>
    public List<string> SkipPathPatterns { get; set; } = new List<string>
    {
        "/health*",
        "/swagger*",
        "/favicon.ico",
        "/_*"
    };
}


/// <summary>
/// 租户解析失败策略
/// </summary>
public enum TenantResolutionFailureStrategy
{
    /// <summary>
    /// 使用默认租户
    /// </summary>
    [Display(Name = "使用默认租户")]
    UseDefault,
    
    /// <summary>
    /// 抛出异常
    /// </summary>
    [Display(Name = "抛出异常")]
    ThrowException,
    
    /// <summary>
    /// 返回404错误
    /// </summary>
    [Display(Name = "返回404错误")]
    Return404
}

