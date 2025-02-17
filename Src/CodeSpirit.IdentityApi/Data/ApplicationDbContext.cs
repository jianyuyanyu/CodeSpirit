using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long,
    IdentityUserClaim<long>, ApplicationUserRole, IdentityUserLogin<long>,
    IdentityRoleClaim<long>, IdentityUserToken<long>>
    {
        /// <summary>
        /// 角色与权限的关联实体集。
        /// </summary>
        public DbSet<RolePermission> RolePermissions { get; set; }

        /// <summary>
        /// 登录日志实体集。
        /// </summary>
        public DbSet<LoginLog> LoginLogs { get; set; }

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<ApplicationDbContext> logger;
        private readonly ChangeTracker changeTracker;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICurrentUser _currentUser;

        /// <summary>
        /// 是否启用软删除
        /// </summary>
        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDeleteAuditable>() ?? false;

        /// <summary>
        /// 数据筛选器
        /// </summary>
        public IDataFilter DataFilter { get; private set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        protected long? CurrentUserId => this.UserId ?? _currentUser?.Id;

        public long? UserId { get; set; }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUser currentUser) : base(options)
        {
            this.serviceProvider = serviceProvider;
            this.httpContextAccessor = httpContextAccessor;
            logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>() ?? NullLogger<ApplicationDbContext>.Instance;
            changeTracker = ChangeTracker;

            changeTracker.StateChanged += ChangeTracker_StateChanged;
            changeTracker.Tracking += ChangeTracker_Tracking;

            DataFilter = serviceProvider.GetService<IDataFilter>();
            _currentUser = currentUser;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 定义一个转换器：将 string[] 转换为单一字符串存储，反之转换回来
            ValueConverter<string[], string> stringArrayConverter = new(
                v => string.Join(",", v),   // 数组 -> 字符串（写入数据库时）
                v => v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) // 字符串 -> 数组（读取数据库时）
            );

            #region 用户
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable(nameof(ApplicationUser));
                b.Property(q => q.Id).ValueGeneratedNever();
                b.Property(q => q.PhoneNumber).HasColumnType("varchar(15)");
                b.HasIndex(q => q.IdNo).IsUnique(true);
                b.HasIndex(q => q.PhoneNumber);
            });
            #endregion

            builder.Entity<ApplicationRole>(b =>
            {
                b.Property(q => q.Id).ValueGeneratedNever();
                b.ToTable(nameof(ApplicationRole));
            });

            // 配置 ApplicationUserRole 的关系
            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.ToTable(nameof(ApplicationUserRole));

                //userRole.HasKey(ur => ur.Id); // 使用单独的主键

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict); // 设置为 Restrict，避免级联删除

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade); // 保持级联删除

                //userRole.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            });

            // 应用转换器到 RolePermission 实体的 PermissionIds 属性
            builder.Entity<RolePermission>()
                .Property(rp => rp.PermissionIds)
                .HasConversion(stringArrayConverter);

            // 配置 LoginLog 的索引
            builder.Entity<LoginLog>(entity =>
            {
                // 索引 UserId，提高按用户查询的性能
                entity.HasIndex(l => l.UserId)
                      .HasDatabaseName("IX_LoginLogs_UserId");

                // 索引 LoginTime，提高按时间范围查询或排序的性能
                entity.HasIndex(l => l.LoginTime)
                      .HasDatabaseName("IX_LoginLogs_LoginTime");

                // 索引 UserName，提高按用户名过滤的性能
                entity.HasIndex(l => l.UserName)
                      .HasDatabaseName("IX_LoginLogs_UserName");

                // 索引 IsSuccess，提高按登录结果过滤的性能
                entity.HasIndex(l => l.IsSuccess)
                      .HasDatabaseName("IX_LoginLogs_IsSuccess");

                // 添加复合索引
                entity.HasIndex(l => new { l.UserId, l.LoginTime })
                      .HasDatabaseName("IX_LoginLogs_UserId_LoginTime");
            });

            ConfigureGlobalFiltersOnModelCreating(builder);
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
      = typeof(ApplicationDbContext)
      .GetMethod(nameof(ConfigureGlobalFilters),
                 BindingFlags.Instance | BindingFlags.Public);

        private void ChangeTracker_Tracking(object sender, EntityTrackingEventArgs e)
        {
            //logger.LogInformation($"ef ChangeTracker:ChangeTracker_Tracking {e.Entry.State} {e.Entry.Entity.GetType().FullName}...");

        }

        private void ChangeTracker_StateChanged(object sender, EntityStateChangedEventArgs e)
        {
            switch (e.OldState)
            {
                case EntityState.Detached:
                    break;
                case EntityState.Unchanged:
                    break;
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
                default:
                    break;
            }
        }

        /// <summary>
        /// 推送实体事件
        /// </summary>
        /// <param name="e"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        private void PublishEntityEventData(EntityStateChangedEventArgs e, object entity)
        {
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
            if (entity is ISoftDeleteAuditable deletionObj)
            {
                deletionObj.IsDeleted = true;
                deletionObj.DeletedBy = CurrentUserId;
                deletionObj.DeletedAt = DateTime.UtcNow;
                return Update(entity);
            }
            throw new NotSupportedException($"{typeof(TEntity).Name} 未实现接口'ISoftDeleteAuditable'，无法执行软删除逻辑！");
        }

        /// <summary>
        /// 设置审计字段
        /// </summary>
        public void SetAuditFields()
        {
            var currentTime = DateTime.UtcNow;
            
            foreach (EntityEntry entry in changeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                // 处理修改审计
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

                // 处理创建审计
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

                // 处理删除审计
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
            return typeof(ISoftDeleteAuditable).IsAssignableFrom(typeof(TEntity)) ||
                   typeof(IIsActive).IsAssignableFrom(typeof(TEntity));
        }

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

        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(T));

            ReplaceExpressionVisitor leftVisitor = new(expression1.Parameters[0], parameter);
            Expression left = leftVisitor.Visit(expression1.Body);

            ReplaceExpressionVisitor rightVisitor = new(expression2.Parameters[0], parameter);
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
                return node == _oldValue ? _newValue : base.Visit(node);
            }

        }
    }
}
