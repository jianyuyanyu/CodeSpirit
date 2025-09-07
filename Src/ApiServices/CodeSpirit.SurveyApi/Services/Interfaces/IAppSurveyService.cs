using CodeSpirit.SurveyApi.Dtos.App;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// App端问卷服务接口
/// </summary>
public interface IAppSurveyService
{
    /// <summary>
    /// 获取公开问卷列表
    /// </summary>
    /// <returns>公开问卷列表</returns>
    Task<List<AppSurveyDto>> GetPublicSurveysAsync();

    /// <summary>
    /// 根据ID获取问卷详情
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷详情</returns>
    Task<AppSurveyDetailDto?> GetSurveyAsync(int id);

    /// <summary>
    /// 检查问卷是否可以参与
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>检查结果</returns>
    Task<object?> CheckSurveyAvailabilityAsync(int id);

    /// <summary>
    /// 获取问卷分类选项
    /// </summary>
    /// <returns>分类选项列表</returns>
    Task<List<object>> GetSurveyCategoriesAsync();

    /// <summary>
    /// 计算预计完成时间（分钟）
    /// </summary>
    /// <param name="questionCount">题目数量</param>
    /// <returns>预计完成时间</returns>
    int CalculateEstimatedMinutes(int questionCount);

    /// <summary>
    /// 根据公开访问码获取问卷详情
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>问卷详情</returns>
    Task<AppSurveyDetailDto?> GetSurveyByAccessCodeAsync(string accessCode);

    /// <summary>
    /// 根据公开访问码检查问卷可用性
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>检查结果</returns>
    Task<object?> CheckSurveyAvailabilityByAccessCodeAsync(string accessCode);
}
