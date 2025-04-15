using AutoMapper;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Constants;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.ExamApi.Settings.Enums;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using System.Net;
using System.Text.Json;

/// <summary>
/// 题目服务实现
/// </summary>
namespace CodeSpirit.ExamApi.Services.Implementations
{
    public class QuestionService : BaseCRUDIService<Question, QuestionDto, long, CreateQuestionDto, UpdateQuestionDto, QuestionBatchImportItemDto>, IQuestionService
    {
        private const string QuestionSettings = "QuestionSettings";
        private readonly IRepository<Question> _repository;
        private readonly IRepository<QuestionCategory> _categoryRepository;
        private readonly IRepository<QuestionVersion> _versionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionService> _logger;
        private readonly ISettingsService _settingsService;
        private string? _changeReason;
        private QuestionTextParserV2 _questionTextParserV2;
        private readonly IIdGenerator _idGenerator;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QuestionService(
            IRepository<Question> repository,
            IRepository<QuestionCategory> categoryRepository,
            IRepository<QuestionVersion> versionRepository,
            IMapper mapper,
            ILogger<QuestionService> logger,
            QuestionTextParserV2 questionTextParserV2,
            IIdGenerator idGenerator,
            ISettingsService settingsService)
            : base(repository, mapper)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _versionRepository = versionRepository;
            _mapper = mapper;
            _logger = logger;
            _questionTextParserV2 = questionTextParserV2;
            _idGenerator = idGenerator;
            _settingsService = settingsService;
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
        /// 获取选择题选项列表
        /// </summary>
        /// <param name="queryDto"></param>
        /// <returns></returns>
        public async Task<List<QuestionSelectListDto>> GetQuestionSelectListAsync(QuestionSelectListQueryDto queryDto)
        {
            var query = _repository.CreateQuery();
            if (queryDto.CategoryIds != null && queryDto.CategoryIds.Any())
                query = query.Where(s => queryDto.CategoryIds.Contains(s.CategoryId));

            if (queryDto.Difficulty.HasValue)
                query = query.Where(s => s.Difficulty == queryDto.Difficulty.Value);

            if (queryDto.Type.HasValue)
                query = query.Where(s => s.Type == queryDto.Type.Value);

            return await query.Select(s => new QuestionSelectListDto()
            {
                Id = s.Id,
                Content = s.Content
            }).ToListAsync();
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

            // 获取当前的唯一性校验模式
            var settings = await GetQuestionSettingsAsync();

            // 根据唯一性校验模式进行校验
            if (settings.UniquenessMode != QuestionUniquenessMode.None)
            {
                // 构建查询条件
                var query = _repository.CreateQuery()
                    .Where(q => q.Content == createDto.Content && q.Type == createDto.Type);

                // 如果是分类唯一，则添加分类条件
                if (settings.UniquenessMode == QuestionUniquenessMode.Category)
                {
                    query = query.Where(q => q.CategoryId == createDto.CategoryId);
                }

                // 检查题目是否重复
                var existingQuestion = await query.FirstOrDefaultAsync();
                if (existingQuestion != null)
                {
                    throw new AppServiceException(400, "该题目已存在！");
                }
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

            // 获取当前的唯一性校验模式
            var settings = await GetQuestionSettingsAsync();

            // 根据唯一性校验模式进行校验
            if (settings.UniquenessMode != QuestionUniquenessMode.None)
            {
                // 构建查询条件
                var query = _repository.CreateQuery()
                    .Where(q => q.Id != id && q.Content == updateDto.Content && q.Type == updateDto.Type);

                // 如果是分类唯一，则添加分类条件
                if (settings.UniquenessMode == QuestionUniquenessMode.Category)
                {
                    query = query.Where(q => q.CategoryId == updateDto.CategoryId);
                }

                // 检查题目是否重复
                var existingQuestion = await query.FirstOrDefaultAsync();
                if (existingQuestion != null)
                {
                    throw new AppServiceException(400, "该题目已存在！");
                }
            }

            // 根据题目类型验证选项和答案
            ValidateOptionsAndAnswer(updateDto.Type, updateDto.Options, updateDto.CorrectAnswer);
        }

        /// <summary>
        /// 创建实体前的处理
        /// </summary>
        protected override async Task OnCreating(Question entity, CreateQuestionDto createDto)
        {
            // HTML编码处理
            entity.Content = WebUtility.HtmlEncode(createDto.Content);
            entity.CorrectAnswer = WebUtility.HtmlEncode(createDto.CorrectAnswer);
            entity.Analysis = createDto.Analysis != null ? WebUtility.HtmlEncode(createDto.Analysis) : null;

            // 对选项列表进行HTML编码处理
            if (entity.Options != null && entity.Options.Any())
            {
                entity.Options = entity.Options.Select(o => WebUtility.HtmlEncode(o)).ToList();
            }

            // 处理JSON序列化
            if (createDto.Tags?.Any() == true)
            {
                entity.Tags = JsonSerializer.Serialize(createDto.Tags);
            }

            // 设置初始版本
            entity.Version = 1;
        }

        protected override async Task OnCreated(Question entity, CreateQuestionDto createDto)
        {
            // 创建初始版本记录
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
                ChangeReason = "初始创建"
            };

            await _versionRepository.AddAsync(version);
            await _versionRepository.SaveChangesAsync();
            await base.OnCreated(entity, createDto);
        }

