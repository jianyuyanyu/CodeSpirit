using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Extensions;
using CodeSpirit.ExamApi.Constants;
using CodeSpirit.Shared.DistributedLock;
using System.Collections.Concurrent;
using System.Net;

namespace CodeSpirit.ExamApi.Services;

/// <summary>
/// 考试客户端服务实现（门面服务）
/// </summary>
public class ClientService : IClientService, IScopedDependency
{
    private readonly IExamSettingService _examSettingService;
    private readonly IExamRecordService _examRecordService;
    private readonly IStudentService _studentService;
    private readonly IExamCacheService _examCacheService;
    private readonly ILogger<ClientService> _logger;
    private readonly IDistributedLockProvider _distributedLockProvider;
    // 移除重复的缓存机制，统一使用 ExamCacheService

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examSettingService">考试设置服务</param>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="studentService">学生服务</param>
    /// <param name="examCacheService">考试缓存服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="distributedLockProvider">分布式锁提供程序</param>
    public ClientService(
        IExamSettingService examSettingService,
        IExamRecordService examRecordService,
        IStudentService studentService,
        IExamCacheService examCacheService,
        ILogger<ClientService> logger,
        IDistributedLockProvider distributedLockProvider)
    {
        _examSettingService = examSettingService;
        _examRecordService = examRecordService;
        _studentService = studentService;
        _examCacheService = examCacheService;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider;
    }

