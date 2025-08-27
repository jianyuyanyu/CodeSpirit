using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.LLM;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷LLM生成服务实现
/// </summary>
public class SurveyLLMGeneratorService : ISurveyLLMGeneratorService, IScopedDependency
{
    private readonly LLMAssistant _llmAssistant;
    private readonly ISurveySettingsService _settingsService;
    private readonly ISurveyService _surveyService;
    private readonly IQuestionService _questionService;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyLLMGeneratorService> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="llmAssistant">LLM助手</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="surveyService">问卷服务</param>
    /// <param name="questionService">题目服务</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    /// <param name="currentUser">当前用户</param>
    public SurveyLLMGeneratorService(
        LLMAssistant llmAssistant,
        ISurveySettingsService settingsService,
        ISurveyService surveyService,
        IQuestionService questionService,
        IMapper mapper,
        ILogger<SurveyLLMGeneratorService> logger,
        ICurrentUser currentUser)
    {
        _llmAssistant = llmAssistant;
        _settingsService = settingsService;
        _surveyService = surveyService;
        _questionService = questionService;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 根据主题生成问卷
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成的问卷</returns>
    public async Task<GeneratedSurveyDto> GenerateSurveyAsync(GenerateSurveyRequest request)
    {
        try
        {
            _logger.LogInformation("开始生成问卷，主题：{Topic}", request.Topic);

            // 获取LLM设置
            var llmSettings = await _settingsService.GetLLMSettingsAsync();

            // 构建提示词
            var prompt = await BuildSurveyGenerationPromptAsync(request);

            // 验证提示词长度
            var validationResult = await ValidatePromptAsync(prompt);
            if (!validationResult.IsValid)
            {
                if (validationResult.NeedsCompression)
                {
                    prompt = await CompressPromptAsync(prompt, llmSettings.MaxPromptLength);
                }
                else
                {
                    throw new BusinessException($"提示词验证失败：{validationResult.Message}");
                }
            }

            // 调用LLM生成内容
            var llmResponse = await _llmAssistant.GenerateContentAsync(prompt, llmSettings.MaxTokens);

            // 解析LLM响应
            var generatedSurvey = await ParseLLMResponseAsync(llmResponse, request, prompt);

            _logger.LogInformation("问卷生成成功，题目数量：{QuestionCount}", generatedSurvey.Questions.Count);

            // 保存生成的问卷及题目
            var savedSurvey = await SaveGeneratedSurveyAsync(generatedSurvey, request, llmResponse);
            
            // 更新生成结果，添加保存后的问卷ID
            generatedSurvey.SavedSurveyId = savedSurvey.Id;

            return generatedSurvey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成问卷失败，主题：{Topic}", request.Topic);
            throw new BusinessException($"生成问卷失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 优化现有问卷
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="optimizationGoals">优化目标</param>
    /// <returns>优化建议</returns>
    public async Task<SurveyOptimizationResult> OptimizeSurveyAsync(int surveyId, string optimizationGoals)
    {
        try
        {
            _logger.LogInformation("开始优化问卷 {SurveyId}，优化目标：{Goals}", surveyId, optimizationGoals);

            // TODO: 获取现有问卷数据
            // var survey = await _surveyService.GetAsync(surveyId);

            // 构建优化提示词
            var prompt = BuildOptimizationPrompt(surveyId, optimizationGoals);

            // 调用LLM生成优化建议
            var llmResponse = await _llmAssistant.GenerateContentAsync(prompt);

            // 解析优化建议
            var result = ParseOptimizationResponse(llmResponse);

            _logger.LogInformation("问卷优化完成，建议数量：{SuggestionCount}", result.Suggestions.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化问卷失败，ID：{SurveyId}", surveyId);
            throw new BusinessException($"优化问卷失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>洞察分析结果</returns>
    public async Task<SurveyInsightResult> GenerateInsightsAsync(int surveyId)
    {
        try
        {
            _logger.LogInformation("开始生成问卷洞察分析 {SurveyId}", surveyId);

            // 检查是否启用洞察功能
            var llmSettings = await _settingsService.GetLLMSettingsAsync();
            if (!llmSettings.EnableInsights)
            {
                throw new BusinessException("LLM洞察分析功能已禁用");
            }

            // TODO: 获取问卷数据和回答数据
            // var survey = await _surveyService.GetAsync(surveyId);
            // var responses = await _responseService.GetResponsesAsync(surveyId);

            // 构建洞察分析提示词
            var prompt = BuildInsightPrompt(surveyId);

            // 调用LLM生成洞察
            var llmResponse = await _llmAssistant.GenerateContentAsync(prompt);

            // 解析洞察结果
            var result = ParseInsightResponse(llmResponse);

            _logger.LogInformation("问卷洞察分析完成，洞察数量：{InsightCount}", result.Insights.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成问卷洞察失败，ID：{SurveyId}", surveyId);
            throw new BusinessException($"生成洞察失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证提示词长度
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <returns>验证结果</returns>
    public async Task<PromptValidationResult> ValidatePromptAsync(string prompt)
    {
        if (string.IsNullOrEmpty(prompt))
        {
            return new PromptValidationResult
            {
                IsValid = false,
                Length = 0,
                EstimatedTokens = 0,
                Message = "提示词不能为空"
            };
        }

        var llmSettings = await _settingsService.GetLLMSettingsAsync();
        var length = prompt.Length;
        var estimatedTokens = EstimateTokenCount(prompt);

        var result = new PromptValidationResult
        {
            Length = length,
            EstimatedTokens = estimatedTokens
        };

        if (length > llmSettings.MaxPromptLength)
        {
            result.IsValid = false;
            result.NeedsCompression = true;
            result.Message = $"提示词长度超过限制（{length} > {llmSettings.MaxPromptLength}），建议压缩";
        }
        else if (estimatedTokens > llmSettings.MaxTokens * 0.8) // 预留20%的Token给响应
        {
            result.IsValid = false;
            result.NeedsCompression = true;
            result.Message = $"预估Token数过多（{estimatedTokens} > {llmSettings.MaxTokens * 0.8}），建议压缩";
        }
        else
        {
            result.IsValid = true;
            result.Message = "提示词验证通过";
        }

        return result;
    }

    /// <summary>
    /// 压缩提示词
    /// </summary>
    /// <param name="prompt">原始提示词</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>压缩后的提示词</returns>
    public Task<string> CompressPromptAsync(string prompt, int maxLength)
    {
        if (prompt.Length <= maxLength)
        {
            return Task.FromResult(prompt);
        }

        _logger.LogInformation("开始压缩提示词，原长度：{OriginalLength}，目标长度：{MaxLength}", 
            prompt.Length, maxLength);

        // 简单的压缩策略：移除多余的空白字符和换行
        var compressed = Regex.Replace(prompt, @"\s+", " ").Trim();
        
        // 如果还是太长，截断到最大长度
        if (compressed.Length > maxLength)
        {
            compressed = compressed.Substring(0, maxLength - 3) + "...";
        }

        _logger.LogInformation("提示词压缩完成，压缩后长度：{CompressedLength}", compressed.Length);

        return Task.FromResult(compressed);
    }

    #region 私有辅助方法

    /// <summary>
    /// 构建问卷生成提示词
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>提示词</returns>
    private Task<string> BuildSurveyGenerationPromptAsync(GenerateSurveyRequest request)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("你是一个专业的问卷设计专家，请根据以下要求生成一份问卷：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"问卷主题：{request.Topic}");
        
        if (!string.IsNullOrEmpty(request.Description))
        {
            promptBuilder.AppendLine($"问卷描述：{request.Description}");
        }
        
        if (!string.IsNullOrEmpty(request.SurveyType))
        {
            promptBuilder.AppendLine($"问卷类型：{request.SurveyType}");
        }
        
        promptBuilder.AppendLine($"题目数量：{request.QuestionCount}");
        
        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            promptBuilder.AppendLine($"目标受众：{request.TargetAudience}");
        }
        
        if (!string.IsNullOrEmpty(request.Goals))
        {
            promptBuilder.AppendLine($"调查目标：{request.Goals}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("请按照以下JSON格式返回问卷数据：");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"title\": \"问卷标题\",");
        promptBuilder.AppendLine("  \"description\": \"问卷描述\",");
        promptBuilder.AppendLine("  \"questions\": [");
        promptBuilder.AppendLine("    {");
        promptBuilder.AppendLine("      \"title\": \"题目标题\",");
        promptBuilder.AppendLine("      \"description\": \"题目描述（可选）\",");
        promptBuilder.AppendLine("      \"type\": \"题目类型（SingleChoice/MultipleChoice/Text/TextArea/Number/Date等）\",");
        promptBuilder.AppendLine("      \"isRequired\": true/false,");
        promptBuilder.AppendLine("      \"orderIndex\": 排序索引,");
        promptBuilder.AppendLine("      \"options\": [");
        promptBuilder.AppendLine("        {");
        promptBuilder.AppendLine("          \"text\": \"选项文本\",");
        promptBuilder.AppendLine("          \"value\": \"选项值\",");
        promptBuilder.AppendLine("          \"orderIndex\": 排序索引");
        promptBuilder.AppendLine("        }");
        promptBuilder.AppendLine("      ]");
        promptBuilder.AppendLine("    }");
        promptBuilder.AppendLine("  ]");
        promptBuilder.AppendLine("}");

        if (!string.IsNullOrEmpty(request.CustomPrompt))
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("额外要求：");
            promptBuilder.AppendLine(request.CustomPrompt);
        }

        return Task.FromResult(promptBuilder.ToString());
    }

    /// <summary>
    /// 构建优化提示词
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="optimizationGoals">优化目标</param>
    /// <returns>提示词</returns>
    private string BuildOptimizationPrompt(int surveyId, string optimizationGoals)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("你是一个问卷优化专家，请分析以下问卷并提供优化建议：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"问卷ID：{surveyId}");
        promptBuilder.AppendLine($"优化目标：{optimizationGoals}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("请从以下角度分析并提供建议：");
        promptBuilder.AppendLine("1. 题目设计的合理性");
        promptBuilder.AppendLine("2. 选项设置的完整性");
        promptBuilder.AppendLine("3. 问卷结构的逻辑性");
        promptBuilder.AppendLine("4. 用户体验的友好性");
        promptBuilder.AppendLine("5. 数据收集的有效性");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 构建洞察分析提示词
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>提示词</returns>
    private string BuildInsightPrompt(int surveyId)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("你是一个数据分析专家，请分析以下问卷的回答数据并提供洞察：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"问卷ID：{surveyId}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("请从以下角度进行分析：");
        promptBuilder.AppendLine("1. 回答数据的整体趋势");
        promptBuilder.AppendLine("2. 异常数据或模式");
        promptBuilder.AppendLine("3. 用户行为特征");
        promptBuilder.AppendLine("4. 关键发现和建议");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 解析LLM响应为问卷数据
    /// </summary>
    /// <param name="llmResponse">LLM响应</param>
    /// <param name="request">原始请求</param>
    /// <param name="usedPrompt">使用的完整提示词</param>
    /// <returns>生成的问卷</returns>
    private Task<GeneratedSurveyDto> ParseLLMResponseAsync(string llmResponse, GenerateSurveyRequest request, string usedPrompt)
    {
        try
        {
            // 尝试从响应中提取JSON
            var jsonMatch = Regex.Match(llmResponse, @"\{[\s\S]*\}", RegexOptions.Multiline);
            var jsonContent = jsonMatch.Success ? jsonMatch.Value : llmResponse;

            // 解析JSON
            var surveyData = JsonConvert.DeserializeObject<dynamic>(jsonContent);

            var result = new GeneratedSurveyDto
            {
                Title = surveyData?.title ?? request.Topic ?? "未知主题",
                Description = surveyData?.description ?? request.Description,
                Questions = new List<GeneratedQuestionDto>(),
                UsedPrompt = usedPrompt,
                GeneratedAt = DateTime.UtcNow,
                QualityScore = CalculateQualityScore(surveyData)
            };

            // 解析题目
            if (surveyData?.questions != null)
            {
                int orderIndex = 1;
                foreach (var questionData in surveyData.questions)
                {
                    var question = new GeneratedQuestionDto
                    {
                        Title = questionData?.title ?? "",
                        Description = questionData?.description,
                        Type = questionData?.type ?? "Text",
                        IsRequired = questionData?.isRequired ?? false,
                        OrderIndex = orderIndex++,
                        Options = new List<GeneratedQuestionOptionDto>()
                    };

                    // 解析选项
                    if (questionData?.options != null)
                    {
                        int optionIndex = 1;
                        foreach (var optionData in questionData.options)
                        {
                            question.Options.Add(new GeneratedQuestionOptionDto
                            {
                                Text = optionData?.text ?? "",
                                Value = optionData?.value,
                                OrderIndex = optionIndex++
                            });
                        }
                    }

                    result.Questions.Add(question);
                }
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析LLM响应失败");
            
            // 返回默认结果
            return Task.FromResult(new GeneratedSurveyDto
            {
                Title = request.Topic ?? "未知主题",
                Description = request.Description,
                Questions = new List<GeneratedQuestionDto>(),
                GeneratedAt = DateTime.UtcNow,
                QualityScore = 1
            });
        }
    }

    /// <summary>
    /// 解析优化建议响应
    /// </summary>
    /// <param name="llmResponse">LLM响应</param>
    /// <returns>优化结果</returns>
    private SurveyOptimizationResult ParseOptimizationResponse(string llmResponse)
    {
        // 简单解析，实际可以更复杂
        var suggestions = new List<OptimizationSuggestion>
        {
            new OptimizationSuggestion
            {
                Type = "General",
                Content = llmResponse,
                Priority = 3
            }
        };

        return new SurveyOptimizationResult
        {
            Suggestions = suggestions,
            OverallScore = 7,
            ExpectedImprovement = "基于LLM分析的优化建议"
        };
    }

    /// <summary>
    /// 解析洞察分析响应
    /// </summary>
    /// <param name="llmResponse">LLM响应</param>
    /// <returns>洞察结果</returns>
    private SurveyInsightResult ParseInsightResponse(string llmResponse)
    {
        // 简单解析，实际可以更复杂
        var insights = new List<SurveyInsight>
        {
            new SurveyInsight
            {
                Type = "General",
                Content = llmResponse,
                Confidence = 0.8
            }
        };

        return new SurveyInsightResult
        {
            Insights = insights,
            DataQualityScore = 8,
            KeyFindings = new List<string> { "基于LLM分析的关键发现" },
            RecommendedActions = new List<string> { "基于LLM分析的建议行动" }
        };
    }

    /// <summary>
    /// 估算Token数量
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>预估Token数</returns>
    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // 简单估算：1个Token约等于4个字符（英文）或1-2个中文字符
        var chineseChars = Regex.Matches(text, @"[\u4e00-\u9fff]").Count;
        var otherChars = text.Length - chineseChars;
        
        return (int)(chineseChars * 1.5 + otherChars / 4.0);
    }

    /// <summary>
    /// 计算生成质量评分
    /// </summary>
    /// <param name="surveyData">问卷数据</param>
    /// <returns>质量评分（1-10）</returns>
    private int CalculateQualityScore(dynamic surveyData)
    {
        var score = 5; // 基础分

        try
        {
            // 有标题和描述 +1
            if (!string.IsNullOrEmpty((string?)surveyData?.title) && 
                !string.IsNullOrEmpty((string?)surveyData?.description))
            {
                score += 1;
            }

            // 题目数量合理 +1
            if (surveyData?.questions != null)
            {
                var questionCount = ((System.Collections.IEnumerable)surveyData.questions).Cast<object>().Count();
                if (questionCount >= 3 && questionCount <= 20)
                {
                    score += 1;
                }
            }

            // 题目类型多样 +1
            // 题目有描述 +1
            // 选择题有选项 +1

            score = Math.Min(score, 10);
        }
        catch
        {
            // 解析失败，使用基础分
        }

        return score;
    }

    /// <summary>
    /// 保存生成的问卷及题目
    /// </summary>
    /// <param name="generatedSurvey">生成的问卷数据</param>
    /// <param name="request">原始生成请求</param>
    /// <param name="llmRawOutput">LLM原始输出内容</param>
    /// <returns>保存后的问卷DTO</returns>
    private async Task<SurveyDto> SaveGeneratedSurveyAsync(GeneratedSurveyDto generatedSurvey, GenerateSurveyRequest request, string llmRawOutput)
    {
        try
        {
            _logger.LogInformation("开始保存生成的问卷：{Title}", generatedSurvey.Title);

            // 创建问卷实体
            var createSurveyDto = new CreateSurveyDto
            {
                Title = generatedSurvey.Title,
                Description = generatedSurvey.Description,
                AccessType = SurveyAccessType.Public,
                IsTemplate = false,
                LLMPrompt = generatedSurvey.UsedPrompt,
                LLMRawOutput = llmRawOutput
            };

            // 保存问卷
            var savedSurvey = await _surveyService.CreateAsync(createSurveyDto);

            _logger.LogInformation("问卷已保存，ID：{SurveyId}", savedSurvey.Id);

            // 保存题目（带验证和重试机制）
            var validationErrors = new List<string>();
            var validatedQuestions = new List<GeneratedQuestionDto>();

            foreach (var generatedQuestion in generatedSurvey.Questions)
            {
                var questionType = MapStringToQuestionType(generatedQuestion.Type);
                
                var createQuestionDto = new CreateQuestionDto
                {
                    SurveyId = savedSurvey.Id,
                    Title = generatedQuestion.Title,
                    Description = generatedQuestion.Description,
                    Type = questionType,
                    OrderIndex = generatedQuestion.OrderIndex,
                    IsRequired = generatedQuestion.IsRequired,
                    LLMGenerated = true
                };

                // 添加选项
                if (generatedQuestion.Options?.Any() == true)
                {
                    createQuestionDto.Options = generatedQuestion.Options.Select(o => new CreateQuestionOptionDto
                    {
                        Text = o.Text,
                        Value = o.Value ?? o.Text,
                        OrderIndex = o.OrderIndex,
                        IsOther = false
                    }).ToList();
                }

                try
                {
                    await _questionService.CreateAsync(createQuestionDto);
                    validatedQuestions.Add(generatedQuestion);
                }
                catch (BusinessException ex) when (ex.Message.Contains("题目验证失败"))
                {
                    _logger.LogWarning("题目验证失败：{Title} - {Error}", generatedQuestion.Title, ex.Message);
                    validationErrors.Add($"题目\"{generatedQuestion.Title}\"：{ex.Message}");
                }
            }

            // 如果有验证错误，尝试修正
            if (validationErrors.Any())
            {
                _logger.LogInformation("存在 {ErrorCount} 个题目验证错误，尝试AI修正", validationErrors.Count);
                
                var correctionResult = await TryCorrectQuestionsAsync(request, generatedSurvey, validationErrors, savedSurvey.Id);
                
                if (correctionResult.HasCorrectedQuestions)
                {
                    validatedQuestions.AddRange(correctionResult.CorrectedQuestions);
                    _logger.LogInformation("AI修正成功，新增 {CorrectedCount} 个题目", correctionResult.CorrectedQuestions.Count);
                }
                else
                {
                    _logger.LogWarning("AI修正失败，部分题目无法保存：{Errors}", string.Join("；", validationErrors));
                }
            }

            _logger.LogInformation("问卷及题目保存完成，问卷ID：{SurveyId}，题目数量：{QuestionCount}", 
                savedSurvey.Id, generatedSurvey.Questions.Count);

            return savedSurvey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存生成的问卷失败：{Title}", generatedSurvey.Title);
            throw new BusinessException($"保存问卷失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 将字符串类型映射为QuestionType枚举
    /// </summary>
    /// <param name="typeString">类型字符串</param>
    /// <returns>QuestionType枚举</returns>
    private QuestionType MapStringToQuestionType(string typeString)
    {
        if (string.IsNullOrEmpty(typeString))
            return QuestionType.Text;

        return typeString.ToLower() switch
        {
            "singlechoice" or "single" or "radio" => QuestionType.SingleChoice,
            "multiplechoice" or "multiple" or "checkbox" => QuestionType.MultipleChoice,
            "text" or "input" => QuestionType.Text,
            "textarea" or "longtext" => QuestionType.Textarea,
            "number" or "numeric" => QuestionType.Number,
            "rating" or "rate" => QuestionType.Rating,
            "date" => QuestionType.Date,
            "time" => QuestionType.Time,
            "datetime" => QuestionType.DateTime,
            "matrix" => QuestionType.Matrix,
            "ranking" or "rank" => QuestionType.Ranking,
            _ => QuestionType.Text
        };
    }

    /// <summary>
    /// 尝试修正验证失败的题目
    /// </summary>
    /// <param name="originalRequest">原始请求</param>
    /// <param name="originalSurvey">原始生成的问卷</param>
    /// <param name="validationErrors">验证错误列表</param>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>修正结果</returns>
    private async Task<QuestionCorrectionResult> TryCorrectQuestionsAsync(
        GenerateSurveyRequest originalRequest, 
        GeneratedSurveyDto originalSurvey, 
        List<string> validationErrors, 
        int surveyId)
    {
        try
        {
            // 获取LLM设置
            var llmSettings = await _settingsService.GetLLMSettingsAsync();

            // 构建修正提示词
            var correctionPrompt = BuildCorrectionPrompt(originalRequest, originalSurvey, validationErrors);

            // 调用LLM生成修正后的题目
            var llmResponse = await _llmAssistant.GenerateContentAsync(correctionPrompt, llmSettings.MaxTokens);

            // 解析修正后的题目
            var correctedQuestions = await ParseCorrectionResponseAsync(llmResponse);

            // 验证并保存修正后的题目
            var savedQuestions = new List<GeneratedQuestionDto>();
            foreach (var correctedQuestion in correctedQuestions)
            {
                var questionType = MapStringToQuestionType(correctedQuestion.Type);
                
                var createQuestionDto = new CreateQuestionDto
                {
                    SurveyId = surveyId,
                    Title = correctedQuestion.Title,
                    Description = correctedQuestion.Description,
                    Type = questionType,
                    OrderIndex = correctedQuestion.OrderIndex,
                    IsRequired = correctedQuestion.IsRequired,
                    LLMGenerated = true
                };

                // 添加选项
                if (correctedQuestion.Options?.Any() == true)
                {
                    createQuestionDto.Options = correctedQuestion.Options.Select(o => new CreateQuestionOptionDto
                    {
                        Text = o.Text,
                        Value = o.Value ?? o.Text,
                        OrderIndex = o.OrderIndex,
                        IsOther = false
                    }).ToList();
                }

                try
                {
                    await _questionService.CreateAsync(createQuestionDto);
                    savedQuestions.Add(correctedQuestion);
                    _logger.LogInformation("修正题目保存成功：{Title}", correctedQuestion.Title);
                }
                catch (BusinessException ex)
                {
                    _logger.LogWarning("修正题目仍然验证失败：{Title} - {Error}", correctedQuestion.Title, ex.Message);
                }
            }

            return new QuestionCorrectionResult
            {
                HasCorrectedQuestions = savedQuestions.Any(),
                CorrectedQuestions = savedQuestions,
                RemainingErrors = savedQuestions.Count < correctedQuestions.Count 
                    ? new List<string> { "部分修正题目仍然验证失败" } 
                    : new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI修正题目过程中发生错误");
            return new QuestionCorrectionResult
            {
                HasCorrectedQuestions = false,
                CorrectedQuestions = new List<GeneratedQuestionDto>(),
                RemainingErrors = new List<string> { $"AI修正失败：{ex.Message}" }
            };
        }
    }

    /// <summary>
    /// 构建修正提示词
    /// </summary>
    /// <param name="originalRequest">原始请求</param>
    /// <param name="originalSurvey">原始生成的问卷</param>
    /// <param name="validationErrors">验证错误列表</param>
    /// <returns>修正提示词</returns>
    private string BuildCorrectionPrompt(GenerateSurveyRequest originalRequest, GeneratedSurveyDto originalSurvey, List<string> validationErrors)
    {
        var promptBuilder = new StringBuilder();

        promptBuilder.AppendLine("你是一个问卷设计专家，需要修正以下问卷题目的验证错误：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"原始问卷主题：{originalRequest.Topic}");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("发现的验证错误：");
        foreach (var error in validationErrors)
        {
            promptBuilder.AppendLine($"- {error}");
        }
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("请根据以下验证规则修正题目：");
        promptBuilder.AppendLine("1. 单选题和多选题必须至少包含2个有效选项");
        promptBuilder.AppendLine("2. 选项文本不能为空且不能重复");
        promptBuilder.AppendLine("3. 矩阵题至少需要2个行选项，并需要配置列选项设置");
        promptBuilder.AppendLine("4. 排序题至少需要2个选项");
        promptBuilder.AppendLine("5. 评分题如果有自定义标签，标签不能为空");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("请只修正有错误的题目，保持题目的原始意图，并以JSON格式返回修正后的题目：");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("期望的JSON格式：");
        promptBuilder.AppendLine(@"{
  ""correctedQuestions"": [
    {
      ""title"": ""题目标题"",
      ""description"": ""题目描述"",
      ""type"": ""题目类型"",
      ""isRequired"": true/false,
      ""orderIndex"": 排序索引,
      ""options"": [
        {
          ""text"": ""选项文本"",
          ""value"": ""选项值"",
          ""orderIndex"": 1
        }
      ]
    }
  ]
}");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// 解析修正响应
    /// </summary>
    /// <param name="llmResponse">LLM响应</param>
    /// <returns>修正后的题目列表</returns>
    private Task<List<GeneratedQuestionDto>> ParseCorrectionResponseAsync(string llmResponse)
    {
        try
        {
            // 尝试从响应中提取JSON
            var jsonMatch = Regex.Match(llmResponse, @"\{[\s\S]*\}", RegexOptions.Multiline);
            var jsonContent = jsonMatch.Success ? jsonMatch.Value : llmResponse;

            // 解析JSON
            var correctionData = JsonConvert.DeserializeObject<dynamic>(jsonContent);

            var result = new List<GeneratedQuestionDto>();

            // 解析修正后的题目
            if (correctionData?.correctedQuestions != null)
            {
                foreach (var questionData in correctionData.correctedQuestions)
                {
                    var question = new GeneratedQuestionDto
                    {
                        Title = questionData?.title ?? "",
                        Description = questionData?.description,
                        Type = questionData?.type ?? "Text",
                        IsRequired = questionData?.isRequired ?? false,
                        OrderIndex = questionData?.orderIndex ?? 0,
                        Options = new List<GeneratedQuestionOptionDto>()
                    };

                    // 解析选项
                    if (questionData?.options != null)
                    {
                        int optionIndex = 1;
                        foreach (var optionData in questionData.options)
                        {
                            question.Options.Add(new GeneratedQuestionOptionDto
                            {
                                Text = optionData?.text ?? "",
                                Value = optionData?.value,
                                OrderIndex = optionIndex++
                            });
                        }
                    }

                    result.Add(question);
                }
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析修正响应失败");
            return Task.FromResult(new List<GeneratedQuestionDto>());
        }
    }

    #endregion
}

/// <summary>
/// 题目修正结果
/// </summary>
public class QuestionCorrectionResult
{
    /// <summary>
    /// 是否有修正成功的题目
    /// </summary>
    public bool HasCorrectedQuestions { get; set; }

    /// <summary>
    /// 修正成功的题目列表
    /// </summary>
    public List<GeneratedQuestionDto> CorrectedQuestions { get; set; } = new();

    /// <summary>
    /// 剩余的错误
    /// </summary>
    public List<string> RemainingErrors { get; set; } = new();
}
