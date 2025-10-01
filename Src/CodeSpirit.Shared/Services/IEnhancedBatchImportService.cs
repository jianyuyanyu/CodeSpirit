using CodeSpirit.Shared.Dtos.Common;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 增强的批量导入服务接口（混入接口）
    /// </summary>
    /// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
    public interface IEnhancedBatchImportService<TBatchImportDto> where TBatchImportDto : class
    {
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

    /// <summary>
    /// 增强的批量导入服务混入实现
    /// </summary>
    /// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
    public class EnhancedBatchImportMixin<TBatchImportDto> : IEnhancedBatchImportService<TBatchImportDto> 
        where TBatchImportDto : class
    {
        private readonly EnhancedBatchImportHelper<TBatchImportDto> _helper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="helper">批量导入助手</param>
        public EnhancedBatchImportMixin(EnhancedBatchImportHelper<TBatchImportDto> helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// 增强的批量导入（需要子类实现具体的导入逻辑）
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <returns>导入结果</returns>
        public virtual async Task<BatchImportResultDto> EnhancedBatchImportAsync(IEnumerable<TBatchImportDto> importData)
        {
            throw new NotImplementedException("子类必须实现具体的导入逻辑");
        }

        /// <summary>
        /// 获取导入结果
        /// </summary>
        /// <param name="importId">导入ID</param>
        /// <returns>导入结果</returns>
        public async Task<BatchImportResultDto?> GetImportResultAsync(string importId)
        {
            return await _helper.GetImportResultAsync(importId);
        }

        /// <summary>
        /// 导出失败记录
        /// </summary>
        /// <param name="failedRecords">失败记录</param>
        /// <returns>Excel文件字节数组</returns>
        public async Task<byte[]> ExportFailedRecordsAsync(List<ImportFailedRecord> failedRecords)
        {
            return await _helper.ExportFailedRecordsAsync(failedRecords);
        }

        /// <summary>
        /// 受保护的助手方法，供子类使用
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <param name="importProcessor">导入处理器</param>
        /// <param name="validator">自定义验证器</param>
        /// <returns>导入结果</returns>
        protected async Task<BatchImportResultDto> ExecuteEnhancedBatchImportAsync(
            IEnumerable<TBatchImportDto> importData,
            Func<TBatchImportDto, int, Task<string?>> importProcessor,
            Func<TBatchImportDto, int, Task<List<ValidationError>>>? validator = null)
        {
            return await _helper.EnhancedBatchImportAsync(importData, importProcessor, validator);
        }
    }
}
