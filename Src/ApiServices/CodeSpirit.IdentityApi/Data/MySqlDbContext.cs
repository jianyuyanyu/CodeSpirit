using CodeSpirit.Core;
using CodeSpirit.IdentityApi.EventHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CodeSpirit.IdentityApi.Data
{
    /// <summary>
    /// MySQL 特定的数据库上下文
    /// 用于迁移和MySQL特定的配置
    /// </summary>
    public class MySqlDbContext : ApplicationDbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        /// <param name="currentUser">当前用户</param>
        /// <param name="entityFileReferenceEventHandler">实体文件引用事件处理器</param>
        public MySqlDbContext(
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
            
            // 应用MySQL特定配置
            ApplyMySqlSpecificConfigurations(modelBuilder);
        }

        /// <summary>
        /// 应用MySQL特定的配置
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private void ApplyMySqlSpecificConfigurations(ModelBuilder modelBuilder)
        {
            Console.WriteLine($"正在为 {GetType().Name} 应用 MySQL 特定配置");
            
            // MySQL特定配置
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("datetime(6)");
                    }
                    
                    // MySQL字符串类型配置
                    if (property.ClrType == typeof(string))
                    {
                        var maxLength = property.GetMaxLength();
                        
                        // 特殊处理Identity相关字段，确保它们有合适的长度以支持索引
                        var propertyName = property.Name;
                        if (propertyName == "NormalizedName" || propertyName == "NormalizedUserName" || 
                            propertyName == "NormalizedEmail" || propertyName == "ConcurrencyStamp")
                        {
                            if (!maxLength.HasValue || maxLength.Value > 256)
                            {
                                property.SetMaxLength(256);
                                maxLength = 256;
                            }
                        }
                        
                        if (maxLength.HasValue && maxLength.Value > 255)
                        {
                            // 长文本使用TEXT类型，但Identity字段限制在256以内
                            if (maxLength.Value <= 1000)
                            {
                                property.SetColumnType($"varchar({maxLength.Value}) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                            }
                            else
                            {
                                property.SetColumnType("text");
                            }
                        }
                        else
                        {
                            // 短文本使用VARCHAR并设置字符集
                            property.SetColumnType($"varchar({maxLength ?? 255}) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                        }
                    }
                    
                    // MySQL布尔类型配置
                    if (property.ClrType == typeof(bool) || property.ClrType == typeof(bool?))
                    {
                        property.SetColumnType("tinyint(1)");
                    }
                }
            }
        }
    }
}
