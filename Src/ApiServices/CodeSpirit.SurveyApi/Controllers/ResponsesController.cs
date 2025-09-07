using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.SurveyApi.Dtos.Response;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Controllers;

/// <summary>
/// 问卷回答管理控制器
/// </summary>
[DisplayName("答卷管理")]
[Navigation(Icon = "fa-solid fa-clipboard-list")]
public class ResponsesController : ApiControllerBase
{
    private readonly IResponseService _responseService;
    private readonly IQuestionService _questionService;
    private readonly IRepository<Question> _questionRepository;
    private readonly ILogger<ResponsesController> _logger;

    /// <summary>
    /// 初始化问卷回答管理控制器
    /// </summary>
    /// <param name="responseService">回答服务</param>
    /// <param name="questionService">题目服务</param>
    /// <param name="questionRepository">题目仓储</param>
    /// <param name="logger">日志记录器</param>
    public ResponsesController(
        IResponseService responseService,
        IQuestionService questionService,
        IRepository<Question> questionRepository,
        ILogger<ResponsesController> logger)
    {
        ArgumentNullException.ThrowIfNull(responseService);
        ArgumentNullException.ThrowIfNull(questionService);
        ArgumentNullException.ThrowIfNull(questionRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _responseService = responseService;
        _questionService = questionService;
        _questionRepository = questionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取回答分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页结果</returns>
    [HttpGet]
    [DisplayName("获取回答列表")]
    public async Task<ActionResult<ApiResponse<PageList<ResponseDto>>>> GetPagedResponses([FromQuery] ResponseQueryDto queryDto)
    {
        var result = await _responseService.GetPagedResponsesAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 根据ID获取回答详情
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>回答详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取回答详情")]
    public async Task<ActionResult<ApiResponse<ResponseDetailDto>>> GetResponseDetail(int id)
    {
        var result = await _responseService.GetResponseDetailAsync(id);
        if (result == null)
        {
            return BadResponse<ResponseDetailDto>("回答不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 根据问卷ID获取回答列表
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="includeAnswers">是否包含答案详情</param>
    /// <returns>回答列表</returns>
    [HttpGet("survey/{surveyId}")]
    [DisplayName("获取问卷回答列表")]
    public async Task<ActionResult<ApiResponse<List<ResponseDto>>>> GetResponsesBySurveyId(int surveyId, [FromQuery] bool includeAnswers = false)
    {
        var result = await _responseService.GetResponsesBySurveyIdAsync(surveyId, includeAnswers);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取问卷回答统计信息
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>统计信息</returns>
    [HttpGet("statistics/{surveyId}")]
    [DisplayName("获取回答统计")]
    public async Task<ActionResult<ApiResponse<ResponseStatisticsDto>>> GetResponseStatistics(int surveyId)
    {
        var result = await _responseService.GetResponseStatisticsAsync(surveyId);
        if (result == null)
        {
            return BadResponse<ResponseStatisticsDto>("问卷不存在");
        }
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取回答的答案详情
    /// </summary>
    /// <param name="responseId">回答ID</param>
    /// <returns>答案详情列表</returns>
    [HttpGet("{responseId}/answers")]
    [DisplayName("获取回答答案")]
    public async Task<ActionResult<ApiResponse<List<ResponseAnswerDto>>>> GetResponseAnswers(int responseId)
    {
        var result = await _responseService.GetResponseAnswersAsync(responseId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取问卷回答趋势统计
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="days">统计天数</param>
    /// <returns>趋势统计数据</returns>
    [HttpGet("trend/{surveyId}")]
    [DisplayName("获取回答趋势")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, int>>>> GetResponseTrend(int surveyId, [FromQuery] int days = 30)
    {
        var result = await _responseService.GetResponseTrendAsync(surveyId, days);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 删除回答
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除回答")]
    public async Task<ActionResult<ApiResponse>> DeleteResponse(int id)
    {
        var result = await _responseService.BatchDeleteResponsesAsync(new List<int> { id });
        if (result)
        {
            return SuccessResponse("删除成功");
        }
        return BadResponse("删除失败");
    }

    /// <summary>
    /// 批量删除回答
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch-delete")]
    [DisplayName("批量删除回答")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteResponses([FromBody] BatchDeleteResponsesRequest request)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return BadResponse("请选择要删除的回答");
        }

        var result = await _responseService.BatchDeleteResponsesAsync(request.Ids);
        if (result)
        {
            return SuccessResponse($"成功删除 {request.Ids.Count} 条回答");
        }
        return BadResponse("批量删除失败");
    }

    /// <summary>
    /// 根据问卷ID删除所有回答
    /// </summary>
    /// <param name="request">删除请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("delete-by-survey")]
    [DisplayName("清空问卷回答")]
    public async Task<ActionResult<ApiResponse>> DeleteResponsesBySurvey([FromBody] DeleteResponsesBySurveyRequest request)
    {
        var result = await _responseService.DeleteResponsesBySurveyIdAsync(request.SurveyId);
        if (result)
        {
            return SuccessResponse("清空问卷回答成功");
        }
        return BadResponse("清空问卷回答失败");
    }

    /// <summary>
    /// 标记回答为已放弃
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/abandon")]
    [DisplayName("标记为已放弃")]
    public async Task<ActionResult<ApiResponse>> MarkAsAbandoned(int id)
    {
        var result = await _responseService.MarkAsAbandonedAsync(id);
        if (result)
        {
            return SuccessResponse("标记成功");
        }
        return BadResponse("标记失败");
    }

    /// <summary>
    /// 导出问卷回答数据
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="format">导出格式</param>
    /// <returns>导出文件</returns>
    [HttpGet("export/{surveyId}")]
    [DisplayName("导出回答数据")]
    public async Task<ActionResult> ExportResponses(int surveyId, [FromQuery] string format = "excel")
    {
        try
        {
            var data = await _responseService.ExportResponsesAsync(surveyId, format);
            
            var fileName = format.ToLower() == "excel" 
                ? $"问卷回答数据_{surveyId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                : $"问卷回答数据_{surveyId}_{DateTime.Now:yyyyMMddHHmmss}.csv";

            var contentType = format.ToLower() == "excel"
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "text/csv";

            return File(data, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出问卷回答数据失败，问卷ID: {SurveyId}, 格式: {Format}", surveyId, format);
            return BadRequest(BadResponse("导出失败"));
        }
    }

    /// <summary>
    /// 检查回答是否存在
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>检查结果</returns>
    [HttpGet("check-exists")]
    [DisplayName("检查回答是否存在")]
    public async Task<ActionResult<ApiResponse<object>>> CheckResponseExists([FromQuery] int surveyId, [FromQuery] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadResponse<object>("会话ID不能为空");
        }

        var result = await _responseService.CheckResponseExistsAsync(surveyId, sessionId);
        return SuccessResponse<object>(result);
    }

    /// <summary>
    /// 获取用户的回答历史
    /// </summary>
    /// <param name="respondentId">答题者ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>回答历史分页结果</returns>
    [HttpGet("user-history/{respondentId}")]
    [DisplayName("获取用户回答历史")]
    public async Task<ActionResult<ApiResponse<PageList<ResponseDto>>>> GetUserResponseHistory(
        string respondentId, 
        [FromQuery] int pageIndex = 1, 
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(respondentId))
        {
            return BadResponse<PageList<ResponseDto>>("答题者ID不能为空");
        }

        var result = await _responseService.GetUserResponseHistoryAsync(respondentId, pageIndex, pageSize);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 预览答卷
    /// </summary>
    /// <param name="id">答卷ID</param>
    /// <returns>预览配置</returns>
    [HttpGet("{id}/preview")]
    [Operation(label: "预览", actionType: "service")]
    [DisplayName("预览答卷")]
    public async Task<ActionResult<ApiResponse<JObject>>> PreviewResponse(int id)
    {
        var response = await _responseService.GetResponseDetailAsync(id);
        if (response == null)
        {
            return NotFound("答卷不存在");
        }

        var panelConfig = new JObject
        {
            ["type"] = "service",
            ["schemaApi"] = $"get:/survey/api/survey/responses/{id}/preview-config",
            ["body"] = new JObject
            {
                ["title"] = $"预览答卷 - {response.SurveyTitle}",
                ["type"] = "panel",
                ["body"] = "${content}"
            }
        };

        return SuccessResponse(panelConfig);
    }

    /// <summary>
    /// 获取答卷预览的Amis配置
    /// </summary>
    /// <param name="id">答卷ID</param>
    /// <returns>答卷预览的Amis配置</returns>
    [HttpGet("{id}/preview-config")]
    [DisplayName("获取答卷预览配置")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetResponsePreviewConfig(int id)
    {
        var response = await _responseService.GetResponseDetailAsync(id);
        if (response == null)
        {
            return NotFound("答卷不存在");
        }

        // 构建表单项
        var formItems = new JArray();

        // 添加答卷基本信息头部
        var headerInfo = new JObject
        {
            ["type"] = "panel",
            ["className"] = "response-header",
            ["body"] = new JArray()
        };

        var headerBody = (JArray)headerInfo["body"]!;

        // 基本信息
        headerBody!.Add(new JObject
        {
            ["type"] = "html",
            ["html"] = $@"
                <div class=""response-header-info"">
                    <h3>答卷详情 - {response.SurveyTitle}</h3>
                    <div class=""response-basic-info"">
                        <span>答题者：{response.RespondentId ?? "匿名用户"}</span> | 
                        <span>状态：{GetResponseStatusName(response.Status.ToString())}</span> | 
                        <span>开始时间：{response.StartedAt:yyyy-MM-dd HH:mm:ss}</span>
                        {(response.CompletedAt.HasValue ? $" | <span>完成时间：{response.CompletedAt:yyyy-MM-dd HH:mm:ss}</span>" : "")}
                        {(response.DurationMinutes.HasValue ? $" | <span>用时：{response.DurationMinutes}分钟</span>" : "")}
                    </div>
                    {(!string.IsNullOrEmpty(response.IpAddress) ? $"<div class=\"response-ip\">IP地址：{response.IpAddress}</div>" : "")}
                </div>
            "
        });

        formItems.Add(headerInfo);

        // 添加分隔线
        formItems.Add(new JObject 
        { 
            ["type"] = "html", 
            ["html"] = "<div style=\"margin: 20px 0; border-bottom: 2px solid #f0f0f0;\"></div>" 
        });

        // 添加答案详情标题
        formItems.Add(new JObject
        {
            ["type"] = "html",
            ["html"] = "<h4 style=\"color: #1890ff; margin: 20px 0 15px 0;\">答题详情</h4>"
        });

        // 处理每个答案
        if (response.Answers != null && response.Answers.Any())
        {
        // 获取所有题目的详细信息（包含选项）
        var questionIds = response.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var questions = new Dictionary<int, dynamic>();

        // 直接从仓储获取包含选项的题目数据
        var questionsWithOptions = await _questionRepository.Find(q => questionIds.Contains(q.Id))
            .Include(q => q.Options)
            .ToListAsync();

        foreach (var question in questionsWithOptions)
        {
            questions[question.Id] = question;
        }

            var groupedAnswers = response.Answers
                .OrderBy(a => a.QuestionId)
                .GroupBy(a => a.QuestionType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.ToList());

            int questionIndex = 1;

            foreach (var typeGroup in groupedAnswers)
            {
                string questionType = typeGroup.Key ?? "Unknown";
                var typeAnswers = typeGroup.Value;
                string typeName = GetQuestionTypeName(questionType);

                // 创建题型分组头部
                var typeHeader = new JObject
                {
                    ["type"] = "html",
                    ["html"] = $@"
                        <div class=""question-type-header"">
                            <h5 style=""color: #52c41a; border-bottom: 1px solid #52c41a; padding-bottom: 5px; margin: 15px 0 10px 0;"">
                                {typeName}（{typeAnswers.Count}题）
                            </h5>
                        </div>
                    "
                };
                formItems.Add(typeHeader);

                // 处理该题型下的所有答案
                foreach (var answer in typeAnswers)
                {
                    // 获取题目信息用于格式化答案
                    questions.TryGetValue(answer.QuestionId, out var questionDetail);
                    
                    // 添加题目标题
                    var questionTitle = new JObject
                    {
                        ["type"] = "html",
                        ["html"] = $@"
                            <div style=""font-weight: bold; margin-bottom: 10px; margin-top: 15px; color: #333; font-size: 14px;"">
                                {questionIndex}. {answer.QuestionTitle}
                            </div>"
                    };
                    formItems.Add(questionTitle);

                    // 根据题型添加相应的组件
                    var answerComponent = CreateAnswerComponent(answer.AnswerValue, questionType, questionDetail, answer.AnswerText);
                    formItems.Add(answerComponent);

                    questionIndex++;
                }
            }
        }
        else
        {
            // 没有答案的情况
            formItems.Add(new JObject
            {
                ["type"] = "html",
                ["html"] = "<div style=\"text-align: center; color: #999; padding: 40px;\">该答卷暂无答题内容</div>"
            });
        }

        // 构建Amis配置对象
        var amisConfig = new JObject
        {
            ["type"] = "form",
            ["title"] = "答卷预览",
            ["id"] = "responsePreviewForm",
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
                    ["label"] = "删除答卷",
                    ["actionType"] = "ajax",
                    ["api"] = $"DELETE:/survey/api/survey/responses/{id}",
                    ["level"] = "danger",
                    ["confirmText"] = "确定要删除此答卷吗？删除后无法恢复。",
                    ["reload"] = "window"
                }
            }
        };

        return SuccessResponse(amisConfig);
    }

    /// <summary>
    /// 获取答卷状态中文名称
    /// </summary>
    /// <param name="status">答卷状态</param>
    /// <returns>中文名称</returns>
    private static string GetResponseStatusName(string status)
    {
        return status switch
        {
            "InProgress" => "进行中",
            "Completed" => "已完成",
            "Abandoned" => "已放弃",
            _ => status
        };
    }

    /// <summary>
    /// 获取题目类型中文名称
    /// </summary>
    /// <param name="questionType">题目类型</param>
    /// <returns>中文名称</returns>
    private static string GetQuestionTypeName(string questionType)
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
            "Textarea" => "多行文本题",
            "Matrix" => "矩阵题",
            "Ranking" => "排序题",
            _ => questionType
        };
    }

    /// <summary>
    /// 格式化答案值显示（带选项信息）
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="questionDetail">题目详情（包含选项）</param>
    /// <returns>格式化后的答案</returns>
    private string FormatAnswerValueWithOptions(string? answerValue, string questionType, dynamic? questionDetail)
    {
        return questionType switch
        {
            "SingleChoice" => RenderSingleChoiceWithOptions(answerValue, questionDetail),
            "MultipleChoice" => RenderMultipleChoiceWithOptions(answerValue, questionDetail),
            "Rating" => RenderRatingWithOptions(answerValue, questionDetail),
            _ => string.IsNullOrEmpty(answerValue) ? "<span style=\"color: #ccc;\">未作答</span>" : answerValue
        };
    }

    /// <summary>
    /// 渲染单选题选项（显示所有选项并标识选择）
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>渲染后的选项HTML</returns>
    private string RenderSingleChoiceWithOptions(string? answerValue, dynamic? questionDetail)
    {
        if (questionDetail?.Options == null)
        {
            return string.IsNullOrEmpty(answerValue) ? "<span style=\"color: #ccc;\">未作答</span>" : answerValue;
        }

        try
        {
            var optionsHtml = new List<string>();
            var hasAnswer = !string.IsNullOrEmpty(answerValue);

            foreach (var option in questionDetail.Options)
            {
                var optionValue = option.Value?.ToString() ?? "";
                var optionText = option.Text?.ToString() ?? optionValue;
                var isSelected = hasAnswer && (optionValue == answerValue || optionText == answerValue);

                var optionHtml = $@"
                    <div style=""margin: 5px 0; padding: 8px 12px; border-radius: 4px; {(isSelected ? "background-color: #e6f7ff; border: 1px solid #1890ff;" : "background-color: #f9f9f9; border: 1px solid #d9d9d9;")}"">
                        <span style=""display: inline-block; width: 20px; height: 20px; border-radius: 50%; border: 2px solid {(isSelected ? "#1890ff" : "#d9d9d9")}; margin-right: 8px; position: relative; vertical-align: middle;"">
                            {(isSelected ? "<span style=\"display: block; width: 8px; height: 8px; background-color: #1890ff; border-radius: 50%; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);\"></span>" : "")}
                        </span>
                        <span style=""vertical-align: middle; {(isSelected ? "color: #1890ff; font-weight: bold;" : "color: #666;")}"">
                            {optionText}
                        </span>
                    </div>";
                optionsHtml.Add(optionHtml);
            }

            if (!hasAnswer)
            {
                optionsHtml.Insert(0, "<div style=\"color: #ff4d4f; margin-bottom: 10px; font-style: italic;\">未作答</div>");
            }

            return string.Join("", optionsHtml);
        }
        catch
        {
            return string.IsNullOrEmpty(answerValue) ? "<span style=\"color: #ccc;\">未作答</span>" : answerValue;
        }
    }

    /// <summary>
    /// 渲染多选题选项（显示所有选项并标识选择）
    /// </summary>
    /// <param name="answerValue">答案值（逗号分隔）</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>渲染后的选项HTML</returns>
    private string RenderMultipleChoiceWithOptions(string? answerValue, dynamic? questionDetail)
    {
        if (questionDetail?.Options == null)
        {
            return string.IsNullOrEmpty(answerValue) ? "<span style=\"color: #ccc;\">未作答</span>" : answerValue.Replace(",", "、");
        }

        try
        {
            var selectedValues = new HashSet<string>();
            var hasAnswer = !string.IsNullOrEmpty(answerValue);

            if (hasAnswer)
            {
                var values = answerValue!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        selectedValues.Add(value.Trim());
                    }
                }
            }

            var optionsHtml = new List<string>();

            foreach (var option in questionDetail.Options)
            {
                var optionValue = option.Value?.ToString() ?? "";
                var optionText = option.Text?.ToString() ?? optionValue;
                var isSelected = selectedValues.Contains(optionValue) || selectedValues.Contains(optionText);

                var optionHtml = $@"
                    <div style=""margin: 5px 0; padding: 8px 12px; border-radius: 4px; {(isSelected ? "background-color: #e6f7ff; border: 1px solid #1890ff;" : "background-color: #f9f9f9; border: 1px solid #d9d9d9;")}"">
                        <span style=""display: inline-block; width: 18px; height: 18px; border: 2px solid {(isSelected ? "#1890ff" : "#d9d9d9")}; margin-right: 8px; position: relative; vertical-align: middle; border-radius: 2px;"">
                            {(isSelected ? "<span style=\"display: block; width: 8px; height: 5px; border-left: 2px solid #1890ff; border-bottom: 2px solid #1890ff; transform: rotate(-45deg); position: absolute; top: 3px; left: 3px;\"></span>" : "")}
                        </span>
                        <span style=""vertical-align: middle; {(isSelected ? "color: #1890ff; font-weight: bold;" : "color: #666;")}"">
                            {optionText}
                        </span>
                    </div>";
                optionsHtml.Add(optionHtml);
            }

            if (!hasAnswer)
            {
                optionsHtml.Insert(0, "<div style=\"color: #ff4d4f; margin-bottom: 10px; font-style: italic;\">未作答</div>");
            }

            return string.Join("", optionsHtml);
        }
        catch
        {
            return string.IsNullOrEmpty(answerValue) ? "<span style=\"color: #ccc;\">未作答</span>" : answerValue.Replace(",", "、");
        }
    }

    /// <summary>
    /// 渲染评分题选项（显示星级评分）
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>渲染后的评分HTML</returns>
    private string RenderRatingWithOptions(string? answerValue, dynamic? questionDetail)
    {
        if (string.IsNullOrEmpty(answerValue))
        {
            return "<span style=\"color: #ccc;\">未作答</span>";
        }

        try
        {
            if (int.TryParse(answerValue, out int rating))
            {
                var maxRating = 5; // 默认5星
                
                // 尝试从题目详情中获取最大评分
                try
                {
                    if (questionDetail?.MaxRating != null)
                    {
                        maxRating = (int)questionDetail.MaxRating;
                    }
                    else if (questionDetail?.Options != null)
                    {
                        // 如果有选项，取选项数量作为最大评分
                        var optionsCount = 0;
                        foreach (var _ in questionDetail.Options)
                        {
                            optionsCount++;
                        }
                        if (optionsCount > 0)
                        {
                            maxRating = optionsCount;
                        }
                    }
                }
                catch { }

                var starsHtml = new List<string>();
                for (int i = 1; i <= maxRating; i++)
                {
                    var isFilled = i <= rating;
                    var starHtml = $@"<span style=""color: {(isFilled ? "#fadb14" : "#d9d9d9")}; font-size: 20px; margin-right: 2px;"">★</span>";
                    starsHtml.Add(starHtml);
                }

                return $@"
                    <div style=""margin: 10px 0;"">
                        <div style=""margin-bottom: 5px;"">
                            {string.Join("", starsHtml)}
                        </div>
                        <div style=""color: #666; font-size: 14px;"">
                            评分：{rating} / {maxRating} 星
                        </div>
                    </div>";
            }
        }
        catch { }

        return $"{answerValue} 星";
    }

    /// <summary>
    /// 格式化答案值显示（兼容旧方法）
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionType">题目类型</param>
    /// <returns>格式化后的答案</returns>
    private static string FormatAnswerValue(string? answerValue, string questionType)
    {
        if (string.IsNullOrEmpty(answerValue))
        {
            return "<span style=\"color: #ccc;\">未作答</span>";
        }

        return questionType switch
        {
            "MultipleChoice" => answerValue.Replace(",", "、"),
            "Rating" => $"{answerValue} 星",
            _ => answerValue
        };
    }

    /// <summary>
    /// 创建答案显示组件
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="questionDetail">题目详情（包含选项）</param>
    /// <param name="answerText">答案文本</param>
    /// <returns>Amis组件配置</returns>
    private JObject CreateAnswerComponent(string? answerValue, string questionType, dynamic? questionDetail, string? answerText)
    {
        if (string.IsNullOrEmpty(answerValue))
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = "<div style=\"color: #ff4d4f; font-style: italic; padding: 10px;\">未作答</div>"
            };
        }

        return questionType switch
        {
            "SingleChoice" => CreateSingleChoiceComponent(answerValue, questionDetail),
            "MultipleChoice" => CreateMultipleChoiceComponent(answerValue, questionDetail),
            "Rating" => CreateRatingComponent(answerValue, questionDetail),
            _ => CreateTextAnswerComponent(answerValue, answerText)
        };
    }

    /// <summary>
    /// 创建单选题组件
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>Amis单选组件</returns>
    private JObject CreateSingleChoiceComponent(string answerValue, dynamic? questionDetail)
    {
        if (questionDetail?.Options == null)
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = $"<div style=\"padding: 10px;\"><strong>答案：</strong>{answerValue}</div>"
            };
        }

        try
        {
            var options = new JArray();
            foreach (var option in questionDetail.Options)
            {
                string optionValue = option.Value?.ToString() ?? "";
                string optionText = option.Text?.ToString() ?? "";
                
                options.Add(new JObject
                {
                    ["label"] = optionText,
                    ["value"] = optionValue
                });
            }

            return new JObject
            {
                ["type"] = "radios",
                ["name"] = $"answer_display_{Guid.NewGuid():N}",
                ["value"] = answerValue,
                ["options"] = options,
                ["disabled"] = true,
                ["className"] = "response-preview-radios",
                ["style"] = new JObject
                {
                    ["marginBottom"] = "10px"
                }
            };
        }
        catch
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = $"<div style=\"padding: 10px;\"><strong>答案：</strong>{answerValue}</div>"
            };
        }
    }

