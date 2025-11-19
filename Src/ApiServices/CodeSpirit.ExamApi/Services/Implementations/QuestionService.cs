using AutoMapper;
using CodeSpirit.Core;
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
                _logger.LogInformation("解析到 {Count} 个题目", parseResults.Count);

                // 记录题目总数用于日志
                _logger.LogInformation("解析到 {TotalCount} 道题目，将进行分批处理", parseResults.Count);

                // 转换为预览题目对象
                var previewQuestions = parseResults.Select(parsedQuestion => new QuestionPreviewDto
                {
                    Content = parsedQuestion.Content,
                    Type = parsedQuestion.Type,
                    Options = parsedQuestion.Options ?? new List<string>(),
                    CorrectAnswer = parsedQuestion.CorrectAnswer,
                    Analysis = parsedQuestion.Analysis,
                    Difficulty = parsedQuestion.Difficulty,
                    DefaultScore = parsedQuestion.Score,
                    Tags = parsedQuestion.Tags ?? new List<string>()
                }).ToList();

                // AI审核（如果启用）
                if (input.EnableAiAudit)
                {
                    _logger.LogInformation("开始分批AI审核 {Count} 道题目", previewQuestions.Count);
                    var auditedQuestions = await BatchAuditQuestionsWithAutoSplitAsync(previewQuestions, input.AutoCorrectErrors);
                    result.Questions = auditedQuestions;

                    // 计算审核统计
                    result.AiAuditSummary.TotalCount = auditedQuestions.Count;
                    result.AiAuditSummary.PassedCount = auditedQuestions.Count(q => q.AuditStatus == AuditStatus.Passed);
                    result.AiAuditSummary.ErrorCount = auditedQuestions.Count(q => q.HasErrors);
                    result.AiAuditSummary.CorrectedCount = auditedQuestions.Count(q => q.IsCorrected);

                    _logger.LogInformation("AI审核完成：总计{TotalCount}道，通过{PassedCount}道，错误{ErrorCount}道，修正{CorrectedCount}道",
                        result.AiAuditSummary.TotalCount, result.AiAuditSummary.PassedCount, result.AiAuditSummary.ErrorCount, result.AiAuditSummary.CorrectedCount);
                }
                else
                {
                    result.Questions = previewQuestions;
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

            _logger.LogInformation("已保存 {Count} 个题目的编辑内容", input.Questions.Count);
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

            // 重复验证逻辑
            await ValidateImportQuestionsAsync(questionsToImport, categoryId);

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
                        var versionNumber = await GetNextVersionNumberAsync(question.Id);
                        var version = new QuestionVersion
                        {
                            Id = _idGenerator.NewId(),
                            QuestionId = question.Id,
                            Version = versionNumber,
                            Content = question.Content,
                            Options = question.Options,
                            CorrectAnswer = question.CorrectAnswer,
                            Analysis = question.Analysis,
                            KnowledgePoints = question.KnowledgePoints,
                            DefaultScore = (int)question.DefaultScore,
                            Tags = question.Tags,
                            ChangeReason = questionPreview.IsCorrected ? "AI修正后导入" : "初始创建",
                            TenantId = question.TenantId
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

            _logger.LogDebug("开始提取JSON，原始响应长度: {Length}", aiResponse.Length);

            // 记录原始响应的前500个字符用于调试
            if (aiResponse.Length > 0)
            {
                var previewLength = Math.Min(500, aiResponse.Length);
                _logger.LogDebug("原始AI响应预览: {Preview}", aiResponse.Substring(0, previewLength));
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
                _logger.LogDebug("移除```json代码块标记");
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
                _logger.LogDebug("移除```代码块标记");
            }

            // 查找第一个{和最后一个}，提取JSON部分
            var startIndex = cleaned.IndexOf('{');
            var endIndex = cleaned.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
                _logger.LogDebug("提取JSON片段，起始位置: {Start}，结束位置: {End}", startIndex, endIndex);
            }
            else
            {
                _logger.LogWarning("未找到有效的JSON边界，起始位置: {Start}，结束位置: {End}", startIndex, endIndex);
            }

            var result = cleaned.Trim();
            _logger.LogDebug("JSON提取完成，最终长度: {Length}", result.Length);

            return result;
        }

        /// <summary>
        /// 自动分批AI审核多个题目（支持超过30道题目的自动分批处理）
        /// </summary>
        /// <param name="questions">待审核的题目列表</param>
        /// <param name="autoCorrect">是否自动修正错误</param>
        /// <returns>审核后的题目列表</returns>
        private async Task<List<QuestionPreviewDto>> BatchAuditQuestionsWithAutoSplitAsync(List<QuestionPreviewDto> questions, bool autoCorrect = true)
        {
            if (questions == null || !questions.Any())
            {
                return new List<QuestionPreviewDto>();
            }

            const int batchSize = 10; // 每批最多处理10道题目，减少AI响应截断风险
            var allAuditedQuestions = new List<QuestionPreviewDto>();
            var totalBatches = (int)Math.Ceiling((double)questions.Count / batchSize);

            _logger.LogInformation("开始分批AI审核：总计 {TotalCount} 道题目，分为 {BatchCount} 批处理，每批最多 {BatchSize} 道题目",
                questions.Count, totalBatches, batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var startIndex = batchIndex * batchSize;
                var currentBatch = questions.Skip(startIndex).Take(batchSize).ToList();

                _logger.LogInformation("处理第 {CurrentBatch}/{TotalBatches} 批，题目范围：{StartIndex}-{EndIndex}，共 {BatchCount} 道题目",
                    batchIndex + 1, totalBatches, startIndex + 1, startIndex + currentBatch.Count, currentBatch.Count);

                try
                {
                    // 处理当前批次
                    var batchAuditedQuestions = await BatchAuditQuestionsWithAiAsync(currentBatch, autoCorrect);
                    allAuditedQuestions.AddRange(batchAuditedQuestions);

                    _logger.LogInformation("第 {CurrentBatch}/{TotalBatches} 批处理完成", batchIndex + 1, totalBatches);

                    // 如果不是最后一批，添加短暂延迟以避免过于频繁的AI请求
                    if (batchIndex < totalBatches - 1)
                    {
                        await Task.Delay(1000); // 延迟1秒
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "第 {CurrentBatch}/{TotalBatches} 批处理失败，将该批次题目标记为审核失败", batchIndex + 1, totalBatches);

                    // 如果某批次失败，将该批次的题目标记为审核失败
                    var failedBatchQuestions = currentBatch.Select(q =>
                    {
                        var failedQuestion = new QuestionPreviewDto
                        {
                            Content = q.Content,
                            Type = q.Type,
                            Options = new List<string>(q.Options),
                            CorrectAnswer = q.CorrectAnswer,
                            Analysis = q.Analysis,
                            Difficulty = q.Difficulty,
                            DefaultScore = q.DefaultScore,
                            Tags = new List<string>(q.Tags),
                            KnowledgePoints = q.KnowledgePoints,
                            AuditStatus = AuditStatus.Failed,
                            AuditMessage = $"批次处理失败: {ex.Message}"
                        };
                        return failedQuestion;
                    }).ToList();

                    allAuditedQuestions.AddRange(failedBatchQuestions);
                }
            }

            _logger.LogInformation("所有批次处理完成：总计 {TotalCount} 道题目，成功处理 {SuccessCount} 道，失败 {FailedCount} 道",
                questions.Count,
                allAuditedQuestions.Count(q => q.AuditStatus != AuditStatus.Failed),
                allAuditedQuestions.Count(q => q.AuditStatus == AuditStatus.Failed));

            return allAuditedQuestions;
        }

        /// <summary>
        /// 批量AI审核多个题目（一次请求发送所有题目，限制在30道以内）
        /// </summary>
        /// <param name="questions">待审核的题目列表</param>
        /// <param name="autoCorrect">是否自动修正错误</param>
        /// <returns>审核后的题目列表</returns>
        private async Task<List<QuestionPreviewDto>> BatchAuditQuestionsWithAiAsync(List<QuestionPreviewDto> questions, bool autoCorrect = true)
        {
            if (questions == null || !questions.Any())
            {
                return new List<QuestionPreviewDto>();
            }

            // 确保不超过10道题目
            if (questions.Count > 10)
            {
                throw new ArgumentException($"单批次最多只能处理10道题目，当前批次有{questions.Count}道题目");
            }

            const int maxRetries = 2; // 最多重试2次
            
            // 为每个题目分配唯一ID，用于跟踪
            var questionsWithId = questions.Select((q, index) => new
            {
                OriginalIndex = index,
                Question = new QuestionPreviewDto
                {
                    Content = q.Content,
                    Type = q.Type,
                    Options = new List<string>(q.Options),
                    CorrectAnswer = q.CorrectAnswer,
                    Analysis = q.Analysis,
                    Difficulty = q.Difficulty,
                    DefaultScore = q.DefaultScore,
                    Tags = new List<string>(q.Tags),
                    KnowledgePoints = q.KnowledgePoints,
                    AuditStatus = AuditStatus.Passed
                }
            }).ToList();

            // 跟踪已成功审核的题目（按原始索引）
            var successfulResults = new Dictionary<int, QuestionPreviewDto>();
            var questionsToRetry = questionsWithId.ToList();
            var currentAttempt = 0;

            while (currentAttempt <= maxRetries && questionsToRetry.Any())
            {
                try
                {
                    currentAttempt++;
                    _logger.LogDebug("开始第 {Attempt}/{MaxAttempts} 次AI审核尝试，待审核题目数量: {Count}",
                        currentAttempt, maxRetries + 1, questionsToRetry.Count);

                    // 构建批量审核的提示词（只包含待审核的题目）
                    var questionsForAudit = questionsToRetry.Select(q => q.Question).ToList();
                    var prompt = BuildBatchAuditPrompt(questionsForAudit, autoCorrect);

                    _logger.LogDebug("发送批量AI审核请求，题目数量: {Count}，提示词长度: {PromptLength}",
                        questionsForAudit.Count, prompt?.Length ?? 0);

                    // 发送AI请求
                    var aiResponse = await _llmAssistant.GenerateContentAsync(prompt);

                    _logger.LogDebug("收到AI响应，响应长度: {ResponseLength}", aiResponse?.Length ?? 0);

                    // 记录AI响应的前500个字符用于调试
                    if (!string.IsNullOrEmpty(aiResponse) && aiResponse.Length > 0)
                    {
                        var previewLength = Math.Min(500, aiResponse.Length);
                        _logger.LogDebug("AI响应预览: {Preview}...", aiResponse.Substring(0, previewLength));
                    }

                    // 解析AI响应
                    var currentResults = ParseBatchAuditResponse(aiResponse, questionsForAudit);

                    // 分离成功和失败的题目
                    var currentSuccessful = new List<(int OriginalIndex, QuestionPreviewDto Result)>();
                    var currentFailed = new List<(int OriginalIndex, QuestionPreviewDto Question)>();

                    for (int i = 0; i < questionsToRetry.Count; i++)
                    {
                        var questionWithId = questionsToRetry[i];
                        var result = i < currentResults.Count ? currentResults[i] : null;

                        if (result != null && IsAuditSuccessful(result))
                        {
                            currentSuccessful.Add((questionWithId.OriginalIndex, result));
                        }
                        else
                        {
                            // 如果没有结果或审核失败，准备重试
                            var questionForRetry = new QuestionPreviewDto
                            {
                                Content = questionWithId.Question.Content,
                                Type = questionWithId.Question.Type,
                                Options = new List<string>(questionWithId.Question.Options),
                                CorrectAnswer = questionWithId.Question.CorrectAnswer,
                                Analysis = questionWithId.Question.Analysis,
                                Difficulty = questionWithId.Question.Difficulty,
                                DefaultScore = questionWithId.Question.DefaultScore,
                                Tags = new List<string>(questionWithId.Question.Tags),
                                KnowledgePoints = questionWithId.Question.KnowledgePoints,
                                AuditStatus = AuditStatus.Passed
                            };
                            currentFailed.Add((questionWithId.OriginalIndex, questionForRetry));
                        }
                    }

                    // 将成功的结果添加到最终结果中
                    foreach (var (originalIndex, result) in currentSuccessful)
                    {
                        successfulResults[originalIndex] = result;
                    }

                    _logger.LogDebug("第 {Attempt} 次尝试完成，成功审核: {SuccessCount}，失败: {FailedCount}",
                        currentAttempt, currentSuccessful.Count, currentFailed.Count);

                    // 更新待重试的题目列表
                    questionsToRetry = currentFailed.Select(f => new
                    {
                        OriginalIndex = f.OriginalIndex,
                        Question = f.Question
                    }).ToList();

                    // 如果没有失败的题目，结束循环
                    if (!questionsToRetry.Any())
                    {
                        _logger.LogInformation("所有题目审核成功，第 {Attempt} 次尝试完成", currentAttempt);
                        break;
                    }
                    else if (currentAttempt > maxRetries)
                    {
                        _logger.LogWarning("已达到最大重试次数({MaxRetries})，剩余 {RemainingCount} 道题目未能成功审核",
                            maxRetries, questionsToRetry.Count);

                        // 将剩余失败的题目添加到结果中
                        foreach (var failedItem in questionsToRetry)
                        {
                            failedItem.Question.AuditStatus = AuditStatus.Failed;
                            failedItem.Question.AuditMessage = $"AI审核失败(重试{maxRetries}次后仍未成功)";
                            successfulResults[failedItem.OriginalIndex] = failedItem.Question;
                        }
                        break;
                    }
                    else
                    {
                        _logger.LogInformation("第 {Attempt} 次尝试后，还有 {RemainingCount} 道题目需要重试",
                            currentAttempt, questionsToRetry.Count);

                        // 重试前等待一段时间
                        await Task.Delay(1000 * currentAttempt); // 递增延迟：1s, 2s
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "第 {Attempt}/{MaxAttempts} 次AI审核尝试失败",
                        currentAttempt, maxRetries + 1);

                    // 如果是最后一次尝试，将剩余题目标记为失败
                    if (currentAttempt > maxRetries)
                    {
                        _logger.LogError("所有重试尝试均失败，将剩余 {Count} 道题目标记为审核失败", questionsToRetry.Count);

                        foreach (var failedItem in questionsToRetry)
                        {
                            failedItem.Question.AuditStatus = AuditStatus.Failed;
                            failedItem.Question.AuditMessage = $"批量AI审核失败(重试{maxRetries}次): {ex.Message}";
                            successfulResults[failedItem.OriginalIndex] = failedItem.Question;
                        }
                        break;
                    }

                    // 重试前等待
                    await Task.Delay(2000 * currentAttempt); // 递增延迟：2s, 4s
                }
            }

            // 按原始顺序组装结果
            var orderedResults = new List<QuestionPreviewDto>();
            for (int i = 0; i < questions.Count; i++)
            {
                if (successfulResults.TryGetValue(i, out var result))
                {
                    orderedResults.Add(result);
                }
                else
                {
                    // 如果没找到结果，使用原始题目并标记为失败
                    var originalQuestion = questions[i];
                    originalQuestion.AuditStatus = AuditStatus.Failed;
                    originalQuestion.AuditMessage = "未找到审核结果";
                    orderedResults.Add(originalQuestion);
                }
            }

            return orderedResults;
        }

        /// <summary>
        /// 判断审核是否成功
        /// </summary>
        /// <param name="result">审核结果</param>
        /// <returns>是否成功</returns>
        private bool IsAuditSuccessful(QuestionPreviewDto result)
        {
            if (result == null)
                return false;

            // 如果状态是失败，直接返回false
            if (result.AuditStatus == AuditStatus.Failed)
                return false;

            // 如果状态是通过，但包含特定的失败消息，也认为是失败
            if (result.AuditStatus == AuditStatus.Passed &&
                !string.IsNullOrEmpty(result.AuditMessage) &&
                (result.AuditMessage.Contains("AI审核响应不完整") ||
                 result.AuditMessage.Contains("未找到对应的审核结果") ||
                 result.AuditMessage.Contains("应用审核结果失败")))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 构建批量审核的提示词
        /// </summary>
        /// <param name="questions">待审核的题目列表</param>
        /// <param name="autoCorrect">是否自动修正错误</param>
        /// <returns>批量审核提示词</returns>
        private string BuildBatchAuditPrompt(List<QuestionPreviewDto> questions, bool autoCorrect)
        {
            var prompt = $@"请批量审核以下 {questions.Count} 道题目的格式和内容，检查是否存在错误：

审核标准：
1. 题目内容不应包含序号、分值、选项、答案，应完整、清晰
2. 选项格式不应包含序号、ABCD等标记，多个选项应使用逗号分隔，选项不应重复
3. 正确答案是否与选项匹配，是否合理；如果正确答案使用序号或ABCD等标记，应修正为选项文本
4. 是否存在错别字或标点符号错误
5. 解析是否合理

{(autoCorrect ? @"如果发现错误，请自动修正并说明修正内容。

【严格修正规则 - 必须遵守】：
1. 【题目内容修正】：
   - 只能删除：明显的序号（如""1.""、""（1）""、""第1题""等）、分值标记（如""(5分)""）
   - 只能修正：明显的错别字和标点符号错误
   - 绝对禁止：删除题目的引导语（如""下列说法正确的是""、""以下哪项是""等）
   - 绝对禁止：修改题目的核心语义和表达方式
   - 绝对禁止：补充任何原题目中没有的内容

2. 【选项修正】：
   - 只能删除：选项前的序号标记（如""A.""、""①""等）
   - 只能修正：明显的错别字
   - 绝对禁止：修改选项的事实内容和语义
   - 绝对禁止：调整选项的逻辑关系

3. 【答案修正】：
   - 只能修正：将序号答案（如""A""、""①""）转换为对应的选项文本
   - 绝对禁止：修改答案的实际内容

【修正示例】：
✅ 正确修正：""1. 下列说法正确的是"" → ""下列说法正确的是""（删除序号）
❌ 错误修正：""下列说法正确的是"" → ""关于计算机的说法，正确的是""（修改语义）
✅ 正确修正：""A. 数据总线决定速度"" → ""数据总线决定速度""（删除选项标记）
❌ 错误修正：""PII/300比PII/350时钟频率高"" → ""PII/300的时钟频率低于PII/350""（修改事实）" : "如果发现错误，请指出错误但不要修正。")}

题目列表：
";

            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                prompt += $@"
题目 {i + 1}:
- 内容：{question.Content}
- 类型：{question.Type}
- 选项：{string.Join(", ", question.Options)}
- 正确答案：{question.CorrectAnswer}
- 难度：{question.Difficulty}
- 解析：{question.Analysis}
- 标签：{string.Join(", ", question.Tags)}
";
            }

            prompt += $@"

请以JSON格式返回审核结果，包含每道题目的审核信息：
{{
  ""results"": [
    {{
      ""questionIndex"": 0,
      ""hasErrors"": true/false,
      ""errors"": [""错误描述1"", ""错误描述2""],
      ""corrections"": [""修正说明1"", ""修正说明2""],
      ""correctedContent"": ""修正后的题目内容"",
      ""correctedOptions"": [""修正后的选项""],
      ""correctedAnswer"": ""修正后的答案"",
      ""correctedAnalysis"": ""修正后的解析""
    }}
  ]
}}

注意：
- questionIndex 从0开始，对应题目在列表中的位置
- 如果题目没有错误，hasErrors为false，corrections和corrected字段可以为空
- 如果不需要修正某个字段，对应的corrected字段可以为空或null
- 严格遵守上述修正规则，绝对不能修改题目的语义内容和事实信息
- 如果题目内容不完整但无法确定缺失内容，请在errors中说明问题，但correctedContent应为空或null
- 请确保返回的JSON格式正确，包含所有 {questions.Count} 道题目的审核结果";

            return prompt;
        }

        /// <summary>
        /// 解析批量审核的AI响应
        /// </summary>
        /// <param name="aiResponse">AI响应内容</param>
        /// <param name="originalQuestions">原始题目列表</param>
        /// <returns>审核后的题目列表</returns>
        private List<QuestionPreviewDto> ParseBatchAuditResponse(string aiResponse, List<QuestionPreviewDto> originalQuestions)
        {
            try
            {
                _logger.LogDebug("开始解析AI响应，原始响应长度: {Length}", aiResponse?.Length ?? 0);

                // 清理AI返回的结果，提取JSON内容
                var cleanedResponse = ExtractJsonFromAiResponse(aiResponse);
                _logger.LogDebug("清理后的JSON长度: {Length}", cleanedResponse?.Length ?? 0);

                // 验证JSON格式
                if (!IsValidJson(cleanedResponse))
                {
                    _logger.LogWarning("AI返回的JSON格式无效，尝试修复。清理后内容预览: {Preview}",
                        cleanedResponse.Length > 200 ? cleanedResponse.Substring(0, 200) + "..." : cleanedResponse);

                    var originalLength = cleanedResponse.Length;
                    cleanedResponse = TryFixJson(cleanedResponse);

                    _logger.LogDebug("JSON修复尝试完成，原长度: {OriginalLength}，修复后长度: {FixedLength}",
                        originalLength, cleanedResponse.Length);

                    if (!IsValidJson(cleanedResponse))
                    {
                        _logger.LogError("AI返回的JSON格式无效且无法修复，使用降级处理。修复后内容: {Content}",
                            cleanedResponse.Length > 500 ? cleanedResponse.Substring(0, 500) + "..." : cleanedResponse);
                        return CreateFailedAuditResults(originalQuestions, "AI返回的JSON格式无效且无法修复");
                    }
                    else
                    {
                        _logger.LogInformation("JSON修复成功，继续解析");
                    }
                }
                else
                {
                    _logger.LogDebug("AI返回的JSON格式有效，无需修复");
                }

                // 解析AI返回的结果
                var batchResult = JsonSerializer.Deserialize<JsonElement>(cleanedResponse);

                if (!batchResult.TryGetProperty("results", out var resultsElement))
                {
                    _logger.LogError("AI响应中缺少results字段，使用降级处理");
                    return CreateFailedAuditResults(originalQuestions, "AI响应中缺少results字段");
                }

                var results = resultsElement.EnumerateArray().ToList();
                _logger.LogInformation("成功解析AI响应，原始题目数量: {OriginalCount}，AI返回结果数量: {ResultCount}",
                    originalQuestions.Count, results.Count);

                // 检查AI返回的结果数量是否与原始题目数量匹配
                if (results.Count < originalQuestions.Count)
                {
                    var missingCount = originalQuestions.Count - results.Count;
                    var completionRate = (double)results.Count / originalQuestions.Count * 100;

                    _logger.LogWarning("AI返回的结果数量({ResultCount})少于原始题目数量({OriginalCount})，缺失{MissingCount}道题目，完成率{CompletionRate:F1}%",
                        results.Count, originalQuestions.Count, missingCount, completionRate);

                    // 记录缺失的题目索引
                    var existingIndices = results.Where(r => r.TryGetProperty("questionIndex", out var idx))
                                                .Select(r => r.GetProperty("questionIndex").GetInt32())
                                                .ToHashSet();

                    var missingIndices = Enumerable.Range(0, originalQuestions.Count)
                                                  .Where(i => !existingIndices.Contains(i))
                                                  .ToList();

                    _logger.LogWarning("缺失的题目索引: [{MissingIndices}]", string.Join(", ", missingIndices));

                    // 如果缺失题目过多（超过50%），建议减少批次大小
                    if (completionRate < 50)
                    {
                        _logger.LogError("AI响应完成率过低({CompletionRate:F1}%)，建议减少批次大小或检查AI模型配置", completionRate);
                    }
                }

                // 创建审核后的题目列表
                var auditedQuestions = new List<QuestionPreviewDto>();

                for (int i = 0; i < originalQuestions.Count; i++)
                {
                    var originalQuestion = originalQuestions[i];
                    _logger.LogDebug("处理题目 {Index}: {Content}", i,
                        originalQuestion.Content?.Length > 50 ? originalQuestion.Content.Substring(0, 50) + "..." : originalQuestion.Content);

                    var auditedQuestion = new QuestionPreviewDto
                    {
                        Content = originalQuestion.Content,
                        Type = originalQuestion.Type,
                        Options = new List<string>(originalQuestion.Options),
                        CorrectAnswer = originalQuestion.CorrectAnswer,
                        Analysis = originalQuestion.Analysis,
                        Difficulty = originalQuestion.Difficulty,
                        DefaultScore = originalQuestion.DefaultScore,
                        Tags = new List<string>(originalQuestion.Tags),
                        KnowledgePoints = originalQuestion.KnowledgePoints,
                        AuditStatus = AuditStatus.Passed
                    };

                    // 查找对应的审核结果
                    var auditResult = results.FirstOrDefault(r =>
                        r.TryGetProperty("questionIndex", out var indexProp) &&
                        indexProp.GetInt32() == i);

                    if (auditResult.ValueKind != JsonValueKind.Undefined)
                    {
                        _logger.LogDebug("找到题目 {Index} 的审核结果", i);
                        try
                        {
                            ApplyAuditResult(auditedQuestion, auditResult, autoCorrect: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "应用第 {Index} 道题目的审核结果失败", i);
                            auditedQuestion.AuditStatus = AuditStatus.Failed;
                            auditedQuestion.AuditMessage = $"应用审核结果失败: {ex.Message}";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("未找到题目 {Index} 的审核结果，可能是AI响应不完整", i);

                        // 如果没有找到对应的审核结果，根据情况处理
                        if (results.Count < originalQuestions.Count)
                        {
                            // AI返回结果不完整，标记为通过但添加警告信息
                            auditedQuestion.AuditStatus = AuditStatus.Passed;
                            auditedQuestion.AuditMessage = "AI审核响应不完整，未能获取详细审核结果，请人工复核";
                            _logger.LogDebug("题目 {Index} 因AI响应不完整标记为通过，需人工复核", i);
                        }
                        else
                        {
                            // AI返回了足够的结果但没有匹配到，可能是索引问题
                            auditedQuestion.AuditStatus = AuditStatus.Failed;
                            auditedQuestion.AuditMessage = "未找到对应的审核结果，可能存在索引匹配问题";
                            _logger.LogDebug("题目 {Index} 索引匹配失败，标记为审核失败", i);
                        }
                    }

                    auditedQuestions.Add(auditedQuestion);
                }

                // 统计审核结果
                var passedCount = auditedQuestions.Count(q => q.AuditStatus == AuditStatus.Passed);
                var failedCount = auditedQuestions.Count(q => q.AuditStatus == AuditStatus.Failed);
                var needsReviewCount = auditedQuestions.Count(q => q.AuditStatus == AuditStatus.Passed &&
                    !string.IsNullOrEmpty(q.AuditMessage) &&
                    q.AuditMessage.Contains("AI审核响应不完整"));
                var hasErrorsCount = auditedQuestions.Count(q => q.AuditStatus == AuditStatus.Passed &&
                    !string.IsNullOrEmpty(q.AuditMessage) &&
                    !q.AuditMessage.Contains("AI审核响应不完整"));

                _logger.LogInformation("批量AI审核完成统计 - 总计: {Total}, 通过: {Passed}, 失败: {Failed}, 有错误但已修正: {HasErrors}, 需人工复核: {NeedsReview}",
                    auditedQuestions.Count, passedCount, failedCount, hasErrorsCount, needsReviewCount);

                return auditedQuestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析批量AI审核响应失败，原始响应长度: {Length}，错误类型: {ExceptionType}",
                    aiResponse?.Length ?? 0, ex.GetType().Name);

                // 记录更详细的调试信息
                if (aiResponse != null && aiResponse.Length < 10000) // 避免记录过长的响应
                {
                    _logger.LogDebug("AI响应内容: {Response}", aiResponse);
                }

                // 解析失败时，将所有题目标记为审核失败
                var failedQuestions = originalQuestions.Select(q =>
                {
                    var failedQuestion = new QuestionPreviewDto
                    {
                        Content = q.Content,
                        Type = q.Type,
                        Options = new List<string>(q.Options),
                        CorrectAnswer = q.CorrectAnswer,
                        Analysis = q.Analysis,
                        Difficulty = q.Difficulty,
                        DefaultScore = q.DefaultScore,
                        Tags = new List<string>(q.Tags),
                        KnowledgePoints = q.KnowledgePoints,
                        AuditStatus = AuditStatus.Failed,
                        AuditMessage = $"解析AI审核响应失败: {ex.Message}"
                    };
                    return failedQuestion;
                }).ToList();

                return failedQuestions;
            }
        }

        /// <summary>
        /// 创建失败的审核结果
        /// </summary>
        /// <param name="originalQuestions">原始题目列表</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>标记为失败的题目列表</returns>
        private List<QuestionPreviewDto> CreateFailedAuditResults(List<QuestionPreviewDto> originalQuestions, string errorMessage)
        {
            _logger.LogDebug("创建失败的审核结果，题目数量: {Count}，错误: {Error}", originalQuestions.Count, errorMessage);

            return originalQuestions.Select(q => new QuestionPreviewDto
            {
                Content = q.Content,
                Type = q.Type,
                Options = new List<string>(q.Options),
                CorrectAnswer = q.CorrectAnswer,
                Analysis = q.Analysis,
                Difficulty = q.Difficulty,
                DefaultScore = q.DefaultScore,
                Tags = new List<string>(q.Tags),
                KnowledgePoints = q.KnowledgePoints,
                AuditStatus = AuditStatus.Failed,
                AuditMessage = errorMessage
            }).ToList();
        }

        /// <summary>
        /// 验证JSON格式是否有效
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>是否有效</returns>
        private bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试修复JSON格式
        /// </summary>
        /// <param name="json">原始JSON字符串</param>
        /// <returns>修复后的JSON字符串</returns>
        private string TryFixJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            try
            {
                _logger.LogDebug("开始JSON修复，原始长度: {Length}", json.Length);

                // 常见的JSON修复策略
                var fixedJson = json.Trim();

                // 1. 处理截断的JSON
                fixedJson = HandleTruncatedJson(fixedJson);

                // 2. 移除可能的非JSON前缀文本
                var jsonStart = fixedJson.IndexOf('{');
                if (jsonStart > 0)
                {
                    fixedJson = fixedJson.Substring(jsonStart);
                    _logger.LogDebug("移除JSON前缀文本，新起始位置: {Start}", jsonStart);
                }

                // 2. 查找最后一个完整的对象或数组结束位置
                var lastValidEnd = FindLastValidJsonEnd(fixedJson);
                if (lastValidEnd > 0 && lastValidEnd < fixedJson.Length - 1)
                {
                    fixedJson = fixedJson.Substring(0, lastValidEnd + 1);
                    _logger.LogDebug("截取到最后有效JSON位置: {End}", lastValidEnd);
                }

                // 3. 检查并修复括号平衡
                fixedJson = BalanceBrackets(fixedJson);

                // 4. 移除可能的尾随逗号
                var beforeCommaRemoval = fixedJson;
                fixedJson = System.Text.RegularExpressions.Regex.Replace(fixedJson, @",(\s*[}\]])", "$1");
                if (fixedJson != beforeCommaRemoval)
                {
                    _logger.LogDebug("移除了尾随逗号，修复前长度: {Before}，修复后长度: {After}",
                        beforeCommaRemoval.Length, fixedJson.Length);
                }

                // 5. 修复可能的字符串引号问题
                fixedJson = FixStringQuotes(fixedJson);

                // 6. 如果仍然无效，尝试提取部分有效的JSON
                if (!IsValidJson(fixedJson))
                {
                    fixedJson = ExtractPartialValidJson(fixedJson);
                }

                _logger.LogDebug("JSON修复完成，原长度: {OriginalLength}，修复后长度: {FixedLength}",
                    json.Length, fixedJson.Length);

                return fixedJson;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JSON修复失败");
                return json; // 返回原始JSON
            }
        }

        /// <summary>
        /// 查找最后一个有效的JSON结束位置
        /// </summary>
        private int FindLastValidJsonEnd(string json)
        {
            var braceCount = 0;
            var bracketCount = 0;
            var inString = false;
            var escapeNext = false;

            for (int i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString) continue;

                switch (c)
                {
                    case '{':
                        braceCount++;
                        break;
                    case '}':
                        braceCount--;
                        if (braceCount == 0 && bracketCount == 0)
                        {
                            return i; // 找到完整的JSON对象结束位置
                        }
                        break;
                    case '[':
                        bracketCount++;
                        break;
                    case ']':
                        bracketCount--;
                        break;
                }
            }

            return -1; // 没有找到完整的结束位置
        }

        /// <summary>
        /// 平衡括号
        /// </summary>
        private string BalanceBrackets(string json)
        {
            var openBraces = json.Count(c => c == '{');
            var closeBraces = json.Count(c => c == '}');
            var openBrackets = json.Count(c => c == '[');
            var closeBrackets = json.Count(c => c == ']');

            var result = json;

            // 补充缺少的结尾大括号
            while (openBraces > closeBraces)
            {
                result += "}";
                closeBraces++;
            }

            // 补充缺少的结尾方括号
            while (openBrackets > closeBrackets)
            {
                result += "]";
                closeBrackets++;
            }

            return result;
        }

        /// <summary>
        /// 处理截断的JSON
        /// </summary>
        private string HandleTruncatedJson(string json)
        {
            try
            {
                // 检查是否以不完整的内容结尾
                var trimmed = json.TrimEnd();
                _logger.LogDebug("检查JSON截断，原始长度: {Length}", trimmed.Length);

                // 常见的截断模式
                var truncationPatterns = new[]
                {
                    "...",           // 省略号
                    "，存在明显错别字",  // 从用户提供的例子看到的截断
                    "存在明显错别字",
                    "\"errors\": [",  // 数组开始但未完成
                    "\"corrections\":", // 字段开始但未完成
                    "],",            // 数组结束后有多余逗号
                    "},",            // 对象结束后有多余逗号
                    ",\"",           // 字段分隔符后截断
                    "\"",            // 单独的引号
                };

                foreach (var pattern in truncationPatterns)
                {
                    if (trimmed.EndsWith(pattern))
                    {
                        _logger.LogDebug("检测到截断模式: {Pattern}", pattern);

                        // 根据截断位置尝试修复
                        if (pattern == "..." || pattern.Contains("错别字"))
                        {
                            // 如果是在字符串值中截断，尝试闭合字符串
                            var lastQuoteIndex = trimmed.LastIndexOf('"');
                            var secondLastQuoteIndex = trimmed.LastIndexOf('"', lastQuoteIndex - 1);

                            if (lastQuoteIndex > secondLastQuoteIndex && secondLastQuoteIndex >= 0)
                            {
                                // 在字符串中截断，移除截断内容并添加闭合引号
                                var beforeTruncation = trimmed.Substring(0, lastQuoteIndex);
                                trimmed = beforeTruncation + "\"";
                                _logger.LogDebug("修复字符串截断，移除截断内容");
                            }
                        }
                        else if (pattern.Contains("["))
                        {
                            // 数组开始但未完成，添加空数组结束
                            trimmed += "]";
                            _logger.LogDebug("修复数组截断");
                        }
                        else if (pattern.Contains(":"))
                        {
                            // 字段开始但未完成，添加空值
                            trimmed += "null";
                            _logger.LogDebug("修复字段截断");
                        }
                        else if (pattern == "]," || pattern == "},")
                        {
                            // 数组或对象结束后有多余逗号，移除逗号
                            trimmed = trimmed.Substring(0, trimmed.Length - 1); // 移除最后的逗号
                            _logger.LogDebug("移除多余的逗号: {Pattern}", pattern);
                        }
                        else if (pattern == ",\"")
                        {
                            // 字段分隔符后截断，移除不完整的字段
                            var lastCommaIndex = trimmed.LastIndexOf(",\"");
                            if (lastCommaIndex >= 0)
                            {
                                trimmed = trimmed.Substring(0, lastCommaIndex);
                                _logger.LogDebug("移除不完整的字段");
                            }
                        }

                        break;
                    }
                }

                // 检查是否在字符串中间截断（没有配对的引号）
                var quoteCount = trimmed.Count(c => c == '"');
                if (quoteCount % 2 != 0)
                {
                    _logger.LogDebug("检测到奇数个引号，可能在字符串中截断");

                    // 奇数个引号，可能在字符串中截断
                    var lastQuoteIndex = trimmed.LastIndexOf('"');
                    if (lastQuoteIndex >= 0)
                    {
                        // 查找这个引号是否是字段名还是字符串值
                        var colonAfterQuote = trimmed.IndexOf(':', lastQuoteIndex);

                        if (colonAfterQuote < 0) // 没有冒号，可能是字符串值截断
                        {
                            // 查找前一个完整的字段结束位置
                            var beforeQuote = trimmed.Substring(0, lastQuoteIndex);
                            var lastCompleteField = Math.Max(beforeQuote.LastIndexOf(','), beforeQuote.LastIndexOf('{'));

                            if (lastCompleteField >= 0)
                            {
                                // 截断到最后一个完整字段
                                trimmed = trimmed.Substring(0, lastCompleteField);
                                _logger.LogDebug("截断到最后完整字段位置: {Position}", lastCompleteField);
                            }
                            else
                            {
                                // 添加闭合引号
                                trimmed += "\"";
                                _logger.LogDebug("添加闭合引号");
                            }
                        }
                    }
                }

                return trimmed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理截断JSON失败");
                return json;
            }
        }

        /// <summary>
        /// 修复字符串引号问题
        /// </summary>
        private string FixStringQuotes(string json)
        {
            try
            {
                var result = json;

                // 1. 修复未闭合的字符串引号
                result = FixUnclosedQuotes(result);

                // 2. 修复字段名缺少引号的问题
                result = FixUnquotedFieldNames(result);

                // 3. 修复值中的特殊字符
                result = FixSpecialCharactersInValues(result);

                _logger.LogDebug("字符串引号修复完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "字符串引号修复失败");
                return json;
            }
        }

        /// <summary>
        /// 修复未闭合的字符串引号
        /// </summary>
        private string FixUnclosedQuotes(string json)
        {
            var result = new System.Text.StringBuilder(json);
            var inString = false;
            var escapeNext = false;
            var lastQuoteIndex = -1;

            for (int i = 0; i < result.Length; i++)
            {
                var c = result[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    if (inString)
                    {
                        inString = false;
                    }
                    else
                    {
                        inString = true;
                        lastQuoteIndex = i;
                    }
                }
            }

            // 如果字符串没有闭合，在适当位置添加闭合引号
            if (inString && lastQuoteIndex >= 0)
            {
                // 查找下一个逗号、大括号或方括号作为字符串结束位置
                for (int i = lastQuoteIndex + 1; i < result.Length; i++)
                {
                    var c = result[i];
                    if (c == ',' || c == '}' || c == ']' || c == '\n' || c == '\r')
                    {
                        result.Insert(i, '"');
                        _logger.LogDebug("在位置 {Position} 添加闭合引号", i);
                        break;
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 修复字段名缺少引号的问题
        /// </summary>
        private string FixUnquotedFieldNames(string json)
        {
            // 使用正则表达式查找并修复未加引号的字段名
            var pattern = @"(\s*)([a-zA-Z_][a-zA-Z0-9_]*)\s*:";
            var replacement = "$1\"$2\":";

            var result = System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);

            if (result != json)
            {
                _logger.LogDebug("修复了未加引号的字段名");
            }

            return result;
        }

        /// <summary>
        /// 修复值中的特殊字符
        /// </summary>
        private string FixSpecialCharactersInValues(string json)
        {
            // 转义字符串值中的特殊字符
            var result = json;

            // 修复常见的转义字符问题
            result = result.Replace("\\n", "\\\\n");
            result = result.Replace("\\t", "\\\\t");
            result = result.Replace("\\r", "\\\\r");

            // 但不要重复转义已经正确转义的字符
            result = result.Replace("\\\\\\\\n", "\\\\n");
            result = result.Replace("\\\\\\\\t", "\\\\t");
            result = result.Replace("\\\\\\\\r", "\\\\r");

            return result;
        }

        /// <summary>
        /// 提取部分有效的JSON
        /// </summary>
        private string ExtractPartialValidJson(string json)
        {
            try
            {
                _logger.LogDebug("尝试提取部分有效JSON，原始长度: {Length}", json.Length);

                // 策略1: 尝试提取完整的results数组
                var partialJson = TryExtractResultsArray(json);
                if (!string.IsNullOrEmpty(partialJson) && IsValidJson(partialJson))
                {
                    _logger.LogDebug("成功提取完整的results数组");
                    return partialJson;
                }

                // 策略2: 尝试提取单个有效的result对象
                partialJson = TryExtractSingleResults(json);
                if (!string.IsNullOrEmpty(partialJson) && IsValidJson(partialJson))
                {
                    _logger.LogDebug("成功提取单个result对象");
                    return partialJson;
                }

                // 策略3: 尝试从片段中重构JSON
                partialJson = TryReconstructFromFragments(json);
                if (!string.IsNullOrEmpty(partialJson) && IsValidJson(partialJson))
                {
                    _logger.LogDebug("成功从片段重构JSON");
                    return partialJson;
                }

                // 最后的降级策略：返回空结构
                _logger.LogDebug("无法提取有效JSON，返回空结构");
                return "{\"results\":[]}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取部分JSON失败");
                return "{\"results\":[]}";
            }
        }

        /// <summary>
        /// 尝试提取results数组
        /// </summary>
        private string TryExtractResultsArray(string json)
        {
            try
            {
                var resultsStart = json.IndexOf("\"results\"");
                if (resultsStart < 0) return null;

                var colonIndex = json.IndexOf(':', resultsStart);
                if (colonIndex < 0) return null;

                var arrayStart = json.IndexOf('[', colonIndex);
                if (arrayStart < 0) return null;

                // 查找匹配的数组结束位置
                var bracketCount = 0;
                var inString = false;
                var escapeNext = false;
                var arrayEnd = -1;

                for (int i = arrayStart; i < json.Length; i++)
                {
                    var c = json[i];

                    if (escapeNext)
                    {
                        escapeNext = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escapeNext = true;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = !inString;
                        continue;
                    }

                    if (inString) continue;

                    if (c == '[')
                    {
                        bracketCount++;
                    }
                    else if (c == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            arrayEnd = i;
                            break;
                        }
                    }
                }

                if (arrayEnd > arrayStart)
                {
                    var arrayContent = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                    return "{\"results\":" + arrayContent + "}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试提取单个result对象
        /// </summary>
        private string TryExtractSingleResults(string json)
        {
            try
            {
                var results = new List<string>();
                var questionIndexPattern = @"""questionIndex""\s*:\s*(\d+)";
                var matches = System.Text.RegularExpressions.Regex.Matches(json, questionIndexPattern);

                _logger.LogDebug("在JSON中找到 {Count} 个questionIndex匹配项", matches.Count);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var startPos = match.Index;
                    var questionIndex = match.Groups[1].Value;

                    // 向前查找对象开始
                    var objStart = json.LastIndexOf('{', startPos);
                    if (objStart < 0)
                    {
                        _logger.LogDebug("questionIndex {Index} 找不到对象开始位置", questionIndex);
                        continue;
                    }

                    // 向后查找对象结束
                    var braceCount = 0;
                    var inString = false;
                    var escapeNext = false;
                    var objEnd = -1;

                    for (int i = objStart; i < json.Length; i++)
                    {
                        var c = json[i];

                        if (escapeNext)
                        {
                            escapeNext = false;
                            continue;
                        }

                        if (c == '\\')
                        {
                            escapeNext = true;
                            continue;
                        }

                        if (c == '"')
                        {
                            inString = !inString;
                            continue;
                        }

                        if (inString) continue;

                        if (c == '{')
                        {
                            braceCount++;
                        }
                        else if (c == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                objEnd = i;
                                break;
                            }
                        }
                    }

                    if (objEnd > objStart)
                    {
                        var objContent = json.Substring(objStart, objEnd - objStart + 1);
                        results.Add(objContent);
                        _logger.LogDebug("成功提取questionIndex {Index} 的对象，长度: {Length}", questionIndex, objContent.Length);
                    }
                    else
                    {
                        _logger.LogDebug("questionIndex {Index} 找不到对象结束位置", questionIndex);
                    }
                }

                if (results.Count > 0)
                {
                    var finalJson = "{\"results\":[" + string.Join(",", results) + "]}";
                    _logger.LogDebug("成功提取 {Count} 个result对象，总长度: {Length}", results.Count, finalJson.Length);
                    return finalJson;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取单个result对象失败");
                return null;
            }
        }

        /// <summary>
        /// 尝试从片段重构JSON
        /// </summary>
        private string TryReconstructFromFragments(string json)
        {
            try
            {
                // 查找所有可能的questionIndex
                var questionIndexPattern = @"""questionIndex""\s*:\s*(\d+)";
                var hasErrorsPattern = @"""hasErrors""\s*:\s*(true|false)";

                var indexMatches = System.Text.RegularExpressions.Regex.Matches(json, questionIndexPattern);
                var errorMatches = System.Text.RegularExpressions.Regex.Matches(json, hasErrorsPattern);

                if (indexMatches.Count > 0)
                {
                    var results = new List<string>();

                    for (int i = 0; i < indexMatches.Count; i++)
                    {
                        var index = indexMatches[i].Groups[1].Value;
                        var hasErrors = i < errorMatches.Count ? errorMatches[i].Groups[1].Value : "false";

                        var resultObj = $"{{\"questionIndex\":{index},\"hasErrors\":{hasErrors}}}";
                        results.Add(resultObj);
                    }

                    return "{\"results\":[" + string.Join(",", results) + "]}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 应用单个题目的审核结果
        /// </summary>
        /// <param name="question">题目对象</param>
        /// <param name="auditResult">审核结果</param>
        /// <param name="autoCorrect">是否自动修正</param>
        private void ApplyAuditResult(QuestionPreviewDto question, JsonElement auditResult, bool autoCorrect)
        {
            try
            {
                question.AuditStatus = AuditStatus.Passed;
                question.HasErrors = auditResult.GetProperty("hasErrors").GetBoolean();

                if (question.HasErrors)
                {
                    if (auditResult.TryGetProperty("errors", out var errorsElement))
                    {
                        var errors = errorsElement.EnumerateArray()
                            .Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                        question.AuditMessage = string.Join("; ", errors);
                    }

                    if (autoCorrect)
                    {
                        // 保存原始内容（在修正前）
                        question.OriginalContent = question.Content;
                        question.OriginalOptions = new List<string>(question.Options);
                        question.OriginalAnswer = question.CorrectAnswer;
                        question.OriginalAnalysis = question.Analysis;

                        // 应用AI修正
                        if (auditResult.TryGetProperty("correctedContent", out var correctedContent))
                        {
                            var correctedContentStr = correctedContent.GetString();
                            if (!string.IsNullOrEmpty(correctedContentStr))
                            {
                                question.Content = correctedContentStr;
                                question.IsCorrected = true;
                            }
                        }

                        if (auditResult.TryGetProperty("correctedOptions", out var correctedOptions))
                        {
                            var optionsList = correctedOptions.EnumerateArray()
                                .Select(o => o.GetString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                            if (optionsList.Any())
                            {
                                question.Options = optionsList;
                                question.IsCorrected = true;
                            }
                        }

                        if (auditResult.TryGetProperty("correctedAnswer", out var correctedAnswer))
                        {
                            var answerStr = correctedAnswer.GetString();
                            if (!string.IsNullOrEmpty(answerStr))
                            {
                                question.CorrectAnswer = answerStr;
                                question.IsCorrected = true;
                            }
                        }

                        if (auditResult.TryGetProperty("correctedAnalysis", out var correctedAnalysis))
                        {
                            var analysisStr = correctedAnalysis.GetString();
                            if (!string.IsNullOrEmpty(analysisStr))
                            {
                                question.Analysis = analysisStr;
                                question.IsCorrected = true;
                            }
                        }

                        if (auditResult.TryGetProperty("corrections", out var corrections))
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用审核结果失败");
                question.AuditStatus = AuditStatus.Failed;
                question.AuditMessage = $"应用审核结果失败: {ex.Message}";
            }
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


{(autoCorrect ? @"如果发现错误，请自动修正并说明修正内容。

【严格修正规则 - 必须遵守】：
1. 【题目内容修正】：
   - 只能删除：明显的序号（如""1.""、""（1）""、""第1题""等）、分值标记（如""(5分)""）
   - 只能修正：明显的错别字和标点符号错误
   - 绝对禁止：删除题目的引导语（如""下列说法正确的是""、""以下哪项是""等）
   - 绝对禁止：修改题目的核心语义和表达方式
   - 绝对禁止：补充任何原题目中没有的内容

2. 【选项修正】：
   - 只能删除：选项前的序号标记（如""A.""、""①""等）
   - 只能修正：明显的错别字,避免重复选项
   - 绝对禁止：修改选项的事实内容和语义
   - 绝对禁止：调整选项的逻辑关系

3. 【答案修正】：
   - 只能修正：将序号答案（如""A""、""①""）转换为对应的选项文本
   - 绝对禁止：修改答案的实际内容

【修正示例】：
✅ 正确修正：""1. 下列说法正确的是"" → ""下列说法正确的是""（删除序号）
❌ 错误修正：""下列说法正确的是"" → ""关于计算机的说法，正确的是""（修改语义）
✅ 正确修正：""A. 数据总线决定速度"" → ""数据总线决定速度""（删除选项标记）
❌ 错误修正：""PII/300比PII/350时钟频率高"" → ""PII/300的时钟频率低于PII/350""（修改事实）" : "如果发现错误，请指出错误但不要修正。")}

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

            // 根据设置验证题目内容唯一性
            var settings = await GetQuestionSettingsAsync();
            var contentToValidate = createDto.Content?.Trim();
            if (!string.IsNullOrWhiteSpace(contentToValidate))
            {
                await ValidateQuestionUniquenessAsync(
                    new List<string> { contentToValidate },
                    createDto.CategoryId,
                    settings.UniquenessMode);
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
                Version = await GetNextVersionNumberAsync(question.Id),
                Content = question.Content,
                Options = question.Options,
                CorrectAnswer = question.CorrectAnswer,
                Analysis = question.Analysis,
                DefaultScore = question.DefaultScore,
                Tags = question.Tags,
                KnowledgePoints = question.KnowledgePoints,
                ChangeReason = "创建题目",
                TenantId = question.TenantId,
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
                    Version = await GetNextVersionNumberAsync(question.Id),
                    Content = question.Content,
                    Options = question.Options,
                    CorrectAnswer = question.CorrectAnswer,
                    Analysis = question.Analysis,
                    DefaultScore = question.DefaultScore,
                    Tags = question.Tags,
                    KnowledgePoints = question.KnowledgePoints,
                    ChangeReason = "更新题目",
                    TenantId = question.TenantId,
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

        /// <summary>
        /// 获取题目的下一个版本号
        /// </summary>
        /// <param name="questionId">题目ID</param>
        /// <returns>下一个版本号</returns>
        private async Task<int> GetNextVersionNumberAsync(long questionId)
        {
            var maxVersion = await _versionRepository.CreateQuery()
                .Where(v => v.QuestionId == questionId)
                .MaxAsync(v => (int?)v.Version);

            return (maxVersion ?? -1) + 1;
        }

        public async Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<QuestionBatchImportItemDto> items)
        {
            var itemsList = items.ToList();
            var successCount = 0;
            var failedIds = new List<string>();

            // 重复验证逻辑
            await ValidateBatchImportItemsAsync(itemsList);

            foreach (var item in itemsList)
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

        /// <summary>
        /// 从文本导入题目（简化版本，直接解析并导入）
        /// </summary>
        /// <param name="input">导入请求</param>
        /// <returns>导入结果：成功数量和失败项目列表</returns>
        public async Task<(int successCount, List<string> failedItems)> ImportFromTextAsync(QuestionImportFromTextDto input)
        {
            ArgumentNullException.ThrowIfNull(input);
            
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                throw new AppServiceException(400, "题目文本内容不能为空！");
            }

            // 验证分类是否存在
            var category = await _categoryRepository.GetByIdAsync(input.CategoryId);
            if (category == null)
            {
                throw new AppServiceException(400, "所选分类不存在！");
            }

            var successCount = 0;
            var failedItems = new List<string>();

            try
            {
                _logger.LogInformation("开始从文本导入题目，分类：{CategoryName}({CategoryId})", category.Name, input.CategoryId);

                // 使用文本解析器解析题目
                var parseResults = _questionTextParserV2.Parse(input.Text);
                _logger.LogInformation("解析到 {Count} 个题目", parseResults.Count);

                if (!parseResults.Any())
                {
                    return (0, new List<string> { "未能从文本中解析到任何题目，请检查文本格式是否正确" });
                }

                // 逐个导入题目
                for (int i = 0; i < parseResults.Count; i++)
                {
                    var parsedQuestion = parseResults[i];
                    try
                    {
                        // 转换为创建题目DTO
                        var createDto = new CreateQuestionDto
                        {
                            Content = parsedQuestion.Content,
                            Type = parsedQuestion.Type,
                            Options = parsedQuestion.Options ?? new List<string>(),
                            CorrectAnswer = parsedQuestion.CorrectAnswer,
                            Analysis = parsedQuestion.Analysis,
                            Difficulty = parsedQuestion.Difficulty,
                            DefaultScore = (int)parsedQuestion.Score,
                            CategoryId = input.CategoryId,
                            Tags = parsedQuestion.Tags ?? new List<string>()
                        };

                        // 创建题目
                        await CreateQuestionAsync(createDto);
                        successCount++;

                        _logger.LogDebug("成功导入第 {Index}/{Total} 个题目: {Content}", 
                            i + 1, parseResults.Count, parsedQuestion.Content.Substring(0, Math.Min(50, parsedQuestion.Content.Length)));
                    }
                    catch (Exception ex)
                    {
                        var questionPreview = parsedQuestion.Content.Length > 30 
                            ? parsedQuestion.Content.Substring(0, 30) + "..." 
                            : parsedQuestion.Content;
                        
                        var errorMessage = $"题目 [{questionPreview}] 导入失败: {ex.Message}";
                        failedItems.Add(errorMessage);
                        
                        _logger.LogError(ex, "导入第 {Index}/{Total} 个题目失败: {Content}", 
                            i + 1, parseResults.Count, parsedQuestion.Content.Substring(0, Math.Min(50, parsedQuestion.Content.Length)));
                    }
                }

                _logger.LogInformation("文本导入完成: 成功 {SuccessCount} 个，失败 {FailedCount} 个", successCount, failedItems.Count);
                
                if (failedItems.Any())
                {
                    _logger.LogWarning("以下题目导入失败: {FailedItems}", string.Join("; ", failedItems));
                }

                return (successCount, failedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从文本导入题目时发生错误");
                throw new AppServiceException(500, $"导入题目失败: {ex.Message}");
            }
        }

        public async Task<QuestionSettingsDto> GetQuestionSettingsAsync()
        {
            // 从设置服务获取题目唯一性校验模式
            var uniquenessModeString = await _settingsService.GetGlobalSettingAsync("Question", "UniquenessMode");
            
            // 解析枚举值，默认为分类唯一
            var uniquenessMode = QuestionUniquenessMode.Category;
            if (!string.IsNullOrEmpty(uniquenessModeString) && 
                Enum.TryParse<QuestionUniquenessMode>(uniquenessModeString, out var parsedMode))
            {
                uniquenessMode = parsedMode;
            }

            return new QuestionSettingsDto
            {
                UniquenessMode = uniquenessMode
            };
        }

        public async Task<bool> UpdateQuestionSettingsAsync(QuestionSettingsDto settings)
        {
            // 保存题目唯一性校验模式设置
            await _settingsService.SetGlobalSettingAsync(
                "Question", 
                "UniquenessMode", 
                settings.UniquenessMode.ToString());

            return true;
        }

        /// <summary>
        /// 验证导入题目的重复性
        /// </summary>
        /// <param name="questionsToImport">要导入的题目列表</param>
        /// <param name="categoryId">分类ID</param>
        /// <returns>验证任务</returns>
        private async Task ValidateImportQuestionsAsync(List<QuestionPreviewDto> questionsToImport, long categoryId)
        {
            // 获取唯一性校验模式
            var settings = await GetQuestionSettingsAsync();
            
            // 根据设置进行验证
            await ValidateQuestionUniquenessAsync(
                questionsToImport.Select(q => q.Content?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList(),
                categoryId,
                settings.UniquenessMode);
        }

        /// <summary>
        /// 验证批量导入项的重复性
        /// </summary>
        /// <param name="items">批量导入项列表</param>
        /// <returns>验证任务</returns>
        private async Task ValidateBatchImportItemsAsync(List<QuestionBatchImportItemDto> items)
        {
            // 获取唯一性校验模式
            var settings = await GetQuestionSettingsAsync();
            
            // 根据设置进行验证（注意：QuestionBatchImportItemDto没有CategoryId，所以使用null）
            await ValidateQuestionUniquenessAsync(
                items.Select(q => q.Content?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList(),
                null,
                settings.UniquenessMode);
        }

        /// <summary>
        /// 根据唯一性校验模式验证题目内容的重复性
        /// </summary>
        /// <param name="contentList">要验证的题目内容列表</param>
        /// <param name="categoryId">分类ID（可为null）</param>
        /// <param name="uniquenessMode">唯一性校验模式</param>
        /// <returns>验证任务</returns>
        private async Task ValidateQuestionUniquenessAsync(List<string> contentList, long? categoryId, QuestionUniquenessMode uniquenessMode)
        {
            if (!contentList.Any())
                return;

            // 1. 检查导入数据内部重复
            var duplicateContents = contentList
                .GroupBy(c => c)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateContents.Any())
            {
                var duplicateList = string.Join("、", duplicateContents.Select(c => 
                    c.Length > 50 ? c.Substring(0, 50) + "..." : c));
                throw new AppServiceException(400, $"导入数据中存在重复的题目内容：{duplicateList}");
            }

            // 2. 根据唯一性校验模式检查与数据库现有题目的重复
            switch (uniquenessMode)
            {
                case QuestionUniquenessMode.None:
                    // 不校验唯一性，直接返回
                    return;

                case QuestionUniquenessMode.Global:
                    // 全局唯一校验
                    await ValidateGlobalUniquenessAsync(contentList);
                    break;

                case QuestionUniquenessMode.Category:
                    // 分类唯一校验
                    if (categoryId.HasValue)
                    {
                        await ValidateCategoryUniquenessAsync(contentList, categoryId.Value);
                    }
                    else
                    {
                        // 如果没有分类ID，则按全局唯一校验
                        await ValidateGlobalUniquenessAsync(contentList);
                    }
                    break;

                default:
                    throw new AppServiceException(400, "不支持的唯一性校验模式");
            }
        }

        /// <summary>
        /// 验证全局唯一性
        /// </summary>
        /// <param name="contentList">题目内容列表</param>
        /// <returns>验证任务</returns>
        private async Task ValidateGlobalUniquenessAsync(List<string> contentList)
        {
            var existingQuestions = await _repository.CreateQuery()
                .Where(q => contentList.Contains(q.Content))
                .Select(q => q.Content)
                .ToListAsync();

            if (existingQuestions.Any())
            {
                var existingList = string.Join("、", existingQuestions.Select(c => 
                    c.Length > 50 ? c.Substring(0, 50) + "..." : c));
                throw new AppServiceException(400, $"以下题目内容已存在于系统中：{existingList}");
            }
        }

        /// <summary>
        /// 验证分类唯一性
        /// </summary>
        /// <param name="contentList">题目内容列表</param>
        /// <param name="categoryId">分类ID</param>
        /// <returns>验证任务</returns>
        private async Task ValidateCategoryUniquenessAsync(List<string> contentList, long categoryId)
        {
            var existingQuestions = await _repository.CreateQuery()
                .Where(q => q.CategoryId == categoryId && contentList.Contains(q.Content))
                .Select(q => q.Content)
                .ToListAsync();

            if (existingQuestions.Any())
            {
                var existingList = string.Join("、", existingQuestions.Select(c => 
                    c.Length > 50 ? c.Substring(0, 50) + "..." : c));
                throw new AppServiceException(400, $"以下题目内容已存在于该分类中：{existingList}");
            }
        }
    }
}
