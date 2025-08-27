using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷服务接口
/// </summary>
public interface ISurveyService : IBaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>
{
    /// <summary>
    /// 获取问卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷分页列表</returns>
    Task<PageList<SurveyDto>> GetSurveysAsync(SurveyQueryDto queryDto);

    /// <summary>
    /// 发布问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    Task PublishSurveyAsync(int id);

    /// <summary>
    /// 关闭问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    Task CloseSurveyAsync(int id);

    /// <summary>
    /// 归档问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    Task ArchiveSurveyAsync(int id);

    /// <summary>
    /// 复制问卷
    /// </summary>
    /// <param name="id">源问卷ID</param>
    /// <param name="title">新问卷标题</param>
    /// <returns>新问卷</returns>
    Task<SurveyDto> CopySurveyAsync(int id, string title);

    /// <summary>
    /// 获取问卷统计信息
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>统计信息</returns>
    Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int id);

    /// <summary>
    /// 获取我的问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷列表</returns>
    Task<PageList<SurveyDto>> GetMySurveysAsync(SurveyQueryDto queryDto);

    /// <summary>
    /// 获取问卷模板列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>模板列表</returns>
    Task<PageList<SurveyDto>> GetSurveyTemplatesAsync(SurveyQueryDto queryDto);

    /// <summary>
    /// 从模板创建问卷
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="title">新问卷标题</param>
    /// <returns>新问卷</returns>
    Task<SurveyDto> CreateFromTemplateAsync(int templateId, string title);

    /// <summary>
    /// 标记问卷已预览
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    Task MarkPreviewedAsync(int id);

    /// <summary>
    /// 获取问卷选项列表（用于下拉选择）
    /// </summary>
    /// <returns>问卷选项列表</returns>
    Task<List<SurveyOptionDto>> GetSurveyOptionsAsync();
}
