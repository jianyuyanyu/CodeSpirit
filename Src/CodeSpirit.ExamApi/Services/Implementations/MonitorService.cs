using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Monitor;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 监考服务实现类
/// </summary>
public class MonitorService : IMonitorService
{
    private readonly IRepository<ExamSetting> _examSettingRepository;
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly IRepository<ExamAnswerRecord> _answerRecordRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<ExamPaper> _examPaperRepository;
    private readonly IRepository<ExamPaperQuestion> _examPaperQuestionRepository;
    private readonly ILogger<MonitorService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MonitorService(
        IRepository<ExamSetting> examSettingRepository,
        IRepository<ExamRecord> examRecordRepository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<Student> studentRepository,
        IRepository<ExamPaper> examPaperRepository,
        IRepository<ExamPaperQuestion> examPaperQuestionRepository,
        ILogger<MonitorService> logger)
    {
        _examSettingRepository = examSettingRepository;
        _examRecordRepository = examRecordRepository;
        _answerRecordRepository = answerRecordRepository;
        _studentRepository = studentRepository;
        _examPaperRepository = examPaperRepository;
        _examPaperQuestionRepository = examPaperQuestionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取考试监控信息
    /// </summary>
    public async Task<ExamMonitorDto> GetExamMonitorAsync(long examId)
    {
        // 获取考试设置信息
        var examSetting = await _examSettingRepository.CreateQuery()
            .Include(e => e.ExamPaper)
            .FirstOrDefaultAsync(e => e.Id == examId);
            
        if (examSetting == null)
        {
            throw new ArgumentException("考试不存在", nameof(examId));
        }

        // 获取该考试的所有考试记录
        var examRecords = await _examRecordRepository.CreateQuery()
            .Include(r => r.Student)
            .Where(r => r.ExamSettingId == examId)
            .ToListAsync();

        // 获取考试的所有题目数量
        var totalQuestions = await _examPaperQuestionRepository.CreateQuery()
            .CountAsync(q => q.ExamPaperId == examSetting.ExamPaperId);

        // 获取服务器当前时间（本地时间）
        var serverTime = DateTime.Now;
        
        // 构建考试监控信息
        var monitorDto = new ExamMonitorDto
        {
            Id = examSetting.Id,
            Name = examSetting.Name,
            Description = examSetting.Description ?? string.Empty,
            Duration = examSetting.Duration,
            StartTime = examSetting.StartTime,
            EndTime = examSetting.EndTime,
            TotalScore = examSetting.ExamPaper.TotalScore,
            TotalQuestions = totalQuestions,
            TotalParticipants = examRecords.Count,
            OnlineCount = examRecords.Count(r => r.Status == ExamRecordStatus.InProgress),
            SubmittedCount = examRecords.Count(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded),
            SuspiciousCount = examRecords.Count(r => r.CheatingSuspicionLevel > 50),
            Status = GetExamStatusText(examSetting),
            Students = new List<ExamStudentMonitorDto>(),
            ServerTime = serverTime,
            LastUpdate = serverTime.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // 构建考生信息
        foreach (var record in examRecords)
        {
            // 获取已答题数量
            var answeredCount = await _answerRecordRepository.CreateQuery()
                .CountAsync(a => a.ExamRecordId == record.Id && !string.IsNullOrEmpty(a.Answer));

            // 计算剩余时间
            int? remainingSeconds = null;
            string remainingTimeDisplay = "--";
            
            if (record.Status == ExamRecordStatus.InProgress)
            {
                var endTime = record.StartTime.AddMinutes(examSetting.Duration);
                var now = DateTime.UtcNow;
                if (endTime > now)
                {
                    remainingSeconds = (int)(endTime - now).TotalSeconds;
                    var minutes = Math.Floor((double)remainingSeconds / 60);
                    var seconds = remainingSeconds % 60;
                    remainingTimeDisplay = $"{minutes}分{seconds}秒";
                }
                else
                {
                    remainingSeconds = 0;
                    remainingTimeDisplay = "0分0秒";
                }
            }

            // 构建进度显示
            var progressPercentage = totalQuestions > 0 ? Math.Round((double)answeredCount / totalQuestions * 100, 2) : 0;
            var progressDisplay = $"{answeredCount}/{totalQuestions} ({progressPercentage}%)";

            // 构建考生监控信息
            var studentDto = new ExamStudentMonitorDto
            {
                ExamId = record.ExamSettingId,
                RecordId = record.Id,
                StudentId = record.StudentId,
                Name = record.Student.Name,
                StudentNumber = record.Student.StudentNumber,
                IdCardNumber = record.Student.IdNo ?? "",
                Gender = record.Student.Gender.GetDisplayName(),
                IpAddress = record.IpAddress,
                DeviceInfo = record.DeviceInfo ?? string.Empty,
                StartTime = record.StartTime,
                SubmitTime = record.SubmitTime,
                Status = record.Status,
                StatusText = record.Status.GetDisplayName(),
                ScreenSwitchCount = record.ScreenSwitchCount,
                CheatingSuspicionLevel = record.CheatingSuspicionLevel,
                AnsweredCount = answeredCount,
                TotalQuestions = totalQuestions,
                ProgressPercentage = progressPercentage,
                ProgressDisplay = progressDisplay,
                RemainingSeconds = remainingSeconds,
                RemainingTimeDisplay = remainingTimeDisplay,
                LastActivityTime = record.UpdatedAt,
                IsOnline = record.Status == ExamRecordStatus.InProgress,
                CheatingSuspicionRecord = record.CheatingSuspicionRecord ?? string.Empty
            };

            monitorDto.Students.Add(studentDto);
        }

        return monitorDto;
    }

    /// <summary>
    /// 获取考生监控信息
    /// </summary>
    public async Task<ExamStudentMonitorDto> GetStudentMonitorAsync(long recordId)
    {
        // 获取考试记录
        var record = await _examRecordRepository.CreateQuery()
            .Include(r => r.Student)
            .Include(r => r.ExamSetting)
                .ThenInclude(e => e.ExamPaper)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (record == null)
        {
            throw new ArgumentException("考试记录不存在", nameof(recordId));
        }

        // 获取已答题数量
        var answeredCount = await _answerRecordRepository.CreateQuery()
            .CountAsync(a => a.ExamRecordId == record.Id && !string.IsNullOrEmpty(a.Answer));

        // 获取试卷题目总数
        var totalQuestions = await _examPaperQuestionRepository.CreateQuery()
            .CountAsync(q => q.ExamPaperId == record.ExamSetting.ExamPaperId);

        // 计算剩余时间
        int? remainingSeconds = null;
        string remainingTimeDisplay = "--";
        
        if (record.Status == ExamRecordStatus.InProgress)
        {
            var endTime = record.StartTime.AddMinutes(record.ExamSetting.Duration);
            var now = DateTime.UtcNow;
            if (endTime > now)
            {
                remainingSeconds = (int)(endTime - now).TotalSeconds;
                var minutes = Math.Floor((double)remainingSeconds / 60);
                var seconds = remainingSeconds % 60;
                remainingTimeDisplay = $"{minutes}分{seconds}秒";
            }
            else
            {
                remainingSeconds = 0;
                remainingTimeDisplay = "0分0秒";
            }
        }

        // 计算进度显示
        var progressPercentage = totalQuestions > 0 
            ? Math.Round((double)answeredCount / totalQuestions * 100, 2) 
            : 0;
        var progressDisplay = $"{answeredCount}/{totalQuestions} ({progressPercentage}%)";

        // 构建考生监控信息
        var studentDto = new ExamStudentMonitorDto
        {
            ExamId = record.ExamSettingId,
            RecordId = record.Id,
            StudentId = record.StudentId,
            Name = record.Student.Name,
            StudentNumber = record.Student.StudentNumber,
            IdCardNumber = record.Student.IdNo ?? "",
            Gender = record.Student.Gender.GetDisplayName(),
            IpAddress = record.IpAddress,
            DeviceInfo = record.DeviceInfo ?? string.Empty,
            StartTime = record.StartTime,
            SubmitTime = record.SubmitTime,
            Status = record.Status,
            StatusText = record.Status.GetDisplayName(),
            ScreenSwitchCount = record.ScreenSwitchCount,
            CheatingSuspicionLevel = record.CheatingSuspicionLevel,
            AnsweredCount = answeredCount,
            TotalQuestions = totalQuestions,
            ProgressPercentage = progressPercentage,
            ProgressDisplay = progressDisplay,
            RemainingSeconds = remainingSeconds,
            RemainingTimeDisplay = remainingTimeDisplay,
            LastActivityTime = record.UpdatedAt,
            IsOnline = record.Status == ExamRecordStatus.InProgress,
            CheatingSuspicionRecord = record.CheatingSuspicionRecord ?? string.Empty
        };

        return studentDto;
    }

    /// <summary>
    /// 强制结束考生考试
    /// </summary>
    public async Task TerminateStudentExamAsync(long recordId)
    {
        // 获取考试记录
        var record = await _examRecordRepository.GetByIdAsync(recordId);
        if (record == null)
        {
            throw new ArgumentException("考试记录不存在", nameof(recordId));
        }

        // 检查考试状态
        if (record.Status != ExamRecordStatus.InProgress)
        {
            throw new InvalidOperationException("考试已结束，无法强制交卷");
        }

        // 更新考试记录状态
        record.Status = ExamRecordStatus.Submitted;
        record.SubmitTime = DateTime.UtcNow;
        
        // 记录强制交卷日志
        var cheatingSuspicionRecord = string.IsNullOrEmpty(record.CheatingSuspicionRecord) 
            ? "[]" 
            : record.CheatingSuspicionRecord;
        
        var cheatRecord = new
        {
            time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            type = "forced_submit",
            description = "监考人员强制交卷"
        };
        
        // 更新作弊记录
        if (cheatingSuspicionRecord == "[]")
        {
            record.CheatingSuspicionRecord = $"[{JsonConvert.SerializeObject(cheatRecord)}]";
        }
        else
        {
            record.CheatingSuspicionRecord = cheatingSuspicionRecord.TrimEnd(']') + "," 
                + JsonConvert.SerializeObject(cheatRecord) + "]";
        }

        // 保存更改
        await _examRecordRepository.UpdateAsync(record);
        
        _logger.LogInformation("强制结束考生考试成功，考试记录ID: {RecordId}", recordId);
    }

    /// <summary>
    /// 标记考生作弊
    /// </summary>
    public async Task FlagStudentCheatingAsync(long recordId, string reason)
    {
        // 获取考试记录
        var record = await _examRecordRepository.GetByIdAsync(recordId);
        if (record == null)
        {
            throw new ArgumentException("考试记录不存在", nameof(recordId));
        }

        // 设置作弊嫌疑等级
        record.CheatingSuspicionLevel = 100; // 标记为最高作弊嫌疑
        
        // 记录作弊日志
        var cheatingSuspicionRecord = string.IsNullOrEmpty(record.CheatingSuspicionRecord) 
            ? "[]" 
            : record.CheatingSuspicionRecord;
        
        var cheatRecord = new
        {
            time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            type = "manual_flag",
            description = string.IsNullOrEmpty(reason) ? "监考人员标记作弊" : reason
        };
        
        // 更新作弊记录
        if (cheatingSuspicionRecord == "[]")
        {
            record.CheatingSuspicionRecord = $"[{JsonConvert.SerializeObject(cheatRecord)}]";
        }
        else
        {
            record.CheatingSuspicionRecord = cheatingSuspicionRecord.TrimEnd(']') + "," 
                + JsonConvert.SerializeObject(cheatRecord) + "]";
        }

        // 保存更改
        await _examRecordRepository.UpdateAsync(record);
        
        _logger.LogInformation("标记考生作弊成功，考试记录ID: {RecordId}, 原因: {Reason}", recordId, reason);
    }
    
    /// <summary>
    /// 获取考试状态文本
    /// </summary>
    private string GetExamStatusText(ExamSetting examSetting)
    {
        var now = DateTime.UtcNow;
        
        if (now < examSetting.StartTime)
        {
            return "未开始";
        }
        else if (now <= examSetting.EndTime)
        {
            return "进行中";
        }
        else
        {
            return "已结束";
        }
    }
} 