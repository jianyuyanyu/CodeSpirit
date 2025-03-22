using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Shared.Extensions
{
    /// <summary>
    /// DbSet扩展方法
    /// </summary>
    public static class DbSetExtensions
    {
        /// <summary>
        /// 重写DbSet的AddRangeAsync方法，添加前设置ID
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="dbSet">数据集</param>
        /// <param name="entities">实体集合</param>
        /// <returns>异步任务</returns>
        public static async Task AddRangeAsyncWithIdGeneration<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> entities) 
            where TEntity : class
        {
            // 获取DbContext
            var context = GetDbContext(dbSet);
            
            // 如果是AuditableDbContext，预设ID
            if (context is AuditableDbContext auditableContext)
            {
                auditableContext.OnAddRangeToDbSet(entities);
            }
            
            // 调用原生方法
            await dbSet.AddRangeAsync(entities);
        }
        
        /// <summary>
        /// 获取DbSet的DbContext
        /// </summary>
        private static DbContext GetDbContext<TEntity>(DbSet<TEntity> dbSet) where TEntity : class
        {
            // 通过反射获取内部DbContext字段
            var internalContext = dbSet.GetType()
                .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(dbSet);
                
            if (internalContext == null)
            {
                throw new InvalidOperationException("无法获取DbSet的DbContext");
            }
            
            // 可能需要进一步获取实际的DbContext对象，具体实现根据EF Core版本可能不同
            var context = internalContext.GetType()
                .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(internalContext) as DbContext;
                
            if (context == null)
            {
                // 尝试另一种获取方式
                context = internalContext as DbContext;
            }
            
            if (context == null)
            {
                throw new InvalidOperationException("无法获取DbContext");
            }
            
            return context;
        }
    }
} 