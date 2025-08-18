using CodeSpirit.Charts;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Controllers.System;

/// <summary>
/// 系统平台用户统计控制器
/// </summary>
[DisplayName("用户统计")]
[Navigation(Icon = "fa-solid fa-chart-line", PlatformType = PlatformType.System)]
public class SystemUserStatisticsController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<SystemUserStatisticsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userService">用户服务</param>
    /// <param name="logger">日志服务</param>
    public SystemUserStatisticsController(
        IUserService userService,
        ILogger<SystemUserStatisticsController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取跨租户用户增长趋势对比图的配置
    /// </summary>
    /// <param name="dateRange">日期范围</param>
    /// <returns>图表配置</returns>
    [HttpGet("cross-tenant-growth")]
    [Display(Name = "跨租户用户增长趋势")]
    [Chart("line")]
    [ChartData(XField = "Date", YField = "UserCount", SeriesField = "TenantName")]
    [DisplayName("获取跨租户用户增长趋势")]
    public async Task<IActionResult> GetCrossTenantUserGrowthTrendAsync([FromQuery] DateTime[] dateRange)
    {
        DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-3);
        DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

        // 获取数据
        var crossTenantGrowth = await _userService.GetCrossTenantUserGrowthTrendAsync(startDate, endDate);
        return await this.AutoChartResult(crossTenantGrowth);
    }

    /// <summary>
    /// 获取各租户用户活跃度排行图的配置
    /// </summary>
    /// <param name="dateRange">日期范围</param>
    /// <returns>图表配置</returns>
    [HttpGet("tenant-activity-ranking")]
    [Display(Name = "租户用户活跃度排行")]
    [Chart("bar")]
    [ChartData(CategoryField = "TenantName", ValueField = "ActivityScore")]
    [DisplayName("获取租户活跃度排行")]
    [ChartTitle("租户用户活跃度排行")]
    public async Task<IActionResult> GetTenantUserActivityRankingAsync([FromQuery] DateTime[] dateRange)
    {
        DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
        DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

        var activityRanking = await _userService.GetTenantUserActivityRankingAsync(startDate, endDate);
        return await this.AutoChartResult(activityRanking);
    }

    /// <summary>
    /// 获取租户用户规模分布统计
    /// </summary>
    /// <returns>图表配置</returns>
    [HttpGet("tenant-scale-distribution")]
    [Display(Name = "租户用户规模分布")]
    [Chart("pie")]
    [ChartData(CategoryField = "ScaleRange", ValueField = "TenantCount")]
    [DisplayName("获取租户用户规模分布")]
    [ChartTitle("租户用户规模分布")]
    public async Task<IActionResult> GetTenantUserScaleDistributionAsync()
    {
        var scaleDistribution = await _userService.GetTenantUserScaleDistributionAsync();
        return await this.AutoChartResult(scaleDistribution);
    }

    /// <summary>
    /// 获取系统整体用户健康度分析
    /// </summary>
    /// <returns>用户健康度分析数据</returns>
    [HttpGet("system-health-analysis")]
    [DisplayName("获取系统用户健康度分析")]
    public async Task<ActionResult<ApiResponse<object>>> GetSystemUserHealthAnalysisAsync()
    {
        var healthAnalysis = await _userService.GetSystemUserHealthAnalysisAsync();
        return SuccessResponse(healthAnalysis);
    }

    /// <summary>
    /// 获取租户用户迁移历史统计
    /// </summary>
    /// <param name="dateRange">日期范围</param>
    /// <returns>图表配置</returns>
    [HttpGet("user-transfer-history")]
    [Display(Name = "用户迁移历史统计")]
    [Chart("line")]
    [ChartData(XField = "Date", YField = "TransferCount")]
    [DisplayName("获取用户迁移历史统计")]
    public async Task<IActionResult> GetUserTransferHistoryStatisticsAsync([FromQuery] DateTime[] dateRange)
    {
        DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-6);
        DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

        var transferHistory = await _userService.GetUserTransferHistoryStatisticsAsync(startDate, endDate);
        return await this.AutoChartResult(transferHistory);
    }

    /// <summary>
    /// 获取系统级用户总体统计概览
    /// </summary>
    /// <returns>用户总体统计数据</returns>
    [HttpGet("overview")]
    [DisplayName("获取系统用户概览")]
    public async Task<ActionResult<ApiResponse<object>>> GetSystemUserOverviewAsync()
    {
        try
        {
            // 获取租户用户统计
            var tenantStatistics = await _userService.GetUsersByTenantStatisticsAsync();
            
            // 计算系统总体数据
            object overview = new
            {
                TotalTenants = tenantStatistics.Count,
                TotalUsers = tenantStatistics.Sum(t => t.TotalUsers),
                TotalActiveUsers = tenantStatistics.Sum(t => t.ActiveUsers),
                TotalInactiveUsers = tenantStatistics.Sum(t => t.InactiveUsers),
                TotalAdminUsers = tenantStatistics.Sum(t => t.AdminUsers),
                TotalNormalUsers = tenantStatistics.Sum(t => t.NormalUsers),
                NewUsersThisMonth = tenantStatistics.Sum(t => t.NewUsersThisMonth),
                ActiveUsersThisMonth = tenantStatistics.Sum(t => t.ActiveUsersThisMonth),
                AverageActivityRate = tenantStatistics.Average(t => t.ActivityRate),
                AverageGrowthRate = tenantStatistics.Average(t => t.GrowthRate),
                LastUpdated = DateTimeOffset.Now
            };

            return SuccessResponse(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统用户概览时发生错误");
            return BadResponse<object>("获取系统用户概览失败，请稍后重试");
        }
    }

    /// <summary>
    /// 获取租户用户统计详细列表
    /// </summary>
    /// <returns>租户用户统计列表</returns>
    [HttpGet("tenant-details")]
    [DisplayName("获取租户统计详情")]
    public async Task<ActionResult<ApiResponse<List<TenantUserStatisticsDto>>>> GetTenantStatisticsDetails()
    {
        try
        {
            var statistics = await _userService.GetUsersByTenantStatisticsAsync();
            return SuccessResponse(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取租户用户统计详情时发生错误");
            return BadResponse<List<TenantUserStatisticsDto>>("获取租户统计详情失败，请稍后重试");
        }
    }
} 