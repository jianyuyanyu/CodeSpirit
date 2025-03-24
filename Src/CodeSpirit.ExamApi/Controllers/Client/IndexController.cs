using CodeSpirit.ExamApi.Dtos.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.ExamApi.Controllers.Client;

/// <summary>
/// 考试客户端接口
/// </summary>
[DisplayName("考试客户端")]
[Route("api/exam/client")]
public class IndexController : ApiControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<IndexController> _logger;
    private readonly ICurrentUser currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientService">客户端服务</param>
    /// <param name="logger">日志服务</param>
    public IndexController(IClientService clientService, ILogger<IndexController> logger, ICurrentUser currentUser)
    {
        _clientService = clientService;
        _logger = logger;
        this.currentUser = currentUser;
    }

    /// <summary>
    /// 获取可参加的考试列表
    /// </summary>
    /// <returns>可参加的考试列表</returns>
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<List<ClientExamDto>>>> GetAvailableExams()
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetAvailableExamsAsync(currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试历史记录
    /// </summary>
    /// <returns>考试历史记录</returns>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<ClientExamHistoryDto>>>> GetExamHistory()
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetExamHistoryAsync(currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ClientExamDetailDto>>> GetExamDetail(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }
        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        var result = await _clientService.GetExamDetailAsync(id, currentUserId, userIp, deviceInfo);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <param name="answers">考试答案</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitExam(long id, [FromBody] List<ClientExamAnswerDto> answers)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }
        await _clientService.SubmitExamAsync(id, currentUserId, answers);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取考试结果
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试结果</returns>
    [HttpGet("result/{id}")]
    public async Task<ActionResult<ApiResponse<ClientExamResultDto>>> GetExamResult(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        var result = await _clientService.GetExamResultAsync(id, currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取考试题目的Amis配置
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试题目的Amis配置</returns>
    [HttpGet("{id}/amis")]
    public async Task<IActionResult> GetExamAmisConfig(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }

        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        // 获取考试详情
        var examDetail = await _clientService.GetExamDetailAsync(id, currentUserId, userIp, deviceInfo);

        // 使用JObject/JArray构建表单
        var formItems = new JArray();

        // 为每个题目创建对应的表单组件
        for (int i = 0; i < examDetail.Questions.Count; i++)
        {
            var question = examDetail.Questions[i];
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
            switch (question.Type)
            {
                case "SingleChoice":
                    // 解析选项
                    var singleOptions = new JArray();
                    var options = question.Options.Split(',');
                    for (int idx = 0; idx < options.Length; idx++)
                    {
                        singleOptions.Add(new JObject
                        {
                            ["label"] = options[idx],
                            ["value"] = ((char)('A' + idx)).ToString()
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
                                    ["script"] = $"saveAnswer({question.Id}, event.data.value);"
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
                    var multiChoiceOptions = question.Options.Split(',');
                    for (int idx = 0; idx < multiChoiceOptions.Length; idx++)
                    {
                        multiOptions.Add(new JObject
                        {
                            ["label"] = multiChoiceOptions[idx],
                            ["value"] = ((char)('A' + idx)).ToString()
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
                                    ["script"] = $"saveAnswer({question.Id}, event.data.value);"
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
                                    ["script"] = $"saveAnswer({question.Id}, event.data.value);"
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
                                    ["script"] = $"saveAnswer({question.Id}, event.data.value);"
                                }
                            }
                        }
                    };
                    textareaObj["onEvent"] = textareaEvent;
                    formItems.Add(textareaObj);
                    break;
            }

            // 如果不是最后一个题目，添加分隔线
            if (i < examDetail.Questions.Count - 1)
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

        return Ok(amisConfig);
    }

    /// <summary>
    /// 获取考试基本信息
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试基本信息</returns>
    [HttpGet("{id}/basic")]
    public async Task<ActionResult<ApiResponse<ClientExamBasicInfoDto>>> GetExamBasicInfo(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }

        var result = await _clientService.GetExamBasicInfoAsync(id, currentUserId);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建考试记录
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试记录ID</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult<object>> StartExam(long id)
    {
        var currentUserId = currentUser.Id.HasValue ? currentUser.Id.Value : 0;
        if (currentUserId == 0)
        {
            return Unauthorized();
        }

        var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知";
        var deviceInfo = HttpContext.Request.Headers["User-Agent"].ToString();

        var record = await _clientService.CreateExamRecordAsync(id, currentUserId, userIp, deviceInfo);
        return new
        {
            id = record.ExamSettingId
        };
    }
}