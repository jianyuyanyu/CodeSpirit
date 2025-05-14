using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.Shared.Services.Background;
using CodeSpirit.Shared.Services.Background.Dtos;
using CodeSpirit.Shared.Services.Files;
using Magicodes.ExporterAndImporter.Pdf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
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
    private readonly IBackgroundJobService _backgroundJobService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examRecordService">考试记录服务</param>
    /// <param name="examPaperService">试卷服务</param>
    /// <param name="backgroundJobService">后台任务服务</param>
    public ExamRecordsController(
        IExamRecordService examRecordService,
        IExamPaperService examPaperService,
        IBackgroundJobService backgroundJobService)
    {
        _examRecordService = examRecordService;
        _examPaperService = examPaperService;
        _backgroundJobService = backgroundJobService;
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
            try
            {
                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"exam_export_{taskId}");
                Directory.CreateDirectory(tempDir);
                var zipPath = Path.Combine(tempDir, fileName);

                int processedCount = 0;

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var recordId in dto.Ids)
                    {
                        if (cancellationToken.IsCancellationRequested)
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

                                // 获取考试记录详情
                                var record = await recordExamService.GetStudentExamPaperDetailAsync(recordId);
                                if (record == null) continue;

                                var examPaper = await recordExamPaperService.GetAsync(record.ExamPaperId);
                                if (examPaper == null) continue;

                                // 生成PDF字节数组
                                var pdfBytes = await GeneratePdfFromHtml(record, examPaper);

                                // 将PDF添加到ZIP
                                var studentName = record.StudentName ?? "未知学生";
                                var examName = record.ExamName ?? "未知考试";
                                var entryName = $"{examName}_{studentName}_{record.ExamRecordId}.pdf";

                                // 添加到ZIP文件
                                var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                {
                                    await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length, cancellationToken);
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
            catch (Exception ex)
            {
                // 记录详细错误日志
                var logger = scope.ServiceProvider.GetService<ILogger<ExamRecordsController>>();
                logger?.LogError(ex, "批量导出PDF任务失败: {Message}", ex.Message);

                // 更新任务状态为失败
                await UpdateExportTaskStatus(taskId, $"失败: {ex.Message}", 0, 0, null, scopedFileService);
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
    /// 生成试卷PDF
    /// </summary>
    private async Task<byte[]> GeneratePdfFromHtml(ExamPaperDetailDto record, ExamPaperDto examPaper)
    {
        // 生成HTML内容
        string htmlContent = await GenerateHtmlContent(record, examPaper);
        var pdfExporter = new PdfExporter();
        var result = await pdfExporter.ExportBytesByTemplate(record, new PdfExporterAttribute
        {
            PaperKind = WkHtmlToPdfDotNet.PaperKind.A4,
            Orientation = WkHtmlToPdfDotNet.Orientation.Portrait,
            IsEnablePagesCount = true,
        },
        htmlContent);

        return result;
    }

    /// <summary>
    /// 将HTML转换为PDF
    /// </summary>
    private async Task<string> GenerateHtmlContent(ExamPaperDetailDto record, ExamPaperDto examPaper)
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
        htmlBuilder.AppendLine("    .header { text-align: center; margin-bottom: 20px; }");
        htmlBuilder.AppendLine("    .title { font-size: 18px; font-weight: bold; }");
        htmlBuilder.AppendLine("    .info { font-size: 14px; margin-top: 5px; }");
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
        htmlBuilder.AppendLine("  </style>");
        htmlBuilder.AppendLine("</head>");
        htmlBuilder.AppendLine("<body>");

        // 页面头部
        htmlBuilder.AppendLine("  <div class=\"header\">");
        htmlBuilder.AppendLine($"    <div class=\"title\">{examPaper.Name ?? "考试答卷"}</div>");
        htmlBuilder.AppendLine("    <div class=\"info\">");

        // 获取学生信息
        var studentName = record.StudentName ?? "未知学生";

        htmlBuilder.AppendLine($"      学生: {studentName} | ");
        htmlBuilder.AppendLine($"      得分: {record.TotalScore}/{examPaper.TotalScore} | ");

        var submitTime = record.SubmitTime;

        htmlBuilder.AppendLine($"      提交时间: {submitTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未提交"}");
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
}