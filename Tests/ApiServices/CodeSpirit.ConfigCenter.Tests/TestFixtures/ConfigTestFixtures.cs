namespace CodeSpirit.ConfigCenter.Tests.TestFixtures;

/// <summary>
/// 配置中心测试数据生成器
/// </summary>
public static class ConfigTestFixtures
{
    /// <summary>
    /// 创建有效的应用实体
    /// </summary>
    public static App CreateValidApp(string? id = null, string? name = null)
    {
        return new App
        {
            Id = id ?? "test-app-001",
            Name = name ?? "测试应用",
            Description = "测试应用描述",
            Enabled = true,
            Secret = Guid.NewGuid().ToString("N")
        };
    }

    /// <summary>
    /// 创建有效的配置项实体
    /// </summary>
    public static ConfigItem CreateValidConfigItem(
        int id = 1,
        string appId = "test-app-001",
        string key = "TestKey",
        string value = "TestValue",
        ConfigValueType valueType = ConfigValueType.String,
        ConfigStatus status = ConfigStatus.Released)
    {
        return new ConfigItem
        {
            Id = id,
            AppId = appId,
            Key = key,
            Value = value,
            ValueType = valueType,
            Status = status,
            Version = 1,
            Group = "default",
            Description = "测试配置项"
        };
    }

    /// <summary>
    /// 创建有效的配置项DTO
    /// </summary>
    public static ConfigItemDto CreateValidConfigItemDto(
        int id = 1,
        string appId = "test-app-001",
        string key = "TestKey",
        string value = "TestValue")
    {
        return new ConfigItemDto
        {
            Id = id,
            AppId = appId,
            Key = key,
            Value = value,
            ValueType = ConfigValueType.String,
            Status = ConfigStatus.Released,
            Version = 1
        };
    }

    /// <summary>
    /// 创建有效的创建配置项DTO
    /// </summary>
    public static CreateConfigDto CreateValidCreateConfigDto(
        string appId = "test-app-001",
        string key = "NewKey",
        string value = "NewValue")
    {
        return new CreateConfigDto
        {
            AppId = appId,
            Key = key,
            Value = value,
            ValueType = ConfigValueType.String,
            Status = ConfigStatus.Editing,
            Group = "default",
            Description = "新配置项"
        };
    }

    /// <summary>
    /// 创建有效的更新配置项DTO
    /// </summary>
    public static UpdateConfigDto CreateValidUpdateConfigDto(
        string value = "UpdatedValue")
    {
        return new UpdateConfigDto
        {
            Value = value,
            ValueType = ConfigValueType.String,
            Description = "更新后的配置项"
        };
    }

    /// <summary>
    /// 创建有效的发布历史实体
    /// </summary>
    public static ConfigPublishHistory CreateValidPublishHistory(
        int id = 1,
        string appId = "test-app-001",
        long version = 1)
    {
        return new ConfigPublishHistory
        {
            Id = id,
            AppId = appId,
            Version = version,
            Description = "测试发布"
        };
    }

    /// <summary>
    /// 创建配置项列表
    /// </summary>
    public static List<ConfigItem> CreateConfigItemList(
        string appId = "test-app-001",
        int count = 3)
    {
        var items = new List<ConfigItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(CreateValidConfigItem(
                id: i,
                appId: appId,
                key: $"Key{i}",
                value: $"Value{i}"
            ));
        }
        return items;
    }

    /// <summary>
    /// 创建不同类型的配置项
    /// </summary>
    public static List<ConfigItem> CreateMixedTypeConfigItems(string appId = "test-app-001")
    {
        return new List<ConfigItem>
        {
            CreateValidConfigItem(1, appId, "StringConfig", "StringValue", ConfigValueType.String),
            CreateValidConfigItem(2, appId, "IntConfig", "42", ConfigValueType.Int),
            CreateValidConfigItem(3, appId, "BoolConfig", "true", ConfigValueType.Boolean),
            CreateValidConfigItem(4, appId, "DoubleConfig", "3.14", ConfigValueType.Double),
            CreateValidConfigItem(5, appId, "JsonConfig", "{\"key\":\"value\"}", ConfigValueType.Json)
        };
    }
}

