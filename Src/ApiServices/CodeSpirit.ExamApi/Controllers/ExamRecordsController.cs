using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.Shared.Services.Background;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using CodeSpirit.Shared.Services.Files;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Services.Background.Dtos;
using System.IO.Compression;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试记录控制器
/// </summary>
[DisplayName("考试记录管理")]
[Navigation(Icon = "fa-solid fa-clipboard-check", PlatformType = PlatformType.Tenant)]
public class ExamRecordsController : ApiControllerBase
{
    private readonly IExamRecordService _examRecordService;
    private readonly IExamPaperService _examPaperService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly Services.PdfGeneration.IQuestPdfGenerationService? _questPdfGenerationService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ExamRecordsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="examPaperService">试卷服务</param>
    /// <param name="backgroundJobService">后台任务服务</param>
    /// <param name="currentUser">当前用户信息</param>
    /// <param name="logger">日志服务</param>
    /// <param name="questPdfGenerationService">QuestPDF生成服务（可选）</param>
    public ExamRecordsController(
        IExamRecordService examRecordService,
        IExamPaperService examPaperService,
        IBackgroundJobService backgroundJobService,
        ICurrentUser currentUser,
        ILogger<ExamRecordsController> logger,
        Services.PdfGeneration.IQuestPdfGenerationService? questPdfGenerationService = null)
    {
        _examRecordService = examRecordService;
        _examPaperService = examPaperService;
        _backgroundJobService = backgroundJobService;
        _questPdfGenerationService = questPdfGenerationService;
        _currentUser = currentUser;
        _logger = logger;
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

        // 创建题目字典，以QuestionId为键，包含OrderNumber（答卷中的题目顺序）
        var questionOrderDict = preview.Answers
            .ToDictionary(a => a.QuestionId, a => a.OrderNumber);
        
        // 创建题目信息字典，以QuestionId为键
        var questionInfoDict = examPaper.Questions
            .ToDictionary(q => q.QuestionId, q => q);

        // 按照答卷的题目顺序排序题目列表
        var orderedQuestions = preview.Answers
            .OrderBy(a => a.OrderNumber)
            .Select(a => questionInfoDict.ContainsKey(a.QuestionId) ? questionInfoDict[a.QuestionId] : null)
            .Where(q => q != null)
            .ToList()!;

        // 计算总得分和成绩换算信息
        double finalScore = preview.StudentScore ?? 0;
        bool isScoreConverted = examPaper.EnableScoreConversion && examPaper.OriginalTotalScore.HasValue && examPaper.ConversionRatio.HasValue;
        
        // 计算原始成绩（如果启用了换算）
        double originalScore = finalScore;
        int originalFullScore = examPaper.TotalScore;
        int convertedFullScore = examPaper.TotalScore;
        
        if (isScoreConverted)
        {
            originalFullScore = examPaper.OriginalTotalScore!.Value;
            convertedFullScore = examPaper.TotalScore;
            // 反向计算原始成绩
            originalScore = finalScore / (double)examPaper.ConversionRatio!.Value;
        }

        // 按题目类型分组（使用排序后的题目列表）
        var questionsByType = orderedQuestions
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
                ["subTitle"] = isScoreConverted 
                    ? $"总分: {finalScore:F1}/{convertedFullScore} (原始: {originalScore:F1}/{originalFullScore})"
                    : $"总分: {finalScore}/{preview.TotalScore}"
            },
            ["body"] = new JArray()
        };

        var headerBody = (JArray)header["body"];

        // 成绩换算提醒框
        if (isScoreConverted)
        {
            var conversionDescription = $"将{originalFullScore}分制转换为{convertedFullScore}分制";
            if (examPaper.ConversionRatio.HasValue)
            {
                conversionDescription += $"，换算比例为{examPaper.ConversionRatio!.Value:F4}";
            }
            if (examPaper.ConversionDecimalPlaces > 0)
            {
                conversionDescription += $"，小数保留{examPaper.ConversionDecimalPlaces}位";
            }

            headerBody.Add(new JObject
            {
                ["type"] = "alert",
                ["level"] = "success",
                ["className"] = "score-conversion-notice",
                ["body"] = $@"
                    <div class=""score-conversion-info"">
                        <div class=""conversion-title"">✅ 已应用成绩换算</div>
                        <div class=""conversion-details"">
                            <div class=""score-comparison"">
                                <div class=""original-score"">
                                    原始成绩：<strong>{originalScore:F1}/{originalFullScore}分</strong>
                                </div>
                                <div class=""conversion-arrow"">↓ 换算</div>
                                <div class=""converted-score"">
                                    最终成绩：<strong style=""color: #52c41a"">{finalScore:F1}/{convertedFullScore}分</strong>
                                </div>
                            </div>
                            <div class=""conversion-desc"">
                                <small>{conversionDescription}</small>
                            </div>
                        </div>
                    </div>
                "
            });
        }

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

        // 按照答卷的题目顺序遍历所有题目
        int questionIndex = 1;
        foreach (var question in orderedQuestions)
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

            // 按照答卷的题目顺序排序该类型的题目
            var typeQuestions = questionsByType[type]
                .OrderBy(q => questionOrderDict.ContainsKey(q.QuestionId) ? questionOrderDict[q.QuestionId] : int.MaxValue)
                .ToList();
            
            foreach (var question in typeQuestions)
            {
                // 使用答卷中的顺序号
                int orderNumber = questionOrderDict.ContainsKey(question.QuestionId) 
                    ? questionOrderDict[question.QuestionId] 
                    : orderedQuestions.IndexOf(question) + 1;
                var questionCard = CreateQuestionCard(question, orderNumber, preview.Answers);
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
                            ["label"] = $"✓ {(string.Equals(correctAnswer, "True", StringComparison.OrdinalIgnoreCase) ? "✓" : "")}",
                            ["value"] = "True"
                        },
                        new JObject
                        {
                            ["label"] = $"✗ {(string.Equals(correctAnswer, "False", StringComparison.OrdinalIgnoreCase) ? "✓" : "")}",
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
    /// 批量导出考生试卷（使用 QuestPDF，优化版）
    /// </summary>
    /// <param name="dto">批量导出参数</param>
    /// <returns>导出任务信息</returns>
    [HttpPost("BatchExportPdfV2")]
    [Operation("批量导出试卷(V2)", "form", null, null, isBulkOperation: true, Redirect = "${WEB_HOST|raw}${redirect|raw}", Blank = true)]
    [DisplayName("批量导出试卷(V2)")]
    public async Task<ActionResult<ApiResponse<JObject>>> BatchExportExamPapersPdfV2([FromBody] BatchExportExamPapersDto dto)
    {
        if (_questPdfGenerationService == null)
        {
            return BadResponse<JObject>("QuestPDF 服务未启用，请使用原版导出功能");
        }

        if (dto.Ids == null || !dto.Ids.Any())
        {
            return BadResponse<JObject>("请至少选择一条考试记录");
        }

        // 捕获当前租户上下文，在后台任务中恢复
        var capturedTenantId = _currentUser.TenantId;
        var capturedUserId = _currentUser.Id;
        var capturedUserName = _currentUser.UserName;

        _logger.LogDebug("捕获租户上下文用于PDF导出任务：TenantId={TenantId}, UserId={UserId}, UserName={UserName}",
            capturedTenantId, capturedUserId, capturedUserName);

        // 创建导出任务
        var taskId = Guid.NewGuid().ToString();
        var fileName = $"考试试卷导出_{DateTime.Now:yyyyMMddHHmmss}.zip";
        var taskInfo = new ExportTaskDto
        {
            TaskId = taskId,
            FileName = fileName,
            Status = "处理中",
            Progress = 0,
            ProcessedRecords = 0,
            TotalRecords = dto.Ids.Count,
            CreateTime = DateTime.UtcNow
        };

        // 启动后台任务处理导出
        await _backgroundJobService.EnqueueAsync(async (serviceScopeFactory, cancellationToken) =>
        {
            using var scope = serviceScopeFactory.CreateScope();

            // 在后台任务的作用域中恢复租户上下文
            if (!string.IsNullOrEmpty(capturedTenantId))
            {
                try
                {
                    var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
                    if (currentUser is ISettableCurrentUser settableCurrentUser)
                    {
                        settableCurrentUser.SetTenantId(capturedTenantId);
                        if (capturedUserId.HasValue)
                        {
                            settableCurrentUser.SetUserId(capturedUserId);
                        }
                        if (!string.IsNullOrEmpty(capturedUserName))
                        {
                            settableCurrentUser.SetUserName(capturedUserName);
                        }
                        _logger.LogDebug("已在后台任务中设置租户上下文：TenantId={TenantId}", capturedTenantId);
                    }
                    else
                    {
                        _logger.LogWarning("无法设置租户上下文，ICurrentUser不支持设置租户ID");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "设置租户上下文时发生错误，任务ID: {TaskId}", taskId);
                }
            }

            var scopedFileService = scope.ServiceProvider.GetRequiredService<ITempFileService>();
            await scopedFileService.UpdateExportTaskAsync(taskInfo);

            // 设置整体任务超时，防止任务无限期运行
            using var overallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            overallCts.CancelAfter(TimeSpan.FromMinutes(30)); // 30分钟整体超时

            int processedCount = 0; // 移到try块之前，使catch块能够访问

            try
            {
                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"exam_export_{taskId}");
                Directory.CreateDirectory(tempDir);
                var zipPath = Path.Combine(tempDir, fileName);

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var recordId in dto.Ids)
                    {
                        // 检查整体任务取消状态
                        if (overallCts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            // 使用独立的作用域处理每个记录，确保DbContext正确释放
                            using (var recordScope = serviceScopeFactory.CreateScope())
                            {
                                // 在每个记录的作用域中也设置租户上下文
                                if (!string.IsNullOrEmpty(capturedTenantId))
                                {
                                    try
                                    {
                                        var recordCurrentUser = recordScope.ServiceProvider.GetService<ICurrentUser>();
                                        if (recordCurrentUser is ISettableCurrentUser recordSettableUser)
                                        {
                                            recordSettableUser.SetTenantId(capturedTenantId);
                                            if (capturedUserId.HasValue)
                                            {
                                                recordSettableUser.SetUserId(capturedUserId);
                                            }
                                            if (!string.IsNullOrEmpty(capturedUserName))
                                            {
                                                recordSettableUser.SetUserName(capturedUserName);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var recordLogger = recordScope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                                        recordLogger?.LogWarning(ex, "设置记录作用域租户上下文失败，记录ID: {RecordId}", recordId);
                                    }
                                }

                                var recordExamService = recordScope.ServiceProvider.GetRequiredService<IExamRecordService>();
                                var recordExamPaperService = recordScope.ServiceProvider.GetRequiredService<IExamPaperService>();
                                var recordQuestPdfService = recordScope.ServiceProvider.GetRequiredService<Services.PdfGeneration.IQuestPdfGenerationService>();

                                // 获取考试记录详情
                                var record = await recordExamService.GetStudentExamPaperDetailAsync(recordId);
                                if (record == null) continue;

                                var examPaper = await recordExamPaperService.GetAsync(record.ExamPaperId);
                                if (examPaper == null) continue;

                                // 获取导出设置（在作用域内获取）
                                var exportSettings = await recordExamService.GetExamPaperExportSettingsAsync();

                                // 使用 QuestPDF 生成PDF字节数组
                                var pdfBytes = await recordQuestPdfService.GenerateExamPaperPdfAsync(record, examPaper, exportSettings);

                                // 将PDF添加到ZIP
                                var studentName = record.StudentName ?? "未知学生";
                                var examName = record.ExamName ?? "未知考试";
                                var entryName = $"{examName}_{studentName}_{record.ExamRecordId}.pdf";

                                // 添加到ZIP文件
                                var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                {
                                    await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length, cancellationToken);
                                    await entryStream.FlushAsync(cancellationToken);
                                }
                            }

                            // 更新进度
                            processedCount++;
                            int progress = (int)((double)processedCount / dto.Ids.Count * 100);
                            await UpdateExportTaskProgress(taskId, progress, processedCount, scopedFileService);
                        }
                        catch (Exception ex)
                        {
                            // 记录单个试卷处理错误但继续处理其他试卷
                            var logger = scope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                            logger?.LogError(ex, "处理考试记录 {RecordId} 导出PDF时发生错误", recordId);

                            // 尝试更新任务中添加错误信息
                            try
                            {
                                var currentTask = await scopedFileService.GetExportTaskAsync(taskId);
                                if (currentTask != null)
                                {
                                    currentTask.ErrorMessages = currentTask.ErrorMessages ?? new List<string>();
                                    currentTask.ErrorMessages.Add($"记录ID {recordId}: {ex.Message}");
                                    await scopedFileService.UpdateExportTaskAsync(currentTask);
                                }
                            }
                            catch
                            {
                                // 忽略更新任务时的错误
                            }
                        }
                    }
                }

                // 上传ZIP文件到文件存储服务
                using var fileStream = System.IO.File.OpenRead(zipPath);
                var fileUploadResult = await scopedFileService.UploadToCacheAsync(fileStream, fileName, "application/zip");

                // 关闭文件流后再清理
                fileStream.Close();

                // 更新任务状态为完成并添加下载链接
                await UpdateExportTaskStatus(taskId, "已完成", 100, processedCount, fileUploadResult.FileUrl, scopedFileService);

                // 清理临时文件
                try
                {
                    if (System.IO.File.Exists(zipPath))
                    {
                        System.IO.File.Delete(zipPath);
                    }

                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    // 记录清理错误但不影响任务完成
                    var logger = scope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                    logger?.LogWarning(ex, "清理临时文件失败: {Message}", ex.Message);
                }
            }
            catch (OperationCanceledException) when (overallCts.Token.IsCancellationRequested)
            {
                // 整体任务超时或取消
                var logger = scope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                logger?.LogWarning("批量导出PDF任务被取消或超时，任务ID: {TaskId}", taskId);

                // 更新任务状态为取消
                await UpdateExportTaskStatus(taskId, "任务超时或被取消", 0, processedCount, null, scopedFileService);
            }
            catch (Exception ex)
            {
                // 记录详细错误日志
                var logger = scope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                logger?.LogError(ex, "批量导出PDF任务失败: {Message}", ex.Message);

                // 更新任务状态为失败
                await UpdateExportTaskStatus(taskId, $"失败: {ex.Message}", 0, processedCount, null, scopedFileService);
            }
        });

        // 返回进度页面链接
        var result = new JObject
        {
            ["status"] = 0,
            ["msg"] = "导出任务已创建，请稍后查看结果",
            ["data"] = new JObject
            {
                ["taskId"] = taskId,
                ["redirect"] = "/Tasks/Export-Task/" + taskId
            }
        };

        Thread.Sleep(1000);
        return Ok(result);
    }

    /// <summary>
    /// 更新导出任务进度
    /// </summary>
    private async Task UpdateExportTaskProgress(string taskId, int progress, int processedCount, ITempFileService fileService)
    {
        var taskInfo = await fileService.GetExportTaskAsync(taskId);
        if (taskInfo != null)
        {
            taskInfo.Progress = progress;
            taskInfo.ProcessedRecords = processedCount;
            taskInfo.UpdateTime = DateTime.UtcNow;

            await fileService.UpdateExportTaskAsync(taskInfo);
        }
    }

    /// <summary>
    /// 更新导出任务状态
    /// </summary>
    private async Task UpdateExportTaskStatus(
        string taskId,
        string status,
        int progress,
        int processedCount,
        string fileUrl,
        ITempFileService fileService)
    {
        var taskInfo = await fileService.GetExportTaskAsync(taskId);
        if (taskInfo != null)
        {
            taskInfo.Status = status;
            taskInfo.Progress = progress;
            taskInfo.ProcessedRecords = processedCount;
            taskInfo.FileUrl = fileUrl;
            taskInfo.UpdateTime = DateTime.UtcNow;
            taskInfo.CompletionTime = status == "已完成" ? DateTime.UtcNow : null;

            await fileService.UpdateExportTaskAsync(taskInfo);
        }
    }
}