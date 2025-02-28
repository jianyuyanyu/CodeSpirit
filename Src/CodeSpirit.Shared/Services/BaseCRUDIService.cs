using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Repositories;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// 服务层抽象基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
/// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
public abstract class BaseCRUDIService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> : BaseCRUDService<TEntity, TDto, TKey, TCreateDto, TUpdateDto>, IBaseCRUDIService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> where TEntity : class
    where TDto : class
    where TKey : IEquatable<TKey>
    where TCreateDto : class
    where TUpdateDto : class
    where TBatchImportDto : class
{
    protected BaseCRUDIService(IRepository<TEntity> repository, IMapper mapper) : base(repository, mapper)
    {
    }

    /// <summary>
    /// 批量导入
    /// </summary>
    public virtual async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<TBatchImportDto> importData)
    {
        ArgumentNullException.ThrowIfNull(importData);

        int successCount = 0;
        List<string> failedIds = [];
        List<TEntity> validEntities = [];
        IEnumerable<TBatchImportDto> items = await ValidateImportItems(importData);

        foreach (TBatchImportDto item in items)
        {
            try
            {
                TEntity entity = Mapper.Map<TEntity>(item);
                OnImportMapping(entity, item);
                await OnImporting(entity);
                validEntities.Add(entity);
            }
            catch (Exception)
            {
                failedIds.Add(GetImportItemId(item) ?? "null");
            }
        }

        await Repository.ExecuteInTransactionAsync(async () =>
        {
            foreach (TEntity entity in validEntities)
            {
                await Repository.AddAsync(entity, false);
                successCount++;
            }
            await Repository.SaveChangesAsync();
        });

        return (successCount, failedIds);
    }

    #region Protected Virtual Methods for Override

    /// <summary>
    /// 验证导入项
    /// </summary>
    protected virtual Task<IEnumerable<TBatchImportDto>> ValidateImportItems(IEnumerable<TBatchImportDto> importData) => (Task<IEnumerable<TBatchImportDto>>)Task.CompletedTask;

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected abstract string GetImportItemId(TBatchImportDto importDto);

    /// <summary>
    /// 导入前的处理
    /// </summary>
    protected virtual Task OnImporting(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 导入时的映射后处理
    /// </summary>
    /// <param name="entity">映射后的实体</param>
    /// <param name="importDto">导入的DTO</param>
    protected virtual Task OnImportMapping(TEntity entity, TBatchImportDto importDto) => Task.CompletedTask;

    #endregion
}