using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services
{
    public interface IBaseCRUDIService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> : 
        IBaseCRUDService<TEntity, TDto, TKey, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TKey : IEquatable<TKey>
        where TCreateDto : class
        where TUpdateDto : class
        where TBatchImportDto : class
    {
        Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<TBatchImportDto> importData);
    }
} 