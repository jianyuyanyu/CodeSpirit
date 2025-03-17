namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 题目类型
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 单选题
    /// </summary>
    SingleChoice = 1,
    
    /// <summary>
    /// 多选题
    /// </summary>
    MultipleChoice = 2,
    
    /// <summary>
    /// 判断题
    /// </summary>
    TrueFalse = 3
}
