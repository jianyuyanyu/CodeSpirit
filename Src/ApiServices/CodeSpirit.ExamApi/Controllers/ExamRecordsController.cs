using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.Shared.Services.Background;
using CodeSpirit.Shared.Services.Background.Dtos;
using CodeSpirit.Shared.Services.Files;
using CodeSpirit.PdfGeneration.Services;
using PuppeteerSharp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Text;
using PuppeteerSharp.Media;
using CodeSpirit.Core.Enums;

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
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly ILogger<ExamRecordsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="examPaperService">试卷服务</param>
    /// <param name="backgroundJobService">后台任务服务</param>
    /// <param name="pdfGenerationService">PDF生成服务</param>
    /// <param name="logger">日志服务</param>
    public ExamRecordsController(
        IExamRecordService examRecordService,
        IExamPaperService examPaperService,
        IBackgroundJobService backgroundJobService,
        IPdfGenerationService pdfGenerationService,
        ILogger<ExamRecordsController> logger)
    {
        _examRecordService = examRecordService;
        _examPaperService = examPaperService;
        _backgroundJobService = backgroundJobService;
        _pdfGenerationService = pdfGenerationService;
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
    /// 批量导出考生试卷
    /// </summary>
    /// <param name="dto">批量导出参数</param>
    /// <returns>导出任务信息</returns>
    [HttpPost("BatchExportPdf")]
    [Operation("批量导出试卷", "form", null, null, isBulkOperation: true, Redirect = "${WEB_HOST|raw}${redirect|raw}")]
    [DisplayName("批量导出试卷")]
    public async Task<ActionResult<ApiResponse<JObject>>> BatchExportExamPapersPdf([FromBody] BatchExportExamPapersDto dto)
    {
        if (dto.Ids == null || !dto.Ids.Any())
        {
            return BadResponse<JObject>("请至少选择一条考试记录");
        }

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
                                var recordExamService = recordScope.ServiceProvider.GetRequiredService<IExamRecordService>();
                                var recordExamPaperService = recordScope.ServiceProvider.GetRequiredService<IExamPaperService>();
                                var recordPdfService = recordScope.ServiceProvider.GetRequiredService<IPdfGenerationService>();

                                // 获取考试记录详情
                                var record = await recordExamService.GetStudentExamPaperDetailAsync(recordId);
                                if (record == null) continue;

                                var examPaper = await recordExamPaperService.GetAsync(record.ExamPaperId);
                                if (examPaper == null) continue;

                                // 获取导出设置（在作用域内获取）
                                var exportSettings = await recordExamService.GetExamPaperExportSettingsAsync();

                                // 生成PDF字节数组（传入作用域内的服务和设置）
                                var pdfBytes = await GeneratePdfFromHtmlWithScope(record, examPaper, exportSettings, recordPdfService, recordScope.ServiceProvider.GetService<ILogger<ExamRecordsController>>());

                                // 将PDF添加到ZIP
                                var studentName = record.StudentName ?? "未知学生";
                                var examName = record.ExamName ?? "未知考试";
                                var entryName = $"{examName}_{studentName}_{record.ExamRecordId}.pdf";

                                // 添加到ZIP文件
                                var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                {
                                    // 分块写入大文件，避免超时
                                    int bufferSize = 8192; // 8KB 缓冲区
                                    int offset = 0;
                                    
                                    while (offset < pdfBytes.Length)
                                    {
                                        // 检查取消令牌但不在写入操作中使用，避免取消导致ZIP文件损坏
                                        if (overallCts.Token.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                        
                                        int bytesToWrite = Math.Min(bufferSize, pdfBytes.Length - offset);
                                        
                                        // 使用短超时写入，而不是依赖 cancellationToken
                                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                                        {
                                            try
                                            {
                                                await entryStream.WriteAsync(pdfBytes, offset, bytesToWrite, cts.Token);
                                                await entryStream.FlushAsync(cts.Token);
                                                offset += bytesToWrite;
                                            }
                                            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                                            {
                                                // 写入超时，记录日志但继续处理下一个文件
                                                var logger = recordScope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                                                logger?.LogWarning("写入ZIP文件超时，跳过记录 {RecordId}", recordId);
                                                break;
                                            }
                                        }
                                    }
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
                            // 可以添加日志记录逻辑
                            Console.WriteLine($"处理考试记录 {recordId} 时发生错误: {ex.Message}");

                            // 获取记录错误的日志服务
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
    /// 在独立作用域中生成试卷PDF
    /// </summary>
    /// <param name="record">考试记录详情</param>
    /// <param name="examPaper">试卷信息</param>
    /// <param name="exportSettings">导出设置</param>
    /// <param name="pdfService">PDF生成服务</param>
    /// <param name="logger">日志服务</param>
    /// <returns>PDF字节数组</returns>
    private async Task<byte[]> GeneratePdfFromHtmlWithScope(
        ExamPaperDetailDto record, 
        ExamPaperDto examPaper, 
        ExamPaperExportSettingsDto exportSettings,
        IPdfGenerationService pdfService,
        ILogger<ExamRecordsController> logger)
    {
        try
        {
            // 确保PDF生成服务已初始化
            var status = await pdfService.GetStatusAsync();
            if (!status.IsInitialized)
            {
                await pdfService.InitializeAsync();
            }

            // 生成HTML内容
            string htmlContent = await GenerateHtmlContent(record, examPaper, exportSettings);

            // 根据设置配置PDF生成选项
            var pdfOptions = new PdfOptions
            {
                Format = exportSettings.PageSize switch
                {
                    PdfPageSize.A3 => PaperFormat.A3,
                    PdfPageSize.Letter => PaperFormat.Letter,
                    _ => PaperFormat.A4
                },
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "10mm",
                    Bottom = "10mm",
                    Left = "10mm",
                    Right = "10mm"
                },
                PreferCSSPageSize = true,
                Scale = 1.0m,
                Landscape = exportSettings.PageOrientation == PdfOrientation.Landscape
            };

            // 生成PDF
            var result = await pdfService.GeneratePdfAsync(htmlContent, pdfOptions);
            logger?.LogInformation(
                "成功生成PDF，学生：{StudentName}，考试：{ExamName}，大小：{Size:N0} 字节",
                record.StudentName,
                record.ExamName,
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, 
                "生成PDF失败，学生：{StudentName}，考试：{ExamName}",
                record.StudentName,
                record.ExamName);
            throw;
        }
    }

    /// <summary>
    /// 将HTML转换为PDF
    /// </summary>
    private async Task<string> GenerateHtmlContent(ExamPaperDetailDto record, ExamPaperDto examPaper, ExamPaperExportSettingsDto exportSettings)
    {
        // 使用HTML模板生成PDF内容
        StringBuilder htmlBuilder = new StringBuilder();
        htmlBuilder.AppendLine("<!DOCTYPE html>");
        htmlBuilder.AppendLine("<html lang=\"zh-CN\">");
        htmlBuilder.AppendLine("<head>");
        htmlBuilder.AppendLine("  <meta charset=\"UTF-8\">");
        htmlBuilder.AppendLine("  <title>考试答卷</title>");
        htmlBuilder.AppendLine("  <style>");
        htmlBuilder.AppendLine("    body { font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 20px; }");
        htmlBuilder.AppendLine("    .header { text-align: center; margin-bottom: 20px; border-bottom: 2px solid #ddd; padding-bottom: 15px; }");
        htmlBuilder.AppendLine("    .title { font-size: 20px; font-weight: bold; margin-bottom: 10px; }");
        htmlBuilder.AppendLine("    .info { font-size: 14px; margin-top: 8px; line-height: 1.6; }");
        htmlBuilder.AppendLine("    .summary { border: 1px solid #ddd; padding: 10px; margin-bottom: 20px; }");
        htmlBuilder.AppendLine("    .summary-title { font-weight: bold; margin-bottom: 10px; }");
        htmlBuilder.AppendLine("    .summary-table { width: 100%; border-collapse: collapse; }");
        htmlBuilder.AppendLine("    .summary-table th, .summary-table td { border: 1px solid #ddd; padding: 8px; text-align: center; }");
        htmlBuilder.AppendLine("    .summary-table th { background-color: #f2f2f2; }");
        htmlBuilder.AppendLine("    .question-type { font-weight: bold; border-bottom: 1px solid #ddd; margin: 15px 0 10px 0; padding-bottom: 5px; }");
        htmlBuilder.AppendLine("    .question { margin-bottom: 15px; }");
        htmlBuilder.AppendLine("    .question-title { margin-bottom: 5px; }");
        htmlBuilder.AppendLine("    .question-content { margin-left: 20px; }");
        htmlBuilder.AppendLine("    .correct { color: green; }");
        htmlBuilder.AppendLine("    .incorrect { color: red; }");
        htmlBuilder.AppendLine("    .option { margin: 5px 0; }");
        htmlBuilder.AppendLine("    .answer-box { background-color: #f9f9f9; padding: 10px; margin-top: 5px; }");
        htmlBuilder.AppendLine("    .correct-answer { background-color: #f0fff0; padding: 10px; margin-top: 5px; }");
        htmlBuilder.AppendLine("    .footer { text-align: center; font-size: 12px; margin-top: 30px; }");
        
        // 添加水印样式
        if (exportSettings.EnableWatermark)
        {
            htmlBuilder.AppendLine("    .watermark {");
            htmlBuilder.AppendLine("      position: fixed;");
            htmlBuilder.AppendLine("      top: 50%;");
            htmlBuilder.AppendLine("      left: 50%;");
            htmlBuilder.AppendLine("      transform: translate(-50%, -50%) rotate(" + exportSettings.WatermarkRotation + "deg);");
            htmlBuilder.AppendLine($"      font-size: {exportSettings.WatermarkFontSize}px;");
            htmlBuilder.AppendLine($"      color: {exportSettings.WatermarkColor};");
            htmlBuilder.AppendLine($"      opacity: {exportSettings.WatermarkOpacity};");
            htmlBuilder.AppendLine("      pointer-events: none;");
            htmlBuilder.AppendLine("      z-index: 1000;");
            htmlBuilder.AppendLine("      white-space: nowrap;");
            htmlBuilder.AppendLine("      font-weight: bold;");
            htmlBuilder.AppendLine("    }");
        }

        // 添加页眉页脚样式
        if (!string.IsNullOrEmpty(exportSettings.HeaderText))
        {
            htmlBuilder.AppendLine("    .page-header {");
            htmlBuilder.AppendLine("      position: fixed;");
            htmlBuilder.AppendLine("      top: 0;");
            htmlBuilder.AppendLine("      left: 0;");
            htmlBuilder.AppendLine("      right: 0;");
            htmlBuilder.AppendLine("      height: 30px;");
            htmlBuilder.AppendLine("      text-align: center;");
            htmlBuilder.AppendLine("      font-size: 12px;");
            htmlBuilder.AppendLine("      border-bottom: 1px solid #ddd;");
            htmlBuilder.AppendLine("      padding: 5px;");
            htmlBuilder.AppendLine("      background: white;");
            htmlBuilder.AppendLine("    }");
        }

        if (!string.IsNullOrEmpty(exportSettings.FooterText))
        {
            htmlBuilder.AppendLine("    .page-footer {");
            htmlBuilder.AppendLine("      position: fixed;");
            htmlBuilder.AppendLine("      bottom: 0;");
            htmlBuilder.AppendLine("      left: 0;");
            htmlBuilder.AppendLine("      right: 0;");
            htmlBuilder.AppendLine("      height: 30px;");
            htmlBuilder.AppendLine("      text-align: center;");
            htmlBuilder.AppendLine("      font-size: 12px;");
            htmlBuilder.AppendLine("      border-top: 1px solid #ddd;");
            htmlBuilder.AppendLine("      padding: 5px;");
            htmlBuilder.AppendLine("      background: white;");
            htmlBuilder.AppendLine("    }");
        }

        htmlBuilder.AppendLine("  </style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        // 添加水印元素
        if (exportSettings.EnableWatermark)
        {
            var watermarkText = ReplaceWatermarkVariables(exportSettings.WatermarkText, record, examPaper);
            htmlBuilder.AppendLine($"  <div class=\"watermark\">{watermarkText}</div>");
        }

        // 添加页眉
        if (!string.IsNullOrEmpty(exportSettings.HeaderText))
        {
            var headerText = ReplaceWatermarkVariables(exportSettings.HeaderText, record, examPaper);
            htmlBuilder.AppendLine($"  <div class=\"page-header\">{headerText}</div>");
        }

        // 添加页脚
        if (!string.IsNullOrEmpty(exportSettings.FooterText))
        {
            var footerText = ReplaceWatermarkVariables(exportSettings.FooterText, record, examPaper);
            htmlBuilder.AppendLine($"  <div class=\"page-footer\">{footerText}</div>");
        }

        // 页面头部
        htmlBuilder.AppendLine("  <div class=\"header\">");
        htmlBuilder.AppendLine($"    <div class=\"title\">{examPaper.Name ?? "考试答卷"}</div>");
        htmlBuilder.AppendLine("    <div class=\"info\">");

        // 获取学生信息
        var studentName = record.StudentName ?? "未知学生";
        var idNo = record.IdNo ?? "未设置";
        var admissionTicket = record.AdmissionTicket ?? "未设置";

        htmlBuilder.AppendLine($"      考生姓名: {studentName} | ");
        htmlBuilder.AppendLine($"      身份证号: {idNo} | ");
        htmlBuilder.AppendLine($"      准考证号: {admissionTicket}");
        htmlBuilder.AppendLine("    </div>");
        htmlBuilder.AppendLine("    <div class=\"info\">");
        htmlBuilder.AppendLine($"      得分: {record.TotalScore}/{examPaper.TotalScore} | ");

        var submitTime = record.SubmitTime;
        var startTime = record.StartTime;
        var duration = record.Duration;

        // 将UTC时间转换为本地时间
        var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(startTime, TimeZoneInfo.Local);
        var localSubmitTime = submitTime.HasValue 
            ? TimeZoneInfo.ConvertTimeFromUtc(submitTime.Value, TimeZoneInfo.Local) 
            : (DateTime?)null;

        htmlBuilder.AppendLine($"      开始作答时间: {localStartTime.ToString("yyyy-MM-dd HH:mm:ss")} | ");
        htmlBuilder.AppendLine($"      提交时间: {localSubmitTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未提交"} | ");
        
        if (duration.HasValue)
        {
            htmlBuilder.AppendLine($"      作答时长: {duration.Value}分钟");
        }
        else if (localSubmitTime.HasValue)
        {
            var actualDuration = (int)Math.Ceiling((localSubmitTime.Value - localStartTime).TotalMinutes);
            htmlBuilder.AppendLine($"      作答时长: {actualDuration}分钟");
        }
        else
        {
            htmlBuilder.AppendLine("      作答时长: 未完成");
        }
        
        htmlBuilder.AppendLine("    </div>");
        htmlBuilder.AppendLine("  </div>");

        // 成绩统计表
        htmlBuilder.AppendLine("  <div class=\"summary\">");
        htmlBuilder.AppendLine("    <div class=\"summary-title\">成绩统计</div>");
        htmlBuilder.AppendLine("    <table class=\"summary-table\">");
        htmlBuilder.AppendLine("      <tr>");
        htmlBuilder.AppendLine("        <th>题型</th>");
        htmlBuilder.AppendLine("        <th>题目数</th>");
        htmlBuilder.AppendLine("        <th>得分/总分</th>");
        htmlBuilder.AppendLine("      </tr>");

        // 按题型统计得分
        var questionsByType = examPaper.Questions
            .GroupBy(q => q.Type.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var type in questionsByType.Keys)
        {
            var typeQuestions = questionsByType[type];
            var typeAnswers = record.Answers.Where(a => a.QuestionType == type).ToList();

            double typeScore = typeAnswers.Sum(a => a.Score ?? 0);
            int typeTotalScore = typeQuestions.Sum(q => q.Score);
            string typeName = GetQuestionTypeName(type);

            htmlBuilder.AppendLine("      <tr>");
            htmlBuilder.AppendLine($"        <td>{typeName}</td>");
            htmlBuilder.AppendLine($"        <td>{typeQuestions.Count}</td>");
            htmlBuilder.AppendLine($"        <td>{typeScore}/{typeTotalScore}</td>");
            htmlBuilder.AppendLine("      </tr>");
        }

        // 总分行
        htmlBuilder.AppendLine("      <tr>");
        htmlBuilder.AppendLine("        <th>总分</th>");
        htmlBuilder.AppendLine($"        <th>{examPaper.Questions.Count}</th>");
        htmlBuilder.AppendLine($"        <th>{record.TotalScore}/{examPaper.TotalScore}</th>");
        htmlBuilder.AppendLine("      </tr>");
        htmlBuilder.AppendLine("    </table>");
        htmlBuilder.AppendLine("  </div>");

        // 题目部分
        int questionIndex = 1;
        foreach (var type in questionsByType.Keys)
        {
            string typeName = GetQuestionTypeName(type);
            var typeQuestions = questionsByType[type];

            htmlBuilder.AppendLine($"  <div class=\"question-type\">{typeName}</div>");

            foreach (var question in typeQuestions)
            {
                var answerInfo = record.Answers.FirstOrDefault(x => x.QuestionId == question.QuestionId);
                var studentAnswer = answerInfo?.Answer ?? "";
                var correctAnswer = answerInfo?.CorrectAnswer ?? "";
                var score = answerInfo?.Score ?? 0;
                var isCorrect = answerInfo?.IsCorrect ?? false;

                htmlBuilder.AppendLine("  <div class=\"question\">");
                htmlBuilder.AppendLine($"    <div class=\"question-title\">{questionIndex++}. {question.Content} ");
                htmlBuilder.AppendLine($"      <span class=\"{(isCorrect ? "correct" : "incorrect")}\">[得分: {score}/{question.Score} {(isCorrect ? "✓" : "✗")}]</span>");
                htmlBuilder.AppendLine("    </div>");

                htmlBuilder.AppendLine("    <div class=\"question-content\">");

                switch (question.Type.ToString())
                {
                    case "SingleChoice":
                        // 单选题
                        var selectedOption = studentAnswer;
                        foreach (var option in question.Options)
                        {
                            bool isSelected = option == selectedOption;
                            bool isCorrectOption = option == correctAnswer;

                            htmlBuilder.AppendLine("      <div class=\"option\">");
                            htmlBuilder.Append($"        {(isSelected ? "☑" : "☐")} {option}");

                            if (isCorrectOption)
                            {
                                htmlBuilder.Append(" <span class=\"correct\">✓</span>");
                            }
                            else if (isSelected && !isCorrectOption)
                            {
                                htmlBuilder.Append(" <span class=\"incorrect\">✗</span>");
                            }

                            htmlBuilder.AppendLine("</div>");
                        }
                        break;

                    case "MultipleChoice":
                        // 多选题
                        var selectedOptions = studentAnswer.Split(',').Select(o => o.Trim()).ToArray();
                        var correctOptions = correctAnswer.Split(',').Select(o => o.Trim()).ToArray();

                        foreach (var option in question.Options)
                        {
                            bool isSelected = selectedOptions.Contains(option);
                            bool isCorrectOption = correctOptions.Contains(option);

                            htmlBuilder.AppendLine("      <div class=\"option\">");
                            htmlBuilder.Append($"        {(isSelected ? "☑" : "☐")} {option}");

                            if (isCorrectOption)
                            {
                                htmlBuilder.Append(" <span class=\"correct\">✓</span>");
                            }
                            else if (isSelected && !isCorrectOption)
                            {
                                htmlBuilder.Append(" <span class=\"incorrect\">✗</span>");
                            }

                            htmlBuilder.AppendLine("</div>");
                        }
                        break;

                    case "TrueFalse":
                        // 判断题
                        bool isTrue = studentAnswer == "True";
                        bool correctIsTrue = correctAnswer == "True";

                        htmlBuilder.AppendLine("      <div class=\"option\">");
                        htmlBuilder.Append($"        {(isTrue ? "☑" : "☐")} 正确");

                        if (correctIsTrue)
                        {
                            htmlBuilder.Append(" <span class=\"correct\">✓</span>");
                        }
                        else if (isTrue)
                        {
                            htmlBuilder.Append(" <span class=\"incorrect\">✗</span>");
                        }

                        htmlBuilder.AppendLine("</div>");

                        htmlBuilder.AppendLine("      <div class=\"option\">");
                        htmlBuilder.Append($"        {(!isTrue ? "☑" : "☐")} 错误");

                        if (!correctIsTrue)
                        {
                            htmlBuilder.Append(" <span class=\"correct\">✓</span>");
                        }
                        else if (!isTrue)
                        {
                            htmlBuilder.Append(" <span class=\"incorrect\">✗</span>");
                        }

                        htmlBuilder.AppendLine("</div>");
                        break;

                    default:
                        // 简答题
                        htmlBuilder.AppendLine("      <div class=\"answer-title\">学生答案:</div>");
                        htmlBuilder.AppendLine($"      <div class=\"answer-box\">{(string.IsNullOrEmpty(studentAnswer) ? "未作答" : studentAnswer)}</div>");
                        htmlBuilder.AppendLine("      <div class=\"answer-title\">参考答案:</div>");
                        htmlBuilder.AppendLine($"      <div class=\"correct-answer\">{correctAnswer}</div>");
                        break;
                }

                htmlBuilder.AppendLine("    </div>");
                htmlBuilder.AppendLine("  </div>");
            }
        }

        // 页脚
        htmlBuilder.AppendLine("  <div class=\"footer\">");
        htmlBuilder.AppendLine($"    {DateTime.Now:yyyy-MM-dd HH:mm:ss} 导出");
        htmlBuilder.AppendLine("  </div>");

        htmlBuilder.AppendLine("</body>");
        htmlBuilder.AppendLine("</html>");

        return htmlBuilder.ToString();
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

    /// <summary>
    /// 获取答卷导出设置
    /// </summary>
    /// <returns>答卷导出设置</returns>
    [HttpGet("export-settings")]
    [DisplayName("获取答卷导出设置")]
    public async Task<ActionResult<ApiResponse<ExamPaperExportSettingsDto>>> GetExamPaperExportSettings()
    {
        var settings = await _examRecordService.GetExamPaperExportSettingsAsync();
        return SuccessResponse(settings);
    }

    /// <summary>
    /// 更新答卷导出设置
    /// </summary>
    /// <param name="settings">答卷导出设置</param>
    /// <returns>操作结果</returns>
    [HttpPut("export-settings")]
    [HeaderOperation("导出设置", "form", null, null, InitApi = "/exam/api/exam/examRecords/export-settings")]
    [DisplayName("更新答卷导出设置")]
    public async Task<ActionResult<ApiResponse>> UpdateExamPaperExportSettings(
        [FromBody] ExamPaperExportSettingsDto settings)
    {
        var result = await _examRecordService.UpdateExamPaperExportSettingsAsync(settings);
        if (result)
        {
            return SuccessResponse("答卷导出设置已更新");
        }
        else
        {
            return BadResponse("更新答卷导出设置失败");
        }
    }

    /// <summary>
    /// 替换水印文本中的变量
    /// </summary>
    /// <param name="template">模板文本</param>
    /// <param name="record">考试记录</param>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>替换后的文本</returns>
    private string ReplaceWatermarkVariables(string template, ExamPaperDetailDto record, ExamPaperDto examPaper)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = template
            .Replace("{StudentName}", record.StudentName ?? "未知学生")
            .Replace("{ExamName}", record.ExamName ?? "未知考试")
            .Replace("{DateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{StudentId}", record.StudentId.ToString())
            .Replace("{ExamRecordId}", record.ExamRecordId.ToString())
            .Replace("{IdNo}", record.IdNo ?? "未设置")
            .Replace("{AdmissionTicket}", record.AdmissionTicket ?? "未设置")
            .Replace("{Score}", record.TotalScore.ToString())
            .Replace("{TotalScore}", record.MaxScore.ToString())
            .Replace("{PageNumber}", "1") // PDF生成时会自动处理页码
            .Replace("{TotalPages}", "1"); // PDF生成时会自动处理总页数

        return result;
    }
}