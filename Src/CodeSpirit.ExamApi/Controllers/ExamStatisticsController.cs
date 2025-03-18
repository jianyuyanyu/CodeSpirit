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
    private readonly IChartService _chartService;
    private readonly IEChartConfigGenerator _eChartConfigGenerator;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="examStatisticsService">考试统计服务</param>
    /// <param name="chartService">图表服务</param>
    /// <param name="eChartConfigGenerator">EChart配置生成器</param>
    public ExamStatisticsController(
        IExamStatisticsService examStatisticsService,
        IChartService chartService,
        IEChartConfigGenerator eChartConfigGenerator)
    {
        _examStatisticsService = examStatisticsService;
        _chartService = chartService;
        _eChartConfigGenerator = eChartConfigGenerator;
    }

    /// <summary>
    /// 获取考试成绩统计
    /// </summary>
    /// <param name="examSettingId">考试设置ID</param>
    /// <param name="dateRange">日期范围</param>
    /// <returns>图表配置</returns>
    [HttpGet("score-statistics")]
    [Display(Name = "考试成绩统计")]
    [Chart("考试成绩统计", "展示考试成绩的各项统计指标")]
    [ChartType(ChartType.Card)]
    public async Task<IActionResult> GetScoreStatisticsAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange)
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : null;
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : null;

        var statistics = await _examStatisticsService.GetScoreStatisticsAsync(examSettingId, startDate, endDate);
        return this.AutoChartResult(statistics);
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
    [Chart("及格率分析", "展示考试及格率随时间的变化趋势")]
    [ChartType(ChartType.Line)]
    [ChartData(dimensionField: "TimePeriod", metricFields: new[] { "PassRate" })]
    public async Task<IActionResult> GetPassRateAnalysisAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange,
        [FromQuery] string groupBy = "Day")
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now;

        var passRateData = await _examStatisticsService.GetPassRateAnalysisAsync(examSettingId, startDate, endDate, groupBy);
        return this.AutoChartResult(passRateData);
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
    [Chart("分数段分布", "展示考试成绩在不同分数段的分布情况")]
    [ChartType(ChartType.Bar)]
    [ChartData(dimensionField: "ScoreRange", metricFields: new[] { "Count" })]
    public async Task<IActionResult> GetScoreDistributionAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] DateTime[] dateRange,
        [FromQuery] int segments = 10)
    {
        DateTimeOffset? startDate = dateRange?.Length > 0 ? dateRange[0] : null;
        DateTimeOffset? endDate = dateRange?.Length > 1 ? dateRange[1] : null;

        var distributionData = await _examStatisticsService.GetScoreDistributionAsync(examSettingId, startDate, endDate, segments);
        return this.AutoChartResult(distributionData);
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
    [Chart("题目正确率分析", "展示题目的正确率排名")]
    [ChartType(ChartType.Bar)]
    [ChartData(dimensionField: "QuestionTitle", metricFields: new[] { "CorrectRate" })]
    public async Task<IActionResult> GetQuestionCorrectRateAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] int? questionType,
        [FromQuery] int topCount = 10)
    {
        var correctRateData = await _examStatisticsService.GetQuestionCorrectRateAsync(examSettingId, questionType, topCount);
        return this.AutoChartResult(correctRateData);
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
    [Chart("错题分析", "展示错题频率排名")]
    [ChartType(ChartType.Bar)]
    [ChartData(dimensionField: "QuestionTitle", metricFields: new[] { "WrongCount" })]
    public async Task<IActionResult> GetWrongQuestionAnalysisAsync(
        [FromQuery] long? examSettingId,
        [FromQuery] int? questionType,
        [FromQuery] int topCount = 10)
    {
        var wrongQuestionData = await _examStatisticsService.GetWrongQuestionAnalysisAsync(examSettingId, questionType, topCount);
        return this.AutoChartResult(wrongQuestionData);
    }
} 