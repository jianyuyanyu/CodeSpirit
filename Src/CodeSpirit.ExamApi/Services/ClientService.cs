using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Controllers;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.ExamApi.Services.Graders;

namespace CodeSpirit.ExamApi.Services;

/// <summary>
/// 考试客户端服务实现
/// </summary>
public class ClientService : IClientService
{
    private readonly ExamDbContext _context;
    private readonly ILogger<ClientService> _logger;
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public ClientService(ExamDbContext context, ILogger<ClientService> logger, IIdGenerator idGenerator)
    {
        _context = context;
        _logger = logger;
        _idGenerator = idGenerator;
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
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 查询当前用户所属的学生组
            var studentGroups = await _context.StudentGroupMappings.Include(p => p.Student)
                .Where(m => m.Student.UserId == userId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            // 获取可参加的考试
            var now = DateTime.UtcNow;
            var availableExams = await _context.ExamSettings
                .Include(e => e.StudentGroups)
                .Include(e => e.ExamPaper)
                .Where(e => e.StartTime <= now && e.EndTime >= now)
                .Where(e => e.StudentGroups.Any() == false || e.StudentGroups.Any(g => studentGroups.Contains(g.StudentGroupId)))
                .Select(e => new ClientExamDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Duration = e.Duration,
                    TotalScore = e.ExamPaper.TotalScore,
                    Status = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == student.Id &&
                        r.Status == ExamRecordStatus.InProgress)
                        ? "进行中"
                        : (_context.ExamRecords.Any(r =>
                            r.ExamSettingId == e.Id &&
                            r.StudentId == student.Id &&
                            (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                            ? "已完成"
                            : (e.StartTime <= now && e.EndTime >= now ? "进行中" :
                               (e.StartTime > now ? "未开始" : "已结束"))),
                    // 检查是否已参加并获取成绩
                    HasResult = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == student.Id &&
                        (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                })
                .ToListAsync();

            return availableExams;
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
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
                return new List<ClientExamHistoryDto>();

            var examHistory = await _context.ExamRecords
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Where(r => r.StudentId == student.Id)
                .Where(r => r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted)
                .OrderByDescending(r => r.StartTime)
                .Select(r => new ClientExamHistoryDto
                {
                    Id = r.Id,
                    ExamId = r.ExamSettingId,
                    Name = r.ExamSetting.Name,
                    StartTime = r.StartTime,
                    SubmitTime = r.SubmitTime,
                    Duration = r.Duration ?? r.ExamSetting.Duration,
                    Score = r.Score,
                    TotalScore = r.ExamSetting.ExamPaper.TotalScore,
                    IsPassed = r.IsPassed,
                    Status = r.Status.ToString()
                })
                .ToListAsync();

            return examHistory;
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
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 加载试卷题目
            await _context.Entry(examSetting.ExamPaper)
                .Collection(p => p.ExamPaperQuestions)
                .LoadAsync();

            // 预先加载ExamPaperQuestions的关联对象
            foreach (var question in examSetting.ExamPaper.ExamPaperQuestions)
            {
                await _context.Entry(question)
                    .Reference(q => q.Question)
                    .LoadAsync();

                await _context.Entry(question)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
            }

            // 检查考试时间
            var now = DateTime.UtcNow;
            if (examSetting.StartTime > now || examSetting.EndTime < now)
            {
                throw new InvalidOperationException("不在考试时间范围内");
            }

            // 检查是否有权限参加考试
            var studentGroups = await _context.StudentGroupMappings
                .Include(m => m.Student)
                .Where(m => m.Student.UserId == userId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            var hasPermission = !examSetting.StudentGroups.Any() ||
                                examSetting.StudentGroups.Any(g => studentGroups.Contains(g.Id));

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 获取学生实体
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 检查考试次数
            var attemptCount = await _context.ExamRecords
                .CountAsync(r => r.ExamSettingId == examId && r.StudentId == student.Id);

            //if (attemptCount >= examSetting.AllowedAttempts)
            //{
            //    throw new InvalidOperationException($"已达到最大考试次数限制（{examSetting.AllowedAttempts}次）");
            //}

            // 查找进行中的考试记录
            var examRecord = await _context.ExamRecords
                .Where(r => r.ExamSettingId == examId && r.StudentId == student.Id && r.Status == ExamRecordStatus.InProgress)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync();
            if (examRecord == null)
            {
                throw new InvalidOperationException("考试记录不存在！");
            }

            // 处理题目乱序
            var questions = examSetting.ExamPaper.ExamPaperQuestions.ToList();
            if (examSetting.EnableRandomQuestionOrder)
            {
                Random rnd = new Random();
                questions = questions.OrderBy(q => rnd.Next()).ToList();
            }

            // 处理选项乱序
            if (examSetting.EnableRandomOptionOrder)
            {
                var randomGenerator = new Random();
                foreach (var question in questions)
                {
                    // 只对单选题和多选题进行选项乱序处理
                    if (question.Question.Type == QuestionType.SingleChoice ||
                        question.Question.Type == QuestionType.MultipleChoice)
                    {
                        var options = question.QuestionVersion.Options.OrderBy(o => randomGenerator.Next()).ToList();
                        question.QuestionVersion.Options = options;
                    }
                }
            }

            // 组装考试详情
            var examDetail = new ClientExamDetailDto
            {
                Id = examSetting.Id,
                RecordId = examRecord.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examRecord.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                AttemptNumber = examRecord.AttemptNumber,
                AllowedAttempts = examSetting.AllowedAttempts,
                Questions = questions.Select(q => new ClientExamQuestionDto
                {
                    Id = q.Id,
                    QuestionId = q.QuestionId,
                    QuestionVersionId = q.QuestionVersionId,
                    Content = q.QuestionVersion.Content,
                    Type = q.Question.Type.ToString(),
                    Options = string.Join(",", q.QuestionVersion.Options),
                    Score = q.Score,
                    SequenceNumber = q.OrderNumber,
                    IsRequired = q.IsRequired
                })
    .OrderBy(q => q.Type)
    .ToList()
            };

            return examDetail;
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
    /// <returns>是否提交成功</returns>
    public async Task<bool> SubmitExamAsync(long recordId, long userId, List<ClientExamAnswerDto> answers)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 获取学生实体
                var student = await _context.Students
                    .Where(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    throw new InvalidOperationException("未找到考生信息");
                }

                var examRecord = await _context.ExamRecords
                    .Include(r => r.ExamSetting)
                    .ThenInclude(s => s.ExamPaper)
                    .Where(r => r.Id == recordId && r.StudentId == student.Id)
                    .FirstOrDefaultAsync();

                if (examRecord == null)
                {
                    throw new AppServiceException(400, "考试记录不存在");
                }

                // 加载试卷题目
                await _context.Entry(examRecord.ExamSetting.ExamPaper)
                    .Collection(p => p.ExamPaperQuestions)
                    .LoadAsync();

                if (examRecord.Status != ExamRecordStatus.InProgress)
                {
                    throw new InvalidOperationException("考试已提交，不能重复提交");
                }

                var now = DateTime.UtcNow;
                examRecord.SubmitTime = now;
                examRecord.Status = ExamRecordStatus.Submitted;
                examRecord.Duration = (int)Math.Ceiling((now - examRecord.CreatedAt).TotalMinutes);

                // 添加答案记录
                foreach (var answer in answers)
                {
                    // 查找题目版本
                    var examPaperQuestion = examRecord.ExamSetting.ExamPaper.ExamPaperQuestions
                        .FirstOrDefault(q => q.Id == answer.QuestionId);

                    if (examPaperQuestion == null)
                    {
                        continue;
                    }

                    var answerRecord = new ExamAnswerRecord
                    {
                        Id = _idGenerator.NewId(),
                        ExamRecordId = recordId,
                        QuestionId = examPaperQuestion.QuestionId,
                        QuestionVersionId = examPaperQuestion.QuestionVersionId,
                        OrderNumber = examPaperQuestion.OrderNumber,
                        Answer = answer.Answer,
                        IsCorrect = false // 先默认为错误，后续评分时更新
                    };

                    _context.ExamAnswerRecords.Add(answerRecord);
                }

                await _context.SaveChangesAsync();

                // 如果是客观题，可以自动评分
                await AutoGradeObjectiveQuestions(examRecord);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId && r.StudentId == student.Id)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new ArgumentException("考试记录不存在", nameof(recordId));
            }

            if (examRecord.Status == ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试尚未提交，无法查看结果");
            }

            // 加载答案记录的题目关系
            foreach (var answer in examRecord.AnswerRecords)
            {
                await _context.Entry(answer)
                    .Reference(a => a.Question)
                    .LoadAsync();

                await _context.Entry(answer)
                    .Reference(a => a.QuestionVersion)
                    .LoadAsync();
            }

            var result = new ClientExamResultDto
            {
                Id = examRecord.Id,
                ExamId = examRecord.ExamSettingId,
                Name = examRecord.ExamSetting.Name,
                StartTime = examRecord.StartTime,
                SubmitTime = examRecord.SubmitTime,
                Duration = examRecord.Duration ?? 0,
                Score = examRecord.Score,
                TotalScore = examRecord.ExamSetting.ExamPaper.TotalScore,
                IsPassed = examRecord.IsPassed,
                Status = examRecord.Status.ToString(),
                Comments = examRecord.Comments,
                Answers = examRecord.AnswerRecords.Select(a => new ClientExamAnswerResultDto
                {
                    QuestionId = a.QuestionId,
                    Content = a.QuestionVersion.Content,
                    Type = a.Question.Type.ToString(),
                    Score = Convert.ToInt32(a.QuestionVersion.DefaultScore),
                    UserAnswer = a.Answer,
                    CorrectAnswer = a.QuestionVersion.CorrectAnswer,
                    IsCorrect = a.IsCorrect ?? false,
                    ObtainedScore = a.Score ?? 0
                }).ToList()
            };

            return result;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "获取考试结果时发生错误");
            throw;
        }
    }

    // 客观题自动评分
    private async Task AutoGradeObjectiveQuestions(ExamRecord examRecord)
    {
        // 加载所有答案记录
        var answerRecords = await _context.ExamAnswerRecords
            .Where(a => a.ExamRecordId == examRecord.Id)
            .ToListAsync();

        // 加载所有答案关联的题目和题目版本
        foreach (var answer in answerRecords)
        {
            await _context.Entry(answer)
                .Reference(a => a.Question)
                .LoadAsync();

            await _context.Entry(answer)
                .Reference(a => a.QuestionVersion)
                .LoadAsync();
        }

        // 使用评分器进行评分
        var grader = new ObjectiveQuestionGrader();
        var result = grader.Grade(answerRecords, examRecord.ExamSetting.ExamPaper.PassScore);

        // 如果全部为客观题，更新考试记录状态
        if (result.IsAllObjective)
        {
            examRecord.Score = result.TotalScore;
            examRecord.Status = ExamRecordStatus.Graded;
            examRecord.IsPassed = result.TotalScore >= examRecord.ExamSetting.ExamPaper.PassScore;
            examRecord.GradedTime = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
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
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 检查考试时间
            var now = DateTime.UtcNow;
            if (examSetting.StartTime > now || examSetting.EndTime < now)
            {
                throw new InvalidOperationException("不在考试时间范围内");
            }

            // 检查是否有权限参加考试
            var studentGroups = await _context.StudentGroupMappings
                .Include(m => m.Student)
                .Where(m => m.Student.UserId == userId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            var hasPermission = !examSetting.StudentGroups.Any() ||
                                examSetting.StudentGroups.Any(g => studentGroups.Contains(g.Id));

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 获取学生实体
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到学生信息");
            }

            // 查找进行中的考试记录
            var existingRecord = await _context.ExamRecords
                .Where(r => r.ExamSettingId == examId && r.StudentId == student.Id && r.Status == ExamRecordStatus.InProgress)
                .OrderByDescending(r => r.StartTime)
                .FirstOrDefaultAsync();

            // 组装考试基本信息
            var examBasicInfo = new ClientExamBasicInfoDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = existingRecord?.StartTime ?? now,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                RecordId = existingRecord?.Id,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                ScreenSwitchCount = existingRecord?.ScreenSwitchCount ?? 0
            };

            return examBasicInfo;
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
    /// <returns>考试记录ID</returns>
    public async Task<ExamRecord> CreateExamRecordAsync(long examId, long userId, string userIp, string deviceInfo)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 检查考试时间
            var now = DateTime.UtcNow;
            if (examSetting.StartTime > now || examSetting.EndTime < now)
            {
                throw new InvalidOperationException("不在考试时间范围内");
            }

            // 获取学生实体
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 检查考试次数
            var attemptCount = await _context.ExamRecords
                .CountAsync(r => r.ExamSettingId == examId && r.StudentId == student.Id);

            // 创建考试记录
            var examRecord = new ExamRecord
            {
                Id = _idGenerator.NewId(),
                ExamSettingId = examId,
                StudentId = student.Id,
                AttemptNumber = attemptCount + 1,
                StartTime = now,
                Status = ExamRecordStatus.InProgress,
                IpAddress = userIp,
                DeviceInfo = deviceInfo
            };

            _context.ExamRecords.Add(examRecord);
            await _context.SaveChangesAsync();

            return examRecord;
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
    /// <returns>是否成功</returns>
    public async Task<bool> RecordScreenSwitchAsync(long recordId, long userId, string userIp)
    {
        try
        {
            // 获取学生实体
            var student = await _context.Students
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            // 获取考试记录
            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamSetting)
                .Where(r => r.Id == recordId && r.StudentId == student.Id)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new ArgumentException("考试记录不存在", nameof(recordId));
            }

            // 检查考试状态
            if (examRecord.Status != ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试已结束，无法记录切屏");
            }

            // 更新IP地址（如果提供了新的IP且不同于原IP）
            if (!string.IsNullOrEmpty(userIp) && examRecord.IpAddress != userIp)
            {
                examRecord.IpAddress = userIp;

                // 如果IP变更，可能是作弊行为，记录
                var cheatingSuspicionRecord = string.IsNullOrEmpty(examRecord.CheatingSuspicionRecord)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(examRecord.CheatingSuspicionRecord);

                if (cheatingSuspicionRecord == null)
                {
                    cheatingSuspicionRecord = new List<string>();
                }

                //这里记录当前时间及IP变更信息
                cheatingSuspicionRecord.Add($"IP变更（{DateTime.Now:yyyy-MM-dd HH:mm:ss}）：从 {examRecord.IpAddress} 变更为 {userIp}");

                examRecord.CheatingSuspicionRecord = System.Text.Json.JsonSerializer.Serialize(cheatingSuspicionRecord);

                // 增加作弊嫌疑等级
                examRecord.CheatingSuspicionLevel = Math.Min(100, examRecord.CheatingSuspicionLevel + 20);
            }

            // 增加切屏次数
            examRecord.ScreenSwitchCount += 1;

            // 更新作弊嫌疑等级
            int maxAllowedSwitches = examRecord.ExamSetting.AllowedScreenSwitchCount;
            if (maxAllowedSwitches > 0 && examRecord.ScreenSwitchCount > maxAllowedSwitches)
            {
                // 超过允许的切屏次数，提高作弊嫌疑等级
                int exceedCount = examRecord.ScreenSwitchCount - maxAllowedSwitches;
                int suspicionIncrease = 10 * exceedCount; // 每超过一次增加10点嫌疑

                examRecord.CheatingSuspicionLevel += suspicionIncrease;
                if (examRecord.CheatingSuspicionLevel > 100)
                {
                    examRecord.CheatingSuspicionLevel = 100; // 最大不超过100
                }

                // 记录作弊嫌疑记录
                var cheatingSuspicionRecord = string.IsNullOrEmpty(examRecord.CheatingSuspicionRecord)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(examRecord.CheatingSuspicionRecord);

                if (cheatingSuspicionRecord == null)
                {
                    cheatingSuspicionRecord = new List<string>();
                }

                //这里记录当前时间及切屏超限信息
                cheatingSuspicionRecord.Add($"切屏超限（{DateTime.Now:yyyy-MM-dd HH:mm:ss}）：累计切屏 {examRecord.ScreenSwitchCount} 次，超过限制 {exceedCount} 次");

                examRecord.CheatingSuspicionRecord = System.Text.Json.JsonSerializer.Serialize(cheatingSuspicionRecord);
            }

            // 保存更改
            await _context.SaveChangesAsync();

            _logger.LogInformation($"考试ID {recordId} 切屏记录更新，当前切屏次数: {examRecord.ScreenSwitchCount}");

            return true;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, $"记录切屏事件时发生错误（考试记录ID: {recordId}）");
            throw;
        }
    }
}