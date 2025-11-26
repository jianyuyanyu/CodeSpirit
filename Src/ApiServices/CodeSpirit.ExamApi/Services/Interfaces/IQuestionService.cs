using CodeSpirit.ExamApi.Dtos.Question;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Dtos.QuestionVersion;
using CodeSpirit.Core.Dtos;

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
    /// 获取题目选项列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns></returns>
    Task<List<QuestionSelectListDto>> GetQuestionSelectListAsync(QuestionSelectListQueryDto queryDto);

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

    Task<(int successCount, List<long> failedIds)> BatchDeleteAsync(IEnumerable<long> ids);
    Task<(int successCount, List<string> failedIds)> BatchImportAsync(IEnumerable<QuestionBatchImportItemDto> importData);
    
    /// <summary>
    /// 文本识别导入题目
    /// </summary>
    /// <returns>导入结果，包含成功数量和失败项</returns>
    Task<(int successCount, List<string> failedItems)> ImportFromTextAsync(QuestionImportFromTextDto input);
    
    /// <summary>
    /// 获取题目设置
    /// </summary>
    /// <returns>题目设置</returns>
    Task<QuestionSettingsDto> GetQuestionSettingsAsync();

    /// <summary>
    /// 更新题目设置
    /// </summary>
    /// <param name="settings">题目设置</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateQuestionSettingsAsync(QuestionSettingsDto settings);

        /// <summary>
        /// 步骤1：解析文本并进行AI审核
        /// </summary>
        /// <param name="input">解析配置</param>
        /// <returns>审核结果</returns>
        Task<QuestionBatchPreviewResponseDto> ParseQuestionsFromTextAsync(QuestionImportStepDto input);

        /// <summary>
        /// 步骤2：获取题目预览数据
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns>预览数据</returns>
        Task<QuestionBatchPreviewResponseDto> GetQuestionPreviewAsync(string sessionId);

        /// <summary>
        /// 步骤3：保存用户编辑的题目
        /// </summary>
        /// <param name="input">编辑数据</param>
        /// <returns>任务</returns>
        Task SaveQuestionEditsAsync(QuestionBatchPreviewResponseDto input);

        /// <summary>
        /// 步骤4：确认导入题目
        /// </summary>
        /// <param name="input">导入确认数据</param>
        /// <returns>导入结果</returns>
        Task<ImportResultDto> ImportQuestionsAsync(QuestionBatchImportConfirmDto input);

    /// <summary>
    /// 获取所有标签列表（去重）
    /// </summary>
    /// <returns>标签列表</returns>
    Task<List<OptionDto<string>>> GetAllTagsAsync();
} 