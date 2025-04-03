using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Controllers;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Graders;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examSettingService">考试设置服务</param>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="studentService">学生服务</param>
    /// <param name="logger">日志记录器</param>
    public ClientService(
        IExamSettingService examSettingService,
        IExamRecordService examRecordService,
        IStudentService studentService,
        ILogger<ClientService> logger)
    {
        _examSettingService = examSettingService;
        _examRecordService = examRecordService;
        _studentService = studentService;
        _logger = logger;
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            return await _examSettingService.GetAvailableExamsForClientAsync(student.Id);
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                return new List<ClientExamHistoryDto>();
            }

            return await _examRecordService.GetExamHistoryForClientAsync(student.Id);
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
        try
        {
            // 获取学生实体
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 创建考试记录
            var examRecord = await _examRecordService.CreateExamRecordAsync(examId, student.Id, userIp, deviceInfo);
            
            // 获取考试详情
            return await _examSettingService.GetExamDetailForClientAsync(examId, examRecord.Id);
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试详情时发生错误");
            throw;
        }
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务提交考试
            return await _examRecordService.SubmitExamForClientAsync(recordId, student.Id, answers);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务获取结果
            return await _examRecordService.GetExamResultForClientAsync(recordId, student.Id);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
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
        try
        {
            // 获取学生实体
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到学生信息");
            }

            // 查找进行中的考试记录
            var records = await _examRecordService.GetPagedListAsync(
                new ExamRecordQueryDto 
                { 
                    Page = 1, 
                    PerPage = 1,
                    ExamSettingId = examId
                },
                r => r.StudentId == student.Id && r.Status == ExamRecordStatus.InProgress,
                "Student");
            
            long? recordId = records.Items.FirstOrDefault()?.Id;

            // 获取考试基本信息
            return await _examSettingService.GetExamBasicInfoForClientAsync(examId, student.Id, recordId);
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试基本信息时发生错误");
            throw;
        }
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务创建记录
            return await _examRecordService.CreateExamRecordAsync(examId, student.Id, userIp, deviceInfo);
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException)
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
            var student = await _studentService.GetByUserIdAsync(userId);
            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 委托给考试记录服务记录切屏
            await _examRecordService.RecordScreenSwitchForClientAsync(recordId, student.Id, userIp);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
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
        try
        {
            // 获取学生信息
            var student = await _studentService.GetByUserIdAsync(userId);
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
                // 假设StudentDto包含了学生组信息
                StudentGroups = student.StudentGroups ?? new List<string>()
            };
        }
        catch (Exception ex) when (ex is not AppServiceException)
        {
            _logger.LogError(ex, "获取考生个人信息时发生错误");
            throw new AppServiceException(500, "获取考生信息失败");
        }
    }
}