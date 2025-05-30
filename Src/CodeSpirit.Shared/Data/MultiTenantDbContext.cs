using CodeSpirit.Core;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Data;

/// <summary>
/// 多租户数据库上下文基类
/// 提供完整的多租户数据隔离功能，继承自AuditableDbContext
/// </summary>
public abstract class MultiTenantDbContext : AuditableDbContext
{
    #region 私有字段

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MultiTenantDbContext> _multiTenantLogger;
    private readonly Lazy<MultiTenantOptions> _multiTenantOptions;
    private readonly object _tenantCacheLock = new();
    private string _currentTenantId;
    private readonly ICurrentUser _currentUser;

    #endregion

    #region 属性

    /// <summary>
    /// 多租户配置选项
    /// </summary>
    protected MultiTenantOptions MultiTenantOptions 
    {
        get
        {
            try
            {
                return _multiTenantOptions.Value;
            }
            catch
            {
                // 在设计时或配置缺失时返回默认配置
                return new MultiTenantOptions { Enabled = false, DefaultTenantId = "default" };
            }
        }
    }

    /// <summary>
    /// 是否启用多租户过滤
    /// </summary>
    protected virtual bool IsMultiTenantFilterEnabled => 
        MultiTenantOptions.Enabled && 
        (DataFilter?.IsEnabled<IMultiTenant>() ?? true);

    /// <summary>
    /// 当前租户ID（缓存版本，避免重复计算）
    /// </summary>
    protected virtual string CurrentTenantId
    {
        get
        {
            if (_currentTenantId != null)
                return _currentTenantId;

            lock (_tenantCacheLock)
            {
                if (_currentTenantId == null)
                {
                    _currentTenantId = ResolveTenantId();
                }
                return _currentTenantId;
            }
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户服务</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    protected MultiTenantDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : base(options, serviceProvider, currentUser)
    {
        _httpContextAccessor = httpContextAccessor;
        _multiTenantLogger = serviceProvider.GetService<ILogger<MultiTenantDbContext>>();
        _currentUser = currentUser;

        // 延迟初始化多租户配置
        _multiTenantOptions = new Lazy<MultiTenantOptions>(() =>
        {
            var configuration = serviceProvider.GetService<IConfiguration>();
            var options = new MultiTenantOptions();
            configuration?.GetSection("MultiTenant").Bind(options);
            return options;
        });
    }

    #endregion

    #region 租户解析

    /// <summary>
    /// 解析当前租户ID
    /// 按优先级从多个来源获取租户信息
    /// </summary>
    /// <returns>租户ID</returns>
    protected virtual string ResolveTenantId()
    {
        try
        {
            // 1. 优先从CurrentUser获取（JWT Claims）
            var tenantId = _currentUser?.TenantId;
            if (!string.IsNullOrEmpty(tenantId))
            {
                _multiTenantLogger?.LogDebug("从CurrentUser获取租户ID: {TenantId}", tenantId);
                return tenantId;
            }

            // 2. 从HttpContext Items获取（多租户中间件设置）
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.Items.ContainsKey("TenantId") == true)
            {
                tenantId = httpContext.Items["TenantId"] as string;
                if (!string.IsNullOrEmpty(tenantId))
                {
                    _multiTenantLogger?.LogDebug("从HttpContext获取租户ID: {TenantId}", tenantId);
                    return tenantId;
                }
            }

            // 3. 使用默认租户ID
            tenantId = MultiTenantOptions.DefaultTenantId;
            _multiTenantLogger?.LogDebug("使用默认租户ID: {TenantId}", tenantId);
            return tenantId;
        }
        catch (Exception ex)
        {
            _multiTenantLogger?.LogError(ex, "解析租户ID时发生异常，使用默认租户ID");
            return MultiTenantOptions.DefaultTenantId;
        }
    }

    /// <summary>
    /// 清除租户ID缓存
    /// 在某些场景下（如切换用户）可能需要重新解析租户ID
    /// </summary>
    protected virtual void ClearTenantCache()
    {
        lock (_tenantCacheLock)
        {
            _currentTenantId = null;
        }
    }

    #endregion

    #region 多租户字段设置

    /// <summary>
    /// 设置多租户字段
    /// 在保存实体时自动为新实体设置租户ID
    /// </summary>
    protected virtual void SetMultiTenantFields()
    {
        if (!MultiTenantOptions.Enabled)
            return;

        var currentTenantId = CurrentTenantId;
        if (string.IsNullOrEmpty(currentTenantId))
        {
            _multiTenantLogger?.LogWarning("无法获取当前租户ID，跳过多租户字段设置");
            return;
        }

        var addedEntities = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is IMultiTenant)
            .ToList();

        foreach (var entry in addedEntities)
        {
            var multiTenantEntity = (IMultiTenant)entry.Entity;
            
            // 只在租户ID为空时设置，允许显式指定租户ID
            if (string.IsNullOrEmpty(multiTenantEntity.TenantId))
            {
                multiTenantEntity.TenantId = currentTenantId;
                _multiTenantLogger?.LogDebug("为实体 {EntityType} 设置租户ID: {TenantId}",
                    entry.Entity.GetType().Name, currentTenantId);
            }
            else
            {
                // 验证显式设置的租户ID是否合法
                ValidateExplicitTenantId(multiTenantEntity.TenantId, entry.Entity.GetType().Name);
            }
        }
    }

