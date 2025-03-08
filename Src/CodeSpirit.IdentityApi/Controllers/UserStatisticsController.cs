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

        /// <summary>
        /// 获取用户性别分布统计
        /// </summary>
        /// <returns>图表配置</returns>
        [HttpGet("gender-distribution")]
        [Display(Name = "用户性别分布")]
        [Chart("用户性别分布", "展示不同性别用户的数量分布")]
        [ChartType(ChartType.Pie)]
        [ChartData(dimensionField: "Gender", metricFields: new[] { "Count" })]
        public async Task<IActionResult> GetGenderDistributionAsync()
        {
            var genderDistribution = await _userService.GetUserGenderDistributionAsync();
            return this.AutoChartResult(genderDistribution);
        }

        /// <summary>
        /// 获取用户活跃状态分布
        /// </summary>
        /// <returns>图表配置</returns>
        [HttpGet("active-status")]
        [Display(Name = "用户活跃状态")]
        [Chart("用户活跃状态", "展示活跃和非活跃用户的分布")]
        [ChartType(ChartType.Pie)]
        [ChartData(dimensionField: "Status", metricFields: new[] { "Count" })]
        public async Task<IActionResult> GetActiveStatusDistributionAsync()
        {
            var statusDistribution = await _userService.GetUserActiveStatusDistributionAsync();
            return this.AutoChartResult(statusDistribution);
        }

        /// <summary>
        /// 获取用户注册时间分布
        /// </summary>
        /// <param name="groupBy">分组方式: Day, Week, Month, Year</param>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("registration-trend")]
        [Display(Name = "用户注册趋势")]
        [Chart("用户注册趋势", "展示用户注册时间的分布趋势")]
        [ChartData(dimensionField: "TimePeriod", metricFields: new[] { "RegisteredCount" })]
        public async Task<IActionResult> GetRegistrationTrendAsync(
            [FromQuery] string groupBy = "Day",
            [FromQuery] DateTime[] dateRange = null)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddYears(-1);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

            var registrationTrend = await _userService.GetUserRegistrationTrendAsync(startDate, endDate, groupBy);
            return this.AutoChartResult(registrationTrend);
        }

        /// <summary>
        /// 获取用户登录频率统计
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("login-frequency")]
        [Display(Name = "用户登录频率")]
        [Chart("用户登录频率", "展示用户登录频率的分布")]
        [ChartData(dimensionField: "FrequencyRange", metricFields: new[] { "UserCount" })]
        public async Task<IActionResult> GetLoginFrequencyAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-3);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

            var loginFrequency = await _userService.GetUserLoginFrequencyAsync(startDate, endDate);
            return this.AutoChartResult(loginFrequency);
        }
        
        /// <summary>
        /// 获取长期未登录用户统计
        /// </summary>
        /// <param name="thresholdDays">未登录天数阈值，默认30天</param>
        /// <returns>图表配置</returns>
        [HttpGet("inactive-users")]
        [Display(Name = "长期未登录用户")]
        [Chart("长期未登录用户", "展示长期未登录用户的数量随时间的变化")]
        [ChartType(ChartType.Line)]
        [ChartData(dimensionField: "InactiveDays", metricFields: new[] { "UserCount" })]
        public async Task<IActionResult> GetInactiveUsersAsync([FromQuery] int thresholdDays = 30)
        {
            var inactiveUsers = await _userService.GetInactiveUsersStatisticsAsync(thresholdDays);
            return this.AutoChartResult(inactiveUsers);
        }
    }
}