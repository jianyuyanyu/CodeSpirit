using CodeSpirit.ConfigCenter.Events;
using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.ConfigCenter.Tests.Services;

/// <summary>
/// 配置通知服务测试
/// </summary>
public class ConfigNotificationServiceTests
{
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<ConfigNotificationService>> _loggerMock;
    private readonly ConfigNotificationService _service;

    public ConfigNotificationServiceTests()
    {
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<ConfigNotificationService>>();
        _service = new ConfigNotificationService(
            _eventBusMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_ValidAppId_PublishesEvent()
    {
        // Arrange
        var appId = "test-app-001";
        var version = 1L;
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId, version);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(
                It.Is<ConfigChangedEvent>(evt => evt.AppId == appId && evt.Version == version)), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_EmptyAppId_StillPublishesEvent()
    {
        // Arrange
        var appId = string.Empty;
        var version = 1L;
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId, version);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_MultipleCalls_PublishesEachTime()
    {
        // Arrange
        var appId1 = "app-001";
        var appId2 = "app-002";
        var version = 1L;
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.NotifyConfigChangedAsync(appId1, version);
        await _service.NotifyConfigChangedAsync(appId2, version);
        await _service.NotifyConfigChangedAsync(appId1, version + 1);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(It.Is<ConfigChangedEvent>(evt => evt.AppId == appId1)), 
            Times.Exactly(2));
        _eventBusMock.Verify(
            e => e.PublishAsync(It.Is<ConfigChangedEvent>(evt => evt.AppId == appId2)), 
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_EventBusThrowsException_PropagatesException()
    {
        // Arrange
        var appId = "test-app-001";
        var version = 1L;
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .ThrowsAsync(new InvalidOperationException("发布失败"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.NotifyConfigChangedAsync(appId, version));
    }
}

