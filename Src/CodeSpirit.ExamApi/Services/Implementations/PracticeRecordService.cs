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

        if (queryDto.StartTime.HasValue)
        {
            DateTime startDate = queryDto.StartTime.Value.Date;
            predicate = predicate.And(x => x.PracticeTime >= startDate);
        }

        if (queryDto.EndTime.HasValue)
        {
            DateTime endDate = queryDto.EndTime.Value.Date.AddDays(1);
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

        // 检查考生是否存在
        Student student = await _studentRepository.GetByIdAsync(updateDto.StudentId);
        if (student == null)
        {
            throw new AppServiceException(400, "考生不存在");
        }

        // 检查题目是否存在
        Question question = await _questionRepository.GetByIdAsync(updateDto.QuestionId);
        if (question == null)
        {
            throw new AppServiceException(400, "题目不存在");
        }
    }

    /// <summary>
    /// 获取导入项的ID
    /// </summary>
    protected override string GetImportItemId(PracticeRecordBatchImportDto importDto)
    {
        return $"{importDto.StudentId}_{importDto.QuestionId}";
    }
} 