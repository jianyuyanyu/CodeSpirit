using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Dtos.Settings;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.AiFormFill.Services;
using CodeSpirit.AiFormFill.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using CodeSpirit.Core.Extensions;


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
    private readonly IQuestionService _questionService;
    private readonly IAiFormFillService _aiFormFillService;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// 初始化问卷管理控制器
    /// </summary>
    /// <param name="surveyService">问卷服务</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="questionService">题目服务</param>
    /// <param name="aiFormFillService">AI表单填充服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveysController(
        ISurveyService surveyService,
        ISurveySettingsService settingsService,
        ISurveyLLMGeneratorService llmGeneratorService,
        IQuestionService questionService,
        IAiFormFillService aiFormFillService,
        ILogger<SurveysController> logger)
    {
        ArgumentNullException.ThrowIfNull(surveyService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(llmGeneratorService);
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(aiFormFillService);
        ArgumentNullException.ThrowIfNull(logger);

        _surveyService = surveyService;
        _settingsService = settingsService;
        _llmGeneratorService = llmGeneratorService;
        _questionService = questionService;
        _aiFormFillService = aiFormFillService;
        _logger = logger;
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
    /// 获取题目类型选项
    /// </summary>
    /// <returns>题目类型选项</returns>
    [HttpGet("question-type-options")]
    [DisplayName("获取题目类型选项")]
    public ActionResult<ApiResponse<List<object>>> GetQuestionTypeOptions()
    {
        var options = new List<object>
        {
            new { value = "SingleChoice", label = "单选题" },
            new { value = "MultipleChoice", label = "多选题" },
            new { value = "Text", label = "填空题" },
            new { value = "Textarea", label = "长文本题" },
            new { value = "Rating", label = "评分题" },
            new { value = "Number", label = "数字题" },
            new { value = "Date", label = "日期题" },
            new { value = "Time", label = "时间题" },
            new { value = "DateTime", label = "日期时间题" },
            new { value = "Matrix", label = "矩阵题" },
            new { value = "Ranking", label = "排序题" }
        };
        return SuccessResponse(options);
    }

    /// <summary>
    /// 获取插入位置选项
    /// </summary>
    /// <returns>插入位置选项</returns>
    [HttpGet("insert-position-options")]
    [DisplayName("获取插入位置选项")]
    public ActionResult<ApiResponse<List<object>>> GetInsertPositionOptions()
    {
        var options = new List<object>
        {
            new { value = "end", label = "末尾添加" },
            new { value = "beginning", label = "开头插入" },
            new { value = "middle", label = "中间插入" }
        };
        return SuccessResponse(options);
    }

    ///// <summary>
    ///// 获取问卷详情
    ///// </summary>
    ///// <param name="id">问卷ID</param>
    ///// <returns>问卷详情</returns>
    //[HttpGet("{id}")]
    //[DisplayName("获取问卷详情")]
    //public async Task<ActionResult<ApiResponse<SurveyDto>>> GetSurvey(int id)
    //{
    //    var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
    //    return SuccessResponse(survey);
    //}



    ///// <summary>
    ///// 创建问卷
    ///// </summary>
    ///// <param name="createDto">创建问卷DTO</param>
    ///// <returns>创建的问卷</returns>
    //[HttpPost]
    //[DisplayName("创建问卷")]
    //public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateSurvey([FromBody] CreateSurveyDto createDto)
    //{
    //    var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).CreateAsync(createDto);
    //    return SuccessResponseWithCreate("GetSurvey", survey);
    //}

    /// <summary>
    /// 更新问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <param name="updateDto">更新问卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [Operation("编辑", "form", null, null, visibleOn: "status == 0")]
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
    [Operation("删除", "ajax", null, "确定要删除此问卷吗？", visibleOn: "status == 0")]
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
    [Operation("发布", "ajax", null, "确定要发布此问卷吗？", visibleOn: "status == 0 && isPreviewChecked")]
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
    [Operation("关闭", "ajax", null, "确定要关闭此问卷吗？", visibleOn: "status == 1")]
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
    [Operation("归档", "ajax", null, "确定要归档此问卷吗？", visibleOn: "status == 2")]
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
    [Operation(label: "预览", actionType: "service")]
    [DisplayName("预览")]
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

    ///// <summary>
    ///// 从模板创建问卷
    ///// </summary>
    ///// <param name="templateId">模板ID</param>
    ///// <param name="request">创建请求</param>
    ///// <returns>创建的问卷</returns>
    //[HttpPost("templates/{templateId}/create")]
    //[HeaderOperation("从模板创建", "form")]
    //[DisplayName("从模板创建问卷")]
    //public async Task<ActionResult<ApiResponse<SurveyDto>>> CreateFromTemplate(int templateId, [FromBody] CreateFromTemplateRequest request)
    //{
    //    var survey = await _surveyService.CreateFromTemplateAsync(templateId, request.Title);
    //    return SuccessResponse(survey);
    //}

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
    [DisplayName("全局设置")]
    [HeaderOperation("全局设置", "form", null, null, InitApi = "/survey/api/survey/Surveys/settings")]
    public async Task<ActionResult<ApiResponse>> UpdateSurveySettings([FromBody] SurveySettingsDto settings)
    {
        await _settingsService.UpdateSurveySettingsAsync(settings);
        return SuccessResponse("设置更新成功");
    }

    ///// <summary>
    ///// 重置为默认设置
    ///// </summary>
    ///// <returns>操作结果</returns>
    //[HttpPost("settings/reset")]
    //[HeaderOperation("重置默认", "ajax", null, "确定要重置为默认设置吗？")]
    //[DisplayName("重置设置")]
    //public async Task<ActionResult<ApiResponse>> ResetToDefaultSettings()
    //{
    //    await _settingsService.ResetToDefaultSettingsAsync();
    //    return SuccessResponse("设置已重置为默认值");
    //}

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

    ///// <summary>
    ///// 根据主题生成问卷建议
    ///// </summary>
    ///// <param name="request">生成建议请求</param>
    ///// <returns>问卷建议数据</returns>
    //[HttpPost("generate-suggestions")]
    //[DisplayName("生成问卷建议")]
    //public async Task<ActionResult<ApiResponse<GenerateSurveyRequest>>> GenerateSurveyFieldSuggestions([FromBody] GenerateSurveyRequest request)
    //{
    //    return await this.HandleAiFillAsync(_aiFormFillService, request);
    //}

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

    ///// <summary>
    ///// 优化现有问卷
    ///// </summary>
    ///// <param name="request">优化请求</param>
    ///// <returns>优化建议</returns>
    //[HttpPost("ai/optimize")]
    //[Operation("AI优化问卷", "form")]
    //[DisplayName("AI优化问卷")]
    //public async Task<ActionResult<ApiResponse<SurveyOptimizationResult>>> OptimizeSurvey([FromBody] OptimizeSurveyRequest request)
    //{
    //    var result = await _llmGeneratorService.OptimizeSurveyAsync(request.SurveyId, request.OptimizationGoals);
    //    return SuccessResponse(result);
    //}

    /// <summary>
    /// 生成问卷洞察分析
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>洞察分析结果</returns>
    [HttpPost("{id}/ai/insights")]
    [Operation("AI洞察分析", "ajax", null, null, visibleOn: "status == 1 || status == 2", 
        FeedbackTitle = "问卷洞察分析结果",
        FeedbackBodyTpl = @"{
            ""type"": ""form"",
            ""body"": [
                {
                    ""type"": ""flex"",
                    ""justify"": ""space-between"",
                    ""alignItems"": ""center"",
                    ""className"": ""mb-3"",
                    ""items"": [
                        {
                            ""type"": ""alert"",
                            ""level"": ""info"",
                            ""body"": ""基于AI技术对问卷进行深度分析，为您提供专业的洞察报告。"",
                            ""className"": ""flex-1 mr-3""
                        },
                        {
                            ""type"": ""button"",
                            ""label"": ""打印报告"",
                            ""icon"": ""fa fa-print"",
                            ""level"": ""primary"",
                            ""size"": ""sm"",
                            ""onEvent"": {
                                ""click"": {
                                    ""actions"": [
                                        {
                                            ""actionType"": ""print"",
                                            ""args"": {""id"":""insight-report-content""}
                                        }
                                    ]
                                }
                            }
                        }
                    ]
                },
                {
                    ""type"": ""container"",
                    ""id"": ""insight-report-content"",
                    ""className"": ""print-content"",
                    ""body"": [
                        {
                            ""type"": ""tpl"",
                            ""tpl"": ""<div class='print-header'><h2>问卷洞察分析报告</h2><p class='text-muted'>生成时间：${DATETOSTR(NOW(), 'YYYY-MM-DD HH:mm:ss')}</p></div>"",
                            ""className"": ""mb-4""
                        },
                        {
                            ""type"": ""divider"",
                            ""title"": ""洞察分析""
                        },
                        {
                            ""type"": ""markdown"",
                            ""value"": ""${insights}"",
                            ""className"": ""mb-4""
                        },
                        {
                            ""type"": ""divider"",
                            ""title"": ""数据质量评分""
                        },
                        {
                            ""type"": ""flex"",
                            ""alignItems"": ""center"",
                            ""className"": ""mb-4"",
                            ""items"": [
                                {
                                    ""type"": ""progress"",
                                    ""value"": ""${dataQualityScore * 10}"",
                                    ""showLabel"": true,
                                    ""className"": ""flex-1 mr-3""
                                },
                                {
                                    ""type"": ""tpl"",
                                    ""tpl"": ""<div class='text-muted'><strong>评分：${dataQualityScore}/10</strong></div>""
                                }
                            ]
                        },
                        {
                            ""type"": ""divider"",
                            ""title"": ""关键发现""
                        },
                        {
                            ""type"": ""each"",
                            ""name"": ""keyFindings"",
                            ""items"": {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='mb-2 print-item'><i class='fa fa-lightbulb text-warning'></i> ${item}</div>""
                            },
                            ""className"": ""mb-4""
                        },
                        {
                            ""type"": ""divider"",
                            ""title"": ""建议行动""
                        },
                        {
                            ""type"": ""each"",
                            ""name"": ""recommendedActions"",
                            ""items"": {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='mb-2 print-item'><i class='fa fa-check-circle text-success'></i> ${item}</div>""
                            },
                            ""className"": ""mb-4""
                        },
                        {
                            ""type"": ""tpl"",
                            ""tpl"": ""<div class='print-footer text-center text-muted mt-4'><small>本报告由 CodeSpirit 问卷系统自动生成</small></div>""
                        }
                    ]
                }
            ],
            ""style"": {
                "".print-content"": {
                    ""@media print"": {
                        ""margin"": ""0"",
                        ""padding"": ""20px"",
                        ""font-size"": ""12px"",
                        ""line-height"": ""1.5""
                    }
                },
                "".print-header h2"": {
                    ""@media print"": {
                        ""color"": ""#000"",
                        ""font-size"": ""18px"",
                        ""margin-bottom"": ""10px""
                    }
                },
                "".print-item"": {
                    ""@media print"": {
                        ""break-inside"": ""avoid"",
                        ""margin-bottom"": ""8px""
                    }
                },
                "".print-footer"": {
                    ""@media print"": {
                        ""position"": ""fixed"",
                        ""bottom"": ""20px"",
                        ""width"": ""100%""
                    }
                }
            }
        }",
        FeedBackSize = "xl",
        Icon = "fa fa-magic")]
    [DisplayName("AI洞察分析")]
    public async Task<ActionResult<ApiResponse<SurveyInsightResult>>> GenerateInsights(int id)
    {
        // 获取问卷信息
        var survey = await _surveyService.GetAsync(id);
        if (survey == null)
        {
            return NotFound("问卷不存在");
        }

        var result = await _llmGeneratorService.GenerateInsightsAsync(survey);
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
    /// AI扩题功能
    /// </summary>
    /// <param name="request">扩题请求</param>
    /// <returns>扩题结果</returns>
    [HttpPost("ai/expand-questions")]
    [Operation("AI扩题", "form", null, null, null,
        FeedbackTitle = "AI扩题结果",
        FeedbackBodyTpl = @"{
            ""type"": ""form"",
            ""body"": [
                {
                    ""type"": ""alert"",
                    ""level"": ""success"",
                    ""body"": ""AI扩题完成！成功生成 ${successCount} 道新题目。"",
                    ""className"": ""mb-3""
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""扩展说明""
                },
                {
                    ""type"": ""tpl"",
                    ""tpl"": ""${expandDescription}"",
                    ""className"": ""mb-3""
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""生成的题目""
                },
                {
                    ""type"": ""each"",
                    ""name"": ""generatedQuestions"",
                    ""items"": {
                        ""type"": ""panel"",
                        ""className"": ""mb-2"",
                        ""body"": [
                            {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='mb-2'><strong>${item.orderIndex}. [${item.type}] ${item.title}</strong></div>""
                            },
                            {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='text-muted'>${item.description}</div>"",
                                ""visibleOn"": ""item.description""
                            }
                        ]
                    }
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""扩展建议"",
                    ""visibleOn"": ""suggestions && suggestions.length > 0""
                },
                {
                    ""type"": ""each"",
                    ""name"": ""suggestions"",
                    ""items"": {
                        ""type"": ""tpl"",
                        ""tpl"": ""<div class='mb-2'><i class='fa fa-lightbulb text-warning'></i> ${item}</div>""
                    },
                    ""visibleOn"": ""suggestions && suggestions.length > 0""
                },
                {
                    ""type"": ""divider""
                },
                {
                    ""type"": ""flex"",
                    ""justify"": ""space-between"",
                    ""items"": [
                        {
                            ""type"": ""button"",
                            ""label"": ""查看题目管理"",
                            ""actionType"": ""link"",
                            ""link"": ""/survey/questions?surveyId=${surveyId}"",
                            ""level"": ""primary""
                        },
                        {
                            ""type"": ""button"",
                            ""label"": ""预览问卷"",
                            ""actionType"": ""dialog"",
                            ""dialog"": {
                                ""title"": ""预览问卷"",
                                ""size"": ""xl"",
                                ""body"": {
                                    ""type"": ""service"",
                                    ""schemaApi"": ""get:/survey/api/survey/surveys/${surveyId}/questions-preview""
                                }
                            },
                            ""level"": ""info""
                        }
                    ]
                }
            ]
        }",
        FeedBackSize = "lg",
        Icon = "fa fa-plus-circle")]
    [DisplayName("AI扩题")]
    public async Task<ActionResult<ApiResponse<ExpandQuestionsResult>>> ExpandQuestions([FromBody] ExpandQuestionsRequest request)
    {
        var result = await _llmGeneratorService.ExpandQuestionsAsync(request);
        return SuccessResponse(result);
    }

    ///// <summary>
    ///// 压缩提示词
    ///// </summary>
    ///// <param name="request">压缩请求</param>
    ///// <returns>压缩结果</returns>
    //[HttpPost("ai/compress-prompt")]
    //[Operation("压缩提示词", "form")]
    //[DisplayName("压缩提示词")]
    //public async Task<ActionResult<ApiResponse<CompressPromptResult>>> CompressPrompt([FromBody] CompressPromptRequest request)
    //{
    //    var compressedPrompt = await _llmGeneratorService.CompressPromptAsync(request.Prompt, request.MaxLength);
        
    //    var result = new CompressPromptResult
    //    {
    //        OriginalPrompt = request.Prompt,
    //        CompressedPrompt = compressedPrompt,
    //        OriginalLength = request.Prompt.Length,
    //        CompressedLength = compressedPrompt.Length,
    //        CompressionRatio = (double)compressedPrompt.Length / request.Prompt.Length
    //    };

    //    return SuccessResponse(result);
    //}

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
        var questions = await _questionService.GetQuestionsBySurveyIdAsync(id);

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
                        <span>状态：{survey.Status.GetDisplayName()}</span> | 
                        <span>访问类型：{survey.AccessType.GetDisplayName()}</span>
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
                        var singleChoiceOptions = new JArray();
                        foreach (var option in question.Options.OrderBy(o => o.OrderIndex))
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
                        var multiChoiceOptions = new JArray();
                        foreach (var option in question.Options.OrderBy(o => o.OrderIndex))
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

    #region 题目管理相关方法

    /// <summary>
    /// 获取问卷的题目列表
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>题目列表</returns>
    [HttpGet("{id}/questions")]
    [DisplayName("获取问卷题目")]
    public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetSurveyQuestions(int id)
    {
        // 验证问卷是否存在
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        if (survey == null)
        {
            return NotFound("问卷不存在");
        }

        // 获取题目列表
        var questions = await _questionService.GetQuestionsBySurveyIdAsync(id);

        return SuccessResponse(questions);
    }

    /// <summary>
    /// 复制题目到当前问卷或其他问卷
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <param name="request">复制请求</param>
    /// <returns>复制的题目</returns>
    [HttpPost("questions/{questionId}/copy")]
    [DisplayName("复制题目")]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CopyQuestionToSurvey(int questionId, [FromBody] CopyQuestionToSurveyRequest request)
    {
        // 验证目标问卷是否存在
        var targetSurvey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(request.TargetSurveyId);
        if (targetSurvey == null)
        {
            return NotFound("目标问卷不存在");
        }

        // 使用QuestionService复制题目
        var copiedQuestion = await _questionService.CopyQuestionToSurveyAsync(questionId, request.TargetSurveyId, request.NewTitle);

        return SuccessResponse(copiedQuestion);
    }

    /// <summary>
    /// 题目管理操作
    /// </summary>
    /// <returns>操作结果</returns>
    [Operation("题目管理", "link", "/survey/questions?surveyId=$id", null, visibleOn: "status == 0", Icon = "fa-solid fa-question-circle")]
    [DisplayName("题目管理")]
    public ActionResult<ApiResponse> Questions_Manager()
    {
        return SuccessResponse();
    }

    /// <summary>
    /// AI扩题操作（针对特定问卷）
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <param name="request">扩题请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/ai/expand-questions")]
    [Operation("AI扩题", "form", null, null, visibleOn: "status == 0",
        InitApi = "/survey/api/survey/surveys/{id}/expand-questions-init",
        FeedbackTitle = "AI扩题结果",
        FeedbackBodyTpl = @"{
            ""type"": ""form"",
            ""body"": [
                {
                    ""type"": ""alert"",
                    ""level"": ""success"",
                    ""body"": ""AI扩题完成！成功为问卷生成了 ${successCount} 道新题目。"",
                    ""className"": ""mb-3""
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""扩展说明""
                },
                {
                    ""type"": ""tpl"",
                    ""tpl"": ""${expandDescription}"",
                    ""className"": ""mb-3""
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""生成的题目 (${generatedQuestions.length}道)""
                },
                {
                    ""type"": ""each"",
                    ""name"": ""generatedQuestions"",
                    ""items"": {
                        ""type"": ""panel"",
                        ""className"": ""mb-2 p-3 border rounded"",
                        ""body"": [
                            {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='mb-2'><strong>第${item.orderIndex}题 [${item.type | raw}] ${item.title}</strong></div>""
                            },
                            {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='text-muted mb-2'>${item.description}</div>"",
                                ""visibleOn"": ""item.description""
                            },
                            {
                                ""type"": ""tpl"",
                                ""tpl"": ""<div class='text-success'><i class='fa fa-check'></i> 必填题目</div>"",
                                ""visibleOn"": ""item.isRequired""
                            }
                        ]
                    }
                },
                {
                    ""type"": ""divider"",
                    ""title"": ""扩展建议"",
                    ""visibleOn"": ""suggestions && suggestions.length > 0""
                },
                {
                    ""type"": ""each"",
                    ""name"": ""suggestions"",
                    ""items"": {
                        ""type"": ""tpl"",
                        ""tpl"": ""<div class='mb-2'><i class='fa fa-lightbulb text-warning'></i> ${item}</div>""
                    },
                    ""visibleOn"": ""suggestions && suggestions.length > 0""
                },
                {
                    ""type"": ""divider""
                },
                {
                    ""type"": ""flex"",
                    ""justify"": ""space-between"",
                    ""items"": [
                        {
                            ""type"": ""button"",
                            ""label"": ""查看题目管理"",
                            ""actionType"": ""link"",
                            ""link"": ""/survey/questions?surveyId="" + id,
                            ""level"": ""primary"",
                            ""icon"": ""fa fa-list""
                        },
                        {
                            ""type"": ""button"",
                            ""label"": ""预览问卷"",
                            ""actionType"": ""dialog"",
                            ""dialog"": {
                                ""title"": ""预览问卷"",
                                ""size"": ""xl"",
                                ""body"": {
                                    ""type"": ""service"",
                                    ""schemaApi"": ""get:/survey/api/survey/surveys/"" + id + ""/questions-preview""
                                }
                            },
                            ""level"": ""info"",
                            ""icon"": ""fa fa-eye""
                        }
                    ]
                }
            ]
        }",
        FeedBackSize = "lg",
        Icon = "fa fa-plus-circle")]
    [DisplayName("AI扩题")]
    public async Task<ActionResult<ApiResponse<ExpandQuestionsResult>>> ExpandQuestionsForSurvey([FromRoute] int id, [FromBody] ExpandQuestionsRequest request)
    {
        // 设置问卷ID
        request.Id = id;
        
        var result = await _llmGeneratorService.ExpandQuestionsAsync(request);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取AI扩题初始化数据
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>初始化数据</returns>
    [HttpGet("{id}/expand-questions-init")]
    [DisplayName("获取扩题初始化数据")]
    public async Task<ActionResult<ApiResponse<ExpandQuestionsRequest>>> GetExpandQuestionsInit(int id)
    {
        // 获取问卷信息
        var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
        if (survey == null)
        {
            return NotFound("问卷不存在");
        }

        // 获取现有题目数量
        var existingQuestions = await _questionService.GetQuestionsBySurveyIdAsync(id);

        // 构建初始化数据
        var initData = new ExpandQuestionsRequest
        {
            Id = id,
            ExpandCount = 5, // 默认扩展5题
            MaintainStyle = true,
            InsertPosition = "end",
            ExpandDirection = existingQuestions.Count > 0 
                ? $"基于现有{existingQuestions.Count}道题目，进一步深入了解相关主题" 
                : "为问卷添加基础题目"
        };

        return SuccessResponse(initData);
    }

    #endregion
}