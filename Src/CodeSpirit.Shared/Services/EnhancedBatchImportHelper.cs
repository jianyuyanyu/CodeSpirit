using CodeSpirit.Shared.Dtos.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CodeSpirit.Shared.Services
{
    /// <summary>
    /// 增强的批量导入助手类（使用组合模式）
    /// </summary>
    /// <typeparam name="TBatchImportDto">批量导入DTO类型</typeparam>
    public class EnhancedBatchImportHelper<TBatchImportDto> where TBatchImportDto : class
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<EnhancedBatchImportHelper<TBatchImportDto>> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="distributedCache">分布式缓存</param>
        /// <param name="logger">日志</param>
        public EnhancedBatchImportHelper(
            IDistributedCache distributedCache,
            ILogger<EnhancedBatchImportHelper<TBatchImportDto>> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        /// <summary>
        /// 增强的批量导入
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <param name="importProcessor">导入处理器，返回null表示成功，返回错误消息表示失败</param>
        /// <param name="validator">自定义验证器</param>
        /// <returns>导入结果</returns>
        public async Task<BatchImportResultDto> EnhancedBatchImportAsync(
            IEnumerable<TBatchImportDto> importData,
            Func<TBatchImportDto, int, Task<string?>> importProcessor,
            Func<TBatchImportDto, int, Task<List<ValidationError>>>? validator = null)
        {
            var importId = Guid.NewGuid().ToString();
            var result = new BatchImportResultDto
            {
                ImportId = importId,
                StartTime = DateTime.UtcNow,
                Status = ImportStatus.Processing
            };

            // 缓存导入结果，用于后续查询
            await SetImportResultCacheAsync(importId, result);

            try
            {
                var importList = importData.ToList();
                result.TotalCount = importList.Count;

                // 验证导入数据
                var validationResults = await ValidateImportDataAsync(importList, validator);
                
                var validItems = new List<(TBatchImportDto dto, int index)>();
                var failedRecords = new List<ImportFailedRecord>();

                for (int i = 0; i < importList.Count; i++)
                {
                    var item = importList[i];
                    var itemValidationResults = validationResults.Where(v => v.Index == i).ToList();
                    
                    if (itemValidationResults.Any())
                    {
                        // 有验证错误
                        failedRecords.Add(new ImportFailedRecord
                        {
                            RowIndex = i + 1,
                            ErrorMessage = string.Join("; ", itemValidationResults.Select(v => v.ErrorMessage)),
                            ErrorFields = itemValidationResults.SelectMany(v => v.ErrorFields).ToList()
                        });
                    }
                    else
                    {
                        validItems.Add((item, i));
                    }
                }

                // 处理有效数据
                var successCount = 0;
                foreach (var (dto, index) in validItems)
                {
                    try
                    {
                        var errorMessage = await importProcessor(dto, index);
                        if (errorMessage == null)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedRecords.Add(new ImportFailedRecord
                            {
                                RowIndex = index + 1,
                                ErrorMessage = errorMessage,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "导入第{Index}行数据失败: {Error}", index + 1, ex.Message);
                        failedRecords.Add(new ImportFailedRecord
                        {
                            RowIndex = index + 1,
                            ErrorMessage = ex.Message,
                        });
                    }
                }

                // 更新结果
                result.SuccessCount = successCount;
                result.FailedCount = failedRecords.Count;
                result.FailedRecords = failedRecords.OrderBy(r => r.RowIndex).ToList();
                result.EndTime = DateTime.UtcNow;
                
                if (result.FailedCount == 0)
                {
                    result.Status = ImportStatus.Success;
                    result.Message = $"成功导入 {result.SuccessCount} 条记录";
                }
                else if (result.SuccessCount > 0)
                {
                    result.Status = ImportStatus.PartialSuccess;
                    result.Message = $"成功导入 {result.SuccessCount} 条记录，失败 {result.FailedCount} 条记录";
                }
                else
                {
                    result.Status = ImportStatus.Failed;
                    result.Message = $"导入失败，共 {result.FailedCount} 条记录存在错误";
                }

                // 更新缓存
                await SetImportResultCacheAsync(importId, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入过程中发生异常: {Error}", ex.Message);
                
                result.Status = ImportStatus.Failed;
                result.Message = $"导入过程中发生异常：{ex.Message}";
                result.EndTime = DateTime.UtcNow;
                
                await SetImportResultCacheAsync(importId, result);
                
                return result;
            }
        }

        /// <summary>
        /// 获取导入结果
        /// </summary>
        /// <param name="importId">导入ID</param>
        /// <returns>导入结果</returns>
        public async Task<BatchImportResultDto?> GetImportResultAsync(string importId)
        {
            var cacheKey = $"import_result_{importId}";
            var cachedResult = await _distributedCache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(cachedResult))
                return null;
                
            return JsonSerializer.Deserialize<BatchImportResultDto>(cachedResult);
        }

        /// <summary>
        /// 导出失败记录
        /// </summary>
        /// <param name="failedRecords">失败记录</param>
        /// <returns>Excel文件字节数组</returns>
        public async Task<byte[]> ExportFailedRecordsAsync(List<ImportFailedRecord> failedRecords)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("导入失败记录");
                
                // 设置表头
                worksheet.Cell(1, 1).Value = "行号";
                worksheet.Cell(1, 2).Value = "错误信息";
                worksheet.Cell(1, 3).Value = "错误字段";
                
                // 设置表头样式
                var headerRange = worksheet.Range(1, 1, 1, 3);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                
                // 填充数据（按行号排序）
                var sortedFailedRecords = failedRecords.OrderBy(r => r.RowIndex).ToList();
                for (int i = 0; i < sortedFailedRecords.Count; i++)
                {
                    var record = sortedFailedRecords[i];
                    var row = i + 2;
                    
                    worksheet.Cell(row, 1).Value = record.RowIndex;
                    worksheet.Cell(row, 2).Value = record.ErrorMessage;
                    worksheet.Cell(row, 3).Value = string.Join(", ", record.ErrorFields);
                }
                
                // 自动调整列宽
                worksheet.ColumnsUsed().AdjustToContents();
                
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            });
        }

        #region Private Methods

        /// <summary>
        /// 设置导入结果缓存
        /// </summary>
        /// <param name="importId">导入ID</param>
        /// <param name="result">导入结果</param>
        private async Task SetImportResultCacheAsync(string importId, BatchImportResultDto result)
        {
            var cacheKey = $"import_result_{importId}";
            var serializedResult = JsonSerializer.Serialize(result);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
            
            await _distributedCache.SetStringAsync(cacheKey, serializedResult, options);
        }

        /// <summary>
        /// 验证导入数据
        /// </summary>
        /// <param name="importData">导入数据</param>
        /// <param name="customValidator">自定义验证器</param>
        /// <returns>验证结果</returns>
        private async Task<List<ValidationError>> ValidateImportDataAsync(
            List<TBatchImportDto> importData,
            Func<TBatchImportDto, int, Task<List<ValidationError>>>? customValidator)
        {
            var results = new List<ValidationError>();
            
            for (int i = 0; i < importData.Count; i++)
            {
                var item = importData[i];
                
                // DataAnnotations验证
                var validationContext = new ValidationContext(item);
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                
                if (!Validator.TryValidateObject(item, validationContext, validationResults, true))
                {
                    foreach (var validationResult in validationResults)
                    {
                        results.Add(new ValidationError
                        {
                            Index = i,
                            ErrorMessage = validationResult.ErrorMessage ?? "验证失败",
                            ErrorFields = validationResult.MemberNames.ToList()
                        });
                    }
                }
                
                // 自定义验证
                if (customValidator != null)
                {
                    var customValidationResults = await customValidator(item, i);
                    results.AddRange(customValidationResults);
                }
            }
            
            return results;
        }

        #endregion
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 错误字段
        /// </summary>
        public List<string> ErrorFields { get; set; } = new List<string>();
    }
}
