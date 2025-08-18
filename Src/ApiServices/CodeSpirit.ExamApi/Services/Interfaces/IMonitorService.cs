using CodeSpirit.ExamApi.Dtos.Monitor;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 监考服务接口
/// </summary>
public interface IMonitorService
{
    /// <summary>
    /// 获取考试监控信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试监控信息</returns>
    Task<ExamMonitorDto> GetExamMonitorAsync(long examId);
    
    /// <summary>
    /// 获取考生监控信息
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考生监控信息</returns>
    Task<ExamStudentMonitorDto> GetStudentMonitorAsync(long recordId);
    
    /// <summary>
    /// 强制结束考生考试
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    Task TerminateStudentExamAsync(long recordId);
    
    /// <summary>
    /// 标记考生作弊
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="reason">作弊原因</param>
    Task FlagStudentCheatingAsync(long recordId, string reason);
} 