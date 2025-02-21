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
public abstract class BaseService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> : IBaseService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> where TEntity : class
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
        TEntity entity = await Repository.GetByIdAsync(id);
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
        string orderDir = null,
        params string[] includes)
    {
        PageList<TEntity> result = await Repository.GetPagedAsync(
            page,
            perPage,
            predicate,
            orderBy,
            orderDir,
            includes
        );

        return Mapper.Map<PageList<TDto>>(result);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    public virtual async Task<PageList<TDto>> GetPagedListAsync<TQueryDto>(TQueryDto queryDto, Expression<Func<TEntity, bool>> predicate = null,
        params string[] includes) where TQueryDto : QueryDtoBase
    {
        PageList<TEntity> result = await Repository.GetPagedAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy,
            queryDto.OrderDir,
            includes
        );

        return Mapper.Map<PageList<TDto>>(result);
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        await ValidateCreateDto(createDto);

        TEntity entity = Mapper.Map<TEntity>(createDto);
        await OnCreating(entity, createDto);

        TEntity createdEntity = await Repository.AddAsync(entity);
        await OnCreated(createdEntity, createDto);
        TDto dto = Mapper.Map<TDto>(createdEntity);
        return dto;
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public virtual async Task UpdateAsync(TKey id, TUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        await ValidateUpdateDto(id, updateDto);

        TEntity entity = await GetEntityForUpdate(id, updateDto);
        ArgumentNullException.ThrowIfNull(entity);

        Mapper.Map(updateDto, entity);
        await OnUpdating(entity, updateDto);

        await Repository.UpdateAsync(entity);
        await OnUpdated(entity);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public virtual async Task DeleteAsync(TKey id)
    {
        TEntity entity = await Repository.GetByIdAsync(id);
        if (entity == null)
        {
            return;
        }

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

    /// <summary>
    /// 批量删除
    /// </summary>
    public virtual async Task<(int successCount, List<TKey> failedIds)> BatchDeleteAsync(IEnumerable<TKey> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        int successCount = 0;
        List<TKey> failedIds = [];
        List<TKey> distinctIds = ids.Distinct().ToList();
        if (!distinctIds.Any())
        {
            throw new AppServiceException(400, "无有效的ID！");
        }

        await Repository.ExecuteInTransactionAsync(async () =>
        {
            foreach (TKey id in distinctIds)
            {
                TEntity entity = await Repository.GetByIdAsync(id);
                if (entity == null)
                {
                    failedIds.Add(id);
                    continue;
                }

                await OnDeleting(entity);
                await Repository.DeleteAsync(id);
                await OnDeleted(entity);
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
    protected virtual Task ValidateUpdateDto(TKey id, TUpdateDto updateDto) => Task.CompletedTask;

    /// <summary>
    /// 验证导入项
    /// </summary>
    protected virtual Task<IEnumerable<TBatchImportDto>> ValidateImportItems(IEnumerable<TBatchImportDto> importData) => (Task<IEnumerable<TBatchImportDto>>)Task.CompletedTask;

    /// <summary>
    /// 获取要更新的实体
    /// </summary>
    protected virtual async Task<TEntity> GetEntityForUpdate(TKey id, TUpdateDto updateDto)
    {
        TEntity entity = await Repository.GetByIdAsync(id);
        return entity == null ? throw new AppServiceException(404, "实体不存在！") : entity;
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected abstract string GetImportItemId(TBatchImportDto importDto);

    /// <summary>
    /// 创建前的处理
    /// </summary>
    protected virtual Task OnCreating(TEntity entity, TCreateDto createDto) => Task.CompletedTask;

    /// <summary>
    /// 更新前的处理
    /// </summary>
    protected virtual Task OnUpdating(TEntity entity, TUpdateDto updateDto) => Task.CompletedTask;

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
    protected virtual Task OnCreated(TEntity entity, TCreateDto createDto) => Task.CompletedTask;

    /// <summary>
    /// 更新后的处理
    /// </summary>
    protected virtual Task OnUpdated(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 删除后的处理
    /// </summary>
    protected virtual Task OnDeleted(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// 导入时的映射后处理
    /// </summary>
    /// <param name="entity">映射后的实体</param>
    /// <param name="importDto">导入的DTO</param>
    protected virtual Task OnImportMapping(TEntity entity, TBatchImportDto importDto) => Task.CompletedTask;

    #endregion
}