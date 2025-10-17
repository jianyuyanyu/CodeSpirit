using CodeSpirit.ExamApi.Dtos.ExamPaper;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 试卷检查服务接口
/// </summary>
public interface IExamPaperCheckService
{
    /// <summary>
    /// 验证试卷和题目
    /// </summary>
    /// <param name="examPaper">试卷DTO</param>
    /// <returns>验证结果</returns>
    ExamPaperCheckResult ValidateExamPaper(ExamPaperDto examPaper);
}

