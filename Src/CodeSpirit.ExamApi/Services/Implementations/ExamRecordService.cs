using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试记录服务实现类
/// </summary>
public class ExamRecordService : BaseCRUDService<ExamRecord, ExamRecordDto, long, StartExamDto, object>, IExamRecordService
{
    private readonly IRepository<ExamAnswerRecord> _answerRecordRepository;
    private readonly IRepository<ExamSetting> _examSettingRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<QuestionVersion> _questionVersionRepository;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamRecordService(
        IRepository<ExamRecord> repository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<ExamSetting> examSettingRepository,
        IRepository<Student> studentRepository,
        IRepository<QuestionVersion> questionVersionRepository,
        IMapper mapper) : base(repository, mapper)
    {
        _answerRecordRepository = answerRecordRepository;
        _examSettingRepository = examSettingRepository;
        _studentRepository = studentRepository;
        _questionVersionRepository = questionVersionRepository;
    }
    
    /// <summary>
    /// 开始考试
    /// </summary>
    public async Task<ExamRecordDto> StartExamAsync(StartExamDto startExamDto)
    {
        // 验证考试设置是否存在
        var examSetting = await _examSettingRepository.GetByIdAsync(startExamDto.ExamSettingId);
        if (examSetting == null)
        {
            throw new BusinessException("考试设置不存在");
        }
        
        // 验证考生是否存在
        if (startExamDto.StudentId.HasValue)
        {
            var student = await _studentRepository.GetByIdAsync(startExamDto.StudentId.Value);
            if (student == null)
            {
                throw new BusinessException("考生不存在");
            }
        }
        
        // 检查考试是否在有效时间内
        var now = DateTime.Now;
        if (now < examSetting.StartTime || now > examSetting.EndTime)
        {
            throw new BusinessException("不在考试时间范围内");
        }
        
        // 检查考试尝试次数
        int attemptNumber = 1;
        if (startExamDto.StudentId.HasValue)
        {
            var attemptCount = await Repository.CreateQuery()
                .CountAsync(r => r.StudentId == startExamDto.StudentId.Value && 
                                 r.ExamSettingId == startExamDto.ExamSettingId);
                                 
            attemptNumber = attemptCount + 1;
            
            if (attemptNumber > examSetting.AllowedAttempts)
            {
                throw new BusinessException("已超过允许的考试次数");
            }
        }
        
        // 创建考试记录
        var examRecord = Mapper.Map<ExamRecord>(startExamDto);
        examRecord.AttemptNumber = attemptNumber;
        examRecord.StartTime = DateTime.Now;
        
        // 保存考试记录
        await Repository.AddAsync(examRecord);
        
        // 获取试卷题目并创建答题记录
        var examPaperId = examSetting.ExamPaperId;
        var examPaper = await _examSettingRepository.CreateQuery()
            .Include(es => es.ExamPaper)
            .ThenInclude(ep => ep.ExamPaperQuestions)
            .ThenInclude(epq => epq.QuestionVersion)
            .FirstOrDefaultAsync(es => es.Id == startExamDto.ExamSettingId);
            
        if (examPaper == null || examPaper.ExamPaper.ExamPaperQuestions == null)
        {
            throw new BusinessException("试卷题目不存在");
        }
        
        var questions = examPaper.ExamPaper.ExamPaperQuestions.ToList();
        
        // 题目乱序处理
        if (examSetting.EnableRandomQuestionOrder)
        {
            var random = new Random();
            questions = questions.OrderBy(q => random.Next()).ToList();
        }
        
        // 创建答题记录
        var answerRecords = new List<ExamAnswerRecord>();
        for (int i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            answerRecords.Add(new ExamAnswerRecord
            {
                ExamRecordId = examRecord.Id,
                QuestionId = question.QuestionId,
                QuestionVersionId = question.QuestionVersionId,
                OrderNumber = i + 1,
                IsMarked = false
            });
        }
        
        await _answerRecordRepository.AddRangeAsync(answerRecords);
        
        return Mapper.Map<ExamRecordDto>(examRecord);
    }
    
