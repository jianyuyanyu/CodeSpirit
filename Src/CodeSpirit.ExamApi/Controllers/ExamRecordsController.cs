using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试记录控制器
/// </summary>
[DisplayName("考试记录管理")]
[Navigation(Icon = "fa-solid fa-clipboard-check")]
public class ExamRecordsController : ApiControllerBase
{
    private readonly IExamRecordService _examRecordService;
    private readonly IExamPaperService _examPaperService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examRecordService">考试记录服务</param>
    public ExamRecordsController(IExamRecordService examRecordService, IExamPaperService examPaperService)
    {
        _examRecordService = examRecordService;
        _examPaperService = examPaperService;
    }

    /// <summary>
    /// 获取考试记录列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>考试记录列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<ExamRecordDto>>>> GetExamRecords([FromQuery] ExamRecordQueryDto queryDto)
    {
        var records = await _examRecordService.GetPagedListAsync(queryDto, includes: ["ExamSetting", "Student"]);
        return SuccessResponse(records);
    }

    /// <summary>
    /// 获取考试记录详情
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试记录详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExamRecordDto>>> GetExamRecordDetail(long id)
    {
        var record = await _examRecordService.GetExamRecordDetailAsync(id);
        return SuccessResponse(record);
    }

    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <returns>考试统计信息</returns>
    [Operation("考试统计", "link", "/exam/examStatistics?examSettingId=${id}", null)]
    public ActionResult<ApiResponse> GetExamStatistics()
    {
        return SuccessResponse();
    }

    /// <summary>
    /// 获取错题列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>错题列表</returns>
    [Operation("错题管理", "link", "/exam/wrongQuestions?studentId=${studentId}", null)]
    public ActionResult<ApiResponse> GetWrongQuestions()
    {
        return SuccessResponse();
    }
    /// <summary>
    /// 预览答卷
    /// </summary>
    /// <param name="id">答卷ID</param>
    /// <returns>预览配置</returns>
    [HttpGet("{id}/preview")]
    [Operation(label: "答卷预览", actionType: "service")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewExamPaper(long id)
    { 
        var panelConfig = new JObject
        {
            ["type"] = "service",
            ["schemaApi"] = $"get:/exam/api/exam/examRecords/{id}/questions-preview",
            ["body"] = new JObject
            {
                ["title"] = $"答卷预览",
                ["type"] = "panel",
                ["body"] = "${content}"
            }
        };

        return SuccessResponse(panelConfig);
    }

    /// <summary>
    /// 获取试卷题目预览的Amis配置
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>试卷题目的Amis配置</returns>
    [HttpGet("{id}/questions-preview")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetExamQuestionsPreviewConfig(long id)
    {
        var preview = await _examRecordService.GetAnswerPreviewAsync(id); 

        var examPaper = await _examPaperService.GetAsync(preview.ExamPaperId);
        if (examPaper == null)
        {
            return NotFound("试卷不存在");
        }

        // 使用JObject/JArray构建表单
        var formItems = new JArray();

        // 为每个题目创建对应的表单组件
        for (int i = 0; i < examPaper.Questions.Count; i++)
        {
            var question = examPaper.Questions[i];
            int index = i + 1;
            var answer = preview.Answers.FirstOrDefault(x => x.QuestionId == question.QuestionId)?.Answer;

            // 问题标题
            var titleObj = new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = $"<div class=\"question-label\">{index}. {question.Content} <span style=\"color:#999\">（{question.Score}分）</span></div>",
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
                        ["required"] = question.IsRequired,
                        ["value"] = answer
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
                        ["required"] = question.IsRequired,
                        ["value"] = answer
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
                        ["required"] = question.IsRequired,
                        ["value"] = answer
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
                        ["required"] = question.IsRequired,
                        ["value"] = answer
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

            // 如果不是最后一个题目，添加分隔线
            if (i < examPaper.Questions.Count - 1)
            {
                formItems.Add(new JObject { ["type"] = "divider" });
            }
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "",
            ["id"] = "examForm",
            ["body"] = formItems,
            ["actions"] = new JArray()  // 添加空的actions数组，隐藏表单自带的提交按钮
        };

        return SuccessResponse(amisConfig);
    }

}