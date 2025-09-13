using CodeSpirit.Core;
using CodeSpirit.IdentityApi.EventHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CodeSpirit.IdentityApi.Data
{
    /// <summary>
    /// SQL Server 特定的数据库上下文
    /// 用于迁移和SQL Server特定的配置
    /// </summary>
    public class SqlServerDbContext : ApplicationDbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        /// <param name="currentUser">当前用户</param>
        /// <param name="entityFileReferenceEventHandler">实体文件引用事件处理器</param>
        public SqlServerDbContext(
            DbContextOptions options,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUser currentUser,
            EntityFileReferenceEventHandler entityFileReferenceEventHandler) 
            : base(options, serviceProvider, httpContextAccessor, currentUser, entityFileReferenceEventHandler)
        {
        }

        /// <summary>
        /// 配置模型创建
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 调用基类的模型创建
            base.OnModelCreating(modelBuilder);
            
            // 应用SQL Server特定配置
            ApplySqlServerSpecificConfigurations(modelBuilder);
        }

        /// <summary>
        /// 应用SQL Server特定的配置
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private void ApplySqlServerSpecificConfigurations(ModelBuilder modelBuilder)
        {
            Console.WriteLine($"正在为 {GetType().Name} 应用 SQL Server 特定配置");
            
            // SQL Server特定配置 - 使用默认配置，避免复杂的类型映射
            // SQL Server的EF Core提供程序已经有很好的默认配置
            
            // 只处理Identity相关字段的长度限制
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        var propertyName = property.Name;
                        if (propertyName == "NormalizedName" || propertyName == "NormalizedUserName" || 
                            propertyName == "NormalizedEmail" || propertyName == "ConcurrencyStamp")
                        {
                            var maxLength = property.GetMaxLength();
                            if (!maxLength.HasValue || maxLength.Value > 256)
                            {
                                property.SetMaxLength(256);
                            }
                        }
                    }
                }
            }
        }
    }
}