    /// <summary>
    /// 根据用户ID获取学生，使用统一缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>学生信息</returns>
    private async Task<StudentDto?> GetStudentByUserIdAsync(long userId)
    {
        try
        {
            // 使用统一的考试缓存服务获取学生信息
            return await _examCacheService.GetStudentInfoWithCacheAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生信息时出错，用户ID: {UserId}", userId);
            // 缓存失败时直接从学生服务获取
            return await _studentService.GetByUserIdAsync(userId);
        }
    }

    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>可参加的考试列表</returns>
    public async Task<List<ClientExamDto>> GetAvailableExamsAsync(long userId)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new BusinessException("未找到考生信息");
            }
            // ✅ 传递 userId 和 studentId，避免下游再次查询
            var exams = await _examSettingService.GetAvailableExamsForClientAsync(userId, student.Id);

            return exams ?? new List<ClientExamDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可参加的考试列表时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取用户考试历史记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>历史考试记录</returns>
    public async Task<List<ClientExamHistoryDto>> GetExamHistoryAsync(long userId)
    {
        try
        {
            // 使用缓存检查学生信息
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                return new List<ClientExamHistoryDto>();
            }

            // 直接从数据库获取考试历史记录，不缓存
            var result = await _examRecordService.GetExamHistoryForClientAsync(student.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试历史记录时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试详情并创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试详情</returns>
    public async Task<ClientExamDetailDto> GetExamDetailAsync(long examId, long userId, string userIp, string deviceInfo, long examRecordId)
    {
        // 获取学生实体
        var student = await GetStudentByUserIdAsync(userId);
        if (student == null)
        {
            throw new InvalidOperationException("未找到考生信息");
        }
        //var examRecord = await _examRecordService.CreateExamRecordAsync(examId, student.Id, userIp, deviceInfo);
        // 获取考试详情（使用已有的考试记录ID）
        return await _examSettingService.GetExamDetailForClientAsync(examId, examRecordId);
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>带有提交结果的对象，包含是否可以查看结果</returns>
    public async Task<(bool Success, bool EnableViewResult)> SubmitExamAsync(long recordId, long userId, List<ClientExamAnswerDto>? answers = null)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务提交考试（传递最后的答案数据）
            return await _examRecordService.SubmitExamForClientAsync(recordId, student.Id, answers ?? new List<ClientExamAnswerDto>());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "提交考试答案时参数错误");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "提交考试答案时操作无效");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交考试答案时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试结果</returns>
    public async Task<ClientExamResultDto> GetExamResultAsync(long recordId, long userId)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 直接获取考试结果，不缓存
            return await _examRecordService.GetExamResultForClientAsync(recordId, student.Id);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            throw; // 直接抛出业务相关异常
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试结果时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试基本信息
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试基本信息</returns>
    public async Task<ClientExamBasicInfoDto> GetExamBasicInfoAsync(long examId, long userId)
    {
        // 获取学生实体
        var student = await GetStudentByUserIdAsync(userId);
        if (student == null)
        {
            throw new InvalidOperationException("未找到学生信息");
        }

        // 优化查找进行中的考试记录 - 减少查询参数
        var recordQuery = new ExamRecordQueryDto
        {
            Page = 1,
            PerPage = 1,
            ExamSettingId = examId
        };

        // 批量获取考试基本信息和考试记录
        var recordsTask = _examRecordService.GetPagedListAsync(
            recordQuery,
            r => r.StudentId == student.Id && r.Status == ExamRecordStatus.InProgress);

        var records = await recordsTask;
        long? recordId = records.Items.FirstOrDefault()?.Id;

        // 直接获取考试基本信息，不缓存
        return await _examSettingService.GetExamBasicInfoForClientAsync(examId, student.Id, recordId);
    }

    /// <summary>
    /// 获取考试题目数据（带缓存，字典格式）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典</returns>
    public async Task<Dictionary<long, ClientExamQuestionDto>> GetExamQuestionsDataWithCacheAsync(long examId)
    {
        // 委托给专门的缓存服务处理
        var result = await _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);
        return result ?? new Dictionary<long, ClientExamQuestionDto>();
    }

    /// <summary>
    /// 获取考试基本信息（带缓存，不包含用户特定数据）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本信息</returns>
    public async Task<ExamBasicInfoCacheDto> GetExamBasicInfoWithCacheAsync(long examId)
    {
        try
        {
            var result = await _examCacheService.GetExamBasicInfoWithCacheAsync(examId);
            return result ?? throw new InvalidOperationException($"考试不存在或已删除: {examId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试基本信息时发生错误，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户考试记录信息（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户考试记录信息</returns>
    public async Task<UserExamRecordCacheDto> GetUserExamRecordWithCacheAsync(long examId, long userId)
    {
        try
        {
            var result = await _examCacheService.GetUserExamRecordWithCacheAsync(examId, userId);
            return result ?? throw new InvalidOperationException($"用户考试记录不存在: ExamId={examId}, UserId={userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户考试记录时发生错误，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            throw;
        }
    }

    /// <summary>
    /// 获取已提交的答案（已移除缓存，直接查询数据库）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>答案列表</returns>
    public async Task<List<ClientExamAnswerDto>> GetSubmittedAnswersWithCacheAsync(long recordId, long userId)
    {
        // 已移除缓存，直接查询数据库
        return await GetSubmittedAnswersAsync(recordId, userId);
    }

    /// <summary>
    /// 获取考试轻量信息（用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>考试轻量信息</returns>
    public async Task<ClientExamLightInfoDto> GetExamLightInfoAsync(long examId, long userId)
    {
        // ✅ 性能优化：使用缓存获取学生信息
        var student = await GetStudentByUserIdAsync(userId);
        if (student == null)
        {
            throw new InvalidOperationException("未找到学生信息");
        }

        // ✅ 性能优化：使用带缓存的方法获取考试轻量信息
        var lightInfo = await _examCacheService.GetExamLightInfoWithCacheAsync(examId, student.Id);
        if (lightInfo == null)
        {
            throw new InvalidOperationException($"考试不存在或无权访问: {examId}");
        }

        // 设置服务器当前时间（UTC）
        lightInfo.ServerTime = DateTime.UtcNow;

        // 根据时间判断考试状态和是否可以开始
        var now = DateTime.UtcNow;
        if (now < lightInfo.StartTime)
        {
            lightInfo.Status = "pending";
            lightInfo.CanStart = false;
        }
        else if (now >= lightInfo.StartTime && now < lightInfo.EndTime)
        {
            lightInfo.Status = "inProgress";
            lightInfo.CanStart = true;
        }
        else
        {
            lightInfo.Status = "ended";
            lightInfo.CanStart = false;
        }

        return lightInfo;
    }

    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试记录</returns>
    public async Task<ExamRecord> CreateExamRecordAsync(long examId, long userId, string userIp, string deviceInfo)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务创建记录
            return await _examRecordService.CreateExamRecordAsync(examId, student.Id, userIp, deviceInfo);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            throw; // 直接抛出业务相关异常
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建考试记录时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <returns>任务完成状态</returns>
    public async Task RecordScreenSwitchAsync(long recordId, long userId, string userIp)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 直接记录切屏事件，不缓存
            await _examRecordService.RecordScreenSwitchForClientAsync(recordId, student.Id, userIp);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            throw; // 直接抛出业务相关异常
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"记录切屏事件时发生错误（考试记录ID: {recordId}）");
            throw;
        }
    }

    /// <summary>
    /// 获取考生个人信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>考生个人信息</returns>
    public async Task<ClientProfileDto> GetStudentProfileAsync(long userId)
    {
        // 直接调用缓存版本，保持接口兼容性
        return await GetStudentProfileWithCacheAsync(userId);
    }

    /// <summary>
    /// 获取考生个人信息（带二级缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>考生个人信息</returns>
    public async Task<ClientProfileDto> GetStudentProfileWithCacheAsync(long userId)
    {
        // 使用统一的考试缓存服务获取客户端档案信息
        return await _examCacheService.GetClientProfileWithCacheAsync(userId);
    }

    /// <summary>
    /// 获取已提交的答案（加锁保护，确保读写互斥）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>答案列表</returns>
    public async Task<List<ClientExamAnswerDto>> GetSubmittedAnswersAsync(long recordId, long userId)
    {

        try
        {
                // 获取学生实体
                var student = await GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    throw new InvalidOperationException("未找到考生信息");
                }

                // 检查考试记录是否存在且属于该学生
                var record = await _examRecordService.GetAsync(recordId);
                if (record == null || record.StudentId != student.Id)
                {
                    throw new InvalidOperationException("无效的考试记录");
                }

                // 在锁保护下获取已保存的答案，确保不会读到写入中的数据
                var answerEntities = await _examRecordService.GetExamAnswersAsync(recordId);

                // 将答案实体转换为DTO
                var answers = answerEntities.Select(a => new ClientExamAnswerDto
                {
                    QuestionId = a.QuestionId,
                    Answer = a.Answer ?? string.Empty
                }).ToList();

                _logger.LogDebug("读取到 {Count} 个答案，记录ID: {RecordId}", answers.Count, recordId);
                return answers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已提交的答案时发生错误，记录ID: {RecordId}, 用户ID: {UserId}", recordId, userId);
            return new List<ClientExamAnswerDto>(); // 返回空列表而不是抛出异常，避免影响客户端体验
        }
    }

    /// <summary>
    /// 保存考试答案但不提交
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>保存是否成功</returns>
    public async Task<bool> SaveAnswerAsync(long recordId, long userId, List<ClientExamAnswerDto> answers)
    {
        // 使用分布式锁防止并发保存导致数据冲突
        var lockKey = $"exam_answer_save_{recordId}_{userId}";

        try
        {
            //using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10)))
            {
                //_logger.LogDebug("已获取答案保存锁: {LockKey}", lockKey);

                // 获取学生实体
                var student = await GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    throw new InvalidOperationException("未找到考生信息");
                }

                if (answers == null || !answers.Any())
                {
                    _logger.LogWarning("保存答案失败：答案列表为空");
                    return false;
                }

                // 检查考试记录状态（使用缓存优化，避免每次都查询数据库）
                var examRecord = await _examCacheService.GetExamRecordWithCacheAsync(recordId);
                if (examRecord == null)
                {
                    _logger.LogWarning("保存答案失败：考试记录不存在，记录ID: {RecordId}", recordId);
                    return false;
                }

                // 验证记录归属
                if (examRecord.StudentId != student.Id)
                {
                    _logger.LogWarning("保存答案失败：考试记录归属不匹配，学生ID: {StudentId}, 记录学生ID: {RecordStudentId}",
                        student.Id, examRecord.StudentId);
                    return false;
                }

                if (examRecord.Status != ExamRecordStatus.InProgress)
                {
                    _logger.LogWarning("保存答案失败：考试状态不是进行中，当前状态: {Status}", examRecord.Status);
                    return false;
                }

                // 保存到数据库（已移除缓存，刷新时直接查询数据库）
                var result = await _examRecordService.SubmitAnswersAsync(recordId, answers);
                if (!result)
                {
                    _logger.LogWarning("批量保存答案失败，考试记录ID: {RecordId}", recordId);
                    return false;
                }

                _logger.LogDebug("已保存考试 {RecordId} 的 {Count} 个答案到数据库", recordId, answers.Count);
                return true;
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "获取答案保存锁超时: {LockKey}，保存失败", lockKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存答案时发生错误，考试记录ID: {RecordId}", recordId);
            return false;
        }
    }

    /// <summary>
    /// 预热考试缓存（提前加载考试数据到缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">触发预热的用户ID（未使用，仅保持接口兼容性）</param>
    /// <returns>预热是否成功</returns>
    public async Task<bool> WarmUpExamCacheAsync(long examId, long userId)
    {
        try
        {
            _logger.LogInformation("开始预热考试缓存，考试ID: {ExamId}", examId);
            await _examCacheService.WarmupExamCacheAsync(examId);
            _logger.LogInformation("考试缓存预热完成，考试ID: {ExamId}", examId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预热考试缓存时发生错误，考试ID: {ExamId}", examId);
            return false;
        }
    }

    /// <summary>
    /// 清空考试相关的所有缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>清空是否成功</returns>
    public async Task<bool> ClearExamCacheAsync(long examId)
    {
        try
        {
            _logger.LogInformation("开始清空考试缓存，考试ID: {ExamId}", examId);
            await _examCacheService.ClearExamCacheAsync(examId);
            _logger.LogInformation("考试缓存清空完成，考试ID: {ExamId}", examId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空考试缓存时发生错误，考试ID: {ExamId}", examId);
            return false;
        }
    }
}