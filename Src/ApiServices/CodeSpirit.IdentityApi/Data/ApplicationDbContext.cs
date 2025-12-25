using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.EventHandlers;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// 刷新令牌实体集。
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        /// <summary>
        /// API密钥实体集。
        /// </summary>
        public DbSet<ApiKey> ApiKeys { get; set; }

        /// <summary>
        /// 租户信息实体集。
        /// </summary>
        public DbSet<TenantInfo> Tenants { get; set; }

        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<ApplicationDbContext> logger;
        private readonly ChangeTracker changeTracker;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICurrentUser _currentUser;
        private readonly Lazy<string> _defaultTenantId;
        private readonly EntityFileReferenceEventHandler _entityFileReferenceEventHandler;

        /// <summary>
        /// 是否启用软删除
        /// </summary>
        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDeleteAuditable>() ?? false;

        /// <summary>
        /// 是否启用多租户过滤
        /// </summary>
        protected virtual bool IsMultiTenantFilterEnabled => DataFilter?.IsEnabled<IMultiTenant>() ?? true;

        /// <summary>
        /// 数据筛选器
        /// </summary>
        public IDataFilter DataFilter { get; private set; }


        /// <summary>
        /// 部门信息实体集
        /// </summary>
        public DbSet<Department> Departments { get; set; }

        /// <summary>
        /// 职工信息实体集
        /// </summary>
        public DbSet<Employee> Employees { get; set; }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        protected long? CurrentUserId => this.UserId ?? _currentUser?.Id;

        public long? UserId { get; set; }

        /// <summary>
        /// 获取当前租户ID
        /// </summary>
        protected virtual string GetCurrentTenantId()
        {
            try
            {
                // 设计时检查 - 如果是设计时上下文，返回默认值
                if (_currentUser == null && httpContextAccessor == null)
                {
                    return "default";
                }

                // 优先从CurrentUser获取租户ID（更安全，避免异步调用）
                var tenantId = _currentUser?.TenantId;
                
                // 如果CurrentUser中没有，尝试从HttpContext获取
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = httpContextAccessor?.HttpContext?.Items["TenantId"] as string;
                }
                
                // 如果仍然没有，使用默认租户ID（可配置）
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = _defaultTenantId?.Value ?? "default";
                }
                
                return tenantId;
            }
            catch (Exception ex)
            {
                // 设计时或其他异常情况下，返回默认值
                logger?.LogWarning(ex, "获取租户ID时发生异常，使用默认值");
                return "default";
            }
        }

        /// <summary>
        /// 设置多租户字段
        /// 在保存实体时自动设置租户ID
        /// </summary>
        protected virtual void SetMultiTenantFields()
        {
            var currentTenantId = GetCurrentTenantId();
            if (string.IsNullOrEmpty(currentTenantId))
            {
                return;
            }

            foreach (EntityEntry entry in changeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Entity is IMultiTenant))
            {
                var multiTenantEntity = (IMultiTenant)entry.Entity;
                if (string.IsNullOrEmpty(multiTenantEntity.TenantId))
                {
                    multiTenantEntity.TenantId = currentTenantId;
                    logger?.LogDebug("为实体 {EntityType} 设置租户ID: {TenantId}", 
                        entry.Entity.GetType().Name, currentTenantId);
                }
            }
        }

        public ApplicationDbContext(
            DbContextOptions options,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUser currentUser,
            EntityFileReferenceEventHandler entityFileReferenceEventHandler) : base(options)
        {
            this.serviceProvider = serviceProvider;
            this.httpContextAccessor = httpContextAccessor;
            logger = serviceProvider.GetService<ILogger<ApplicationDbContext>>() ?? NullLogger<ApplicationDbContext>.Instance;
            changeTracker = ChangeTracker;
            _entityFileReferenceEventHandler = entityFileReferenceEventHandler ?? throw new ArgumentNullException(nameof(entityFileReferenceEventHandler));

            changeTracker.StateChanged += ChangeTracker_StateChanged;
            changeTracker.Tracking += ChangeTracker_Tracking;

            DataFilter = serviceProvider.GetRequiredService<IDataFilter>();
            _currentUser = currentUser;
            
            // 延迟初始化默认租户ID，从配置中读取
            _defaultTenantId = new Lazy<string>(() => 
            {
                var configuration = serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                return configuration?.GetValue<string>("MultiTenant:DefaultTenantId") ?? "default";
            });
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // 移除ASP.NET Core Identity默认创建的RoleNameIndex
            var roleEntity = builder.Entity<ApplicationRole>();
            var roleNameIndex = roleEntity.Metadata.GetIndexes()
                .FirstOrDefault(i => i.GetDatabaseName() == "RoleNameIndex");
            if (roleNameIndex != null)
            {
                roleEntity.Metadata.RemoveIndex(roleNameIndex);
            }

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
                
                // 移除ASP.NET Core Identity默认创建的UserNameIndex
                var userNameIndex = b.Metadata.GetIndexes()
                    .FirstOrDefault(i => i.GetDatabaseName() == "UserNameIndex");
                if (userNameIndex != null)
                {
                    b.Metadata.RemoveIndex(userNameIndex);
                }
                
                // 重新配置用户名索引，包含租户ID以支持多租户
                b.HasIndex(u => new { u.NormalizedUserName, u.TenantId })
                    .HasDatabaseName("IX_ApplicationUser_NormalizedUserName_TenantId")
                    .IsUnique();
                
                // 租户感知的IdNo复合唯一索引：同一租户内身份证号码唯一，但不同租户可以有相同身份证号码
                b.HasIndex(q => new { q.TenantId, q.IdNo })
                    .IsUnique(true)
                    .HasDatabaseName("IX_ApplicationUser_TenantId_IdNo")
                    .HasFilter("[IdNo] IS NOT NULL");
                    
                b.HasIndex(q => q.PhoneNumber);
            });
            #endregion

            builder.Entity<ApplicationRole>(b =>
            {
                b.Property(q => q.Id).ValueGeneratedNever();
                b.ToTable(nameof(ApplicationRole));
                
                // 重新配置角色名称索引，包含租户ID以支持多租户
                b.HasIndex(r => new { r.NormalizedName, r.TenantId })
                 .HasDatabaseName("IX_ApplicationRole_NormalizedName_TenantId")
                 .IsUnique();
            });

            // 配置 ApplicationUserRole 的关系
            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.ToTable(nameof(ApplicationUserRole));

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

                // 创建包含租户ID的复合唯一索引，确保在同一租户内用户角色关联的唯一性
                userRole.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.TenantId })
                    .IsUnique()
                    .HasDatabaseName("IX_ApplicationUserRole_UserId_RoleId_TenantId");

                // 为TenantId单独创建索引，提高多租户查询性能
                userRole.HasIndex(ur => ur.TenantId)
                    .HasDatabaseName("IX_ApplicationUserRole_TenantId");

                // 为经常查询的组合创建索引
                userRole.HasIndex(ur => new { ur.TenantId, ur.UserId })
                    .HasDatabaseName("IX_ApplicationUserRole_TenantId_UserId");
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

            // 配置 RefreshToken 的索引和关系
            builder.Entity<RefreshToken>(entity =>
            {
                // 主键设置
                entity.HasKey(r => r.Id);
                
                // 设置与用户的关系
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // 索引 Token，提高根据令牌查询的性能
                entity.HasIndex(r => r.Token)
                      .HasDatabaseName("IX_RefreshTokens_Token");
                
                // 索引 UserId，提高按用户查询的性能
                entity.HasIndex(r => r.UserId)
                      .HasDatabaseName("IX_RefreshTokens_UserId");
                
                // 索引 ExpiryTime，便于清理过期令牌
                entity.HasIndex(r => r.ExpiryTime)
                      .HasDatabaseName("IX_RefreshTokens_ExpiryTime");
                
                // 复合索引，提高按用户+令牌查询的性能
                entity.HasIndex(r => new { r.UserId, r.Token })
                      .HasDatabaseName("IX_RefreshTokens_UserId_Token");
            });

            // 配置 TenantInfo 实体
            builder.Entity<TenantInfo>(entity =>
            {
                // 主键设置
                entity.HasKey(t => t.Id);
                
                // 索引 TenantId，提高按租户ID查询的性能
                entity.HasIndex(t => t.TenantId)
                      .IsUnique()
                      .HasDatabaseName("IX_Tenants_TenantId");
                
                // 索引 Name，提高按名称查询的性能
                entity.HasIndex(t => t.Name)
                      .HasDatabaseName("IX_Tenants_Name");
                
                // 索引 Domain，提高按域名查询的性能
                entity.HasIndex(t => t.Domain)
                      .HasDatabaseName("IX_Tenants_Domain");
                
                // 索引 IsActive，提高按状态过滤的性能
                entity.HasIndex(t => t.IsActive)
                      .HasDatabaseName("IX_Tenants_IsActive");
                
                // 索引 ExpiresAt，便于查询过期租户
                entity.HasIndex(t => t.ExpiresAt)
                      .HasDatabaseName("IX_Tenants_ExpiresAt");
            });

            // 配置 Department 实体
            builder.Entity<Department>(entity =>
            {
                entity.ToTable(nameof(Department));
                entity.Property(d => d.Id).ValueGeneratedNever();

                // 租户感知的部门编码复合唯一索引：同一租户内部门编码唯一
                entity.HasIndex(d => new { d.TenantId, d.Code })
                    .IsUnique()
                    .HasDatabaseName("IX_Department_TenantId_Code");

                // 索引 ParentId，提高查询子部门的性能
                entity.HasIndex(d => d.ParentId)
                    .HasDatabaseName("IX_Department_ParentId");

                // 索引 ManagerId，提高查询负责人的性能
                entity.HasIndex(d => d.ManagerId)
                    .HasDatabaseName("IX_Department_ManagerId");

                // 索引 IsActive，提高按状态过滤的性能
                entity.HasIndex(d => d.IsActive)
                    .HasDatabaseName("IX_Department_IsActive");

                // 配置自引用关系
                entity.HasOne(d => d.Parent)
                    .WithMany(d => d.Children)
                    .HasForeignKey(d => d.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 配置与职工的关系（负责人）
                entity.HasOne(d => d.Manager)
                    .WithMany()
                    .HasForeignKey(d => d.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置 Employee 实体
            builder.Entity<Employee>(entity =>
            {
                entity.ToTable(nameof(Employee));
                entity.Property(e => e.Id).ValueGeneratedNever();

                // 租户感知的工号复合唯一索引：同一租户内工号唯一
                entity.HasIndex(e => new { e.TenantId, e.EmployeeNo })
                    .IsUnique()
                    .HasDatabaseName("IX_Employee_TenantId_EmployeeNo");

                // 索引 DepartmentId，提高查询部门员工的性能
                entity.HasIndex(e => e.DepartmentId)
                    .HasDatabaseName("IX_Employee_DepartmentId");

                // 索引 UserId，提高查询用户关联的性能
                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_Employee_UserId");

                // 索引 IsActive，提高按状态过滤的性能
                entity.HasIndex(e => e.IsActive)
                    .HasDatabaseName("IX_Employee_IsActive");

                // 索引 EmploymentStatus，提高按在职状态过滤的性能
                entity.HasIndex(e => e.EmploymentStatus)
                    .HasDatabaseName("IX_Employee_EmploymentStatus");

                // 索引 IdNo，提高按身份证查询的性能
                entity.HasIndex(e => new { e.TenantId, e.IdNo })
                    .HasDatabaseName("IX_Employee_TenantId_IdNo")
                    .HasFilter("[IdNo] IS NOT NULL");

                // 配置与部门的关系
                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 配置与用户的关系
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置 ApiKey 实体
            builder.Entity<ApiKey>(entity =>
            {
                entity.ToTable(nameof(ApiKey));
                // 移除标识种子，支持手动插入ID
                entity.Property(a => a.Id).ValueGeneratedNever();

                // 索引 KeyHash，提高密钥验证查询的性能
                entity.HasIndex(a => a.KeyHash)
                    .HasDatabaseName("IX_ApiKey_KeyHash");

                // 索引 UserId，提高按用户查询的性能
                entity.HasIndex(a => a.UserId)
                    .HasDatabaseName("IX_ApiKey_UserId");

                // 索引 IsActive，提高按状态过滤的性能
                entity.HasIndex(a => a.IsActive)
                    .HasDatabaseName("IX_ApiKey_IsActive");

                // 索引 ExpiresAt，便于清理过期密钥
                entity.HasIndex(a => a.ExpiresAt)
                    .HasDatabaseName("IX_ApiKey_ExpiresAt");

                // 租户感知的复合索引
                entity.HasIndex(a => new { a.TenantId, a.UserId })
                    .HasDatabaseName("IX_ApiKey_TenantId_UserId");
            });

            ConfigureGlobalFiltersOnModelCreating(builder);
        }


        public override int SaveChanges()
        {
            SetAuditFields();
            SetMultiTenantFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAuditFields();
            SetMultiTenantFields();
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
        /// <param name="e">实体状态变更事件参数</param>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        private void PublishEntityEventData(EntityStateChangedEventArgs e, object entity)
        {
            // 委托给独立的事件处理器处理
            _entityFileReferenceEventHandler.HandleEntityStateChanged(e, entity);
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
                   typeof(IIsActive).IsAssignableFrom(typeof(TEntity)) ||
                   typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity));
        }

        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
            where TEntity : class
        {
            Expression<Func<TEntity, bool>> expression = null;

            // 软删除过滤
            if (typeof(ISoftDeleteAuditable).IsAssignableFrom(typeof(TEntity)))
            {
                expression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool>(e, "IsDeleted");
            }

            // 多租户过滤
            if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
            {
                // 修复：使用方法调用而不是编译时常量，确保每次查询时动态获取租户ID
                Expression<Func<TEntity, bool>> tenantFilter = e => 
                    !IsMultiTenantFilterEnabled || 
                    EF.Property<string>(e, "TenantId") == GetCurrentTenantId();
                
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

        #region 多租户操作方法

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
        /// 禁用多租户过滤执行操作
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

        #endregion

    }
}
