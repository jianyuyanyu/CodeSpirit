using CodeSpirit.Charts;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Controllers;

/// <summary>
/// 考试统计控制器
/// </summary>
[DisplayName("考试统计")]
[Navigation(Icon = "fa-solid fa-chart-pie")]
public class ExamStatisticsController : ApiControllerBase
{
    private readonly IExamStatisticsService _examStatisticsService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examStatisticsService">考试统计服务</param>
    public ExamStatisticsController(
        IExamStatisticsService examStatisticsService)
    {
        _examStatisticsService = examStatisticsService;
    }

    /// <summary>
    /// 获取考试成绩统计
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="dateRange">日期范围</param>
    /// <returns>图表配置</returns>
    [HttpGet("score-statistics")]
    [Display(Name = "考试成绩统计")]
    [Chart("line", Title = "考试成绩统计", Description = "展示考试成绩的各项统计指标")]
    [ChartData(XField = "Category", YField = "Value")]
    [DisplayName("获取考试成绩统计")]
    public async Task<IActionResult> GetScoreStatisticsAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange)
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : null;
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : null;

        var statistics = await _examStatisticsService.GetScoreStatisticsAsync(examSettingId, startDate, endDate);
        return await this.AutoChartResult(statistics);
    }

    /// <summary>
    /// 获取考试及格率分析
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="dateRange">日期范围</param>
    /// <param name="groupBy">分组方式: Day, Week, Month, Year</param>
    /// <returns>图表配置</returns>
    [HttpGet("pass-rate")]
    [Display(Name = "及格率分析")]
    [Chart("line", Title = "及格率分析", Description = "展示考试及格率随时间的变化趋势")]
    [ChartData(XField = "TimePeriod", YField = "PassRate")]
    [DisplayName("获取考试及格率分析")]
    public async Task<IActionResult> GetPassRateAnalysisAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange,
        [FromQuery] string groupBy = "Day")
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now;

        var passRateData = await _examStatisticsService.GetPassRateAnalysisAsync(examSettingId, startDate, endDate, groupBy);
        return await this.AutoChartResult(passRateData);
    }

    /// <summary>
    /// 获取分数段分布
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="dateRange">日期范围</param>
    /// <param name="segments">分数段数量</param>
    /// <returns>图表配置</returns>
    [HttpGet("score-distribution")]
    [Display(Name = "分数段分布")]
    [Chart("bar", Title = "分数段分布", Description = "展示考试成绩在不同分数段的分布情况")]
    [ChartData(XField = "ScoreRange", YField = "Count")]
    [DisplayName("获取分数段分布")]
    public async Task<IActionResult> GetScoreDistributionAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange,
        [FromQuery] int segments = 10)
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : null;
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : null;

        var distributionData = await _examStatisticsService.GetScoreDistributionAsync(examSettingId, startDate, endDate, segments);
        return await this.AutoChartResult(distributionData);
    }

    /// <summary>
    /// 获取题目正确率分析
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>图表配置</returns>
    [HttpGet("question-correct-rate")]
    [Display(Name = "题目正确率分析")]
    [Chart("bar", Title = "题目正确率分析", Description = "展示题目的正确率排名")]
    [ChartData(XField = "QuestionTitle", YField = "CorrectRate")]
    [DisplayName("获取题目正确率分析")]
    public async Task<IActionResult> GetQuestionCorrectRateAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] int? questionType,
        [FromQuery] int topCount = 10)
    {
        var correctRateData = await _examStatisticsService.GetQuestionCorrectRateAsync(examSettingId, questionType, topCount);
        return await this.AutoChartResult(correctRateData);
    }

    /// <summary>
    /// 获取错题分析
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="questionType">题目类型</param>
    /// <param name="topCount">获取数量</param>
    /// <returns>图表配置</returns>
    [HttpGet("wrong-question-analysis")]
    [Display(Name = "错题分析")]
    [Chart("bar", Title = "错题分析", Description = "展示错题频率排名")]
    [ChartData(XField = "QuestionTitle", YField = "WrongCount")]
    [DisplayName("获取错题分析")]
    public async Task<IActionResult> GetWrongQuestionAnalysisAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] int? questionType,
        [FromQuery] int topCount = 10)
    {
        var wrongQuestionData = await _examStatisticsService.GetWrongQuestionAnalysisAsync(examSettingId, questionType, topCount);
        return await this.AutoChartResult(wrongQuestionData);
    }
} 