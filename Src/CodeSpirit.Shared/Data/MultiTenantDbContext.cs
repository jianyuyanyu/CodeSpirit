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
    private Lazy<MultiTenantOptions> _multiTenantOptions;
    private readonly object _tenantCacheLock = new();
    private string _currentTenantId;
    private readonly ICurrentUser _currentUser;
    private MultiTenantOptions _cachedDefaultOptions;
    private readonly IServiceProvider _serviceProvider;

    #endregion

    #region 属性

    /// <summary>
    /// 多租户配置选项
    /// </summary>
    protected MultiTenantOptions MultiTenantOptions 
    {
        get
        {
            // 防止在构造函数执行过程中访问未初始化的 _multiTenantOptions
            if (_multiTenantOptions == null)
            {
                _multiTenantLogger?.LogWarning("多租户配置尚未初始化，使用默认配置");
                return _cachedDefaultOptions ??= CreateDefaultMultiTenantOptions();
            }

            try
            {
                return _multiTenantOptions.Value;
            }
            catch (Exception ex)
            {
                // 记录异常信息便于排查问题
                _multiTenantLogger?.LogWarning(ex, "获取多租户配置失败，使用默认配置。异常信息: {Message}", ex.Message);
                
                // 缓存默认配置，避免重复创建对象
                return _cachedDefaultOptions ??= CreateDefaultMultiTenantOptions();
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

    #region 配置方法

    /// <summary>
    /// 创建默认的多租户配置选项
    /// 可在派生类中重写以提供自定义的默认配置
    /// </summary>
    /// <returns>默认的多租户配置选项</returns>
    protected virtual MultiTenantOptions CreateDefaultMultiTenantOptions()
    {
        return new MultiTenantOptions 
        { 
            Enabled = false, 
            DefaultTenantId = "default" 
        };
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
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _multiTenantLogger = serviceProvider.GetService<ILogger<MultiTenantDbContext>>();
        _currentUser = currentUser;

        // 立即初始化多租户配置，避免在基类构造函数执行期间访问时出现空引用
        InitializeMultiTenantOptions();
    }

    /// <summary>
    /// 初始化多租户配置选项
    /// </summary>
    private void InitializeMultiTenantOptions()
    {
        _multiTenantOptions = new Lazy<MultiTenantOptions>(() =>
        {
            try
            {
                var configuration = _serviceProvider.GetService<IConfiguration>();
                if (configuration == null)
                {
                    _multiTenantLogger?.LogWarning("无法获取IConfiguration服务，使用默认多租户配置");
                    return CreateDefaultMultiTenantOptions();
                }

                var options = new MultiTenantOptions();
                var section = configuration.GetSection("MultiTenant");
                
                if (!section.Exists())
                {
                    _multiTenantLogger?.LogInformation("未找到MultiTenant配置节，使用默认配置");
                    return CreateDefaultMultiTenantOptions();
                }

                section.Bind(options);
                
                // 验证配置的有效性
                if (string.IsNullOrEmpty(options.DefaultTenantId))
                {
                    _multiTenantLogger?.LogWarning("DefaultTenantId配置为空，使用默认值");
                    options.DefaultTenantId = "default";
                }

                _multiTenantLogger?.LogInformation("成功加载多租户配置: Enabled={Enabled}, DefaultTenantId={DefaultTenantId}", 
                    options.Enabled, options.DefaultTenantId);
                
                return options;
            }
            catch (Exception ex)
            {
                _multiTenantLogger?.LogError(ex, "初始化多租户配置时发生异常，使用默认配置");
                return CreateDefaultMultiTenantOptions();
            }
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

            // 3. 使用默认租户ID（安全获取，避免循环调用）
            tenantId = GetDefaultTenantIdSafely();
            _multiTenantLogger?.LogDebug("使用默认租户ID: {TenantId}", tenantId);
            return tenantId;
        }
        catch (Exception ex)
        {
            _multiTenantLogger?.LogError(ex, "解析租户ID时发生异常，使用默认租户ID");
            return GetDefaultTenantIdSafely();
        }
    }

    /// <summary>
    /// 安全获取默认租户ID，避免循环调用
    /// </summary>
    /// <returns>默认租户ID</returns>
    private string GetDefaultTenantIdSafely()
    {
        // 直接从懒加载配置获取，如果初始化失败则使用硬编码默认值
        try
        {
            return _multiTenantOptions?.Value?.DefaultTenantId ?? "default";
        }
        catch
        {
            return "default";
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
        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            // 获取当前租户ID作为编译时常量
            var currentTenantId = CurrentTenantId;
            
            // 创建简单的字符串比较表达式，EF Core可以转换为SQL
            Expression<Func<TEntity, bool>> tenantFilter = e => 
                !IsMultiTenantFilterEnabled || 
                EF.Property<string>(e, "TenantId") == currentTenantId;
            
            if (expression != null)
            {
                expression = CombineExpressions(expression, tenantFilter);
            }
            else
            {
                expression = tenantFilter;
            }
        }

        return expression;
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
}