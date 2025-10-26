using CodeSpirit.Charts.Models;
using Xunit;

namespace CodeSpirit.Charts.Tests.Models
{
    public class ChartConfigTests
    {
        [Fact]
        public void ChartConfig_DefaultValues_AreCorrect()
        {
            // 创建图表配置
            var config = new ChartConfig();

            // 断言默认值
            Assert.NotNull(config.Id);
            Assert.Null(config.Title);
            Assert.Equal(ChartType.Auto, config.Type);
            Assert.Null(config.SubType);
            Assert.NotNull(config.DataSource);
            Assert.Null(config.XAxis);
            Assert.Null(config.YAxis);
            Assert.Null(config.Legend);
            Assert.NotNull(config.Series);
            Assert.Empty(config.Series);
            Assert.Null(config.Toolbox);
            Assert.False(config.AutoRefresh);
            Assert.Equal(60, config.RefreshInterval);
            Assert.Null(config.Interaction);
            Assert.Equal("default", config.Theme);
            Assert.Null(config.ExtraStyles);
        }

        [Fact]
        public void SeriesConfig_DefaultValues_AreCorrect()
        {
            // 创建系列配置
            var series = new SeriesConfig();

            // 断言默认值
            Assert.Null(series.Name);
            Assert.Equal("line", series.Type);
            Assert.Null(series.Data);
            Assert.Null(series.Label);
            Assert.Null(series.ItemStyle);
            Assert.Null(series.Emphasis);
            Assert.Null(series.Stack);
            Assert.Null(series.ExtraOptions);
        }

        [Fact]
        public void ToolboxConfig_DefaultValues_AreCorrect()
        {
            // 创建工具箱配置
            var toolbox = new ToolboxConfig();

            // 断言默认值
            Assert.True(toolbox.Show);
            Assert.Equal("horizontal", toolbox.Orient);
            Assert.NotNull(toolbox.Features);
            Assert.Equal(5, toolbox.Features.Count);
            Assert.True(toolbox.Features["saveAsImage"]);
            Assert.True(toolbox.Features["dataView"]);
            Assert.True(toolbox.Features["restore"]);
            Assert.True(toolbox.Features["dataZoom"]);
            Assert.True(toolbox.Features["magicType"]);
        }

        [Fact]
        public void ChartConfig_CanConfigureFullChart()
        {
            // 创建完整的图表配置
            var config = new ChartConfig
            {
                Title = "销售趋势",
                Type = ChartType.Line,
                XAxis = new AxisConfig
                {
                    Name = "日期",
                    Type = "time"
                },
                YAxis = new AxisConfig
                {
                    Name = "销售额"
                },
                Series = new List<SeriesConfig>
                {
                    new SeriesConfig
                    {
                        Name = "销售额",
                        Type = "line",
                        Data = new List<object> { 100, 120, 140 }
                    }
                },
                Toolbox = new ToolboxConfig(),
                AutoRefresh = true,
                RefreshInterval = 30
            };

            // 断言配置
            Assert.Equal("销售趋势", config.Title);
            Assert.Equal(ChartType.Line, config.Type);
            Assert.NotNull(config.XAxis);
            Assert.Equal("日期", config.XAxis.Name);
            Assert.Equal("time", config.XAxis.Type);
            Assert.NotNull(config.YAxis);
            Assert.Equal("销售额", config.YAxis.Name);
            Assert.NotNull(config.Series);
            Assert.Single(config.Series);
            Assert.Equal("销售额", config.Series[0].Name);
            Assert.Equal("line", config.Series[0].Type);
            Assert.NotNull(config.Series[0].Data);
            Assert.Equal(3, config.Series[0].Data.Count);
            Assert.NotNull(config.Toolbox);
            Assert.True(config.AutoRefresh);
            Assert.Equal(30, config.RefreshInterval);
        }

        [Fact]
        public void ChartDataSource_DefaultValues_AreCorrect()
        {
            // 创建数据源配置
            var dataSource = new ChartDataSource();

            // 断言默认值
            Assert.Equal(DataSourceType.Api, dataSource.Type);
            Assert.Null(dataSource.ApiUrl);
            Assert.Equal("GET", dataSource.Method);
            Assert.Null(dataSource.Parameters);
            Assert.Null(dataSource.StaticData);
            Assert.Null(dataSource.Mapping);
            Assert.Null(dataSource.Transformers);
            Assert.Null(dataSource.Analysis);
        }
    }
} 