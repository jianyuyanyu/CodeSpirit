using CodeSpirit.Core;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Repositories
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task DeleteAsync(object id);

        Task<TEntity> GetByIdAsync(object id);
        Task<IEnumerable<TEntity>> GetAllAsync();

        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<PageList<TEntity>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);

        Task ExecuteInTransactionAsync(Func<Task> operation);

        IQueryable<TEntity> CreateQuery();
        Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null);
    }
}
