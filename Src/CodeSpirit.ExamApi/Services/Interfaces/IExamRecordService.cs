using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.Shared.Services;
using CodeSpirit.Core.Dtos;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试记录服务接口
/// </summary>
public interface IExamRecordService : IBaseCRUDService<ExamRecord, ExamRecordDto, long, StartExamDto, object>
{
    /// <summary>
    /// 开始考试
    /// </summary>
    /// <param name="startExamDto">开始考试参数</param>
    /// <returns>考试记录DTO</returns>
    Task<ExamRecordDto> StartExamAsync(StartExamDto startExamDto);
    
    /// <summary>
    /// 提交答案
    /// </summary>
    /// <param name="submitAnswerDto">提交答案参数</param>
    /// <returns>是否成功</returns>
    Task<bool> SubmitAnswerAsync(SubmitAnswerDto submitAnswerDto);
    
    /// <summary>
    /// 批量提交答案
    /// </summary>
    /// <param name="examRecordId">考试记录ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>是否全部成功</returns>
    Task<bool> SubmitAnswersAsync(long examRecordId, List<ClientExamAnswerDto> answers);
    
    /// <summary>
    /// 完成考试
    /// </summary>
    /// <param name="finishExamDto">完成考试参数</param>
    /// <returns>考试结果</returns>
    Task<ExamRecordDto> FinishExamAsync(FinishExamDto finishExamDto);
    
    /// <summary>
    /// 获取考试统计
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <returns>考试统计DTO</returns>
    Task<ExamStatisticsDto> GetExamStatisticsAsync(long examSettingId);
    
    /// <summary>
    /// 获取错题列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>错题列表</returns>
    Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto);
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>是否成功</returns>
    Task<bool> RecordScreenSwitchAsync(long recordId);
    
    /// <summary>
    /// 获取考试记录及答题详情
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录详情</returns>
    Task<ExamRecordDto> GetExamRecordDetailAsync(long recordId);
    /// <summary>
    /// 获取答题预览要素
    /// </summary>
    /// <param name="recordId"></param>
    /// <returns></returns>
    Task<AnswerPreviewDto> GetAnswerPreviewAsync(long recordId);
    
    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试记录</returns>
    Task<ExamRecord> CreateExamRecordAsync(long examId, long studentId, string userIp, string deviceInfo);
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <returns>任务完成状态</returns>
    Task RecordScreenSwitchForClientAsync(long recordId, long studentId, string userIp);
    
    /// <summary>
    /// 客户端提交考试
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">可选的答案列表，用于最后提交前保存</param>
    /// <returns>提交结果和是否可查看结果</returns>
    Task<(bool Success, bool EnableViewResult)> SubmitExamForClientAsync(long recordId, long studentId, List<ClientExamAnswerDto> answers = null);
    
    /// <summary>
    /// 获取用户的考试历史记录
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>历史考试记录</returns>
    Task<List<ClientExamHistoryDto>> GetExamHistoryForClientAsync(long studentId);
    
    /// <summary>
    /// 获取考试结果（客户端视图）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试结果</returns>
    Task<ClientExamResultDto> GetExamResultForClientAsync(long recordId, long studentId);
} 