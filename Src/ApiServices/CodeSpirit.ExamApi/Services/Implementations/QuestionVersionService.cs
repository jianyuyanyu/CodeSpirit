using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 题目版本服务实现
/// </summary>
public class QuestionVersionService : BaseCRUDService<QuestionVersion, QuestionVersionDto, long, CreateQuestionVersionDto, UpdateQuestionVersionDto>, IQuestionVersionService, IScopedDependency
{
    private readonly IRepository<Question> _questionRepository;
    private readonly ILogger<QuestionVersionService> _logger;
    private readonly IExamDataScopeService _dataScopeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionVersionService(
        IRepository<QuestionVersion> repository,
        IRepository<Question> questionRepository,
        IMapper mapper,
        ILogger<QuestionVersionService> logger,
        IExamDataScopeService dataScopeService)
        : base(repository, mapper)
    {
        _questionRepository = questionRepository;
        _logger = logger;
        _dataScopeService = dataScopeService;
    }

    /// <summary>
    /// 获取题目版本分页列表
    /// </summary>
    public async Task<PageList<QuestionVersionDto>> GetQuestionVersionsAsync(QuestionVersionQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<QuestionVersion>(true);

        // 数据可见性过滤：非 Admin/exam_view_all 用户仅能查看自己创建的题目版本
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return new PageList<QuestionVersionDto>([], 0);
            }
            predicate = predicate.And(x => x.CreatedBy == userId.Value);
        }

        if (queryDto.QuestionId.HasValue)
        {
            predicate = predicate.And(x => x.QuestionId == queryDto.QuestionId.Value);
        }

        if (queryDto.Version.HasValue)
        {
            predicate = predicate.And(x => x.Version == queryDto.Version.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.Tags))
        {
            predicate = predicate.And(x => x.Tags != null && x.Tags.Contains(queryDto.Tags));
        }

        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt >= queryDto.StartTime.Value);
        }

        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt <= queryDto.EndTime.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.And(x =>
                x.Content.Contains(queryDto.Keywords) ||
                (x.Analysis != null && x.Analysis.Contains(queryDto.Keywords)) ||
                (x.ChangeReason != null && x.ChangeReason.Contains(queryDto.Keywords))
            );
        }

        return await GetPagedListAsync(
            queryDto,
            predicate,
            "Question"
        );
    }

    /// <summary>
    /// 获取题目的所有版本
    /// </summary>
    public async Task<List<QuestionVersionDto>> GetVersionsByQuestionIdAsync(long questionId)
    {
        var predicate = PredicateBuilder.New<QuestionVersion>(true);
        predicate = predicate.And(x => x.QuestionId == questionId);

        // 数据可见性过滤
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return [];
            }
            predicate = predicate.And(x => x.CreatedBy == userId.Value);
        }

        var versions = await Repository.Find(predicate)
            .Include(x => x.Question)
            .OrderByDescending(x => x.Version)
            .ToListAsync();
        return Mapper.Map<List<QuestionVersionDto>>(versions);
    }

    /// <summary>
    /// 获取题目的特定版本
    /// </summary>
    public async Task<QuestionVersionDto> GetQuestionVersionAsync(long questionId, int version)
    {
        var questionVersion = await Repository.CreateQuery()
            .Where(x => x.QuestionId == questionId && x.Version == version)
            .Include(x => x.Question)
            .FirstOrDefaultAsync();

        if (questionVersion == null)
        {
            throw new AppServiceException(404, "未找到指定的题目版本");
        }

        // 数据可见性校验
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || questionVersion.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "未找到指定的题目版本");
            }
        }

        return Mapper.Map<QuestionVersionDto>(questionVersion);
    }

    /// <summary>
    /// 创建新版本
    /// </summary>
    public async Task<QuestionVersionDto> CreateNewVersionAsync(CreateQuestionVersionDto createDto)
    {
        // 验证题目是否存在
        var question = await _questionRepository.GetByIdAsync(createDto.QuestionId);
        if (question == null)
        {
            throw new AppServiceException(400, "题目不存在");
        }

        // 关联数据可见性校验：非 Admin/exam_view_all 用户只能为自己创建的题目创建新版本
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || question.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "题目不存在");
            }
        }

        // 获取当前最高版本号
        int currentVersion = await Repository.CreateQuery()
            .Where(x => x.QuestionId == createDto.QuestionId)
            .MaxAsync(x => (int?)x.Version) ?? 0;

        // 创建新版本
        var entity = Mapper.Map<QuestionVersion>(createDto);
        entity.Version = currentVersion + 1;
        entity.Question = question;

        await Repository.AddAsync(entity);

        return Mapper.Map<QuestionVersionDto>(entity);
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateQuestionVersionDto createDto)
    {
        await base.ValidateCreateDto(createDto);

        // 验证题目是否存在
        var questionExists = await _questionRepository.ExistsAsync(q => q.Id == createDto.QuestionId);
        if (!questionExists)
        {
            throw new AppServiceException(400, "题目不存在");
        }
    }

    /// <summary>
    /// 创建前的处理
    /// </summary>
    protected override async Task OnCreating(QuestionVersion entity, CreateQuestionVersionDto createDto)
    {
        await base.OnCreating(entity, createDto);

        // 获取当前最高版本号
        int currentVersion = await Repository.CreateQuery()
            .Where(x => x.QuestionId == createDto.QuestionId)
            .MaxAsync(x => (int?)x.Version) ?? 0;

        // 设置新的版本号
        entity.Version = currentVersion + 1;

        // 加载关联实体
        entity.Question = await _questionRepository.GetByIdAsync(entity.QuestionId);
    }

    /// <summary>
    /// 获取实体时包含关联实体
    /// </summary>
    protected override async Task<QuestionVersion> GetEntityForUpdate(long id, UpdateQuestionVersionDto updateDto)
    {
        var entity = await Repository.CreateQuery()
            .Include(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            throw new AppServiceException(404, "题目版本不存在！");
        }

        // 数据可见性校验
        if (!await _dataScopeService.CanViewAllExamDataAsync())
        {
            var userId = _dataScopeService.GetCurrentUserId();
            if (!userId.HasValue || entity.CreatedBy != userId.Value)
            {
                throw new AppServiceException(404, "题目版本不存在！");
            }
        }

        return entity;
    }
} 