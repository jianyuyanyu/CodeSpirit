using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Dtos.ExamRecord;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 答题日志服务接口
/// </summary>
public interface IExamAnswerLogService : IScopedDependency
{
    /// <summary>
    /// 分页查询答题日志
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页结果</returns>
    Task<PageList<ExamAnswerLogDto>> GetPagedListAsync(ExamAnswerLogQueryDto queryDto);
}