        /// <summary>
        /// 更新实体前的处理
        /// </summary>
        protected override async Task OnUpdating(Question entity, UpdateQuestionDto updateDto)
        {
            // 保存修改原因，供 OnUpdated 使用
            _changeReason = updateDto.ChangeReason != null ? WebUtility.HtmlEncode(updateDto.ChangeReason) : null;

            // HTML编码处理
            entity.Content = WebUtility.HtmlEncode(updateDto.Content);
            entity.CorrectAnswer = WebUtility.HtmlEncode(updateDto.CorrectAnswer);
            entity.Analysis = updateDto.Analysis != null ? WebUtility.HtmlEncode(updateDto.Analysis) : null;

            // 对选项列表进行HTML编码处理
            if (updateDto.Options != null && updateDto.Options.Any())
            {
                entity.Options = updateDto.Options.Select(o => WebUtility.HtmlEncode(o)).ToList();
            }

            // 处理 JSON 序列化
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
            // 增加版本号
            entity.Version++;
            // 创建版本记录
            var version = new QuestionVersion
            {
                Id = _idGenerator.NewId(),
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
                if (type != QuestionType.TrueFalse)
                {
                    throw new AppServiceException(400, "题目必须包含选项！");
                }
            }
            // 检查选项是否有重复
            else if (type == QuestionType.SingleChoice || type == QuestionType.MultipleChoice)
            {
                // 检查选项是否有重复
                if (options.Distinct().Count() != options.Count)
                {
                    throw new AppServiceException(400, "题目选项不能重复！");
                }
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
            throw new NotSupportedException("暂不支持！");
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
                .Where(q => q.ExamPaperQuestions.Any())
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

        /// <summary>
        /// 文本识别导入题目
        /// </summary>
        /// <returns>导入结果</returns>
        public async Task<(int successCount, List<string> failedItems)> ImportFromTextAsync(QuestionImportFromTextDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                throw new AppServiceException(400, "试卷文本内容不能为空！");
            }

            // 验证分类是否存在
            var category = await _categoryRepository.GetByIdAsync(input.CategoryId);
            if (category == null)
            {
                throw new AppServiceException(400, "所选分类不存在！");
            }

            var successCount = 0;
            var failedItems = new List<string>();

            // 使用事务包装所有数据库操作
            await _repository.ExecuteInTransactionAsync(async () =>
            {
                var text = WebUtility.HtmlEncode(input.Text);
                var parsedQuestions = _questionTextParserV2.Parse(text);

                if (!parsedQuestions.Any())
                {
                    throw new AppServiceException(400, "未能从文本中解析出任何题目，请检查文本格式！");
                }

                // 获取当前的唯一性校验模式
                var settings = await GetQuestionSettingsAsync();

                foreach (var questionData in parsedQuestions)
                {
                    try
                    {
                        // 检查题目是否重复（根据唯一性校验模式）
                        if (settings.UniquenessMode != QuestionUniquenessMode.None)
                        {
                            IQueryable<Question> query = _repository.CreateQuery()
                                .Where(q => q.Content == questionData.Content && q.Type == questionData.Type);

                            // 如果是分类唯一，则添加分类条件
                            if (settings.UniquenessMode == QuestionUniquenessMode.Category)
                            {
                                query = query.Where(q => q.CategoryId == input.CategoryId);
                            }

                            var existingQuestion = await query.FirstOrDefaultAsync();

                            if (existingQuestion != null)
                            {
                                string typeStr = questionData.Type == QuestionType.SingleChoice ? "单选题" : "判断题";
                                failedItems.Add($"{typeStr}「{questionData.Content}」已存在");
                                continue;
                            }
                        }

                        // 确保选项和答案格式正确
                        try
                        {
                            ValidateOptionsAndAnswer(questionData.Type, questionData.Options, questionData.CorrectAnswer);
                        }
                        catch (AppServiceException ex)
                        {
                            string typeStr = questionData.Type == QuestionType.SingleChoice ? "单选题" : "判断题";
                            failedItems.Add($"{typeStr}「{questionData.Content}」验证失败：{ex.Message}");
                            continue;
                        }

                        var question = new Question
                        {
                            Id = _idGenerator.NewId(),
                            Content = questionData.Content,
                            Options = questionData.Options?.Select(o => o).ToList() ?? new List<string>(),
                            CorrectAnswer = questionData.CorrectAnswer,
                            Type = questionData.Type,
                            Difficulty = questionData.Difficulty,
                            CategoryId = input.CategoryId,
                            Version = 1,
                            DefaultScore = questionData.Score,
                            Analysis = questionData.Analysis != null ? questionData.Analysis : null
                        };

                        // 处理标签
                        if (questionData.Tags?.Any() == true)
                        {
                            question.Tags = JsonSerializer.Serialize(questionData.Tags);
                        }
                        // 创建初始版本记录
                        var version = new QuestionVersion
                        {
                            Id = _idGenerator.NewId(),
                            QuestionId = question.Id,
                            Version = question.Version,
                            Content = question.Content,
                            Options = question.Options,
                            CorrectAnswer = question.CorrectAnswer,
                            Analysis = question.Analysis,
                            KnowledgePoints = question.KnowledgePoints,
                            DefaultScore = question.DefaultScore,
                            Tags = question.Tags,
                            ChangeReason = "初始创建"
                        };

                        await _repository.AddAsync(question, false);
                        await _versionRepository.AddAsync(version, false);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        string typeStr = questionData.Type == QuestionType.SingleChoice ? "单选题" : "判断题";
                        _logger.LogError(ex, "导入{Type}失败: {Content}", typeStr, questionData.Content);
                        failedItems.Add($"{typeStr}「{questionData.Content}」导入失败：{ex.Message}");
                    }
                }

                // 在事务结束时统一保存所有更改
                await _repository.SaveChangesAsync();
                await _versionRepository.SaveChangesAsync();
            });
            if (failedItems.Any())
            {
                _logger.LogInformation($"{failedItems.Count} 个题目导入失败: \n{string.Join(", \n", failedItems)}");
            }
            return (successCount, failedItems);
        }

        /// <summary>
        /// 获取题目设置
        /// </summary>
        /// <returns>题目设置</returns>
        public async Task<QuestionSettingsDto> GetQuestionSettingsAsync()
        {
            var settings = await _settingsService.GetGlobalSettingAsync<QuestionSettingsDto>(
                ExamConstants.ExamModule,
                QuestionSettings);

            return settings ?? new QuestionSettingsDto()
            {
                UniquenessMode = QuestionUniquenessMode.Global
            };
        }

        /// <summary>
        /// 更新题目设置
        /// </summary>
        /// <param name="settings">题目设置</param>
        /// <returns>是否更新成功</returns>
        public async Task<bool> UpdateQuestionSettingsAsync(QuestionSettingsDto settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // 更新唯一性校验模式设置
            var result = await _settingsService.SetGlobalSettingAsync(
                ExamConstants.ExamModule,
                QuestionSettings,
                settings);

            return result;
        }
    }
}