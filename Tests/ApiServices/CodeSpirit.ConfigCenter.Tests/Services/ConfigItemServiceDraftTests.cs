using CodeSpirit.ConfigCenter.Tests.TestFixtures;
using CodeSpirit.ConfigCenter.Tests.TestHelpers;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ConfigCenter.Dtos.PublishHistory;
using CodeSpirit.Core;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Dtos.Common;
using CodeSpirit.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CodeSpirit.ConfigCenter.Tests.Services;

/// <summary>
/// 配置项服务草稿机制和版本号测试
/// </summary>
public class ConfigItemServiceDraftTests
{
    private readonly Mock<IRepository<ConfigItem>> _repositoryMock;
    private readonly Mock<IRepository<App>> _appRepositoryMock;
    private readonly Mock<IConfigCacheService> _cacheServiceMock;
    private readonly Mock<IConfigNotificationService> _notificationServiceMock;
    private readonly Mock<IConfigPublishHistoryService> _publishHistoryServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ConfigItemService>> _loggerMock;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly Mock<ILogger<EnhancedBatchImportHelper<ConfigItemBatchImportDto>>> _importHelperLoggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly ConfigItemService _service;
    private readonly List<ConfigItem> _inMemoryData;
    private TestDbContext? _testDbContext;

    public ConfigItemServiceDraftTests()
    {
        _repositoryMock = new Mock<IRepository<ConfigItem>>();
        _appRepositoryMock = new Mock<IRepository<App>>();
        _cacheServiceMock = new Mock<IConfigCacheService>();
        _notificationServiceMock = new Mock<IConfigNotificationService>();
        _publishHistoryServiceMock = new Mock<IConfigPublishHistoryService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ConfigItemService>>();
        _distributedCacheMock = new Mock<IDistributedCache>();
        _importHelperLoggerMock = new Mock<ILogger<EnhancedBatchImportHelper<ConfigItemBatchImportDto>>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _inMemoryData = new List<ConfigItem>();

        // 创建 EnhancedBatchImportHelper 实例（需要提供构造函数参数）
        var importHelper = new EnhancedBatchImportHelper<ConfigItemBatchImportDto>(
            _distributedCacheMock.Object,
            _importHelperLoggerMock.Object);

        _service = new ConfigItemService(
            _repositoryMock.Object,
            _appRepositoryMock.Object,
            _cacheServiceMock.Object,
            _notificationServiceMock.Object,
            _publishHistoryServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            importHelper,
            _serviceProviderMock.Object);
    }

    #region 草稿机制测试

    /// <summary>
    /// 测试：编辑已发布配置时应创建草稿
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReleasedConfig_ShouldCreateDraft()
    {
        // Arrange
        var configId = 1;
        var publishedConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: configId,
            appId: "test-app",
            key: "TestKey",
            value: "OriginalValue",
            status: ConfigStatus.Released,
            valueType: ConfigValueType.String);
        publishedConfig.Version = 5;

        var updateDto = ConfigTestFixtures.CreateValidUpdateConfigDto("NewValue");

        _repositoryMock.Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync(publishedConfig);

        // 模拟没有现有草稿
        SetupFindMock(new List<ConfigItem>());

        _mapperMock.Setup(m => m.Map(updateDto, It.IsAny<ConfigItem>()))
            .Returns((UpdateConfigDto dto, ConfigItem entity) =>
            {
                entity.Value = dto.Value;
                entity.ValueType = dto.ValueType;
                entity.Description = dto.Description;
                entity.Group = dto.Group;
                return entity;
            });

