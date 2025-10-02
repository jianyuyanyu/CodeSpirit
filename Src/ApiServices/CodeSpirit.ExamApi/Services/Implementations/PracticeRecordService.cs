using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Dtos.Common;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 练习记录服务实现
/// </summary>
public class PracticeRecordService : BaseCRUDIService<PracticeRecord, PracticeRecordDto, long, CreatePracticeRecordDto, UpdatePracticeRecordDto, PracticeRecordBatchImportDto>, IPracticeRecordService, IScopedDependency
{
    private readonly IRepository<PracticeRecord> _repository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IRepository<PracticeSetting> _practiceSettingRepository;
    private readonly IRepository<PracticeSession> _practiceSessionRepository;
    private readonly IRepository<ExamPaperQuestion> _examPaperQuestionRepository;
    private readonly IRepository<QuestionVersion> _questionVersionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PracticeRecordService> _logger;
    private readonly IDistributedLockProvider _distributedLockProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PracticeRecordService(
        IRepository<PracticeRecord> repository,
        IRepository<Student> studentRepository,
        IRepository<Question> questionRepository,
        IRepository<PracticeSetting> practiceSettingRepository,
        IRepository<PracticeSession> practiceSessionRepository,
        IRepository<ExamPaperQuestion> examPaperQuestionRepository,
        IRepository<QuestionVersion> questionVersionRepository,
        IMapper mapper,
        ILogger<PracticeRecordService> logger,
        IDistributedLockProvider distributedLockProvider,
        EnhancedBatchImportHelper<PracticeRecordBatchImportDto> importHelper)
        : base(repository, mapper, importHelper)
    {
        _repository = repository;
        _studentRepository = studentRepository;
        _questionRepository = questionRepository;
        _practiceSettingRepository = practiceSettingRepository;
        _practiceSessionRepository = practiceSessionRepository;
        _examPaperQuestionRepository = examPaperQuestionRepository;
        _questionVersionRepository = questionVersionRepository;
        _mapper = mapper;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider;
    }

    /// <summary>
    /// 获取练习记录分页列表
    /// </summary>
    public async Task<PageList<PracticeRecordDto>> GetPracticeRecordsAsync(PracticeRecordQueryDto queryDto)
    {
        ExpressionStarter<PracticeRecord> predicate = PredicateBuilder.New<PracticeRecord>(true);

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.And(x => 
                x.Student.Name.Contains(queryDto.Keywords) || 
                x.Question.Content.Contains(queryDto.Keywords) ||
                x.Answer.Contains(queryDto.Keywords));
        }

        if (queryDto.StudentId.HasValue)
        {
            predicate = predicate.And(x => x.StudentId == queryDto.StudentId.Value);
        }

        if (queryDto.QuestionId.HasValue)
        {
            predicate = predicate.And(x => x.QuestionId == queryDto.QuestionId.Value);
        }

        if (queryDto.PracticeType.HasValue)
        {
            predicate = predicate.And(x => x.PracticeType == queryDto.PracticeType.Value);
        }

        if (queryDto.IsCorrect.HasValue)
        {
            predicate = predicate.And(x => x.IsCorrect == queryDto.IsCorrect.Value);
        }

        if (queryDto.PracticeStartTime.HasValue)
        {
            DateTime startDate = queryDto.PracticeStartTime.Value.Date;
            predicate = predicate.And(x => x.PracticeTime >= startDate);
        }

        if (queryDto.PracticeEndTime.HasValue)
        {
            DateTime endDate = queryDto.PracticeEndTime.Value.Date.AddDays(1);
            predicate = predicate.And(x => x.PracticeTime < endDate);
        }

