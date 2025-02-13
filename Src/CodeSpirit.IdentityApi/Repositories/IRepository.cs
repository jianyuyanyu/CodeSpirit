using CodeSpirit.IdentityApi.Data;
using System.Linq.Expressions;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IRepository<T, TKey> where T : class
    {
        // 获取所有实体
        Task<IEnumerable<T>> GetAllAsync();

        // 根据条件获取实体
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // 根据ID获取实体
        Task<T> GetByIdAsync(TKey id);

        // 添加新实体
        Task AddAsync(T entity);

        // 添加多个实体
        Task AddRangeAsync(IEnumerable<T> entities);

        // 更新实体
        void Update(T entity);

        // 删除实体
        void Remove(T entity);

        // 删除多个实体
        void RemoveRange(IEnumerable<T> entities);

        // 保存更改
        Task<int> SaveChangesAsync();
        ApplicationDbContext GetDbContext();
    }
}
