using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Dtos.Settings;
using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Services;
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
    private readonly IQuestionService _questionService;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// 初始化问卷管理控制器
    /// </summary>
    /// <param name="surveyService">问卷服务</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="llmGeneratorService">LLM生成服务</param>
    /// <param name="questionService">题目服务</param>
    /// <param name="logger">日志记录器</param>
    public SurveysController(
        ISurveyService surveyService,
        ISurveySettingsService settingsService,
        ISurveyLLMGeneratorService llmGeneratorService,
        IQuestionService questionService,
        ILogger<SurveysController> logger)
    {
        ArgumentNullException.ThrowIfNull(surveyService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(llmGeneratorService);
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(logger);

        _surveyService = surveyService;
        _settingsService = settingsService;
        _llmGeneratorService = llmGeneratorService;
        _questionService = questionService;
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
        return SuccessResponseWithCreate("GetSurvey", survey);
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

    /// <summary>
    /// 从模板创建问卷
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>创建的问卷</returns>
    [HttpPost("templates/{templateId}/create")]
    [HeaderOperation("从模板创建", "form")]
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
    /// 根据主题生成问卷建议
    /// </summary>
    /// <param name="request">生成建议请求</param>
    /// <returns>问卷建议数据</returns>
    [HttpPost("generate-suggestions")]
    [DisplayName("生成问卷建议")]
    public async Task<ActionResult<ApiResponse<GenerateSurveyRequest>>> GenerateSurveyFieldSuggestions([FromBody] GenerateSurveyRequest request)
    {
        // 如果主题为空，返回错误
        if (string.IsNullOrEmpty(request.Topic?.Trim()))
        {
            return BadResponse<GenerateSurveyRequest>("请先输入问卷主题");
        }

        // 基于主题生成其他字段的建议
        var suggestions = await _llmGeneratorService.GenerateFieldSuggestionsAsync(request.Topic);
        
        // 返回包含建议内容的请求对象
        var result = new GenerateSurveyRequest
        {
            Topic = request.Topic,
            Description = suggestions.Description,
            SurveyType = suggestions.SurveyType,
            QuestionCount = suggestions.QuestionCount,
            TargetAudience = suggestions.TargetAudience,
            Goals = suggestions.Goals,
            CustomPrompt = request.CustomPrompt // 保持原有的自定义提示词
        };

        return SuccessResponse(result);
    }

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
    [Operation("AI洞察分析", "ajax", null, null, null, 
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

    ///// <summary>
    ///// 批量编辑题目
    ///// </summary>
    ///// <param name="request">批量编辑请求</param>
    ///// <returns>操作结果</returns>
    //[HttpPut("questions/batch-edit")]
    //[Operation("批量编辑", "form", Icon = "fa-solid fa-edit")]
    //[DisplayName("批量编辑题目")]
    //public async Task<ActionResult<ApiResponse>> BatchEditQuestions([FromBody] BatchEditQuestionsRequest request)
    //{
    //    await _surveyService.BatchEditQuestionsAsync(request);
    //    return SuccessResponse("题目批量编辑成功");
    //}

    ///// <summary>
    ///// 批量删除题目
    ///// </summary>
    ///// <param name="request">批量删除请求</param>
    ///// <returns>操作结果</returns>
    //[HttpDelete("questions/batch-delete")]
    //[Operation("批量删除", "form", Icon = "fa-solid fa-trash")]
    //[DisplayName("批量删除题目")]
    //public async Task<ActionResult<ApiResponse>> BatchDeleteQuestions([FromBody] BatchDeleteQuestionsRequest request)
    //{
    //    await _surveyService.BatchDeleteQuestionsAsync(request);
    //    return SuccessResponse("题目批量删除成功");
    //}

    ///// <summary>
    ///// 拖拽排序题目
    ///// </summary>
    ///// <param name="request">排序请求</param>
    ///// <returns>操作结果</returns>
    //[HttpPut("questions/drag-sort")]
    //[Operation("拖拽排序", "ajax", Icon = "fa-solid fa-sort")]
    //[DisplayName("拖拽排序题目")]
    //public async Task<ActionResult<ApiResponse>> DragSortQuestions([FromBody] DragSortQuestionsRequest request)
    //{
    //    await _surveyService.DragSortQuestionsAsync(request);
    //    return SuccessResponse("题目排序成功");
    //}

    ///// <summary>
    ///// 快速添加题目
    ///// </summary>
    ///// <param name="request">快速添加请求</param>
    ///// <returns>创建的题目</returns>
    //[HttpPost("questions/quick-add")]
    //[DisplayName("快速添加题目")]
    //public async Task<ActionResult<ApiResponse<QuestionDto>>> QuickAddQuestion([FromBody] QuickAddQuestionRequest request)
    //{
    //    // 验证问卷是否存在
    //    var survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(request.SurveyId);
    //    if (survey == null)
    //    {
    //        return NotFound("问卷不存在");
    //    }

    //    // 构建创建题目DTO
    //    var createQuestionDto = new CreateQuestionDto
    //    {
    //        SurveyId = request.SurveyId,
    //        Title = request.Title,
    //        Description = request.Description,
    //        Type = request.Type,
    //        IsRequired = request.IsRequired,
    //        OrderIndex = 0, // QuestionService会自动计算排序索引
    //        Options = request.Options?.Select((option, index) => new CreateQuestionOptionDto
    //        {
    //            Text = option,
    //            OrderIndex = index
    //        }).ToList() ?? new List<CreateQuestionOptionDto>()
    //    };

    //    // 使用QuestionService创建题目
    //    var questionDto = await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).CreateAsync(createQuestionDto);

    //    return SuccessResponse(questionDto);
    //}

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
    /// 获取问卷编辑器配置
    /// </summary>
    /// <param name="id">问卷ID，如果为0则创建新问卷</param>
    /// <returns>问卷编辑器Amis配置</returns>
    [HttpGet("{id}/editor")]
    [Operation(label: "问卷编辑", actionType: "service")]
    [DisplayName("问卷编辑")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetSurveyEditorConfig(int id)
    {
        SurveyDto? survey = null;
        List<QuestionDto> questions = new();

        // 如果是编辑模式，获取现有问卷和题目
        if (id > 0)
        {
            survey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(id);
            if (survey == null)
            {
                return NotFound("问卷不存在");
            }
            questions = await _questionService.GetQuestionsBySurveyIdAsync(id);
        }

        var editorConfig = BuildSurveyEditorConfig(survey, questions);
        return SuccessResponse(editorConfig);
    }

    /// <summary>
    /// 保存问卷编辑器数据
    /// </summary>
    /// <param name="request">编辑器保存请求</param>
    /// <returns>保存结果</returns>
    [HttpPost("editor/save")]
    [DisplayName("保存问卷编辑")]
    public async Task<ActionResult<ApiResponse<SurveyDto>>> SaveSurveyEditor([FromBody] SaveSurveyEditorRequest request)
    {
        SurveyDto resultSurvey;

        if (request.SurveyId > 0)
        {
            // 更新现有问卷
            var updateDto = new UpdateSurveyDto
            {
                Title = request.Title,
                Description = request.Description,
                AccessType = request.AccessType,
                ExpiresAt = request.ExpiresAt,
                IsTemplate = request.IsTemplate
            };
            
            await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).UpdateAsync(request.SurveyId, updateDto);
            resultSurvey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).GetAsync(request.SurveyId);
        }
        else
        {
            // 创建新问卷
            var createDto = new CreateSurveyDto
            {
                Title = request.Title,
                Description = request.Description,
                AccessType = request.AccessType,
                ExpiresAt = request.ExpiresAt,
                IsTemplate = request.IsTemplate
            };
            
            resultSurvey = await ((IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>)_surveyService).CreateAsync(createDto);
        }

        // 保存题目数据
        if (request.Questions?.Any() == true)
        {
            await SaveEditorQuestions(resultSurvey.Id, request.Questions);
        }

        return SuccessResponse(resultSurvey);
    }

    /// <summary>
    /// 获取题目类型模板配置
    /// </summary>
    /// <returns>题目类型模板及对应的Amis组件配置</returns>
    [HttpGet("editor/question-templates")]
    [DisplayName("获取题目模板")]
    public Task<ActionResult<ApiResponse<object>>> GetQuestionTemplates()
    {
        var templates = new List<object>
        {
            new
            {
                type = QuestionType.SingleChoice.ToString(),
                typeValue = (int)QuestionType.SingleChoice,
                name = "单选题",
                icon = "fa-solid fa-dot-circle",
                description = "从多个选项中选择一个答案",
                category = "选择题",
                defaultOptions = new[] { "选项1", "选项2", "选项3" },
                amisComponent = new
                {
                    type = "radios",
                    mode = "horizontal",
                    options = new[]
                    {
                        new { label = "选项1", value = "选项1" },
                        new { label = "选项2", value = "选项2" },
                        new { label = "选项3", value = "选项3" }
                    }
                },
                validation = new
                {
                    required = true,
                    minItems = 1,
                    maxItems = 1
                },
                settings = new
                {
                    allowOther = false,
                    randomOrder = false,
                    displayMode = "vertical"
                }
            },
            new
            {
                type = QuestionType.MultipleChoice.ToString(),
                typeValue = (int)QuestionType.MultipleChoice,
                name = "多选题", 
                icon = "fa-solid fa-check-square",
                description = "从多个选项中选择多个答案",
                category = "选择题",
                defaultOptions = new[] { "选项1", "选项2", "选项3" },
                amisComponent = new
                {
                    type = "checkboxes",
                    options = new[]
                    {
                        new { label = "选项1", value = "选项1" },
                        new { label = "选项2", value = "选项2" },
                        new { label = "选项3", value = "选项3" }
                    }
                },
                validation = new
                {
                    required = false,
                    minItems = 0,
                    maxItems = 0 // 0表示无限制
                },
                settings = new
                {
                    allowOther = false,
                    randomOrder = false,
                    displayMode = "vertical"
                }
            },
            new
            {
                type = QuestionType.Text.ToString(),
                typeValue = (int)QuestionType.Text,
                name = "填空题",
                icon = "fa-solid fa-edit",
                description = "输入短文本答案",
                category = "文本题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-text",
                    placeholder = "请输入答案",
                    clearable = true
                },
                validation = new
                {
                    required = false,
                    minLength = 0,
                    maxLength = 200,
                    pattern = ""
                },
                settings = new
                {
                    placeholder = "请输入答案",
                    inputType = "text"
                }
            },
            new
            {
                type = QuestionType.Textarea.ToString(),
                typeValue = (int)QuestionType.Textarea,
                name = "长文本题",
                icon = "fa-solid fa-align-left",
                description = "输入长文本答案",
                category = "文本题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "textarea",
                    placeholder = "请输入详细答案",
                    minRows = 3,
                    maxRows = 8,
                    showCounter = true
                },
                validation = new
                {
                    required = false,
                    minLength = 0,
                    maxLength = 1000
                },
                settings = new
                {
                    placeholder = "请输入详细答案",
                    rows = 4
                }
            },
            new
            {
                type = QuestionType.Number.ToString(),
                typeValue = (int)QuestionType.Number,
                name = "数字题",
                icon = "fa-solid fa-calculator",
                description = "输入数字答案",
                category = "数值题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-number",
                    placeholder = "请输入数字",
                    precision = 2,
                    showSteps = true
                },
                validation = new
                {
                    required = false,
                    min = (double?)null,
                    max = (double?)null,
                    step = 1.0
                },
                settings = new
                {
                    placeholder = "请输入数字",
                    precision = 2,
                    unit = ""
                }
            },
            new
            {
                type = QuestionType.Rating.ToString(),
                typeValue = (int)QuestionType.Rating,
                name = "评分题",
                icon = "fa-solid fa-star",
                description = "通过星级评分",
                category = "评价题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-rating",
                    count = 5,
                    allowHalf = false,
                    readOnly = false,
                    tooltip = new[] { "很差", "较差", "一般", "较好", "很好" }
                },
                validation = new
                {
                    required = false,
                    min = 1,
                    max = 5
                },
                settings = new
                {
                    maxRating = 5,
                    allowHalf = false,
                    showText = true
                }
            },
            new
            {
                type = QuestionType.Date.ToString(),
                typeValue = (int)QuestionType.Date,
                name = "日期题",
                icon = "fa-solid fa-calendar",
                description = "选择日期",
                category = "时间题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-date",
                    format = "YYYY-MM-DD",
                    placeholder = "请选择日期",
                    clearable = true
                },
                validation = new
                {
                    required = false,
                    minDate = (string?)null,
                    maxDate = (string?)null
                },
                settings = new
                {
                    format = "YYYY-MM-DD",
                    placeholder = "请选择日期"
                }
            },
            new
            {
                type = QuestionType.Time.ToString(),
                typeValue = (int)QuestionType.Time,
                name = "时间题",
                icon = "fa-solid fa-clock",
                description = "选择时间",
                category = "时间题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-time",
                    format = "HH:mm",
                    placeholder = "请选择时间",
                    clearable = true
                },
                validation = new
                {
                    required = false,
                    minTime = (string?)null,
                    maxTime = (string?)null
                },
                settings = new
                {
                    format = "HH:mm",
                    placeholder = "请选择时间"
                }
            },
            new
            {
                type = QuestionType.DateTime.ToString(),
                typeValue = (int)QuestionType.DateTime,
                name = "日期时间题",
                icon = "fa-solid fa-calendar-alt",
                description = "选择日期和时间",
                category = "时间题",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "input-datetime",
                    format = "YYYY-MM-DD HH:mm",
                    placeholder = "请选择日期时间",
                    clearable = true
                },
                validation = new
                {
                    required = false,
                    minDateTime = (string?)null,
                    maxDateTime = (string?)null
                },
                settings = new
                {
                    format = "YYYY-MM-DD HH:mm",
                    placeholder = "请选择日期时间"
                }
            },
            new
            {
                type = QuestionType.Matrix.ToString(),
                typeValue = (int)QuestionType.Matrix,
                name = "矩阵题",
                icon = "fa-solid fa-table",
                description = "矩阵选择题",
                category = "高级题型",
                defaultOptions = (string[]?)null,
                amisComponent = new
                {
                    type = "matrix-checkboxes",
                    columns = new[]
                    {
                        new { label = "非常满意", value = "5" },
                        new { label = "满意", value = "4" },
                        new { label = "一般", value = "3" },
                        new { label = "不满意", value = "2" },
                        new { label = "非常不满意", value = "1" }
                    },
                    rows = new[]
                    {
                        new { label = "产品质量", value = "quality" },
                        new { label = "服务态度", value = "service" },
                        new { label = "价格合理性", value = "price" }
                    }
                },
                validation = new
                {
                    required = false
                },
                settings = new
                {
                    matrixType = "radio", // radio 或 checkbox
                    columns = new[] { "非常满意", "满意", "一般", "不满意", "非常不满意" },
                    rows = new[] { "产品质量", "服务态度", "价格合理性" }
                }
            },
            new
            {
                type = QuestionType.Ranking.ToString(),
                typeValue = (int)QuestionType.Ranking,
                name = "排序题",
                icon = "fa-solid fa-sort",
                description = "对选项进行排序",
                category = "高级题型",
                defaultOptions = new[] { "选项A", "选项B", "选项C", "选项D" },
                amisComponent = new
                {
                    type = "input-array",
                    inline = false,
                    draggable = true,
                    items = new
                    {
                        type = "input-text",
                        placeholder = "选项内容"
                    }
                },
                validation = new
                {
                    required = false,
                    minItems = 2,
                    maxItems = 10
                },
                settings = new
                {
                    allowDuplicate = false,
                    randomOrder = false,
                    defaultOptions = new[] { "选项A", "选项B", "选项C", "选项D" }
                }
            }
        };

        // 按分类组织数据
        var groupedTemplates = templates
            .GroupBy(t => ((dynamic)t).category)
            .Select(g => new
            {
                category = g.Key,
                items = g.ToList()
            })
            .ToList();

        return Task.FromResult<ActionResult<ApiResponse<object>>>(SuccessResponse<object>(new
        {
            templates = templates,
            groupedTemplates = groupedTemplates,
            categories = templates.Select(t => ((dynamic)t).category).Distinct().ToList()
        }));
    }

    /// <summary>
    /// 获取指定题目类型的Amis组件配置
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <returns>Amis组件配置</returns>
    [HttpGet("editor/question-component/{questionType}")]
    [DisplayName("获取题目组件配置")]
    public Task<ActionResult<ApiResponse<JObject>>> GetQuestionAmisComponent(string questionType)
    {
        if (!Enum.TryParse<QuestionType>(questionType, true, out var type))
        {
            return Task.FromResult<ActionResult<ApiResponse<JObject>>>(BadRequest("无效的题目类型"));
        }

        var component = GetAmisComponentForQuestionType(type);
        
        // 添加题目特定的配置
        component["name"] = $"question_{DateTime.Now.Ticks}";
        component["label"] = GetQuestionTypeName(type.ToString());
        component["required"] = false;

        return Task.FromResult<ActionResult<ApiResponse<JObject>>>(SuccessResponse(component));
    }

    /// <summary>
    /// 批量获取题目类型的Amis组件配置
    /// </summary>
    /// <param name="questionTypes">题目类型列表</param>
    /// <returns>Amis组件配置映射</returns>
    [HttpPost("editor/question-components")]
    [DisplayName("批量获取题目组件配置")]
    public Task<ActionResult<ApiResponse<Dictionary<string, JObject>>>> GetQuestionAmisComponents([FromBody] string[] questionTypes)
    {
        var components = new Dictionary<string, JObject>();

        foreach (var questionType in questionTypes)
        {
            if (Enum.TryParse<QuestionType>(questionType, true, out var type))
            {
                var component = GetAmisComponentForQuestionType(type);
                component["name"] = $"question_{type}";
                component["label"] = GetQuestionTypeName(type.ToString());
                component["required"] = false;
                
                components[questionType] = component;
            }
        }

        return Task.FromResult<ActionResult<ApiResponse<Dictionary<string, JObject>>>>(SuccessResponse(components));
    }

    /// <summary>
    /// 根据题目类型获取对应的Amis组件配置
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <param name="options">题目选项</param>
    /// <returns>Amis组件配置</returns>
    private JObject GetAmisComponentForQuestionType(QuestionType questionType, List<QuestionOptionDto>? options = null)
    {
        switch (questionType)
        {
            case QuestionType.SingleChoice:
                var singleOptions = options?.OrderBy(o => o.OrderIndex).Select(o => new JObject
                {
                    ["label"] = o.Text,
                    ["value"] = o.Value ?? o.Text
                }).ToArray() ?? new JObject[0];
                
                return new JObject
                {
                    ["type"] = "radios",
                    ["mode"] = "horizontal",
                    ["options"] = new JArray(singleOptions)
                };

            case QuestionType.MultipleChoice:
                var multiOptions = options?.OrderBy(o => o.OrderIndex).Select(o => new JObject
                {
                    ["label"] = o.Text,
                    ["value"] = o.Value ?? o.Text
                }).ToArray() ?? new JObject[0];
                
                return new JObject
                {
                    ["type"] = "checkboxes",
                    ["options"] = new JArray(multiOptions)
                };

            case QuestionType.Text:
                return new JObject
                {
                    ["type"] = "input-text",
                    ["placeholder"] = "请输入答案",
                    ["clearable"] = true
                };

            case QuestionType.Textarea:
                return new JObject
                {
                    ["type"] = "textarea",
                    ["placeholder"] = "请输入详细答案",
                    ["minRows"] = 3,
                    ["maxRows"] = 8,
                    ["showCounter"] = true
                };

            case QuestionType.Number:
                return new JObject
                {
                    ["type"] = "input-number",
                    ["placeholder"] = "请输入数字",
                    ["precision"] = 2,
                    ["showSteps"] = true
                };

            case QuestionType.Rating:
                return new JObject
                {
                    ["type"] = "input-rating",
                    ["count"] = 5,
                    ["allowHalf"] = false,
                    ["tooltip"] = new JArray { "很差", "较差", "一般", "较好", "很好" }
                };

            case QuestionType.Date:
                return new JObject
                {
                    ["type"] = "input-date",
                    ["format"] = "YYYY-MM-DD",
                    ["placeholder"] = "请选择日期",
                    ["clearable"] = true
                };

            case QuestionType.Time:
                return new JObject
                {
                    ["type"] = "input-time",
                    ["format"] = "HH:mm",
                    ["placeholder"] = "请选择时间",
                    ["clearable"] = true
                };

            case QuestionType.DateTime:
                return new JObject
                {
                    ["type"] = "input-datetime",
                    ["format"] = "YYYY-MM-DD HH:mm",
                    ["placeholder"] = "请选择日期时间",
                    ["clearable"] = true
                };

            case QuestionType.Matrix:
                return new JObject
                {
                    ["type"] = "matrix-checkboxes",
                    ["columns"] = new JArray
                    {
                        new JObject { ["label"] = "非常满意", ["value"] = "5" },
                        new JObject { ["label"] = "满意", ["value"] = "4" },
                        new JObject { ["label"] = "一般", ["value"] = "3" },
                        new JObject { ["label"] = "不满意", ["value"] = "2" },
                        new JObject { ["label"] = "非常不满意", ["value"] = "1" }
                    },
                    ["rows"] = new JArray
                    {
                        new JObject { ["label"] = "产品质量", ["value"] = "quality" },
                        new JObject { ["label"] = "服务态度", ["value"] = "service" },
                        new JObject { ["label"] = "价格合理性", ["value"] = "price" }
                    }
                };

            case QuestionType.Ranking:
                var rankingOptions = options?.OrderBy(o => o.OrderIndex).Select(o => o.Text).ToArray() 
                    ?? new[] { "选项A", "选项B", "选项C", "选项D" };
                
                return new JObject
                {
                    ["type"] = "input-array",
                    ["inline"] = false,
                    ["draggable"] = true,
                    ["value"] = new JArray(rankingOptions),
                    ["items"] = new JObject
                    {
                        ["type"] = "input-text",
                        ["placeholder"] = "选项内容"
                    }
                };

            default:
                return new JObject
                {
                    ["type"] = "input-text",
                    ["placeholder"] = "请输入答案"
                };
        }
    }

    /// <summary>
    /// 构建问卷编辑器配置
    /// </summary>
    /// <param name="survey">问卷数据</param>
    /// <param name="questions">题目列表</param>
    /// <returns>编辑器配置</returns>
    private JObject BuildSurveyEditorConfig(SurveyDto? survey, List<QuestionDto> questions)
    {
        var isNewSurvey = survey == null;
        var surveyId = survey?.Id ?? 0;

        var editorConfig = new JObject
        {
            ["type"] = "page",
            ["title"] = isNewSurvey ? "创建问卷" : $"编辑问卷 - {survey!.Title}",
            ["body"] = new JArray
            {
                // 工具栏
                new JObject
                {
                    ["type"] = "flex",
                    ["className"] = "survey-editor-toolbar",
                    ["style"] = new JObject
                    {
                        ["padding"] = "16px",
                        ["backgroundColor"] = "#f5f5f5",
                        ["borderBottom"] = "1px solid #e8e8e8"
                    },
                    ["items"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "预览",
                            ["icon"] = "fa fa-eye",
                            ["level"] = "info",
                            ["size"] = "sm",
                            ["actionType"] = "dialog",
                            ["dialog"] = new JObject
                            {
                                ["title"] = "问卷预览",
                                ["size"] = "xl",
                                ["body"] = new JObject
                                {
                                    ["type"] = "service",
                                    ["schemaApi"] = $"get:/survey/api/survey/surveys/{surveyId}/questions-preview"
                                }
                            }
                        },
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "保存",
                            ["icon"] = "fa fa-save",
                            ["level"] = "primary",
                            ["size"] = "sm",
                            ["actionType"] = "ajax",
                            ["api"] = "post:/survey/api/survey/surveys/editor/save",
                            ["data"] = new JObject
                            {
                                ["surveyId"] = surveyId,
                                ["&"] = "$$"
                            }
                        },
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "返回",
                            ["icon"] = "fa fa-arrow-left",
                            ["level"] = "default",
                            ["size"] = "sm",
                            ["actionType"] = "url",
                            ["url"] = "/survey/surveys"
                        }
                    }
                },

                // 主编辑区域
                new JObject
                {
                    ["type"] = "flex",
                    ["direction"] = "row",
                    ["style"] = new JObject { ["height"] = "calc(100vh - 120px)" },
                    ["items"] = new JArray
                    {
                        // 左侧题目类型面板
                        BuildQuestionTypesPanel(),
                        
                        // 中间编辑区域
                        BuildMainEditorArea(survey, questions),
                        
                        // 右侧属性面板
                        BuildPropertiesPanel()
                    }
                }
            }
        };

        return editorConfig;
    }

    /// <summary>
    /// 构建题目类型面板
    /// </summary>
    /// <returns>题目类型面板配置</returns>
    private JObject BuildQuestionTypesPanel()
    {
        return new JObject
        {
            ["type"] = "panel",
            ["title"] = "题目类型",
            ["className"] = "question-types-panel",
            ["style"] = new JObject
            {
                ["width"] = "280px",
                ["borderRight"] = "1px solid #e8e8e8",
                ["padding"] = "16px",
                ["maxHeight"] = "calc(100vh - 120px)",
                ["overflowY"] = "auto"
            },
            ["body"] = new JObject
            {
                ["type"] = "service",
                ["schemaApi"] = "get:/survey/api/survey/surveys/editor/question-templates",
                ["body"] = new JArray
                {
                    // 按分类显示题目类型
                    new JObject
                    {
                        ["type"] = "each",
                        ["name"] = "data.groupedTemplates",
                        ["items"] = new JObject
                        {
                            ["type"] = "collapse",
                            ["header"] = "${category}",
                            ["className"] = "question-category-collapse",
                            ["style"] = new JObject
                            {
                                ["marginBottom"] = "12px"
                            },
                            ["body"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "each",
                                    ["name"] = "items",
                                    ["items"] = new JObject
                                    {
                                        ["type"] = "flex",
                                        ["className"] = "question-type-item",
                                        ["style"] = new JObject
                                        {
                                            ["marginBottom"] = "8px",
                                            ["padding"] = "8px",
                                            ["border"] = "1px solid #e8e8e8",
                                            ["borderRadius"] = "6px",
                                            ["cursor"] = "pointer",
                                            ["transition"] = "all 0.3s ease"
                                        },
                                        ["items"] = new JArray
                                        {
                                            new JObject
                                            {
                                                ["type"] = "icon",
                                                ["icon"] = "${icon}",
                                                ["className"] = "question-type-icon",
                                                ["style"] = new JObject
                                                {
                                                    ["marginRight"] = "8px",
                                                    ["color"] = "#666",
                                                    ["fontSize"] = "16px"
                                                }
                                            },
                                            new JObject
                                            {
                                                ["type"] = "container",
                                                ["style"] = new JObject
                                                {
                                                    ["flex"] = "1"
                                                },
                                                ["body"] = new JArray
                                                {
                                                    new JObject
                                                    {
                                                        ["type"] = "tpl",
                                                        ["tpl"] = "${name}",
                                                        ["className"] = "question-type-name",
                                                        ["style"] = new JObject
                                                        {
                                                            ["fontWeight"] = "500",
                                                            ["fontSize"] = "14px",
                                                            ["color"] = "#333",
                                                            ["marginBottom"] = "2px"
                                                        }
                                                    },
                                                    new JObject
                                                    {
                                                        ["type"] = "tpl",
                                                        ["tpl"] = "${description}",
                                                        ["className"] = "question-type-desc",
                                                        ["style"] = new JObject
                                                        {
                                                            ["fontSize"] = "12px",
                                                            ["color"] = "#999",
                                                            ["lineHeight"] = "1.4"
                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        ["onEvent"] = new JObject
                                        {
                                            ["click"] = new JObject
                                            {
                                                ["actions"] = new JArray
                                                {
                                                    new JObject
                                                    {
                                                        ["actionType"] = "custom",
                                                        ["script"] = @"
                                                            const item = event.data;
                                                            const questionType = item.type;
                                                            const typeValue = item.typeValue;
                                                            const defaultOptions = item.defaultOptions;
                                                            const amisComponent = item.amisComponent;
                                                            
                                                            console.log('添加题目:', questionType, amisComponent);
                                                            
                                                            if (window.addQuestion) {
                                                                window.addQuestion(questionType, typeValue, defaultOptions, amisComponent);
                                                            } else {
                                                                // 兜底方案：使用Amis的事件机制
                                                                doAction({
                                                                    actionType: 'broadcast',
                                                                    eventName: 'addQuestion',
                                                                    data: {
                                                                        type: questionType,
                                                                        typeValue: typeValue,
                                                                        defaultOptions: defaultOptions,
                                                                        amisComponent: amisComponent
                                                                    }
                                                                });
                                                            }
                                                        "
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// 构建主编辑区域
    /// </summary>
    /// <param name="survey">问卷数据</param>
    /// <param name="questions">题目列表</param>
    /// <returns>主编辑区域配置</returns>
    private JObject BuildMainEditorArea(SurveyDto? survey, List<QuestionDto> questions)
    {
        var questionsData = JArray.FromObject(questions.OrderBy(q => q.OrderIndex).Select(q => new
        {
            id = q.Id,
            title = q.Title,
            description = q.Description,
            type = q.Type.ToString(),
            typeName = GetQuestionTypeName(q.Type.ToString()),
            isRequired = q.IsRequired,
            orderIndex = q.OrderIndex,
            options = q.Options.OrderBy(o => o.OrderIndex).Select(o => new
            {
                id = o.Id,
                text = o.Text,
                value = o.Value,
                orderIndex = o.OrderIndex,
                isOther = o.IsOther
            }).ToArray()
        }).ToArray());

        return new JObject
        {
            ["type"] = "panel",
            ["title"] = "问卷设计",
            ["className"] = "survey-main-editor",
            ["style"] = new JObject
            {
                ["flex"] = "1",
                ["padding"] = "16px",
                ["overflow"] = "auto"
            },
            ["body"] = new JArray
            {
                // 问卷基本信息表单
                new JObject
                {
                    ["type"] = "form",
                    ["title"] = "问卷信息",
                    ["className"] = "survey-basic-form",
                    ["body"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "input-text",
                            ["name"] = "title",
                            ["label"] = "问卷标题",
                            ["placeholder"] = "请输入问卷标题",
                            ["required"] = true,
                            ["value"] = survey?.Title ?? ""
                        },
                        new JObject
                        {
                            ["type"] = "textarea",
                            ["name"] = "description",
                            ["label"] = "问卷描述",
                            ["placeholder"] = "请输入问卷描述",
                            ["value"] = survey?.Description ?? ""
                        }
                    }
                },

                // 题目列表
                new JObject
                {
                    ["type"] = "panel",
                    ["title"] = "题目设计",
                    ["className"] = "questions-designer",
                    ["body"] = new JObject
                    {
                        ["type"] = "input-array",
                        ["name"] = "questions",
                        ["label"] = false,
                        ["addable"] = false,
                        ["removable"] = true,
                        ["draggable"] = true,
                        ["value"] = questionsData,
                        ["items"] = BuildQuestionEditorItem()
                    }
                }
            }
        };
    }

    /// <summary>
    /// 构建属性面板
    /// </summary>
    /// <returns>属性面板配置</returns>
    private JObject BuildPropertiesPanel()
    {
        return new JObject
        {
            ["type"] = "panel",
            ["title"] = "属性设置",
            ["className"] = "properties-panel",
            ["style"] = new JObject
            {
                ["width"] = "300px",
                ["borderLeft"] = "1px solid #e8e8e8",
                ["padding"] = "16px"
            },
            ["body"] = new JObject
            {
                ["type"] = "form",
                ["body"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "select",
                        ["name"] = "accessType",
                        ["label"] = "访问类型",
                        ["options"] = new JArray
                        {
                            new JObject { ["label"] = "公开", ["value"] = "Public" },
                            new JObject { ["label"] = "私有", ["value"] = "Private" },
                            new JObject { ["label"] = "密码保护", ["value"] = "Password" }
                        }
                    },
                    new JObject
                    {
                        ["type"] = "input-datetime",
                        ["name"] = "expiresAt",
                        ["label"] = "过期时间",
                        ["format"] = "YYYY-MM-DD HH:mm:ss"
                    },
                    new JObject
                    {
                        ["type"] = "switch",
                        ["name"] = "isTemplate",
                        ["label"] = "保存为模板"
                    }
                }
            }
        };
    }

    /// <summary>
    /// 构建题目编辑项配置
    /// </summary>
    /// <returns>题目编辑项配置</returns>
    private JObject BuildQuestionEditorItem()
    {
        return new JObject
        {
            ["type"] = "panel",
            ["className"] = "question-editor-item",
            ["style"] = new JObject
            {
                ["border"] = "1px solid #e8e8e8",
                ["borderRadius"] = "4px",
                ["padding"] = "16px",
                ["marginBottom"] = "16px"
            },
            ["body"] = new JArray
            {
                // 题目头部信息
                new JObject
                {
                    ["type"] = "flex",
                    ["justify"] = "space-between",
                    ["items"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "tpl",
                            ["tpl"] = "<strong>${typeName}</strong>",
                            ["className"] = "question-type-label"
                        },
                        new JObject
                        {
                            ["type"] = "button-group",
                            ["buttons"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "button",
                                    ["icon"] = "fa fa-edit",
                                    ["size"] = "xs",
                                    ["tooltip"] = "编辑",
                                    ["actionType"] = "dialog",
                                    ["dialog"] = BuildQuestionEditDialog()
                                },
                                new JObject
                                {
                                    ["type"] = "button",
                                    ["icon"] = "fa fa-copy",
                                    ["size"] = "xs",
                                    ["tooltip"] = "复制"
                                },
                                new JObject
                                {
                                    ["type"] = "button",
                                    ["icon"] = "fa fa-trash",
                                    ["size"] = "xs",
                                    ["level"] = "danger",
                                    ["tooltip"] = "删除"
                                }
                            }
                        }
                    }
                },

                // 题目标题和描述
                new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = "${title}${isRequired ? '<span style=\"color:red\">*</span>' : ''}",
                    ["className"] = "question-title"
                },
                new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = "${description}",
                    ["className"] = "question-description",
                    ["visibleOn"] = "${description}"
                }
            }
        };
    }

    /// <summary>
    /// 构建题目编辑对话框
    /// </summary>
    /// <returns>题目编辑对话框配置</returns>
    private JObject BuildQuestionEditDialog()
    {
        return new JObject
        {
            ["title"] = "编辑题目",
            ["size"] = "lg",
            ["body"] = new JObject
            {
                ["type"] = "form",
                ["body"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "input-text",
                        ["name"] = "title",
                        ["label"] = "题目标题",
                        ["required"] = true
                    },
                    new JObject
                    {
                        ["type"] = "textarea",
                        ["name"] = "description",
                        ["label"] = "题目描述"
                    },
                    new JObject
                    {
                        ["type"] = "switch",
                        ["name"] = "isRequired",
                        ["label"] = "是否必填"
                    },
                    new JObject
                    {
                        ["type"] = "input-array",
                        ["name"] = "options",
                        ["label"] = "选项设置",
                        ["visibleOn"] = "${type === 'SingleChoice' || type === 'MultipleChoice'}",
                        ["items"] = new JObject
                        {
                            ["type"] = "input-text",
                            ["placeholder"] = "输入选项内容"
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// 保存编辑器中的题目数据
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="editorQuestions">编辑器题目数据</param>
    /// <returns>异步任务</returns>
    private async Task SaveEditorQuestions(int surveyId, List<EditorQuestionDto> editorQuestions)
    {
        // 获取现有题目
        var existingQuestions = await _questionService.GetQuestionsBySurveyIdAsync(surveyId);
        var existingQuestionIds = existingQuestions.Select(q => q.Id).ToList();
        
        // 处理题目更新和创建
        for (int i = 0; i < editorQuestions.Count; i++)
        {
            var editorQuestion = editorQuestions[i];
            editorQuestion.OrderIndex = i + 1;

            if (editorQuestion.Id > 0 && existingQuestionIds.Contains(editorQuestion.Id))
            {
                // 更新现有题目
                var updateDto = new UpdateQuestionDto
                {
                    Title = editorQuestion.Title,
                    Description = editorQuestion.Description,
                    Type = editorQuestion.Type,
                    OrderIndex = editorQuestion.OrderIndex,
                    IsRequired = editorQuestion.IsRequired,
                    Options = editorQuestion.Options?.Select(o => new UpdateQuestionOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Value = o.Value,
                        OrderIndex = o.OrderIndex,
                        IsOther = o.IsOther
                    }).ToList() ?? new List<UpdateQuestionOptionDto>()
                };

                await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).UpdateAsync(editorQuestion.Id, updateDto);
                existingQuestionIds.Remove(editorQuestion.Id);
            }
            else
            {
                // 创建新题目
                var createDto = new CreateQuestionDto
                {
                    SurveyId = surveyId,
                    Title = editorQuestion.Title,
                    Description = editorQuestion.Description,
                    Type = editorQuestion.Type,
                    OrderIndex = editorQuestion.OrderIndex,
                    IsRequired = editorQuestion.IsRequired,
                    Options = editorQuestion.Options?.Select(o => new CreateQuestionOptionDto
                    {
                        Text = o.Text,
                        Value = o.Value,
                        OrderIndex = o.OrderIndex,
                        IsOther = o.IsOther
                    }).ToList() ?? new List<CreateQuestionOptionDto>()
                };

                await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).CreateAsync(createDto);
            }
        }

        // 删除不再存在的题目
        foreach (var deletedQuestionId in existingQuestionIds)
        {
            await ((IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>)_questionService).DeleteAsync(deletedQuestionId);
        }
    }

    #endregion
}

