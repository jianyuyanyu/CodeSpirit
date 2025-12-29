using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ScheduledTasks.Services;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.Tasks;

/// <summary>
/// 考试记录预生成任务处理器
/// </summary>
public class ExamRecordPreGenerationTaskHandler : ITaskHandler
{
    private readonly IExamRecordPreGenerationService _preGenerationService;
    private readonly ILogger<ExamRecordPreGenerationTaskHandler> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamRecordPreGenerationTaskHandler(
        IExamRecordPreGenerationService preGenerationService,
        ILogger<ExamRecordPreGenerationTaskHandler> logger)
    {
        _preGenerationService = preGenerationService;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行预生成任务
    /// </summary>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("考试记录预生成任务开始执行");
        _logger.LogInformation("========================================");
        
        try
        {
            // 解析参数
            if (string.IsNullOrEmpty(parameters))
            {
                throw new ArgumentException("任务参数为空，需要提供 examId");
            }
            
            var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters);
            if (paramDict == null || !paramDict.ContainsKey("examId"))
            {
                throw new ArgumentException("任务参数缺少 examId");
            }
            
            var examId = Convert.ToInt64(paramDict["examId"]);
            _logger.LogInformation("目标考试ID: {ExamId}", examId);
            
            // 执行预生成
            var result = await _preGenerationService.PreGenerateExamRecordsAsync(examId, cancellationToken);
            
            var message = $"预生成完成 - 成功: {result.SuccessCount}, 失败: {result.FailedCount}, 跳过: {result.SkippedCount}";
            _logger.LogInformation(message);
            
            if (result.FailedCount > 0)
            {
                _logger.LogWarning("部分学生预生成失败，失败的学生ID: {FailedIds}", 
                    string.Join(", ", result.FailedStudentIds));
            }
            
            _logger.LogInformation("========================================");
            
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "考试记录预生成任务执行失败");
            throw;
        }
    }
}

