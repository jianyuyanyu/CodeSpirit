using AutoMapper;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// 题目服务实现
/// </summary>
public class QuestionService : BaseCRUDIService<Question, QuestionDto, long, CreateQuestionDto, UpdateQuestionDto, QuestionBatchImportItemDto>, IQuestionService
{
    private readonly IRepository<Question> _repository;
    private readonly IRepository<QuestionCategory> _categoryRepository;
    private readonly IRepository<QuestionVersion> _versionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<QuestionService> _logger;
    private string? _changeReason;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestionService(
        IRepository<Question> repository,
        IRepository<QuestionCategory> categoryRepository,
        IRepository<QuestionVersion> versionRepository,
        IMapper mapper,
        ILogger<QuestionService> logger)
        : base(repository, mapper)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _versionRepository = versionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目分页列表
    /// </summary>
    public async Task<PageList<QuestionDto>> GetQuestionsAsync(QuestionQueryDto queryDto)
    {
        ExpressionStarter<Question> predicate = PredicateBuilder.New<Question>(true);

        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Content.Contains(queryDto.Keywords));
        }

        if (queryDto.Type.HasValue)
        {
            predicate = predicate.And(x => x.Type == queryDto.Type.Value);
        }

        if (queryDto.Difficulty.HasValue)
        {
            predicate = predicate.And(x => x.Difficulty == queryDto.Difficulty.Value);
        }

        if (queryDto.CategoryId.HasValue)
        {
            predicate = predicate.And(x => x.CategoryId == queryDto.CategoryId.Value);
        }

        if (!string.IsNullOrEmpty(queryDto.KnowledgePoint))
        {
            predicate = predicate.And(x => x.KnowledgePoints != null && x.KnowledgePoints.Contains(queryDto.KnowledgePoint));
        }

        if (!string.IsNullOrEmpty(queryDto.Tag))
        {
            predicate = predicate.And(x => x.Tags != null && x.Tags.Contains(queryDto.Tag));
        }

        return await GetPagedListAsync(
            queryDto,
            predicate,
            "Category"
        );
    }

    /// <summary>
    /// 获取题目详情
    /// </summary>
    public async Task<QuestionDto> GetQuestionAsync(long id)
    {
        return await GetAsync(id);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    public async Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto createDto)
    {
        return await CreateAsync(createDto);
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    public async Task UpdateQuestionAsync(long id, UpdateQuestionDto updateDto)
    {
        await UpdateAsync(id, updateDto);
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    public async Task DeleteQuestionAsync(long id)
    {
        // 检查题目是否存在
        var question = await _repository
            .CreateQuery()
            .Where(q => q.Id == id)
            .Include(q => q.ExamPaperQuestions)
            .FirstOrDefaultAsync();

        if (question == null)
        {
            throw new AppServiceException(404, "题目不存在！");
        }

        // 检查题目是否被试卷引用
        if (question.IsReferenced)
        {
            throw new AppServiceException(400, "该题目已被试卷引用，无法删除！");
        }

        try
        {
            await _repository.DeleteAsync(question);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除题目失败: {Id}", id);
            throw new AppServiceException(500, "删除题目失败！");
        }
    }

    /// <summary>
    /// 获取题目历史版本
    /// </summary>
    public async Task<List<QuestionVersionDto>> GetQuestionVersionsAsync(long questionId)
    {
        var versions = await _versionRepository
            .CreateQuery()
            .Where(v => v.QuestionId == questionId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();

        return _mapper.Map<List<QuestionVersionDto>>(versions);
    }

    /// <summary>
    /// 验证创建DTO
    /// </summary>
    protected override async Task ValidateCreateDto(CreateQuestionDto createDto)
    {
        // 验证分类是否存在
        var category = await _categoryRepository.GetByIdAsync(createDto.CategoryId);
        if (category == null)
        {
            throw new AppServiceException(400, "所选分类不存在！");
        }

        // 检查题目是否重复
        var existingQuestion = await _repository.CreateQuery()
            .Where(q => q.Content == createDto.Content && q.Type == createDto.Type)
            .FirstOrDefaultAsync();

        if (existingQuestion != null)
        {
            throw new AppServiceException(400, "该题目已存在！");
        }

        // 根据题目类型验证选项和答案
        ValidateOptionsAndAnswer(createDto.Type, createDto.Options, createDto.CorrectAnswer);
    }

    /// <summary>
    /// 验证更新DTO
    /// </summary>
    protected override async Task ValidateUpdateDto(long id, UpdateQuestionDto updateDto)
    {
        // 验证分类是否存在
        var category = await _categoryRepository.GetByIdAsync(updateDto.CategoryId);
        if (category == null)
        {
            throw new AppServiceException(400, "所选分类不存在！");
        }

        // 检查题目是否重复（排除自身）
        var existingQuestion = await _repository.CreateQuery()
            .Where(q => q.Id != id && q.Content == updateDto.Content && q.Type == updateDto.Type)
            .FirstOrDefaultAsync();

        if (existingQuestion != null)
        {
            throw new AppServiceException(400, "该题目已存在！");
        }

        // 根据题目类型验证选项和答案
        ValidateOptionsAndAnswer(updateDto.Type, updateDto.Options, updateDto.CorrectAnswer);
    }

    /// <summary>
    /// 创建实体前的处理
    /// </summary>
    protected override async Task OnCreating(Question entity, CreateQuestionDto createDto)
    {
        // 处理JSON序列化
        if (createDto.KnowledgePoints?.Any() == true)
        {
            entity.KnowledgePoints = JsonSerializer.Serialize(createDto.KnowledgePoints);
        }

        if (createDto.Tags?.Any() == true)
        {
            entity.Tags = JsonSerializer.Serialize(createDto.Tags);
        }

        // 设置初始版本
        entity.Version = 1;
    }

    /// <summary>
    /// 更新实体前的处理
    /// </summary>
    protected override async Task OnUpdating(Question entity, UpdateQuestionDto updateDto)
    {
        // 保存修改原因，供 OnUpdated 使用
        _changeReason = updateDto.ChangeReason;

        // 处理 JSON 序列化
        if (updateDto.KnowledgePoints?.Any() == true)
        {
            entity.KnowledgePoints = JsonSerializer.Serialize(updateDto.KnowledgePoints);
        }

        if (updateDto.Tags?.Any() == true)
        {
            entity.Tags = JsonSerializer.Serialize(updateDto.Tags);
        }
    }

    /// <summary>
    /// 更新实体后的处理
    /// </summary>
    protected override async Task OnUpdated(Question entity)
    {
        // 创建版本记录
        var version = new QuestionVersion
        {
            QuestionId = entity.Id,
            Version = entity.Version,
            Content = entity.Content,
            Options = entity.Options,
            CorrectAnswer = entity.CorrectAnswer,
            Analysis = entity.Analysis,
            KnowledgePoints = entity.KnowledgePoints,
            DefaultScore = entity.DefaultScore,
            Tags = entity.Tags,
            ChangeReason = _changeReason
        };

        await _versionRepository.AddAsync(version);
        await _versionRepository.SaveChangesAsync();

        // 增加版本号
        entity.Version++;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        // 清除修改原因
        _changeReason = null;
    }

    /// <summary>
    /// 验证选项和答案
    /// </summary>
    private void ValidateOptionsAndAnswer(QuestionType type, List<string> options, string correctAnswer)
    {
        if (!options.Any())
        {
            throw new AppServiceException(400, "题目必须包含选项！");
        }

        switch (type)
        {
            case QuestionType.SingleChoice:
                if (!options.Contains(correctAnswer))
                {
                    throw new AppServiceException(400, "正确答案必须是选项之一！");
                }
                break;

            case QuestionType.MultipleChoice:
                try
                {
                    var answers = JsonSerializer.Deserialize<List<string>>(correctAnswer);
                    if (answers == null || !answers.Any() || !answers.All(a => options.Contains(a)))
                    {
                        throw new AppServiceException(400, "所有正确答案必须是选项之一！");
                    }
                }
                catch (JsonException)
                {
                    throw new AppServiceException(400, "多选题答案格式无效！");
                }
                break;

            case QuestionType.TrueFalse:
                if (!new[] { "True", "False" }.Contains(correctAnswer))
                {
                    throw new AppServiceException(400, "判断题答案必须是True或False！");
                }
                break;

            default:
                throw new AppServiceException(400, "不支持的题目类型！");
        }
    }

    /// <summary>
    /// 批量导入题目
    /// </summary>
    public override async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<QuestionBatchImportItemDto> importData)
    {
        ArgumentNullException.ThrowIfNull(importData);

        var successCount = 0;
        var failedIds = new List<string>();
        var importList = importData.ToList();

        // 预先检查所有题目的内容是否重复
        var contents = importList.Select(x => x.Content).ToList();
        var existingContents = await _repository
            .CreateQuery()
            .Where(q => contents.Contains(q.Content))
            .Select(q => q.Content)
            .ToListAsync();

        if (existingContents.Any())
        {
            return (0, existingContents.Select(c => $"重复的题目内容: {c}").ToList());
        }

        foreach (var item in importList)
        {
            try
            {
                // 解析题目类型
                if (!Enum.TryParse<QuestionType>(item.QuestionType, out var questionType))
                {
                    failedIds.Add($"{item.Content}(无效的题目类型)");
                    continue;
                }

                // 解析难度等级
                if (!Enum.IsDefined(typeof(QuestionDifficulty), item.DifficultyLevel))
                {
                    failedIds.Add($"{item.Content}(无效的难度等级)");
                    continue;
                }

                // 创建题目实体
                var question = new Question
                {
                    Content = item.Content,
                    Type = questionType,
                    Difficulty = (QuestionDifficulty)item.DifficultyLevel,
                    CorrectAnswer = item.Answer,
                    Analysis = item.Analysis,
                    Version = 1
                };

                // 处理标签
                if (!item.Tags.IsNullOrWhiteSpace())
                {
                    question.Tags = JsonSerializer.Serialize(item.Tags);
                }

                try
                {
                    // 验证选项和答案
                    ValidateOptionsAndAnswer(questionType, new List<string>(), item.Answer);
                }
                catch (AppServiceException ex)
                {
                    failedIds.Add($"{item.Content}({ex.Message})");
                    continue;
                }

                // 添加到数据库
                await _repository.AddAsync(question);
                await _repository.SaveChangesAsync();

                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入题目失败: {Content}", item.Content);
                failedIds.Add($"{item.Content}(导入失败)");
            }
        }

        return (successCount, failedIds);
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    public override async Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var successCount = 0;
        var failedIds = new List<long>();
        var idList = ids.ToList();

        // 检查是否有题目被试卷引用
        var referencedQuestions = await _repository
            .CreateQuery()
            .Where(q => idList.Contains(q.Id))
            .Include(q => q.ExamPaperQuestions)
            .Where(q => q.IsReferenced)
            .Select(q => q.Id)
            .ToListAsync();

        if (referencedQuestions.Any())
        {
            return (0, referencedQuestions);
        }

        foreach (var id in idList)
        {
            try
            {
                await DeleteQuestionAsync(id);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除题目失败: {Id}", id);
                failedIds.Add(id);
            }
        }

        return (successCount, failedIds);
    }

    protected override string GetImportItemId(QuestionBatchImportItemDto importDto)
    {
        return importDto.Content;
    }
}