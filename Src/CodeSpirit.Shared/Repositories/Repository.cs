using CodeSpirit.Core;
using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.Shared.Extensions.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public IQueryable<TEntity> CreateQuery()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<TEntity> AddAsync(TEntity entity, bool saveChanges = true)
        {
            await _dbSet.AddAsync(entity);
            if (saveChanges)
            {
                await SaveChangesAsync();
            }
            return entity;
        }

        public async Task UpdateAsync(TEntity entity, bool saveChanges = true)
        {
            _dbSet.Update(entity);
            if (saveChanges)
            {
                await SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(TEntity entity, bool saveChanges = true)
        {
            if (entity is ISoftDeleteAuditable softDeleteEntity)
            {
                softDeleteEntity.IsDeleted = true;
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }

            if (saveChanges)
            {
                await SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(object id, bool saveChanges = true)
        {
            TEntity entity = await GetByIdAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity, saveChanges);
            }
        }

        public async Task<TEntity> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Where(predicate).AsQueryable();
        }

        public async Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params string[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsQueryable();

            if (includes != null)
            {
                foreach (string include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            int totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            List<TEntity> items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PageList<TEntity>(items, totalCount);
        }

        public async Task<PageList<TEntity>> GetPagedAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>> predicate = null,
            string orderBy = null,
            string orderDir = null,
            params string[] includes)
        {
            IQueryable<TEntity> query = _dbSet.AsQueryable();

            if (includes != null)
            {
                foreach (string include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            int totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = query.ApplySorting(orderBy, orderDir);
            }

            List<TEntity> items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PageList<TEntity>(items, totalCount);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 异步检查是否存在满足条件的实体
        /// </summary>
        /// <param name="predicate">查询条件表达式</param>
        /// <returns>如果存在返回 true，否则返回 false</returns>
        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// 同步检查是否存在满足条件的实体
        /// </summary>
        /// <param name="predicate">查询条件表达式</param>
        /// <returns>如果存在返回 true，否则返回 false</returns>
        public bool Exists(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Any(predicate);
        }

        public Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            throw new NotImplementedException();
        }

        public Task<PageList<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> AddAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(object id)
        {
            throw new NotImplementedException();
        }
    }
}