    /// <summary>
    /// 提交答案
    /// </summary>
    public async Task<bool> SubmitAnswerAsync(SubmitAnswerDto submitAnswerDto)
    {
        // 验证考试记录是否存在
        var examRecord = await Repository.GetByIdAsync(submitAnswerDto.ExamRecordId);
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }
        
        // 检查考试状态
        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束，无法提交答案");
        }
        
        // 获取对应的答题记录
        var answerRecord = await _answerRecordRepository.CreateQuery()
            .FirstOrDefaultAsync(a => a.ExamRecordId == submitAnswerDto.ExamRecordId && 
                                     a.QuestionId == submitAnswerDto.QuestionId &&
                                     a.QuestionVersionId == submitAnswerDto.QuestionVersionId);
                                     
        if (answerRecord == null)
        {
            throw new BusinessException("答题记录不存在");
        }
        
        // 更新答题记录
        answerRecord.Answer = submitAnswerDto.Answer;
        answerRecord.IsMarked = submitAnswerDto.IsMarked;
        
        if (submitAnswerDto.StartTime.HasValue && !answerRecord.StartTime.HasValue)
        {
            answerRecord.StartTime = submitAnswerDto.StartTime;
        }
        
        answerRecord.SubmitTime = submitAnswerDto.SubmitTime ?? DateTime.Now;
        
        if (answerRecord.StartTime.HasValue && answerRecord.SubmitTime.HasValue)
        {
            answerRecord.Duration = (int)(answerRecord.SubmitTime.Value - answerRecord.StartTime.Value).TotalSeconds;
        }
        
        await _answerRecordRepository.UpdateAsync(answerRecord);
        
        return true;
    }
    
    /// <summary>
    /// 完成考试
    /// </summary>
    public async Task<ExamRecordDto> FinishExamAsync(FinishExamDto finishExamDto)
    {
        // 验证考试记录是否存在
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.AnswerRecords)
            .Include(r => r.ExamSetting)
            .ThenInclude(es => es.ExamPaper)
            .FirstOrDefaultAsync(r => r.Id == finishExamDto.ExamRecordId);
            
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }
        
        // 检查考试状态
        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束");
        }
        
        // 检查是否所有题目都已作答
        var unansweredQuestions = examRecord.AnswerRecords.Count(a => string.IsNullOrEmpty(a.Answer));
        if (unansweredQuestions > 0 && !finishExamDto.ForceSubmit)
        {
            throw new BusinessException($"还有{unansweredQuestions}道题目未作答，是否确认提交？");
        }
        
        // 更新考试记录状态
        examRecord.Status = ExamRecordStatus.Submitted;
        examRecord.SubmitTime = finishExamDto.SubmitTime ?? DateTime.Now;
        
        if (examRecord.StartTime != null && examRecord.SubmitTime != null)
        {
            examRecord.Duration = (int)(examRecord.SubmitTime.Value - examRecord.StartTime).TotalMinutes;
        }
        
        // 自动评分（客观题）
        double totalScore = 0;
        foreach (var answer in examRecord.AnswerRecords)
        {
            var questionVersion = await _questionVersionRepository.CreateQuery()
                .Include(qv => qv.Question)
                .FirstOrDefaultAsync(qv => qv.Id == answer.QuestionVersionId);
                
            if (questionVersion != null && !string.IsNullOrEmpty(answer.Answer))
            {
                // 判断题和单选题可以自动评分
                if (questionVersion.Question.Type == QuestionType.SingleChoice || 
                    questionVersion.Question.Type == QuestionType.TrueFalse)
                {
                    bool isCorrect = answer.Answer.Trim() == questionVersion.CorrectAnswer.Trim();
                    answer.IsCorrect = isCorrect;
                    answer.Score = isCorrect ? questionVersion.DefaultScore : 0;
                    totalScore += answer.Score ?? 0;
                }
                // 多选题也可以自动评分，但需要特殊处理
                else if (questionVersion.Question.Type == QuestionType.MultipleChoice)
                {
                    var studentAnswers = answer.Answer.Split(',').Select(a => a.Trim()).OrderBy(a => a).ToArray();
                    var correctAnswers = questionVersion.CorrectAnswer.Split(',').Select(a => a.Trim()).OrderBy(a => a).ToArray();
                    
                    bool isCorrect = studentAnswers.SequenceEqual(correctAnswers);
                    answer.IsCorrect = isCorrect;
                    answer.Score = isCorrect ? questionVersion.DefaultScore : 0;
                    totalScore += answer.Score ?? 0;
                }
                // 其他题型需要人工评分
            }
        }
        
        // 更新总分和是否通过
        examRecord.Score = totalScore;
        examRecord.IsPassed = totalScore >= examRecord.ExamSetting.ExamPaper.PassScore;
        
        // 保存更改
        await _answerRecordRepository.UpdateRangeAsync(examRecord.AnswerRecords);
        await Repository.UpdateAsync(examRecord);
        
        return Mapper.Map<ExamRecordDto>(examRecord);
    }
    
    /// <summary>
    /// 获取考试统计
    /// </summary>
    public async Task<ExamStatisticsDto> GetExamStatisticsAsync(long examSettingId)
    {
        var examSetting = await _examSettingRepository.GetByIdAsync(examSettingId);
        if (examSetting == null)
        {
            throw new BusinessException("考试设置不存在");
        }
        
        // 查询考试记录数据
        var records = await Repository.CreateQuery()
            .Where(r => r.ExamSettingId == examSettingId)
            .ToListAsync();
            
        if (records.Count == 0)
        {
            return new ExamStatisticsDto
            {
                ExamSettingId = examSettingId,
                ExamName = examSetting.Name
            };
        }
        
        // 计算统计数据
        int totalParticipants = records.Select(r => r.StudentId).Distinct().Count();
        int completedCount = records.Count(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded);
        int passedCount = records.Count(r => r.IsPassed);
        decimal passRate = completedCount > 0 ? (decimal)passedCount / completedCount * 100 : 0;
        
        var scoredRecords = records.Where(r => r.Score.HasValue).ToList();
        decimal averageScore = scoredRecords.Count > 0 ? (decimal)scoredRecords.Average(r => r.Score.Value) : 0;
        double highestScore = scoredRecords.Count > 0 ? scoredRecords.Max(r => r.Score.Value) : 0;
        double lowestScore = scoredRecords.Count > 0 ? scoredRecords.Min(r => r.Score.Value) : 0;
        
        var completedRecords = records.Where(r => r.Duration.HasValue).ToList();
        double averageTime = completedRecords.Count > 0 ? completedRecords.Average(r => r.Duration.Value) : 0;
        
        int cheatingSuspicionCount = records.Count(r => r.CheatingSuspicionLevel >= 50);
        
        return new ExamStatisticsDto
        {
            ExamSettingId = examSettingId,
            ExamName = examSetting.Name,
            TotalParticipants = totalParticipants,
            CompletedCount = completedCount,
            PassedCount = passedCount,
            PassRate = passRate,
            AverageScore = averageScore,
            HighestScore = highestScore,
            LowestScore = lowestScore,
            AverageCompletionTime = averageTime,
            CheatingSuspicionCount = cheatingSuspicionCount
        };
    }
    
    /// <summary>
    /// 获取错题列表
    /// </summary>
    public async Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto)
    {
        // 构建查询条件
        Expression<Func<ExamAnswerRecord, bool>> predicate = a =>
            a.IsCorrect == false &&
            (!queryDto.StudentId.HasValue || a.ExamRecord.StudentId == queryDto.StudentId.Value) &&
            (!queryDto.ExamSettingId.HasValue || a.ExamRecord.ExamSettingId == queryDto.ExamSettingId.Value) &&
            (!queryDto.QuestionId.HasValue || a.QuestionId == queryDto.QuestionId.Value);
            
        // 查询错题记录
        var query = _answerRecordRepository.CreateQuery()
            .Where(predicate);

        // 按时间范围筛选
        if (queryDto.ExamTimeRange != null && queryDto.ExamTimeRange.Length == 2)
        {
            query = query.Where(a => a.ExamRecord.StartTime >= queryDto.ExamTimeRange[0] && 
                                  a.ExamRecord.StartTime <= queryDto.ExamTimeRange[1]);
        }
        
        // 按题目类型筛选
        if (!string.IsNullOrEmpty(queryDto.QuestionType))
        {
            if (Enum.TryParse<QuestionType>(queryDto.QuestionType, out var questionType))
            {
                query = query.Where(a => a.QuestionVersion.Question.Type == questionType);
            }
        }
        
        // 包含关联数据（需要在筛选之后加载）
        query = query.Include(a => a.ExamRecord)
                     .Include(a => a.QuestionVersion)
                     .Include(a => a.QuestionVersion.Question);
        
        // 排序和分页
        string orderBy = queryDto.OrderBy ?? "ExamRecord.StartTime";
        string orderDir = queryDto.OrderDir ?? "desc";
        
        int totalCount = await query.CountAsync();
        
        var pagedItems = await query
            .OrderByDynamic(orderBy, orderDir == "desc")
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();
            
        // 映射为DTO
        var wrongQuestionDtos = Mapper.Map<List<WrongQuestionDto>>(pagedItems);
        
        return new PageList<WrongQuestionDto>(wrongQuestionDtos, totalCount);
    }
    
    /// <summary>
    /// 记录切屏事件
    /// </summary>
    public async Task<bool> RecordScreenSwitchAsync(long recordId)
    {
        var examRecord = await Repository.GetByIdAsync(recordId);
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }
        
        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束，无法记录切屏");
        }
        
        // 增加切屏记录
        examRecord.ScreenSwitchCount++;
        
        // 根据切屏次数判断作弊嫌疑
        var examSetting = await _examSettingRepository.GetByIdAsync(examRecord.ExamSettingId);
        if (examSetting != null)
        {
            if (examRecord.ScreenSwitchCount > examSetting.AllowedScreenSwitchCount)
            {
                // 计算作弊嫌疑等级
                int suspicionLevel = Math.Min(100, 50 + (examRecord.ScreenSwitchCount - examSetting.AllowedScreenSwitchCount) * 10);
                examRecord.CheatingSuspicionLevel = suspicionLevel;
                
                // 记录作弊日志
                var cheatingSuspicionRecord = examRecord.CheatingSuspicionRecord ?? "[]";
                var cheatRecord = $"{{\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\"type\":\"screen_switch\",\"count\":{examRecord.ScreenSwitchCount}}}";
                examRecord.CheatingSuspicionRecord = cheatingSuspicionRecord.TrimEnd(']') + (cheatingSuspicionRecord.Length > 2 ? "," : "") + cheatRecord + "]";
            }
        }
        
        await Repository.UpdateAsync(examRecord);
        
        return true;
    }
    
    /// <summary>
    /// 获取考试记录及答题详情
    /// </summary>
    public async Task<ExamRecordDto> GetExamRecordDetailAsync(long recordId)
    {
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.ExamSetting)
            .Include(r => r.Student)
            .Include(r => r.AnswerRecords)
            .ThenInclude(a => a.QuestionVersion)
            .ThenInclude(qv => qv.Question)
            .FirstOrDefaultAsync(r => r.Id == recordId);
            
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }
        
        return Mapper.Map<ExamRecordDto>(examRecord);
    }
}

/// <summary>
/// 查询扩展方法
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// 动态排序扩展方法
    /// </summary>
    public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string propertyName, bool isDescending)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return query;
        }
        
        var parameter = Expression.Parameter(typeof(T), "x");
        
        // 处理嵌套属性，如"ExamRecord.StartTime"
        Expression property = parameter;
        foreach (var member in propertyName.Split('.'))
        {
            property = Expression.PropertyOrField(property, member);
        }
        
        var lambda = Expression.Lambda(property, parameter);
        var methodName = isDescending ? "OrderByDescending" : "OrderBy";
        
        var methodCallExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), property.Type },
            query.Expression,
            Expression.Quote(lambda));
            
        return query.Provider.CreateQuery<T>(methodCallExpression);
    }
} 