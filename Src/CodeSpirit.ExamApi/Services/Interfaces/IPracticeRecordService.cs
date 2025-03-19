using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 练习记录服务接口
/// </summary>
public interface IPracticeRecordService : IBaseCRUDIService<
    PracticeRecord,
    PracticeRecordDto,
    long,
    CreatePracticeRecordDto,
    UpdatePracticeRecordDto,
    PracticeRecordBatchImportDto>
{
    /// <summary>
    /// 获取练习记录分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习记录分页列表</returns>
    Task<PageList<PracticeRecordDto>> GetPracticeRecordsAsync(PracticeRecordQueryDto queryDto);
    
    /// <summary>
    /// 获取练习记录详情
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <returns>练习记录详情</returns>
    Task<PracticeRecordDto> GetPracticeRecordAsync(long id);
    
    /// <summary>
    /// 创建练习记录
    /// </summary>
    /// <param name="createDto">创建练习记录DTO</param>
    /// <returns>创建的练习记录</returns>
    Task<PracticeRecordDto> CreatePracticeRecordAsync(CreatePracticeRecordDto createDto);
    
    /// <summary>
    /// 更新练习记录
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <param name="updateDto">更新练习记录DTO</param>
    Task UpdatePracticeRecordAsync(long id, UpdatePracticeRecordDto updateDto);
    
    /// <summary>
    /// 删除练习记录
    /// </summary>
    /// <param name="id">练习记录ID</param>
    Task DeletePracticeRecordAsync(long id);
    
    /// <summary>
    /// 批量删除练习记录
    /// </summary>
    /// <param name="ids">练习记录ID列表</param>
    /// <returns>成功删除数量和失败ID列表</returns>
    Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids);
    
    /// <summary>
    /// 批量导入练习记录
    /// </summary>
    /// <param name="importData">导入数据</param>
    /// <returns>成功导入数量和失败ID列表</returns>
    Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<PracticeRecordBatchImportDto> importData);
} 