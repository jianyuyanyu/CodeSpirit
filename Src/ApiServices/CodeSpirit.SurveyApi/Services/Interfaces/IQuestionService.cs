using CodeSpirit.SurveyApi.Dtos.Question;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 题目服务接口
/// </summary>
public interface IQuestionService : IBaseCRUDService<Question, QuestionDto, int, CreateQuestionDto, UpdateQuestionDto>
{
    /// <summary>
    /// 根据问卷ID获取题目列表
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <returns>题目列表</returns>
    Task<List<QuestionDto>> GetQuestionsBySurveyIdAsync(int surveyId);

    /// <summary>
    /// 获取题目分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目分页列表</returns>
    Task<PageList<QuestionDto>> GetQuestionsAsync(QuestionQueryDto queryDto);

    /// <summary>
    /// 批量排序题目
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="questionOrders">题目排序信息</param>
    /// <returns>异步任务</returns>
    Task ReorderQuestionsAsync(int surveyId, Dictionary<int, int> questionOrders);

    /// <summary>
    /// 复制题目到指定问卷
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetSurveyId">目标问卷ID</param>
    /// <returns>复制的题目</returns>
    Task<QuestionDto> CopyQuestionToSurveyAsync(int questionId, int targetSurveyId);

    /// <summary>
    /// 复制题目到指定问卷（带自定义标题）
    /// </summary>
    /// <param name="questionId">源题目ID</param>
    /// <param name="targetSurveyId">目标问卷ID</param>
    /// <param name="newTitle">新标题</param>
    /// <returns>复制的题目</returns>
    Task<QuestionDto> CopyQuestionToSurveyAsync(int questionId, int targetSurveyId, string? newTitle);
}
