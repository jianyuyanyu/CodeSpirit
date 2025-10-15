namespace CodeSpirit.ExamApi.Constants;

/// <summary>
/// 缓存键常量
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// 考试题目数据缓存键前缀（字典格式：QuestionId -> QuestionDto）
    /// 格式: Exam:QuestionsData:{examId}
    /// 说明: 缓存原始题目数据（未排序），不包含用户特定的题目顺序
    /// 用户的题目顺序保存在 AnswerRecord.OrderNumber 中
    /// </summary>
    public const string ExamQuestionsData = "Exam:QuestionsData:{0}";
    
    /// <summary>
    /// 考试基本信息缓存键前缀（不包含用户特定数据）
    /// 格式: Exam:BasicInfo:{examId}
    /// </summary>
    public const string ExamBasicInfo = "Exam:BasicInfo:{0}";
    
    /// <summary>
    /// 用户考试记录缓存键前缀
    /// 格式: Exam:UserRecord:{examId}:{userId}
    /// </summary>
    public const string UserExamRecord = "Exam:UserRecord:{0}:{1}";
    
    /// <summary>
    /// 用户答案缓存键前缀
    /// 格式: Exam:UserAnswers:{recordId}
    /// </summary>
    public const string UserAnswers = "Exam:UserAnswers:{0}";
    
    /// <summary>
    /// 生成考试题目数据缓存键
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>缓存键</returns>
    public static string GetExamQuestionsDataKey(long examId) 
        => string.Format(ExamQuestionsData, examId);
    
    /// <summary>
    /// 生成考试基本信息缓存键
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>缓存键</returns>
    public static string GetExamBasicInfoKey(long examId) 
        => string.Format(ExamBasicInfo, examId);
    
    /// <summary>
    /// 生成用户考试记录缓存键
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>缓存键</returns>
    public static string GetUserExamRecordKey(long examId, long userId) 
        => string.Format(UserExamRecord, examId, userId);
    
    /// <summary>
    /// 生成用户答案缓存键
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>缓存键</returns>
    public static string GetUserAnswersKey(long recordId) 
        => string.Format(UserAnswers, recordId);
}

