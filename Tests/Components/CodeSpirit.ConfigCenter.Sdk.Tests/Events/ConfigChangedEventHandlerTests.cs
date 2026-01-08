namespace CodeSpirit.ConfigCenter.Sdk.Tests.Events;

/// <summary>
/// 配置变更事件处理器测试
/// </summary>
public class ConfigChangedEventHandlerTests
{
    #region ConfigChangedEvent Tests

    [Fact]
    public void ConfigChangedEvent_DefaultValues_AreCorrect()
    {
        // Act
        var @event = new ConfigChangedEvent();

        // Assert
        @event.AppId.Should().BeEmpty();
        @event.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        @event.ChangedKey.Should().BeNull();
    }

    [Fact]
    public void ConfigChangedEvent_SetAppId_ReturnsCorrectValue()
    {
        // Arrange
        var expectedAppId = "test-app-001";

        // Act
        var @event = new ConfigChangedEvent { AppId = expectedAppId };

        // Assert
        @event.AppId.Should().Be(expectedAppId);
    }

    [Fact]
    public void ConfigChangedEvent_SetTimestamp_ReturnsCorrectValue()
    {
        // Arrange
        var expectedTimestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var @event = new ConfigChangedEvent { Timestamp = expectedTimestamp };

        // Assert
        @event.Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void ConfigChangedEvent_SetChangedKey_ReturnsCorrectValue()
    {
        // Arrange
        var expectedKey = "database:connection";

        // Act
        var @event = new ConfigChangedEvent { ChangedKey = expectedKey };

        // Assert
        @event.ChangedKey.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("app-1", "key1")]
    [InlineData("service-a", "config:nested:value")]
    [InlineData("my-application", null)]
    public void ConfigChangedEvent_AllProperties_ContainCorrectValues(string appId, string? changedKey)
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

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

    #endregion

    #region Event Matching Logic Tests

    [Fact]
    public void EventAppIdMatching_SameAppId_ReturnsTrue()
    {
        // Arrange
        var currentAppId = "my-app";
        var eventAppId = "my-app";

        // Act
        var isMatch = !string.IsNullOrEmpty(currentAppId) && currentAppId == eventAppId;

        // Assert
        isMatch.Should().BeTrue();
    }

    [Fact]
    public void EventAppIdMatching_DifferentAppId_ReturnsFalse()
    {
        // Arrange
        var currentAppId = "my-app";
        var eventAppId = "other-app";

        // Act
        var isMatch = !string.IsNullOrEmpty(currentAppId) && currentAppId == eventAppId;

        // Assert
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void EventAppIdMatching_EmptyCurrentAppId_ReturnsFalse()
    {
        // Arrange
        var currentAppId = string.Empty;
        var eventAppId = "some-app";

        // Act
        var isMatch = !string.IsNullOrEmpty(currentAppId) && currentAppId == eventAppId;

        // Assert
        isMatch.Should().BeFalse();
    }

    [Fact]
    public void EventAppIdMatching_NullCurrentAppId_ReturnsFalse()
    {
        // Arrange
        string? currentAppId = null;
        var eventAppId = "some-app";

        // Act
        var isMatch = !string.IsNullOrEmpty(currentAppId) && currentAppId == eventAppId;

        // Assert
        isMatch.Should().BeFalse();
    }

    [Theory]
    [InlineData("app-1", "app-1", true)]
    [InlineData("app-1", "app-2", false)]
    [InlineData("", "app-1", false)]
    [InlineData("app-1", "", false)]
    public void EventAppIdMatching_VariousCases_ReturnsExpectedResult(
        string currentAppId, string eventAppId, bool expectedMatch)
    {
        // Act
        var isMatch = !string.IsNullOrEmpty(currentAppId) && currentAppId == eventAppId;

        // Assert
        isMatch.Should().Be(expectedMatch);
    }

    #endregion

    #region ConfigItemsExportDto Tests

    [Fact]
    public void ConfigItemsExportDto_EmptyConfigs_HasZeroItems()
    {
        // Arrange & Act
        var dto = new ConfigItemsExportDto
        {
            AppId = "test-app",
            Configs = new Dictionary<string, object>()
        };

        // Assert
        dto.Configs.Should().BeEmpty();
    }

    [Fact]
    public void ConfigItemsExportDto_WithConfigs_ContainsAllItems()
    {
        // Arrange
        var configs = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true }
        };

        // Act
        var dto = new ConfigItemsExportDto
        {
            AppId = "test-app",
            Configs = configs
        };

        // Assert
        dto.Configs.Should().HaveCount(3);
        dto.Configs["key1"].Should().Be("value1");
        dto.Configs["key2"].Should().Be(42);
        dto.Configs["key3"].Should().Be(true);
    }

    [Fact]
    public void ConfigItemsExportDto_AppId_IsSetCorrectly()
    {
        // Arrange
        var expectedAppId = "my-application";

        // Act
        var dto = new ConfigItemsExportDto
        {
            AppId = expectedAppId,
            Configs = new Dictionary<string, object>()
        };

        // Assert
        dto.AppId.Should().Be(expectedAppId);
    }

    #endregion
}
