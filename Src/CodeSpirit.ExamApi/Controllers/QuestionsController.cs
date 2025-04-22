using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.Question;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目管理控制器
/// </summary>
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-book")]
public class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IAIQuestionGeneratorService _aiQuestionGeneratorService;
    private readonly ILogger<QuestionsController> _logger;
    private readonly INotificationService _notificationService;
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 初始化题目管理控制器
    /// </summary>
    /// <param name="questionService">题目服务</param>
    /// <param name="aiQuestionGeneratorService">AI题目生成服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="notificationService">通知服务</param>
    /// <param name="distributedCache">分布式缓存</param>
    public QuestionsController(
        IQuestionService questionService,
        IAIQuestionGeneratorService aiQuestionGeneratorService,
        ILogger<QuestionsController> logger,
        INotificationService notificationService,
        IDistributedCache distributedCache)
    {
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(aiQuestionGeneratorService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(notificationService);
        ArgumentNullException.ThrowIfNull(distributedCache);

        _questionService = questionService;
        _aiQuestionGeneratorService = aiQuestionGeneratorService;
        _logger = logger;
        _notificationService = notificationService;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 获取题目列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目列表分页结果</returns>
    [HttpGet]
    [DisplayName("获取题目列表")]
    public async Task<ActionResult<ApiResponse<PageList<QuestionDto>>>> GetQuestions([FromQuery] QuestionQueryDto queryDto)
    {
        PageList<QuestionDto> questions = await _questionService.GetQuestionsAsync(queryDto);
        return SuccessResponse(questions);
    }

    /// <summary>
    /// 导出题目列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>导出的题目列表</returns>
    [HttpGet("Export")]
    [DisplayName("导出题目列表")]
    public async Task<ActionResult<ApiResponse<PageList<QuestionDto>>>> Export([FromQuery] QuestionQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 最大导出数量限制
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取题目数据
        PageList<QuestionDto> questions = await _questionService.GetQuestionsAsync(queryDto);

        // 如果数据为空则返回错误信息
        return questions.Items.Count == 0
            ? BadResponse<PageList<QuestionDto>>("没有数据可供导出")
            : SuccessResponse(questions);
    }

    /// <summary>
    /// 获取题目选择列表
    /// </summary>
    /// <param name="queryDto"></param>
    /// <returns></returns>
    [HttpGet("select-list")]
    [DisplayName("获取题目选择列表")]
    public async Task<ActionResult<ApiResponse<List<QuestionSelectListDto>>>> GetSelectList([FromQuery] QuestionSelectListQueryDto queryDto)
    {
        List<QuestionSelectListDto> questions = await _questionService.GetQuestionSelectListAsync(queryDto);
        return SuccessResponse(questions);
    }

    /// <summary>
    /// 获取题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目详细信息</returns>
    [HttpGet("{id:long}")]
    [DisplayName("获取题目详情")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> GetQuestion(long id)
    {
        QuestionDto question = await _questionService.GetQuestionAsync(id);
        return SuccessResponse(question);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="createQuestionDto">创建题目请求数据</param>
    /// <returns>创建的题目信息</returns>
    [HttpPost]
    [DisplayName("创建题目")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion(CreateQuestionDto createQuestionDto)
    {
        ArgumentNullException.ThrowIfNull(createQuestionDto);
        QuestionDto questionDto = await _questionService.CreateQuestionAsync(createQuestionDto);
        return SuccessResponse(questionDto);
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateQuestionDto">更新题目请求数据</param>
    /// <returns>更新后的题目信息</returns>
    [HttpPut("{id:long}")]
    [DisplayName("更新题目")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestion(long id, [FromBody] UpdateQuestionDto updateQuestionDto)
    {
        await _questionService.UpdateQuestionAsync(id, updateQuestionDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id:long}")]
    [Operation("删除", "ajax", null, "确定要删除此题目吗？")]
    [DisplayName("删除题目")]
    public async Task<ActionResult<ApiResponse>> DeleteQuestion(long id)
    {
        await _questionService.DeleteQuestionAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取题目历史版本
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目历史版本列表</returns>
    [HttpGet("{id:long}/versions")]
    [Operation("历史版本", "link", "/exam/questionVersions?questionId=${id}", null)]
    [DisplayName("获取题目历史版本")]
    public ActionResult GetQuestionVersions(long id)
    {
        return Ok();
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的题目吗？", isBulkOperation: true)]
    [DisplayName("批量删除题目")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        (int successCount, List<long> failedQuestionIds) = await _questionService.BatchDeleteAsync(request.Ids);

        return failedQuestionIds.Any()
            ? SuccessResponse($"成功删除 {successCount} 个题目，但以下题目删除失败: {string.Join(", ", failedQuestionIds)}")
            : SuccessResponse($"成功删除 {successCount} 个题目！");
    }

    ///// <summary>
    ///// 批量导入题目
    ///// </summary>
    ///// <param name="importDto">导入数据</param>
    ///// <returns>导入结果</returns>
    //[HttpPost("batch/import")]
    //public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<QuestionBatchImportItemDto> importDto)
    //{
    //    ArgumentNullException.ThrowIfNull(importDto);

    //    (int successCount, List<string> failedQuestions) = await _questionService.BatchImportAsync(importDto.ImportData);

    //    return failedQuestions.Any()
    //        ? SuccessResponse($"成功导入 {successCount} 个题目，但以下题目导入失败: {string.Join(", ", failedQuestions)}")
    //        : SuccessResponse($"成功导入 {successCount} 个题目！");
    //}

    [HttpPost("batch/Parser-from-text")]
    [HeaderOperation("从文本导入", "form")]
    [DisplayName("从文本导入题目")]
    public async Task<ActionResult<ApiResponse>> BatchParserFromText([FromBody] QuestionImportFromTextDto input)
    {
        (int successCount, List<string> failedQuestions) = await _questionService.ImportFromTextAsync(input);

        return failedQuestions.Any()
            ? SuccessResponse($"{successCount} 个题目导入成功，{failedQuestions.Count} 个题目导入失败: \n{string.Join(", \n", failedQuestions)}")
            : SuccessResponse($"成功导入 {successCount} 个题目！");
    }

    /// <summary>
    /// 预览题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目预览的Amis配置</returns>
    [HttpGet("{id:long}/preview")]
    [Operation(label: "预览", actionType: "service")]
    [DisplayName("预览题目")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewQuestion(long id)
    {
        var question = await _questionService.GetQuestionAsync(id);
        if (question == null)
        {
            return NotFound("题目不存在");
        }

        var panelConfig = new JObject
        {
            ["type"] = "service",
            ["schemaApi"] = $"get:/exam/api/exam/questions/{id}/question-preview",
            ["body"] = new JObject
            {
                ["title"] = $"预览题目",
                ["type"] = "panel",
                ["body"] = "${content}"
            }
        };

        return SuccessResponse(panelConfig);
    }

    /// <summary>
    /// 获取题目预览的Amis配置
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目的Amis配置</returns>
    [HttpGet("{id:long}/question-preview")]
    [DisplayName("获取题目预览配置")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetQuestionPreviewConfig(long id)
    {
        var question = await _questionService.GetQuestionAsync(id);
        if (question == null)
        {
            return NotFound("题目不存在");
        }
        question.Content = WebUtility.HtmlDecode(question.Content);
        question.Options = question.Options.Select(p => WebUtility.HtmlDecode(p)).ToList();
        question.CorrectAnswer = WebUtility.HtmlDecode(question.CorrectAnswer);
        question.Analysis = WebUtility.HtmlDecode(question.Analysis ?? string.Empty);
        question.KnowledgePoints = WebUtility.HtmlDecode(question.KnowledgePoints ?? string.Empty);

        // 使用JObject/JArray构建表单
        var formItems = new JArray();

        // 问题标题
        var titleObj = new JObject
        {
            ["type"] = "tpl",
            ["tpl"] = $"<div class=\"question-label\"><pre>1. {question.Content} </pre><span style=\"color:#999\">（{question.DefaultScore}分）</span></div>",
            ["inline"] = false
        };
        formItems.Add(titleObj);

        // 根据题目类型添加不同的表单控件
        switch (question.Type.ToString())
        {
            case "SingleChoice":
                // 解析选项
                var singleOptions = new JArray();
                var options = question.Options;
                for (int idx = 0; idx < options.Count; idx++)
                {
                    singleOptions.Add(new JObject
                    {
                        ["label"] = options[idx],
                        ["value"] = options[idx]
                    });
                }

                var singleChoiceObj = new JObject
                {
                    ["type"] = "radios",
                    ["name"] = $"question_{id}",
                    ["options"] = singleOptions,
                    ["mode"] = "horizontal",
                    ["required"] = true
                };
                formItems.Add(singleChoiceObj);
                break;

            case "MultipleChoice":
                // 解析选项
                var multiOptions = new JArray();
                var multiChoiceOptions = question.Options;
                for (int idx = 0; idx < multiChoiceOptions.Count; idx++)
                {
                    multiOptions.Add(new JObject
                    {
                        ["label"] = multiChoiceOptions[idx],
                        ["value"] = multiChoiceOptions[idx],
                    });
                }

                var multiChoiceObj = new JObject
                {
                    ["type"] = "checkboxes",
                    ["name"] = $"question_{id}",
                    ["options"] = multiOptions,
                    ["mode"] = "horizontal",
                    ["required"] = true
                };
                formItems.Add(multiChoiceObj);
                break;

            case "TrueFalse":
                // 创建判断题选项（统一使用radios组件）
                var tfOptions = new JArray
                {
                    new JObject { ["label"] = "正确", ["value"] = "True" },
                    new JObject { ["label"] = "错误", ["value"] = "False" }
                };

                var tfObj = new JObject
                {
                    ["type"] = "radios",
                    ["name"] = $"question_{id}",
                    ["options"] = tfOptions,
                    ["mode"] = "horizontal",
                    ["required"] = true
                };
                formItems.Add(tfObj);
                break;

            default:
                // 简答题和其他题型
                var textareaObj = new JObject
                {
                    ["type"] = "textarea",
                    ["name"] = $"question_{id}",
                    ["placeholder"] = "请输入答案",
                    ["minRows"] = 3,
                    ["maxRows"] = 6,
                    ["required"] = true
                };
                formItems.Add(textareaObj);
                break;
        }

        // 添加答案和解析区域
        formItems.Add(new JObject { ["type"] = "divider" });

        // 显示正确答案
        formItems.Add(new JObject
        {
            ["type"] = "tpl",
            ["tpl"] = $"<div style=\"color:#009900; font-weight:bold;\">正确答案：{question.CorrectAnswer}</div>",
            ["inline"] = false
        });

        // 如果有解析，则显示解析
        if (!string.IsNullOrEmpty(question.Analysis))
        {
            formItems.Add(new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = $"<div style=\"margin-top:10px;\"><b>解析：</b>{question.Analysis}</div>",
                ["inline"] = false
            });
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "",
            ["id"] = "questionPreviewForm",
            ["body"] = formItems,
            ["actions"] = new JArray()  // 添加空的actions数组，隐藏表单自带的提交按钮
        };

        return SuccessResponse(amisConfig);
    }

    /// <summary>
    /// 获取题目设置
    /// </summary>
    /// <returns>题目设置</returns>
    [HttpGet("settings")]
    [DisplayName("获取题目设置")]
    public async Task<ActionResult<ApiResponse<QuestionSettingsDto>>> GetQuestionSettings()
    {
        var settings = await _questionService.GetQuestionSettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 更新题目设置
    /// </summary>
    /// <param name="settings">题目设置</param>
    /// <returns>操作结果</returns>
    [HttpPut("settings")]
    [HeaderOperation("设置", "form", null, null, InitApi = "/exam/api/exam/Questions/settings")]
    [DisplayName("更新题目设置")]
    public async Task<ActionResult<ApiResponse>> UpdateQuestionSettings(
        [FromBody] QuestionSettingsDto settings)
    {
        var result = await _questionService.UpdateQuestionSettingsAsync(settings);
        if (result)
        {
            return SuccessResponse("题目设置已更新");
        }
        else
        {
            return BadResponse("更新题目设置失败");
        }
    }

    ///// <summary>
    ///// 使用AI生成题目
    ///// </summary>
    ///// <param name="request">生成题目请求</param>
    ///// <returns>生成的题目</returns>
    //[HttpPost("ai/generate")]
    //[HeaderOperation("AI生成题目", "form")]
    //[DisplayName("AI生成题目")]
    //public async Task<ActionResult<ApiResponse<List<CreateQuestionDto>>>> GenerateQuestions(AIGenerateQuestionDto request)
    //{
    //    try
    //    {
    //        var questions = await _aiQuestionGeneratorService.GenerateQuestionsAsync(request);
    //        return SuccessResponse(questions);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "生成题目时发生错误");
    //        return BadResponse<List<CreateQuestionDto>>(ex.Message);
    //    }
    //}

    /// <summary>
    /// 使用AI生成并保存题目
    /// </summary>
    /// <param name="request">生成题目请求</param>
    /// <returns>保存结果</returns>
    [HttpPost("ai/generate-and-save")]
    [DisplayName("AI题目生成")]
    [HeaderOperation("AI题目生成", "link", "/Tasks/QuestionGeneration", null, Blank = true)]
    public IActionResult GenerateQuestions()
    {
        try
        {
            // 生成唯一会话ID
            string sessionId = Guid.NewGuid().ToString("N");

            // 返回会话ID给前端
            return Ok(SuccessResponse(new { sessionId }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化题目生成过程时发生错误");
            return BadRequest(BadResponse(ex.Message));
        }
    }

    /// <summary>
    /// 执行AI题目生成（后台处理）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="request">生成题目请求</param>
    [HttpPost("ai/execute-generation/{sessionId}")]
    //[NonAction] // 标记为非API操作，由前端单独调用
    public async Task ExecuteGenerationAsync(string sessionId, AIGenerateQuestionDto request)
    {
        try
        {
            // 开始计时
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 发送开始生成通知
            await _notificationService.NotifyGenerationStartedAsync(sessionId, request);
            
            // 使用修改后的生成方法，直接传递sessionId和notificationService
            var questions = await _aiQuestionGeneratorService.GenerateQuestionsAsync(
                request, 
                sessionId, 
                _notificationService);
            
            // 保存题目到缓存，设置过期时间为1小时
            var cacheKey = $"GeneratedQuestions_{sessionId}";
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));
            
            // 保存原始生成的题目到缓存（需要序列化）
            await _distributedCache.SetStringAsync(
                cacheKey, 
                JsonConvert.SerializeObject(questions), 
                cacheOptions);
            
            // 保存题目
            int successCount = 0;
            List<string> failedItems = new();
            List<QuestionDto> savedQuestions = new();
            
            await _notificationService.NotifyGenerationProgressAsync(sessionId, "saving", "正在保存题目...", 95);
            
            foreach (var question in questions)
            {
                try
                {
                    var savedQuestion = await _questionService.CreateQuestionAsync(question);
                    savedQuestions.Add(savedQuestion);
                    successCount++;
                    
                    // 计算保存进度
                    int savePercentage = 95 + (int)((float)successCount / questions.Count * 5);
                    // 确保进度不超过100%
                    savePercentage = Math.Min(99, savePercentage);
                    
                    await _notificationService.NotifyGenerationProgressAsync(
                        sessionId, 
                        "saving", 
                        $"正在保存题目 ({successCount}/{questions.Count})...", 
                        savePercentage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存生成的题目时发生错误");
                    failedItems.Add($"题目 [{question.Content.Substring(0, Math.Min(30, question.Content.Length))}...] 保存失败: {ex.Message}");
                }
            }
            
            // 保存成功保存的题目到缓存
            var savedCacheKey = $"SavedQuestions_{sessionId}";
            await _distributedCache.SetStringAsync(
                savedCacheKey, 
                JsonConvert.SerializeObject(savedQuestions), 
                cacheOptions);
            
            // 停止计时
            stopwatch.Stop();
            
            // 发送完成通知
            await _notificationService.NotifyGenerationCompletedAsync(sessionId, questions, stopwatch.ElapsedMilliseconds);
            
            if (failedItems.Any())
            {
                string errorMessage = $"成功保存 {successCount} 个题目，{failedItems.Count} 个题目保存失败: {string.Join(", ", failedItems)}";
                await _notificationService.NotifyGenerationErrorAsync(sessionId, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成并保存题目时发生错误");
            await _notificationService.NotifyGenerationErrorAsync(sessionId, ex.Message);
        }
    }

    /// <summary>
    /// 获取已生成的题目列表
    /// </summary>
    /// <param name="sessionId">生成会话ID</param>
    /// <returns>已生成的题目列表</returns>
    [HttpGet("generated/{sessionId}")]
    [DisplayName("获取已生成题目")]
    public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetGeneratedQuestions(string sessionId)
    {
        try
        {
            _logger.LogInformation("正在获取生成会话 {SessionId} 的题目", sessionId);
            
            // 优先从缓存中获取已保存的题目
            var savedCacheKey = $"SavedQuestions_{sessionId}";
            string savedCacheValue = await _distributedCache.GetStringAsync(savedCacheKey);
            
            if (!string.IsNullOrEmpty(savedCacheValue))
            {
                var savedQuestions = JsonConvert.DeserializeObject<List<QuestionDto>>(savedCacheValue);
                if (savedQuestions != null && savedQuestions.Any())
                {
                    _logger.LogInformation("从缓存中获取到生成会话 {SessionId} 的 {Count} 个已保存题目", 
                        sessionId, savedQuestions.Count);
                    return SuccessResponse(savedQuestions);
                }
            }
            
            // 如果没有已保存的题目，尝试获取原始生成的题目
            var generatedCacheKey = $"GeneratedQuestions_{sessionId}";
            string generatedCacheValue = await _distributedCache.GetStringAsync(generatedCacheKey);
            
            if (!string.IsNullOrEmpty(generatedCacheValue))
            {
                var generatedQuestions = JsonConvert.DeserializeObject<List<CreateQuestionDto>>(generatedCacheValue);
                if (generatedQuestions != null && generatedQuestions.Any())
                {
                    _logger.LogInformation("从缓存中获取到生成会话 {SessionId} 的 {Count} 个原始生成题目", 
                        sessionId, generatedQuestions.Count);
                    
                    // 将CreateQuestionDto转换为QuestionDto（简化版本）
                    var convertedQuestions = generatedQuestions.Select((q, index) => new QuestionDto
                    {
                        Id = -(index + 1), // 使用负数ID表示未保存到数据库的题目
                        Content = q.Content,
                        Type = q.Type,
                        // 使用枚举ToString作为类型名称
                        Difficulty = q.Difficulty,
                        // 使用枚举ToString作为难度名称
                        Options = q.Options,
                        CorrectAnswer = q.CorrectAnswer,
                        Analysis = q.Analysis,
                        KnowledgePoints = q.KnowledgePoints,
                        DefaultScore = q.DefaultScore
                        // 避免使用创建和更新时间属性，因为QuestionDto可能没有这些属性
                    }).ToList();
                    
                    return SuccessResponse(convertedQuestions);
                }
            }
            
            // 如果缓存中没有，则尝试从数据库查询
            _logger.LogInformation("缓存中未找到生成会话 {SessionId} 的题目，尝试从数据库查询", sessionId);
            
            // 查询条件：创建时间最近的题目
            var queryDto = new QuestionQueryDto
            {
                PerPage = 1000, // 使用较大的数量限制以确保获取所有题目
                Page = 1
                // 注意：实际应用中应通过SessionId或其他方式过滤题目
            };
            
            // 获取题目列表 - 这里仅模拟返回，实际项目中需改进此查询
            var result = await _questionService.GetQuestionsAsync(queryDto);
            var questions = result.Items;
            
            // 在内存中过滤最近创建的题目
            // 这是临时解决方案，实际项目中应在数据库层面实现过滤
            var recentQuestions = questions.Take(10).ToList();
            
            if (recentQuestions.Count == 0)
            {
                _logger.LogWarning("未找到生成会话 {SessionId} 的题目", sessionId);
                return SuccessResponse(new List<QuestionDto>());
            }
            
            // 将查询结果缓存起来
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(1))
                .SetAbsoluteExpiration(TimeSpan.FromHours(2));
            
            await _distributedCache.SetStringAsync(
                savedCacheKey, 
                JsonConvert.SerializeObject(recentQuestions), 
                cacheOptions);
            
            _logger.LogInformation("从数据库获取并缓存生成会话的 {Count} 个题目", recentQuestions.Count);
            return SuccessResponse(recentQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生成会话的题目时发生错误: {Message}", ex.Message);
            return BadResponse<List<QuestionDto>>($"获取生成题目失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清除指定会话的题目缓存
    /// </summary>
    /// <param name="sessionId">生成会话ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("generated/{sessionId}/cache")]
    [DisplayName("清除题目缓存")]
    public async Task<ActionResult<ApiResponse>> ClearGeneratedQuestionsCache(string sessionId)
    {
        try
        {
            _logger.LogInformation("正在清除生成会话 {SessionId} 的题目缓存", sessionId);
            
            // 移除两种缓存
            var savedCacheKey = $"SavedQuestions_{sessionId}";
            var generatedCacheKey = $"GeneratedQuestions_{sessionId}";
            
            await _distributedCache.RemoveAsync(savedCacheKey);
            await _distributedCache.RemoveAsync(generatedCacheKey);
            
            _logger.LogInformation("已清除生成会话 {SessionId} 的题目缓存", sessionId);
            return SuccessResponse("题目缓存已清除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除生成会话的题目缓存时发生错误: {Message}", ex.Message);
            return BadResponse($"清除题目缓存失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取缓存状态
    /// </summary>
    /// <param name="sessionId">生成会话ID</param>
    /// <returns>缓存状态信息</returns>
    [HttpGet("generated/{sessionId}/cache-status")]
    [DisplayName("获取缓存状态")]
    public async Task<ActionResult<ApiResponse<object>>> GetCacheStatus(string sessionId)
    {
        try
        {
            _logger.LogInformation("正在检查生成会话 {SessionId} 的缓存状态", sessionId);
            
            var savedCacheKey = $"SavedQuestions_{sessionId}";
            var generatedCacheKey = $"GeneratedQuestions_{sessionId}";
            
            // 检查已保存题目的缓存
            string savedCacheValue = await _distributedCache.GetStringAsync(savedCacheKey);
            bool hasSavedCache = !string.IsNullOrEmpty(savedCacheValue);
            int savedCount = 0;
            
            if (hasSavedCache)
            {
                var savedQuestions = JsonConvert.DeserializeObject<List<QuestionDto>>(savedCacheValue);
                savedCount = savedQuestions?.Count ?? 0;
            }
            
            // 检查生成题目的缓存
            string generatedCacheValue = await _distributedCache.GetStringAsync(generatedCacheKey);
            bool hasGeneratedCache = !string.IsNullOrEmpty(generatedCacheValue);
            int generatedCount = 0;
            
            if (hasGeneratedCache)
            {
                var generatedQuestions = JsonConvert.DeserializeObject<List<CreateQuestionDto>>(generatedCacheValue);
                generatedCount = generatedQuestions?.Count ?? 0;
            }
            
            var cacheStatus = new
            {
                SessionId = sessionId,
                HasSavedCache = hasSavedCache,
                SavedQuestionsCount = savedCount,
                HasGeneratedCache = hasGeneratedCache,
                GeneratedQuestionsCount = generatedCount,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogInformation("生成会话 {SessionId} 的缓存状态: 已保存题目缓存={HasSavedCache}, 数量={SavedCount}; 生成题目缓存={HasGeneratedCache}, 数量={GeneratedCount}", 
                sessionId, hasSavedCache, savedCount, hasGeneratedCache, generatedCount);
                
            return SuccessResponse<object>(cacheStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生成会话的缓存状态时发生错误: {Message}", ex.Message);
            return BadResponse<object>($"获取缓存状态失败: {ex.Message}");
        }
    }
}