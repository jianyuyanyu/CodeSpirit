using CodeSpirit.Shared.EventBus.Interfaces;

namespace CodeSpirit.ConfigCenter.Tests.Services;

/// <summary>
/// RabbitMQ 配置变更通知器测试
/// 注意：RabbitMQConfigChangeNotifier 类已不存在，此测试类暂时禁用
/// </summary>
public class RabbitMQConfigChangeNotifierTests
{
    // RabbitMQConfigChangeNotifier 类已不存在，所有测试暂时禁用
    // 如果将来重新实现该类，可以取消注释以下测试

    /*
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<RabbitMQConfigChangeNotifier>> _loggerMock;
    private readonly RabbitMQConfigChangeNotifier _notifier;

    public RabbitMQConfigChangeNotifierTests()
    {
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<RabbitMQConfigChangeNotifier>>();
        _notifier = new RabbitMQConfigChangeNotifier(_eventBusMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_ValidAppId_PublishesEvent()
    {
        // Arrange
        var appId = "test-app-001";
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notifier.NotifyConfigChangedAsync(appId);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(
                It.Is<ConfigChangedEvent>(evt => evt.AppId == appId)),
            Times.Once);
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_WithSubscriber_InvokesCallback()
    {
        // Arrange
        var appId = "test-app-001";
        var callbackInvoked = false;
        
        await _notifier.SubscribeAsync(appId, () =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notifier.NotifyConfigChangedAsync(appId);

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_ValidAppId_RegistersCallback()
    {
        // Arrange
        var appId = "test-app-001";
        var callbackInvoked = false;

        // Act
        await _notifier.SubscribeAsync(appId, () =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        // 触发通知以验证回调已注册
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);
        await _notifier.NotifyConfigChangedAsync(appId);

        // Assert
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_AfterSubscribe_RemovesCallback()
    {
        // Arrange
        var appId = "test-app-001";
        var callbackInvoked = false;

        await _notifier.SubscribeAsync(appId, () =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        // Act
        await _notifier.UnsubscribeAsync(appId);

        // 触发通知
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);
        await _notifier.NotifyConfigChangedAsync(appId);

        // Assert
        callbackInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_WithoutSubscriber_DoesNotThrow()
    {
        // Arrange
        var appId = "no-subscriber-app";
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _notifier.NotifyConfigChangedAsync(appId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubscribeAsync_MultipleApps_RegistersEachCallback()
    {
        // Arrange
        var app1Called = false;
        var app2Called = false;

        await _notifier.SubscribeAsync("app1", () =>
        {
            app1Called = true;
            return Task.CompletedTask;
        });

        await _notifier.SubscribeAsync("app2", () =>
        {
            app2Called = true;
            return Task.CompletedTask;
        });

        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Returns(Task.CompletedTask);

        // Act
        await _notifier.NotifyConfigChangedAsync("app1");

        // Assert
        app1Called.Should().BeTrue();
        app2Called.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyConfigChangedAsync_EventContainsCorrectTimestamp()
    {
        // Arrange
        var appId = "test-app-001";
        ConfigChangedEvent? publishedEvent = null;

        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigChangedEvent>()))
            .Callback<ConfigChangedEvent>((evt) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        var beforeCall = DateTime.UtcNow;

        // Act
        await _notifier.NotifyConfigChangedAsync(appId);

        var afterCall = DateTime.UtcNow;

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.Timestamp.Should().BeOnOrAfter(beforeCall);
        publishedEvent.Timestamp.Should().BeOnOrBefore(afterCall);
    }
    */
}
