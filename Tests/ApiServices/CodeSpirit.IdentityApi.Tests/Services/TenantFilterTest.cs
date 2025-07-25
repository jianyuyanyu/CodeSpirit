using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using CodeSpirit.Core.IdGenerator;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Services;

/// <summary>
/// 租户筛选器测试
/// </summary>
public class TenantFilterTest : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleService _roleService;
    private readonly ServiceProvider _serviceProvider;

    public TenantFilterTest()
    {
        var services = new ServiceCollection();

        // 配置内存数据库
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // 注册必要的服务
        services.AddScoped<IDataFilter, DataFilter>();
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(IMultiTenant)] = new DataFilterState(isEnabled: true);
        });

        // 模拟HttpContext和当前用户
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockCurrentUser = new Mock<ICurrentUser>();
        
        mockCurrentUser.Setup(x => x.TenantId).Returns("tenant1");
        mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        
        services.AddSingleton(mockHttpContextAccessor.Object);
        services.AddSingleton(mockCurrentUser.Object);

        // 注册其他必要服务
        services.AddScoped(typeof(Repository<>));
        services.AddAutoMapper(typeof(Program));
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddLogging();
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // 创建RoleService
        var repository = new Repository<ApplicationRole>(_dbContext);
        var mapper = _serviceProvider.GetRequiredService<IMapper>();
        var cache = _serviceProvider.GetRequiredService<IDistributedCache>();
        var logger = _serviceProvider.GetRequiredService<ILogger<RoleService>>();
        var idGenerator = _serviceProvider.GetRequiredService<IIdGenerator>();
        var userRepository = new Repository<ApplicationUser>(_dbContext);
        
        _roleService = new RoleService(repository, mapper, cache, logger, idGenerator, userRepository);

        // 初始化测试数据
        InitializeTestData();
    }

    private void InitializeTestData()
    {
        // 创建不同租户的角色数据
        var roles = new List<ApplicationRole>
        {
            new ApplicationRole
            {
                Id = 1,
                Name = "Tenant1Admin",
                TenantId = "tenant1",
                Description = "租户1管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1
            },
            new ApplicationRole
            {
                Id = 2,
                Name = "Tenant1User",
                TenantId = "tenant1",
                Description = "租户1用户",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1
            },
            new ApplicationRole
            {
                Id = 3,
                Name = "Tenant2Admin",
                TenantId = "tenant2",
                Description = "租户2管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1
            }
        };

        _dbContext.Roles.AddRange(roles);
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// 测试租户筛选器是否正确工作
    /// </summary>
    [Fact]
    public async Task GetRolesAsync_ShouldFilterByCurrentTenant()
    {
        // Arrange
        var queryDto = new RoleQueryDto { Page = 1, PerPage = 10 };

        // Act
        var result = await _roleService.GetRolesAsync(queryDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Total); // 应该只返回tenant1的2个角色
        Assert.All(result.Items, role => Assert.Contains("Tenant1", role.Name));
    }

    /// <summary>
    /// 测试手动查询是否能看到所有数据（验证过滤器确实在工作）
    /// </summary>
    [Fact]
    public async Task DirectQuery_WithoutFilter_ShouldReturnAllRoles()
    {
        // Act - 直接查询数据库，不通过服务层
        var allRoles = await _dbContext.Roles.ToListAsync();

        // Assert
        Assert.Equal(3, allRoles.Count); // 数据库中确实有3个角色
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}

public class Repository<T> : CodeSpirit.Shared.Repositories.Repository<T> where T : class
{
    public Repository(DbContext context) : base(context)
    {
    }
} 