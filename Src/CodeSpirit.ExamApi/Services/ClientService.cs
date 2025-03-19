using CodeSpirit.ExamApi.Controllers;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ExamApi.Services;

/// <summary>
/// 考试客户端服务实现
/// </summary>
public class ClientService : IClientService
{
    private readonly ExamDbContext _context;
    private readonly ILogger<ClientService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public ClientService(ExamDbContext context, ILogger<ClientService> logger)
    {
        _context = context;
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
            // 查询当前用户所属的学生组
            var studentGroups = await _context.StudentGroupMappings.Include(p => p.Student)
                .Where(m => m.Student.UserId == userId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            // 获取可参加的考试
            var now = DateTime.Now;
            var availableExams = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.StartTime <= now && e.EndTime >= now)
                .Where(e => e.StudentGroups.Any(g => studentGroups.Contains(g.Id)) || !e.StudentGroups.Any())
                .Select(e => new ClientExamDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Duration = e.Duration,
                    TotalScore = e.ExamPaper.TotalScore,
                    Status = e.StartTime <= now && e.EndTime >= now ? "进行中" :
                             (e.StartTime > now ? "未开始" : "已结束"),
                    // 检查是否已参加并获取成绩
                    HasResult = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == r.StudentId &&
                        r.Status == ExamRecordStatus.Graded)
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
            var now = DateTime.Now;
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

            // 检查考试次数
            var attemptCount = await _context.ExamRecords
                .CountAsync(r => r.ExamSettingId == examId && r.StudentId == userId);

            if (attemptCount >= examSetting.AllowedAttempts)
            {
                throw new InvalidOperationException($"已达到最大考试次数限制（{examSetting.AllowedAttempts}次）");
            }

            // 创建考试记录
            var examRecord = new ExamRecord
            {
                ExamSettingId = examId,
                StudentId = userId,
                AttemptNumber = attemptCount + 1,
                StartTime = now,
                Status = ExamRecordStatus.InProgress,
                IpAddress = userIp,
                DeviceInfo = deviceInfo
            };

            _context.ExamRecords.Add(examRecord);
            await _context.SaveChangesAsync();

            // 处理题目乱序
            var questions = examSetting.ExamPaper.ExamPaperQuestions.ToList();
            if (examSetting.EnableRandomQuestionOrder)
            {
                Random rnd = new Random();
                questions = questions.OrderBy(q => rnd.Next()).ToList();
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
                }).ToList()
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
            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Where(r => r.Id == recordId && r.StudentId == userId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new ArgumentException("考试记录不存在", nameof(recordId));
            }

            // 加载试卷题目
            await _context.Entry(examRecord.ExamSetting.ExamPaper)
                .Collection(p => p.ExamPaperQuestions)
                .LoadAsync();

            if (examRecord.Status != ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试已提交，不能重复提交");
            }

            var now = DateTime.Now;
            examRecord.SubmitTime = now;
            examRecord.Status = ExamRecordStatus.Submitted;
            examRecord.Duration = (int)Math.Ceiling((now - examRecord.StartTime).TotalMinutes);

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

            return true;
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
            var examRecord = await _context.ExamRecords
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId && r.StudentId == userId)
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
        var objectiveQuestionTypes = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse };

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

        // 筛选客观题
        var objectiveAnswers = answerRecords
            .Where(a => a.Question != null && objectiveQuestionTypes.Contains(a.Question.Type))
            .ToList();

        double totalScore = 0;

        foreach (var answer in objectiveAnswers)
        {
            if (string.Equals(answer.Answer, answer.QuestionVersion.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
            {
                answer.IsCorrect = true;
                answer.Score = Convert.ToInt32(answer.QuestionVersion.DefaultScore);
                totalScore += answer.Score ?? 0;
            }
            else
            {
                answer.IsCorrect = false;
                answer.Score = 0;
            }
        }

        // 检查是否全部为客观题，如果是则可以完成评分
        var subjectiveAnswersCount = answerRecords
            .Count(a => a.Question != null && !objectiveQuestionTypes.Contains(a.Question.Type));

        if (subjectiveAnswersCount == 0)
        {
            examRecord.Score = totalScore;
            examRecord.Status = ExamRecordStatus.Graded;
            examRecord.IsPassed = totalScore >= examRecord.ExamSetting.ExamPaper.PassScore;
            examRecord.GradedTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }
}