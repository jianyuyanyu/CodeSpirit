using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试客户端服务接口
/// </summary>
public interface IClientService : IScopedDependency
{
    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>可参加的考试列表</returns>
    Task<List<ClientExamDto>> GetAvailableExamsAsync(long userId);
    
    /// <summary>
    /// 获取用户考试历史记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>历史考试记录</returns>
    Task<List<ClientExamHistoryDto>> GetExamHistoryAsync(long userId);
    
    /// <summary>
    /// 获取考试详情并创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试详情</returns>
    Task<ClientExamDetailDto> GetExamDetailAsync(long examId, long userId, string userIp, string deviceInfo);
    
    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>提交结果，包含是否成功和是否可查看结果</returns>
    Task<(bool Success, bool EnableViewResult)> SubmitExamAsync(long recordId, long userId, List<ClientExamAnswerDto> answers = null);
    
    /// <summary>
    /// 保存考试答案但不提交
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>保存是否成功</returns>
    Task<bool> SaveAnswerAsync(long recordId, long userId, List<ClientExamAnswerDto> answers);
    
    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试结果</returns>
    Task<ClientExamResultDto> GetExamResultAsync(long recordId, long userId);

    /// <summary>
    /// 获取考试基本信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试基本信息</returns>
    Task<ClientExamBasicInfoDto> GetExamBasicInfoAsync(long examId, long userId);
    
    /// <summary>
    /// 获取考试轻量信息（用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试轻量信息</returns>
    Task<ClientExamLightInfoDto> GetExamLightInfoAsync(long examId, long userId);
    
    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord> CreateExamRecordAsync(long examId, long userId, string userIp, string deviceInfo);
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <returns>任务完成状态</returns>
    Task RecordScreenSwitchAsync(long recordId, long userId, string userIp);
    
    /// <summary>
    /// 获取考生个人信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>考生个人信息</returns>
    Task<ClientProfileDto> GetStudentProfileAsync(long userId);
    
    /// <summary>
    /// 获取考生个人信息（带二级缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>考生个人信息</returns>
    Task<ClientProfileDto> GetStudentProfileWithCacheAsync(long userId);
    
    /// <summary>
    /// 获取已提交的答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>已提交的答案列表</returns>
    Task<List<ClientExamAnswerDto>> GetSubmittedAnswersAsync(long recordId, long userId);
    
    /// <summary>
    /// 获取考试题目数据（带缓存，字典格式）
    /// 说明：缓存原始题目数据，不包含用户特定的题目顺序
    /// 用户的题目顺序保存在 AnswerRecord.OrderNumber 中
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典（QuestionId -> 题目详情）</returns>
    Task<Dictionary<long, ClientExamQuestionDto>> GetExamQuestionsDataWithCacheAsync(long examId);
    
    /// <summary>
    /// 获取考试基本信息（带缓存，不包含用户特定数据）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本信息缓存DTO</returns>
    Task<ExamBasicInfoCacheDto> GetExamBasicInfoWithCacheAsync(long examId);
    
    /// <summary>
    /// 获取用户考试记录信息（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户考试记录缓存DTO</returns>
    Task<UserExamRecordCacheDto> GetUserExamRecordWithCacheAsync(long examId, long userId);
    
    /// <summary>
    /// 获取用户已提交的答案（带缓存）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户答案列表</returns>
    Task<List<ClientExamAnswerDto>> GetSubmittedAnswersWithCacheAsync(long recordId, long userId);
    
    /// <summary>
    /// 预热考试缓存（提前加载考试数据到缓存）
    /// 说明：预热题目数据（字典格式）和基本信息，所有用户共享
    /// 用户的题目顺序保存在 AnswerRecord.OrderNumber 中，不需要预热
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">触发预热的用户ID（用于权限验证）</param>
    /// <returns>预热是否成功</returns>
    Task<bool> WarmUpExamCacheAsync(long examId, long userId);
    
    /// <summary>
    /// 清空考试相关的所有缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>清空是否成功</returns>
    Task<bool> ClearExamCacheAsync(long examId);
} 