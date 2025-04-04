using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Dtos.Student;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CodeSpirit.ExamApi.Services;

/// <summary>
/// 考试客户端服务实现（门面服务）
/// </summary>
public class ClientService : IClientService
{
    private readonly IExamSettingService _examSettingService;
    private readonly IExamRecordService _examRecordService;
    private readonly IStudentService _studentService;
    private readonly ILogger<ClientService> _logger;
    private readonly IDistributedCache _distributedCache;
    private readonly ConcurrentDictionary<long, StudentDto> _studentCache = new();

    // 进程内考试详情缓存，有效期更短
    private readonly ConcurrentDictionary<long, ClientExamDto> _examDetailsCache = new();

    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examSettingService">考试设置服务</param>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="studentService">学生服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="distributedCache">分布式缓存</param>
    public ClientService(
        IExamSettingService examSettingService,
        IExamRecordService examRecordService,
        IStudentService studentService,
        ILogger<ClientService> logger,
        IDistributedCache distributedCache)
    {
        _examSettingService = examSettingService;
        _examRecordService = examRecordService;
        _studentService = studentService;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 根据用户ID获取学生，支持缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>学生信息</returns>
    private async Task<StudentDto> GetStudentByUserIdAsync(long userId)
    {
        // 先从本地缓存获取
        if (_studentCache.TryGetValue(userId, out var cachedStudent))
        {
            return cachedStudent;
        }

        // 如果本地缓存没有，尝试从分布式缓存获取
        string cacheKey = $"Student_User_{userId}";
        var cachedData = await _distributedCache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            try
            {
                // 从缓存反序列化学生信息
                var studentFromCache = JsonSerializer.Deserialize<StudentDto>(cachedData);
                if (studentFromCache != null)
                {
                    // 更新本地缓存
                    _studentCache[userId] = studentFromCache;
                    return studentFromCache;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从缓存反序列化学生信息时出错");
            }
        }

        // 缓存未命中或反序列化失败，从数据库查询
        var student = await _studentService.GetByUserIdAsync(userId);

        if (student != null)
        {
            try
            {
                // 设置分布式缓存
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(_cacheExpiration)
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2));

                await _distributedCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(student),
                    cacheOptions);

                // 更新本地缓存
                _studentCache[userId] = student;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存学生信息时出错");
            }
        }

        return student;
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
                throw new InvalidOperationException("未找到考生信息");
            }

            _logger.LogDebug("用户 {UserId} 直接从数据库获取可参加考试列表", userId);

            // 直接从数据库获取可参加的考试列表
            var exams = await _examSettingService.GetAvailableExamsForClientAsync(student.Id);

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
    public async Task<ClientExamDetailDto> GetExamDetailAsync(long examId, long userId, string userIp, string deviceInfo)
    {
        // 获取学生实体
        var student = await GetStudentByUserIdAsync(userId);
        if (student == null)
        {
            throw new InvalidOperationException("未找到考生信息");
        }
        var examRecord = await _examRecordService.CreateExamRecordAsync(examId, student.Id, userIp, deviceInfo);
        // 获取考试详情（使用已有的考试记录ID）
        return await _examSettingService.GetExamDetailForClientAsync(examId, examRecord.Id);
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>带有提交结果的对象，包含是否可以查看结果</returns>
    public async Task<(bool Success, bool EnableViewResult)> SubmitExamAsync(long recordId, long userId, List<ClientExamAnswerDto> answers)
    {
        try
        {
            // 获取学生实体
            var student = await GetStudentByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务提交考试
            return await _examRecordService.SubmitExamForClientAsync(recordId, student.Id, answers);
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
        // 获取学生信息
        var student = await GetStudentByUserIdAsync(userId);
        if (student == null)
        {
            throw new AppServiceException(404, "未找到考生信息");
        }

        // 构建客户端个人信息DTO
        return new ClientProfileDto
        {
            Id = student.Id,
            UserId = userId,
            Name = student.Name,
            StudentNumber = student.StudentNumber,
            IdNo = student.IdNo ?? string.Empty,
            Gender = student.Gender.GetDisplayName(),
            AdmissionTicket = student.AdmissionTicket ?? string.Empty,
            PhoneNumber = student.PhoneNumber,
            StudentGroups = student.StudentGroups?.ToList() ?? new List<string>()
        };
    }
}