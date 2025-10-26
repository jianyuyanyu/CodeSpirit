using CodeSpirit.Charts.Attributes;
using Xunit;

namespace CodeSpirit.Charts.Tests.Attributes
{
    public class ChartAttributeTests
    {
        [Fact]
        public void ChartAttribute_DefaultConstructor_InitializesDefaults()
        {
            // 创建特性
            var attr = new ChartAttribute();

            // 断言默认值
            Assert.Equal(string.Empty, attr.Title);
            Assert.Equal(string.Empty, attr.Description);
            Assert.False(attr.AutoRefresh);
            Assert.Equal(60, attr.RefreshInterval);
            Assert.True(attr.ShowToolbox);
            Assert.Equal("default", attr.Theme);
            Assert.True(attr.EnableInteraction);
            Assert.True(attr.EnableExport);
        }

        [Fact]
        public void ChartAttribute_TitleConstructor_SetsTitle()
        {
            // 创建特性
            var attr = new ChartAttribute()
            {
                Title = "测试图表"
            };

            // 断言
            Assert.Equal("测试图表", attr.Title);
            Assert.Equal(string.Empty, attr.Description);
        }

        [Fact]
        public void ChartAttribute_FullConstructor_SetsTitleAndDescription()
        {
            // 创建特性
            var attr = new ChartAttribute()
            {
                Title = "测试图表",
                Description = "这是一个测试图表"
            };

            // 断言
            Assert.Equal("测试图表", attr.Title);
            Assert.Equal("这是一个测试图表", attr.Description);
        }

        [Fact]
        public void ChartAttribute_AllProperties_CanBeConfigured()
        {
            // 创建并配置特性
            var attr = new ChartAttribute
            {
                Title = "销售统计",
                Description = "销售月度统计图表",
                AutoRefresh = true,
                RefreshInterval = 30,
                ShowToolbox = false,
                Theme = "dark",
                EnableInteraction = false,
                EnableExport = false
            };

            // 断言
            Assert.Equal("销售统计", attr.Title);
            Assert.Equal("销售月度统计图表", attr.Description);
            Assert.True(attr.AutoRefresh);
            Assert.Equal(30, attr.RefreshInterval);
            Assert.False(attr.ShowToolbox);
            Assert.Equal("dark", attr.Theme);
            Assert.False(attr.EnableInteraction);
            Assert.False(attr.EnableExport);
        }
    }
}