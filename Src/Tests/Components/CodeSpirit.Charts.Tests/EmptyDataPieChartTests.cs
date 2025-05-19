using CodeSpirit.Charts.Core.Abstractions;
using CodeSpirit.Charts.Providers.ECharts;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xunit;

namespace CodeSpirit.Charts.Tests;

/// <summary>
/// 饼图空数据处理测试
/// </summary>
public class EmptyDataPieChartTests
{
    private readonly Mock<ILogger<EChartsProvider>> _loggerMock;
    private readonly EChartsProvider _provider;

    public EmptyDataPieChartTests()
    {
        _loggerMock = new Mock<ILogger<EChartsProvider>>();
        _provider = new EChartsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GeneratePieChart_WithEmptyData_ReturnsExpectedFormat()
    {
        // 准备
        var emptyData = new List<object>();
        var options = new Dictionary<string, object>
        {
            ["categoryField"] = "Gender",
            ["valueField"] = "Count"
        };

        // 执行
        var result = _provider.GenerateChartConfig("pie", emptyData, options);

        // 序列化结果以查看其结构
        var resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);

        // 预期的API输出格式（基于用户提供的样例）
        var expectedOutput = @"{
  ""tooltip"": {
    ""trigger"": ""item"",
    ""formatter"": ""{a} <br/>{b}: {c} ({d}%)""
  },
  ""legend"": {
    ""orient"": ""vertical"",
    ""left"": 10,
    ""data"": [
      ""暂无数据""
    ]
  },
  ""series"": [
    {
      ""name"": ""数据统计"",
      ""type"": ""pie"",
      ""radius"": ""50%"",
      ""center"": [
        ""50%"",
        ""55%""
      ],
      ""data"": [
        {
          ""name"": ""暂无数据"",
          ""value"": 100
        }
      ],
      ""emphasis"": {
        ""itemStyle"": {
          ""shadowBlur"": 10,
          ""shadowOffsetX"": 0,
          ""shadowColor"": ""rgba(0, 0, 0, 0.5)""
        }
      }
    }
  ],
  ""categoryField"": ""Gender"",
  ""valueField"": ""Count""
}";

        // 断言
        var resultDict = (Dictionary<string, object>)result;
        
        // 验证关键字段存在
        Assert.True(resultDict.ContainsKey("tooltip"));
        Assert.True(resultDict.ContainsKey("legend"));
        Assert.True(resultDict.ContainsKey("series"));
        Assert.True(resultDict.ContainsKey("categoryField"));
        Assert.True(resultDict.ContainsKey("valueField"));
        
        // 验证tooltip
        var tooltip = (Dictionary<string, object>)resultDict["tooltip"];
        Assert.Equal("item", tooltip["trigger"]);
        
        // 验证legend
        var legend = (Dictionary<string, object>)resultDict["legend"];
        Assert.Equal("vertical", legend["orient"]);
        Assert.Equal(10, legend["left"]);
        var legendData = (object[])legend["data"];
        Assert.Single(legendData);
        Assert.Equal("暂无数据", legendData[0]);
        
        // 验证series
        var series = (object[])resultDict["series"];
        Assert.Single(series);
        var seriesItem = (Dictionary<string, object>)series[0];
        Assert.Equal("pie", seriesItem["type"]);
        Assert.Equal("50%", seriesItem["radius"]);
        
        // 验证center
        var center = (object[])seriesItem["center"];
        Assert.Equal(2, center.Length);
        Assert.Equal("50%", center[0]);
        Assert.Equal("55%", center[1]);
        
        // 验证数据
        var data = (object[])seriesItem["data"];
        Assert.Single(data);
        var dataItem = (Dictionary<string, object>)data[0];
        Assert.Equal("暂无数据", dataItem["name"]);
        Assert.Equal(100, dataItem["value"]);
        
        // 验证分类和值字段
        Assert.Equal("Gender", resultDict["categoryField"]);
        Assert.Equal("Count", resultDict["valueField"]);
    }

    [Fact]
    public void GeneratePieChart_WithJsonStringData_ParsesAndFormatsProperly()
    {
        // 准备
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
        
        // 验证series和数据
        var series = (object[])resultDict["series"];
        var seriesItem = (Dictionary<string, object>)series[0];
        var pieData = (object[])seriesItem["data"];
        
        // 确认所有三个数据项都被正确转换
        Assert.Equal(3, pieData.Length);
        
        // 验证legend数据包含三个标签
        var legendData = (object[])((Dictionary<string, object>)resultDict["legend"])["data"];
        Assert.Equal(3, legendData.Length);
        Assert.Contains("Unknown", legendData);
        Assert.Contains("Male", legendData);
        Assert.Contains("Female", legendData);
    }
} 