using CodeSpirit.ExamApi.Dtos.Question;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 题目验证和自动修复服务接口
/// </summary>
public interface IQuestionValidationService
{
    /// <summary>
    /// 验证和修复单个题目
    /// </summary>
    /// <param name="question">待验证的题目</param>
    /// <returns>验证和修复后的题目</returns>
    Task<CreateQuestionDto> ValidateAndFixQuestionAsync(CreateQuestionDto question);

    /// <summary>
    /// 批量验证和修复题目
    /// </summary>
    /// <param name="questions">待验证的题目列表</param>
    /// <returns>验证和修复后的题目列表</returns>
    Task<List<CreateQuestionDto>> ValidateAndFixQuestionsAsync(List<CreateQuestionDto> questions);

    /// <summary>
    /// 验证题目格式
    /// </summary>
    /// <param name="question">待验证的题目</param>
    /// <returns>验证结果</returns>
    QuestionValidationResult ValidateQuestion(CreateQuestionDto question);

    /// <summary>
    /// 自动修复题目
    /// </summary>
    /// <param name="question">待修复的题目</param>
    /// <param name="validationResult">验证结果</param>
    /// <returns>修复后的题目</returns>
    CreateQuestionDto FixQuestion(CreateQuestionDto question, QuestionValidationResult validationResult);
}

/// <summary>
/// 题目验证结果
/// </summary>
public class QuestionValidationResult
{
    /// <summary>
    /// 是否有错误
    /// </summary>
    public bool HasErrors { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 修复建议列表
    /// </summary>
    public List<string> FixSuggestions { get; set; } = new();

    /// <summary>
    /// 是否需要修复答案格式（ABCD转选项文本）
    /// </summary>
    public bool NeedsAnswerFormatFix { get; set; }

    /// <summary>
    /// 是否需要清理选项序号
    /// </summary>
    public bool NeedsOptionCleanup { get; set; }

    /// <summary>
    /// 是否需要验证答案匹配性
    /// </summary>
    public bool NeedsAnswerValidation { get; set; }
}
