using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 成绩换算服务接口
/// </summary>
public interface IScoreConversionService
{
    /// <summary>
    /// 计算换算比例
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <returns>换算比例</returns>
    decimal CalculateConversionRatio(int originalFullScore, int targetFullScore);

    /// <summary>
    /// 换算成绩
    /// </summary>
    /// <param name="originalScore">原始成绩</param>
    /// <param name="conversionRatio">换算比例</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算后成绩</returns>
    decimal ConvertScore(double originalScore, decimal conversionRatio, int decimalPlaces = 1);

    /// <summary>
    /// 换算及格分
    /// </summary>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="conversionRatio">换算比例</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算后及格分</returns>
    decimal ConvertPassScore(int originalPassScore, decimal conversionRatio, int decimalPlaces = 1);

    /// <summary>
    /// 批量换算考试记录成绩
    /// </summary>
    /// <param name="examRecords">考试记录列表</param>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>换算后的考试记录</returns>
    Task<List<ExamRecord>> BatchConvertExamRecordScoresAsync(List<ExamRecord> examRecords, ExamPaper examPaper);

    /// <summary>
    /// 单个考试记录成绩换算
    /// </summary>
    /// <param name="examRecord">考试记录</param>
    /// <param name="examPaper">试卷信息</param>
    /// <returns>换算后的考试记录</returns>
    ExamRecord ConvertExamRecordScore(ExamRecord examRecord, ExamPaper examPaper);

    /// <summary>
    /// 验证换算配置
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>验证结果</returns>
    (bool IsValid, string ErrorMessage) ValidateConversionConfiguration(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int decimalPlaces);

    /// <summary>
    /// 生成换算说明
    /// </summary>
    /// <param name="originalFullScore">原始满分</param>
    /// <param name="targetFullScore">目标满分</param>
    /// <param name="originalPassScore">原始及格分</param>
    /// <param name="targetPassScore">目标及格分</param>
    /// <param name="decimalPlaces">小数保留位数</param>
    /// <returns>换算说明</returns>
    string GenerateConversionDescription(
        int originalFullScore, 
        int targetFullScore, 
        int originalPassScore, 
        int targetPassScore, 
        int decimalPlaces);

    /// <summary>
    /// 检查是否需要重新换算（试卷配置变更时）
    /// </summary>
    /// <param name="examPaper">试卷信息</param>
    /// <param name="existingRecords">现有考试记录</param>
    /// <returns>是否需要重新换算</returns>
    bool RequiresReconversion(ExamPaper examPaper, List<ExamRecord> existingRecords);
}