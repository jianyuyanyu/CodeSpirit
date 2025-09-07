using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Shared.Services;
using CodeSpirit.SurveyApi.Dtos.Response;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷回答服务接口
/// </summary>
public interface IResponseService
{
    /// <summary>
    /// 根据查询条件获取回答分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页结果</returns>
    Task<PageList<ResponseDto>> GetPagedResponsesAsync(ResponseQueryDto queryDto);

    /// <summary>
    /// 根据ID获取回答详情
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>回答详情</returns>
    Task<ResponseDetailDto?> GetResponseDetailAsync(int id);

    /// <summary>
    /// 根据问卷ID获取回答列表
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="includeAnswers">是否包含答案详情</param>
    /// <returns>回答列表</returns>
    Task<List<ResponseDto>> GetResponsesBySurveyIdAsync(int surveyId, bool includeAnswers = false);

    /// <summary>
    /// 获取问卷回答统计信息
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>统计信息</returns>
    Task<ResponseStatisticsDto?> GetResponseStatisticsAsync(int surveyId);

    /// <summary>
    /// 批量删除回答
    /// </summary>
    /// <param name="ids">回答ID列表</param>
    /// <returns>删除结果</returns>
    Task<bool> BatchDeleteResponsesAsync(List<int> ids);

    /// <summary>
    /// 根据问卷ID删除所有回答
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteResponsesBySurveyIdAsync(int surveyId);

    /// <summary>
    /// 导出问卷回答数据
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="format">导出格式（excel、csv）</param>
    /// <returns>导出文件字节数组</returns>
    Task<byte[]> ExportResponsesAsync(int surveyId, string format = "excel");

    /// <summary>
    /// 检查回答是否存在
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>是否存在</returns>
    Task<bool> CheckResponseExistsAsync(int surveyId, string sessionId);

    /// <summary>
    /// 获取用户的回答历史
    /// </summary>
    /// <param name="respondentId">答题者ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>回答历史分页结果</returns>
    Task<PageList<ResponseDto>> GetUserResponseHistoryAsync(string respondentId, int pageIndex = 1, int pageSize = 20);

    /// <summary>
    /// 标记回答为已放弃
    /// </summary>
    /// <param name="id">回答ID</param>
    /// <returns>操作结果</returns>
    Task<bool> MarkAsAbandonedAsync(int id);

    /// <summary>
    /// 获取回答的答案详情
    /// </summary>
    /// <param name="responseId">回答ID</param>
    /// <returns>答案详情列表</returns>
    Task<List<ResponseAnswerDto>> GetResponseAnswersAsync(int responseId);

    /// <summary>
    /// 获取问卷的所有回答统计（按日期分组）
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="days">统计天数</param>
    /// <returns>按日期分组的统计数据</returns>
    Task<Dictionary<string, int>> GetResponseTrendAsync(int surveyId, int days = 30);
}
