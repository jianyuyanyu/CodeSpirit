namespace CodeSpirit.ConfigCenter.Tests.Events;

/// <summary>
/// 配置变更事件测试
/// </summary>
public class ConfigChangedEventTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsCorrectDefaults()
    {
        // Act
        var @event = new ConfigChangedEvent();

        // Assert
        @event.AppId.Should().BeEmpty();
        @event.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.ChangedKey.Should().BeNull();
    }

    [Fact]
    public void AppId_SetValue_ReturnsCorrectValue()
    {
        // Arrange
        var expectedAppId = "test-app-001";

        // Act
        var @event = new ConfigChangedEvent { AppId = expectedAppId };

        // Assert
        @event.AppId.Should().Be(expectedAppId);
    }

    [Fact]
    public void Timestamp_SetValue_ReturnsCorrectValue()
    {
        // Arrange
        var expectedTimestamp = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var @event = new ConfigChangedEvent { Timestamp = expectedTimestamp };

        // Assert
        @event.Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void ChangedKey_SetValue_ReturnsCorrectValue()
    {
        // Arrange
        var expectedKey = "SomeConfigKey";

        // Act
        var @event = new ConfigChangedEvent { ChangedKey = expectedKey };

        // Assert
        @event.ChangedKey.Should().Be(expectedKey);
    }

    [Fact]
    public void Event_WithAllProperties_ContainsAllValues()
    {
        // Arrange
        var appId = "my-app";
        var timestamp = DateTime.UtcNow;
        var changedKey = "database:connectionString";

        // Act
        var @event = new ConfigChangedEvent
        {
            AppId = appId,
            Timestamp = timestamp,
            ChangedKey = changedKey
        };

        // Assert
        @event.AppId.Should().Be(appId);
        @event.Timestamp.Should().Be(timestamp);
        @event.ChangedKey.Should().Be(changedKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("app-1")]
    [InlineData("very-long-app-id-with-many-characters-123456789")]
    public void AppId_VariousValues_AcceptsAll(string appId)
    {
        // Act
        var @event = new ConfigChangedEvent { AppId = appId };

        // Assert
        @event.AppId.Should().Be(appId);
    }
}

