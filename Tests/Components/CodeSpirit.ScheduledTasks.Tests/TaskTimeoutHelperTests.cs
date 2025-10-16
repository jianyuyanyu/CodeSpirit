using CodeSpirit.ScheduledTasks.Helpers;
using Xunit;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// 任务超时辅助类测试
/// </summary>
public class TaskTimeoutHelperTests
{
    [Fact]
    public void CreateTimeoutToken_ShouldCreateTokenWithTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        using var cts = TaskTimeoutHelper.CreateTimeoutToken(timeout);

        // Assert
        Assert.NotNull(cts);
        Assert.False(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_TaskCompletesInTime_ShouldReturnResult()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(2);
        var expectedResult = "Success";

        // Act
        var result = await TaskTimeoutHelper.ExecuteWithTimeoutAsync(
            async (ct) =>
            {
                await Task.Delay(100, ct);
                return expectedResult;
            },
            timeout);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_TaskTimesOut_ShouldThrowTimeoutException()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.ExecuteWithTimeoutAsync(
                async (ct) =>
                {
                    await Task.Delay(1000, ct);
                    return "Should not reach here";
                },
                timeout));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_NoReturnValue_TaskCompletesInTime_ShouldComplete()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(2);
        var completed = false;

        // Act
        await TaskTimeoutHelper.ExecuteWithTimeoutAsync(
            async (ct) =>
            {
                await Task.Delay(100, ct);
                completed = true;
            },
            timeout);

        // Assert
        Assert.True(completed);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_NoReturnValue_TaskTimesOut_ShouldThrowTimeoutException()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.ExecuteWithTimeoutAsync(
                async (ct) =>
                {
                    await Task.Delay(1000, ct);
                },
                timeout));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAndRetryAsync_TaskSucceedsOnFirstTry_ShouldReturnResult()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);
        var maxRetryCount = 3;
        var expectedResult = "Success";

        // Act
        var result = await TaskTimeoutHelper.ExecuteWithTimeoutAndRetryAsync(
            async (ct) =>
            {
                await Task.Delay(100, ct);
                return expectedResult;
            },
            timeout,
            maxRetryCount);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAndRetryAsync_TaskFailsMultipleTimes_ShouldRetryAndSucceed()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);
        var maxRetryCount = 3;
        var retryInterval = TimeSpan.FromMilliseconds(10);
        var attemptCount = 0;
        var expectedResult = "Success";

        // Act
        var result = await TaskTimeoutHelper.ExecuteWithTimeoutAndRetryAsync(
            async (ct) =>
            {
                attemptCount++;
                await Task.Delay(50, ct);
                
                if (attemptCount < 3)
                {
                    throw new InvalidOperationException("Simulated failure");
                }
                
                return expectedResult;
            },
            timeout,
            maxRetryCount,
            retryInterval);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAndRetryAsync_TaskAlwaysFails_ShouldThrowLastException()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);
        var maxRetryCount = 2;
        var retryInterval = TimeSpan.FromMilliseconds(10);
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TaskTimeoutHelper.ExecuteWithTimeoutAndRetryAsync(
                async (ct) =>
                {
                    attemptCount++;
                    await Task.Delay(50, ct);
                    throw new InvalidOperationException($"Attempt {attemptCount} failed");
                },
                timeout,
                maxRetryCount,
                retryInterval));

        Assert.Equal(3, attemptCount); // 初始尝试 + 2次重试
        Assert.Contains("Attempt 3 failed", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTimeoutException_ShouldIdentifyTimeoutExceptions(bool isTimeoutException)
    {
        // Arrange
        Exception exception = isTimeoutException 
            ? new TimeoutException("Task timed out")
            : new InvalidOperationException("Some other error");

        // Act
        var result = TaskTimeoutHelper.IsTimeoutException(exception);

        // Assert
        Assert.Equal(isTimeoutException, result);
    }

    [Fact]
    public void IsTimeoutException_OperationCanceledWithTimeoutMessage_ShouldReturnTrue()
    {
        // Arrange
        var exception = new OperationCanceledException("任务执行超时");

        // Act
        var result = TaskTimeoutHelper.IsTimeoutException(exception);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(30, 2.0, 60)]      // 30秒 * 2 = 60秒
    [InlineData(10, 3.0, 30)]      // 10秒 * 3 = 30秒
    [InlineData(5, 1.5, 30)]       // 5秒 * 1.5 = 7.5秒，但最小30秒
    [InlineData(3600, 2.0, 7200)]  // 1小时 * 2 = 2小时
    public void GetRecommendedTimeout_ShouldReturnCorrectTimeout(int estimatedSeconds, double safetyFactor, int expectedSeconds)
    {
        // Arrange
        var estimatedDuration = TimeSpan.FromSeconds(estimatedSeconds);

        // Act
        var result = TaskTimeoutHelper.GetRecommendedTimeout(estimatedDuration, safetyFactor);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), result);
    }

    [Fact]
    public void GetRecommendedTimeout_VeryLongDuration_ShouldCapAt24Hours()
    {
        // Arrange
        var estimatedDuration = TimeSpan.FromHours(20);
        var safetyFactor = 2.0;

        // Act
        var result = TaskTimeoutHelper.GetRecommendedTimeout(estimatedDuration, safetyFactor);

        // Assert
        Assert.Equal(TimeSpan.FromHours(24), result);
    }

    [Fact]
    public void GetRecommendedTimeout_InvalidSafetyFactor_ShouldUseDefault()
    {
        // Arrange
        var estimatedDuration = TimeSpan.FromSeconds(60);
        var invalidSafetyFactor = 0.5; // 小于1.0

        // Act
        var result = TaskTimeoutHelper.GetRecommendedTimeout(estimatedDuration, invalidSafetyFactor);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(120), result); // 60 * 2.0 (默认安全系数)
    }
}
