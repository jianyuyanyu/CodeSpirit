using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 成绩换算服务实现
/// </summary>
public class ScoreConversionService : IScoreConversionService
{
    private readonly ILogger<ScoreConversionService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ScoreConversionService(ILogger<ScoreConversionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 计算换算比例
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <returns>换算比例</returns>
    public decimal CalculateConversionRatio(int originalFullScore, int targetFullScore)
    {
        if (originalFullScore <= 0)
            throw new BusinessException("原始满分必须大于0");
        
        if (targetFullScore <= 0)
            throw new BusinessException("目标满分必须大于0");

        return (decimal)targetFullScore / originalFullScore;
    }

    /// <summary>
    /// 换算成绩
    /// </summary>
    /// <param name="originalScore">原始成绩</param>
    /// <param name="conversionRatio">换算比例</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算后成绩</returns>
    public decimal ConvertScore(double originalScore, decimal conversionRatio, int decimalPlaces = 1)
    {
        if (originalScore < 0)
            throw new BusinessException("原始成绩不能为负数");

        if (conversionRatio <= 0)
            throw new BusinessException("换算比例必须大于0");

        if (decimalPlaces < 0 || decimalPlaces > 2)
            throw new BusinessException("小数保留位数必须在0-2之间");

        var convertedScore = (decimal)originalScore * conversionRatio;
        return Math.Round(convertedScore, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 换算及格分
    /// </summary>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="conversionRatio">换算比例</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算后及格分</returns>
    public decimal ConvertPassScore(int originalPassScore, decimal conversionRatio, int decimalPlaces = 1)
    {
        if (originalPassScore < 0)
            throw new BusinessException("原始及格分不能为负数");

        return ConvertScore(originalPassScore, conversionRatio, decimalPlaces);
    }

    /// <summary>
    /// 批量换算考试记录成绩
    /// </summary>
    /// <param name="examRecords">考试记录列表</param>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>换算后的考试记录</returns>
    public async Task<List<ExamRecord>> BatchConvertExamRecordScoresAsync(List<ExamRecord> examRecords, ExamPaper examPaper)
    {
        if (examRecords == null || !examRecords.Any())
            return new List<ExamRecord>();

        if (!examPaper.EnableScoreConversion || !examPaper.ConversionTargetFullScore.HasValue)
        {
            _logger.LogWarning("试卷 {ExamPaperId} 未启用成绩换算或缺少换算配置", examPaper.Id);
            return examRecords;
        }

        var conversionRatio = CalculateConversionRatio(examPaper.TotalScore, examPaper.ConversionTargetFullScore.Value);
        
        foreach (var record in examRecords)
        {
            ConvertExamRecordScore(record, examPaper);
        }

        _logger.LogInformation("批量换算了 {Count} 条考试记录，换算比例：{Ratio}", 
            examRecords.Count, conversionRatio);

        return examRecords;
    }

    /// <summary>
    /// 单个考试记录成绩换算
    /// </summary>
    /// <param name="examRecord">考试记录</param>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>换算后的考试记录</returns>
    public ExamRecord ConvertExamRecordScore(ExamRecord examRecord, ExamPaper examPaper)
    {
        if (examRecord == null)
            throw new ArgumentNullException(nameof(examRecord));

        if (examPaper == null)
            throw new ArgumentNullException(nameof(examPaper));

        // 如果未启用换算或已经换算过，则跳过
        if (!examPaper.EnableScoreConversion || 
            !examPaper.ConversionTargetFullScore.HasValue ||
            examRecord.IsScoreConverted ||
            !examRecord.Score.HasValue)
        {
            return examRecord;
        }

        // 计算换算比例
        var conversionRatio = CalculateConversionRatio(examPaper.TotalScore, examPaper.ConversionTargetFullScore.Value);

        // 保存原始成绩
        examRecord.OriginalScore = examRecord.Score;
        
        // 换算成绩
        var convertedScore = ConvertScore(examRecord.Score.Value, conversionRatio, examPaper.ConversionDecimalPlaces);
        examRecord.Score = (double)convertedScore;

        // 标记已换算
        examRecord.IsScoreConverted = true;
        examRecord.ScoreConversionRatio = conversionRatio;

        // 重新计算是否通过（使用换算后的及格分）
        if (examPaper.OriginalPassScore.HasValue)
        {
            var convertedPassScore = ConvertPassScore(examPaper.OriginalPassScore.Value, conversionRatio, examPaper.ConversionDecimalPlaces);
            examRecord.IsPassed = examRecord.Score >= (double)convertedPassScore;
        }
        else
        {
            // 如果没有原始及格分，使用当前试卷的及格分（假设已经是换算后的）
            examRecord.IsPassed = examRecord.Score >= examPaper.PassScore;
        }

        _logger.LogDebug("考试记录 {RecordId} 成绩从 {OriginalScore} 换算为 {ConvertedScore}，比例：{Ratio}", 
            examRecord.Id, examRecord.OriginalScore, examRecord.Score, conversionRatio);

        return examRecord;
    }

    /// <summary>
    /// 验证换算配置
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>验证结果</returns>
    public (bool IsValid, string ErrorMessage) ValidateConversionConfiguration(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int decimalPlaces)
    {
        if (originalFullScore <= 0)
            return (false, "原始满分必须大于0");

        if (targetFullScore <= 0)
            return (false, "目标满分必须大于0");

        if (originalPassScore < 0)
            return (false, "原始及格分不能为负数");

        if (originalPassScore > originalFullScore)
            return (false, "原始及格分不能大于原始满分");

        if (decimalPlaces < 0 || decimalPlaces > 2)
            return (false, "小数保留位数必须在0-2之间");

        // 检查换算后的及格分是否合理
        var conversionRatio = CalculateConversionRatio(originalFullScore, targetFullScore);
        var convertedPassScore = ConvertPassScore(originalPassScore, conversionRatio, decimalPlaces);
        
        if (convertedPassScore > targetFullScore)
            return (false, $"换算后的及格分({convertedPassScore})超过了目标满分({targetFullScore})");

        return (true, string.Empty);
    }

    /// <summary>
    /// 生成换算说明
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="targetPassScore">目标及格分</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算说明</returns>
    public string GenerateConversionDescription(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int targetPassScore, 
        int decimalPlaces)
    {
        var ratio = CalculateConversionRatio(originalFullScore, targetFullScore);
        var decimalFormat = decimalPlaces switch
        {
            0 => "F0",
            1 => "F1", 
            2 => "F2",
            _ => "F1"
        };

        return $"成绩换算：{originalFullScore}分制 → {targetFullScore}分制，" +
               $"换算比例：{ratio:F4}，" +
               $"及格分：{originalPassScore} → {targetPassScore}，" +
               $"小数保留：{decimalPlaces}位。" +
               $"换算公式：换算后成绩 = 原始成绩 × {ratio:F4}（保留{decimalPlaces}位小数）";
    }

    /// <summary>
    /// 检查是否需要重新换算（试卷配置变更时）
    /// </summary>
    /// <param name="examPaper">试卷信息</param>
    /// <param name="existingRecords">现有考试记录</param>
    /// <returns>是否需要重新换算</returns>
    public bool RequiresReconversion(ExamPaper examPaper, List<ExamRecord> existingRecords)
    {
        if (!examPaper.EnableScoreConversion || !examPaper.ConversionTargetFullScore.HasValue)
            return false;

        if (!existingRecords.Any())
            return false;

        var currentRatio = CalculateConversionRatio(examPaper.TotalScore, examPaper.ConversionTargetFullScore.Value);

        // 检查是否有记录使用了不同的换算比例
        return existingRecords.Any(r => r.IsScoreConverted && 
                                       r.ScoreConversionRatio.HasValue && 
                                       Math.Abs(r.ScoreConversionRatio.Value - currentRatio) > 0.0001m);
    }
}