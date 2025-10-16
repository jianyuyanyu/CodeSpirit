using CodeSpirit.ScheduledTasks.Helpers;
using Xunit;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// Cron辅助类测试
/// </summary>
public class CronHelperTests
{
    [Theory]
    [InlineData("0 */5 * * * *", true)]  // 每5分钟
    [InlineData("0 0 9 * * 1-5", true)]  // 工作日上午9点
    [InlineData("* * * * * *", true)]    // 每秒
    [InlineData("invalid cron", false)]   // 无效表达式
    [InlineData("", false)]               // 空字符串
    [InlineData(null, false)]             // null
    public void IsValidCronExpression_ShouldReturnCorrectResult(string? cronExpression, bool expected)
    {
        // Act
        var result = CronHelper.IsValidCronExpression(cronExpression);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNextOccurrence_ValidCronExpression_ShouldReturnNextTime()
    {
        // Arrange
        var cronExpression = "0 */5 * * * *"; // 每5分钟
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CronHelper.GetNextOccurrence(cronExpression, baseTime);

        // Assert
        Assert.NotNull(result);
        Assert.True(result > baseTime);
        Assert.Equal(0, result.Value.Second);
        Assert.True(result.Value.Minute % 5 == 0);
    }

    [Fact]
    public void GetNextOccurrence_InvalidCronExpression_ShouldReturnNull()
    {
        // Arrange
        var cronExpression = "invalid cron";

        // Act
        var result = CronHelper.GetNextOccurrence(cronExpression);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetNextOccurrences_ValidCronExpression_ShouldReturnMultipleTimes()
    {
        // Arrange
        var cronExpression = "0 */15 * * * *"; // 每15分钟
        var count = 3;
        var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var results = CronHelper.GetNextOccurrences(cronExpression, count, baseTime);

        // Assert
        Assert.Equal(count, results.Count);
        
        for (int i = 0; i < results.Count; i++)
        {
            Assert.True(results[i] > baseTime);
            Assert.Equal(0, results[i].Second);
            Assert.True(results[i].Minute % 15 == 0);
            
            if (i > 0)
            {
                Assert.True(results[i] > results[i - 1]);
            }
        }
    }

    [Fact]
    public void GetNextOccurrences_InvalidCronExpression_ShouldReturnEmptyList()
    {
        // Arrange
        var cronExpression = "invalid cron";
        var count = 3;

        // Act
        var results = CronHelper.GetNextOccurrences(cronExpression, count);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void GetDescription_ValidCronExpression_ShouldReturnDescription()
    {
        // Arrange
        var cronExpression = "0 */5 * * * *";

        // Act
        var result = CronHelper.GetDescription(cronExpression);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(cronExpression, result);
    }

    [Fact]
    public void GetDescription_InvalidCronExpression_ShouldReturnErrorMessage()
    {
        // Arrange
        var cronExpression = "invalid cron";

        // Act
        var result = CronHelper.GetDescription(cronExpression);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("无效", result);
    }

    [Fact]
    public void Presets_ShouldContainExpectedValues()
    {
        // Assert
        Assert.Equal("* * * * * *", CronHelper.Presets.EverySecond);
        Assert.Equal("0 * * * * *", CronHelper.Presets.EveryMinute);
        Assert.Equal("0 0 * * * *", CronHelper.Presets.EveryHour);
        Assert.Equal("0 0 0 * * *", CronHelper.Presets.Daily);
        Assert.Equal("0 0 0 * * 1", CronHelper.Presets.Weekly);
        Assert.Equal("0 0 0 1 * *", CronHelper.Presets.Monthly);
        Assert.Equal("0 0 0 * * 1-5", CronHelper.Presets.Weekdays);
    }

    [Fact]
    public void Presets_GetAll_ShouldReturnAllPresets()
    {
        // Act
        var presets = CronHelper.Presets.GetAll();

        // Assert
        Assert.NotEmpty(presets);
        Assert.Contains("每分钟执行", presets.Keys);
        Assert.Contains("每小时执行", presets.Keys);
        Assert.Contains("每天执行", presets.Keys);
        Assert.Contains("工作日执行", presets.Keys);
        
        // 验证所有预设表达式都是有效的
        foreach (var preset in presets.Values)
        {
            Assert.True(CronHelper.IsValidCronExpression(preset), $"预设表达式无效: {preset}");
        }
    }

    [Theory]
    [InlineData("0 * * * * *")]      // 每分钟
    [InlineData("0 0 * * * *")]      // 每小时
    [InlineData("0 0 0 * * *")]      // 每天
    [InlineData("0 0 0 * * 1-5")]    // 工作日
    [InlineData("0 */15 * * * *")]   // 每15分钟
    public void PresetExpressions_ShouldBeValid(string cronExpression)
    {
        // Act & Assert
        Assert.True(CronHelper.IsValidCronExpression(cronExpression));
        
        var nextOccurrence = CronHelper.GetNextOccurrence(cronExpression);
        Assert.NotNull(nextOccurrence);
        Assert.True(nextOccurrence > DateTime.UtcNow);
    }
}
