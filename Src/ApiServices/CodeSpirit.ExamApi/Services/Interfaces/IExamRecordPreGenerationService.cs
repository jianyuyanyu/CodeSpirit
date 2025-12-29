namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试记录预生成服务接口
/// </summary>
public interface IExamRecordPreGenerationService
{
    /// <summary>
    /// 为指定考试预生成所有学生的考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预生成结果</returns>
    Task<PreGenerationResult> PreGenerateExamRecordsAsync(long examId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 为指定学生批量预生成考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentIds">学生ID列表</param>
    /// <param name="attemptNumber">考试次数（默认第1次）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预生成结果</returns>
    Task<PreGenerationResult> PreGenerateBatchAsync(
        long examId, 
        IEnumerable<long> studentIds, 
        int attemptNumber = 1,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 预生成结果
/// </summary>
public class PreGenerationResult
{
    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }
    
    /// <summary>
    /// 跳过数量（已存在）
    /// </summary>
    public int SkippedCount { get; set; }
    
    /// <summary>
    /// 失败的学生ID列表
    /// </summary>
    public List<long> FailedStudentIds { get; set; } = new();
}

