using CodeSpirit.Core;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeSpirit.Shared.Data
{
    /// <summary>
    /// 可审计的数据库上下文基类
    /// 提供实体审计、软删除等通用功能
    /// </summary>
    public abstract class AuditableDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ChangeTracker _changeTracker;
        private readonly ICurrentUser _currentUser;

        /// <summary>
        /// 是否启用软删除过滤器
        /// 当 DataFilter 启用了 ISoftDeleteAuditable 时返回 true
        /// </summary>
        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDeleteAuditable>() ?? false;

        /// <summary>
        /// 数据过滤器
        /// 用于控制软删除等过滤条件的启用/禁用
        /// </summary>
        public IDataFilter DataFilter { get; private set; }

        /// <summary>
        /// 获取当前用户ID
        /// 用于审计字段自动填充
        /// </summary>
        protected virtual long? CurrentUserId => _currentUser?.Id;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="currentUser">当前用户服务</param>
        protected AuditableDbContext(
            DbContextOptions options,
            IServiceProvider serviceProvider,
            ICurrentUser currentUser) : base(options)
        {
            _serviceProvider = serviceProvider;
            _currentUser = currentUser;
            _changeTracker = ChangeTracker;

            // 注册实体状态变更事件
            _changeTracker.StateChanged += ChangeTracker_StateChanged;
            _changeTracker.Tracking += ChangeTracker_Tracking;
            DataFilter = serviceProvider.GetService<IDataFilter>();
        }

        /// <summary>
        /// 重写保存更改方法，在保存前设置审计字段
        /// </summary>
        public override int SaveChanges()
        {
            SetAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// 重写异步保存更改方法，在保存前设置审计字段
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 设置审计字段
        /// 自动填充创建者、修改者、删除者等审计信息
        /// </summary>
        protected virtual void SetAuditFields()
        {
            var currentTime = DateTime.UtcNow;
            
            foreach (EntityEntry entry in _changeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                if (entry.Entity is IUpdateAuditable modifiedObj)
                {
                    if (modifiedObj.UpdatedBy == default)
                    {
                        modifiedObj.UpdatedBy = CurrentUserId;
                    }

                    if (modifiedObj.UpdatedAt == default)
                    {
                        modifiedObj.UpdatedAt = currentTime;
                    }
                }

                if (entry.State == EntityState.Added && entry.Entity is ICreationAuditable addedObj)
                {
                    if (addedObj.CreatedBy == default)
                    {
                        addedObj.CreatedBy = CurrentUserId ?? throw new InvalidOperationException("Cannot set CreatedBy: CurrentUserId is null");
                    }

                    if (addedObj.CreatedAt == default)
                    {
                        addedObj.CreatedAt = currentTime;
                    }
                }

                if (entry.Entity is ISoftDeleteAuditable deletionObj && deletionObj.IsDeleted && deletionObj.DeletedBy == default)
                {
                    deletionObj.DeletedBy = CurrentUserId;
                    
                    if (deletionObj.DeletedAt == default)
                    {
                        deletionObj.DeletedAt = currentTime;
                    }
                }
            }
        }

        /// <summary>
        /// 执行软删除
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entity">要删除的实体</param>
        /// <returns>实体条目</returns>
        public virtual EntityEntry<TEntity> SoftDelete<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is ISoftDeleteAuditable softDeleteObj)
            {
                softDeleteObj.IsDeleted = true;
                softDeleteObj.DeletedBy = CurrentUserId;
                softDeleteObj.DeletedAt = DateTime.UtcNow;
                return Update(entity);
            }
            
            throw new NotSupportedException($"{typeof(TEntity).Name} 未实现接口'ISoftDeleteAuditable'，无法执行软删除逻辑！");
        }

        /// <summary>
        /// 配置全局过滤器
        /// </summary>
        protected virtual void ConfigureGlobalFiltersOnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                // 使用反射调用泛型方法
                var configureMethod = typeof(AuditableDbContext)
                    .GetMethod(nameof(ConfigureGlobalFilters), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);
                    
                configureMethod?.Invoke(this, new object[] { modelBuilder, entityType });
            }
        }

        /// <summary>
        /// 为指定实体类型配置全局过滤器
        /// </summary>
        protected virtual void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
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

        /// <summary>
        /// 判断实体是否需要应用过滤器
        /// </summary>
        protected virtual bool ShouldFilterEntity<TEntity>(IMutableEntityType entityType) where TEntity : class
        {
            return typeof(ISoftDeleteAuditable).IsAssignableFrom(typeof(TEntity));
        }

        /// <summary>
        /// 创建过滤器表达式
        /// </summary>
        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
            where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = null;

            if (typeof(ISoftDeleteAuditable).IsAssignableFrom(typeof(TEntity)))
            {
                expression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(e, "IsDeleted");
            }

            return expression;
        }

        /// <summary>
        /// 合并两个表达式
        /// </summary>
        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> expression1, 
            Expression<Func<T, bool>> expression2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));

            ReplaceExpressionVisitor leftVisitor = new(expression1.Parameters[0], parameter);
            Expression left = leftVisitor.Visit(expression1.Body);

            ReplaceExpressionVisitor rightVisitor = new(expression2.Parameters[0], parameter);
            Expression right = rightVisitor.Visit(expression2.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }

        /// <summary>
        /// 表达式访问者，用于替换表达式中的参数
        /// </summary>
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
                return node == _oldValue ? _newValue : base.Visit(node);
            }
        }

        /// <summary>
        /// 实体跟踪事件处理
        /// </summary>
        private void ChangeTracker_Tracking(object sender, EntityTrackingEventArgs e)
        {
            // 可以在这里添加实体跟踪的日志记录
        }

        /// <summary>
        /// 实体状态变更事件处理
        /// </summary>
        private void ChangeTracker_StateChanged(object sender, EntityStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case EntityState.Deleted:
                    if (e.Entry.Entity is IEntityDeletedEvent entityDeleted)
                    {
                        PublishEntityEventData(e, entityDeleted);
                    }
                    break;
                case EntityState.Modified:
                    if (e.Entry.Entity is IEntityUpdatedEvent entityUpdated)
                    {
                        PublishEntityEventData(e, entityUpdated);
                    }
                    break;
                case EntityState.Added:
                    if (e.Entry.Entity is IEntityCreatedEvent entityCreated)
                    {
                        PublishEntityEventData(e, entityCreated);
                    }
                    break;
            }
        }

        /// <summary>
        /// 发布实体事件数据
        /// 可在派生类中实现具体的事件发布逻辑
        /// </summary>
        protected virtual void PublishEntityEventData(EntityStateChangedEventArgs e, object entity)
        {
            // 派生类可以实现具体的事件发布逻辑
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置全局过滤器（软删除等）
            ConfigureGlobalFiltersOnModelCreating(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }
    }
} 