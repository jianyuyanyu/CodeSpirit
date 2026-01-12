using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services
{
    public interface IBaseCRUDService<TEntity, TDto, TKey, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TKey : IEquatable<TKey>
        where TCreateDto : class
        where TUpdateDto : class
    {
        /// <summary>
        /// 批量删除实体（软删除）
        /// </summary>
        /// <param name="ids">实体ID集合</param>
        Task<(int successCount, List<TKey> failedIds)> BatchDeleteAsync(IEnumerable<TKey> ids);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        /// <param name="ids">实体ID集合</param>
        /// <param name="hardDelete">是否硬删除（永久删除）</param>
        Task<(int successCount, List<TKey> failedIds)> BatchDeleteAsync(IEnumerable<TKey> ids, bool hardDelete);

        Task<TDto> CreateAsync(TCreateDto createDto);

        /// <summary>
        /// 删除实体（软删除）
        /// </summary>
        /// <param name="id">实体ID</param>
        Task DeleteAsync(TKey id);

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <param name="hardDelete">是否硬删除（永久删除）</param>
        Task DeleteAsync(TKey id, bool hardDelete);

        Task<TDto> GetAsync(TKey id);
        Task<PageList<TDto>> GetPagedListAsync(int page, int perPage, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null, params string[] includes);
        Task<PageList<TDto>> GetPagedListAsync<TQueryDto>(TQueryDto queryDto, Expression<Func<TEntity, bool>> predicate = null, params string[] includes) where TQueryDto : QueryDtoBase;
        Task UpdateAsync(TKey id, TUpdateDto updateDto);
        Task<IEnumerable<TDto>> GetAllAsync();
    }
} 