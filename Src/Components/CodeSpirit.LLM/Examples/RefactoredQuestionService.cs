using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM.Examples;

/// <summary>
/// 重构后的题目服务示例
/// 展示如何使用新的LLM组件能力
/// </summary>
public class RefactoredQuestionService
{
    private readonly LLMAssistant _llmAssistant;
    private readonly ILogger<RefactoredQuestionService> _logger;

    /// <summary>
    /// 初始化重构后的题目服务
    /// </summary>
    /// <param name="llmAssistant">LLM助手</param>
    /// <param name="logger">日志记录器</param>
    public RefactoredQuestionService(
        LLMAssistant llmAssistant,
        ILogger<RefactoredQuestionService> logger)
    {
        _llmAssistant = llmAssistant;
        _logger = logger;
    }

    /// <summary>
    /// 批量审核题目（使用新的LLM组件能力）
    /// </summary>
    /// <param name="questions">待审核的题目列表</param>
    /// <param name="autoCorrect">是否自动修正错误</param>
    /// <returns>审核后的题目列表</returns>
    public async Task<List<QuestionPreviewDto>> BatchAuditQuestionsAsync(
        List<QuestionPreviewDto> questions, 
        bool autoCorrect = true)
    {
        if (questions == null || !questions.Any())
        {
            return new List<QuestionPreviewDto>();
        }

        _logger.LogInformation("开始批量审核 {Count} 道题目，自动修正: {AutoCorrect}", 
            questions.Count, autoCorrect);

        try
        {
            // 使用模板处理结构化任务
            var templateParameters = new
            {
                questions = questions,
                autoCorrect = autoCorrect
            };

            var taskOptions = new StructuredTaskOptions
            {
                EnableRetry = true,
                MaxRetries = 2,
                RetryDelay = TimeSpan.FromSeconds(1)
            };

            var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<BatchAuditResult>(
                "question_batch_audit", 
                templateParameters, 
                taskOptions);

            if (result.IsSuccess && result.Result != null)
            {
                _logger.LogInformation("批量审核成功，处理 {Count} 道题目，耗时 {Duration}ms，是否修复JSON: {WasRepaired}",
                    questions.Count, result.Duration.TotalMilliseconds, result.WasRepaired);

                // 应用审核结果到原始题目
                return ApplyAuditResults(questions, result.Result);
            }
            else
            {
                _logger.LogWarning("批量审核失败，错误: {Errors}", string.Join("; ", result.Errors));
                
                // 返回标记为审核失败的题目
                return questions.Select(q =>
                {
                    q.AuditStatus = AuditStatus.Failed;
                    q.AuditMessage = $"批量审核失败: {string.Join("; ", result.Errors)}";
                    return q;
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量审核题目时发生异常");
            
            // 返回标记为审核失败的题目
            return questions.Select(q =>
            {
                q.AuditStatus = AuditStatus.Failed;
                q.AuditMessage = $"审核异常: {ex.Message}";
                return q;
            }).ToList();
        }
    }

    /// <summary>
    /// 自动分批审核题目（支持大量题目的自动分批处理）
    /// </summary>
    /// <param name="questions">待审核的题目列表</param>
    /// <param name="autoCorrect">是否自动修正错误</param>
    /// <returns>审核后的题目列表</returns>
    public async Task<List<QuestionPreviewDto>> BatchAuditQuestionsWithAutoSplitAsync(
        List<QuestionPreviewDto> questions, 
        bool autoCorrect = true)
    {
        if (questions == null || !questions.Any())
        {
            return new List<QuestionPreviewDto>();
        }

        _logger.LogInformation("开始自动分批审核 {Count} 道题目", questions.Count);

        try
        {
            // 定义提示词生成器
            string PromptGenerator(List<QuestionPreviewDto> batch)
            {
                var templateParameters = new
                {
                    questions = batch,
                    autoCorrect = autoCorrect
                };

                // 这里简化处理，实际应该使用PromptBuilder
                return $"请审核以下 {batch.Count} 道题目..."; // 简化示例
            }

            // 使用批量处理能力
            var batchOptions = new Processors.BatchProcessingOptions
            {
                BatchSize = 10, // 每批10道题目
                MaxRetries = 2,
                DelayBetweenBatches = TimeSpan.FromSeconds(1),
                ContinueOnFailure = true
            };

            var batchResult = await _llmAssistant.ProcessBatchStructuredTaskAsync<QuestionPreviewDto, BatchAuditResult>(
                questions, 
                PromptGenerator, 
                batchOptions);

            _logger.LogInformation("自动分批审核完成：总计 {TotalItems} 道题目，成功 {SuccessCount} 个批次，失败 {FailedCount} 个批次，耗时 {Duration}ms",
                batchResult.TotalItems, batchResult.SuccessBatches, batchResult.FailedBatchCount, batchResult.Duration.TotalMilliseconds);

            // 处理成功的结果
            var allAuditedQuestions = new List<QuestionPreviewDto>();
            foreach (var successResult in batchResult.SuccessResults)
            {
                if (successResult.IsSuccess && successResult.Result != null)
                {
                    // 这里需要根据实际的批次对应关系来应用结果
                    // 简化示例中直接返回原始题目
                    allAuditedQuestions.AddRange(questions.Take(10)); // 简化处理
                }
            }

            // 处理失败的批次
            foreach (var failedBatch in batchResult.FailedBatches)
            {
                _logger.LogWarning("批次 {BatchIndex} 处理失败: {Error}", 
                    failedBatch.BatchIndex, failedBatch.ErrorMessage);
                
                // 将失败批次的题目标记为审核失败
                var failedQuestions = questions
                    .Skip(failedBatch.BatchIndex * batchOptions.BatchSize)
                    .Take(failedBatch.BatchSize)
                    .Select(q =>
                    {
                        q.AuditStatus = AuditStatus.Failed;
                        q.AuditMessage = $"批次审核失败: {failedBatch.ErrorMessage}";
                        return q;
                    }).ToList();
                
                allAuditedQuestions.AddRange(failedQuestions);
            }

            return allAuditedQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动分批审核题目时发生异常");
            throw;
        }
    }

    /// <summary>
    /// 单个题目审核
    /// </summary>
    /// <param name="question">待审核的题目</param>
    /// <param name="autoCorrect">是否自动修正错误</param>
    /// <returns>审核后的题目</returns>
    public async Task<QuestionPreviewDto> AuditSingleQuestionAsync(
        QuestionPreviewDto question, 
        bool autoCorrect = true)
    {
        _logger.LogDebug("开始审核单个题目: {Content}", 
            question.Content?.Length > 50 ? question.Content.Substring(0, 50) + "..." : question.Content);

        try
        {
            var templateParameters = new
            {
                Content = question.Content,
                Type = question.Type,
                Options = string.Join(", ", question.Options),
                CorrectAnswer = question.CorrectAnswer,
                Difficulty = question.Difficulty,
                Analysis = question.Analysis,
                Tags = string.Join(", ", question.Tags),
                autoCorrect = autoCorrect
            };

            var result = await _llmAssistant.ProcessStructuredTaskWithTemplateAsync<QuestionAuditResult>(
                "question_single_audit", 
                templateParameters);

            if (result.IsSuccess && result.Result != null)
            {
                return ApplySingleAuditResult(question, result.Result);
            }
            else
            {
                question.AuditStatus = AuditStatus.Failed;
                question.AuditMessage = $"单个审核失败: {string.Join("; ", result.Errors)}";
                return question;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审核单个题目时发生异常");
            question.AuditStatus = AuditStatus.Failed;
            question.AuditMessage = $"审核异常: {ex.Message}";
            return question;
        }
    }

    /// <summary>
    /// 应用批量审核结果到原始题目
    /// </summary>
    /// <param name="originalQuestions">原始题目列表</param>
    /// <param name="auditResult">审核结果</param>
    /// <returns>应用结果后的题目列表</returns>
    private List<QuestionPreviewDto> ApplyAuditResults(
        List<QuestionPreviewDto> originalQuestions, 
        BatchAuditResult auditResult)
    {
        var auditedQuestions = new List<QuestionPreviewDto>();

        for (int i = 0; i < originalQuestions.Count; i++)
        {
            var originalQuestion = originalQuestions[i];
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
            var auditResultItem = auditResult.Results.FirstOrDefault(r => r.QuestionIndex == i);
            if (auditResultItem != null)
            {
                auditedQuestion = ApplySingleAuditResult(auditedQuestion, auditResultItem);
            }
            else
            {
                auditedQuestion.AuditStatus = AuditStatus.Failed;
                auditedQuestion.AuditMessage = "未找到对应的审核结果";
            }

            auditedQuestions.Add(auditedQuestion);
        }

        return auditedQuestions;
    }

    /// <summary>
    /// 应用单个审核结果
    /// </summary>
    /// <param name="question">原始题目</param>
    /// <param name="auditResult">审核结果</param>
    /// <returns>应用结果后的题目</returns>
    private QuestionPreviewDto ApplySingleAuditResult(
        QuestionPreviewDto question, 
        QuestionAuditResult auditResult)
    {
        question.HasErrors = auditResult.HasErrors;
        question.AuditStatus = AuditStatus.Passed;

        if (auditResult.HasErrors)
        {
            question.AuditMessage = string.Join("; ", auditResult.Errors);

            // 如果有修正内容，应用修正
            if (!string.IsNullOrEmpty(auditResult.CorrectedContent))
            {
                question.OriginalContent = question.Content;
                question.Content = auditResult.CorrectedContent;
                question.IsCorrected = true;
            }

            if (auditResult.CorrectedOptions?.Any() == true)
            {
                question.OriginalOptions = new List<string>(question.Options);
                question.Options = auditResult.CorrectedOptions;
                question.IsCorrected = true;
            }

            if (!string.IsNullOrEmpty(auditResult.CorrectedAnswer))
            {
                question.OriginalAnswer = question.CorrectAnswer;
                question.CorrectAnswer = auditResult.CorrectedAnswer;
                question.IsCorrected = true;
            }

            if (!string.IsNullOrEmpty(auditResult.CorrectedAnalysis))
            {
                question.OriginalAnalysis = question.Analysis;
                question.Analysis = auditResult.CorrectedAnalysis;
                question.IsCorrected = true;
            }

            if (auditResult.Corrections?.Any() == true)
            {
                question.CorrectionNotes = auditResult.Corrections;
                question.AuditMessage += $" [已修正: {string.Join("; ", auditResult.Corrections)}]";
            }
        }
        else
        {
            question.AuditMessage = "题目格式和内容正确";
        }

        return question;
    }
}
