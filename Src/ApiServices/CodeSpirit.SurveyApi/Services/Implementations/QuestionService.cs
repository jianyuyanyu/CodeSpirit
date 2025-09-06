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
using Newtonsoft.Json;

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
    /// 批量排序题目（新版本，使用表格拖拽）
    /// </summary>
    /// <param name="request">排序请求</param>
    /// <returns>异步任务</returns>
    public async Task ReorderQuestionsAsync(QuestionReorderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Questions == null || !request.Questions.Any())
        {
            throw new BusinessException("题目列表不能为空");
        }

        // 获取问卷中的所有题目
        var questionIds = request.Questions.Select(q => q.Id).ToList();
        var questions = await _repository.Find(q => q.SurveyId == request.SurveyId && questionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count != request.Questions.Count)
        {
            throw new BusinessException("部分题目不存在或不属于该问卷");
        }

        // 按照集合顺序重新分配排序索引
        for (int i = 0; i < request.Questions.Count; i++)
        {
            var sortItem = request.Questions[i];
            var question = questions.First(q => q.Id == sortItem.Id);
            question.OrderIndex = i + 1; // 从1开始编号
            await _repository.UpdateAsync(question, false);
        }

        await _repository.SaveChangesAsync();

        _logger.LogInformation("问卷 {SurveyId} 的题目排序已更新，按集合顺序重新分配了 {Count} 个题目的排序索引", request.SurveyId, request.Questions.Count);
    }

    /// <summary>
    /// 获取问卷的题目排序列表（简化版本）
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>题目排序列表</returns>
    public async Task<List<QuestionSortDto>> GetQuestionSortListAsync(int surveyId)
    {
        var questions = await _repository.Find(q => q.SurveyId == surveyId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        return _mapper.Map<List<QuestionSortDto>>(questions);
    }

    /// <summary>
    /// 创建题目（重写以处理选项保存和题型特定字段）
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

        // 处理题型特定设置
        question.Settings = BuildQuestionSettings(createDto);

        // 保存题目
        var createdQuestion = await _repository.AddAsync(question);

        // 保存选项
        await SaveQuestionOptionsAsync(createdQuestion.Id, createDto.Options, createDto.Type);

        // 处理矩阵题的特殊选项
        await SaveMatrixOptionsAsync(createdQuestion.Id, createDto);

        _logger.LogInformation("题目创建成功：{Title}，ID：{QuestionId}，类型：{Type}，选项数量：{OptionCount}",
            createdQuestion.Title, createdQuestion.Id, createDto.Type, createDto.Options?.Count ?? 0);

        // 重新查询以获取完整数据
        var questionWithOptions = await _repository.Find(q => q.Id == createdQuestion.Id)
            .Include(q => q.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync();

        return _mapper.Map<QuestionDto>(questionWithOptions ?? createdQuestion);
    }

    /// <summary>
    /// 更新题目（重写以处理选项更新和题型特定字段）
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateDto">更新题目DTO</param>
    /// <returns>异步任务</returns>
    public override async Task UpdateAsync(int id, UpdateQuestionDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        // 获取现有题目
        var existingQuestion = await _repository.Find(q => q.Id == id)
            .Include(q => q.Options)
            .FirstOrDefaultAsync();

        if (existingQuestion == null)
        {
            throw new BusinessException("题目不存在");
        }

        // 验证更新数据
        await ValidateUpdateDto(id, updateDto);

        // 更新基本字段
        _mapper.Map(updateDto, existingQuestion);

        // 处理题型特定设置
        existingQuestion.Settings = BuildQuestionSettings(updateDto);

        // 更新题目
        await _repository.UpdateAsync(existingQuestion, false);

        // 更新选项
        await UpdateQuestionOptionsAsync(id, updateDto.Options, updateDto.Type);

        // 处理矩阵题的特殊选项
        await UpdateMatrixOptionsAsync(id, updateDto);

        await _repository.SaveChangesAsync();

        _logger.LogInformation("题目更新成功：{Title}，ID：{QuestionId}，类型：{Type}",
            updateDto.Title, id, updateDto.Type);
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

        var errors = ValidateQuestionCommon(createDto.Type, createDto.Options?.Cast<object>().ToList());

        // 验证题型特定字段
        errors.AddRange(ValidateQuestionTypeSpecificFields(createDto));

        if (errors.Any())
        {
            throw new BusinessException($"题目验证失败：{string.Join("；", errors)}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 验证更新题目DTO
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateDto">更新题目DTO</param>
    /// <returns>异步任务</returns>
    protected override Task ValidateUpdateDto(int id, UpdateQuestionDto updateDto)
    {
        _logger.LogInformation("开始验证更新题目：{Title}，类型：{Type}，选项数量：{OptionCount}",
            updateDto.Title, updateDto.Type, updateDto.Options?.Count ?? 0);

        var errors = ValidateQuestionCommon(updateDto.Type, updateDto.Options?.Cast<object>().ToList());

        // 验证题型特定字段
        errors.AddRange(ValidateQuestionTypeSpecificFields(updateDto));

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

    #region 题型特定处理方法

    /// <summary>
    /// 构建题目设置JSON
    /// </summary>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>设置JSON字符串</returns>
    private string? BuildQuestionSettings(CreateQuestionDto createDto)
    {
        var settings = new Dictionary<string, object>();

        switch (createDto.Type)
        {
            case QuestionType.Rating:
                if (createDto.RatingMin.HasValue) settings["ratingMin"] = createDto.RatingMin.Value;
                if (createDto.RatingMax.HasValue) settings["ratingMax"] = createDto.RatingMax.Value;
                if (createDto.RatingStep.HasValue) settings["ratingStep"] = createDto.RatingStep.Value;
                break;

            case QuestionType.Number:
                if (createDto.NumberMin.HasValue) settings["numberMin"] = createDto.NumberMin.Value;
                if (createDto.NumberMax.HasValue) settings["numberMax"] = createDto.NumberMax.Value;
                if (createDto.NumberStep.HasValue) settings["numberStep"] = createDto.NumberStep.Value;
                break;

            case QuestionType.Text:
            case QuestionType.Textarea:
                if (createDto.TextMinLength.HasValue) settings["textMinLength"] = createDto.TextMinLength.Value;
                if (createDto.TextMaxLength.HasValue) settings["textMaxLength"] = createDto.TextMaxLength.Value;
                if (!string.IsNullOrEmpty(createDto.TextInputMode)) settings["textInputMode"] = createDto.TextInputMode;
                break;

            case QuestionType.Date:
            case QuestionType.DateTime:
                if (!string.IsNullOrEmpty(createDto.DateFormat)) settings["dateFormat"] = createDto.DateFormat;
                if (createDto.Type == QuestionType.DateTime && !string.IsNullOrEmpty(createDto.TimeFormat))
                    settings["timeFormat"] = createDto.TimeFormat;
                break;

            case QuestionType.Time:
                if (!string.IsNullOrEmpty(createDto.TimeFormat)) settings["timeFormat"] = createDto.TimeFormat;
                break;

            case QuestionType.Matrix:
                if (createDto.MatrixRows?.Any() == true) settings["matrixRows"] = createDto.MatrixRows;
                if (createDto.MatrixColumns?.Any() == true) settings["matrixColumns"] = createDto.MatrixColumns;
                break;
        }

        return settings.Any() ? JsonConvert.SerializeObject(settings) : null;
    }

    /// <summary>
    /// 构建题目设置JSON（更新版本）
    /// </summary>
    /// <param name="updateDto">更新题目DTO</param>
    /// <returns>设置JSON字符串</returns>
    private string? BuildQuestionSettings(UpdateQuestionDto updateDto)
    {
        var settings = new Dictionary<string, object>();

        switch (updateDto.Type)
        {
            case QuestionType.Rating:
                if (updateDto.RatingMin.HasValue) settings["ratingMin"] = updateDto.RatingMin.Value;
                if (updateDto.RatingMax.HasValue) settings["ratingMax"] = updateDto.RatingMax.Value;
                if (updateDto.RatingStep.HasValue) settings["ratingStep"] = updateDto.RatingStep.Value;
                break;

            case QuestionType.Number:
                if (updateDto.NumberMin.HasValue) settings["numberMin"] = updateDto.NumberMin.Value;
                if (updateDto.NumberMax.HasValue) settings["numberMax"] = updateDto.NumberMax.Value;
                if (updateDto.NumberStep.HasValue) settings["numberStep"] = updateDto.NumberStep.Value;
                break;

            case QuestionType.Text:
            case QuestionType.Textarea:
                if (updateDto.TextMinLength.HasValue) settings["textMinLength"] = updateDto.TextMinLength.Value;
                if (updateDto.TextMaxLength.HasValue) settings["textMaxLength"] = updateDto.TextMaxLength.Value;
                if (!string.IsNullOrEmpty(updateDto.TextInputMode)) settings["textInputMode"] = updateDto.TextInputMode;
                break;

            case QuestionType.Date:
            case QuestionType.DateTime:
                if (!string.IsNullOrEmpty(updateDto.DateFormat)) settings["dateFormat"] = updateDto.DateFormat;
                if (updateDto.Type == QuestionType.DateTime && !string.IsNullOrEmpty(updateDto.TimeFormat))
                    settings["timeFormat"] = updateDto.TimeFormat;
                break;

            case QuestionType.Time:
                if (!string.IsNullOrEmpty(updateDto.TimeFormat)) settings["timeFormat"] = updateDto.TimeFormat;
                break;

            case QuestionType.Matrix:
                if (updateDto.MatrixRows?.Any() == true) settings["matrixRows"] = updateDto.MatrixRows;
                if (updateDto.MatrixColumns?.Any() == true) settings["matrixColumns"] = updateDto.MatrixColumns;
                break;
        }

        return settings.Any() ? JsonConvert.SerializeObject(settings) : null;
    }

    /// <summary>
    /// 保存题目选项
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="options">选项列表</param>
    /// <param name="questionType">题目类型</param>
    /// <returns>异步任务</returns>
    private async Task SaveQuestionOptionsAsync(int questionId, List<CreateQuestionOptionDto>? options, QuestionType questionType)
    {
        if (options?.Any() != true || !RequiresOptions(questionType))
            return;

        // 根据集合顺序自动设置OrderIndex
        for (int i = 0; i < options.Count; i++)
        {
            var optionDto = options[i];
            var option = new QuestionOption
            {
                QuestionId = questionId,
                Text = optionDto.Text,
                Value = optionDto.Value ?? optionDto.Text,
                OrderIndex = i, // 使用集合索引作为排序索引
                IsOther = optionDto.IsOther
            };

            await _optionRepository.AddAsync(option, false);
        }

        await _optionRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 保存矩阵题的特殊选项
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>异步任务</returns>
    private async Task SaveMatrixOptionsAsync(int questionId, CreateQuestionDto createDto)
    {
        if (createDto.Type != QuestionType.Matrix)
            return;

        // 矩阵题的行选项作为普通选项保存，根据集合顺序自动设置OrderIndex
        if (createDto.MatrixRows?.Any() == true)
        {
            for (int i = 0; i < createDto.MatrixRows.Count; i++)
            {
                var row = createDto.MatrixRows[i];
                var option = new QuestionOption
                {
                    QuestionId = questionId,
                    Text = row,
                    Value = row,
                    OrderIndex = i, // 使用集合索引作为排序索引
                    IsOther = false
                };

                await _optionRepository.AddAsync(option, false);
            }

            await _optionRepository.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 更新题目选项
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="options">选项列表</param>
    /// <param name="questionType">题目类型</param>
    /// <returns>异步任务</returns>
    private async Task UpdateQuestionOptionsAsync(int questionId, List<UpdateQuestionOptionDto>? options, QuestionType questionType)
    {
        // 获取现有选项
        var existingOptions = await _optionRepository.Find(o => o.QuestionId == questionId).ToListAsync();

        // 删除所有现有选项
        foreach (var existingOption in existingOptions)
        {
            await _optionRepository.DeleteAsync(existingOption, false);
        }

        // 添加新选项
        if (options?.Any() == true && RequiresOptions(questionType))
        {
            // 根据集合顺序自动设置OrderIndex
            for (int i = 0; i < options.Count; i++)
            {
                var optionDto = options[i];
                var option = new QuestionOption
                {
                    QuestionId = questionId,
                    Text = optionDto.Text,
                    Value = optionDto.Value ?? optionDto.Text,
                    OrderIndex = i, // 使用集合索引作为排序索引
                    IsOther = optionDto.IsOther
                };

                await _optionRepository.AddAsync(option, false);
            }
        }

        await _optionRepository.SaveChangesAsync();
    }

    /// <summary>
    /// 更新矩阵题的特殊选项
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="updateDto">更新题目DTO</param>
    /// <returns>异步任务</returns>
    private Task UpdateMatrixOptionsAsync(int questionId, UpdateQuestionDto updateDto)
    {
        if (updateDto.Type != QuestionType.Matrix)
            return Task.CompletedTask;

        // 矩阵题的行选项处理已在UpdateQuestionOptionsAsync中处理
        // 这里可以添加额外的矩阵题特殊逻辑
        return Task.CompletedTask;
    }

    /// <summary>
    /// 验证题目通用规则
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <param name="options">选项列表</param>
    /// <returns>错误列表</returns>
    private List<string> ValidateQuestionCommon(QuestionType questionType, List<object>? options)
    {
        var errors = new List<string>();

        // 验证选择题必须有选项
        if (IsChoiceQuestion(questionType))
        {
            if (options == null || !options.Any())
            {
                errors.Add($"{GetQuestionTypeDisplayName(questionType)}必须至少包含一个选项");
            }
            else if (options.Count < 2)
            {
                errors.Add($"{GetQuestionTypeDisplayName(questionType)}至少需要2个选项");
            }
            else
            {
                // 验证选项文本不能为空
                var emptyOptions = options.Where(o => 
                {
                    var text = GetOptionText(o);
                    return string.IsNullOrWhiteSpace(text);
                }).ToList();

                if (emptyOptions.Any())
                {
                    errors.Add("选项文本不能为空");
                }

                // 验证选项文本不能重复
                var optionTexts = options.Select(GetOptionText).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                var duplicateOptions = optionTexts.GroupBy(t => t!.Trim())
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
        if (questionType == QuestionType.Matrix)
        {
            if (options == null || options.Count < 2)
            {
                errors.Add("矩阵题至少需要2个行选项");
            }
        }

        // 验证排序题特殊要求
        if (questionType == QuestionType.Ranking)
        {
            if (options == null || options.Count < 2)
            {
                errors.Add("排序题至少需要2个选项");
            }
        }

        return errors;
    }

    /// <summary>
    /// 验证题型特定字段
    /// </summary>
    /// <param name="dto">题目DTO</param>
    /// <returns>错误列表</returns>
    private List<string> ValidateQuestionTypeSpecificFields(object dto)
    {
        var errors = new List<string>();

        switch (dto)
        {
            case CreateQuestionDto createDto:
                errors.AddRange(ValidateRatingFields(createDto.Type, createDto.RatingMin, createDto.RatingMax, createDto.RatingStep));
                errors.AddRange(ValidateNumberFields(createDto.Type, createDto.NumberMin, createDto.NumberMax, createDto.NumberStep));
                errors.AddRange(ValidateTextFields(createDto.Type, createDto.TextMinLength, createDto.TextMaxLength));
                errors.AddRange(ValidateMatrixFields(createDto.Type, createDto.MatrixRows, createDto.MatrixColumns));
                break;

            case UpdateQuestionDto updateDto:
                errors.AddRange(ValidateRatingFields(updateDto.Type, updateDto.RatingMin, updateDto.RatingMax, updateDto.RatingStep));
                errors.AddRange(ValidateNumberFields(updateDto.Type, updateDto.NumberMin, updateDto.NumberMax, updateDto.NumberStep));
                errors.AddRange(ValidateTextFields(updateDto.Type, updateDto.TextMinLength, updateDto.TextMaxLength));
                errors.AddRange(ValidateMatrixFields(updateDto.Type, updateDto.MatrixRows, updateDto.MatrixColumns));
                break;
        }

        return errors;
    }

    /// <summary>
    /// 验证评分题字段
    /// </summary>
    private List<string> ValidateRatingFields(QuestionType type, int? min, int? max, double? step)
    {
        var errors = new List<string>();

        if (type != QuestionType.Rating)
            return errors;

        if (min.HasValue && max.HasValue && min.Value >= max.Value)
        {
            errors.Add("评分最小值必须小于最大值");
        }

        if (min.HasValue && (min.Value < 1 || min.Value > 10))
        {
            errors.Add("评分最小值必须在1-10之间");
        }

        if (max.HasValue && (max.Value < 1 || max.Value > 10))
        {
            errors.Add("评分最大值必须在1-10之间");
        }

        if (step.HasValue && (step.Value <= 0 || step.Value > 1))
        {
            errors.Add("评分步长必须在0-1之间");
        }

        return errors;
    }

    /// <summary>
    /// 验证数字题字段
    /// </summary>
    private List<string> ValidateNumberFields(QuestionType type, double? min, double? max, double? step)
    {
        var errors = new List<string>();

        if (type != QuestionType.Number)
            return errors;

        if (min.HasValue && max.HasValue && min.Value >= max.Value)
        {
            errors.Add("数字最小值必须小于最大值");
        }

        if (step.HasValue && step.Value <= 0)
        {
            errors.Add("数字步长必须大于0");
        }

        return errors;
    }

    /// <summary>
    /// 验证文本题字段
    /// </summary>
    private List<string> ValidateTextFields(QuestionType type, int? minLength, int? maxLength)
    {
        var errors = new List<string>();

        if (type != QuestionType.Text && type != QuestionType.Textarea)
            return errors;

        if (minLength.HasValue && maxLength.HasValue && minLength.Value >= maxLength.Value)
        {
            errors.Add("文本最小长度必须小于最大长度");
        }

        if (minLength.HasValue && minLength.Value < 0)
        {
            errors.Add("文本最小长度不能小于0");
        }

        if (maxLength.HasValue && maxLength.Value <= 0)
        {
            errors.Add("文本最大长度必须大于0");
        }

        return errors;
    }

    /// <summary>
    /// 验证矩阵题字段
    /// </summary>
    private List<string> ValidateMatrixFields(QuestionType type, List<string>? rows, List<string>? columns)
    {
        var errors = new List<string>();

        if (type != QuestionType.Matrix)
            return errors;

        if (rows == null || rows.Count < 2)
        {
            errors.Add("矩阵题至少需要2个行选项");
        }
        else if (rows.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("矩阵题行选项不能为空");
        }

        if (columns == null || columns.Count < 2)
        {
            errors.Add("矩阵题至少需要2个列选项");
        }
        else if (columns.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("矩阵题列选项不能为空");
        }

        return errors;
    }

    /// <summary>
    /// 获取选项文本
    /// </summary>
    private string? GetOptionText(object option)
    {
        return option switch
        {
            CreateQuestionOptionDto createOption => createOption.Text,
            UpdateQuestionOptionDto updateOption => updateOption.Text,
            _ => null
        };
    }

    /// <summary>
    /// 判断题型是否需要选项
    /// </summary>
    private static bool RequiresOptions(QuestionType questionType)
    {
        return questionType == QuestionType.SingleChoice ||
               questionType == QuestionType.MultipleChoice ||
               questionType == QuestionType.Ranking ||
               questionType == QuestionType.Matrix;
    }

    #endregion
}
