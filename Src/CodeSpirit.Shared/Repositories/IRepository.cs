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

        Task ExecuteInTransactionAsync(Func<Task> operation);

        IQueryable<TEntity> CreateQuery();
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
        bool Exists(Expression<Func<TEntity, bool>> predicate);
        Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null, params string[] includes);
        Task<int> SaveChangesAsync();
    }
}
