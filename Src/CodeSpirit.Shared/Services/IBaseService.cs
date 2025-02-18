using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services
{
    public interface IBaseService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto>
        where TEntity : class
        where TDto : class
        where TKey : IEquatable<TKey>
        where TCreateDto : class
        where TUpdateDto : class
        where TBatchImportDto : class
    {
        Task<(int successCount, List<TKey> failedIds)> BatchDeleteAsync(IEnumerable<TKey> ids);
        Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<TBatchImportDto> importData);
        Task<TDto> CreateAsync(TCreateDto createDto);
        Task DeleteAsync(TKey id);
        Task<TDto> GetAsync(TKey id);
        Task<PageList<TDto>> GetPagedListAsync(int page, int perPage, Expression<Func<TEntity, bool>> predicate = null, string orderBy = null, string orderDir = null, params string[] includes);
        Task<PageList<TDto>> GetPagedListAsync<TQueryDto>(TQueryDto queryDto, Expression<Func<TEntity, bool>> predicate = null, params string[] includes) where TQueryDto : QueryDtoBase;
        Task UpdateAsync(TKey id, TUpdateDto updateDto);
    }
}