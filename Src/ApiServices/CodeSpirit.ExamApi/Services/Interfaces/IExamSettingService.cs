using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试设置服务接口
/// </summary>
public interface IExamSettingService : IBaseCRUDService<ExamSetting, ExamSettingDto, long, CreateExamSettingDto, UpdateExamSettingDto>, IScopedDependency
{
    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>考试设置分页列表</returns>
    Task<PageList<ExamSettingDto>> GetExamSettingsAsync(ExamSettingQueryDto queryDto);
    
    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>考试设置详情</returns>
    Task<ExamSettingDto> GetExamSettingDetailAsync(long id);
    
    /// <summary>
    /// 发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>操作结果</returns>
    Task PublishExamSettingAsync(long id);
    
    /// <summary>
    /// 取消发布考试设置
    /// </summary>
    /// <param name="id">考试设置ID</param>
    /// <returns>操作结果</returns>
    Task UnpublishExamSettingAsync(long id);
    
    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>可参加的考试列表</returns>
    Task<List<ClientExamDto>> GetAvailableExamsForClientAsync(long studentId);
    
    /// <summary>
    /// 获取考试详情（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试详情</returns>
    Task<ClientExamDetailDto> GetExamDetailForClientAsync(long examId, long recordId);
    
    /// <summary>
    /// 获取考试基本信息（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试基本信息</returns>
    Task<ClientExamBasicInfoDto> GetExamBasicInfoForClientAsync(long examId, long studentId, long? recordId = null);
    
    /// <summary>
    /// 获取考试轻量信息（客户端视图，用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    Task<ClientExamLightInfoDto> GetExamLightInfoForClientAsync(long examId, long studentId);
    
    /// <summary>
    /// 获取考试信息用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本配置信息，如果考试不存在则返回null</returns>
    Task<ExamBasicInfoCacheDto?> GetExamInfoForWarmupAsync(long examId);
    
    /// <summary>
    /// 获取考试题目数据用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典（QuestionId -> 题目详情），如果考试不存在则返回null</returns>
    Task<Dictionary<long, ClientExamQuestionDto>?> GetExamQuestionsForWarmupAsync(long examId);

} 