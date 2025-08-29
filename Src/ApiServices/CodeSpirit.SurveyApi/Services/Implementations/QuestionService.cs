using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 题目服务实现
/// </summary>
public class QuestionService : BaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>, IQuestionService, IScopedDependency
{
    private readonly IRepository<Question> _repository;
    private readonly IRepository<QuestionOption> _optionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<QuestionService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">题目仓储</param>
    /// <param name="optionRepository">选项仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    public QuestionService(
        IRepository<Question> repository,
        IRepository<QuestionOption> optionRepository,
        IMapper mapper,
        ILogger<QuestionService> logger)
        : base(repository, mapper)
    {
        _repository = repository;
        _optionRepository = optionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 根据问卷ID获取题目列表
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>题目列表</returns>
    public Task<List<QuestionDto>> GetQuestionsBySurveyIdAsync(int surveyId)
    {
        var questions = _repository.Find(q => q.SurveyId == surveyId)
            .Include(q => q.Options)
            .OrderBy(q => q.OrderIndex)
            .ToList();

        return Task.FromResult(_mapper.Map<List<QuestionDto>>(questions));
    }

    /// <summary>
    /// 获取题目分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目分页列表</returns>
    public async Task<PageList<QuestionDto>> GetQuestionsAsync(QuestionQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Question>(true);

        // 问卷ID筛选
        if (queryDto.SurveyId.HasValue)
        {
            predicate = predicate.And(x => x.SurveyId == queryDto.SurveyId.Value);
        }

        // 关键词搜索（标题）
        if (!string.IsNullOrEmpty(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Title.Contains(queryDto.Keywords) ||
                                          (x.Description != null && x.Description.Contains(queryDto.Keywords)));
        }

        // 题目标题搜索
        if (!string.IsNullOrEmpty(queryDto.Title))
        {
            predicate = predicate.And(x => x.Title.Contains(queryDto.Title));
        }

        // 题目类型筛选
        if (queryDto.Type.HasValue)
        {
            predicate = predicate.And(x => x.Type == queryDto.Type.Value);
        }

        // 是否必填筛选
        if (queryDto.IsRequired.HasValue)
        {
            predicate = predicate.And(x => x.IsRequired == queryDto.IsRequired.Value);
        }

        // LLM生成筛选
        if (queryDto.LLMGenerated.HasValue)
        {
            predicate = predicate.And(x => x.LLMGenerated == queryDto.LLMGenerated.Value);
        }

        // 创建者筛选
        if (queryDto.CreatedBy.HasValue)
        {
            predicate = predicate.And(x => x.CreatedBy == queryDto.CreatedBy.Value);
        }

        // 时间范围筛选
        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt >= queryDto.StartTime.Value);
        }

        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt <= queryDto.EndTime.Value);
        }

        // 排序索引范围筛选
        if (queryDto.MinOrderIndex.HasValue)
        {
            predicate = predicate.And(x => x.OrderIndex >= queryDto.MinOrderIndex.Value);
        }

        if (queryDto.MaxOrderIndex.HasValue)
        {
            predicate = predicate.And(x => x.OrderIndex <= queryDto.MaxOrderIndex.Value);
        }

        // 构建查询
        var baseQuery = _repository.Find(predicate);

        // 排序
        IQueryable<Question> orderedQuery;
        if (!string.IsNullOrEmpty(queryDto.OrderBy))
        {
            switch (queryDto.OrderBy.ToLower())
            {
                case "title":
                    orderedQuery = queryDto.OrderDir?.ToLower() == "asc"
                        ? baseQuery.OrderBy(x => x.Title)
                        : baseQuery.OrderByDescending(x => x.Title);
                    break;
                case "type":
                    orderedQuery = queryDto.OrderDir?.ToLower() == "asc"
                        ? baseQuery.OrderBy(x => x.Type)
                        : baseQuery.OrderByDescending(x => x.Type);
                    break;
                case "orderindex":
                    orderedQuery = queryDto.OrderDir?.ToLower() == "asc"
                        ? baseQuery.OrderBy(x => x.OrderIndex)
                        : baseQuery.OrderByDescending(x => x.OrderIndex);
                    break;
                case "createdat":
                    orderedQuery = queryDto.OrderDir?.ToLower() == "asc"
                        ? baseQuery.OrderBy(x => x.CreatedAt)
                        : baseQuery.OrderByDescending(x => x.CreatedAt);
                    break;
                default:
                    orderedQuery = baseQuery.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id);
                    break;
            }
        }
        else
        {
            // 默认按排序索引和ID排序
            orderedQuery = baseQuery.OrderBy(x => x.OrderIndex).ThenBy(x => x.Id);
        }

        // 包含选项数据
        var queryWithOptions = orderedQuery.Include(x => x.Options);

        // 分页
        var totalCount = await queryWithOptions.CountAsync();
        var items = await queryWithOptions
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        var questionDtos = _mapper.Map<List<QuestionDto>>(items);

        return new PageList<QuestionDto>(questionDtos, totalCount);
    }

    /// <summary>
    /// 批量排序题目
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="questionOrders">题目排序信息</param>
    /// <returns>异步任务</returns>
    public async Task ReorderQuestionsAsync(int surveyId, Dictionary<int, int> questionOrders)
    {
        var questions = _repository.Find(q => q.SurveyId == surveyId).ToList();

        foreach (var question in questions)
        {
            if (questionOrders.TryGetValue(question.Id, out var newOrder))
            {
                question.OrderIndex = newOrder;
            }
        }

        foreach (var question in questions)
        {
            await _repository.UpdateAsync(question, false);
        }

        await _repository.SaveChangesAsync();

        _logger.LogInformation("问卷 {SurveyId} 的题目排序已更新", surveyId);
    }

    /// <summary>
    /// 创建题目（重写以处理选项保存）
    /// </summary>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>创建的题目DTO</returns>
    public override async Task<QuestionDto> CreateAsync(CreateQuestionDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        // 先进行验证
        await ValidateCreateDto(createDto);

        // 创建题目实体
        var question = _mapper.Map<Question>(createDto);

        // 保存题目
        var createdQuestion = await _repository.AddAsync(question);

        // 保存选项
        if (createDto.Options?.Any() == true)
        {
            foreach (var optionDto in createDto.Options)
            {
                var option = new QuestionOption
                {
                    QuestionId = createdQuestion.Id,
                    Text = optionDto.Text,
                    Value = optionDto.Value ?? optionDto.Text,
                    OrderIndex = optionDto.OrderIndex,
                    IsOther = optionDto.IsOther
                };

                await _optionRepository.AddAsync(option, false);
            }

            await _optionRepository.SaveChangesAsync();
        }

        _logger.LogInformation("题目创建成功：{Title}，ID：{QuestionId}，选项数量：{OptionCount}",
            createdQuestion.Title, createdQuestion.Id, createDto.Options?.Count ?? 0);

        return _mapper.Map<QuestionDto>(createdQuestion);
    }

    /// <summary>
    /// 验证创建题目DTO
    /// </summary>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>异步任务</returns>
    protected override Task ValidateCreateDto(CreateQuestionDto createDto)
    {
        _logger.LogInformation("开始验证题目：{Title}，类型：{Type}，选项数量：{OptionCount}",
            createDto.Title, createDto.Type, createDto.Options?.Count ?? 0);

        var errors = new List<string>();

        // 验证选择题必须有选项
        if (IsChoiceQuestion(createDto.Type))
        {
            if (createDto.Options == null || !createDto.Options.Any())
            {
                errors.Add($"{GetQuestionTypeDisplayName(createDto.Type)}必须至少包含一个选项");
            }
            else if (createDto.Options.Count < 2)
            {
                errors.Add($"{GetQuestionTypeDisplayName(createDto.Type)}至少需要2个选项");
            }
            else
            {
                // 验证选项文本不能为空
                var emptyOptions = createDto.Options.Where(o => string.IsNullOrWhiteSpace(o.Text)).ToList();
                if (emptyOptions.Any())
                {
                    errors.Add("选项文本不能为空");
                }

                // 验证选项文本不能重复
                var duplicateOptions = createDto.Options.GroupBy(o => o.Text.Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateOptions.Any())
                {
                    errors.Add($"选项文本不能重复：{string.Join(", ", duplicateOptions)}");
                }
            }
        }

        // 验证矩阵题特殊要求
        if (createDto.Type == QuestionType.Matrix)
        {
            if (createDto.Options == null || createDto.Options.Count < 2)
            {
                errors.Add("矩阵题至少需要2个行选项");
            }
            // 矩阵题还需要列选项，这里可以通过Settings配置
            if (string.IsNullOrWhiteSpace(createDto.Settings))
            {
                errors.Add("矩阵题需要配置列选项设置");
            }
        }

        // 验证排序题特殊要求
        if (createDto.Type == QuestionType.Ranking)
        {
            if (createDto.Options == null || createDto.Options.Count < 2)
            {
                errors.Add("排序题至少需要2个选项");
            }
        }

        // 验证评分题特殊要求
        if (createDto.Type == QuestionType.Rating)
        {
            // 评分题可以有选项（自定义评分标签），也可以没有（使用默认数字评分）
            // 这里不强制要求选项，但如果有选项则验证
            if (createDto.Options != null && createDto.Options.Any())
            {
                var emptyOptions = createDto.Options.Where(o => string.IsNullOrWhiteSpace(o.Text)).ToList();
                if (emptyOptions.Any())
                {
                    errors.Add("评分题的评分标签不能为空");
                }
            }
        }

        if (errors.Any())
        {
            throw new BusinessException($"题目验证失败：{string.Join("；", errors)}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 判断是否为选择题类型
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <returns>是否为选择题</returns>
    private static bool IsChoiceQuestion(QuestionType questionType)
    {
        return questionType == QuestionType.SingleChoice ||
               questionType == QuestionType.MultipleChoice;
    }

    /// <summary>
    /// 获取题目类型显示名称
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <returns>显示名称</returns>
    private static string GetQuestionTypeDisplayName(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.SingleChoice => "单选题",
            QuestionType.MultipleChoice => "多选题",
            QuestionType.Text => "填空题",
            QuestionType.Number => "数字题",
            QuestionType.Rating => "评分题",
            QuestionType.Date => "日期题",
            QuestionType.Time => "时间题",
            QuestionType.DateTime => "日期时间题",
            QuestionType.Textarea => "长文本题",
            QuestionType.Matrix => "矩阵题",
            QuestionType.Ranking => "排序题",
            _ => questionType.ToString()
        };
    }

    /// <summary>
    /// 复制题目到指定问卷
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetSurveyId">目标问卷ID</param>
    /// <returns>复制的题目</returns>
    public async Task<QuestionDto> CopyQuestionToSurveyAsync(int questionId, int targetSurveyId)
    {
        return await CopyQuestionToSurveyAsync(questionId, targetSurveyId, null);
    }

    /// <summary>
    /// 复制题目到指定问卷（带自定义标题）
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetSurveyId">目标问卷ID</param>
    /// <param name="newTitle">新标题</param>
    /// <returns>复制的题目</returns>
    public async Task<QuestionDto> CopyQuestionToSurveyAsync(int questionId, int targetSurveyId, string? newTitle)
    {
        var sourceQuestion = await _repository.GetByIdAsync(questionId);
        if (sourceQuestion == null)
        {
            throw new BusinessException("源题目不存在");
        }

        // 获取目标问卷的最大排序索引
        var maxOrderIndex = _repository.Find(q => q.SurveyId == targetSurveyId)
            .Max(q => (int?)q.OrderIndex) ?? 0;

        // 获取源题目的选项
        var sourceOptions = _optionRepository.Find(o => o.QuestionId == questionId).ToList();

        // 创建新题目
        var newQuestion = new Question
        {
            SurveyId = targetSurveyId,
            Title = newTitle ?? $"{sourceQuestion.Title} (副本)",
            Description = sourceQuestion.Description,
            Type = sourceQuestion.Type,
            OrderIndex = maxOrderIndex + 1,
            IsRequired = sourceQuestion.IsRequired,
            Validation = sourceQuestion.Validation,
            Settings = sourceQuestion.Settings,
            LLMGenerated = sourceQuestion.LLMGenerated
        };

        var createdQuestion = await _repository.AddAsync(newQuestion);

        // 复制选项
        var copiedOptions = new List<QuestionOption>();
        foreach (var sourceOption in sourceOptions)
        {
            var newOption = new QuestionOption
            {
                QuestionId = createdQuestion.Id,
                Text = sourceOption.Text,
                Value = sourceOption.Value,
                OrderIndex = sourceOption.OrderIndex,
                IsOther = sourceOption.IsOther
            };

            var addedOption = await _optionRepository.AddAsync(newOption, false);
            copiedOptions.Add(addedOption);
        }

        await _optionRepository.SaveChangesAsync();

        _logger.LogInformation("题目 {SourceQuestionId} 已复制到问卷 {TargetSurveyId}，新题目ID：{NewQuestionId}",
            questionId, targetSurveyId, createdQuestion.Id);

        // 构建返回DTO，包含选项
        var result = _mapper.Map<QuestionDto>(createdQuestion);
        result.Options = _mapper.Map<List<QuestionOptionDto>>(copiedOptions);

        return result;
    }
}
