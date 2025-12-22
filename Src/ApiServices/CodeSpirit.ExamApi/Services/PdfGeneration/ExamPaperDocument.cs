using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Text;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace CodeSpirit.ExamApi.Services.PdfGeneration;

/// <summary>
/// 考试答卷 PDF 文档
/// </summary>
public class ExamPaperDocument : IDocument
{
    private readonly ExamPaperDetailDto _record;
    private readonly ExamPaperDto _examPaper;
    private readonly ExamPaperExportSettingsDto _settings;
    private readonly string _watermarkText;
    private readonly string _headerText;
    private readonly string _footerText;
    private readonly string _qrCodeContent;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamPaperDocument(
        ExamPaperDetailDto record,
        ExamPaperDto examPaper,
        ExamPaperExportSettingsDto settings,
        string watermarkText,
        string headerText,
        string footerText,
        string qrCodeContent)
    {
        _record = record;
        _examPaper = examPaper;
        _settings = settings;
        _watermarkText = watermarkText;
        _headerText = headerText;
        _footerText = footerText;
        _qrCodeContent = qrCodeContent;
    }

    /// <summary>
    /// 文档元数据
    /// </summary>
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    /// <summary>
    /// 文档设置
    /// </summary>
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    /// <summary>
    /// 生成文档内容
    /// </summary>
    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                // 页面设置
                ConfigurePageSize(page);
                
                // 水印背景层（应用于每一页）
                if (_settings.EnableWatermark && !string.IsNullOrEmpty(_watermarkText))
                {
                    page.Background().Element(ComposeWatermark);
                }
                
                // 页眉
                if (!string.IsNullOrEmpty(_settings.HeaderText))
                {
                    page.Header().Element(ComposeHeader);
                }

                // 页面内容
                page.Content().Element(ComposeContent);

