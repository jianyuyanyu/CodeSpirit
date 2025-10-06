using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.ExamApi.Constants;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.ExamApi.Settings.Enums;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Dtos.Common;
using CodeSpirit.Shared.Dtos;
using CodeSpirit.LLM;
using LinqKit;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations
{
    public partial class QuestionService : BaseCRUDService<Question, QuestionDto, long, CreateQuestionDto, UpdateQuestionDto>, IQuestionService, IScopedDependency
    {
        private readonly IRepository<Question> _repository;
        private readonly IRepository<QuestionCategory> _categoryRepository;
        private readonly IRepository<QuestionVersion> _versionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionService> _logger;
        private readonly ISettingsService _settingsService;
        private QuestionTextParserV2 _questionTextParserV2;
        private readonly IIdGenerator _idGenerator;
        private readonly LLMAssistant _llmAssistant;
        private readonly IDistributedCache _distributedCache;

        public QuestionService(
            IRepository<Question> repository,
            IRepository<QuestionCategory> categoryRepository,
            IRepository<QuestionVersion> versionRepository,
            IMapper mapper,
            ILogger<QuestionService> logger,
            QuestionTextParserV2 questionTextParserV2,
            IIdGenerator idGenerator,
            ISettingsService settingsService,
            LLMAssistant llmAssistant,
            IDistributedCache distributedCache)
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
            _llmAssistant = llmAssistant;
            _distributedCache = distributedCache;
        }

        /// <summary>
        /// 步骤1：解析文本并进行AI审核
        /// </summary>
        /// <param name="input">解析配置</param>
        /// <returns>审核结果</returns>
        public async Task<QuestionBatchPreviewResponseDto> ParseQuestionsFromTextAsync(QuestionImportStepDto input)
        {
            // 验证分类是否存在
            var category = await _categoryRepository.GetByIdAsync(input.CategoryId);
            if (category == null)
            {
                throw new AppServiceException(400, "所选分类不存在！");
            }

            if (string.IsNullOrWhiteSpace(input.Text))
            {
                throw new AppServiceException(400, "题目文本内容不能为空！");
            }

            var result = new QuestionBatchPreviewResponseDto
            {
                CategoryId = input.CategoryId,
                CategoryName = category.Name,
                SessionId = Guid.NewGuid().ToString("N"),
                AiAuditSummary = new AiAuditSummaryDto(),
                ParseErrors = new List<string>(),
                Questions = new List<QuestionPreviewDto>()
            };

            try
            {
                // 解析题目
                var parseResults = _questionTextParserV2.Parse(input.Text);
                _logger.LogInformation($"解析到 {parseResults.Count} 个题目");


                foreach (var parsedQuestion in parseResults)
                {
                    var previewQuestion = new QuestionPreviewDto
                    {
                        Content = parsedQuestion.Content,
                        Type = parsedQuestion.Type,
                        Options = parsedQuestion.Options ?? new List<string>(),
                        CorrectAnswer = parsedQuestion.CorrectAnswer,
                        Analysis = parsedQuestion.Analysis,
                        Difficulty = parsedQuestion.Difficulty,
                        DefaultScore = parsedQuestion.Score,
                        Tags = parsedQuestion.Tags ?? new List<string>()
                    };

                    // AI审核（如果启用）
                    if (input.EnableAiAudit)
                    {
                        try
                        {
                            var auditedQuestion = await AuditQuestionWithAiAsync(previewQuestion, input.AutoCorrectErrors);
                            result.Questions.Add(auditedQuestion);

                            // 更新审核统计
                            result.AiAuditSummary.TotalCount++;
                            if (auditedQuestion.AuditStatus == AuditStatus.Passed)
                            {
                                result.AiAuditSummary.PassedCount++;
                            }
                            if (auditedQuestion.IsCorrected)
                            {
                                result.AiAuditSummary.CorrectedCount++;
                            }
                            if (auditedQuestion.HasErrors)
                            {
                                result.AiAuditSummary.ErrorCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"AI审核题目失败: {ex.Message}");
                            previewQuestion.AuditStatus = AuditStatus.Failed;
                            previewQuestion.AuditMessage = $"AI审核失败: {ex.Message}";
                            result.Questions.Add(previewQuestion);
                        }
                    }
                    else
                    {
                        result.Questions.Add(previewQuestion);
                    }
                }

                // 审核统计已在循环中更新，这里不需要重复计算

                // 缓存预览数据，用于后续步骤
                var cacheKey = $"question_preview_{result.SessionId}";
                var cacheData = new
                {
                    CategoryId = input.CategoryId,
                    CategoryName = category.Name,
                    Questions = result.Questions
                };

                await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cacheData),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                    });

                // SessionId 已在创建 result 时设置
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析题目时发生错误");
                result.ParseErrors.Add($"解析失败: {ex.Message}");
                throw new AppServiceException(500, "解析题目失败");
            }
        }

        /// <summary>
        /// 步骤2：获取题目预览数据
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>预览数据</returns>
        public async Task<QuestionBatchPreviewResponseDto> GetQuestionPreviewAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new AppServiceException(400, "会话ID不能为空！");
            }

            // 从缓存获取预览数据
            var cacheKey = $"question_preview_{sessionId}";
            var cachedDataString = await _distributedCache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedDataString))
            {
                throw new AppServiceException(400, "预览数据已过期，请重新解析！");
            }

            using var jsonDoc = JsonDocument.Parse(cachedDataString);
            var root = jsonDoc.RootElement;
            var questionsJson = root.GetProperty("Questions");
            var questions = JsonSerializer.Deserialize<List<QuestionPreviewDto>>(questionsJson.GetRawText());

            return new QuestionBatchPreviewResponseDto
            {
                SessionId = sessionId,
                CategoryId = root.GetProperty("CategoryId").GetInt64(),
                CategoryName = root.GetProperty("CategoryName").GetString() ?? "",
                Questions = questions ?? new List<QuestionPreviewDto>(),
                AiAuditSummary = new AiAuditSummaryDto
                {
                    TotalCount = questions?.Count ?? 0,
                    PassedCount = questions?.Count(q => q.AuditStatus == AuditStatus.Passed) ?? 0,
                    ErrorCount = questions?.Count(q => q.HasErrors) ?? 0,
                    CorrectedCount = questions?.Count(q => q.IsCorrected) ?? 0
                },
                ParseErrors = new List<string>()
            };
        }

        /// <summary>
        /// 步骤3：保存用户编辑的题目
        /// </summary>
        /// <param name="input">编辑数据</param>
        /// <returns>任务</returns>
        public async Task SaveQuestionEditsAsync(QuestionBatchPreviewResponseDto input)
        {
            if (string.IsNullOrWhiteSpace(input.SessionId))
            {
                throw new AppServiceException(400, "会话ID不能为空！");
            }

            // 从缓存获取原始数据
            var cacheKey = $"question_preview_{input.SessionId}";
            var cachedDataString = await _distributedCache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedDataString))
            {
                throw new AppServiceException(400, "预览数据已过期，请重新解析！");
            }

            using var jsonDoc = JsonDocument.Parse(cachedDataString);
            var root = jsonDoc.RootElement;
            var categoryId = root.GetProperty("CategoryId").GetInt64();
            var categoryName = root.GetProperty("CategoryName").GetString();

            // 更新缓存数据
            var updatedCacheData = new
            {
                CategoryId = categoryId,
                CategoryName = categoryName,
                Questions = input.Questions
            };

            await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(updatedCacheData),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });

            _logger.LogInformation($"已保存 {input.Questions.Count} 个题目的编辑内容");
        }

        /// <summary>
        /// 步骤4：确认导入题目
        /// </summary>
        /// <param name="input">导入确认数据</param>
        /// <returns>导入结果</returns>
        public async Task<ImportResultDto> ImportQuestionsAsync(QuestionBatchImportConfirmDto input)
        {
            if (string.IsNullOrWhiteSpace(input.SessionId))
            {
                throw new AppServiceException(400, "会话ID不能为空！");
            }

            // 从缓存获取预览数据
            var cacheKey = $"question_preview_{input.SessionId}";
            var cachedDataString = await _distributedCache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedDataString))
            {
                throw new AppServiceException(400, "预览数据已过期，请重新解析！");
            }

            using var jsonDoc = JsonDocument.Parse(cachedDataString);
            var root = jsonDoc.RootElement;
            var categoryId = root.GetProperty("CategoryId").GetInt64();
            var questionsJson = root.GetProperty("Questions");
            var cachedQuestions = JsonSerializer.Deserialize<List<QuestionPreviewDto>>(questionsJson.GetRawText());

            if (cachedQuestions == null || !cachedQuestions.Any())
            {
                throw new AppServiceException(400, "没有可导入的题目！");
            }

            // 确定要导入的题目
            var questionsToImport = cachedQuestions;
            if (input.QuestionIndexes?.Any() == true)
            {
                questionsToImport = new List<QuestionPreviewDto>();
                for (int i = 0; i < cachedQuestions.Count; i++)
                {
                    if (input.QuestionIndexes.Contains(i))
                    {
                        questionsToImport.Add(cachedQuestions[i]);
                    }
                }
            }

            // 应用用户修改
            if (input.Questions?.Any() == true)
            {
                foreach (var modifiedQuestion in input.Questions)
                {
                    for (int i = 0; i < questionsToImport.Count; i++)
                    {
                        if (questionsToImport[i].Content == modifiedQuestion.Content)
                        {
                            questionsToImport[i] = modifiedQuestion;
                            break;
                        }
                    }
                }
            }

            var importResult = new ImportResultDto();
            var failedItems = new List<string>();

            try
            {
                foreach (var questionPreview in questionsToImport)
                {
                    try
                    {
                        // 创建题目
                        var question = new Question
                        {
                            Id = _idGenerator.NewId(),
                            Content = questionPreview.Content,
                            Type = questionPreview.Type,
                            Options = questionPreview.Options ?? new List<string>(),
                            CorrectAnswer = questionPreview.CorrectAnswer,
                            Analysis = questionPreview.Analysis,
                            KnowledgePoints = questionPreview.KnowledgePoints,
                            Difficulty = questionPreview.Difficulty,
                            DefaultScore = (int)questionPreview.DefaultScore,
                            CategoryId = categoryId,
                            Version = 1
                        };

                        await _repository.AddAsync(question);

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
                            DefaultScore = (int)question.DefaultScore,
                            Tags = question.Tags,
                            ChangeReason = questionPreview.IsCorrected ? "AI修正后导入" : "初始创建"
                        };

                        await _versionRepository.AddAsync(version);
                        importResult.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"导入题目失败: {questionPreview.Content}");
                        failedItems.Add($"题目: {questionPreview.Content?.Substring(0, Math.Min(50, questionPreview.Content?.Length ?? 0))}... - 错误: {ex.Message}");
                    }
                }

                // 清理缓存
                await _distributedCache.RemoveAsync(cacheKey);

                importResult.FailedCount = failedItems.Count;
                importResult.FailedItems = failedItems;

                return importResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入题目时发生错误");
                throw new AppServiceException(500, "导入题目失败");
            }
        }

        /// <summary>
        /// 从AI响应中提取JSON内容
        /// </summary>
        /// <param name="aiResponse">AI响应内容</param>
        /// <returns>清理后的JSON字符串</returns>
        private string ExtractJsonFromAiResponse(string aiResponse)
        {
            if (string.IsNullOrEmpty(aiResponse))
            {
                throw new ArgumentException("AI响应内容为空", nameof(aiResponse));
            }

            // 移除可能的Markdown代码块标记
            var cleaned = aiResponse.Trim();

            // 如果以```json开头，移除代码块标记
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7); // 移除```json
                var codeBlockEndIndex = cleaned.LastIndexOf("```");
                if (codeBlockEndIndex > 0)
                {
                    cleaned = cleaned.Substring(0, codeBlockEndIndex);
                }
            }
            // 如果以```开头，移除代码块标记
            else if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                if (firstNewline > 0)
                {
                    cleaned = cleaned.Substring(firstNewline + 1);
                }
                var codeBlockEndIndex2 = cleaned.LastIndexOf("```");
                if (codeBlockEndIndex2 > 0)
                {
                    cleaned = cleaned.Substring(0, codeBlockEndIndex2);
                }
            }

            // 查找第一个{和最后一个}，提取JSON部分
            var startIndex = cleaned.IndexOf('{');
            var endIndex = cleaned.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
            }

            return cleaned.Trim();
        }

        /// <summary>
        /// AI审核单个题目
        /// </summary>
        private async Task<QuestionPreviewDto> AuditQuestionWithAiAsync(QuestionPreviewDto question, bool autoCorrect = true)
        {
            string auditResult = string.Empty;
            try
            {
                var prompt = $@"请审核以下题目的格式和内容，检查是否存在错误：

题目内容：{question.Content}
题目类型：{question.Type}
选项：{string.Join(", ", question.Options)}
正确答案：{question.CorrectAnswer}
难度：{question.Difficulty}
解析：{question.Analysis}
标签：{string.Join(", ", question.Tags)}


请检查：
1. 题目内容不应包含序号、分值、选项、答案，应完整、清晰
2. 选项格式不应包含序号、ABCD等标记，多个选项应使用逗号分隔，选项不应重复
3. 正确答案是否与选项匹配，是否合理；如果正确答案使用序号或ABCD等标记，应修正为选项文本
4. 是否存在错别字或标点符号错误
5. 解析是否合理


{(autoCorrect ? "如果发现错误，请自动修正并说明修正内容。" : "如果发现错误，请指出错误但不要修正。")}

请以JSON格式返回结果：
{{
  ""hasErrors"": true/false,
  ""errors"": [""错误描述1"", ""错误描述2""],
  ""corrections"": [""修正说明1"", ""修正说明2""],
  ""correctedContent"": ""修正后的题目内容"",
  ""correctedOptions"": [""修正后的选项""],
  ""correctedAnswer"": ""修正后的答案"",
  ""correctedAnalysis"": ""修正后的解析""
}}";

                auditResult = await _llmAssistant.GenerateContentAsync(prompt);

                // 清理AI返回的结果，提取JSON内容
                var cleanedResult = ExtractJsonFromAiResponse(auditResult);

                // 解析AI返回的结果
                var auditData = JsonSerializer.Deserialize<JsonElement>(cleanedResult);

                question.AuditStatus = AuditStatus.Passed;
                question.HasErrors = auditData.GetProperty("hasErrors").GetBoolean();

                if (question.HasErrors)
                {
                    var errors = auditData.GetProperty("errors").EnumerateArray()
                        .Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    question.AuditMessage = string.Join("; ", errors);

                    if (autoCorrect && auditData.TryGetProperty("correctedContent", out var correctedContent))
                    {
                        // 保存原始内容（在修正前）
                        question.OriginalContent = question.Content;
                        question.OriginalOptions = new List<string>(question.Options);
                        question.OriginalAnswer = question.CorrectAnswer;
                        question.OriginalAnalysis = question.Analysis;

                        // 应用AI修正
                        var correctedContentStr = correctedContent.GetString();
                        if (!string.IsNullOrEmpty(correctedContentStr))
                        {
                            question.Content = correctedContentStr;
                            question.IsCorrected = true;
                        }

                        if (auditData.TryGetProperty("correctedOptions", out var correctedOptions))
                        {
                            var optionsList = correctedOptions.EnumerateArray()
                                .Select(o => o.GetString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                            if (optionsList.Any())
                            {
                                question.Options = optionsList;
                            }
                        }

                        if (auditData.TryGetProperty("correctedAnswer", out var correctedAnswer))
                        {
                            var answerStr = correctedAnswer.GetString();
                            if (!string.IsNullOrEmpty(answerStr))
                            {
                                question.CorrectAnswer = answerStr;
                            }
                        }

                        if (auditData.TryGetProperty("correctedAnalysis", out var correctedAnalysis))
                        {
                            var analysisStr = correctedAnalysis.GetString();
                            if (!string.IsNullOrEmpty(analysisStr))
                            {
                                question.Analysis = analysisStr;
                            }
                        }

                        if (auditData.TryGetProperty("corrections", out var corrections))
                        {
                            var correctionsList = corrections.EnumerateArray()
                                .Select(c => c.GetString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                            if (correctionsList.Any())
                            {
                                question.CorrectionNotes = correctionsList;
                                question.AuditMessage += $" [已修正: {string.Join("; ", correctionsList)}]";
                            }
                        }
                    }
                }
                else
                {
                    question.AuditMessage = "题目格式和内容正确";
                }

                return question;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning($"AI审核结果JSON解析失败: {ex.Message}，原始响应: {auditResult}");
                question.AuditStatus = AuditStatus.Failed;
                question.AuditMessage = $"AI审核失败: JSON解析错误 - {ex.Message}";
                return question;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"AI审核失败: {ex.Message}");
                question.AuditStatus = AuditStatus.Failed;
                question.AuditMessage = $"AI审核失败: {ex.Message}";
                return question;
            }
        }

        // CRUD方法实现
        public async Task<PageList<QuestionDto>> GetQuestionsAsync(QuestionQueryDto query)
        {
            var predicate = PredicateBuilder.New<Question>(true);

            // 关键词搜索（题目内容）
            if (!string.IsNullOrEmpty(query.Keywords))
            {
                predicate = predicate.And(x => x.Content.Contains(query.Keywords) ||
                                              (x.Analysis != null && x.Analysis.Contains(query.Keywords)));
            }

            // 题目类型筛选
            if (query.Type.HasValue)
            {
                predicate = predicate.And(x => x.Type == query.Type.Value);
            }

            // 题目难度筛选
            if (query.Difficulty.HasValue)
            {
                predicate = predicate.And(x => x.Difficulty == query.Difficulty.Value);
            }

            // 分类ID筛选
            if (query.CategoryId.HasValue)
            {
                predicate = predicate.And(x => x.CategoryId == query.CategoryId.Value);
            }

            // 知识点筛选
            if (!string.IsNullOrEmpty(query.KnowledgePoint))
            {
                predicate = predicate.And(x => x.KnowledgePoints != null &&
                                              x.KnowledgePoints.Contains(query.KnowledgePoint));
            }

            // 标签筛选
            if (!string.IsNullOrEmpty(query.Tag))
            {
                predicate = predicate.And(x => x.Tags != null &&
                                              x.Tags.Contains(query.Tag));
            }

            // 构建查询并排序
            var baseQuery = _repository.Find(predicate)
                .OrderByDescending(x => x.CreatedAt); // 默认按创建时间倒序

            // 分页查询
            var totalCount = await baseQuery.CountAsync();
            var items = await baseQuery
                .Skip((query.Page - 1) * query.PerPage)
                .Take(query.PerPage)
                .ToListAsync();

            // 映射到DTO
            var questionDtos = _mapper.Map<List<QuestionDto>>(items);

            return new PageList<QuestionDto>
            {
                Items = questionDtos,
                Total = totalCount
            };
        }

        public async Task<List<QuestionSelectListDto>> GetQuestionSelectListAsync(QuestionSelectListQueryDto query)
        {
            var predicate = PredicateBuilder.New<Question>(true);

            // 题目类型筛选
            if (query.Type.HasValue)
            {
                predicate = predicate.And(x => x.Type == query.Type.Value);
            }

            var questions = await _repository.Find(predicate)
                .OrderByDescending(x => x.CreatedAt)
                .Take(100) // 限制返回数量
                .ToListAsync();

            return _mapper.Map<List<QuestionSelectListDto>>(questions);
        }

        public async Task<QuestionDto> GetQuestionAsync(long id)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null)
            {
                throw new AppServiceException(404, "题目不存在");
            }

            return _mapper.Map<QuestionDto>(question);
        }

        public async Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto createDto)
        {
            // 验证分类是否存在
            var category = await _categoryRepository.GetByIdAsync(createDto.CategoryId);
            if (category == null)
            {
                throw new AppServiceException(400, "所选分类不存在");
            }

            var question = _mapper.Map<Question>(createDto);
            question.Id = _idGenerator.NewId();
            question.CreatedAt = DateTime.UtcNow;
            question.UpdatedAt = DateTime.UtcNow;

            await _repository.AddAsync(question);

            // 创建版本记录
            var version = new QuestionVersion
            {
                Id = _idGenerator.NewId(),
                QuestionId = question.Id,
                Content = question.Content,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Analysis = question.Analysis,
                DefaultScore = question.DefaultScore,
                Tags = question.Tags,
                KnowledgePoints = question.KnowledgePoints,
                ChangeReason = "创建题目",
                CreatedAt = DateTime.UtcNow
            };

            await _versionRepository.AddAsync(version);

            return _mapper.Map<QuestionDto>(question);
        }

        public async Task UpdateQuestionAsync(long id, UpdateQuestionDto updateDto)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null)
            {
                throw new AppServiceException(404, "题目不存在");
            }

            // 验证分类是否存在
            if (updateDto.CategoryId != question.CategoryId)
            {
                var category = await _categoryRepository.GetByIdAsync(updateDto.CategoryId);
                if (category == null)
                {
                    throw new AppServiceException(400, "所选分类不存在");
                }
            }

            // 保存原始数据用于版本记录
            var originalContent = question.Content;
            var originalOptions = question.Options;
            var originalCorrectAnswer = question.CorrectAnswer;

            // 更新题目
            _mapper.Map(updateDto, question);
            question.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(question);

            // 创建版本记录（如果内容有变化）
            if (originalContent != question.Content ||
                !originalOptions.SequenceEqual(question.Options) ||
                originalCorrectAnswer != question.CorrectAnswer)
            {
                var version = new QuestionVersion
                {
                    Id = _idGenerator.NewId(),
                    QuestionId = question.Id,
                    Content = question.Content,
                    Options = question.Options,
                    CorrectAnswer = question.CorrectAnswer,
                    Analysis = question.Analysis,
                    DefaultScore = question.DefaultScore,
                    Tags = question.Tags,
                    KnowledgePoints = question.KnowledgePoints,
                    ChangeReason = "更新题目",
                    CreatedAt = DateTime.UtcNow
                };

                await _versionRepository.AddAsync(version);
            }
        }

        public async Task DeleteQuestionAsync(long id)
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null)
            {
                throw new AppServiceException(404, "题目不存在");
            }

            await _repository.DeleteAsync(question);
        }

        public async Task<List<QuestionVersionDto>> GetQuestionVersionsAsync(long questionId)
        {
            var versions = await _versionRepository.Find(x => x.QuestionId == questionId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<QuestionVersionDto>>(versions);
        }

        public async Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids)
        {
            var successCount = 0;
            var failedIds = new List<long>();

            foreach (var id in ids)
            {
                try
                {
                    await DeleteQuestionAsync(id);
                    successCount++;
                }
                catch
                {
                    failedIds.Add(id);
                }
            }

            return (successCount, failedIds);
        }

        public async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<QuestionBatchImportItemDto> items)
        {
            var successCount = 0;
            var failedIds = new List<string>();

            foreach (var item in items)
            {
                try
                {
                    var createDto = _mapper.Map<CreateQuestionDto>(item);
                    await CreateQuestionAsync(createDto);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"批量导入题目失败: {ex.Message}");
                    failedIds.Add(item.Content?.Substring(0, Math.Min(50, item.Content?.Length ?? 0)) ?? "未知题目");
                }
            }

            return (successCount, failedIds);
        }

        public async Task<(int successCount, List<string> failedItems)> ImportFromTextAsync(QuestionImportFromTextDto input)
        {
            throw new AppServiceException(400, "此方法已被新的多步骤导入向导替代，请使用题目导入向导功能");
        }

        public async Task<QuestionSettingsDto> GetQuestionSettingsAsync()
        {
            await Task.CompletedTask; // 避免async警告

            // 返回默认设置
            return new QuestionSettingsDto();
        }

        public async Task<bool> UpdateQuestionSettingsAsync(QuestionSettingsDto settings)
        {
            await Task.CompletedTask; // 避免async警告

            // 暂时返回成功，实际实现需要根据具体的设置服务接口
            return true;
        }
    }
}
