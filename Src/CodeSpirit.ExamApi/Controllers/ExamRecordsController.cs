using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

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
    [DisplayName("获取考试记录列表")]
    public async Task<ActionResult<ApiResponse<PageList<ExamRecordDto>>>> GetExamRecords([FromQuery] ExamRecordQueryDto queryDto)
    {
        var records = await _examRecordService.GetPagedListAsync(queryDto, includes: ["ExamSetting", "Student"]);
        return SuccessResponse(records);
    }

    /// <summary>
    /// 导出考试记录列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>导出的考试记录列表</returns>
    [HttpGet("Export")]
    [DisplayName("导出考试记录列表")]
    public async Task<ActionResult<ApiResponse<PageList<ExamRecordDto>>>> Export([FromQuery] ExamRecordQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 最大导出数量限制
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取考试记录数据
        var records = await _examRecordService.GetPagedListAsync(queryDto, includes: ["ExamSetting", "Student"]);

        // 如果数据为空则返回错误信息
        return records.Items.Count == 0
            ? BadResponse<PageList<ExamRecordDto>>("没有数据可供导出")
            : SuccessResponse(records);
    }

    /// <summary>
    /// 获取考试记录详情
    /// </summary>
    /// <param name="id">考试记录ID</param>
    /// <returns>考试记录详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取考试记录详情")]
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
    [DisplayName("获取考试统计信息")]
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
    [DisplayName("获取错题列表")]
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
    [DisplayName("预览答卷")]
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
    [DisplayName("获取试卷题目预览配置")]
    public async Task<ActionResult<ApiResponse<JObject>>> GetExamQuestionsPreviewConfig(long id)
    {
        var preview = await _examRecordService.GetAnswerPreviewAsync(id);

        var examPaper = await _examPaperService.GetAsync(preview.ExamPaperId);
        if (examPaper == null)
        {
            return NotFound("试卷不存在");
        }
        
        // 计算总得分
        double totalScore = preview.StudentScore ?? 0;
        
        // 按题目类型分组
        var questionsByType = examPaper.Questions
            .GroupBy(q => q.Type.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());
            
        // 计算每种题型的得分
        var scoreByType = new Dictionary<string, (double Score, int TotalScore)>();
        foreach (var type in questionsByType.Keys)
        {
            var typeQuestions = questionsByType[type];
            var typeAnswers = preview.Answers.Where(a => a.QuestionType == type).ToList();
            
            double typeScore = typeAnswers.Sum(a => a.Score ?? 0);
            int typeTotalScore = typeQuestions.Sum(q => q.Score);
            
            scoreByType[type] = (typeScore, typeTotalScore);
        }

        // 创建Amis页面
        var page = new JObject
        {
            ["type"] = "page",
            ["title"] = "答卷预览",
            ["body"] = new JArray()
        };
        
        // 添加头部统计信息
        var header = new JObject
        {
            ["type"] = "card",
            ["header"] = new JObject
            {
                ["title"] = "成绩统计",
                ["subTitle"] = $"总分: {totalScore}/{preview.TotalScore}"
            },
            ["body"] = new JArray()
        };
        
        var headerBody = (JArray)header["body"];
        
        // 添加各题型得分表格
        var typeScoreTable = new JObject
        {
            ["type"] = "table",
            ["columns"] = new JArray(),
            ["items"] = new JArray()
        };
        
        var tableColumns = (JArray)typeScoreTable["columns"];
        tableColumns.Add(new JObject
        {
            ["label"] = "题型",
            ["name"] = "type"
        });
        
        tableColumns.Add(new JObject
        {
            ["label"] = "得分/总分",
            ["name"] = "score",
            ["type"] = "tpl",
            ["tpl"] = "<span class=\"${scoreClass}\">${score}/${totalScore}</span>"
        });
        
        var tableItems = (JArray)typeScoreTable["items"];
        foreach (var typeScore in scoreByType)
        {
            string typeName = GetQuestionTypeName(typeScore.Key);
            bool isPassed = typeScore.Value.Score >= typeScore.Value.TotalScore * 0.6;
            
            tableItems.Add(new JObject
            {
                ["type"] = typeName,
                ["score"] = typeScore.Value.Score,
                ["totalScore"] = typeScore.Value.TotalScore,
                ["scoreClass"] = isPassed ? "text-success" : "text-danger"
            });
        }
        
        headerBody.Add(typeScoreTable);
        ((JArray)page["body"]).Add(header);
        
        // 创建选项卡区域
        var tabs = new JObject
        {
            ["type"] = "tabs",
            ["tabs"] = new JArray()
        };
        
        // 添加全部题目选项卡
        var allTab = new JObject
        {
            ["title"] = "全部题目",
            ["body"] = new JArray()
        };
        
        // 遍历所有题目
        int questionIndex = 1;
        foreach (var question in examPaper.Questions)
        {
            var questionCard = CreateQuestionCard(question, questionIndex++, preview.Answers);
            ((JArray)allTab["body"]).Add(questionCard);
        }
        
        ((JArray)tabs["tabs"]).Add(allTab);
        
        // 为每种题型创建选项卡
        foreach (var type in questionsByType.Keys)
        {
            string typeName = GetQuestionTypeName(type);
            var typeTab = new JObject
            {
                ["title"] = $"{typeName} ({scoreByType[type].Score}/{scoreByType[type].TotalScore})",
                ["body"] = new JArray()
            };
            
            var typeQuestions = questionsByType[type];
            foreach (var question in typeQuestions)
            {
                int globalIndex = examPaper.Questions.IndexOf(question) + 1;
                var questionCard = CreateQuestionCard(question, globalIndex, preview.Answers);
                ((JArray)typeTab["body"]).Add(questionCard);
            }
            
            ((JArray)tabs["tabs"]).Add(typeTab);
        }
        
        ((JArray)page["body"]).Add(tabs);
        
        return SuccessResponse(page);
    }
    
    /// <summary>
    /// 创建题目卡片
    /// </summary>
    private JObject CreateQuestionCard(ExamPaperQuestionDto question, int index, List<ClientExamAnswerWithCorrectDto> answers)
    {
        var answerInfo = answers.FirstOrDefault(x => x.QuestionId == question.QuestionId);
        var studentAnswer = answerInfo?.Answer ?? "";
        var correctAnswer = answerInfo?.CorrectAnswer ?? "";
        var score = answerInfo?.Score ?? 0;
        var isCorrect = answerInfo?.IsCorrect ?? false;
        var maxScore = question.Score;
        
        // 确定状态标签
        string statusLabel;
        string statusClass;
        
        if (isCorrect)
        {
            statusLabel = "正确";
            statusClass = "success";
        }
        else if (score > 0)
        {
            statusLabel = "部分得分";
            statusClass = "warning";
        }
        else
        {
            statusLabel = "错误";
            statusClass = "danger";
        }
        
        // 创建卡片
        var card = new JObject
        {
            ["type"] = "card",
            ["className"] = "mb-2",
            ["header"] = new JObject
            {
                ["title"] = $"{index}. {question.Content}",
                ["badge"] = new JObject
                {
                    ["label"] = statusLabel,
                    ["variant"] = statusClass
                },
                ["subTitle"] = $"得分: {score}/{maxScore}"
            },
            ["body"] = new JArray()
        };
        
        var cardBody = (JArray)card["body"];
        
        // 根据题目类型添加不同内容
        switch (question.Type.ToString())
        {
            case "SingleChoice":
                // 单选题
                var radioGroup = new JObject
                {
                    ["type"] = "radios",
                    ["name"] = $"q{question.Id}",
                    ["value"] = studentAnswer,
                    ["disabled"] = true,
                    ["options"] = new JArray()
                };
                
                var radioOptions = (JArray)radioGroup["options"];
                foreach (var option in question.Options)
                {
                    bool isCorrectOption = option == correctAnswer;
                    radioOptions.Add(new JObject
                    {
                        ["label"] = $"{option} {(isCorrectOption ? "✓" : "")}",
                        ["value"] = option
                    });
                }
                
                cardBody.Add(radioGroup);
                break;
                
            case "MultipleChoice":
                // 多选题
                var selectedOptions = studentAnswer.Split(',').Select(o => o.Trim()).ToArray();
                var correctOptions = correctAnswer.Split(',').Select(o => o.Trim()).ToArray();
                
                var checkboxGroup = new JObject
                {
                    ["type"] = "checkboxes",
                    ["name"] = $"q{question.Id}",
                    ["value"] = new JArray(selectedOptions),
                    ["disabled"] = true,
                    ["options"] = new JArray()
                };
                
                var checkOptions = (JArray)checkboxGroup["options"];
                foreach (var option in question.Options)
                {
                    bool isCorrectOption = correctOptions.Contains(option);
                    checkOptions.Add(new JObject
                    {
                        ["label"] = $"{option} {(isCorrectOption ? "✓" : "")}",
                        ["value"] = option
                    });
                }
                
                cardBody.Add(checkboxGroup);
                break;
                
            case "TrueFalse":
                // 判断题
                var tfGroup = new JObject
                {
                    ["type"] = "radios",
                    ["name"] = $"q{question.Id}",
                    ["value"] = studentAnswer,
                    ["disabled"] = true,
                    ["options"] = new JArray
                    {
                        new JObject
                        {
                            ["label"] = $"正确 {(correctAnswer == "True" ? "✓" : "")}",
                            ["value"] = "True"
                        },
                        new JObject
                        {
                            ["label"] = $"错误 {(correctAnswer == "False" ? "✓" : "")}",
                            ["value"] = "False"
                        }
                    }
                };
                
                cardBody.Add(tfGroup);
                break;
                
            default:
                // 简答题
                // 学生答案
                cardBody.Add(new JObject
                {
                    ["type"] = "alert",
                    ["level"] = "info",
                    ["showIcon"] = true,
                    ["title"] = "学生答案",
                    ["body"] = string.IsNullOrEmpty(studentAnswer) ? "未作答" : studentAnswer
                });
                
                // 正确答案
                cardBody.Add(new JObject
                {
                    ["type"] = "alert",
                    ["level"] = "success",
                    ["showIcon"] = true,
                    ["title"] = "参考答案",
                    ["body"] = correctAnswer
                });
                break;
        }
        
        return card;
    }
    
    /// <summary>
    /// 获取题目类型名称
    /// </summary>
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

    /// <summary>
    /// 重新批改考试分数
    /// </summary>
    /// <param name="modifyExamScoreDto">批改参数</param>
    /// <returns>考试记录详情</returns>
    [HttpPut("{id}/regrade")]
    [Operation("重新批改", "form", null, "确定要重新批改吗？", visibleOn: "status===3")]
    [DisplayName("重新批改考试")]
    public async Task<ActionResult<ApiResponse<ExamRecordDto>>> ModifyExamScore(long id, ModifyExamScoreDto modifyExamScoreDto)
    {
        if (!ModelState.IsValid)
        {
            return BadResponse<ExamRecordDto>("请求参数无效");
        }

        try
        {
            var result = await _examRecordService.ModifyExamScoreAsync(id, modifyExamScoreDto);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return BadResponse<ExamRecordDto>(ex.Message);
        }
    }
    
    /// <summary>
    /// 批量批改考试分数
    /// </summary>
    /// <param name="request">批量批改请求数据</param>
    /// <returns>批改结果</returns>
    [HttpPost("batch/regrade")]
    [Operation("批量批改", "form", null, "确定要批量批改选中的考试记录吗？", isBulkOperation: true)]
    [DisplayName("批量批改")]
    public async Task<ActionResult<ApiResponse>> BatchModifyExamScore([FromBody] BatchModifyExamScoreDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        if (!ModelState.IsValid)
        {
            return BadResponse("请求参数无效");
        }
        
        int successCount = 0;
        List<long> failedIds = new();
        
        // 创建单个批改DTO
        var modifyExamScoreDto = new ModifyExamScoreDto
        {
            TargetScore = request.TargetScore
        };
        
        foreach (var id in request.Ids)
        {
            try
            {
                await _examRecordService.ModifyExamScoreAsync(id, modifyExamScoreDto);
                successCount++;
            }
            catch
            {
                failedIds.Add(id);
            }
        }
        
        return failedIds.Any()
            ? SuccessResponse($"成功批改 {successCount} 个考试记录，但以下考试记录批改失败: {string.Join(", ", failedIds)}")
            : SuccessResponse($"成功批改 {successCount} 个考试记录！");
    }
}