                // 页脚
                if (!string.IsNullOrEmpty(_settings.FooterText))
                {
                    page.Footer().Element(ComposeFooter);
                }
            });
    }

    /// <summary>
    /// 配置页面大小和方向
    /// </summary>
    private void ConfigurePageSize(PageDescriptor page)
    {
        var pageSize = _settings.PageSize switch
        {
            PdfPageSize.A3 => PageSizes.A3,
            PdfPageSize.Letter => PageSizes.Letter,
            _ => PageSizes.A4
        };

        if (_settings.PageOrientation == PdfOrientation.Landscape)
        {
            page.Size(pageSize.Landscape());
        }
        else
        {
            page.Size(pageSize);
        }

        page.Margin(10, Unit.Millimetre);
        page.DefaultTextStyle(x => x
            .FontSize(10)
            .FontFamily(FontHelper.GetChineseFont())
            .Fallback(style => style.FontFamily(FontHelper.GetFallbackFont())));
    }

    /// <summary>
    /// 水印背景
    /// </summary>
    private void ComposeWatermark(IContainer container)
    {
        container
            .AlignCenter()
            .AlignMiddle()
            .Rotate(_settings.WatermarkRotation)
            .DefaultTextStyle(x => x
                .FontSize(_settings.WatermarkFontSize)
                .FontColor(ParseColor(_settings.WatermarkColor)))
            .Text(_watermarkText);
    }

    /// <summary>
    /// 页眉
    /// </summary>
    private void ComposeHeader(IContainer container)
    {
        container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Text(_headerText)
            .FontSize(9)
            .FontColor(Colors.Grey.Darken1)
            .AlignCenter();
    }

    /// <summary>
    /// 页脚（支持动态页码）
    /// </summary>
    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .AlignCenter()
            .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1))
            .Text(text =>
            {
                // 替换页码变量
                var footerText = _footerText
                    .Replace("{PageNumber}", "")
                    .Replace("{TotalPages}", "");
                
                // 处理页码显示
                if (_footerText.Contains("{PageNumber}") || _footerText.Contains("{TotalPages}"))
                {
                    // 如果包含页码变量，使用动态页码
                    var parts = _footerText.Split(new[] { "{PageNumber}", "{TotalPages}" }, StringSplitOptions.None);
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(parts[i]))
                        {
                            text.Span(parts[i]);
                        }
                        
                        // 在分隔处插入页码
                        if (i < parts.Length - 1)
                        {
                            if (_footerText.IndexOf("{PageNumber}", i * 20) >= 0 && _footerText.IndexOf("{PageNumber}", i * 20) < _footerText.IndexOf("{TotalPages}", i * 20))
                            {
                                text.CurrentPageNumber();
                            }
                            else
                            {
                                text.TotalPages();
                            }
                        }
                    }
                }
                else
                {
                    // 如果不包含页码变量，直接显示文本
                    text.Span(_footerText);
                }
            });
    }

    /// <summary>
    /// 主要内容
    /// </summary>
    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // 标题和基本信息
            column.Item().Element(ComposeTitle);

            // 成绩统计
            column.Item().Element(ComposeScoreSummary);

            // 题目内容
            column.Item().Element(ComposeQuestions);
        });
    }

    /// <summary>
    /// 标题和考生信息
    /// </summary>
    private void ComposeTitle(IContainer container)
    {
        container.Column(column =>
        {
            // 考试标题
            column.Item()
                .AlignCenter()
                .Text(_examPaper.Name ?? "考试答卷")
                .FontSize(18)
                .Bold();

            column.Item().PaddingVertical(5);

            // 考生信息表格
            column.Item().Row(row =>
            {
                // 左侧信息
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.Span("考生姓名：").Bold();
                        text.Span(_record.StudentName ?? "未知学生");
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("身份证号：").Bold();
                        text.Span(_record.IdNo ?? "未设置");
                    });

                    col.Item().Text(text =>
                    {
                        text.Span("准考证号：").Bold();
                        text.Span(_record.AdmissionTicket ?? "未设置");
                    });
                });

                // 右侧：照片或二维码
                if (_settings.ShowStudentPhoto || _settings.ShowQrCode)
                {
                    row.ConstantItem(80).Column(col =>
                    {
                        if (_settings.ShowQrCode)
                        {
                            col.Item().Element(container => ComposeQrCode(container));
                        }
                    });
                }
            });

            column.Item().PaddingVertical(3);

            // 考试信息
            column.Item().Text(text =>
            {
                text.Span("得分：").Bold();
                text.Span($"{_record.TotalScore}/{_examPaper.TotalScore}  ");
                
                text.Span("开始时间：").Bold();
                var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(_record.StartTime, TimeZoneInfo.Local);
                text.Span($"{localStartTime:yyyy-MM-dd HH:mm:ss}  ");
                
                text.Span("提交时间：").Bold();
                var localSubmitTime = _record.SubmitTime.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(_record.SubmitTime.Value, TimeZoneInfo.Local).ToString("yyyy-MM-dd HH:mm:ss")
                    : "未提交";
                text.Span(localSubmitTime);
            });
        });
    }

    /// <summary>
    /// 生成二维码
    /// </summary>
    private void ComposeQrCode(IContainer container)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(_qrCodeContent, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(5);
            
            container.Width(70).Height(70).Image(qrCodeImage);
        }
        catch
        {
            // 如果二维码生成失败，显示占位文本
            container.Width(70).Height(70)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .AlignCenter()
                .AlignMiddle()
                .Text("二维码")
                .FontSize(8);
        }
    }

    /// <summary>
    /// 成绩统计表
    /// </summary>
    private void ComposeScoreSummary(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("成绩统计").Bold().FontSize(12);
            column.Item().PaddingVertical(5);

            // 统计题型得分（使用答卷中的题目顺序）
            // 创建题目顺序字典
            var questionOrderDict = _record.Answers
                .ToDictionary(a => a.QuestionId, a => a.OrderNumber);
            
            // 创建题目信息字典
            var questionInfoDict = _record.Questions
                .ToDictionary(q => q.QuestionId, q => q);

            // 按照答卷的题目顺序排序题目列表
            var orderedQuestions = _record.Answers
                .OrderBy(a => a.OrderNumber)
                .Select(a => questionInfoDict.ContainsKey(a.QuestionId) ? questionInfoDict[a.QuestionId] : null)
                .Where(q => q != null)
                .ToList()!;

            var questionsByType = orderedQuestions
                .GroupBy(q => q.Type.ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // 表头
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                        .Text("题型").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                        .Text("题目数").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                        .Text("得分/总分").Bold();
                });

                // 数据行
                foreach (var type in questionsByType.Keys)
                {
                    var typeQuestions = questionsByType[type];
                    var typeAnswers = _record.Answers.Where(a => a.QuestionType == type).ToList();
                    double typeScore = typeAnswers.Sum(a => a.Score ?? 0);
                    int typeTotalScore = typeQuestions.Sum(q => q.Score);
                    string typeName = GetQuestionTypeName(type);

                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(typeName);
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(typeQuestions.Count.ToString()).AlignCenter();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text($"{typeScore}/{typeTotalScore}").AlignCenter();
                }

                // 总分行
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text("总分").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text(orderedQuestions.Count.ToString()).AlignCenter().Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                    .Text($"{_record.TotalScore}/{_examPaper.TotalScore}").AlignCenter().Bold();
            });
        });
    }

    /// <summary>
    /// 题目内容
    /// </summary>
    private void ComposeQuestions(IContainer container)
    {
        container.Column(column =>
        {
            // 使用答卷中的题目顺序（_record.Questions 已经按照 OrderNumber 排序）
            // 创建题目顺序字典，以QuestionId为键
            var questionOrderDict = _record.Answers
                .ToDictionary(a => a.QuestionId, a => a.OrderNumber);
            
            // 创建题目信息字典，以QuestionId为键
            var questionInfoDict = _record.Questions
                .ToDictionary(q => q.QuestionId, q => q);

            // 按照答卷的题目顺序排序题目列表
            var orderedQuestions = _record.Answers
                .OrderBy(a => a.OrderNumber)
                .Select(a => questionInfoDict.ContainsKey(a.QuestionId) ? questionInfoDict[a.QuestionId] : null)
                .Where(q => q != null)
                .ToList()!;

            // 按题型分组（使用排序后的题目列表）
            var questionsByType = orderedQuestions
                .GroupBy(q => q.Type.ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            int questionIndex = 1;

            foreach (var type in questionsByType.Keys)
            {
                string typeName = GetQuestionTypeName(type);
                // 按照答卷的题目顺序排序该类型的题目
                var typeQuestions = questionsByType[type]
                    .OrderBy(q => questionOrderDict.ContainsKey(q.QuestionId) ? questionOrderDict[q.QuestionId] : int.MaxValue)
                    .ToList();

                // 题型标题
                column.Item().PaddingTop(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                    .PaddingBottom(3)
                    .Text(typeName)
                    .Bold()
                    .FontSize(12);

                // 该题型下的所有题目（按答卷顺序）
                foreach (var question in typeQuestions)
                {
                    // 使用答卷中的顺序号
                    int orderNumber = questionOrderDict.ContainsKey(question.QuestionId) 
                        ? questionOrderDict[question.QuestionId] 
                        : questionIndex;
                    column.Item().PaddingTop(8).Element(c => ComposeQuestion(c, question, orderNumber));
                    questionIndex++;
                }
            }
        });
    }

    /// <summary>
    /// 单个题目
    /// </summary>
    private void ComposeQuestion(IContainer container, ExamPaperQuestionDto question, int index)
    {
        var answerInfo = _record.Answers.FirstOrDefault(x => x.QuestionId == question.QuestionId);
        var studentAnswer = answerInfo?.Answer ?? "";
        var correctAnswer = answerInfo?.CorrectAnswer ?? "";
        var score = answerInfo?.Score ?? 0;
        var isCorrect = answerInfo?.IsCorrect ?? false;
        var maxScore = question.Score;

        container.Column(column =>
        {
            // 题目标题
            column.Item().Row(row =>
            {
                row.RelativeItem().DefaultTextStyle(x => x.FontSize(10)).Text($"{index}. {question.Content}");

                row.ConstantItem(100).AlignRight().DefaultTextStyle(x => x.FontSize(9)).Text(text =>
                {
                    if (isCorrect)
                    {
                        text.Span("✓ ").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Green.Medium);
                    }
                    else
                    {
                        text.Span("✗ ").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Red.Medium);
                    }
                    text.Span($"{score}/{maxScore}分");
                });
            });

            // 题目内容
            column.Item().PaddingLeft(20).PaddingTop(5).Element(c =>
            {
                switch (question.Type.ToString())
                {
                    case "SingleChoice":
                        ComposeSingleChoice(c, question, studentAnswer, correctAnswer);
                        break;
                    case "MultipleChoice":
                        ComposeMultipleChoice(c, question, studentAnswer, correctAnswer);
                        break;
                    case "TrueFalse":
                        ComposeTrueFalse(c, studentAnswer, correctAnswer);
                        break;
                    default:
                        ComposeTextAnswer(c, studentAnswer, correctAnswer);
                        break;
                }
            });
        });
    }

    /// <summary>
    /// 单选题选项
    /// </summary>
    private void ComposeSingleChoice(IContainer container, ExamPaperQuestionDto question, string studentAnswer, string correctAnswer)
    {
        container.Column(column =>
        {
            foreach (var option in question.Options)
            {
                bool isSelected = option == studentAnswer;
                bool isCorrectOption = option == correctAnswer;

                column.Item().Text(text =>
                {
                    text.Span(isSelected ? "☑ " : "☐ ").FontFamily(FontHelper.GetSymbolFont());
                    text.Span(option);

                    if (isCorrectOption)
                    {
                        text.Span(" ✓").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Green.Medium);
                    }
                    else if (isSelected)
                    {
                        text.Span(" ✗").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Red.Medium);
                    }
                });
            }
        });
    }

    /// <summary>
    /// 多选题选项
    /// </summary>
    private void ComposeMultipleChoice(IContainer container, ExamPaperQuestionDto question, string studentAnswer, string correctAnswer)
    {
        var selectedOptions = studentAnswer.Split(',').Select(o => o.Trim()).ToArray();
        var correctOptions = correctAnswer.Split(',').Select(o => o.Trim()).ToArray();

        container.Column(column =>
        {
            foreach (var option in question.Options)
            {
                bool isSelected = selectedOptions.Contains(option);
                bool isCorrectOption = correctOptions.Contains(option);

                column.Item().Text(text =>
                {
                    text.Span(isSelected ? "☑ " : "☐ ").FontFamily(FontHelper.GetSymbolFont());
                    text.Span(option);

                    if (isCorrectOption)
                    {
                        text.Span(" ✓").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Green.Medium);
                    }
                    else if (isSelected)
                    {
                        text.Span(" ✗").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Red.Medium);
                    }
                });
            }
        });
    }

    /// <summary>
    /// 判断题选项
    /// </summary>
    private void ComposeTrueFalse(IContainer container, string studentAnswer, string correctAnswer)
    {
        bool isTrue = string.Equals(studentAnswer, "True", StringComparison.OrdinalIgnoreCase);
        bool correctIsTrue = string.Equals(correctAnswer, "True", StringComparison.OrdinalIgnoreCase);

        container.Column(column =>
        {
            column.Item().Text(text =>
            {
                text.Span(isTrue ? "☑ " : "☐ ").FontFamily(FontHelper.GetSymbolFont());
                text.Span("✓").FontFamily(FontHelper.GetSymbolFont());

                if (correctIsTrue)
                {
                    text.Span(" ✓").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Green.Medium);
                }
                // else if (isTrue)
                // {
                //     text.Span(" ✗").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Red.Medium);
                // }
            });

            column.Item().Text(text =>
            {
                text.Span(!isTrue ? "☑ " : "☐ ").FontFamily(FontHelper.GetSymbolFont());
                text.Span("✗").FontFamily(FontHelper.GetSymbolFont());

                if (!correctIsTrue)
                {
                    text.Span(" ✓").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Green.Medium);
                }
                // else if (!isTrue)
                // {
                //     text.Span(" ✗").FontFamily(FontHelper.GetSymbolFont()).FontColor(Colors.Red.Medium);
                // }
            });
        });
    }

    /// <summary>
    /// 文本答案（简答题、论述题等）
    /// </summary>
    private void ComposeTextAnswer(IContainer container, string studentAnswer, string correctAnswer)
    {
        container.Column(column =>
        {
            // 学生答案
            column.Item().Background(Colors.Blue.Lighten4).Padding(8).Column(col =>
            {
                col.Item().Text("学生答案：").Bold().FontSize(9);
                col.Item().PaddingTop(3).Text(string.IsNullOrEmpty(studentAnswer) ? "未作答" : studentAnswer)
                    .FontSize(9);
            });

            // 参考答案（根据设置显示）
            if (_settings.ShowScoreDetails)
            {
                column.Item().PaddingTop(5).Background(Colors.Green.Lighten4).Padding(8).Column(col =>
                {
                    col.Item().Text("参考答案：").Bold().FontSize(9);
                    col.Item().PaddingTop(3).Text(correctAnswer).FontSize(9);
                });
            }
        });
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
    /// 解析颜色
    /// </summary>
    private string ParseColor(string color)
    {
        // QuestPDF 支持十六进制颜色
        return color;
    }
}

