using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using Microsoft.AspNetCore.Mvc;
using CodeSpirit.ExamApi.Controllers.Client;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 试卷管理
/// </summary>
[DisplayName("试卷管理")]
[Navigation(Icon = "fa-solid fa-file-lines")]
public class ExamPapersController : ApiControllerBase
{
    private readonly IExamPaperService _examPaperService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamPapersController(IExamPaperService examPaperService)
    {
        _examPaperService = examPaperService;
    }

    /// <summary>
    /// 获取试卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>试卷分页列表</returns>
    [HttpGet]
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
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> GetExamPaper(long id)
    {
        var result = await _examPaperService.GetAsync(id);
        if (result == null)
        {
            return NotFound("试卷不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建试卷
    /// </summary>
    /// <param name="createDto">创建试卷DTO</param>
    /// <returns>创建的试卷</returns>
    [HttpPost]
    [HeaderOperation("生成固定试卷", "form")]
    public async Task<ActionResult<ApiResponse<ExamPaperDto>>> CreateExamPaper(CreateExamPaperDto createDto)
    {
        var result = await _examPaperService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <param name="updateDto">更新试卷DTO</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
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
    [Operation("发布", "ajax", null, "确定要发布此试卷吗？", visibleOn: "status == 1")]
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
    [Operation(label: "试卷预览", actionType: "service")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewExamPaper(long id)
    {
        var examPaper = await _examPaperService.GetAsync(id);
        if (examPaper == null)
        {
            return NotFound("试卷不存在");
        }

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
    /// 获取试卷题目预览的Amis配置
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>试卷题目的Amis配置</returns>
    [HttpGet("{id}/questions-preview")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetExamQuestionsPreviewConfig(long id)
    {
        var examPaper = await _examPaperService.GetAsync(id);
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