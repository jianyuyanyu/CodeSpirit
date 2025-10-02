#nullable enable
using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Dtos.Common;
using System.Linq.Expressions;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 带批量导入功能的服务接口
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">DTO类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <typeparam name="TCreateDto">创建DTO类型</typeparam>
    /// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
    /// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
    public interface IBaseCRUDIService<TEntity, TDto, TKey, TCreateDto, TUpdateDto, TBatchImportDto> : 
        IBaseCRUDService<TEntity, TDto, TKey, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TKey : IEquatable<TKey>
        where TCreateDto : class
        where TUpdateDto : class
        where TBatchImportDto : class
    {
        /// <summary>
        /// 批量导入（简单版本，向后兼容）
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <returns>成功数量和失败ID列表</returns>
        Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<TBatchImportDto> importData);

        /// <summary>
        /// 增强的批量导入
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <returns>导入结果</returns>
        Task<BatchImportResultDto> EnhancedBatchImportAsync(IEnumerable<TBatchImportDto> importData);

        /// <summary>
        /// 获取导入结果
        /// </summary>
        /// <param name="importId">导入ID</param>
        /// <returns>导入结果</returns>
        Task<BatchImportResultDto?> GetImportResultAsync(string importId);

        /// <summary>
        /// 导出失败记录
        /// </summary>
        /// <param name="failedRecords">失败记录</param>
        /// <returns>Excel文件字节数组</returns>
        Task<byte[]> ExportFailedRecordsAsync(List<ImportFailedRecord> failedRecords);
    }
} 