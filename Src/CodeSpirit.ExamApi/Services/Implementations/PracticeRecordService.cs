using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.PracticeRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 练习记录服务实现
/// </summary>
public class PracticeRecordService : BaseCRUDIService<PracticeRecord, PracticeRecordDto, long, CreatePracticeRecordDto, UpdatePracticeRecordDto, PracticeRecordBatchImportDto>, IPracticeRecordService
{
    private readonly IRepository<PracticeRecord> _repository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PracticeRecordService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PracticeRecordService(
        IRepository<PracticeRecord> repository,
        IRepository<Student> studentRepository,
        IRepository<Question> questionRepository,
        IMapper mapper,
        ILogger<PracticeRecordService> logger)
        : base(repository, mapper)
    {
        _repository = repository;
        _studentRepository = studentRepository;
        _questionRepository = questionRepository;
        _mapper = mapper;
        _logger = logger;
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
} 