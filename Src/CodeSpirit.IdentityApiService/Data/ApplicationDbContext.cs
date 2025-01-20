using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Models.RoleManagementApiIdentity.Models;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Services;
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
using System.Text.Json;

namespace CodeSpirit.IdentityApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string,
    IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
    IdentityRoleClaim<string>, IdentityUserToken<string>>
    {
        public DbSet<Tenant> Tenants { get; set; }
        //public DbSet<ApplicationUser> Users { get; set; }

        //public DbSet<ApplicationRole> Roles { get; set; }

        //public DbSet<ApplicationUserRole> UserRoles { get; set; }
        /// <summary>
        /// 权限实体集。
        /// </summary>
        public DbSet<Permission> Permissions { get; set; }

        /// <summary>
        /// 角色与权限的关联实体集。
        /// </summary>
        public DbSet<RolePermission> RolePermissions { get; set; }

        /// <summary>
        /// 登录日志实体集。
        /// </summary>
        public DbSet<LoginLog> LoginLogs { get; set; }

        private IServiceProvider serviceProvider;
        private ILogger<ApplicationDbContext> logger;
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

        ///// <summary>
        ///// 是否启用激活筛选器
        ///// </summary>
        //protected virtual bool IsActiveFilterEnabled => DataFilter?.IsEnabled<IIsActive>() ?? false;

        /// <summary>
        /// 数据筛选器
        /// </summary>
        public IDataFilter DataFilter { get; private set; }

        /// <summary>
        /// 当前租户Id
        /// </summary>
        public int? CurrentTenantId => identityAccessorObject.Value?.TenantId;



        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider) : base(options)
        {
            this.serviceProvider = serviceProvider;
            logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>() ?? NullLogger<ApplicationDbContext>.Instance;
            changeTracker = ChangeTracker;

            changeTracker.StateChanged += ChangeTracker_StateChanged;
            changeTracker.Tracking += ChangeTracker_Tracking;

            identityAccessorObject = serviceProvider.GetServiceLazy<IIdentityAccessor>();
            DataFilter = serviceProvider.GetService<IDataFilter>();

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region 用户
            builder.Entity<ApplicationUser>(b =>
            {
                b.ToTable(nameof(ApplicationUser));
                b.Property(q => q.PhoneNumber).HasColumnType("varchar(15)");
                b.HasIndex(q => q.IdNo).IsUnique(true);
                b.HasIndex(q => q.PhoneNumber);
            });
            #endregion

            builder.Entity<ApplicationRole>(b =>
            {
                b.ToTable(nameof(ApplicationRole));
            });

            builder.Entity<Tenant>(b =>
            {
                b.HasIndex(x => x.Name).IsUnique(true);
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

            // 配置权限的自引用关系（用于权限层级）
            builder.Entity<Permission>()
                .HasOne(p => p.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(p => p.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // 防止级联删除

            // 配置角色与权限的多对多关系
            builder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

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

        private async void ChangeTracker_StateChanged(object sender, EntityStateChangedEventArgs e)
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
                        await PublishEntityEventDataAsync(e, entityDeleted);
                    }
                    break;
                case EntityState.Modified:
                    if (e.Entry.Entity is IEntityUpdatedEvent entityUpdated)
                    {
                        await PublishEntityEventDataAsync(e, entityUpdated);
                    }
                    break;
                case EntityState.Added:
                    if (e.Entry.Entity is IEntityCreatedEvent entityCreated)
                    {
                        await PublishEntityEventDataAsync(e, entityCreated);
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
        private async Task PublishEntityEventDataAsync(EntityStateChangedEventArgs e, object entity)
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
            if (entity is IDeletionAuditedObject deletionObj)
            {
                if (deletionObj.DeleterUserId == default)
                {
                    var identityAccessor = identityAccessorObject.Value;
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
            foreach (var entry in changeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
            {
                if (entry.Entity is IAuditedObject modifiedObj)
                {
                    if (modifiedObj.LastModifierUserId == default)
                    {
                        var identityAccessor = identityAccessorObject.Value;
                        if (identityAccessor != null && identityAccessor.UserId.HasValue)
                            modifiedObj.LastModifierUserId = identityAccessor.UserId;
                    }

                    if (modifiedObj.LastModificationTime == default)
                        modifiedObj.LastModificationTime = DateTime.Now;
                }

                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is IAuditedObject addedObj)
                    {
                        if (addedObj.CreatorUserId == default)
                        {
                            var identityAccessor = identityAccessorObject.Value;
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
                            var identityAccessor = identityAccessorObject.Value;
                            if (identityAccessor != null && identityAccessor.TenantId.HasValue)
                                tenant.TenantId = identityAccessor.TenantId;
                        }
                    }
                }

                if (entry.Entity is IDeletionAuditedObject deletionObj && deletionObj.IsDeleted && deletionObj.DeleterUserId == default)
                {
                    var identityAccessor = identityAccessorObject.Value;
                    if (identityAccessor != null && identityAccessor.UserId.HasValue)
                        deletionObj.DeleterUserId = identityAccessor.UserId;

                    if (deletionObj.DeletionTime == default)
                        deletionObj.DeletionTime = DateTime.Now;
                }
            }
        }

        public virtual void ConfigureGlobalFiltersOnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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
                var filterExpression = CreateFilterExpression<TEntity>();
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

            //if (typeof(IIsActive).IsAssignableFrom(typeof(TEntity)))
            //{
            //    Expression<Func<TEntity, bool>> isActiveFilter =
            //        e => !IsActiveFilterEnabled || EF.Property<bool>(e, "IsActive");
            //    expression = expression == null
            //        ? isActiveFilter
            //        : CombineExpressions(expression, isActiveFilter);
            //}

            return expression;
        }

        protected virtual Expression<Func<T, bool>> CombineExpressions<T>(Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expression1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expression1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expression2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expression2.Body);

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }

        class ReplaceExpressionVisitor : ExpressionVisitor
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
