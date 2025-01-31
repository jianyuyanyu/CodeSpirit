using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface ICRUDRepository<TKey, TEntity, TDto, TCreateDto, TUpdateDto, TQueryDto>
    where TEntity : class
    where TDto : class
    where TCreateDto : ICreateDto
    where TUpdateDto : IUpdateDto
    where TQueryDto : QueryDtoBase
    {
        Task<ListData<TDto>> GetAllAsync(TQueryDto parameters);
        Task<TDto> GetByIdAsync(TKey id);
        Task<(bool Success, TKey Id)> CreateAsync(TCreateDto createDto);
        Task<bool> UpdateAsync(TKey id, TUpdateDto updateDto);
        Task<bool> DeleteAsync(TKey id);
    }
}
