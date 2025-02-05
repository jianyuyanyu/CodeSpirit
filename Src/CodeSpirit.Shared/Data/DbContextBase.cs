using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeSpirit.Shared.Data
{

    /// <summary>
    /// 默认DbContext（支持审计字段自动维护、数据筛选器、实体变更事件）
    /// </summary>
    /// <remarks>
    /// 数据筛选器使用文档见：https://docs.abp.io/zh-Hans/abp/latest/Data-Filtering
    /// </remarks>
    public abstract class DbContextBase<TDbContext> : DbContext, IDbContextBase<TDbContext> where TDbContext : DbContext
    {
        private IServiceProvider serviceProvider;
        private ILogger<TDbContext> logger;
        private ChangeTracker changeTracker;
        private Lazy<IIdentityAccessor> identityAccessorObject = null;

        /// <summary>
        /// 是否启用租户筛选器
        /// </summary>
        protected virtual bool IsMultiTenantFilterEnabled => DataFilter?.IsEnabled<ITenant>() ?? false;

        /// <summary>
        /// 是否启用软删除
        /// </summary>
        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<IDeletionAuditedObject>() ?? false;

        /// <summary>
        /// 是否启用激活筛选器
        /// </summary>
        protected virtual bool IsActiveFilterEnabled => DataFilter?.IsEnabled<IIsActive>() ?? false;

        /// <summary>
        /// 数据筛选器
        /// </summary>
        public IDataFilter DataFilter { get; private set; }


        /// <summary>
        /// 当前租户Id
        /// </summary>
        public int? CurrentTenantId => identityAccessorObject.Value?.TenantId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="serviceProvider"></param>
        public DbContextBase(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
        {
            this.serviceProvider = serviceProvider;
            logger = serviceProvider.GetService<ILogger<TDbContext>>() ?? NullLogger<TDbContext>.Instance;
            changeTracker = ChangeTracker;
            changeTracker.Tracking += ChangeTracker_Tracking;

            identityAccessorObject = serviceProvider.GetServiceLazy<IIdentityAccessor>();
            DataFilter = serviceProvider.GetService<IDataFilter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureGlobalFiltersOnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            SetAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        internal static readonly MethodInfo ConfigureGlobalFiltersMethodInfo
      = typeof(TDbContext)
      .GetMethod(nameof(ConfigureGlobalFilters),
                 BindingFlags.Instance | BindingFlags.Public);

        private void ChangeTracker_Tracking(object sender, Microsoft.EntityFrameworkCore.ChangeTracking.EntityTrackingEventArgs e)
        {
            //logger.LogInformation($"ef ChangeTracker:ChangeTracker_Tracking {e.Entry.State} {e.Entry.Entity.GetType().FullName}...");

        }

        /// <summary>
        /// 执行软删除
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public virtual EntityEntry<TEntity> SoftDelete<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is IDeletionAuditedObject deletionObj)
            {
                if (deletionObj.DeleterUserId == default)
                {
                    IIdentityAccessor identityAccessor = identityAccessorObject.Value;
                    if (identityAccessor != null && identityAccessor.UserId == default)
                        deletionObj.DeleterUserId = identityAccessor.UserId;
                }

                if (deletionObj.DeletionTime == default)
                    deletionObj.DeletionTime = DateTime.Now;

                deletionObj.IsDeleted = true;

                return Update(entity);
            }
            throw new NotSupportedException($"{typeof(TEntity).Name} 未实现接口'IDeletionAuditedObject'，无法执行软删除逻辑！");
        }

        /// <summary>
        /// 设置审计字段
        /// </summary>
        public void SetAuditFields()
        {
            foreach (EntityEntry entry in changeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                if (entry.Entity is IAuditedObject modifiedObj)
                {
                    IIdentityAccessor identityAccessor = identityAccessorObject.Value;
                    if (identityAccessor != null && identityAccessor.UserId.HasValue)
                        modifiedObj.LastModifierUserId = identityAccessor.UserId;
                    modifiedObj.LastModificationTime = DateTime.Now;
                }

                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is IAuditedObject addedObj)
                    {
                        if (addedObj.CreatorUserId == default)
                        {
                            IIdentityAccessor identityAccessor = identityAccessorObject.Value;
                            if (identityAccessor != null && identityAccessor.UserId.HasValue)
                                addedObj.CreatorUserId = identityAccessor.UserId;
                        }

                        if (addedObj.CreationTime == default)
                            addedObj.CreationTime = DateTime.Now;
                    }
                    //仅创建时判断
                    if (entry.Entity is ITenant tenant)
                    {
                        if (!tenant.TenantId.HasValue)
                        {
                            IIdentityAccessor identityAccessor = identityAccessorObject.Value;
                            if (identityAccessor != null && identityAccessor.TenantId.HasValue)
                                tenant.TenantId = identityAccessor.TenantId;
                        }
                    }
                }

                if (entry.Entity is IDeletionAuditedObject deletionObj && deletionObj.IsDeleted && deletionObj.DeleterUserId == default)
                {
                    IIdentityAccessor identityAccessor = identityAccessorObject.Value;
                    if (identityAccessor != null && identityAccessor.UserId.HasValue)
                        deletionObj.DeleterUserId = identityAccessor.UserId;

                    if (deletionObj.DeletionTime == default)
                        deletionObj.DeletionTime = DateTime.Now;
                }
            }
        }

        public virtual void ConfigureGlobalFiltersOnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                ConfigureGlobalFiltersMethodInfo
                .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, new object[] { modelBuilder, entityType });
            }
        }
        public virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class
        {
            if (mutableEntityType.IsOwned())
            {
                return;
            }

            if (mutableEntityType.BaseType == null && ShouldFilterEntity<TEntity>(mutableEntityType))
            {
                Expression<Func<TEntity, bool>> filterExpression = CreateFilterExpression<TEntity>();
                if (filterExpression != null)
                {
                    modelBuilder.Entity<TEntity>().HasQueryFilter(filterExpression);
                }
            }
        }

        protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            if (typeof(ITenant).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }

            if (typeof(IDeletionAuditedObject).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }

            if (typeof(IIsActive).IsAssignableFrom(typeof(TEntity)))
            {
                return true;
            }

            return false;
        }

        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
            where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = null;

            if (typeof(IDeletionAuditedObject).IsAssignableFrom(typeof(TEntity)))
            {
                expression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(e, "IsDeleted");
            }

            if (typeof(ITenant).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> multiTenantFilter = e => !IsMultiTenantFilterEnabled || EF.Property<int>(e, "TenantId") == CurrentTenantId;
                expression = expression == null ? multiTenantFilter : CombineExpressions(expression, multiTenantFilter);
            }

            if (typeof(IIsActive).IsAssignableFrom(typeof(TEntity)))
            {
                Expression<Func<TEntity, bool>> isActiveFilter =
                    e => !IsActiveFilterEnabled || EF.Property<bool>(e, "IsActive");
                expression = expression == null
                    ? isActiveFilter
                    : CombineExpressions(expression, isActiveFilter);
            }

            return expression;
        }

        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));

            ReplaceExpressionVisitor leftVisitor = new ReplaceExpressionVisitor(expression1.Parameters[0], parameter);
            Expression left = leftVisitor.Visit(expression1.Body);

            ReplaceExpressionVisitor rightVisitor = new ReplaceExpressionVisitor(expression2.Parameters[0], parameter);
            Expression right = rightVisitor.Visit(expression2.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                {
                    return _newValue;
                }

                return base.Visit(node);
            }
        }
    }
}