    /// <summary>
    /// 创建多选题组件
    /// </summary>
    /// <param name="answerValue">答案值（逗号分隔）</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>Amis多选组件</returns>
    private JObject CreateMultipleChoiceComponent(string answerValue, dynamic? questionDetail)
    {
        if (questionDetail?.Options == null)
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = $"<div style=\"padding: 10px;\"><strong>答案：</strong>{answerValue.Replace(",", "、")}</div>"
            };
        }

        try
        {
            var options = new JArray();
            foreach (var option in questionDetail.Options)
            {
                string optionValue = option.Value?.ToString() ?? "";
                string optionText = option.Text?.ToString() ?? "";
                
                options.Add(new JObject
                {
                    ["label"] = optionText,
                    ["value"] = optionValue
                });
            }

            // 将逗号分隔的答案值转换为数组
            var selectedValues = answerValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToArray();

            return new JObject
            {
                ["type"] = "checkboxes",
                ["name"] = $"answer_display_{Guid.NewGuid():N}",
                ["value"] = new JArray(selectedValues),
                ["options"] = options,
                ["disabled"] = true,
                ["className"] = "response-preview-checkboxes",
                ["style"] = new JObject
                {
                    ["marginBottom"] = "10px"
                }
            };
        }
        catch
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = $"<div style=\"padding: 10px;\"><strong>答案：</strong>{answerValue.Replace(",", "、")}</div>"
            };
        }
    }

    /// <summary>
    /// 创建评分题组件
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="questionDetail">题目详情</param>
    /// <returns>Amis评分组件</returns>
    private JObject CreateRatingComponent(string answerValue, dynamic? questionDetail)
    {
        if (!int.TryParse(answerValue, out int rating))
        {
            return new JObject
            {
                ["type"] = "html",
                ["html"] = $"<div style=\"padding: 10px;\"><strong>评分：</strong>{answerValue}</div>"
            };
        }

        // 获取最大评分，默认为5
        int maxRating = 5;
        try
        {
            if (questionDetail?.MaxRating != null)
            {
                maxRating = (int)questionDetail.MaxRating;
            }
        }
        catch { }

        return new JObject
        {
            ["type"] = "input-rating",
            ["name"] = $"rating_display_{Guid.NewGuid():N}",
            ["value"] = rating,
            ["count"] = maxRating,
            ["readOnly"] = true,
            ["className"] = "response-preview-rating",
            ["style"] = new JObject
            {
                ["marginBottom"] = "10px"
            }
        };
    }

    /// <summary>
    /// 创建文本答案组件
    /// </summary>
    /// <param name="answerValue">答案值</param>
    /// <param name="answerText">答案文本</param>
    /// <returns>文本显示组件</returns>
    private JObject CreateTextAnswerComponent(string answerValue, string? answerText)
    {
        var html = $"<div style=\"padding: 10px;\"><strong>答案：</strong>{answerValue}";
        
        if (!string.IsNullOrEmpty(answerText) && answerText != answerValue)
        {
            html += $"<br/><strong>详细内容：</strong>{answerText}";
        }
        
        html += "</div>";

        return new JObject
        {
            ["type"] = "html",
            ["html"] = html
        };
    }
}

/// <summary>
/// 批量删除回答请求DTO
/// </summary>
[DisplayName("批量删除回答")]
public class BatchDeleteResponsesRequest
{
    /// <summary>
    /// 回答ID列表
    /// </summary>
    [DisplayName("回答ID列表")]
    public List<int> Ids { get; set; } = new();
}

/// <summary>
/// 根据问卷ID删除回答请求DTO
/// </summary>
[DisplayName("清空问卷回答")]
public class DeleteResponsesBySurveyRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }
}
