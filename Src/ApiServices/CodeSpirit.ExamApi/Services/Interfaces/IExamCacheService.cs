using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试缓存服务接口
/// </summary>
public interface IExamCacheService
{
    /// <summary>
    /// 获取考试基本信息（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本信息</returns>
    Task<ExamBasicInfoCacheDto?> GetExamBasicInfoWithCacheAsync(long examId);

    /// <summary>
    /// 获取考试题目数据（带缓存，字典格式）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典</returns>
    Task<Dictionary<long, ClientExamQuestionDto>?> GetExamQuestionsDataWithCacheAsync(long examId);

    /// <summary>
    /// 获取用户考试记录（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户考试记录</returns>
    Task<UserExamRecordCacheDto?> GetUserExamRecordWithCacheAsync(long examId, long userId);

    /// <summary>
    /// 获取用户已提交的答案（带缓存）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户答案列表</returns>
    Task<List<ClientExamAnswerDto>> GetSubmittedAnswersWithCacheAsync(long recordId, long userId);

    /// <summary>
    /// 清除用户答案缓存
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    Task ClearUserAnswersCacheAsync(long recordId, long userId);

    /// <summary>
    /// 刷新用户答案缓存（主动更新缓存数据）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    Task RefreshUserAnswersCacheAsync(long recordId, long userId);

    /// <summary>
    /// 直接更新用户答案缓存（使用已有数据，无需查询数据库）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="newAnswers">新保存的答案</param>
    Task UpdateUserAnswersCacheAsync(long recordId, long userId, List<ClientExamAnswerDto> newAnswers);

    /// <summary>
    /// 预热考试缓存（仅预热即将开始的考试）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>预热任务</returns>
    Task WarmupExamCacheAsync(long examId);

    /// <summary>
    /// 批量预热即将开始的考试缓存
    /// </summary>
    /// <returns>预热任务</returns>
    Task WarmupUpcomingExamsCacheAsync();

    /// <summary>
    /// 清空考试缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>清空任务</returns>
    Task ClearExamCacheAsync(long examId);

    /// <summary>
    /// 清除用户考试记录缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    Task ClearUserExamRecordCacheAsync(long examId, long userId);

    /// <summary>
    /// 获取学生信息（带缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>学生信息</returns>
    Task<StudentDto?> GetStudentInfoWithCacheAsync(long userId);

    /// <summary>
    /// 清除学生信息缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task ClearStudentInfoCacheAsync(long userId);

    /// <summary>
    /// 获取客户端档案信息（带缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>客户端档案信息</returns>
    Task<ClientProfileDto> GetClientProfileWithCacheAsync(long userId);

    /// <summary>
    /// 清除客户端档案信息缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task ClearClientProfileCacheAsync(long userId);

    /// <summary>
    /// 获取考试轻量信息（带缓存，用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    Task<ClientExamLightInfoDto?> GetExamLightInfoWithCacheAsync(long examId, long studentId);

    /// <summary>
    /// 清除考试轻量信息缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    Task ClearExamLightInfoCacheAsync(long examId, long studentId);
    
    /// <summary>
    /// 获取考试记录（带缓存，轻量级）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录缓存DTO</returns>
    Task<ExamRecordCacheDto?> GetExamRecordWithCacheAsync(long recordId);
    
    /// <summary>
    /// 清除考试记录缓存
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    Task ClearExamRecordCacheAsync(long recordId);
}
