/// <summary>
/// 题目服务接口
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// 获取题目分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>题目分页列表</returns>
    Task<PageList<QuestionDto>> GetQuestionsAsync(QuestionQueryDto queryDto);

    /// <summary>
    /// 获取题目详情
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <returns>题目详情</returns>
    Task<QuestionDto> GetQuestionAsync(long id);

    /// <summary>
    /// 创建题目
    /// </summary>
    /// <param name="createDto">创建题目DTO</param>
    /// <returns>创建的题目</returns>
    Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto createDto);

    /// <summary>
    /// 更新题目
    /// </summary>
    /// <param name="id">题目ID</param>
    /// <param name="updateDto">更新题目DTO</param>
    Task UpdateQuestionAsync(long id, UpdateQuestionDto updateDto);

    /// <summary>
    /// 删除题目
    /// </summary>
    /// <param name="id">题目ID</param>
    Task DeleteQuestionAsync(long id);

    /// <summary>
    /// 获取题目历史版本
    /// </summary>
    /// <param name="questionId">题目ID</param>
    /// <returns>题目版本列表</returns>
    Task<List<QuestionVersionDto>> GetQuestionVersionsAsync(long questionId);
} 