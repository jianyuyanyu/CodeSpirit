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
public abstract class BaseService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto>
    where TEntity : class
    where TDto : class
    where TKey : IEquatable<TKey>
    where TCreateDto : class
    where TUpdateDto : class
    where TBatchImportDto : class
{
    protected readonly IRepository<TEntity> Repository;
    protected readonly IMapper Mapper;

    protected BaseService(IRepository<TEntity> repository, IMapper mapper)
    {
        Repository = repository;
        Mapper = mapper;
    }

    /// <summary>
    /// 获取单个实体
    /// </summary>
    public virtual async Task<TDto> GetAsync(TKey id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return entity != null ? Mapper.Map<TDto>(entity) : null;
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public virtual async Task<PageList<TDto>> GetPagedListAsync(
        int page,
        int perPage,
        Expression<Func<TEntity, bool>> predicate = null,
        string orderBy = null,
        string orderDir = null)
    {
        var result = await Repository.GetPagedAsync(
            page,
            perPage,
            predicate,
            orderBy,
            orderDir
        );

        return Mapper.Map<PageList<TDto>>(result);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public virtual async Task<PageList<TDto>> GetPagedListAsync<TQueryDto>(TQueryDto queryDto, Expression<Func<TEntity, bool>> predicate = null) where TQueryDto : QueryDtoBase
    {
        var result = await Repository.GetPagedAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy,
            queryDto.OrderDir
        );

        return Mapper.Map<PageList<TDto>>(result);
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        await ValidateCreateDto(createDto);

        var entity = Mapper.Map<TEntity>(createDto);
        await OnCreating(entity);

        var createdEntity = await Repository.AddAsync(entity);
        await OnCreated(createdEntity);
        return createdEntity;
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public virtual async Task UpdateAsync(TUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        await ValidateUpdateDto(updateDto);

        var entity = await GetEntityForUpdate(updateDto);
        ArgumentNullException.ThrowIfNull(entity);

        Mapper.Map(updateDto, entity);
        await OnUpdating(entity);

        await Repository.UpdateAsync(entity);
        await OnUpdated(entity);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public virtual async Task DeleteAsync(TKey id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null) return;

        await OnDeleting(entity);
        await Repository.DeleteAsync(id);
        await OnDeleted(entity);
    }

    /// <summary>
    /// 批量导入
    /// </summary>
    public virtual async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<TBatchImportDto> importData)
    {
        ArgumentNullException.ThrowIfNull(importData);

        var successCount = 0;
        var failedIds = new List<string>();
        var validEntities = new List<TEntity>();

        foreach (var item in importData)
        {
            try
            {
                await ValidateImportItem(item);
                var entity = Mapper.Map<TEntity>(item);
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
            foreach (var entity in validEntities)
            {
                await Repository.AddAsync(entity);
                successCount++;
            }
        });

        return (successCount, failedIds);
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    public virtual async Task<(int successCount, List<TKey> failedIds)> BatchDeleteAsync(IEnumerable<TKey> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var successCount = 0;
        var failedIds = new List<TKey>();
        var distinctIds = ids.Distinct().ToList();

        await Repository.ExecuteInTransactionAsync(async () =>
        {
            foreach (var id in distinctIds)
            {
                var entity = await Repository.GetByIdAsync(id);
                if (entity == null)
                {
                    failedIds.Add(id);
                    continue;
                }

                await OnDeleting(entity);
                await Repository.DeleteAsync(id);
                successCount++;
            }
        });

        return (successCount, failedIds);
    }

    #region Protected Virtual Methods for Override

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected virtual Task ValidateCreateDto(TCreateDto createDto) => Task.CompletedTask;

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    protected virtual Task ValidateUpdateDto(TUpdateDto updateDto) => Task.CompletedTask;

    /// <summary>
    /// 验证导入项
    /// </summary>
    protected virtual Task ValidateImportItem(TBatchImportDto importDto) => Task.CompletedTask;

    /// <summary>
    /// 获取要更新的实体
    /// </summary>
    protected abstract Task<TEntity> GetEntityForUpdate(TUpdateDto updateDto);

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected abstract string GetImportItemId(TBatchImportDto importDto);

    /// <summary>
    /// 创建前的处理
    /// </summary>
    protected virtual Task OnCreating(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 更新前的处理
    /// </summary>
    protected virtual Task OnUpdating(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 删除前的处理
    /// </summary>
    protected virtual Task OnDeleting(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 导入前的处理
    /// </summary>
    protected virtual Task OnImporting(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 创建后的处理
    /// </summary>
    protected virtual Task OnCreated(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 更新后的处理
    /// </summary>
    protected virtual Task OnUpdated(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 删除后的处理
    /// </summary>
    protected virtual Task OnDeleted(TEntity entity) => Task.CompletedTask;

    #endregion
}