        // Act
        await _service.UpdateAsync(configId, updateDto);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ConfigItem>(c =>
            c.AppId == publishedConfig.AppId &&
            c.Key == publishedConfig.Key &&
            c.Value == updateDto.Value &&
            c.Status == ConfigStatus.Editing &&
            c.Version == publishedConfig.Version // 版本号应保持不变
        )), Times.Once);

        // 不应更新已发布的配置
        _repositoryMock.Verify(r => r.UpdateAsync(publishedConfig), Times.Never);
    }

    /// <summary>
    /// 测试：编辑已发布配置时如果已存在草稿，应更新现有草稿
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ReleasedConfig_WithExistingDraft_ShouldUpdateDraft()
    {
        // Arrange
        var configId = 1;
        var draftId = 2;
        var publishedConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: configId,
            appId: "test-app",
            key: "TestKey",
            value: "OriginalValue",
            status: ConfigStatus.Released);
        publishedConfig.Version = 5;

        var existingDraft = ConfigTestFixtures.CreateValidConfigItem(
            id: draftId,
            appId: "test-app",
            key: "TestKey",
            value: "OldDraftValue",
            status: ConfigStatus.Editing);
        existingDraft.Version = 5; // 版本号与已发布配置相同

        var updateDto = ConfigTestFixtures.CreateValidUpdateConfigDto("NewDraftValue");

        // 确保数据在内存中
        _inMemoryData.Clear();
        _inMemoryData.Add(publishedConfig);
        _inMemoryData.Add(existingDraft);
        
        SetupGetByIdMock(publishedConfig);
        SetupFindMock(new List<ConfigItem> { existingDraft }); // 存在草稿

        ConfigItem? updatedDraft = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigItem>(), It.IsAny<bool>()))
            .Callback<ConfigItem, bool>((item, _) => updatedDraft = item)
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map(updateDto, It.IsAny<ConfigItem>()))
            .Returns((UpdateConfigDto dto, ConfigItem entity) =>
            {
                entity.Value = dto.Value;
                entity.ValueType = dto.ValueType;
                entity.Description = dto.Description;
                entity.Group = dto.Group;
                return entity;
            });

        // Act
        await _service.UpdateAsync(configId, updateDto);

        // Assert
        updatedDraft.Should().NotBeNull();
        updatedDraft!.Id.Should().Be(draftId);
        updatedDraft.Value.Should().Be(updateDto.Value);

        // 不应创建新草稿
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ConfigItem>()), Times.Never);
    }

    /// <summary>
    /// 测试：编辑草稿配置时应直接更新
    /// </summary>
    [Fact]
    public async Task UpdateAsync_DraftConfig_ShouldUpdateDirectly()
    {
        // Arrange
        var configId = 1;
        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: configId,
            appId: "test-app",
            key: "TestKey",
            value: "OldValue",
            status: ConfigStatus.Editing);
        draftConfig.Version = 3;

        var updateDto = ConfigTestFixtures.CreateValidUpdateConfigDto("NewValue");

        SetupGetByIdMock(draftConfig);

        ConfigItem? updatedConfig = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigItem>(), It.IsAny<bool>()))
            .Callback<ConfigItem, bool>((item, _) => updatedConfig = item)
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map(updateDto, It.IsAny<ConfigItem>()))
            .Returns((UpdateConfigDto dto, ConfigItem entity) =>
            {
                entity.Value = dto.Value;
                entity.ValueType = dto.ValueType;
                entity.Description = dto.Description;
                entity.Group = dto.Group;
                entity.Status = ConfigStatus.Editing;
                return entity;
            });

        // Act
        await _service.UpdateAsync(configId, updateDto);

        // Assert
        updatedConfig.Should().NotBeNull();
        updatedConfig!.Id.Should().Be(configId);
        updatedConfig.Value.Should().Be(updateDto.Value);
        updatedConfig.Status.Should().Be(ConfigStatus.Editing);
        updatedConfig.Version.Should().Be(draftConfig.Version); // 版本号应保持不变

        // 不应创建新草稿
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ConfigItem>()), Times.Never);
    }

    #endregion

    #region 版本号更新测试

    /// <summary>
    /// 测试：发布草稿时版本号应增加
    /// </summary>
    [Fact]
    public async Task BatchPublishAsync_DraftConfig_ShouldIncrementVersion()
    {
        // Arrange
        var draftId = 1;
        var publishedId = 2;
        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: draftId,
            appId: "test-app",
            key: "TestKey",
            value: "DraftValue",
            status: ConfigStatus.Editing);
        draftConfig.Version = 5;

        var publishedConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: publishedId,
            appId: "test-app",
            key: "TestKey",
            value: "OldPublishedValue",
            status: ConfigStatus.Released);
        publishedConfig.Version = 5;

        var publishDto = new ConfigItemsBatchPublishDto
        {
            Ids = new List<int> { draftId },
            Description = "发布测试"
        };

        // 设置 Find Mock，使用 TestDbContext 支持异步操作
        SetupFindMock(new List<ConfigItem> { draftConfig, publishedConfig });

        ConfigItem? updatedPublishedConfig = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigItem>(), It.IsAny<bool>()))
            .Callback<ConfigItem, bool>((item, _) => updatedPublishedConfig = item)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) => await action());

        // Act
        var result = await _service.BatchPublishAsync(publishDto);

        // Assert
        result.successCount.Should().Be(1);
        result.failedIds.Should().BeEmpty();

        // 验证版本号已增加
        updatedPublishedConfig.Should().NotBeNull();
        updatedPublishedConfig!.Id.Should().Be(publishedId);
        updatedPublishedConfig.Version.Should().Be(6); // 版本号应增加1
    }

    /// <summary>
    /// 测试：首次发布配置时版本号应增加
    /// </summary>
    [Fact]
    public async Task BatchPublishAsync_FirstTimePublish_ShouldIncrementVersion()
    {
        // Arrange
        var configId = 1;
        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: configId,
            appId: "test-app",
            key: "TestKey",
            value: "DraftValue",
            status: ConfigStatus.Editing);
        draftConfig.Version = 0; // 初始版本

        var publishDto = new ConfigItemsBatchPublishDto
        {
            Ids = new List<int> { configId },
            Description = "首次发布"
        };

        // 设置 Find Mock，使用 TestDbContext 支持异步操作
        SetupFindMock(new List<ConfigItem> { draftConfig });

        ConfigItem? updatedConfig = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigItem>(), It.IsAny<bool>()))
            .Callback<ConfigItem, bool>((item, _) => updatedConfig = item)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) => await action());

        // Act
        var result = await _service.BatchPublishAsync(publishDto);

        // Assert
        result.successCount.Should().Be(1);

        // 验证版本号已增加
        updatedConfig.Should().NotBeNull();
        updatedConfig!.Id.Should().Be(configId);
        updatedConfig.Version.Should().Be(1); // 版本号应为1（0+1）
        updatedConfig.Status.Should().Be(ConfigStatus.Released);
    }

    #endregion

    #region 草稿标识字段测试

    /// <summary>
    /// 测试：获取配置列表时应正确设置草稿标识
    /// </summary>
    [Fact]
    public async Task GetConfigsAsync_WithDraft_ShouldSetDraftInfo()
    {
        // Arrange
        var publishedId = 1;
        var draftId = 2;
        var publishedConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: publishedId,
            appId: "test-app",
            key: "TestKey",
            value: "PublishedValue",
            status: ConfigStatus.Released);

        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: draftId,
            appId: "test-app",
            key: "TestKey",
            value: "DraftValue",
            status: ConfigStatus.Editing);

        var publishedDto = new ConfigItemDto
        {
            Id = publishedId,
            AppId = "test-app",
            Key = "TestKey",
            Value = "PublishedValue",
            Status = ConfigStatus.Released
        };

        var draftDto = new ConfigItemDto
        {
            Id = draftId,
            AppId = "test-app",
            Key = "TestKey",
            Value = "DraftValue",
            Status = ConfigStatus.Editing
        };

        var queryDto = new ConfigItemQueryDto();
        var pageList = new PageList<ConfigItemDto>
        {
            Items = new List<ConfigItemDto> { publishedDto, draftDto },
            Total = 2
        };

        SetupFindMock(new List<ConfigItem> { publishedConfig, draftConfig });

        _mapperMock.Setup(m => m.Map<PageList<ConfigItemDto>>(It.IsAny<PageList<ConfigItem>>()))
            .Returns(pageList);

        // Act
        var result = await _service.GetConfigsAsync(queryDto);

        // Assert
        var draftItem = result.Items.FirstOrDefault(i => i.Id == draftId);
        draftItem.Should().NotBeNull();
        draftItem!.IsDraft.Should().BeTrue();
        draftItem.PublishedConfigId.Should().Be(publishedId);

        var publishedItem = result.Items.FirstOrDefault(i => i.Id == publishedId);
        publishedItem.Should().NotBeNull();
        publishedItem!.IsDraft.Should().BeFalse();
    }

    #endregion

    #region 发布流程测试

    /// <summary>
    /// 测试：发布草稿时应删除草稿记录
    /// </summary>
    [Fact]
    public async Task BatchPublishAsync_ShouldDeleteDraftAfterPublish()
    {
        // Arrange
        var draftId = 1;
        var publishedId = 2;
        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: draftId,
            appId: "test-app",
            key: "TestKey",
            value: "DraftValue",
            status: ConfigStatus.Editing);

        var publishedConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: publishedId,
            appId: "test-app",
            key: "TestKey",
            value: "OldPublishedValue",
            status: ConfigStatus.Released);

        var publishDto = new ConfigItemsBatchPublishDto
        {
            Ids = new List<int> { draftId },
            Description = "发布测试"
        };

        SetupFindMock(new List<ConfigItem> { draftConfig, publishedConfig });

        _repositoryMock.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) => await action());

        // Act
        await _service.BatchPublishAsync(publishDto);

        // Assert
        // 验证草稿已被删除
        _repositoryMock.Verify(r => r.DeleteAsync(It.Is<ConfigItem>(c =>
            c.Id == draftId
        )), Times.Once);
    }

    /// <summary>
    /// 测试：发布时应清除缓存
    /// </summary>
    [Fact]
    public async Task BatchPublishAsync_ShouldClearCache()
    {
        // Arrange
        var draftId = 1;
        var draftConfig = ConfigTestFixtures.CreateValidConfigItem(
            id: draftId,
            appId: "test-app",
            key: "TestKey",
            value: "DraftValue",
            status: ConfigStatus.Editing);

        var publishDto = new ConfigItemsBatchPublishDto
        {
            Ids = new List<int> { draftId },
            Description = "发布测试"
        };

        SetupFindMock(new List<ConfigItem> { draftConfig });

        _repositoryMock.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) => await action());

        // Act
        await _service.BatchPublishAsync(publishDto);

        // Assert
        // 验证缓存已清除
        _cacheServiceMock.Verify(c => c.RemoveAsync(
            It.Is<string>(key => key.Contains("test-app") && key.Contains("TestKey"))
        ), Times.AtLeastOnce);
    }

    #endregion

    /// <summary>
    /// 设置 Find 方法的 Mock - 返回支持异步操作的 IQueryable
    /// </summary>
    private void SetupFindMock(List<ConfigItem> data)
    {
        _inMemoryData.Clear();
        _inMemoryData.AddRange(data);

        // 释放旧的上下文
        _testDbContext?.Dispose();

        // 创建新的上下文实例（不使用 using，保持上下文存活）
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _testDbContext = new TestDbContext(options);
        _testDbContext.ConfigItems.AddRange(_inMemoryData);
        _testDbContext.SaveChanges();

        _repositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<ConfigItem, bool>>>()))
            .Returns((Expression<Func<ConfigItem, bool>> predicate) =>
            {
                // 确保数据是最新的
                if (_testDbContext != null)
                {
                    // 清除旧数据并重新添加
                    _testDbContext.ConfigItems.RemoveRange(_testDbContext.ConfigItems);
                    _testDbContext.SaveChanges();
                    _testDbContext.ConfigItems.AddRange(_inMemoryData);
                    _testDbContext.SaveChanges();
                    
                    // 返回 DbSet 的 IQueryable，支持异步操作
                    return _testDbContext.ConfigItems.Where(predicate);
                }
                
                // 如果上下文不存在，创建新的
                var newOptions = new DbContextOptionsBuilder<TestDbContext>()
                    .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                    .Options;
                _testDbContext = new TestDbContext(newOptions);
                _testDbContext.ConfigItems.AddRange(_inMemoryData);
                _testDbContext.SaveChanges();
                return _testDbContext.ConfigItems.Where(predicate);
            });
    }

    /// <summary>
    /// 设置 GetByIdAsync 方法的 Mock
    /// </summary>
    private void SetupGetByIdMock(ConfigItem? item)
    {
        if (item != null)
        {
            _inMemoryData.RemoveAll(x => x.Id == item.Id);
            _inMemoryData.Add(item);
        }

        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => 
            {
                var found = _inMemoryData.FirstOrDefault(x => x.Id == id);
                if (found != null) return found;
                // 如果找不到且传入的 item 的 ID 匹配，返回 item
                if (item != null && item.Id == id) return item;
                return null!; // 返回 null，让服务层处理
            });
    }
}
