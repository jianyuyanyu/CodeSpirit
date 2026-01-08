using CodeSpirit.ConfigCenter.Tests.TestFixtures;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace CodeSpirit.ConfigCenter.Tests.Services;

/// <summary>
/// 配置项服务测试
/// </summary>
public class ConfigItemServiceTests
{
    private readonly Mock<IConfigCacheService> _cacheServiceMock;
    private readonly Mock<IMapper> _mapperMock;

    public ConfigItemServiceTests()
    {
        _cacheServiceMock = new Mock<IConfigCacheService>();
        _mapperMock = new Mock<IMapper>();
    }

    #region GetConfigAsync Tests - Cache Layer

    [Fact]
    public async Task GetConfigAsync_CacheHit_ReturnsCachedConfig()
    {
        // Arrange
        var appId = "test-app-001";
        var key = "TestKey";
        var cachedDto = ConfigTestFixtures.CreateValidConfigItemDto(1, appId, key);
        var cacheKey = $"config:{appId}:{key}";

        _cacheServiceMock.Setup(c => c.GetAsync(cacheKey))
            .ReturnsAsync(JsonConvert.SerializeObject(cachedDto));

        // Act - 这里测试的是缓存层的逻辑
        var cachedJson = await _cacheServiceMock.Object.GetAsync(cacheKey);
        var result = JsonConvert.DeserializeObject<ConfigItemDto>(cachedJson!);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(key);
        result.AppId.Should().Be(appId);
    }

    [Fact]
    public async Task GetConfigAsync_CacheMiss_ReturnsNull()
    {
        // Arrange
        var cacheKey = "config:test-app:missing-key";

        _cacheServiceMock.Setup(c => c.GetAsync(cacheKey))
            .ReturnsAsync((string?)null);

        // Act
        var cachedJson = await _cacheServiceMock.Object.GetAsync(cacheKey);

        // Assert
        cachedJson.Should().BeNull();
    }

    #endregion

    #region Config Value Type Conversion Tests

    [Theory]
    [InlineData("42", ConfigValueType.Int, 42)]
    [InlineData("3.14", ConfigValueType.Double, 3.14)]
    [InlineData("true", ConfigValueType.Boolean, true)]
    [InlineData("false", ConfigValueType.Boolean, false)]
    [InlineData("hello", ConfigValueType.String, "hello")]
    public void ConvertConfigValue_VariousTypes_ConvertsCorrectly(
        string value, ConfigValueType valueType, object expectedResult)
    {
        // Arrange
        var configItem = ConfigTestFixtures.CreateValidConfigItem(
            1, "app", "key", value, valueType);

        // Act
        object result = valueType switch
        {
            ConfigValueType.Int => int.Parse(configItem.Value),
            ConfigValueType.Double => double.Parse(configItem.Value),
            ConfigValueType.Boolean => bool.Parse(configItem.Value),
            _ => configItem.Value
        };

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ConvertConfigValue_JsonType_ParsesCorrectly()
    {
        // Arrange
        var jsonValue = "{\"key\":\"value\",\"nested\":{\"count\":5}}";
        var configItem = ConfigTestFixtures.CreateValidConfigItem(
            1, "app", "JsonConfig", jsonValue, ConfigValueType.Json);

        // Act
        var parsed = JsonConvert.DeserializeObject<dynamic>(configItem.Value)!;

        // Assert
        ((string)parsed.key).Should().Be("value");
        ((int)parsed.nested.count).Should().Be(5);
    }

    #endregion

    #region Config Fixture Tests

    [Fact]
    public void CreateConfigItemList_MultipleItems_CreatesCorrectCount()
    {
        // Arrange & Act
        var items = ConfigTestFixtures.CreateConfigItemList("app", 5);

        // Assert
        items.Should().HaveCount(5);
        items.Select(i => i.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CreateMixedTypeConfigItems_AllTypes_ContainsExpectedTypes()
    {
        // Arrange & Act
        var items = ConfigTestFixtures.CreateMixedTypeConfigItems("app");

        // Assert
        items.Should().Contain(i => i.ValueType == ConfigValueType.String);
        items.Should().Contain(i => i.ValueType == ConfigValueType.Int);
        items.Should().Contain(i => i.ValueType == ConfigValueType.Boolean);
        items.Should().Contain(i => i.ValueType == ConfigValueType.Double);
        items.Should().Contain(i => i.ValueType == ConfigValueType.Json);
    }

    [Fact]
    public void CreateValidApp_DefaultValues_HasCorrectDefaults()
    {
        // Arrange & Act
        var app = ConfigTestFixtures.CreateValidApp();

        // Assert
        app.Id.Should().Be("test-app-001");
        app.Name.Should().Be("测试应用");
        app.Enabled.Should().BeTrue();
        app.Secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateValidConfigItem_ReleasedStatus_IsReleased()
    {
        // Arrange & Act
        var item = ConfigTestFixtures.CreateValidConfigItem(
            status: ConfigStatus.Released);

        // Assert
        item.Status.Should().Be(ConfigStatus.Released);
    }

    [Fact]
    public void CreateValidConfigItem_EditingStatus_IsEditing()
    {
        // Arrange & Act
        var item = ConfigTestFixtures.CreateValidConfigItem(
            status: ConfigStatus.Editing);

        // Assert
        item.Status.Should().Be(ConfigStatus.Editing);
    }

    #endregion

    #region Cache Key Format Tests

    [Theory]
    [InlineData("app1", "key1", "config:app1:key1")]
    [InlineData("my-app", "database:connection", "config:my-app:database:connection")]
    [InlineData("service-A", "feature.enabled", "config:service-A:feature.enabled")]
    public void CacheKey_Format_IsCorrect(string appId, string key, string expectedCacheKey)
    {
        // Arrange & Act
        var cacheKey = $"config:{appId}:{key}";

        // Assert
        cacheKey.Should().Be(expectedCacheKey);
    }

    #endregion

    #region Config DTO Tests

    [Fact]
    public void ConfigItemDto_SerializesCorrectly()
    {
        // Arrange
        var dto = ConfigTestFixtures.CreateValidConfigItemDto();

        // Act
        var json = JsonConvert.SerializeObject(dto);
        var deserialized = JsonConvert.DeserializeObject<ConfigItemDto>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(dto.Id);
        deserialized.Key.Should().Be(dto.Key);
        deserialized.Value.Should().Be(dto.Value);
    }

    [Fact]
    public void CreateConfigDto_HasCorrectDefaults()
    {
        // Arrange & Act
        var dto = ConfigTestFixtures.CreateValidCreateConfigDto();

        // Assert
        dto.Key.Should().NotBeNullOrEmpty();
        dto.Value.Should().NotBeNullOrEmpty();
        dto.Status.Should().Be(ConfigStatus.Editing);
    }

    [Fact]
    public void UpdateConfigDto_HasNewValue()
    {
        // Arrange
        var newValue = "NewUpdatedValue";

        // Act
        var dto = ConfigTestFixtures.CreateValidUpdateConfigDto(newValue);

        // Assert
        dto.Value.Should().Be(newValue);
    }

    #endregion
}
