using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Dtos.AI;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目管理控制器
/// </summary>
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-book", PlatformType = PlatformType.Tenant)]
public class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;
    private readonly QuestionAiGeneratorService _questionAiGeneratorService;
    private readonly IAiTaskService _aiTaskService;

    /// <summary>
    /// 初始化题目管理控制器
    /// </summary>
    /// <param name="questionService">题目服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="questionAiGeneratorService">题目AI生成服务</param>
    /// <param name="aiTaskService">AI任务服务</param>
    public QuestionsController(
        IQuestionService questionService,
        ILogger<QuestionsController> logger,
        QuestionAiGeneratorService questionAiGeneratorService,
        IAiTaskService aiTaskService)
    {
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(questionAiGeneratorService);
        ArgumentNullException.ThrowIfNull(aiTaskService);

        _questionService = questionService;
        _logger = logger;
        _questionAiGeneratorService = questionAiGeneratorService;
        _aiTaskService = aiTaskService;
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
    /// 发布题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id:long}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此题目吗？", visibleOn: "status == 1")]
    [DisplayName("发布题目")]
    public async Task<ActionResult<ApiResponse>> PublishQuestion(long id)
    {
        await _questionService.PublishQuestionAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量发布题目
    /// </summary>
    /// <param name="request">批量发布请求数据</param>
    /// <returns>发布结果</returns>
    [HttpPost("batch/publish")]
    [Operation("批量发布", "ajax", null, "确定要批量发布选中的题目吗？", isBulkOperation: true)]
    [DisplayName("批量发布题目")]
    public async Task<ActionResult<ApiResponse>> BatchPublish([FromBody] BatchOperationDto<long> request)
    {
        ArgumentNullException.ThrowIfNull(request);

        (int successCount, List<long> failedQuestionIds) = await _questionService.BatchPublishAsync(request.Ids);

        return failedQuestionIds.Any()
            ? SuccessResponse($"成功发布 {successCount} 个题目，但以下题目发布失败: {string.Join(", ", failedQuestionIds)}")
            : SuccessResponse($"成功发布 {successCount} 个题目！");
    }

    /// <summary>
    /// 取消发布题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id:long}/unpublish")]
    [Operation("取消发布", "ajax", null, "确定要取消发布此题目吗？", visibleOn: "status == 2")]
    [DisplayName("取消发布题目")]
    public async Task<ActionResult<ApiResponse>> UnpublishQuestion(long id)
    {
        await _questionService.UnpublishQuestionAsync(id);
        return SuccessResponse();
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
    [HeaderOperation("从文本导入", "form", DialogSize = DialogSize.XL)]
    [DisplayName("从文本导入题目")]
    public async Task<ActionResult<ApiResponse>> BatchParserFromText([FromBody] QuestionImportFromTextDto input)
    {
        (int successCount, List<string> failedQuestions) = await _questionService.ImportFromTextAsync(input);

        if (successCount == 0 && failedQuestions.Count > 0)
        {
            return BadResponse($"{failedQuestions.Count} 个题目导入失败: \n{string.Join(", \n", failedQuestions)}");
        }
        return failedQuestions.Any()
            ? SuccessResponse($"{successCount} 个题目导入成功，{failedQuestions.Count} 个题目导入失败: \n{string.Join(", \n", failedQuestions)}")
            : SuccessResponse($"成功导入 {successCount} 个题目！");
    }

    /// <summary>
    /// 步骤1：解析文本并进行AI审核
    /// </summary>
    [HttpPost("import-wizard/step1-parse")]
    [DisplayName("解析题目文本")]
    public async Task<ActionResult<ApiResponse<QuestionBatchPreviewResponseDto>>> ParseQuestionsFromText([FromBody] QuestionImportStepDto input)
    {
        var result = await _questionService.ParseQuestionsFromTextAsync(input);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 步骤2：获取题目预览数据
    /// </summary>
    [HttpGet("import-wizard/step2-preview/{sessionId}")]
    [DisplayName("获取题目预览")]
    public async Task<ActionResult<ApiResponse<QuestionBatchPreviewResponseDto>>> GetQuestionPreview([FromRoute] string sessionId)
    {

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("会话ID不能为空");
        }

        var result = await _questionService.GetQuestionPreviewAsync(sessionId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 步骤3：保存用户编辑的题目
    /// </summary>
    [HttpPost("import-wizard/step3-save-edits")]
    [DisplayName("保存题目编辑")]
    public async Task<ActionResult<ApiResponse<string>>> SaveQuestionEdits([FromBody] QuestionBatchPreviewResponseDto input)
    {
        await _questionService.SaveQuestionEditsAsync(input);
        return SuccessResponse<string>("题目编辑已保存");
    }

    /// <summary>
    /// 步骤4：确认导入题目
    /// </summary>
    [HttpPost("import-wizard/step4-import")]
    [DisplayName("确认导入题目")]
    [IgnoreCrud]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> ImportQuestions([FromBody] QuestionBatchImportConfirmDto input)
    {
        var result = await _questionService.ImportQuestionsAsync(input);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取题目修正对比数据（整体格式）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="index">题目索引</param>
    /// <returns>修正对比数据</returns>
    [HttpGet("import-wizard/correction-diff/{sessionId}/{index}")]
    [DisplayName("获取题目修正对比")]
    public async Task<ActionResult<ApiResponse<object>>> GetQuestionCorrectionDiff(string sessionId, int index)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest("会话ID不能为空");
        }

        var previewData = await _questionService.GetQuestionPreviewAsync(sessionId);
        if (previewData?.Questions == null || index >= previewData.Questions.Count)
        {
            return NotFound("题目不存在");
        }

        var question = previewData.Questions[index];

        // 生成原始格式文本
        var originalText = GenerateQuestionText(question, index + 1, true); // true表示使用原始内容

        // 生成修正后格式文本
        var correctedText = GenerateQuestionText(question, index + 1, false); // false表示使用修正后内容

        var result = new
        {
            OriginalText = originalText,
            CorrectedText = correctedText,
            CorrectionNotes = question.CorrectionNotes ?? new List<string>(),
            HasChanges = question.IsCorrected,
            QuestionIndex = index + 1,
            QuestionType = question.Type.ToString()
        };

        return SuccessResponse<object>(result);
    }

    /// <summary>
    /// 生成题目文本格式
    /// </summary>
    /// <param name="question">题目数据</param>
    /// <param name="questionIndex">题目序号</param>
    /// <param name="useOriginal">是否使用原始内容</param>
    /// <returns>格式化的题目文本</returns>
    private string GenerateQuestionText(QuestionPreviewDto question, int questionIndex, bool useOriginal)
    {
        var content = useOriginal ? (question.OriginalContent ?? question.Content) : question.Content;
        List<string> options = useOriginal && question.OriginalOptions != null ? question.OriginalOptions : question.Options;
        var answer = useOriginal ? (question.OriginalAnswer ?? question.CorrectAnswer) : question.CorrectAnswer;
        var analysis = useOriginal ? (question.OriginalAnalysis ?? question.Analysis) : question.Analysis;

        var sb = new System.Text.StringBuilder();

        // 题目内容
        sb.AppendLine($"{questionIndex}、{content}");

        // 选项（仅选择题和多选题）
        if (question.Type == Data.Models.Enums.QuestionType.SingleChoice ||
            question.Type == Data.Models.Enums.QuestionType.MultipleChoice)
        {
            for (int i = 0; i < options.Count; i++)
            {
                sb.AppendLine(options[i]);
            }
        }

        sb.AppendLine($"【答案】{answer}");
        // 难度标记
        if (question.Difficulty != Data.Models.Enums.QuestionDifficulty.Easy)
        {
            var difficultyText = question.Difficulty switch
            {
                Data.Models.Enums.QuestionDifficulty.Medium => "中等",
                Data.Models.Enums.QuestionDifficulty.Hard => "困难",
                _ => "简单"
            };
            sb.AppendLine($"【难度】{difficultyText}");
        }

        // 解析
        if (!string.IsNullOrWhiteSpace(analysis))
        {
            sb.AppendLine($"【解析】{analysis}");
        }

        // 知识点标签
        if (!string.IsNullOrWhiteSpace(question.KnowledgePoints))
        {
            sb.AppendLine($"【标签】{question.KnowledgePoints}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 题目导入向导（Wizard组件）
    /// </summary>
    [HttpPost("import-wizard")]
    [HeaderOperation("AI导入", "service",
        Icon = "fa-solid fa-magic", DialogSize = DialogSize.XL)]
    [DisplayName("AI导入")]
    public async Task<ActionResult<ApiResponse<JObject>>> QuestionImportWizard()
    {
        await Task.CompletedTask; // 避免async警告

        // 使用Service组件加载外部配置文件
        var serviceWrapper = new JObject
        {
            ["type"] = "service",
            ["name"] = "questionImportWizardService",
            ["schemaApi"] = "js:/amis/configs/exam/question-import-wizard.js?type=wizard&baseApi=/exam/api/exam&rootApi=${ROOT_API}"
        };

        return SuccessResponse(serviceWrapper);
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

        // 处理知识点：如果是JSON格式则反序列化为友好格式
        string? knowledgePointsDisplay = null;
        if (!string.IsNullOrEmpty(question.KnowledgePoints))
        {
            try
            {
                // 尝试解析为JSON数组
                var knowledgePointsArray = System.Text.Json.JsonSerializer.Deserialize<List<string>>(question.KnowledgePoints);
                if (knowledgePointsArray != null && knowledgePointsArray.Any())
                {
                    // 用顿号或逗号分隔显示
                    knowledgePointsDisplay = string.Join("、", knowledgePointsArray);
                }
            }
            catch
            {
                // 如果不是JSON格式，直接使用原始字符串
                knowledgePointsDisplay = question.KnowledgePoints;
            }
        }

        // 使用Service组件加载外部配置文件
        var serviceWrapper = new JObject
        {
            ["type"] = "service",
            ["name"] = "questionPreviewService",
            ["data"] = new JObject
            {
                ["id"] = id,
                ["question"] = new JObject
                {
                    ["content"] = question.Content,
                    ["type"] = (int)question.Type,
                    ["options"] = new JArray(question.Options),
                    ["correctAnswer"] = question.CorrectAnswer,
                    ["analysis"] = question.Analysis,
                    ["difficulty"] = (int)question.Difficulty,
                    ["knowledgePoints"] = knowledgePointsDisplay,
                    ["tags"] = question.Tags != null ? new JArray(question.Tags) : null,
                    ["score"] = question.DefaultScore
                }
            },
            ["schemaApi"] = "js:/amis/configs/exam/question-preview.js?baseApi=/exam/api/exam"
        };

        return SuccessResponse(serviceWrapper);
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
            ["tpl"] = "<div class=\"question-label\"><pre>1. ${content | raw} </pre><span style=\"color:#999\">（${score}分）</span></div>",
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
                    ["required"] = true,
                    ["labelTpl"] = "${label | raw}"  // 使用raw过滤器确保特殊字符正确显示
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
                    ["required"] = true,
                    ["labelTpl"] = "${label | raw}"  // 使用raw过滤器确保特殊字符正确显示
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
            ["tpl"] = "<div style=\"color:#009900; font-weight:bold;\">正确答案：${answer | raw}</div>",
            ["inline"] = false
        });

        // 如果有解析，则显示解析
        if (!string.IsNullOrEmpty(question.Analysis))
        {
            formItems.Add(new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = "<div style=\"margin-top:10px;\"><b>解析：</b>${analysis | raw}</div>",
                ["inline"] = false
            });
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "",
            ["id"] = "questionPreviewForm",
            ["data"] = new JObject
            {
                ["content"] = question.Content,
                ["score"] = question.DefaultScore,
                ["answer"] = question.CorrectAnswer,
                ["analysis"] = question.Analysis
            },
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

    /// <summary>
    /// 使用AI生成题目（新版本，基于aiForm）
    /// </summary>
    /// <param name="request">生成题目请求</param>
    /// <returns>任务ID</returns>
    [HttpPost("ai/generate-async")]
    [HeaderOperation("AI题目生成", "aiForm",
        Icon = "fa-solid fa-magic",
        PollingInterval = 2000,
        MaxPollingTime = 300000,
        FormTitle = "题目生成配置",
        StepsTitle = "AI生成进度",
        LogTitle = "生成日志",
        ResultTitle = "生成结果")]
    [DisplayName("AI题目生成")]
    public async Task<ActionResult<ApiResponse<object>>> GenerateQuestionsAsync([FromBody] AIGenerateQuestionDto request)
    {
        var taskId = await _questionAiGeneratorService.GenerateAsync(request);
        return SuccessResponse<object>(new { taskId });
    }

    /// <summary>
    /// 取消AI任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>取消结果</returns>
    [HttpPost("ai/cancel-task")]
    [DisplayName("取消AI任务")]
    public async Task<ActionResult<ApiResponse>> CancelTask([FromBody] string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return BadResponse("任务ID不能为空");
        }

        await _aiTaskService.CancelTaskAsync(taskId);
        return SuccessResponse("任务已取消");
    }

    /// <summary>
    /// 获取所有标签（用于下拉选择）
    /// </summary>
    /// <returns>标签列表</returns>
    [HttpGet("tags")]
    [DisplayName("获取标签列表")]
    public async Task<ActionResult<ApiResponse<List<OptionDto<string>>>>> GetTags()
    {
        var tags = await _questionService.GetAllTagsAsync();
        return SuccessResponse(tags);
    }
}