    /// <summary>
    /// 验证显式设置的租户ID
    /// 可在派生类中重写以实现自定义验证逻辑
    /// </summary>
    /// <param name="tenantId">要验证的租户ID</param>
    /// <param name="entityTypeName">实体类型名称</param>
    protected virtual void ValidateExplicitTenantId(string tenantId, string entityTypeName)
    {
        // 默认允许任何租户ID，派生类可以重写以实现严格验证
        _multiTenantLogger?.LogDebug("实体 {EntityType} 使用显式设置的租户ID: {TenantId}",
            entityTypeName, tenantId);
    }

    #endregion

    #region 多租户过滤

    /// <summary>
    /// 判断实体是否需要应用过滤器
    /// 扩展基类方法，添加多租户过滤支持
    /// </summary>
    protected override bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType)
    {
        return base.ShouldFilterEntity<TEntity>(entityType) ||
               typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity));
    }

    /// <summary>
    /// 创建过滤器表达式
    /// 扩展基类方法，添加多租户过滤支持
    /// </summary>
    protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
    {
        var expression = base.CreateFilterExpression<TEntity>();

        // 添加多租户过滤
        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)) && IsMultiTenantFilterEnabled)
        {
            var tenantFilter = CreateTenantFilterExpression<TEntity>();
            if (tenantFilter != null)
            {
                expression = expression != null
                    ? CombineExpressions(expression, tenantFilter)
                    : tenantFilter;
            }
        }

        return expression;
    }

    /// <summary>
    /// 创建租户过滤表达式
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>> CreateTenantFilterExpression<TEntity>()
        where TEntity : class
    {
        var currentTenantId = CurrentTenantId;

        if (string.IsNullOrEmpty(currentTenantId))
        {
            // 无法确定租户ID时的处理策略
            switch (MultiTenantOptions.UnknownTenantStrategy)
            {
                case UnknownTenantStrategy.AllowAll:
                    return null; // 不添加过滤器
                case UnknownTenantStrategy.DenyAll:
                    _multiTenantLogger?.LogWarning("无法确定租户ID，多租户实体 {EntityType} 查询将返回空结果",
                        typeof(TEntity).Name);
                    return e => false;
                case UnknownTenantStrategy.UseDefault:
                default:
                    currentTenantId = MultiTenantOptions.DefaultTenantId;
                    break;
            }
        }

        _multiTenantLogger?.LogDebug("为实体 {EntityType} 应用租户过滤: {TenantId}",
            typeof(TEntity).Name, currentTenantId);

        return e => EF.Property<string>(e, "TenantId") == currentTenantId;
    }

    #endregion

    #region 保存更改

    /// <summary>
    /// 重写保存更改方法，添加多租户字段设置
    /// </summary>
    public override int SaveChanges()
    {
        SetMultiTenantFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// 重写异步保存更改方法，添加多租户字段设置
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetMultiTenantFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region 多租户操作方法

    /// <summary>
    /// 禁用多租户过滤执行操作
    /// 用于管理员操作或跨租户查询
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <returns>操作结果</returns>
    public virtual T WithoutMultiTenantFilter<T>(Func<T> operation)
    {
        using (DataFilter?.Disable<IMultiTenant>())
        {
            return operation();
        }
    }

    /// <summary>
    /// 禁用多租户过滤执行异步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的异步操作</param>
    /// <returns>异步操作结果</returns>
    public virtual async Task<T> WithoutMultiTenantFilterAsync<T>(Func<Task<T>> operation)
    {
        using (DataFilter?.Disable<IMultiTenant>())
        {
            return await operation();
        }
    }

    /// <summary>
    /// 使用指定租户ID执行操作
    /// 注意：这只影响查询过滤，不影响新实体的租户ID设置
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="tenantId">指定的租户ID</param>
    /// <param name="operation">要执行的操作</param>
    /// <returns>操作结果</returns>
    public virtual T WithTenant<T>(string tenantId, Func<T> operation)
    {
        var originalTenantId = _currentTenantId;
        try
        {
            lock (_tenantCacheLock)
            {
                _currentTenantId = tenantId;
            }
            return operation();
        }
        finally
        {
            lock (_tenantCacheLock)
            {
                _currentTenantId = originalTenantId;
            }
        }
    }

    #endregion
}

/// <summary>
/// 多租户配置选项
/// </summary>
public class MultiTenantOptions
{
    /// <summary>
    /// 是否启用多租户功能
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 默认租户ID
    /// </summary>
    public string DefaultTenantId { get; set; } = "default";

    /// <summary>
    /// 无法确定租户ID时的处理策略
    /// </summary>
    public UnknownTenantStrategy UnknownTenantStrategy { get; set; } = UnknownTenantStrategy.UseDefault;
}

/// <summary>
/// 无法确定租户ID时的处理策略
/// </summary>
public enum UnknownTenantStrategy
{
    /// <summary>
    /// 使用默认租户
    /// </summary>
    UseDefault,

    /// <summary>
    /// 允许访问所有数据（不安全，慎用）
    /// </summary>
    AllowAll,

    /// <summary>
    /// 拒绝访问所有数据
    /// </summary>
    DenyAll
}