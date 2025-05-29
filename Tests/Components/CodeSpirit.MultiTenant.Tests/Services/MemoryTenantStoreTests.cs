namespace CodeSpirit.MultiTenant.Tests.Services;

/// <summary>
/// 内存租户存储单元测试
/// </summary>
public class MemoryTenantStoreTests
{
    private readonly Mock<ILogger<MemoryTenantStore>> _loggerMock;
    private readonly MemoryTenantStore _tenantStore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MemoryTenantStoreTests()
    {
        _loggerMock = new Mock<ILogger<MemoryTenantStore>>();
        _tenantStore = new MemoryTenantStore(_loggerMock.Object);
    }

    /// <summary>
    /// 测试获取默认租户
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_ShouldReturnDefaultTenant_WhenRequestingDefaultTenant()
    {
        // Act
        var result = await _tenantStore.GetTenantAsync("default");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be("default");
        result.Name.Should().Be("默认租户");
        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// 测试获取不存在的租户
    /// </summary>
    [Fact]
    public async Task GetTenantAsync_ShouldReturnNull_WhenTenantNotExists()
    {
        // Act
        var result = await _tenantStore.GetTenantAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// 测试创建租户
    /// </summary>
    [Fact]
    public async Task CreateTenantAsync_ShouldReturnTrue_WhenCreatingNewTenant()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            Id = "test",
            TenantId = "test",
            Name = "测试租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _tenantStore.CreateTenantAsync(tenantInfo);

        // Assert
        result.Should().BeTrue();
        
        var createdTenant = await _tenantStore.GetTenantAsync("test");
        createdTenant.Should().NotBeNull();
        createdTenant!.Name.Should().Be("测试租户");
    }

    /// <summary>
    /// 测试创建重复租户
    /// </summary>
    [Fact]
    public async Task CreateTenantAsync_ShouldReturnFalse_WhenTenantAlreadyExists()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            Id = "default",
            TenantId = "default",
            Name = "重复租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _tenantStore.CreateTenantAsync(tenantInfo);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试更新租户
    /// </summary>
    [Fact]
    public async Task UpdateTenantAsync_ShouldReturnTrue_WhenUpdatingExistingTenant()
    {
        // Arrange
        var updatedTenantInfo = new TenantInfo
        {
            Id = "default",
            TenantId = "default",
            Name = "更新后的默认租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _tenantStore.UpdateTenantAsync(updatedTenantInfo);

        // Assert
        result.Should().BeTrue();
        
        var updatedTenant = await _tenantStore.GetTenantAsync("default");
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Name.Should().Be("更新后的默认租户");
    }

    /// <summary>
    /// 测试删除租户
    /// </summary>
    [Fact]
    public async Task DeleteTenantAsync_ShouldReturnTrue_WhenDeletingExistingTenant()
    {
        // Arrange
        var tenantInfo = new TenantInfo
        {
            Id = "delete-test",
            TenantId = "delete-test",
            Name = "待删除租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };
        await _tenantStore.CreateTenantAsync(tenantInfo);

        // Act
        var result = await _tenantStore.DeleteTenantAsync("delete-test");

        // Assert
        result.Should().BeTrue();
        
        var deletedTenant = await _tenantStore.GetTenantAsync("delete-test");
        deletedTenant.Should().BeNull();
    }

    /// <summary>
    /// 测试删除不存在的租户
    /// </summary>
    [Fact]
    public async Task DeleteTenantAsync_ShouldReturnFalse_WhenTenantNotExists()
    {
        // Act
        var result = await _tenantStore.DeleteTenantAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试检查租户是否存在
    /// </summary>
    [Fact]
    public async Task TenantExistsAsync_ShouldReturnTrue_WhenTenantExists()
    {
        // Act
        var result = await _tenantStore.TenantExistsAsync("default");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 测试检查不存在的租户
    /// </summary>
    [Fact]
    public async Task TenantExistsAsync_ShouldReturnFalse_WhenTenantNotExists()
    {
        // Act
        var result = await _tenantStore.TenantExistsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试获取活跃租户列表
    /// </summary>
    [Fact]
    public async Task GetActiveTenantsAsync_ShouldReturnOnlyActiveTenants()
    {
        // Arrange
        var activeTenant = new TenantInfo
        {
            Id = "active",
            TenantId = "active",
            Name = "活跃租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = true,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var inactiveTenant = new TenantInfo
        {
            Id = "inactive",
            TenantId = "inactive",
            Name = "非活跃租户",
            Strategy = TenantStrategy.SharedDatabase,
            IsActive = false,
            Configuration = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await _tenantStore.CreateTenantAsync(activeTenant);
        await _tenantStore.CreateTenantAsync(inactiveTenant);

        // Act
        var result = await _tenantStore.GetActiveTenantsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(t => t.IsActive);
        result.Should().Contain(t => t.TenantId == "default");
        result.Should().Contain(t => t.TenantId == "active");
        result.Should().NotContain(t => t.TenantId == "inactive");
    }
} 