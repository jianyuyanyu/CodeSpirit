namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 试卷类型
/// </summary>
public enum ExamPaperType
{
    /// <summary>
    /// 固定试卷（手动选题）
    /// </summary>
    Fixed = 1,
    
    /// <summary>
    /// 随机试卷（根据规则自动选题）
    /// </summary>
    Random = 2
}
