#nullable enable
using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Dtos.Common;
using CodeSpirit.Shared.Repositories;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// 服务层抽象基类（支持批量导入）
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
    /// <summary>
    /// 增强批量导入助手
    /// </summary>
    protected readonly EnhancedBatchImportHelper<TBatchImportDto> ImportHelper;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="importHelper">增强批量导入助手</param>
    protected BaseCRUDIService(
        IRepository<TEntity> repository, 
        IMapper mapper,
        EnhancedBatchImportHelper<TBatchImportDto> importHelper) : base(repository, mapper)
    {
        ImportHelper = importHelper;
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
                await OnImportMapping(entity, item);
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

    #region Enhanced Batch Import Methods

    /// <summary>
    /// 增强的批量导入
    /// </summary>
    /// <param name="importData">导入数据</param>
    /// <returns>导入结果</returns>
    public virtual async Task<BatchImportResultDto> EnhancedBatchImportAsync(IEnumerable<TBatchImportDto> importData)
    {
        return await ImportHelper.EnhancedBatchImportAsync(
            importData,
            ProcessImportItemAsync,
            ValidateImportItemAsync
        );
    }

    /// <summary>
    /// 获取导入结果
    /// </summary>
    /// <param name="importId">导入ID</param>
    /// <returns>导入结果</returns>
    public async Task<BatchImportResultDto?> GetImportResultAsync(string importId)
    {
        return await ImportHelper.GetImportResultAsync(importId);
    }

    /// <summary>
    /// 导出失败记录
    /// </summary>
    /// <param name="failedRecords">失败记录</param>
    /// <returns>Excel文件字节数组</returns>
    public async Task<byte[]> ExportFailedRecordsAsync(List<ImportFailedRecord> failedRecords)
    {
        return await ImportHelper.ExportFailedRecordsAsync(failedRecords);
    }

    /// <summary>
    /// 处理单条导入数据（子类必须实现）
    /// </summary>
    /// <param name="importDto">导入DTO</param>
    /// <param name="index">索引</param>
    /// <returns>错误消息（null表示成功）</returns>
    protected virtual async Task<string?> ProcessImportItemAsync(TBatchImportDto importDto, int index)
    {
        try
        {
            // 默认实现：映射并创建实体
            var entity = Mapper.Map<TEntity>(importDto);
            await OnImportMapping(entity, importDto);
            await OnImporting(entity);
            await Repository.AddAsync(entity, false);
            await Repository.SaveChangesAsync();
            
            return null; // 成功
        }
        catch (Exception ex)
        {
            return ex.Message; // 返回错误消息
        }
    }

    /// <summary>
    /// 验证单条导入数据（子类可选择性重写以实现自定义验证）
    /// </summary>
    /// <param name="importDto">导入DTO</param>
    /// <param name="index">索引</param>
    /// <returns>验证错误列表</returns>
    protected virtual Task<List<ValidationError>> ValidateImportItemAsync(TBatchImportDto importDto, int index)
    {
        // 默认不进行额外的自定义验证（DataAnnotations验证由Helper自动处理）
        return Task.FromResult(new List<ValidationError>());
    }

    #endregion
}