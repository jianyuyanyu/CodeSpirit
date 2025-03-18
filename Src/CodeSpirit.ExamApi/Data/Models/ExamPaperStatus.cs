namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 试卷状态
/// </summary>
public enum ExamPaperStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft = 1,
    
    /// <summary>
    /// 已发布
    /// </summary>
    Published = 2,
    
    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 3
} 