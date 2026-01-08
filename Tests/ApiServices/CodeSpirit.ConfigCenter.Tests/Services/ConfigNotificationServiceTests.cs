namespace CodeSpirit.ConfigCenter.Tests.Services;

/// <summary>
/// 配置通知服务测试
/// </summary>
public class ConfigNotificationServiceTests
{
    private readonly Mock<IConfigChangeNotifier> _configChangeNotifierMock;
    private readonly Mock<ILogger<ConfigNotificationService>> _loggerMock;
    private readonly ConfigNotificationService _service;

    public ConfigNotificationServiceTests()
    {
        _configChangeNotifierMock = new Mock<IConfigChangeNotifier>();
        _loggerMock = new Mock<ILogger<ConfigNotificationService>>();
        _service = new ConfigNotificationService(
            _configChangeNotifierMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_ValidAppId_CallsNotifier()
    {
        // Arrange
        var appId = "test-app-001";
        _configChangeNotifierMock.Setup(n => n.NotifyConfigChangedAsync(appId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId);

        // Assert
        _configChangeNotifierMock.Verify(
            n => n.NotifyConfigChangedAsync(appId), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_EmptyAppId_StillCallsNotifier()
    {
        // Arrange
        var appId = string.Empty;
        _configChangeNotifierMock.Setup(n => n.NotifyConfigChangedAsync(appId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId);

        // Assert
        _configChangeNotifierMock.Verify(
            n => n.NotifyConfigChangedAsync(appId), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_MultipleCalls_NotifiesEachTime()
    {
        // Arrange
        var appId1 = "app-001";
        var appId2 = "app-002";
        _configChangeNotifierMock.Setup(n => n.NotifyConfigChangedAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId1);
        await _service.NotifyConfigChangedAsync(appId2);
        await _service.NotifyConfigChangedAsync(appId1);

        // Assert
        _configChangeNotifierMock.Verify(
            n => n.NotifyConfigChangedAsync(appId1), 
            Times.Exactly(2));
        _configChangeNotifierMock.Verify(
            n => n.NotifyConfigChangedAsync(appId2), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_NotifierThrowsException_PropagatesException()
    {
        // Arrange
        var appId = "test-app-001";
        _configChangeNotifierMock.Setup(n => n.NotifyConfigChangedAsync(appId))
            .ThrowsAsync(new InvalidOperationException("通知失败"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.NotifyConfigChangedAsync(appId));
    }
}