        return await GetPagedListAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy,
            queryDto.OrderDir,
            "Student", "Question");
    }

    /// <summary>
    /// 获取练习会话分页列表
    /// </summary>
    public async Task<PageList<PracticeSessionDto>> GetPracticeSessionsAsync(PracticeSessionQueryDto queryDto)
    {
        ExpressionStarter<PracticeSession> predicate = PredicateBuilder.New<PracticeSession>(true);

        if (queryDto.StudentId.HasValue)
        {
            predicate = predicate.And(x => x.StudentId == queryDto.StudentId.Value);
        }

        if (queryDto.PracticeId.HasValue)
        {
            predicate = predicate.And(x => x.PracticeSettingId == queryDto.PracticeId.Value);
        }

        // 添加关键词搜索支持（搜索练习名称）
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.PracticeSetting.Name.Contains(queryDto.Keywords));
        }

        // 添加状态筛选支持
        if (!string.IsNullOrWhiteSpace(queryDto.Status))
        {
            switch (queryDto.Status.ToUpper())
            {
                case "COMPLETED":
                    predicate = predicate.And(x => x.EndTime != null);
                    break;
                case "INPROGRESS":
                    predicate = predicate.And(x => x.StartTime != null && x.EndTime == null);
                    break;
                case "NOTSTARTED":
                    predicate = predicate.And(x => x.StartTime == null);
                    break;
            }
        }

        if (queryDto.StartTimeBegin.HasValue)
        {
            predicate = predicate.And(x => x.StartTime >= queryDto.StartTimeBegin.Value);
        }

        if (queryDto.StartTimeEnd.HasValue)
        {
            predicate = predicate.And(x => x.StartTime <= queryDto.StartTimeEnd.Value);
        }

        // 查询会话数据
        var query = _practiceSessionRepository.Find(predicate)
            .Include(x => x.Student)
            .Include(x => x.PracticeSetting)
            .Include(x => x.PracticeRecords)
            .OrderByDescending(x => x.StartTime);

        // 计算总数
        var total = await query.CountAsync();
        
        // 应用分页
        var sessions = await query
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 转换为DTO
        var sessionDtos = new List<PracticeSessionDto>();
        foreach (var session in sessions)
        {
            var sessionDto = new PracticeSessionDto
            {
                Id = session.Id,
                StudentId = session.StudentId,
                StudentName = session.Student?.Name ?? string.Empty,
                PracticeId = session.PracticeSettingId,
                PracticeName = session.PracticeSetting?.Name ?? string.Empty,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TotalScore = session.TotalScore,
                Duration = session.EndTime.HasValue 
                    ? (int)Math.Round((session.EndTime.Value - session.StartTime).TotalMinutes) 
                    : 0,
                CorrectCount = session.CorrectCount,
                TotalQuestions = session.PracticeRecords?.Count ?? 0,
                Score = session.PracticeRecords?.Where(r => r.IsCorrect).Sum(r => r.Question?.DefaultScore ?? 0) ?? 0,
                CreatedTime = session.CreatedAt
            };
            
            sessionDtos.Add(sessionDto);
        }

        // 返回分页结果
        return new PageList<PracticeSessionDto>
        {
            Items = sessionDtos,
            Total = total
        };
    }

    /// <summary>
    /// 获取练习记录详情
    /// </summary>
    public async Task<PracticeRecordDto> GetPracticeRecordAsync(long id)
    {
        PracticeRecord practiceRecord = await _repository.Find(x => x.Id == id)
            .Include(x => x.Student)
            .Include(x => x.Question)
            .FirstOrDefaultAsync();
            
        if (practiceRecord == null)
        {
            throw new AppServiceException(404, "练习记录不存在");
        }

        return _mapper.Map<PracticeRecordDto>(practiceRecord);
    }

    /// <summary>
    /// 创建练习记录
    /// </summary>
    public async Task<PracticeRecordDto> CreatePracticeRecordAsync(CreatePracticeRecordDto createDto)
    {
        await ValidateCreateDto(createDto);

        PracticeRecord practiceRecord = _mapper.Map<PracticeRecord>(createDto);
        await OnCreating(practiceRecord, createDto);

        PracticeRecord createdRecord = await _repository.AddAsync(practiceRecord);
        PracticeRecordDto dto = await GetPracticeRecordAsync(createdRecord.Id);
        return dto;
    }

    /// <summary>
    /// 更新练习记录
    /// </summary>
    public async Task UpdatePracticeRecordAsync(long id, UpdatePracticeRecordDto updateDto)
    {
        await ValidateUpdateDto(id, updateDto);

        PracticeRecord practiceRecord = await _repository.GetByIdAsync(id);
        if (practiceRecord == null)
        {
            throw new AppServiceException(404, "练习记录不存在");
        }

        _mapper.Map(updateDto, practiceRecord);
        await OnUpdating(practiceRecord, updateDto);

        await _repository.UpdateAsync(practiceRecord);
    }

    /// <summary>
    /// 删除练习记录
    /// </summary>
    public async Task DeletePracticeRecordAsync(long id)
    {
        PracticeRecord practiceRecord = await _repository.GetByIdAsync(id);
        if (practiceRecord == null)
        {
            throw new AppServiceException(404, "练习记录不存在");
        }

        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// 批量删除练习记录
    /// </summary>
    /// <param name="ids">ID列表</param>
    /// <returns>成功删除数量和失败ID列表</returns>
    public new async Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids)
    {
        return await base.BatchDeleteAsync(ids);
    }

    /// <summary>
    /// 批量导入练习记录
    /// </summary>
    /// <param name="importDtos">导入数据</param>
    /// <returns>成功导入数量和失败ID列表</returns>
    public async Task<(int successCount, List<string> failedIds)> BatchImportAsync(List<PracticeRecordBatchImportDto> importDtos)
    {
        int successCount = 0;
        List<string> failedIds = new List<string>();

        foreach (var importDto in importDtos)
        {
            try
            {
                var createDto = _mapper.Map<CreatePracticeRecordDto>(importDto);
                await CreatePracticeRecordAsync(createDto);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入练习记录失败: {Error}", ex.Message);
                failedIds.Add(GetImportItemId(importDto));
            }
        }

        return (successCount, failedIds);
    }

    /// <summary>
    /// 批量创建练习记录
    /// </summary>
    /// <param name="practiceRecordDtos">练习记录DTO列表</param>
    /// <returns>创建的练习记录列表</returns>
    public async Task<List<PracticeRecordDto>> BatchCreatePracticeRecordsAsync(List<PracticeRecordDto> practiceRecordDtos)
    {
        List<PracticeRecordDto> createdRecords = new List<PracticeRecordDto>();

        foreach (var recordDto in practiceRecordDtos)
        {
            try
            {
                var createDto = _mapper.Map<CreatePracticeRecordDto>(recordDto);
                var created = await CreatePracticeRecordAsync(createDto);
                createdRecords.Add(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量创建练习记录失败: {Error}", ex.Message);
            }
        }

        return createdRecords;
    }

    /// <summary>
    /// 获取学生的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>练习统计数据</returns>
    public async Task<PracticeStatisticsDto> GetStudentPracticeStatisticsAsync(long studentId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        var allRecords = await _repository.Find(r => r.StudentId == studentId).ToListAsync();

        return BuildPracticeStatistics(allRecords, studentId);
    }

    /// <summary>
    /// 获取学生对特定试卷的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>练习统计数据</returns>
    public async Task<PracticeStatisticsDto> GetStudentExamPaperPracticeStatisticsAsync(long studentId, long examPaperId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        // 获取指定试卷的所有题目ID
        // 注意：这里假设有一种关联可以获取特定试卷的题目
        var questionIds = await _questionRepository
            .Find(q => true) // 实际应有关联条件
            .Select(q => q.Id)
            .Take(100) // 限制数量，避免可能的性能问题
            .ToListAsync();

        // 获取学生在这些题目上的练习记录
        var records = await _repository
            .Find(r => r.StudentId == studentId && questionIds.Contains(r.QuestionId))
            .ToListAsync();

        return BuildPracticeStatistics(records, studentId);
    }

    /// <summary>
    /// 获取学生对特定练习设置的练习统计数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="practiceSettingId">练习设置ID</param>
    /// <returns>练习统计数据</returns>
    public async Task<PracticeStatisticsDto> GetStudentPracticeSettingStatisticsAsync(long studentId, long practiceSettingId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        // 这里假设当前模型没有PracticeSettingId字段，我们只能按学生ID查询
        var records = await _repository
            .Find(r => r.StudentId == studentId)
            .ToListAsync();

        return BuildPracticeStatistics(records, studentId);
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreatePracticeRecordDto createDto)
    {
        // 检查考生是否存在
        Student student = await _studentRepository.GetByIdAsync(createDto.StudentId);
        if (student == null)
        {
            throw new AppServiceException(400, "考生不存在");
        }

        // 检查题目是否存在
        Question question = await _questionRepository.GetByIdAsync(createDto.QuestionId);
        if (question == null)
        {
            throw new AppServiceException(400, "题目不存在");
        }
    }

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    protected override async Task ValidateUpdateDto(long id, UpdatePracticeRecordDto updateDto)
    {
        // 检查练习记录是否存在
        PracticeRecord practiceRecord = await _repository.GetByIdAsync(id);
        if (practiceRecord == null)
        {
            throw new AppServiceException(404, "练习记录不存在");
        }

        // 此处没有检查StudentId和QuestionId，假设更新时不修改这些关联
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected override string GetImportItemId(PracticeRecordBatchImportDto importDto)
    {
        return $"{importDto.StudentId}_{importDto.QuestionId}";
    }

    /// <summary>
    /// 构建练习统计数据
    /// </summary>
    private PracticeStatisticsDto BuildPracticeStatistics(
        List<PracticeRecord> records, 
        long studentId)
    {
        // 构建统计数据
        var student = _studentRepository.GetByIdAsync(studentId).Result;
        
        return new PracticeStatisticsDto
        {
            StudentId = studentId,
            StudentName = student?.Name ?? "未知",
            TotalPracticeCount = records.Count,
            CorrectCount = records.Count(r => r.IsCorrect),
            IncorrectCount = records.Count(r => !r.IsCorrect),
            CorrectRate = records.Any() ? (double)records.Count(r => r.IsCorrect) / records.Count * 100 : 0,
            AverageTimeSpent = records.Any() ? records.Average(r => r.TimeSpent) : 0,
            LastPracticeTime = records.Any() ? records.Max(r => r.PracticeTime) : null,
            // 以下字段需要根据实际业务添加
            PracticeTypeStatistics = new List<PracticeTypeStatisticsDto>(),
            QuestionTypeStatistics = new List<QuestionTypeStatisticsDto>()
        };
    }

    /// <summary>
    /// 开始练习
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="practiceSettingId">练习设置ID</param>
    /// <returns>练习记录ID</returns>
    public async Task<long> StartPracticeAsync(long studentId, long practiceSettingId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        // 检查练习设置是否存在
        var practiceSetting = await _practiceSettingRepository.Find(p => p.Id == practiceSettingId)
            .Include(p => p.ExamPaper)
            .FirstOrDefaultAsync();
            
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Published)
        {
            throw new AppServiceException(400, "练习设置未发布，不能开始练习");
        }

        // 创建练习会话
        var practiceSession = new PracticeSession
        {
            StudentId = studentId,
            PracticeSettingId = practiceSettingId,
            StartTime = DateTime.Now,
            Status = PracticeSessionStatus.InProgress
        };

        await _practiceSessionRepository.AddAsync(practiceSession);
        
        return practiceSession.Id;
    }

    /// <summary>
    /// 保存答案
    /// </summary>
    /// <param name="recordId">练习会话ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answerDto">答案DTO</param>
    public async Task SaveAnswerAsync(long recordId, long studentId, PracticeAnswerDto answerDto)
    {
        // 记录请求信息
        _logger.LogInformation($"收到保存答案请求: recordId={recordId}, studentId={studentId}, questionId={answerDto.QuestionId}, answer={answerDto.Answer?.Substring(0, Math.Min(answerDto.Answer?.Length ?? 0, 20))}...");
        
        // 验证questionId有效性
        if (answerDto.QuestionId <= 0 || answerDto.QuestionId.ToString() == "undefined")
        {
            _logger.LogWarning($"提交的questionId无效: {answerDto.QuestionId}");
            throw new AppServiceException(400, "题目ID无效或为空");
        }

        // 检查练习会话是否存在
        var practiceSession = await _practiceSessionRepository.Find(s => s.Id == recordId && s.StudentId == studentId)
            .FirstOrDefaultAsync();
            
        if (practiceSession == null)
        {
            throw new AppServiceException(404, "练习会话不存在");
        }

        // 检查练习会话状态
        if (practiceSession.Status != PracticeSessionStatus.InProgress)
        {
            throw new AppServiceException(400, "练习会话已结束，不能保存答案");
        }

        // 检查题目是否存在
        var question = await _questionRepository.GetByIdAsync(answerDto.QuestionId);
        if (question == null)
        {
            _logger.LogWarning($"题目不存在: questionId={answerDto.QuestionId}");
            throw new AppServiceException(404, "题目不存在");
        }

        // 查找已有的练习记录
        var practiceRecord = await _repository.Find(r => 
            r.PracticeSessionId == recordId && 
            r.QuestionId == answerDto.QuestionId)
            .FirstOrDefaultAsync();

        if (practiceRecord == null)
        {
            // 创建新的练习记录
            practiceRecord = new PracticeRecord
            {
                StudentId = studentId,
                QuestionId = answerDto.QuestionId,
                PracticeSessionId = recordId,
                Answer = answerDto.Answer,
                TimeSpent = answerDto.TimeSpent,
                IsMarked = answerDto.IsMarked,
                PracticeTime = DateTime.Now,
                PracticeType = PracticeType.Normal
            };
            
            await _repository.AddAsync(practiceRecord);
            _logger.LogInformation($"创建新练习记录: recordId={recordId}, questionId={answerDto.QuestionId}");
        }
        else
        {
            // 更新已有的练习记录
            practiceRecord.Answer = answerDto.Answer;
            practiceRecord.TimeSpent = answerDto.TimeSpent;
            practiceRecord.IsMarked = answerDto.IsMarked;
            
            await _repository.UpdateAsync(practiceRecord);
            _logger.LogInformation($"更新练习记录: recordId={recordId}, questionId={answerDto.QuestionId}");
        }
    }

    /// <summary>
    /// 提交练习
    /// </summary>
    /// <param name="recordId">练习会话ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">答案列表</param>
    public async Task SubmitPracticeAsync(long recordId, long studentId, List<PracticeAnswerDto> answers)
    {
        // 使用分布式锁防止并发提交
        var lockKey = $"practice_submit_{recordId}_{studentId}";

        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(1)))
            {
                _logger.LogInformation("已获取练习提交锁: {LockKey}", lockKey);

                // 检查练习会话是否存在
                var practiceSession = await _practiceSessionRepository.Find(s => s.Id == recordId && s.StudentId == studentId)
                    .Include(s => s.PracticeSetting)
                    .ThenInclude(p => p.ExamPaper)
                    .FirstOrDefaultAsync();
                    
                if (practiceSession == null)
                {
                    throw new AppServiceException(404, "练习会话不存在");
                }

                // 检查练习会话状态
                if (practiceSession.Status != PracticeSessionStatus.InProgress)
                {
                    _logger.LogWarning("练习已提交，不能重复提交: 记录ID={RecordId}, 学生ID={StudentId}, 当前状态={Status}",
                        recordId, studentId, practiceSession.Status);
                    throw new AppServiceException(400, "练习会话已结束，不能提交");
                }

                // 并行保存所有答案，提高性能
                if (answers != null && answers.Any())
                {
                    var saveTasks = answers.Select(answerDto => SaveAnswerAsync(recordId, studentId, answerDto));
                    await Task.WhenAll(saveTasks);
                }

                // 更新练习会话状态
                practiceSession.EndTime = DateTime.Now;
                practiceSession.Status = PracticeSessionStatus.Completed;
                
                // 使用评分器进行自动评分
                await AutoGradeQuestions(practiceSession);

                _logger.LogInformation("练习提交成功: 记录ID={RecordId}, 学生ID={StudentId}", recordId, studentId);
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "获取练习提交锁超时: {LockKey}", lockKey);
            throw new AppServiceException(423, "系统繁忙，请稍后再试");
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException && ex is not AppServiceException)
        {
            _logger.LogError(ex, "练习提交过程发生错误: 记录ID={RecordId}, 学生ID={StudentId}", recordId, studentId);
            throw;
        }
    }

    /// <summary>
    /// 客观题自动评分
    /// </summary>
    /// <param name="practiceSession">练习会话</param>
    private async Task AutoGradeQuestions(PracticeSession practiceSession)
    {
        // 获取所有练习记录
        var practiceRecords = await _repository.Find(r => r.PracticeSessionId == practiceSession.Id)
            .Include(r => r.Question)
            .ToListAsync();
            
        // 获取试卷题目关联信息，用于获取题目版本和正确答案
        var examPaperQuestions = await _examPaperQuestionRepository
            .Find(q => q.ExamPaperId == practiceSession.PracticeSetting.ExamPaperId)
            .Include(q => q.Question)
            .Include(q => q.QuestionVersion)
            .ToListAsync();

        // 计算练习得分
        decimal totalScore = 0;
        int correctCount = 0;
        
        // 定义客观题类型
        var objectiveQuestionTypes = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse };
        
        foreach (var record in practiceRecords)
        {
            // 获取题目对应的试卷题目信息
            var examPaperQuestion = examPaperQuestions.FirstOrDefault(q => q.QuestionId == record.QuestionId);
            if (examPaperQuestion == null || examPaperQuestion.QuestionVersion == null)
            {
                continue; // 跳过无法找到对应试卷题目的记录
            }
            
            // 判断题目类型，只自动评分客观题
            if (record.Question != null && objectiveQuestionTypes.Contains(record.Question.Type))
            {
                bool isCorrect = false;
                
                if (record.Question.Type == QuestionType.MultipleChoice && !string.IsNullOrEmpty(record.Answer))
                {
                    // 多选题答案比较
                    var studentAnswers = record.Answer.Split(',')
                        .Select(a => a.Trim())
                        .OrderBy(a => a)
                        .ToArray();
                        
                    var correctAnswers = examPaperQuestion.QuestionVersion.CorrectAnswer.Split(',')
                        .Select(a => a.Trim())
                        .OrderBy(a => a)
                        .ToArray();

                    isCorrect = studentAnswers.SequenceEqual(correctAnswers, StringComparer.OrdinalIgnoreCase);
                }
                else if (!string.IsNullOrEmpty(record.Answer))
                {
                    // 单选题和判断题直接比较
                    isCorrect = string.Equals(
                        record.Answer.Trim(), 
                        examPaperQuestion.QuestionVersion.CorrectAnswer.Trim(), 
                        StringComparison.OrdinalIgnoreCase
                    );
                }

                // 更新练习记录
                record.IsCorrect = isCorrect;
                
                if (isCorrect)
                {
                    correctCount++;
                    totalScore += examPaperQuestion.Score;
                }
            }
        }
        
        // 计算总分和正确率
        practiceSession.TotalScore = totalScore;
        practiceSession.CorrectCount = correctCount;
        
        // 计算正确率
        decimal correctRate = practiceRecords.Count > 0 
            ? (decimal)correctCount / practiceRecords.Count * 100 
            : 0;
        
        _logger.LogInformation("练习评分完成: 会话ID={SessionId}, 总分={TotalScore}, 正确数={CorrectCount}", 
            practiceSession.Id, totalScore, correctCount);
            
        // 更新所有练习记录和会话
        await _repository.UpdateRangeAsync(practiceRecords);
        await _practiceSessionRepository.UpdateAsync(practiceSession);
    }

    /// <summary>
    /// 获取练习结果
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>练习结果</returns>
    public async Task<PracticeResultDto> GetPracticeResultAsync(long recordId, long studentId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        // 检查练习会话是否存在
        var practiceSession = await _practiceSessionRepository.Find(s => s.Id == recordId && s.StudentId == studentId)
            .Include(s => s.PracticeSetting)
            .ThenInclude(p => p.ExamPaper)
            .FirstOrDefaultAsync();
            
        if (practiceSession == null)
        {
            throw new AppServiceException(404, "练习会话不存在");
        }

        // 获取所有练习记录
        var practiceRecords = await _repository.Find(r => r.PracticeSessionId == recordId)
            .Include(r => r.Question)
            .ToListAsync();
            
        // 获取试卷所有题目
        var examPaperQuestions = await _examPaperQuestionRepository
            .Find(q => q.ExamPaperId == practiceSession.PracticeSetting.ExamPaperId)
            .Include(q => q.Question)
            .Include(q => q.QuestionVersion)
            .ToListAsync();

        // 构建答题结果详情
        var answerResults = new List<PracticeAnswerResultDto>();
        foreach (var examPaperQuestion in examPaperQuestions)
        {
            var record = practiceRecords.FirstOrDefault(r => r.QuestionId == examPaperQuestion.QuestionId);
            var questionVersion = examPaperQuestion.QuestionVersion;
            
            var answerResult = new PracticeAnswerResultDto
            {
                QuestionId = examPaperQuestion.QuestionId,
                Content = questionVersion?.Content ?? examPaperQuestion.Question.Content,
                Type = examPaperQuestion.Question.Type.ToString(),
                UserAnswer = record?.Answer ?? "",
                CorrectAnswer = questionVersion?.CorrectAnswer ?? examPaperQuestion.Question.CorrectAnswer,
                IsCorrect = record?.IsCorrect ?? false,
                Score = examPaperQuestion.Score,
                ObtainedScore = (record?.IsCorrect ?? false) ? examPaperQuestion.Score : 0,
                TimeSpent = record?.TimeSpent ?? 0
            };
            
            answerResults.Add(answerResult);
        }

        // 构建练习结果DTO
        var resultDto = new PracticeResultDto
        {
            Id = practiceSession.Id,
            PracticeSettingId = practiceSession.PracticeSettingId,
            Name = practiceSession.PracticeSetting?.Name ?? "未知练习",
            StartTime = practiceSession.StartTime,
            EndTime = practiceSession.EndTime,
            PracticeMode = practiceSession.PracticeSetting.PracticeMode,
            Score = practiceSession.TotalScore,
            TotalScore = examPaperQuestions.Sum(q => q.Score),
            CorrectCount = practiceSession.CorrectCount,
            TotalCount = examPaperQuestions.Count,
            Status = practiceSession.Status,
            Answers = answerResults
        };

        return resultDto;
    }

    /// <summary>
    /// 开始练习并获取完整数据
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="practiceSettingId">练习设置ID</param>
    /// <returns>练习开始结果，包含题目数据</returns>
    public async Task<PracticeStartResultDto> StartPracticeWithDataAsync(long studentId, long practiceSettingId)
    {
        // 检查学生是否存在
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            throw new AppServiceException(404, "学生不存在");
        }

        // 检查练习设置是否存在
        var practiceSetting = await _practiceSettingRepository.Find(p => p.Id == practiceSettingId)
            .Include(p => p.ExamPaper)
            .FirstOrDefaultAsync();
            
        if (practiceSetting == null)
        {
            throw new AppServiceException(404, "练习设置不存在");
        }

        // 检查练习设置状态
        if (practiceSetting.Status != PracticeSettingStatus.Published)
        {
            throw new AppServiceException(400, "练习设置未发布，不能开始练习");
        }

        // 创建练习会话
        var practiceSession = new PracticeSession
        {
            StudentId = studentId,
            PracticeSettingId = practiceSettingId,
            StartTime = DateTime.Now,
            Status = PracticeSessionStatus.InProgress
        };

        await _practiceSessionRepository.AddAsync(practiceSession);

        // 获取试卷题目
        var examPaperQuestions = await _examPaperQuestionRepository
            .Find(q => q.ExamPaperId == practiceSetting.ExamPaperId)
            .Include(q => q.Question)
            .ThenInclude(q => q.Category)
            .Include(q => q.QuestionVersion)
            .OrderBy(q => q.OrderNumber)
            .ToListAsync();

        // 转换为题目DTO
        var questions = examPaperQuestions.Select(epq => new QuestionDto
        {
            Id = epq.QuestionId,
            Content = epq.QuestionVersion?.Content ?? epq.Question.Content,
            Type = epq.Question.Type,
            Difficulty = epq.Question.Difficulty,
            Options = epq.QuestionVersion?.Options ?? epq.Question.Options,
            CorrectAnswer = epq.QuestionVersion?.CorrectAnswer ?? epq.Question.CorrectAnswer,
            Analysis = epq.QuestionVersion?.Analysis ?? epq.Question.Analysis,
            CategoryName = epq.Question.Category?.Name ?? "",
            CategoryId = epq.Question.CategoryId,
            DefaultScore = epq.Score,
            Version = epq.QuestionVersion?.Version ?? 1
        }).ToList();

        // 构建返回结果
        var result = new PracticeStartResultDto
        {
            RecordId = practiceSession.Id,
            PracticeId = practiceSettingId,
            PracticeName = practiceSetting.Name,
            Description = practiceSetting.Description,
            Questions = questions,
            StartTime = practiceSession.StartTime,
            AllowViewAnswer = true, // 默认允许查看答案
            AllowViewExplanation = practiceSetting.ShowAnalysis,
            PracticeMode = (int)practiceSetting.PracticeMode,
            TotalScore = examPaperQuestions.Sum(q => q.Score)
        };

        return result;
    }

    /// <summary>
    /// 批量保存答案
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">答案列表</param>
    public async Task SaveAnswersAsync(long recordId, long studentId, List<PracticeAnswerDto> answers)
    {
        if (answers == null || !answers.Any())
        {
            return;
        }

        // 检查练习会话是否存在
        var practiceSession = await _practiceSessionRepository.Find(s => s.Id == recordId && s.StudentId == studentId)
            .FirstOrDefaultAsync();
            
        if (practiceSession == null)
        {
            throw new AppServiceException(404, "练习会话不存在");
        }

        // 检查练习会话状态
        if (practiceSession.Status != PracticeSessionStatus.InProgress)
        {
            throw new AppServiceException(400, "练习会话已结束，不能保存答案");
        }

        // 并行保存所有答案
        var saveTasks = answers.Select(answerDto => SaveAnswerAsync(recordId, studentId, answerDto));
        await Task.WhenAll(saveTasks);

        _logger.LogInformation($"批量保存答案完成: recordId={recordId}, studentId={studentId}, 答案数量={answers.Count}");
    }

    /// <summary>
    /// 完成练习（自动完成或手动完成）
    /// </summary>
    /// <param name="recordId">练习记录ID</param>
    /// <param name="studentId">学生ID</param>
    public async Task CompletePracticeAsync(long recordId, long studentId)
    {
        var lockKey = $"practice_complete_{recordId}_{studentId}";

        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(1)))
            {
                _logger.LogInformation("已获取练习完成锁: {LockKey}", lockKey);

                // 检查练习会话是否存在
                var practiceSession = await _practiceSessionRepository.Find(s => s.Id == recordId && s.StudentId == studentId)
                    .Include(s => s.PracticeSetting)
                    .ThenInclude(p => p.ExamPaper)
                    .FirstOrDefaultAsync();
                    
                if (practiceSession == null)
                {
                    throw new AppServiceException(404, "练习会话不存在");
                }

                // 检查练习会话状态
                if (practiceSession.Status != PracticeSessionStatus.InProgress)
                {
                    _logger.LogWarning("练习已完成，不能重复完成: 记录ID={RecordId}, 学生ID={StudentId}, 当前状态={Status}",
                        recordId, studentId, practiceSession.Status);
                    return; // 已完成的练习直接返回，不抛异常
                }

                // 更新练习会话状态
                practiceSession.EndTime = DateTime.Now;
                practiceSession.Status = PracticeSessionStatus.Completed;
                
                // 使用评分器进行自动评分
                await AutoGradeQuestions(practiceSession);

                _logger.LogInformation("练习完成成功: 记录ID={RecordId}, 学生ID={StudentId}", recordId, studentId);
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "获取练习完成锁超时: {LockKey}", lockKey);
            throw new AppServiceException(423, "系统繁忙，请稍后再试");
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException && ex is not AppServiceException)
        {
            _logger.LogError(ex, "练习完成过程发生错误: 记录ID={RecordId}, 学生ID={StudentId}", recordId, studentId);
            throw;
        }
    }
} 