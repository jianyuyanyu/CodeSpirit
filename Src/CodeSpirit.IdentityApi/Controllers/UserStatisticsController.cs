using CodeSpirit.Core.Attributes;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("用户统计")]
    [Permission(Name = PermissionCodes.UserStatistics)]
    public class UserStatisticsController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UserStatisticsController(
            IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 获取用户增长趋势图的 ECharts 配置
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>ECharts 配置</returns>
        [HttpGet("usergrowth")]
        [Display(Name = "用户增长趋势")]
        public async Task<ActionResult<EChartsConfig>> GetUserGrowthStatisticsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-1);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);

            List<UserGrowthDto> dailyGrowth = await _userService.GetUserGrowthAsync(startDate, endDate);
            List<string> dates = dailyGrowth.Select(g => g.Date.ToString("yyyy-MM-dd")).ToList();
            List<int> userCounts = dailyGrowth.Select(g => g.UserCount).ToList();

            // 创建 ECharts 配置
            EChartsConfig eChartConfig = new()
            {
                Title = new EChartsTitle
                {
                    Text = "用户增长趋势",
                    Left = "center"
                },
                Tooltip = new EChartsTooltip
                {
                    Trigger = "axis",
                    Formatter = "function (params) { var date = params[0].name; var count = params[0].value; return date + '<br/>用户数量: ' + count; }",
                },
                XAxis = new EChartsXAxis
                {
                    Type = "category",
                    Data = dates,
                    BoundaryGap = false
                },
                YAxis = new EChartsYAxis
                {
                    Type = "value",
                    Name = "用户数量"
                },
                Series =
                [
                    new EChartsSeries
                    {
                        Name = "用户增长",
                        Type = "line",
                        Data = userCounts,
                        Smooth = true,
                        ItemStyle = new EChartsItemStyle { Color = "#34b7f1" },
                        LineStyle = new EChartsLineStyle
                        {
                            Color = "#34b7f1"
                        },
                        AnimationDuration = 1000,
                        AnimationEasing = "cubicInOut",
                        Emphasis = new EChartsEmphasis
                        {
                            ItemStyle = new EChartsItemStyle
                            {
                                Color = "#ff6347"
                            }
                        }
                    }
                ],
                BackgroundColor = "#f4f4f4"
            };

            return Ok(eChartConfig);
        }

        /// <summary>
        /// 获取活跃用户统计图的 ECharts 配置
        /// </summary>
        /// <param name="dateRange">日期范围</param>
        /// <returns>ECharts 配置</returns>
        [HttpGet("activeusers")]
        public async Task<ActionResult<EChartsConfig>> GetActiveUsersStatisticsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-3);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now;
            List<ActiveUserDto> dailyActiveUsers = await _userService.GetActiveUsersAsync(startDate, endDate);

            List<string> dates = dailyActiveUsers.Select(g => g.Date.ToString("yyyy-MM-dd")).ToList();
            List<int> activeUserCounts = dailyActiveUsers.Select(g => g.ActiveUserCount).ToList();

            // 创建 ECharts 配置
            EChartsConfig eChartConfig = new()
            {
                Title = new EChartsTitle
                {
                    Text = "活跃用户统计",
                    Left = "center"
                },
                Tooltip = new EChartsTooltip
                {
                    Trigger = "axis",
                    Formatter = "function (params) { var date = params[0].name; var count = params[0].value; return date + '<br/>活跃用户数量: ' + count; }"
                },
                XAxis = new EChartsXAxis
                {
                    Type = "category",
                    Data = dates
                },
                YAxis = new EChartsYAxis
                {
                    Type = "value",
                    Name = "活跃用户数量"
                },
                Series =
                [
                    new EChartsSeries
                    {
                        Name = "活跃用户",
                        Type = "bar",
                        Data = activeUserCounts,
                        BarWidth = "40%",
                        ItemStyle = new EChartsItemStyle
                        {
                            Color = "#7ea1ff"
                        },
                        BorderRadius = 10,
                        AnimationDuration = 1000,
                        AnimationEasing = "cubicInOut",
                        Emphasis = new EChartsEmphasis
                        {
                            ItemStyle = new EChartsItemStyle
                            {
                                Color = "#ff7f50"
                            }
                        }
                    }
                ],
                BackgroundColor = "#f4f4f4"
            };

            return Ok(eChartConfig);
        }

        [HttpGet("usergrowth-and-active-users")]
        public async Task<ActionResult<EChartsConfig>> GetUserGrowthAndActiveUsersStatisticsAsync([FromQuery] DateTime[] dateRange)
        {
            DateTimeOffset startDate = dateRange?.Length > 0 ? dateRange[0] : DateTimeOffset.Now.AddMonths(-2);
            DateTimeOffset endDate = dateRange?.Length > 1 ? dateRange[1] : DateTimeOffset.Now.AddDays(1);
            List<UserGrowthDto> dailyGrowth = await _userService.GetUserGrowthAsync(startDate, endDate);

            List<int> userCounts = dailyGrowth.Select(g => g.UserCount).ToList();

            // 获取活跃用户数据
            List<ActiveUserDto> dailyActiveUsers = await _userService.GetActiveUsersAsync(startDate, endDate);
            List<int> activeUserCounts = dailyActiveUsers.Select(g => g.ActiveUserCount).ToList();
            List<string> dates = dailyActiveUsers.Select(g => g.Date.ToString("yyyy-MM-dd")).ToList();

            // 合并图表配置
            EChartsConfig eChartConfig = new()
            {
                Title = new EChartsTitle
                {
                    Text = "用户增长与活跃用户统计",
                    Left = "left",
                    TextStyle = new EChartsTextStyle
                    {
                        Color = "#333",
                        FontSize = 18,
                        FontWeight = "bold"
                    }
                },
                Tooltip = new EChartsTooltip
                {
                    Trigger = "axis",
                    Formatter = "function (params) { var date = params[0].name; var count = params[0].value; return date + '<br/>用户数量: ' + count + '<br/>活跃用户: ' + params[1].value; }",
                    TextStyle = new EChartsTextStyle
                    {
                        Color = "#fff",
                        FontSize = 14
                    },
                    BackgroundColor = "rgba(0, 0, 0, 0.7)"
                },
                Legend = new EChartsLegend
                {
                    Data = ["用户增长", "活跃用户"],  // 设置legend的名称
                    Left = "center",  // 设置legend的位置
                    Top = "top",   // 设置legend的位置，放在底部
                    TextStyle = new EChartsTextStyle
                    {
                        Color = "#666",  // 设置文字颜色
                        FontSize = 14     // 设置文字大小
                    }
                },
                XAxis = new EChartsXAxis
                {
                    Type = "category",
                    Data = dates,
                    BoundaryGap = false,
                    AxisLine = new EChartsAxisLine
                    {
                        LineStyle = new EChartsLineStyle
                        {
                            Color = "#ccc"
                        }
                    },
                    AxisLabel = new EChartsAxisLabel
                    {
                        Color = "#666",
                        FontSize = 12,
                        Rotate = 45
                    },
                    AxisTick = new EChartsAxisTick { Show = false }
                },
                YAxis = new List<EChartsYAxis>
        {
            new() {
                Type = "value",
                Name = "用户数量",
                AxisLine = new EChartsAxisLine
                {
                    LineStyle = new EChartsLineStyle
                    {
                        Color = "#ccc"
                    }
                },
                AxisLabel = new EChartsAxisLabel
                {
                    Color = "#666",
                    FontSize = 12
                },
                SplitLine = new EChartsSplitLine
                {
                    Show = true,
                    LineStyle = new EChartsLineStyle
                    {
                        Color = "#eee",
                        Type = "dashed"
                    }
                }
            },
            new() {
                Type = "value",
                Name = "活跃用户数量",
                AxisLine = new EChartsAxisLine
                {
                    LineStyle = new EChartsLineStyle
                    {
                        Color = "#ccc"
                    }
                },
                AxisLabel = new EChartsAxisLabel
                {
                    Color = "#666",
                    FontSize = 12
                },
                SplitLine = new EChartsSplitLine
                {
                    Show = true,
                    LineStyle = new EChartsLineStyle
                    {
                        Color = "#eee",
                        Type = "dashed"
                    }
                },
                Position = "right" // 设置活跃用户轴为右侧
            }
        },
                Series =
        [
            new EChartsSeries
            {
                Name = "用户增长",
                Type = "line",
                Data = userCounts,
                Smooth = true,
                ItemStyle = new EChartsItemStyle
                {
                    Color = "#34b7f1"
                },
                LineStyle = new EChartsLineStyle
                {
                    Color = "#34b7f1"
                },
                AnimationDuration = 1000,
                AnimationEasing = "cubicInOut",
                Emphasis = new EChartsEmphasis
                {
                    ItemStyle = new EChartsItemStyle
                    {
                        Color = "#ff6347"
                    }
                },
                YAxisIndex = 0, // 使用左侧 Y 轴
                //Label = new EChartsLabel
                //{
                //    Show = true,
                //    Position = "top",
                //    Color = "#34b7f1",
                //    FontSize = 12,
                //    Formatter = "{c}" // 显示数值
                //}
            },
            new EChartsSeries
            {
                Name = "活跃用户",
                Type = "bar",
                Data = activeUserCounts,
                BarWidth = "40%",
                ItemStyle = new EChartsItemStyle
                {
                    Color = "#7ea1ff"
                },
                BorderRadius = 10,
                AnimationDuration = 1000,
                AnimationEasing = "cubicInOut",
                Emphasis = new EChartsEmphasis
                {
                    ItemStyle = new EChartsItemStyle
                    {
                        Color = "#ff7f50"
                    }
                },
                YAxisIndex = 1, // 使用右侧 Y 轴
                //Label = new EChartsLabel
                //{
                //    Show = true,
                //    Position = "top",
                //    Color = "#7ea1ff",
                //    FontSize = 12,
                //    Formatter = "{c}" // 显示数值
                //}
            }
        ],
                //BackgroundColor = "#f4f4f4"
            };

            return Ok(eChartConfig);
        }

    }

    public class EChartsLegend
    {
        public List<string> Data { get; set; }
        public string Left { get; set; }
        public string Top { get; set; }
        public EChartsTextStyle TextStyle { get; set; }
    }

    public class EChartsLabel
    {
        public bool Show { get; set; }
        public string Position { get; set; }
        public string Color { get; set; }
        public int FontSize { get; set; }
        public string Formatter { get; set; }
    }

    #region ECharts 配置模型
    public class EChartsConfig
    {
        public EChartsTitle Title { get; set; }
        public EChartsTooltip Tooltip { get; set; }
        public object XAxis { get; set; }
        public object YAxis { get; set; }
        public List<EChartsSeries> Series { get; set; }
        public EChartsGrid Grid { get; set; }
        public string BackgroundColor { get; set; } // 图表背景色
        public EChartsLegend Legend { get; set; }
    }

    public class EChartsTitle
    {
        public string Text { get; set; }
        public string Left { get; set; }
        public EChartsTextStyle TextStyle { get; set; }
    }

    public class EChartsTextStyle
    {
        public string Color { get; set; }
        public int FontSize { get; set; }
        public string FontWeight { get; set; }
    }

    public class EChartsTooltip
    {
        public string Trigger { get; set; }
        public string Formatter { get; set; }
        public EChartsTextStyle TextStyle { get; set; }
        public string BackgroundColor { get; set; }
    }

    public class EChartsXAxis
    {
        public string Type { get; set; }
        public List<string> Data { get; set; }
        public bool BoundaryGap { get; set; }
        public EChartsAxisLine AxisLine { get; set; }
        public EChartsAxisLabel AxisLabel { get; set; }
        public EChartsAxisTick AxisTick { get; set; }
    }

    public class EChartsAxisLine
    {
        public EChartsLineStyle LineStyle { get; set; }
    }

    public class EChartsAxisLabel
    {
        public string Color { get; set; }
        public int FontSize { get; set; }
        public int Rotate { get; set; }
    }

    public class EChartsAxisTick
    {
        public bool Show { get; set; }
    }

    public class EChartsYAxis
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public EChartsAxisLine AxisLine { get; set; }
        public EChartsAxisLabel AxisLabel { get; set; }
        public EChartsSplitLine SplitLine { get; set; }
        public string Position { get; set; }
    }

    public class EChartsSplitLine
    {
        public bool Show { get; set; }
        public EChartsLineStyle LineStyle { get; set; }
    }

    public class EChartsLineStyle
    {
        public string Color { get; set; }
        public string Type { get; set; }
    }

    public class EChartsSeries
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<int> Data { get; set; }
        public bool Smooth { get; set; }
        public EChartsItemStyle ItemStyle { get; set; }
        public EChartsLineStyle LineStyle { get; set; }
        public int AnimationDuration { get; set; }
        public string AnimationEasing { get; set; }
        public EChartsEmphasis Emphasis { get; set; }
        public string BarWidth { get; set; } // 柱状图宽度
        public int BorderRadius { get; set; } // 圆角
        public int YAxisIndex { get; set; }
        public EChartsLabel Label { get; set; }
    }

    public class EChartsItemStyle
    {
        public string Color { get; set; }
    }

    public class EChartsEmphasis
    {
        public EChartsItemStyle ItemStyle { get; set; }
    }

    public class EChartsGrid
    {
        public int Top { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public bool ContainLabel { get; set; }
        public string BorderColor { get; set; }
        public int BorderWidth { get; set; }
    }

    #endregion
}