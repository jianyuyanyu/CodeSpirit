using CodeSpirit.Core;
using System;

namespace CodeSpirit.ExamApi.Services.Interfaces;

/// <summary>
/// 考试统计服务接口
/// </summary>
public interface IExamStatisticsService
{
    /// <summary>
    /// 获取考试成绩统计数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>考试成绩统计数据</returns>
    Task<object> GetScoreStatisticsAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate);
    
    /// <summary>
    /// 获取考试及格率分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="groupBy">分组方式: Day, Week, Month, Year</param>
    /// <returns>及格率分析数据</returns>
    Task<object> GetPassRateAnalysisAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate, string groupBy = "Day");
    
    /// <summary>
    /// 获取分数段分布数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="segments">分数段数量</param>
    /// <returns>分数段分布数据</returns>
    Task<object> GetScoreDistributionAsync(long? examSettingId, DateTimeOffset? startDate, DateTimeOffset? endDate, int segments = 10);
    
    /// <summary>
    /// 获取题目正确率分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>题目正确率分析数据</returns>
    Task<object> GetQuestionCorrectRateAsync(long? examSettingId, int? questionType, int topCount = 10);
    
    /// <summary>
    /// 获取错题分析数据
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>错题分析数据</returns>
    Task<object> GetWrongQuestionAnalysisAsync(long? examSettingId, int? questionType, int topCount = 10);
} 