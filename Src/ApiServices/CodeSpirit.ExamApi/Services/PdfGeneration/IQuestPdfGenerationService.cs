using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.Services.PdfGeneration;

/// <summary>
/// QuestPDF 生成服务接口
/// </summary>
public interface IQuestPdfGenerationService
{
    /// <summary>
    /// 生成考试答卷 PDF
    /// </summary>
    /// <param name="record">考试记录详情</param>
    /// <param name="examPaper">试卷信息</param>
    /// <param name="settings">导出设置</param>
    /// <returns>PDF 字节数组</returns>
    Task<byte[]> GenerateExamPaperPdfAsync(
        ExamPaperDetailDto record, 
        ExamPaperDto examPaper, 
        ExamPaperExportSettingsDto settings);

    /// <summary>
    /// 批量生成考试答卷 PDF
    /// </summary>
    /// <param name="records">考试记录详情列表</param>
    /// <param name="examPapers">试卷信息字典（Key: ExamPaperId）</param>
    /// <param name="settings">导出设置</param>
    /// <returns>PDF 字节数组列表</returns>
    Task<IList<byte[]>> GenerateExamPaperPdfBatchAsync(
        IEnumerable<ExamPaperDetailDto> records,
        Dictionary<long, ExamPaperDto> examPapers,
        ExamPaperExportSettingsDto settings);
}

