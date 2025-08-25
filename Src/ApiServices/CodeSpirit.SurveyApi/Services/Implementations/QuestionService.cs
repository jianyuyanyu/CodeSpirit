using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;

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
            .OrderBy(q => q.OrderIndex)
            .ToList();
        
        return Task.FromResult(_mapper.Map<List<QuestionDto>>(questions));
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
    /// 复制题目到指定问卷
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetSurveyId">目标问卷ID</param>
    /// <returns>复制的题目</returns>
    public async Task<QuestionDto> CopyQuestionToSurveyAsync(int questionId, int targetSurveyId)
    {
        var sourceQuestion = await _repository.GetByIdAsync(questionId);
        if (sourceQuestion == null)
        {
            throw new BusinessException("源题目不存在");
        }

        // 获取源题目的选项
        var sourceOptions = _optionRepository.Find(o => o.QuestionId == questionId).ToList();

        // 创建新题目
        var newQuestion = new Question
        {
            SurveyId = targetSurveyId,
            Title = sourceQuestion.Title,
            Description = sourceQuestion.Description,
            Type = sourceQuestion.Type,
            OrderIndex = 0, // 默认放在最后
            IsRequired = sourceQuestion.IsRequired,
            Validation = sourceQuestion.Validation,
            Settings = sourceQuestion.Settings,
            LLMGenerated = sourceQuestion.LLMGenerated
        };

        var createdQuestion = await _repository.AddAsync(newQuestion);

        // 复制选项
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

            await _optionRepository.AddAsync(newOption, false);
        }

        await _optionRepository.SaveChangesAsync();

        _logger.LogInformation("题目 {SourceQuestionId} 已复制到问卷 {TargetSurveyId}，新题目ID：{NewQuestionId}", 
            questionId, targetSurveyId, createdQuestion.Id);

        return _mapper.Map<QuestionDto>(createdQuestion);
    }
}
