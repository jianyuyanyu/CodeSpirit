using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.Shared.Services;
using CodeSpirit.Core.Dtos;

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
    /// 获取练习会话分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>练习会话分页列表</returns>
    Task<PageList<PracticeSessionDto>> GetPracticeSessionsAsync(PracticeSessionQueryDto queryDto);
    
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
    /// <returns>操作结果</returns>
    Task UpdatePracticeRecordAsync(long id, UpdatePracticeRecordDto updateDto);
    
    /// <summary>
    /// 删除练习记录
    /// </summary>
    /// <param name="id">练习记录ID</param>
    /// <returns>操作结果</returns>
    Task DeletePracticeRecordAsync(long id);
    
    /// <summary>
    /// 批量删除练习记录
    /// </summary>
    /// <param name="ids">ID列表</param>
    /// <returns>成功删除数量和失败ID列表</returns>
    Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids);
    
    /// <summary>
    /// 批量导入练习记录
    /// </summary>
    /// <param name="importDtos">导入数据</param>
    /// <returns>成功导入数量和失败ID列表</returns>
    Task<(int successCount, List<string> failedIds)> BatchImportAsync(List<PracticeRecordBatchImportDto> importDtos);
    
    /// <summary>
    /// 批量创建练习记录
    /// </summary>
    /// <param name="practiceRecordDtos">练习记录DTO列表</param>
    /// <returns>创建的练习记录列表</returns>
    Task<List<PracticeRecordDto>> BatchCreatePracticeRecordsAsync(List<PracticeRecordDto> practiceRecordDtos);
    
    /// <summary>
    /// 获取学生的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>练习统计数据</returns>
    Task<PracticeStatisticsDto> GetStudentPracticeStatisticsAsync(long studentId);
    
    /// <summary>
    /// 获取学生对特定试卷的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>练习统计数据</returns>
    Task<PracticeStatisticsDto> GetStudentExamPaperPracticeStatisticsAsync(long studentId, long examPaperId);
    
    /// <summary>
    /// 获取学生对特定练习设置的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="practiceSettingId">练习设置ID</param>
    /// <returns>练习统计数据</returns>
    Task<PracticeStatisticsDto> GetStudentPracticeSettingStatisticsAsync(long studentId, long practiceSettingId);

    /// <summary>
    /// 开始练习
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="practiceSettingId">练习设置ID</param>
    /// <returns>练习记录ID</returns>
    Task<long> StartPracticeAsync(long studentId, long practiceSettingId);
    
    /// <summary>
    /// 保存答案
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answerDto">答案DTO</param>
    Task SaveAnswerAsync(long recordId, long studentId, PracticeAnswerDto answerDto);
    
    /// <summary>
    /// 提交练习
    /// </summary>
    /// <param name="recordId">练习会话ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">答案列表</param>
    Task SubmitPracticeAsync(long recordId, long studentId, List<PracticeAnswerDto> answers);
    
    /// <summary>
    /// 获取练习结果
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>练习结果</returns>
    Task<PracticeResultDto> GetPracticeResultAsync(long recordId, long studentId);
} 