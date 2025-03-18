using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.WrongQuestion;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 错题服务实现
/// </summary>
public class WrongQuestionService : BaseCRUDService<WrongQuestion, WrongQuestionDto, long, CreateWrongQuestionDto, UpdateWrongQuestionDto>, IWrongQuestionService
{
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<Question> _questionRepository;
    private readonly ILogger<WrongQuestionService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WrongQuestionService(
        IRepository<WrongQuestion> repository,
        IRepository<Student> studentRepository,
        IRepository<Question> questionRepository,
        IMapper mapper,
        ILogger<WrongQuestionService> logger)
        : base(repository, mapper)
    {
        _studentRepository = studentRepository;
        _questionRepository = questionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取错题分页列表
    /// </summary>
    public async Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<WrongQuestion>(true);

        if (queryDto.StudentId.HasValue)
        {
            predicate = predicate.And(x => x.StudentId == queryDto.StudentId.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.Tags))
        {
            predicate = predicate.And(x => x.Tags != null && x.Tags.Contains(queryDto.Tags));
        }

        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(x => x.LastWrongTime >= queryDto.StartTime.Value);
        }

        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(x => x.LastWrongTime <= queryDto.EndTime.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.And(x =>
                (x.Question != null && x.Question.Content.Contains(queryDto.Keywords)) ||
                (x.LastWrongAnswer != null && x.LastWrongAnswer.Contains(queryDto.Keywords)) ||
                (x.Notes != null && x.Notes.Contains(queryDto.Keywords))
            );
        }

        return await GetPagedListAsync(
            queryDto,
            predicate,
            "Student", "Question"
        );
    }

    /// <summary>
    /// 获取考生的错题列表
    /// </summary>
    public async Task<List<WrongQuestionDto>> GetStudentWrongQuestionsAsync(long studentId)
    {
        var wrongQuestions = await Repository.CreateQuery()
            .Where(x => x.StudentId == studentId)
            .Include(x => x.Student)
            .Include(x => x.Question)
            .OrderByDescending(x => x.LastWrongTime)
            .ToListAsync();

        return Mapper.Map<List<WrongQuestionDto>>(wrongQuestions);
    }

    /// <summary>
    /// 记录错题（如果已存在则更新错误次数）
    /// </summary>
    public async Task<WrongQuestionDto> RecordWrongQuestionAsync(CreateWrongQuestionDto createDto)
    {
        // 检查是否已存在该错题记录
        var existingWrongQuestion = await Repository.CreateQuery()
            .Where(x => x.StudentId == createDto.StudentId && x.QuestionId == createDto.QuestionId)
            .FirstOrDefaultAsync();

        if (existingWrongQuestion != null)
        {
            // 更新已有记录
            existingWrongQuestion.WrongCount += 1;
            existingWrongQuestion.LastWrongAnswer = createDto.LastWrongAnswer;
            existingWrongQuestion.LastWrongTime = createDto.LastWrongTime;
            
            // 只有在提供了新值的情况下才更新这些可选字段
            if (!string.IsNullOrEmpty(createDto.Tags))
            {
                existingWrongQuestion.Tags = createDto.Tags;
            }
            
            if (!string.IsNullOrEmpty(createDto.Notes))
            {
                existingWrongQuestion.Notes = createDto.Notes;
            }

            await Repository.UpdateAsync(existingWrongQuestion);
            
            return Mapper.Map<WrongQuestionDto>(existingWrongQuestion);
        }
        else
        {
            // 创建新记录
            return await CreateAsync(createDto);
        }
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateWrongQuestionDto createDto)
    {
        await base.ValidateCreateDto(createDto);

        // 验证考生是否存在
        var studentExists = await _studentRepository.ExistsAsync(s => s.Id == createDto.StudentId);
        if (!studentExists)
        {
            throw new AppServiceException(400, "考生不存在");
        }

        // 验证题目是否存在
        var questionExists = await _questionRepository.ExistsAsync(q => q.Id == createDto.QuestionId);
        if (!questionExists)
        {
            throw new AppServiceException(400, "题目不存在");
        }
    }

    /// <summary>
    /// 获取实体时包含关联实体
    /// </summary>
    protected override async Task<WrongQuestion> GetEntityForUpdate(long id, UpdateWrongQuestionDto updateDto)
    {
        WrongQuestion entity = await Repository.CreateQuery()
            .Include(x => x.Student)
            .Include(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? throw new AppServiceException(404, "错题记录不存在！") : entity;
    }

    /// <summary>
    /// 创建前的处理
    /// </summary>
    protected override async Task OnCreating(WrongQuestion entity, CreateWrongQuestionDto createDto)
    {
        await base.OnCreating(entity, createDto);

        // 加载关联实体
        entity.Student = await _studentRepository.GetByIdAsync(entity.StudentId);
        entity.Question = await _questionRepository.GetByIdAsync(entity.QuestionId);
    }
} 