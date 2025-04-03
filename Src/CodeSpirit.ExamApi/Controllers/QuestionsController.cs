using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 题目管理控制器
/// </summary>
[DisplayName("题目管理")]
[Navigation(Icon = "fa-solid fa-book")]
public class QuestionsController : ApiControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    /// <summary>
    /// 初始化题目管理控制器
    /// </summary>
    /// <param name="questionService">题目服务</param>
    /// <param name="logger">日志记录器</param>
    public QuestionsController(
        IQuestionService questionService,
        ILogger<QuestionsController> logger)
    {
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(logger);

        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目列表分页结果</returns>
    [HttpGet]
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
    public async Task<ActionResult<ApiResponse>> UpdateQuestion(long id, UpdateQuestionDto updateQuestionDto)
    {
        ArgumentNullException.ThrowIfNull(updateQuestionDto);
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
    public async Task<ActionResult<ApiResponse<List<QuestionVersionDto>>>> GetQuestionVersions(long id)
    {
        var versions = await _questionService.GetQuestionVersionsAsync(id);
        return SuccessResponse(versions);
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    /// <param name="request">批量删除请求数据</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的题目吗？", isBulkOperation: true)]
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
    public async Task<ActionResult<ApiResponse>> BatchParserFromText([FromBody]QuestionImportFromTextDto input)
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
    public async Task<ActionResult<ApiResponse<JObject>>> GetQuestionPreviewConfig(long id)
    {
        var question = await _questionService.GetQuestionAsync(id);
        if (question == null)
        {
            return NotFound("题目不存在");
        }

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
} 