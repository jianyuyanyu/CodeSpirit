using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 试卷服务接口
/// </summary>
public interface IExamPaperService : IBaseCRUDService<ExamPaper, ExamPaperDto, long, CreateExamPaperDto, UpdateExamPaperDto>, IScopedDependency
{
    /// <summary>
    /// 发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    Task PublishExamPaperAsync(long id);

    /// <summary>
    /// 取消发布试卷
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    Task UnpublishExamPaperAsync(long id);

    /// <summary>
    /// 根据规则随机生成试卷
    /// </summary>
    /// <param name="createDto">随机试卷创建DTO</param>
    /// <returns>生成的试卷</returns>
    Task<ExamPaperDto> GenerateRandomExamPaperAsync(GenerateRandomExamPaperDto createDto);

    /// <summary>
    /// 复制试卷
    /// </summary>
    /// <param name="id">源试卷ID</param>
    /// <returns>复制后的试卷</returns>
    Task<ExamPaperDto> CopyExamPaperAsync(long id);

    /// <summary>
    /// 获取试卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>试卷分页列表</returns>
    Task<PageList<ExamPaperDto>> GetExamPapersAsync(ExamPaperQueryDto queryDto);

    /// <summary>
    /// 获取指定状态的所有试卷
    /// </summary>
    /// <param name="examPaperStatus">试卷状态</param>
    /// <returns>试卷列表</returns>
    Task<IEnumerable<ExamPaperDto>> GetAllExamPapersByStatusAsync(ExamPaperStatus examPaperStatus = ExamPaperStatus.Published);

    /// <summary>
    /// 标记试卷已预览
    /// </summary>
    /// <param name="id">试卷ID</param>
    /// <returns>操作结果</returns>
    Task MarkPreviewedAsync(long id);

    /// <summary>
    /// 获取试卷的题目列表
    /// </summary>
    /// <param name="examPaperId">试卷ID</param>
    /// <returns>题目列表</returns>
    Task<List<ExamPaperQuestionDto>> GetExamPaperQuestionsAsync(long examPaperId);
}