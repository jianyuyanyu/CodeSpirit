using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Dtos.Settings;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;


namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷管理控制器
/// </summary>
[DisplayName("问卷管理")]
[Navigation(Icon = "fa-solid fa-poll", PlatformType = PlatformType.Tenant)]
public class SurveysController : ApiControllerBase
{
    private readonly ISurveyService _surveyService;
    private readonly ISurveySettingsService _settingsService;
    private readonly ISurveyLLMGeneratorService _llmGeneratorService;
    private readonly ILogger<SurveysController> _logger;
    private readonly IRepository<Question> _questionRepository;
    private readonly IRepository<QuestionOption> _questionOptionRepository;

    /// <summary>
    /// 初始化问卷管理控制器
    /// </summary>
    /// <param name="surveyService">问卷服务</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="questionRepository">题目仓储</param>
    /// <param name="questionOptionRepository">题目选项仓储</param>
    public SurveysController(
        ISurveyService surveyService,
        ISurveySettingsService settingsService,
        ISurveyLLMGeneratorService llmGeneratorService,
        ILogger<SurveysController> logger,
        IRepository<Question> questionRepository,
        IRepository<QuestionOption> questionOptionRepository)
    {
        ArgumentNullException.ThrowIfNull(surveyService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(llmGeneratorService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(questionRepository);
        ArgumentNullException.ThrowIfNull(questionOptionRepository);

        _surveyService = surveyService;
        _settingsService = settingsService;
        _llmGeneratorService = llmGeneratorService;
        _logger = logger;
        _questionRepository = questionRepository;
        _questionOptionRepository = questionOptionRepository;
    }

    /// <summary>
    /// 获取问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷列表分页结果</returns>
    [HttpGet]
    [DisplayName("获取问卷列表")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetSurveys([FromQuery] SurveyQueryDto queryDto)
    {
        var surveys = await _surveyService.GetSurveysAsync(queryDto);
        return SuccessResponse(surveys);
    }

    /// <summary>
    /// 获取问卷选项列表（用于下拉选择）
    /// </summary>
    /// <returns>问卷选项列表</returns>
    [HttpGet("options")]
    [DisplayName("获取问卷选项")]
    public async Task<ActionResult<ApiResponse<List<SurveyOptionDto>>>> GetSurveyOptions()
    {
        var surveys = await _surveyService.GetSurveyOptionsAsync();
        return SuccessResponse(surveys);
    }

    /// <summary>
    /// 获取问卷详情
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取问卷详情")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurvey(int id)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        return SuccessResponse(survey);
    }

    /// <summary>
    /// 创建问卷
    /// </summary>
    /// <param name="createDto">创建问卷DTO</param>
    /// <returns>创建的问卷</returns>
    [HttpPost]
    [DisplayName("创建问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto createDto)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).CreateAsync(createDto);
        return SuccessResponseWithCreate(nameof(GetSurvey), survey);
    }

    /// <summary>
    /// 更新问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <param name="updateDto">更新问卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新问卷")]
    public async Task<ActionResult<ApiResponse>> UpdateSurvey(int id, [FromBody] UpdateSurveyDto updateDto)
    {
        await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).UpdateAsync(id, updateDto);
        return SuccessResponse("问卷更新成功");
    }

    /// <summary>
    /// 删除问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除问卷")]
    public async Task<ActionResult<ApiResponse>> DeleteSurvey(int id)
    {
        await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).DeleteAsync(id);
        return SuccessResponse("问卷删除成功");
    }

    /// <summary>
    /// 发布问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此问卷吗？", "status == 'Draft' && isPreviewChecked")]
    [DisplayName("发布问卷")]
    public async Task<ActionResult<ApiResponse>> PublishSurvey(int id)
    {
        await _surveyService.PublishSurveyAsync(id);
        return SuccessResponse("问卷发布成功");
    }

    /// <summary>
    /// 关闭问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/close")]
    [Operation("关闭", "ajax", null, "确定要关闭此问卷吗？", "status == 'Published'")]
    [DisplayName("关闭问卷")]
    public async Task<ActionResult<ApiResponse>> CloseSurvey(int id)
    {
        await _surveyService.CloseSurveyAsync(id);
        return SuccessResponse("问卷关闭成功");
    }

    /// <summary>
    /// 归档问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/archive")]
    [Operation("归档", "ajax", null, "确定要归档此问卷吗？", "status != 'Archived'")]
    [DisplayName("归档问卷")]
    public async Task<ActionResult<ApiResponse>> ArchiveSurvey(int id)
    {
        await _surveyService.ArchiveSurveyAsync(id);
        return SuccessResponse("问卷归档成功");
    }

    /// <summary>
    /// 复制问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <param name="request">复制请求</param>
    /// <returns>复制的问卷</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "form")]
    [DisplayName("复制问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CopySurvey([FromRoute] int id, [FromBody] CopySurveyRequest request)
    {
        var survey = await _surveyService.CopySurveyAsync(id, request.Title);
        return SuccessResponse(survey);
    }

    /// <summary>
    /// 预览问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>预览配置</returns>
    [HttpGet("{id}/preview")]
    [Operation(label: "问卷预览", actionType: "service")]
    [DisplayName("问卷预览")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewSurvey(int id)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        if (survey == null)
        {
            return NotFound("问卷不存在");
        }

        // 自动标记为已预览
        await _surveyService.MarkPreviewedAsync(id);

        var panelConfig = new JObject
        {
            ["type"] = "service",
            ["schemaApi"] = $"get:/survey/api/survey/surveys/{id}/questions-preview",
            ["body"] = new JObject
            {
                ["title"] = $"预览问卷 - {survey.Title}",
                ["type"] = "panel",
                ["body"] = "${content}"
            }
        };

        return SuccessResponse(panelConfig);
    }

    /// <summary>
    /// 标记问卷已完成预览
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/mark-previewed")]
    [DisplayName("标记为已预览")]
    public async Task<ActionResult<ApiResponse>> MarkSurveyPreviewed(int id)
    {
        await _surveyService.MarkPreviewedAsync(id);
        return SuccessResponse("问卷已标记为已预览");
    }

    /// <summary>
    /// 获取问卷统计信息
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>统计信息</returns>
    [HttpGet("{id}/statistics")]
    [DisplayName("获取统计信息")]
    public async Task<ActionResult<ApiResponse<SurveyStatisticsDto>>> GetSurveyStatistics(int id)
    {
        var statistics = await _surveyService.GetSurveyStatisticsAsync(id);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 获取我的问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>我的问卷列表</returns>
    [HttpGet("my")]
    [DisplayName("我的问卷")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetMySurveys([FromQuery] SurveyQueryDto queryDto)
    {
        var surveys = await _surveyService.GetMySurveysAsync(queryDto);
        return SuccessResponse(surveys);
    }

    /// <summary>
    /// 获取问卷模板列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>模板列表</returns>
    [HttpGet("templates")]
    [DisplayName("问卷模板")]
    public async Task<ActionResult<ApiResponse<PageList<SurveyDto>>>> GetSurveyTemplates([FromQuery] SurveyQueryDto queryDto)
    {
        var templates = await _surveyService.GetSurveyTemplatesAsync(queryDto);
        return SuccessResponse(templates);
    }

    /// <summary>
    /// 从模板创建问卷
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>创建的问卷</returns>
    [HttpPost("templates/{templateId}/create")]
    [Operation("从模板创建", "form")]
    [DisplayName("从模板创建问卷")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateFromTemplate(int templateId, [FromBody] CreateFromTemplateRequest request)
    {
        var survey = await _surveyService.CreateFromTemplateAsync(templateId, request.Title);
        return SuccessResponse(survey);
    }

    #region 系统设置相关方法

    /// <summary>
    /// 获取问卷系统设置
    /// </summary>
    /// <returns>设置信息</returns>
    [HttpGet("settings")]
    [DisplayName("获取系统设置")]
    public async Task<ActionResult<ApiResponse<SurveySettingsDto>>> GetSurveySettings()
    {
        var settings = await _settingsService.GetSurveySettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 更新问卷系统设置
    /// </summary>
    /// <param name="settings">设置信息</param>
    /// <returns>操作结果</returns>
    [HttpPut("settings")]
    [DisplayName("更新系统设置")]
    [HeaderOperation("更新系统设置", "form", null, null, InitApi = "/survey/api/survey/Surveys/settings")]
    public async Task<ActionResult<ApiResponse>> UpdateSurveySettings([FromBody] SurveySettingsDto settings)
    {
        await _settingsService.UpdateSurveySettingsAsync(settings);
        return SuccessResponse("设置更新成功");
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    /// <returns>操作结果</returns>
    [HttpPost("settings/reset")]
    [HeaderOperation("重置默认", "ajax", null, "确定要重置为默认设置吗？")]
    [DisplayName("重置设置")]
    public async Task<ActionResult<ApiResponse>> ResetToDefaultSettings()
    {
        await _settingsService.ResetToDefaultSettingsAsync();
        return SuccessResponse("设置已重置为默认值");
    }

    ///// <summary>
    ///// 获取自动保存设置
    ///// </summary>
    ///// <returns>自动保存设置</returns>
    //[HttpGet("settings/auto-save")]
    //[DisplayName("获取自动保存设置")]
    //public async Task<ActionResult<ApiResponse<AutoSaveSettings>>> GetAutoSaveSettings()
    //{
    //    var settings = await _settingsService.GetAutoSaveSettingsAsync();
    //    return SuccessResponse(settings);
    //}

    ///// <summary>
    ///// 获取LLM设置
    ///// </summary>
    ///// <returns>LLM设置</returns>
    //[HttpGet("settings/llm")]
    //[DisplayName("获取LLM设置")]
    //public async Task<ActionResult<ApiResponse<LLMSettings>>> GetLLMSettings()
    //{
    //    var settings = await _settingsService.GetLLMSettingsAsync();
    //    return SuccessResponse(settings);
    //}

    ///// <summary>
    ///// 获取默认限制设置
    ///// </summary>
    ///// <returns>默认限制设置</returns>
    //[HttpGet("settings/restrictions")]
    //[DisplayName("获取限制设置")]
    //public async Task<ActionResult<ApiResponse<DefaultRestrictionsSettings>>> GetDefaultRestrictionsSettings()
    //{
    //    var settings = await _settingsService.GetDefaultRestrictionsSettingsAsync();
    //    return SuccessResponse(settings);
         //}

     #endregion

    #region AI问卷生成相关方法

    /// <summary>
    /// 根据主题生成问卷
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成的问卷</returns>
    [HttpPost("ai/generate")]
    [HeaderOperation("AI生成问卷", "form",Icon = "fa-solid fa-robot")]
    [DisplayName("AI生成问卷")]
    public async Task<ActionResult<ApiResponse<GeneratedSurveyDto>>> GenerateSurvey([FromBody] GenerateSurveyRequest request)
    {
        var result = await _llmGeneratorService.GenerateSurveyAsync(request);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 优化现有问卷
    /// </summary>
    /// <param name="request">优化请求</param>
    /// <returns>优化建议</returns>
    [HttpPost("ai/optimize")]
    [Operation("AI优化问卷", "form")]
    [DisplayName("AI优化问卷")]
    public async Task<ActionResult<ApiResponse<SurveyOptimizationResult>>> OptimizeSurvey([FromBody] OptimizeSurveyRequest request)
    {
        var result = await _llmGeneratorService.OptimizeSurveyAsync(request.SurveyId, request.OptimizationGoals);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>洞察分析结果</returns>
    [HttpPost("{surveyId}/ai/insights")]
    [Operation("AI洞察分析", "ajax", null, "确定要生成AI洞察分析吗？")]
    [DisplayName("AI洞察分析")]
    public async Task<ActionResult<ApiResponse<SurveyInsightResult>>> GenerateInsights(int surveyId)
    {
        var result = await _llmGeneratorService.GenerateInsightsAsync(surveyId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 验证提示词
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("ai/validate-prompt")]
    [DisplayName("验证提示词")]
    public async Task<ActionResult<ApiResponse<PromptValidationResult>>> ValidatePrompt([FromBody] ValidatePromptRequest request)
    {
        var result = await _llmGeneratorService.ValidatePromptAsync(request.Prompt);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 压缩提示词
    /// </summary>
    /// <param name="request">压缩请求</param>
    /// <returns>压缩结果</returns>
    [HttpPost("ai/compress-prompt")]
    [Operation("压缩提示词", "form")]
    [DisplayName("压缩提示词")]
    public async Task<ActionResult<ApiResponse<CompressPromptResult>>> CompressPrompt([FromBody] CompressPromptRequest request)
    {
        var compressedPrompt = await _llmGeneratorService.CompressPromptAsync(request.Prompt, request.MaxLength);
        
        var result = new CompressPromptResult
        {
            OriginalPrompt = request.Prompt,
            CompressedPrompt = compressedPrompt,
            OriginalLength = request.Prompt.Length,
            CompressedLength = compressedPrompt.Length,
            CompressionRatio = (double)compressedPrompt.Length / request.Prompt.Length
        };

        return SuccessResponse(result);
    }

    #endregion

    /// <summary>
    /// 获取问卷题目预览的Amis配置
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷题目的Amis配置</returns>
    [HttpGet("{id}/questions-preview")]
    [DisplayName("获取问卷题目预览配置")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetSurveyQuestionsPreviewConfig(int id)
    {
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        if (survey == null)
        {
            return NotFound("问卷不存在");
        }

        // 获取问卷的题目列表
        var questions = _questionRepository.Find(q => q.SurveyId == id).OrderBy(q => q.OrderIndex).ToList();

        // 使用JObject/JArray构建表单
        var formItems = new JArray();

        // 添加问卷信息头部
        var headerInfo = new JObject
        {
            ["type"] = "panel",
            ["className"] = "survey-header",
            ["body"] = new JArray()
        };

        var headerBody = (JArray)headerInfo["body"]!;

        // 基本信息
        headerBody!.Add(new JObject
        {
            ["type"] = "html",
            ["html"] = $@"
                <div class=""survey-header-info"">
                    <h3>{survey.Title}</h3>
                    <div class=""survey-basic-info"">
                        <span>题目数量：{survey.QuestionCount}题</span> | 
                        <span>状态：{GetSurveyStatusName(survey.Status.ToString())}</span> | 
                        <span>访问类型：{GetAccessTypeName(survey.AccessType.ToString())}</span>
                    </div>
                    {(!string.IsNullOrEmpty(survey.Description) ? $"<div class=\"survey-description\">{survey.Description}</div>" : "")}
                </div>
            "
        });

        formItems.Add(headerInfo);

        // 按题型分组题目并排序
        var questionsByType = questions
            .OrderBy(q => q.OrderIndex)
            .GroupBy(q => q.Type.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        int globalIndex = 1; // 全局题目序号

        // 为每个题型创建分组
        foreach (var typeGroup in questionsByType)
        {
            string questionType = typeGroup.Key;
            var typeQuestions = typeGroup.Value;
            string typeName = GetQuestionTypeName(questionType);

            // 创建题型分组头部
            var typeHeader = new JObject
            {
                ["type"] = "html",
                ["html"] = $@"
                    <div class=""question-type-header"">
                        <h4 style=""color: #1890ff; border-bottom: 2px solid #1890ff; padding-bottom: 8px; margin: 20px 0 15px 0;"">
                            {typeName}（共{typeQuestions.Count}题）
                        </h4>
                    </div>
                "
            };
            formItems.Add(typeHeader);

            // 处理该题型下的所有题目
            for (int i = 0; i < typeQuestions.Count; i++)
            {
                var question = typeQuestions[i];
                
                // 问题标题
                var titleObj = new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = $"<div class=\"question-label\"><pre>{globalIndex}. {question.Title} </pre>{(question.IsRequired ? "<span style=\"color:red\">*</span>" : "")}</div>",
                    ["inline"] = false
                };
                formItems.Add(titleObj);

                // 如果有描述，添加描述
                if (!string.IsNullOrEmpty(question.Description))
                {
                    var descObj = new JObject
                    {
                        ["type"] = "html",
                        ["html"] = $"<div class=\"question-description\" style=\"color: #666; margin-bottom: 10px;\">{question.Description}</div>"
                    };
                    formItems.Add(descObj);
                }

                // 根据题目类型添加不同的表单控件
                switch (question.Type.ToString())
                {
                    case "SingleChoice":
                        // 获取选项
                        var singleOptions = _questionOptionRepository.Find(o => o.QuestionId == question.Id).OrderBy(o => o.OrderIndex).ToList();
                        var singleChoiceOptions = new JArray();
                        foreach (var option in singleOptions)
                        {
                            singleChoiceOptions.Add(new JObject
                            {
                                ["label"] = option.Text,
                                ["value"] = option.Value ?? option.Text
                            });
                        }

                        var singleChoiceObj = new JObject
                        {
                            ["type"] = "radios",
                            ["name"] = $"question_{question.Id}",
                            ["options"] = singleChoiceOptions,
                            ["mode"] = "horizontal",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(singleChoiceObj);
                        break;

                    case "MultipleChoice":
                        // 获取选项
                        var multiOptions = _questionOptionRepository.Find(o => o.QuestionId == question.Id).OrderBy(o => o.OrderIndex).ToList();
                        var multiChoiceOptions = new JArray();
                        foreach (var option in multiOptions)
                        {
                            multiChoiceOptions.Add(new JObject
                            {
                                ["label"] = option.Text,
                                ["value"] = option.Value ?? option.Text
                            });
                        }

                        var multiChoiceObj = new JObject
                        {
                            ["type"] = "checkboxes",
                            ["name"] = $"question_{question.Id}",
                            ["options"] = multiChoiceOptions,
                            ["mode"] = "horizontal",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(multiChoiceObj);
                        break;

                    case "Text":
                        var textObj = new JObject
                        {
                            ["type"] = "input-text",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "请输入答案",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(textObj);
                        break;

                    case "Number":
                        var numberObj = new JObject
                        {
                            ["type"] = "input-number",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "请输入数字",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(numberObj);
                        break;

                    case "Rating":
                        var ratingObj = new JObject
                        {
                            ["type"] = "input-rating",
                            ["name"] = $"question_{question.Id}",
                            ["count"] = 5,
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(ratingObj);
                        break;

                    case "Date":
                        var dateObj = new JObject
                        {
                            ["type"] = "input-date",
                            ["name"] = $"question_{question.Id}",
                            ["format"] = "YYYY-MM-DD",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(dateObj);
                        break;

                    case "Time":
                        var timeObj = new JObject
                        {
                            ["type"] = "input-time",
                            ["name"] = $"question_{question.Id}",
                            ["format"] = "HH:mm",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(timeObj);
                        break;

                    case "DateTime":
                        var datetimeObj = new JObject
                        {
                            ["type"] = "input-datetime",
                            ["name"] = $"question_{question.Id}",
                            ["format"] = "YYYY-MM-DD HH:mm",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(datetimeObj);
                        break;

                    case "Textarea":
                        var textareaObj = new JObject
                        {
                            ["type"] = "textarea",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "请输入答案",
                            ["minRows"] = 3,
                            ["maxRows"] = 6,
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(textareaObj);
                        break;

                    case "Matrix":
                        // 矩阵题需要特殊处理，这里简化为文本输入
                        var matrixObj = new JObject
                        {
                            ["type"] = "textarea",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "矩阵题答案",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(matrixObj);
                        break;

                    case "Ranking":
                        // 排序题需要特殊处理，这里简化为文本输入
                        var rankingObj = new JObject
                        {
                            ["type"] = "textarea",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "排序题答案",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(rankingObj);
                        break;

                    default:
                        // 默认为文本输入
                        var defaultObj = new JObject
                        {
                            ["type"] = "input-text",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "请输入答案",
                            ["required"] = question.IsRequired
                        };
                        formItems.Add(defaultObj);
                        break;
                }

                // 如果不是该题型的最后一个题目，添加分隔线
                if (i < typeQuestions.Count - 1)
                {
                    formItems.Add(new JObject { ["type"] = "divider" });
                }

                globalIndex++; // 增加全局序号
            }

            // 在不同题型之间添加更明显的分隔
            formItems.Add(new JObject 
            { 
                ["type"] = "html", 
                ["html"] = "<div style=\"margin: 30px 0; border-bottom: 1px solid #f0f0f0;\"></div>" 
            });
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "问卷预览",
            ["id"] = "surveyForm",
            ["body"] = formItems,
            ["actions"] = new JArray()
            {
                new JObject
                {
                    ["type"] = "button",
                    ["label"] = "返回",
                    ["actionType"] = "close",
                    ["level"] = "default"
                },
                new JObject
                {
                    ["type"] = "button",
                    ["label"] = "发布问卷",
                    ["actionType"] = "ajax",
                    ["api"] = $"PUT:/survey/api/survey/surveys/{id}/publish",
                    ["level"] = "primary",
                    ["confirmText"] = "确定要发布此问卷吗？发布后将可用于收集回答。",
                    ["visibleOn"] = "status == 'Draft'",
                    ["reload"] = "window"
                }
            }
        };

        return SuccessResponse(amisConfig);
    }

    /// <summary>
    /// 获取问卷状态中文名称
    /// </summary>
    /// <param name="status">状态</param>
    /// <returns>中文名称</returns>
    private string GetSurveyStatusName(string status)
    {
        return status switch
        {
            "Draft" => "草稿",
            "Published" => "已发布",
            "Closed" => "已关闭",
            "Archived" => "已归档",
            _ => status
        };
    }

    /// <summary>
    /// 获取访问类型中文名称
    /// </summary>
    /// <param name="accessType">访问类型</param>
    /// <returns>中文名称</returns>
    private string GetAccessTypeName(string accessType)
    {
        return accessType switch
        {
            "Public" => "公开",
            "Private" => "私有",
            "Password" => "密码保护",
            _ => accessType
        };
    }

    /// <summary>
    /// 获取题目类型中文名称
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <returns>中文名称</returns>
    private string GetQuestionTypeName(string questionType)
    {
        return questionType switch
        {
            "SingleChoice" => "单选题",
            "MultipleChoice" => "多选题",
            "Text" => "填空题",
            "Number" => "数字题",
            "Rating" => "评分题",
            "Date" => "日期题",
            "Time" => "时间题",
            "DateTime" => "日期时间题",
            "Textarea" => "长文本题",
            "Matrix" => "矩阵题",
            "Ranking" => "排序题",
            _ => questionType
        };
    }
}

