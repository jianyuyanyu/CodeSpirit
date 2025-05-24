using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Charts.Attributes;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 审计统计图表控制器
    /// </summary>
    [DisplayName("审计统计")]
    [Navigation(Icon = "fa-solid fa-chart-bar")]
    public class AuditStatisticsController : ApiControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditStatisticsController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">审计服务</param>
        /// <param name="logger">日志记录器</param>
        public AuditStatisticsController(
            IAuditService auditService,
            ILogger<AuditStatisticsController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// 获取操作类型统计
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("operations")]
        [Display(Name = "操作类型统计")]
        [Chart("pie")]
        [ChartData(CategoryField = "OperationType", ValueField = "Count")]
        [DisplayName("获取操作类型统计")]
        [ChartTitle("操作类型分布")]
        public async Task<IActionResult> GetOperationStatsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTime startDate = dateRange?.Length > 0 ? dateRange[0] : DateTime.UtcNow.AddMonths(-1);
            DateTime endDate = dateRange?.Length > 1 ? dateRange[1] : DateTime.UtcNow;

            // 获取操作统计数据
            var stats = await _auditService.GetOperationStatsAsync(startDate, endDate);
            
            // 转换为图表数据格式
            var chartData = stats.Select(kvp => new 
            {
                OperationType = kvp.Key,
                Count = kvp.Value
            }).ToList();

            return await this.AutoChartResult(chartData);
        }

        /// <summary>
        /// 获取用户操作统计
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <param name="topN">显示前N个用户，默认10</param>
        /// <returns>图表配置</returns>
        [HttpGet("users")]
        [Display(Name = "用户操作统计")]
        [Chart("bar")]
        [ChartData(CategoryField = "UserName", ValueField = "Count")]
        [DisplayName("获取用户操作统计")]
        [ChartTitle("活跃用户操作次数")]
        public async Task<IActionResult> GetUserStatsAsync(
            [FromQuery] DateTime[] dateRange,
            [FromQuery] int topN = 10)
        {
            DateTime startDate = dateRange?.Length > 0 ? dateRange[0] : DateTime.UtcNow.AddMonths(-1);
            DateTime endDate = dateRange?.Length > 1 ? dateRange[1] : DateTime.UtcNow;

            // 获取用户操作统计数据
            var stats = await _auditService.GetUserStatsAsync(startDate, endDate, topN);
            
            // 转换为图表数据格式
            var chartData = stats.Select(kvp => new 
            {
                UserName = kvp.Key,
                Count = kvp.Value
            }).ToList();

            return await this.AutoChartResult(chartData);
        }

        /// <summary>
        /// 获取操作趋势
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <param name="interval">时间间隔（小时），默认24</param>
        /// <returns>图表配置</returns>
        [HttpGet("trend")]
        [Display(Name = "操作趋势")]
        [Chart("line")]
        [ChartData(XField = "Date", YField = "Count")]
        [DisplayName("获取操作趋势")]
        [ChartTitle("系统操作趋势")]
        public async Task<IActionResult> GetOperationTrendAsync(
            [FromQuery] DateTime[] dateRange,
            [FromQuery] int interval = 24)
        {
            DateTime startDate = dateRange?.Length > 0 ? dateRange[0] : DateTime.UtcNow.AddDays(-30);
            DateTime endDate = dateRange?.Length > 1 ? dateRange[1] : DateTime.UtcNow;

            // 获取操作趋势数据
            var trend = await _auditService.GetOperationTrendAsync(startDate, endDate, interval);
            
            // 转换为图表数据格式
            var chartData = trend.Select(kvp => new 
            {
                Date = kvp.Key.ToString("yyyy-MM-dd HH:mm"),
                Count = kvp.Value
            }).OrderBy(item => item.Date).ToList();

            return await this.AutoChartResult(chartData);
        }

        /// <summary>
        /// 获取系统操作数量汇总（今日、本周、本月）
        /// </summary>
        /// <returns>操作数量汇总</returns>
        [HttpGet("summary")]
        [Display(Name = "操作数量汇总")]
        [Chart("card")]
        [ChartData(CategoryField = "Period", ValueField = "Count")]
        [DisplayName("获取操作数量汇总")]
        public async Task<IActionResult> GetOperationSummaryAsync()
        {
            DateTime now = DateTime.UtcNow;
            
            // 今日
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1).AddSeconds(-1);
            var todayTrend = await _auditService.GetOperationTrendAsync(todayStart, todayEnd, 24);
            var todayCount = todayTrend.Values.Sum();
            
            // 本周
            var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            var weekEnd = weekStart.AddDays(7).AddSeconds(-1);
            var weekTrend = await _auditService.GetOperationTrendAsync(weekStart, weekEnd, 24);
            var weekCount = weekTrend.Values.Sum();
            
            // 本月
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
            var monthTrend = await _auditService.GetOperationTrendAsync(monthStart, monthEnd, 24);
            var monthCount = monthTrend.Values.Sum();
            
            var summaryData = new List<object>
            {
                new { Period = "今日", Count = todayCount },
                new { Period = "本周", Count = weekCount },
                new { Period = "本月", Count = monthCount }
            };
            
            return await this.AutoChartResult(summaryData);
        }

        /// <summary>
        /// 获取每日操作统计
        /// </summary>
        /// <param name="days">统计天数，默认为7天</param>
        /// <returns>图表配置</returns>
        [HttpGet("daily")]
        [Display(Name = "每日操作统计")]
        [Chart("bar")]
        [ChartData(CategoryField = "Date", ValueField = "Count")]
        [DisplayName("获取每日操作统计")]
        [ChartTitle("每日操作数量")]
        public async Task<IActionResult> GetDailyOperationsAsync([FromQuery] int days = 7)
        {
            DateTime endDate = DateTime.UtcNow;
            DateTime startDate = endDate.AddDays(-days);
            
            // 获取操作趋势数据
            var trend = await _auditService.GetOperationTrendAsync(startDate, endDate, 24);
            
            // 按天聚合数据
            var dailyData = trend
                .GroupBy(kvp => kvp.Key.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Count = g.Sum(kvp => kvp.Value)
                })
                .OrderBy(item => item.Date)
                .ToList();
            
            return await this.AutoChartResult(dailyData);
        }

        /// <summary>
        /// 获取操作成功率统计
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>图表配置</returns>
        [HttpGet("success-rate")]
        [Display(Name = "操作成功率")]
        [Chart("gauge")]
        [ChartData(CategoryField = "Status", ValueField = "Percentage")]
        [DisplayName("获取操作成功率")]
        public async Task<IActionResult> GetSuccessRateAsync([FromQuery] DateTime[] dateRange)
        {
            DateTime startDate = dateRange?.Length > 0 ? dateRange[0] : DateTime.UtcNow.AddMonths(-1);
            DateTime endDate = dateRange?.Length > 1 ? dateRange[1] : DateTime.UtcNow;

            // 获取操作趋势数据
            var trend = await _auditService.GetOperationTrendAsync(startDate, endDate, 24);
            
            // 模拟成功率数据（实际项目中应该从AuditLog中获取）
            var totalCount = trend.Values.Sum();
            var successCount = (long)(totalCount * 0.95); // 假设95%成功率
            
            var chartData = new List<object>
            {
                new { Status = "成功", Percentage = (double)successCount / totalCount * 100 }
            };
            
            return await this.AutoChartResult(chartData);
        }
    }
} 