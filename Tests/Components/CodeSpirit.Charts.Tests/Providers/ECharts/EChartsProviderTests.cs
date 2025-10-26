using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Charts.Tests.Providers.ECharts;

public class EChartsProviderTests
{
    private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
    private readonly EChartsProvider _provider;

    public EChartsProviderTests()
    {
        _loggerMock = new Mock<ILogger<EChartsProvider>>();
        _provider = new EChartsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GenerateChartConfig_WithEmptyPieData_ReturnsDefaultData()
    {
        // 准备
        var data = new List<object>(); // 空数据
        var options = new Dictionary<string, object>
        {
            ["categoryField"] = "Gender",
            ["valueField"] = "Count"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", data, options);

        // 断言
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, object>>(result);

        var resultDict = (Dictionary<string, object>)result;

        // 验证结果包含必要的结构
        Assert.True(resultDict.ContainsKey("series"));
        Assert.True(resultDict.ContainsKey("tooltip"));
        Assert.True(resultDict.ContainsKey("legend"));

        // 验证series结构
        var series = (object[])resultDict["series"];
        Assert.Single(series);

        var seriesItem = (Dictionary<string, object>)series[0];
        Assert.Equal("pie", seriesItem["type"]);
        Assert.Equal("50%", seriesItem["radius"]);

        // 验证数据包含默认值
        var data1 = (object[])seriesItem["data"];
        Assert.NotEmpty(data1);

        var dataItem = (Dictionary<string, object>)data1[0];
        Assert.Equal("暂无数据", dataItem["name"]);
        Assert.Equal(100, dataItem["value"]);
    }

    [Fact]
    public void GenerateChartConfig_WithValidPieData_ReturnsFormattedConfig()
    {
        // 准备
        var data = new List<object>
        {
            new Dictionary<string, object> { ["Gender"] = "Male", ["Count"] = 2 },
            new Dictionary<string, object> { ["Gender"] = "Female", ["Count"] = 1 },
            new Dictionary<string, object> { ["Gender"] = "Unknown", ["Count"] = 4 }
        };

        var options = new Dictionary<string, object>
        {
            ["categoryField"] = "Gender",
            ["valueField"] = "Count",
            ["title"] = "性别分布"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", data, options);

        // 断言
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, object>>(result);

        var resultDict = (Dictionary<string, object>)result;

        // 验证结果包含必要的结构
        Assert.True(resultDict.ContainsKey("series"));
        Assert.True(resultDict.ContainsKey("tooltip"));
        Assert.True(resultDict.ContainsKey("legend"));
        Assert.True(resultDict.ContainsKey("title"));

        // 验证title
        var title = (Dictionary<string, object>)resultDict["title"];
        Assert.Equal("性别分布", title["text"]);

        // 验证series结构
        var series = (object[])resultDict["series"];
        Assert.Single(series);

        var seriesItem = (Dictionary<string, object>)series[0];
        Assert.Equal("pie", seriesItem["type"]);
        Assert.Equal("50%", seriesItem["radius"]);

        // 验证数据转换正确
        var pieData = (object[])seriesItem["data"];
        Assert.Equal(3, pieData.Length);

        // 验证圆饼图的legend数据已生成
        var legendData = (object[])((Dictionary<string, object>)resultDict["legend"])["data"];
        Assert.Equal(3, legendData.Length);
        Assert.Contains("Male", legendData);
        Assert.Contains("Female", legendData);
        Assert.Contains("Unknown", legendData);
    }

    [Fact]
    public void GenerateChartConfig_WithPreformattedPieData_UsesProvidedData()
    {
        // 准备 - 直接使用符合name/value格式的数据
        var data = new List<object>
        {
            new Dictionary<string, object> { ["name"] = "男性", ["value"] = 2 },
            new Dictionary<string, object> { ["name"] = "女性", ["value"] = 1 },
            new Dictionary<string, object> { ["name"] = "未知", ["value"] = 4 }
        };

        var options = new Dictionary<string, object>
        {
            ["title"] = "性别分布"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", data, options);

        // 断言
        Assert.NotNull(result);
        var resultDict = (Dictionary<string, object>)result;
        var series = (object[])resultDict["series"];
        var seriesItem = (Dictionary<string, object>)series[0];
        var pieData = (object[])seriesItem["data"];

        // 验证已提供格式的数据被正确使用
        Assert.Equal(3, pieData.Length);
        
        // 验证第一个数据项
        var firstItem = (Dictionary<string, object>)pieData[0];
        Assert.Equal("男性", firstItem["name"]);
        Assert.Equal(2, firstItem["value"]);
    }

    [Fact]
    public void GenerateChartConfig_WithJsonStringData_ParsesAndFormatsCorrectly()
    {
        // 准备 - JSON字符串数据
        string jsonData = "[{\"Gender\":\"Unknown\",\"Count\":4},{\"Gender\":\"Male\",\"Count\":2},{\"Gender\":\"Female\",\"Count\":1}]";

        var options = new Dictionary<string, object>
        {
            ["categoryField"] = "Gender",
            ["valueField"] = "Count"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", jsonData, options);

        // 断言
        Assert.NotNull(result);
        var resultDict = (Dictionary<string, object>)result;
        var series = (object[])resultDict["series"];
        var seriesItem = (Dictionary<string, object>)series[0];
        var pieData = (object[])seriesItem["data"];

        // 验证JSON数据被解析并正确格式化
        Assert.Equal(3, pieData.Length);
        
        // 验证数据中包含预期的项目
        bool foundUnknown = false;
        bool foundMale = false;
        bool foundFemale = false;

        foreach (Dictionary<string, object> item in pieData)
        {
            if (item["name"].ToString() == "Unknown" && (int)item["value"] == 4)
                foundUnknown = true;
            else if (item["name"].ToString() == "Male" && (int)item["value"] == 2)
                foundMale = true;
            else if (item["name"].ToString() == "Female" && (int)item["value"] == 1)
                foundFemale = true;
        }

        Assert.True(foundUnknown);
        Assert.True(foundMale);
        Assert.True(foundFemale);
    }

    [Fact]
    public void GenerateChartConfig_WithNullData_ReturnsDefaultConfig()
    {
        // 准备
        object data = null;
        var options = new Dictionary<string, object>
        {
            ["categoryField"] = "Gender",
            ["valueField"] = "Count"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", data, options);

        // 断言
        Assert.NotNull(result);
        var resultDict = (Dictionary<string, object>)result;
        var series = (object[])resultDict["series"];
        var seriesItem = (Dictionary<string, object>)series[0];
        var pieData = (object[])seriesItem["data"];

        // 验证使用了默认数据
        Assert.NotEmpty(pieData);
        var defaultItem = (Dictionary<string, object>)pieData[0];
        Assert.Equal("示例数据", defaultItem["name"]);
        Assert.Equal(100, defaultItem["value"]);
    }
} 