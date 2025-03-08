using CodeSpirit.Charts;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Charts.Models;
using CodeSpirit.Charts.Services;
using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("用户统计")]
    [Navigation(Icon = "fa-solid fa-chart-line")]
    public class UserStatisticsController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly IChartService _chartService;
        private readonly IEChartConfigGenerator _eChartConfigGenerator;

        public UserStatisticsController(
            IUserService userService,
            IChartService chartService,
            IEChartConfigGenerator eChartConfigGenerator)
        {
            _userService = userService;
            _chartService = chartService;
            _eChartConfigGenerator = eChartConfigGenerator;
        }

        /// <summary>
        /// 获取用户增长趋势图的配置
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("usergrowth")]
        [Display(Name = "用户增长趋势")]
        [Chart("用户增长趋势", "展示用户随时间的增长趋势")]
        [ChartType(ChartType.Line)]
        [ChartData(dimensionField: "Date", metricFields: new[] { "UserCount" })]
        public async Task<IActionResult> GetUserGrowthStatisticsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

            // 获取数据
            var dailyGrowth = await _userService.GetUserGrowthAsync(startDate, endDate);
            return this.AutoChartResult(dailyGrowth);
        }

        /// <summary>
        /// 获取活跃用户统计图的配置
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("activeusers")]
        [Display(Name = "活跃用户统计")]
        [Chart("活跃用户统计", "展示活跃用户数量随时间的变化")]
        [ChartType(ChartType.Bar)]
        [ChartData(dimensionField: "Date", metricFields: new[] { "ActiveUserCount" })]
        public async Task<IActionResult> GetActiveUsersStatisticsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

            // 获取数据
            var activeUsers = await _userService.GetActiveUsersAsync(startDate, endDate);
            return this.AutoChartResult(activeUsers);
        }
    }
}