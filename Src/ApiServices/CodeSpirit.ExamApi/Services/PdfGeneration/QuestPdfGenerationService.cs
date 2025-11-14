using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace CodeSpirit.ExamApi.Services.PdfGeneration;

/// <summary>
/// QuestPDF 生成服务实现
/// </summary>
public class QuestPdfGenerationService : IQuestPdfGenerationService
{
    private readonly ILogger<QuestPdfGenerationService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public QuestPdfGenerationService(ILogger<QuestPdfGenerationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成考试答卷 PDF
    /// </summary>
    public async Task<byte[]> GenerateExamPaperPdfAsync(
        ExamPaperDetailDto record,
        ExamPaperDto examPaper,
        ExamPaperExportSettingsDto settings)
    {
        try
        {
            _logger.LogDebug("开始生成 PDF，学生：{StudentName}，考试：{ExamName}", 
                record.StudentName, record.ExamName);

            // 处理所有文本中的变量替换
            var watermarkText = ReplaceVariables(settings.WatermarkText, record, examPaper);
            var headerText = ReplaceVariables(settings.HeaderText, record, examPaper);
            var footerText = ReplaceFooterVariables(settings.FooterText, record, examPaper);
            var qrCodeContent = ReplaceVariables(settings.QrCodeContent, record, examPaper);

            // 创建文档
            var document = new ExamPaperDocument(record, examPaper, settings, watermarkText, headerText, footerText, qrCodeContent);

            // 生成 PDF
            var pdfBytes = await Task.Run(() => document.GeneratePdf());

            _logger.LogInformation(
                "成功生成 PDF，学生：{StudentName}，考试：{ExamName}，大小：{Size:N0} 字节",
                record.StudentName,
                record.ExamName,
                pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "生成 PDF 失败，学生：{StudentName}，考试：{ExamName}",
                record.StudentName,
                record.ExamName);
            throw;
        }
    }

    /// <summary>
    /// 批量生成考试答卷 PDF
    /// </summary>
    public async Task<IList<byte[]>> GenerateExamPaperPdfBatchAsync(
        IEnumerable<ExamPaperDetailDto> records,
        Dictionary<long, ExamPaperDto> examPapers,
        ExamPaperExportSettingsDto settings)
    {
        var tasks = records.Select(async record =>
        {
            if (!examPapers.TryGetValue(record.ExamPaperId, out var examPaper))
            {
                _logger.LogWarning("找不到试卷信息，试卷ID：{ExamPaperId}", record.ExamPaperId);
                return Array.Empty<byte>();
            }

            return await GenerateExamPaperPdfAsync(record, examPaper, settings);
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.Length > 0).ToList();
    }

    /// <summary>
    /// 替换变量
    /// </summary>
    private string ReplaceVariables(string template, ExamPaperDetailDto record, ExamPaperDto examPaper)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return template
            .Replace("{StudentName}", record.StudentName ?? "未知学生")
            .Replace("{ExamName}", record.ExamName ?? "未知考试")
            .Replace("{DateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{StudentId}", record.StudentId.ToString())
            .Replace("{ExamRecordId}", record.ExamRecordId.ToString())
            .Replace("{IdNo}", record.IdNo ?? "未设置")
            .Replace("{AdmissionTicket}", record.AdmissionTicket ?? "未设置")
            .Replace("{Score}", record.TotalScore.ToString())
            .Replace("{TotalScore}", record.MaxScore.ToString());
    }

    /// <summary>
    /// 替换页脚变量（支持页码替换）
    /// </summary>
    /// <remarks>
    /// {PageNumber} 和 {TotalPages} 会在页脚渲染时动态替换
    /// </remarks>
    private string ReplaceFooterVariables(string template, ExamPaperDetailDto record, ExamPaperDto examPaper)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        // 替换除页码外的其他变量
        var result = template
            .Replace("{StudentName}", record.StudentName ?? "未知学生")
            .Replace("{ExamName}", record.ExamName ?? "未知考试")
            .Replace("{DateTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{StudentId}", record.StudentId.ToString())
            .Replace("{ExamRecordId}", record.ExamRecordId.ToString())
            .Replace("{IdNo}", record.IdNo ?? "未设置")
            .Replace("{AdmissionTicket}", record.AdmissionTicket ?? "未设置")
            .Replace("{Score}", record.TotalScore.ToString())
            .Replace("{TotalScore}", record.MaxScore.ToString());
        
        // 保留 {PageNumber} 和 {TotalPages}，在页脚组件中动态处理
        return result;
    }
}


