using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Client;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试客户端服务接口
/// </summary>
public interface IClientService
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
    /// <returns>是否提交成功</returns>
    Task<bool> SubmitExamAsync(long recordId, long userId, List<ClientExamAnswerDto> answers);
    
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
    Task<ExamRecord> CreateExamRecordAsync(long examId, long userId, string userIp, string deviceInfo);

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    Task<bool> RecordScreenSwitchAsync(long recordId, long userId);
} 