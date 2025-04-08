using AutoMapper;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using LinqKit;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.Shared.Extensions;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ExamRecordService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamRecordService(
        IRepository<ExamRecord> repository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<ExamSetting> examSettingRepository,
        IRepository<Student> studentRepository,
        IRepository<QuestionVersion> questionVersionRepository,
        IMapper mapper,
        ILogger<ExamRecordService> logger) : base(repository, mapper)
    {
        _answerRecordRepository = answerRecordRepository;
        _examSettingRepository = examSettingRepository;
        _studentRepository = studentRepository;
        _questionVersionRepository = questionVersionRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取考试记录分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <param name="predicate">额外查询条件</param>
    /// <param name="includes">关联实体</param>
    /// <returns>考试记录分页列表</returns>
    public override async Task<PageList<ExamRecordDto>> GetPagedListAsync<TQueryDto>(
        TQueryDto queryDto, 
        Expression<Func<ExamRecord, bool>> predicate = null,
        params string[] includes)
    {
        if (queryDto is ExamRecordQueryDto examRecordQueryDto)
        {
            var predicateBuilder = PredicateBuilder.New<ExamRecord>(true);
            
            // 合并传入的查询条件
            if (predicate != null)
            {
                predicateBuilder = predicateBuilder.And(predicate);
            }
            
            // 按考试设置ID筛选
            if (examRecordQueryDto.ExamSettingId.HasValue)
            {
                predicateBuilder = predicateBuilder.And(x => x.ExamSettingId == examRecordQueryDto.ExamSettingId.Value);
            }
            
            // 按学生姓名筛选
            if (!string.IsNullOrWhiteSpace(examRecordQueryDto.StudentName))
            {
                predicateBuilder = predicateBuilder.And(x => x.Student.Name.Contains(examRecordQueryDto.StudentName));
            }
            
            // 按考试状态筛选
            if (examRecordQueryDto.Status.HasValue)
            {
                predicateBuilder = predicateBuilder.And(x => x.Status == examRecordQueryDto.Status.Value);
            }
            
            // 按是否通过筛选
            if (examRecordQueryDto.IsPassed.HasValue)
            {
                predicateBuilder = predicateBuilder.And(x => x.IsPassed == examRecordQueryDto.IsPassed.Value);
            }
            
            // 按开始时间范围筛选
            if (examRecordQueryDto.StartTimeRange != null && examRecordQueryDto.StartTimeRange.Length == 2)
            {
                DateTime startFrom = examRecordQueryDto.StartTimeRange[0];
                DateTime startTo = examRecordQueryDto.StartTimeRange[1].AddDays(1).AddSeconds(-1); // 结束时间设为当天的23:59:59
                
                predicateBuilder = predicateBuilder.And(x => x.StartTime >= startFrom && x.StartTime <= startTo);
            }
            
            // 按提交时间范围筛选
            if (examRecordQueryDto.SubmitTimeRange != null && examRecordQueryDto.SubmitTimeRange.Length == 2)
            {
                DateTime submitFrom = examRecordQueryDto.SubmitTimeRange[0];
                DateTime submitTo = examRecordQueryDto.SubmitTimeRange[1].AddDays(1).AddSeconds(-1); // 结束时间设为当天的23:59:59
                
                predicateBuilder = predicateBuilder.And(x => x.SubmitTime >= submitFrom && x.SubmitTime <= submitTo);
            }
            
            // 按作弊嫌疑等级最小值筛选
            if (examRecordQueryDto.MinCheatingSuspicionLevel.HasValue)
            {
                predicateBuilder = predicateBuilder.And(x => x.CheatingSuspicionLevel >= examRecordQueryDto.MinCheatingSuspicionLevel.Value);
            }
            
            // 必须包含Student表关联，以支持姓名搜索和准考证号
            var includesList = includes.ToList();
            if (!includesList.Contains("Student"))
            {
                includesList.Add("Student");
            }
            
            // 必须包含AnswerRecords和Question信息，用于计算各题型得分
            if (!includesList.Contains("AnswerRecords"))
            {
                includesList.Add("AnswerRecords");
            }
            
            if (!includesList.Contains("AnswerRecords.QuestionVersion"))
            {
                includesList.Add("AnswerRecords.QuestionVersion");
            }
            
            if (!includesList.Contains("AnswerRecords.QuestionVersion.Question"))
            {
                includesList.Add("AnswerRecords.QuestionVersion.Question");
            }
            
            // 获取查询结果总数
            var totalCount = await Repository.CreateQuery()
                .Where(predicateBuilder)
                .CountAsync();
                
            // 获取分页数据
            var query = Repository.CreateQuery()
                .Where(predicateBuilder);
                
            // 应用包含
            foreach (var include in includesList)
            {
                query = query.Include(include);
            }
            
            // 应用排序和分页
            string orderBy = queryDto.OrderBy ?? "CreatedAt";
            string orderDir = queryDto.OrderDir ?? "desc";
            
            var pagedQuery = query
                .ApplySorting(orderBy, orderDir)
                .Skip((examRecordQueryDto.Page - 1) * examRecordQueryDto.PerPage)
                .Take(examRecordQueryDto.PerPage);
                
            var examRecords = await pagedQuery.ToListAsync();
            
            // 映射到DTO并计算各题型得分
            var examRecordDtos = new List<ExamRecordDto>();
            foreach (var examRecord in examRecords)
            {
                var examRecordDto = Mapper.Map<ExamRecordDto>(examRecord);
                
                // 计算各题型得分
                if (examRecord.AnswerRecords != null && examRecord.AnswerRecords.Any())
                {
                    examRecordDto.SingleChoiceScore = examRecord.AnswerRecords
                        .Where(a => a.QuestionVersion?.Question?.Type == QuestionType.SingleChoice && a.Score.HasValue)
                        .Sum(a => a.Score ?? 0);
                        
                    examRecordDto.MultipleChoiceScore = examRecord.AnswerRecords
                        .Where(a => a.QuestionVersion?.Question?.Type == QuestionType.MultipleChoice && a.Score.HasValue)
                        .Sum(a => a.Score ?? 0);
                        
                    examRecordDto.TrueFalseScore = examRecord.AnswerRecords
                        .Where(a => a.QuestionVersion?.Question?.Type == QuestionType.TrueFalse && a.Score.HasValue)
                        .Sum(a => a.Score ?? 0);
                }
                
                // 添加准考证号
                if (examRecord.Student != null)
                {
                    examRecordDto.AdmissionTicket = examRecord.Student.AdmissionTicket;
                }
                
                examRecordDtos.Add(examRecordDto);
            }
            
            return new PageList<ExamRecordDto>(examRecordDtos, totalCount);
        }
        
        return await base.GetPagedListAsync(queryDto, predicate, includes);
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
        var now = DateTime.UtcNow;
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
        examRecord.StartTime = DateTime.UtcNow;
        
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
    /// 批量提交答案
    /// </summary>
    /// <param name="examRecordId">考试记录ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>是否全部成功</returns>
    public async Task<bool> SubmitAnswersAsync(long examRecordId, List<ClientExamAnswerDto> answers)
    {
        if (answers == null || !answers.Any())
        {
            throw new ArgumentException("答案列表不能为空", nameof(answers));
        }
        
        // 验证考试记录是否存在
        var examRecord = await Repository.GetByIdAsync(examRecordId);
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }
        
        // 检查考试状态
        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束，无法提交答案");
        }
        
        // 检查考试是否超时但仍然接受最后的答案提交
        bool isOvertime = false;
        if (examRecord.StartTime != null && examRecord.ExamSetting?.Duration > 0)
        {
            var endTime = examRecord.StartTime.AddMinutes(examRecord.ExamSetting.Duration);
            if (DateTime.UtcNow > endTime)
            {
                isOvertime = true;
                _logger.LogWarning($"考试已超时，但仍接受最后的答案提交。考试记录ID: {examRecord.Id}");
            }
        }
        
        // 获取考试试卷信息
        var examPaper = await _examSettingRepository.CreateQuery()
            .Include(es => es.ExamPaper)
            .ThenInclude(ep => ep.ExamPaperQuestions)
            .FirstOrDefaultAsync(es => es.Id == examRecord.ExamSettingId);
            
        if (examPaper == null || examPaper.ExamPaper == null || examPaper.ExamPaper.ExamPaperQuestions == null)
        {
            throw new BusinessException("试卷信息不存在，无法提交答案");
        }
        
        // 获取所有试卷题目的映射，用于验证提交的答案
        var examPaperQuestionsMap = examPaper.ExamPaper.ExamPaperQuestions
            .ToDictionary(q => q.QuestionId, q => q);
            
        // 获取所有已有的答题记录
        var existingAnswerRecords = await _answerRecordRepository.CreateQuery()
            .Where(a => a.ExamRecordId == examRecordId)
            .ToListAsync();
            
        var existingAnswerMap = existingAnswerRecords
            .ToDictionary(a => a.QuestionId, a => a);
            
        List<ExamAnswerRecord> recordsToUpdate = new List<ExamAnswerRecord>();
        List<ExamAnswerRecord> recordsToAdd = new List<ExamAnswerRecord>();
        
        foreach (var answer in answers)
        {
            // 验证题目是否存在于试卷中
            if (!examPaperQuestionsMap.TryGetValue(answer.QuestionId, out var examPaperQuestion))
            {
                _logger.LogWarning($"题目 {answer.QuestionId} 不在试卷中，已跳过");
                continue;
            }
            
            // 查找现有的答题记录
            if (existingAnswerMap.TryGetValue(answer.QuestionId, out var answerRecord))
            {
                // 更新已有记录
                answerRecord.Answer = answer.Answer;
                answerRecord.SubmitTime = DateTime.UtcNow;
                
                // 计算答题用时
                if (answerRecord.StartTime.HasValue)
                {
                    answerRecord.Duration = (int)(answerRecord.SubmitTime.Value - answerRecord.StartTime.Value).TotalSeconds;
                }
                
                recordsToUpdate.Add(answerRecord);
            }
            else
            {
                // 创建新记录
                var newRecord = new ExamAnswerRecord
                {
                    ExamRecordId = examRecordId,
                    QuestionId = answer.QuestionId,
                    QuestionVersionId = examPaperQuestion.QuestionVersionId,
                    Answer = answer.Answer,
                    SubmitTime = DateTime.UtcNow,
                    StartTime = DateTime.UtcNow, // 设置相同的开始时间和提交时间
                    Duration = 0,
                    OrderNumber = examPaperQuestion.OrderNumber,
                    IsMarked = false
                };
                
                recordsToAdd.Add(newRecord);
            }
        }
        
        try
        {
            // 批量保存更改
            if (recordsToUpdate.Any())
            {
                await _answerRecordRepository.UpdateRangeAsync(recordsToUpdate);
            }
            
            if (recordsToAdd.Any())
            {
                await _answerRecordRepository.AddRangeAsync(recordsToAdd);
            }
            
            // 记录日志
            _logger.LogInformation($"批量答案提交成功。考试记录ID: {examRecordId}, 更新: {recordsToUpdate.Count}, 新增: {recordsToAdd.Count}");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"批量答案保存失败。考试记录ID: {examRecordId}");
            throw new BusinessException("答案保存失败，请重试");
        }
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
        examRecord.SubmitTime = finishExamDto.SubmitTime ?? DateTime.UtcNow;
        
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
            .ApplySorting(orderBy, orderDir)
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
    /// <summary>
    /// 获取答题预览要素
    /// </summary>
    /// <param name="recordId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>

    public async Task<AnswerPreviewDto> GetAnswerPreviewAsync(long recordId)
    {
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.ExamSetting)
            .Include(r => r.AnswerRecords)
            .Where(r => r.Id == recordId)
            .Select(x => new AnswerPreviewDto {
                ExamPaperId = x.ExamSetting.ExamPaperId,
                Answers = x.AnswerRecords == null? new List<ClientExamAnswerDto>() : x.AnswerRecords.Select(x => new ClientExamAnswerDto {
                    QuestionId = x.QuestionId,
                    Answer = x.Answer
                }).ToList()
            }).FirstOrDefaultAsync();
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        return examRecord;
    }

    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试记录</returns>
    public async Task<ExamRecord> CreateExamRecordAsync(long examId, long studentId, string userIp, string deviceInfo)
    {
        var examSetting = await _examSettingRepository.CreateQuery()
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
        var student = await _studentRepository.GetByIdAsync(studentId);

        if (student == null)
        {
            throw new InvalidOperationException("未找到考生信息");
        }

        // 查找是否已存在未完成的考试记录
        var existingRecord = await Repository.CreateQuery()
            .Where(r => r.ExamSettingId == examId &&
                    r.StudentId == studentId &&
                    r.Status == ExamRecordStatus.InProgress)
            .FirstOrDefaultAsync();

        // 如果存在进行中的考试记录，直接返回
        if (existingRecord != null)
        {
            return existingRecord;
        }

        // 检查考试次数
        var attemptCount = await Repository.CreateQuery()
            .CountAsync(r => r.ExamSettingId == examId && r.StudentId == studentId);

        // 创建考试记录
        var examRecord = new ExamRecord
        {
            ExamSettingId = examId,
            StudentId = studentId,
            AttemptNumber = attemptCount + 1,
            StartTime = now,
            Status = ExamRecordStatus.InProgress,
            IpAddress = userIp,
            DeviceInfo = deviceInfo
        };

        await Repository.AddAsync(examRecord);
        return examRecord;
    }

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <returns>任务完成状态</returns>
    public async Task RecordScreenSwitchForClientAsync(long recordId, long studentId, string userIp)
    {
        try
        {
            // 获取考试记录
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .Where(r => r.Id == recordId && r.StudentId == studentId)
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
            await Repository.UpdateAsync(examRecord);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw;
        }
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">可选的答案列表，用于最后提交前保存</param>
    /// <returns>提交结果，包含是否成功和是否可查看结果</returns>
    public async Task<(bool Success, bool EnableViewResult)> SubmitExamForClientAsync(long recordId, long studentId, List<ClientExamAnswerDto> answers = null)
    {
        try
        {
            // 获取考试记录
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Where(r => r.Id == recordId && r.StudentId == studentId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new AppServiceException(400, "考试记录不存在");
            }

            // 加载试卷题目
            var examPaper = await _examSettingRepository.CreateQuery()
                .Include(es => es.ExamPaper)
                .ThenInclude(ep => ep.ExamPaperQuestions)
                .FirstOrDefaultAsync(es => es.Id == examRecord.ExamSettingId);

            if (examRecord.Status != ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试已提交，不能重复提交");
            }
            
            // 如果提供了未保存的答案，先保存这些答案
            if (answers != null && answers.Any())
            {
                _logger.LogInformation("提交前保存最后 {Count} 个答案", answers.Count);
                await SubmitAnswersAsync(recordId, answers);
            }

            var now = DateTime.UtcNow;
            examRecord.SubmitTime = now;
            examRecord.Status = ExamRecordStatus.Submitted;
            examRecord.Duration = (int)Math.Ceiling((now - examRecord.CreatedAt).TotalMinutes);

            // 获取已保存的所有答案记录
            var existingAnswers = await _answerRecordRepository.CreateQuery()
                .Where(a => a.ExamRecordId == recordId)
                .ToListAsync();

            // 更新考试记录
            await Repository.UpdateAsync(examRecord);

            // 如果是客观题，可以自动评分
            await AutoGradeObjectiveQuestions(examRecord);

            // 返回提交成功状态和是否可以查看结果的设置
            return (true, examRecord.ExamSetting.EnableViewResult);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw;
        }
    }

    /// <summary>
    /// 获取用户的考试历史记录
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>历史考试记录</returns>
    public async Task<List<ClientExamHistoryDto>> GetExamHistoryForClientAsync(long studentId)
    {
        var examHistory = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Where(r => r.StudentId == studentId)
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

    /// <summary>
    /// 获取考试结果（客户端视图）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试结果</returns>
    public async Task<ClientExamResultDto> GetExamResultForClientAsync(long recordId, long studentId)
    {
        try
        {
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId && r.StudentId == studentId)
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
            var answerRecords = await _answerRecordRepository.CreateQuery()
                .Include(a => a.Question)
                .Include(a => a.QuestionVersion)
                .Where(a => a.ExamRecordId == recordId)
                .ToListAsync();

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
                Answers = answerRecords.Select(a => new ClientExamAnswerResultDto
                {
                    QuestionId = a.QuestionId,
                    Content = a.QuestionVersion.Content,
                    Type = a.Question.Type.ToString(),
                    Score = Convert.ToInt32(a.QuestionVersion.DefaultScore),
                    UserAnswer = a.Question.Type == QuestionType.TrueFalse ? 
                        ConvertTrueFalseAnswer(a.Answer) : 
                        a.Answer,
                    CorrectAnswer = a.Question.Type == QuestionType.TrueFalse ? 
                        ConvertTrueFalseAnswer(a.QuestionVersion.CorrectAnswer) : 
                        a.QuestionVersion.CorrectAnswer,
                    IsCorrect = a.IsCorrect ?? false,
                    ObtainedScore = a.Score ?? 0
                }).ToList()
            };

            return result;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw;
        }
    }

    /// <summary>
    /// 将判断题的True/False答案转换为"对"/"错"
    /// </summary>
    /// <param name="answer">原始答案</param>
    /// <returns>转换后的答案</returns>
    private string ConvertTrueFalseAnswer(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return string.Empty;
        }
        
        return answer.Equals("True", StringComparison.OrdinalIgnoreCase) ? "对" : "错";
    }

    // 客观题自动评分
    private async Task AutoGradeObjectiveQuestions(ExamRecord examRecord)
    {
        // 加载所有答案记录
        var answerRecords = await _answerRecordRepository.CreateQuery()
            .Where(a => a.ExamRecordId == examRecord.Id)
            .ToListAsync();

        // 加载所有答案关联的题目和题目版本
        foreach (var answer in answerRecords)
        {
            answer.Question = await _questionVersionRepository.CreateQuery()
                .Include(qv => qv.Question)
                .Where(qv => qv.Id == answer.QuestionVersionId)
                .Select(qv => qv.Question)
                .FirstOrDefaultAsync();

            answer.QuestionVersion = await _questionVersionRepository.GetByIdAsync(answer.QuestionVersionId);
        }

        // 使用评分器进行评分
        var grader = new Graders.ObjectiveQuestionGrader();
        var result = grader.Grade(answerRecords, examRecord.ExamSetting.ExamPaper.PassScore);

        // 如果全部为客观题，更新考试记录状态
        if (result.IsAllObjective)
        {
            examRecord.Score = result.TotalScore;
            examRecord.Status = ExamRecordStatus.Graded;
            examRecord.IsPassed = result.TotalScore >= examRecord.ExamSetting.ExamPaper.PassScore;
            examRecord.GradedTime = DateTime.UtcNow;
            
            await Repository.UpdateAsync(examRecord);
        }
    }

    /// <summary>
    /// 获取考试的所有已保存答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>答案列表</returns>
    public async Task<List<ExamAnswerRecord>> GetExamAnswersAsync(long recordId)
    {
        return await _answerRecordRepository.CreateQuery()
            .Where(a => a.ExamRecordId == recordId)
            .OrderBy(a => a.OrderNumber)
            .ToListAsync();
    }
}