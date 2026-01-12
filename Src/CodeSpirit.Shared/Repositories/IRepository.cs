using CodeSpirit.Core;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity, bool saveChanges = true);
        Task UpdateAsync(TEntity entity, bool saveChanges = true);
        Task DeleteAsync(TEntity entity, bool saveChanges = true);
        Task DeleteAsync(object id, bool saveChanges = true);

        Task<TEntity> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();

        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<PageList<TEntity>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params string[] includes);

        /// <summary>
        /// 在事务中执行操作（支持重试执行策略）
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        Task ExecuteInTransactionAsync(Func<Task> operation);

        IQueryable<TEntity> CreateQuery();
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
        bool Exists(Expression<Func<TEntity, bool>> predicate);
        Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null, params string[] includes);
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <param name="saveChanges">是否立即保存更改</param>
        /// <returns>添加的实体集合</returns>
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, bool saveChanges = true);

        /// <summary>
        /// 批量更新实体
        /// </summary>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>异步任务</returns>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        /// <param name="entities">要更新的实体集合</param>
        /// <returns>异步任务</returns>
        Task DeleteRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 硬删除实体（永久删除，不受软删除机制影响）
        /// </summary>
        /// <param name="entity">要删除的实体</param>
        /// <param name="saveChanges">是否立即保存更改</param>
        Task HardDeleteAsync(TEntity entity, bool saveChanges = true);

        /// <summary>
        /// 硬删除实体（永久删除，不受软删除机制影响）
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="saveChanges">是否立即保存更改</param>
        Task HardDeleteAsync(object id, bool saveChanges = true);

        /// <summary>
        /// 批量硬删除实体（永久删除，不受软删除机制影响）
        /// </summary>
        /// <param name="entities">要删除的实体集合</param>
        /// <param name="saveChanges">是否立即保存更改</param>
        Task HardDeleteRangeAsync(IEnumerable<TEntity> entities, bool saveChanges = true);

    }
}
