using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Controllers.Client;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Dtos.PracticeSetting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;
using CodeSpirit.Core.Enums;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 试卷管理
/// </summary>
[DisplayName("试卷管理")]
[Navigation(Icon = "fa-solid fa-file-lines", PlatformType = PlatformType.Tenant)]
public class ExamPapersController : ApiControllerBase
{
    private readonly IExamPaperService _examPaperService;
    private readonly IExamSettingService _examSettingService;
    private readonly IPracticeSettingService _practiceSettingService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamPapersController(IExamPaperService examPaperService,
        IExamSettingService examSettingService,
        IPracticeSettingService practiceSettingService)
    {
        _examPaperService = examPaperService;
        _examSettingService = examSettingService;
        _practiceSettingService = practiceSettingService;
    }

    /// <summary>
    /// 获取试卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>试卷分页列表</returns>
    [HttpGet]
    [DisplayName("获取试卷列表")]
    public async Task<ActionResult<ApiResponse<PageList<ExamPaperDto>>>> GetExamPapers([FromQuery] ExamPaperQueryDto queryDto)
    {
        var result = await _examPaperService.GetExamPapersAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取试卷详情
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>试卷详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取试卷详情")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> GetExamPaper(long id)
    {
        var result = await _examPaperService.GetAsync(id);
        if (result == null)
        {
            return NotFound("试卷不存在");
        }
        return SuccessResponse(result);
    }

    ///// <summary>
    ///// 创建试卷
    ///// </summary>
    ///// <param name="createDto">创建试卷DTO</param>
    ///// <returns>创建的试卷</returns>
    //[HttpPost]
    //[HeaderOperation("生成固定试卷", "form")]
    //[DisplayName("生成固定试卷")]
    //public async Task<ActionResult<ApiResponse<ExamPaperDto>>> CreateExamPaper(CreateExamPaperDto createDto)
    //{
    //    var result = await _examPaperService.CreateAsync(createDto);
    //    return SuccessResponse(result);
    //}

    /// <summary>
    /// 更新试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <param name="updateDto">更新试卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [DisplayName("编辑")]
    public async Task<ActionResult<ApiResponse>> UpdateExamPaper(long id, UpdateExamPaperDto updateDto)
    {
        await _examPaperService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此试卷吗？")]
    [DisplayName("删除试卷")]
    public async Task<ActionResult<ApiResponse>> DeleteExamPaper(long id)
    {
        await _examPaperService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/publish")]
    [Operation("发布", "ajax", null, "确定要发布此试卷吗？", visibleOn: "status == 1 && isPreviewChecked")]
    [DisplayName("发布试卷")]
    public async Task<ActionResult<ApiResponse>> PublishExamPaper(long id)
    {
        await _examPaperService.PublishExamPaperAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/unpublish")]
    [Operation("取消发布", "ajax", null, "确定要取消发布此试卷吗？", visibleOn: "status == 2")]
    [DisplayName("取消发布试卷")]
    public async Task<ActionResult<ApiResponse>> UnpublishExamPaper(long id)
    {
        await _examPaperService.UnpublishExamPaperAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量删除试卷
    /// </summary>
    /// <param name="ids">试卷ID列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch-delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的试卷吗？", isBulkOperation: true)]
    [DisplayName("批量删除试卷")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteExamPapers([FromBody] IEnumerable<long> ids)
    {
        var result = await _examPaperService.BatchDeleteAsync(ids);
        return SuccessResponse($"成功删除{result.successCount}个试卷，失败{result.failedIds.Count}个");
    }



    /// <summary>
    /// 生成随机试卷
    /// </summary>
    /// <param name="createDto">随机试卷生成DTO</param>
    /// <returns>生成的试卷</returns>
    [HttpPost("generate-random")]
    [HeaderOperation("生成随机试卷", "form")]
    [DisplayName("生成随机试卷")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> GenerateRandomExamPaper(GenerateRandomExamPaperDto createDto)
    {
        var result = await _examPaperService.GenerateRandomExamPaperAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="id">源试卷ID</param>
    /// <returns>复制的新试卷</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "ajax", null, "确定要复制此试卷吗？")]
    [DisplayName("复制")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> CopyExamPaper(long id)
    {
        var result = await _examPaperService.CopyExamPaperAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取试卷下拉列表
    /// </summary>
    /// <returns>试卷下拉列表</returns>
    [HttpGet("select-published")]
    [DisplayName("获取已发布试卷列表")]
    public async Task<ActionResult<ApiResponse<List<OptionDto<long>>>>> GetSelectList()
    {
        var papers = await _examPaperService.GetAllExamPapersByStatusAsync(ExamPaperStatus.Published);
        var result = papers.Select(p => new OptionDto<long>
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();
        return SuccessResponse(result);
    }

    /// <summary>
    /// 预览试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>预览配置</returns>
    [HttpGet("{id}/preview")]
    [Operation(label: "试卷预览", actionType: "service", DialogSize = DialogSize.XL)]
    [DisplayName("试卷预览")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewExamPaper(long id)
    {
        var examPaper = await _examPaperService.GetAsync(id);
        if (examPaper == null)
        {
            return NotFound("试卷不存在");
        }

        // 自动标记为已预览
        await _examPaperService.MarkPreviewedAsync(id);

        var panelConfig = new JObject
        {
            ["type"] = "service",
            ["schemaApi"] = $"get:/exam/api/exam/examPapers/{id}/questions-preview",
            ["body"] = new JObject
            {
                ["title"] = $"预览试卷 - {examPaper.Name}",
                ["type"] = "panel",
                ["body"] = "${content}"
            }
        };

        return SuccessResponse(panelConfig);
    }

    /// <summary>
    /// 标记试卷已完成预览
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/mark-previewed")]
    [DisplayName("标记为已预览")]
    public async Task<ActionResult<ApiResponse>> MarkExamPaperPreviewed(long id)
    {
        await _examPaperService.MarkPreviewedAsync(id);
        return SuccessResponse("试卷已标记为已预览");
    }

    [Operation("考试管理", "link", "/exam/examSettings?examPaperId=$id", null, visibleOn: "status === 2")]
    [DisplayName("考试管理")]
    public ActionResult<ApiResponse> ExamSettings_Manager()
    {
        return SuccessResponse();
    }

    /// <summary>
    /// 创建考试设置
    /// </summary>
    /// <param name="createDto">创建考试设置DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost("create-examSetting")]
    [Operation("创建考试", "form", visibleOn: "status === 2", Redirect = "/exam/examSettings?examPaperId=$examPaperId", Data = "{\"examPaperId\":\"${id}\"}")]
    [DisplayName("创建考试设置")]
    public async Task<ActionResult<ApiResponse<ExamSettingDto>>> CreateExamSetting([FromBody] CreateExamSettingDto createDto)
    {
        var result = await _examSettingService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取试卷题目预览的Amis配置
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>试卷题目的Amis配置</returns>
    [HttpGet("{id}/questions-preview")]
    [DisplayName("获取试卷题目预览配置")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetExamQuestionsPreviewConfig(long id)
    {
        var examPaper = await _examPaperService.GetAsync(id);
        if (examPaper == null)
        {
            return NotFound("试卷不存在");
        }

        // 使用JObject/JArray构建表单
        var formItems = new JArray();

        // 添加试卷信息头部，包含成绩换算提示
        var headerInfo = new JObject
        {
            ["type"] = "panel",
            ["className"] = "exam-paper-header",
            ["body"] = new JArray()
        };

        var headerBody = (JArray)headerInfo["body"]!;

        // 基本信息
        headerBody!.Add(new JObject
        {
            ["type"] = "html",
            ["html"] = $@"
                <div class=""exam-header-info"">
                    <h3>{examPaper.Name}</h3>
                    <div class=""exam-basic-info"">
                        <span>总分：{examPaper.TotalScore}分</span> | 
                        <span>及格分：{examPaper.PassScore}分</span> | 
                        <span>时长：{examPaper.Duration}分钟</span>
                    </div>
                </div>
            "
        });

        // 成绩换算提醒
        if (examPaper.EnableScoreConversion)
        {
            var conversionDescription = string.Empty;
            if (examPaper.OriginalTotalScore.HasValue && examPaper.ConversionRatio.HasValue)
            {
                conversionDescription = $"将{examPaper.OriginalTotalScore.Value}分制转换为{examPaper.TotalScore}分制，" +
                                      $"换算比例为{examPaper.ConversionRatio.Value:F4}，" +
                                      $"小数保留{examPaper.ConversionDecimalPlaces}位";
            }

            headerBody!.Add(new JObject
            {
                ["type"] = "alert",
                ["level"] = "info",
                ["className"] = "score-conversion-alert",
                ["body"] = $@"
                    <div class=""conversion-reminder"">
                        <strong>⚠️ 成绩换算提醒</strong><br/>
                        本试卷启用了成绩换算功能：{conversionDescription}<br/>
                        考生最终成绩将按照换算规则自动转换为<strong>{examPaper.TotalScore}分制</strong>显示。
                    </div>
                "
            });
        }

        formItems.Add(headerInfo);

        // 按题型分组题目
        var questionsByType = examPaper.Questions
            .GroupBy(q => q.Type.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        int globalIndex = 1; // 全局题目序号

        // 为每个题型创建分组
        foreach (var typeGroup in questionsByType)
        {
            string questionType = typeGroup.Key;
            var questions = typeGroup.Value;
            string typeName = GetQuestionTypeName(questionType);

            // 创建题型分组头部
            var typeHeader = new JObject
            {
                ["type"] = "html",
                ["html"] = $@"
                    <div class=""question-type-header"">
                        <h4 style=""color: #1890ff; border-bottom: 2px solid #1890ff; padding-bottom: 8px; margin: 20px 0 15px 0;"">
                            {typeName}（共{questions.Count}题）
                        </h4>
                    </div>
                "
            };
            formItems.Add(typeHeader);

            // 处理该题型下的所有题目
            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                question.Content = WebUtility.HtmlDecode(question.Content);
                question.Options = question.Options.Select(p => WebUtility.HtmlDecode(p)).ToList();
                question.CorrectAnswer = WebUtility.HtmlDecode(question.CorrectAnswer);
                question.Analysis = WebUtility.HtmlDecode(question.Analysis ?? string.Empty);

                // 题目分数显示（原始分数）
                var scoreText = examPaper.EnableScoreConversion
                    ? $"{question.Score}分（原始分值）"
                    : $"{question.Score}分";

                // 问题标题
                var titleObj = new JObject
                {
                    ["type"] = "tpl",
                    ["tpl"] = $"<div class=\"question-label\"><pre>{globalIndex}. {question.Content} </pre><span style=\"color:#999\">（{scoreText}）</span></div>",
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
                            ["name"] = $"question_{question.Id}",
                            ["options"] = singleOptions,
                            ["mode"] = "horizontal",
                            ["required"] = question.IsRequired
                        };

                        var singleChoiceEvent = new JObject
                        {
                            ["change"] = new JObject
                            {
                                ["actions"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["actionType"] = "custom",
                                        ["script"] = $"saveAnswer('{question.Id}', event.data.value);"
                                    }
                                }
                            }
                        };
                        singleChoiceObj["onEvent"] = singleChoiceEvent;
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
                            ["name"] = $"question_{question.Id}",
                            ["options"] = multiOptions,
                            ["mode"] = "horizontal",
                            ["required"] = question.IsRequired
                        };

                        var multiChoiceEvent = new JObject
                        {
                            ["change"] = new JObject
                            {
                                ["actions"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["actionType"] = "custom",
                                        ["script"] = $"saveAnswer('{question.Id}', event.data.value);"
                                    }
                                }
                            }
                        };
                        multiChoiceObj["onEvent"] = multiChoiceEvent;
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
                            ["name"] = $"question_{question.Id}",
                            ["options"] = tfOptions,
                            ["mode"] = "horizontal",
                            ["required"] = question.IsRequired
                        };

                        var tfEvent = new JObject
                        {
                            ["change"] = new JObject
                            {
                                ["actions"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["actionType"] = "custom",
                                        ["script"] = $"saveAnswer('{question.Id}', event.data.value);"
                                    }
                                }
                            }
                        };
                        tfObj["onEvent"] = tfEvent;
                        formItems.Add(tfObj);
                        break;

                    default:
                        // 简答题和其他题型
                        var textareaObj = new JObject
                        {
                            ["type"] = "textarea",
                            ["name"] = $"question_{question.Id}",
                            ["placeholder"] = "请输入答案",
                            ["minRows"] = 3,
                            ["maxRows"] = 6,
                            ["required"] = question.IsRequired
                        };

                        var textareaEvent = new JObject
                        {
                            ["change"] = new JObject
                            {
                                ["actions"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["actionType"] = "custom",
                                        ["script"] = $"saveAnswer('{question.Id}', event.data.value);"
                                    }
                                }
                            }
                        };
                        textareaObj["onEvent"] = textareaEvent;
                        formItems.Add(textareaObj);
                        break;
                }

                // 如果不是该题型的最后一个题目，添加分隔线
                if (i < questions.Count - 1)
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

        // 底部成绩换算说明
        if (examPaper.EnableScoreConversion && examPaper.OriginalTotalScore.HasValue && examPaper.ConversionRatio.HasValue)
        {
            var exampleScore = 90;
            var convertedExampleScore = exampleScore * (double)examPaper.ConversionRatio.Value;

            formItems.Add(new JObject
            {
                ["type"] = "panel",
                ["className"] = "conversion-summary",
                ["body"] = new JObject
                {
                    ["type"] = "html",
                    ["html"] = $@"
                        <div class=""conversion-summary-content"">
                            <h4>📊 成绩换算规则</h4>
                            <div class=""conversion-details"">
                                <div class=""conversion-item"">
                                    <span class=""label"">原始分制：</span>
                                    <span class=""value"">{examPaper.OriginalTotalScore.Value}分（满分）/ {examPaper.OriginalPassScore ?? 0}分（及格）</span>
                                </div>
                                <div class=""conversion-item"">
                                    <span class=""label"">目标分制：</span>
                                    <span class=""value"">{examPaper.TotalScore}分（满分）/ {examPaper.PassScore}分（及格）</span>
                                </div>
                                <div class=""conversion-item"">
                                    <span class=""label"">换算比例：</span>
                                    <span class=""value"">{examPaper.ConversionRatio.Value:F4}</span>
                                </div>
                                <div class=""conversion-item"">
                                    <span class=""label"">小数位数：</span>
                                    <span class=""value"">保留{examPaper.ConversionDecimalPlaces}位小数</span>
                                </div>
                            </div>
                            <div class=""conversion-example"">
                                <strong>换算示例：</strong>假设考生得分{exampleScore}分，换算后成绩为 {exampleScore} × {examPaper.ConversionRatio.Value:F4} = {convertedExampleScore:F1}分
                            </div>
                        </div>
                    "
                }
            });
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "试卷预览",
            ["id"] = "examForm",
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
                        ["label"] = "发布试卷",
                        ["actionType"] = "ajax",
                        ["api"] = $"PUT:/exam/api/exam/examPapers/{id}/publish",
                        ["level"] = "primary",
                        ["confirmText"] = "确定要发布此试卷吗？发布后将可用于创建考试。",
                        ["visibleOn"] = "status == 1",
                        ["reload"] = "window"
                    }
            }
        };

        return SuccessResponse(amisConfig);
    }

    /// <summary>
    /// 创建练习设置
    /// </summary>
    /// <param name="createDto">创建练习设置DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost("create-practiceSetting")]
    [Operation("创建练习", "form", visibleOn: "status === 2", Redirect = "/exam/practiceSettings?examPaperId=$id", Data = "{\"examPaperId\":\"${id}\"}")]
    [DisplayName("创建练习设置")]
    public async Task<ActionResult<ApiResponse<PracticeSettingDto>>> CreatePracticeSetting([FromBody] CreatePracticeSettingDto createDto)
    {
        var result = await _practiceSettingService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 练习管理
    /// </summary>
    /// <returns>跳转链接</returns>
    [Operation("练习管理", "link", "/exam/practiceSettings?examPaperId=$id", null, visibleOn: "status === 2")]
    [DisplayName("练习管理")]
    public ActionResult<ApiResponse> PracticeSettings_Manager()
    {
        return SuccessResponse();
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
            "TrueFalse" => "判断题",
            "ShortAnswer" => "简答题",
            "Essay" => "论述题",
            _ => questionType
        };
    }
}