using CodeSpirit.Authorization;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Core;
using CodeSpirit.Web.Controllers;
using CodeSpirit.Web.Dtos.Cache;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CodeSpirit.Web.Tests.Controllers;

/// <summary>
/// 缓存管理控制器单元测试
/// </summary>
public class CacheManagementControllerTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ICacheManagementService> _cacheManagementServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<CacheManagementController>> _loggerMock;
    private readonly CacheManagementController _controller;

    public CacheManagementControllerTests(ITestOutputHelper output)
    {
        _output = output;
        _cacheManagementServiceMock = new Mock<ICacheManagementService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<CacheManagementController>>();

        _controller = new CacheManagementController(
            _cacheManagementServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetCacheKeys_AsSystemAdmin_ShouldReturnAllKeys()
    {
        // Arrange
        var query = new CacheKeyQueryDto
        {
            Pattern = "CodeSpirit:*",
            Page = 1,
            PerPage = 10
        };

        var cacheKeys = new PageList<CacheKeyInfo>
        {
            Items = new List<CacheKeyInfo>
            {
                new() { Key = "CodeSpirit:Cache:data:key1", Type = "string", Ttl = 1800 },
                new() { Key = "CodeSpirit:Cache:data:key2", Type = "string", Ttl = 1800 }
            },
            Total = 2
        };

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // 系统管理员
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.GetKeysAsync(query.Pattern, null, query.Page, query.PerPage, default))
            .ReturnsAsync(cacheKeys);

        // Act
        var result = await _controller.GetCacheKeys(query);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<CacheKeyDto>>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PageList<CacheKeyDto>>>(okResult.Value);

        response.Status.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data!.Total.Should().Be(2);
        response.Data.Items.Should().HaveCount(2);

        _output.WriteLine($"✅ 系统管理员获取缓存键列表成功，返回 {response.Data.Total} 条");
    }

    [Fact]
    public async Task GetCacheKeys_AsTenantAdmin_ShouldReturnOnlyTenantKeys()
    {
        // Arrange
        var tenantId = "tenant123";
        var query = new CacheKeyQueryDto
        {
            Pattern = "CodeSpirit:*",
            Page = 1,
            PerPage = 10
        };

        var cacheKeys = new PageList<CacheKeyInfo>
        {
            Items = new List<CacheKeyInfo>
            {
                new() { Key = $"CodeSpirit:Cache:data:tenant:{tenantId}:key1", Type = "string", Ttl = 1800 }
            },
            Total = 1
        };

        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.GetKeysAsync(query.Pattern, tenantId, query.Page, query.PerPage, default))
            .ReturnsAsync(cacheKeys);

        // Act
        var result = await _controller.GetCacheKeys(query);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<CacheKeyDto>>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PageList<CacheKeyDto>>>(okResult.Value);

        response.Status.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().AllSatisfy(k => k.Key.Should().Contain($"tenant:{tenantId}"));

        _output.WriteLine($"✅ 租户管理员获取缓存键列表成功，返回 {response.Data.Total} 条");
    }

    [Fact]
    public async Task GetCacheKeys_AsTenantAdmin_WithoutTenantId_ShouldReturnBadResponse()
    {
        // Arrange
        var query = new CacheKeyQueryDto { Page = 1, PerPage = 10 };

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);

        // Act
        var result = await _controller.GetCacheKeys(query);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<CacheKeyDto>>>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PageList<CacheKeyDto>>>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("无法确定租户信息");

        _output.WriteLine("✅ 租户管理员无租户ID时返回错误");
    }

    [Fact]
    public async Task GetCacheValue_AsSystemAdmin_ShouldReturnValue()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:test:key";
        var cacheValue = new CacheValueInfo
        {
            Key = key,
            Type = "string",
            Value = "\"test-value\"",
            Ttl = 1800
        };

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.GetValueAsync(key, default))
            .ReturnsAsync(cacheValue);

        // Act
        var result = await _controller.GetCacheValue(key);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CacheValueDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CacheValueDto>>(okResult.Value);

        response.Status.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data!.Key.Should().Be(key);
        response.Data.Type.Should().Be("string");

        _output.WriteLine($"✅ 获取缓存值详情成功：{response.Data.Key}");
    }

    [Fact]
    public async Task GetCacheValue_AsTenantAdmin_WithUnauthorizedKey_ShouldReturnBadResponse()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:tenant:othertenant:key";
        var tenantId = "tenant123";

        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);

        // Act
        var result = await _controller.GetCacheValue(key);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CacheValueDto>>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CacheValueDto>>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("无权访问");

        _output.WriteLine("✅ 租户管理员访问其他租户的缓存键时返回错误");
    }

    [Fact]
    public async Task GetCacheValue_WithNonExistentKey_ShouldReturnBadResponse()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:nonexistent:key";

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);

        _cacheManagementServiceMock
            .Setup(x => x.GetValueAsync(key, default))
            .ReturnsAsync((CacheValueInfo?)null);

        // Act
        var result = await _controller.GetCacheValue(key);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<CacheValueDto>>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<CacheValueDto>>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("不存在");

        _output.WriteLine("✅ 不存在的缓存键返回错误");
    }

    [Fact]
    public async Task DeleteCacheKey_AsSystemAdmin_ShouldDeleteSuccessfully()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:test:key";

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.DeleteKeyAsync(key, default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCacheKey(key);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);

        response.Status.Should().Be(0);
        _cacheManagementServiceMock.Verify(x => x.DeleteKeyAsync(key, default), Times.Once);

        _output.WriteLine("✅ 删除缓存键成功");
    }

    [Fact]
    public async Task DeleteCacheKey_AsTenantAdmin_WithUnauthorizedKey_ShouldReturnBadResponse()
    {
        // Arrange
        var key = "CodeSpirit:Cache:data:tenant:othertenant:key";
        var tenantId = "tenant123";

        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);

        // Act
        var result = await _controller.DeleteCacheKey(key);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("无权删除");

        _output.WriteLine("✅ 租户管理员删除其他租户的缓存键时返回错误");
    }

    [Fact]
    public async Task DeleteByPattern_AsSystemAdmin_ShouldDeleteSuccessfully()
    {
        // Arrange
        var dto = new BatchDeleteCacheDto
        {
            Pattern = "CodeSpirit:*:temp:*"
        };

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.DeleteByPatternAsync(dto.Pattern, null, default))
            .ReturnsAsync(5L);

        // Act
        var result = await _controller.DeleteByPattern(dto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

        response.Status.Should().Be(0);
        response.Data.Should().NotBeNull();

        _cacheManagementServiceMock.Verify(x => x.DeleteByPatternAsync(dto.Pattern, null, default), Times.Once);

        _output.WriteLine("✅ 按模式批量删除缓存成功");
    }

    [Fact]
    public async Task DeleteByPattern_AsTenantAdmin_ShouldDeleteOnlyTenantKeys()
    {
        // Arrange
        var tenantId = "tenant123";
        var dto = new BatchDeleteCacheDto
        {
            Pattern = "CodeSpirit:*"
        };

        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.DeleteByPatternAsync(dto.Pattern, tenantId, default))
            .ReturnsAsync(3L);

        // Act
        var result = await _controller.DeleteByPattern(dto);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<object>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

        response.Status.Should().Be(0);
        _cacheManagementServiceMock.Verify(x => x.DeleteByPatternAsync(dto.Pattern, tenantId, default), Times.Once);

        _output.WriteLine("✅ 租户管理员按模式删除缓存成功");
    }

    [Fact]
    public async Task ClearAllCache_AsSystemAdmin_ShouldClearSuccessfully()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);
        _currentUserMock.Setup(x => x.Id).Returns(1L);

        _cacheManagementServiceMock
            .Setup(x => x.ClearAllAsync(default))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ClearAllCache();

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);

        response.Status.Should().Be(0);
        _cacheManagementServiceMock.Verify(x => x.ClearAllAsync(default), Times.Once);

        _output.WriteLine("✅ 系统管理员清空所有缓存成功");
    }

    [Fact]
    public async Task ClearAllCache_AsTenantAdmin_ShouldReturnBadResponse()
    {
        // Arrange
        var tenantId = "tenant123";

        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(false);

        // Act
        var result = await _controller.ClearAllCache();

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("仅系统管理员");

        _cacheManagementServiceMock.Verify(x => x.ClearAllAsync(default), Times.Never);

        _output.WriteLine("✅ 租户管理员无法清空所有缓存");
    }

    [Fact]
    public async Task GetCacheKeys_WithException_ShouldReturnBadResponse()
    {
        // Arrange
        var query = new CacheKeyQueryDto { Page = 1, PerPage = 10 };

        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        _currentUserMock.Setup(x => x.IsInRole("SystemAdmin")).Returns(true);

        _cacheManagementServiceMock
            .Setup(x => x.GetKeysAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), default))
            .ThrowsAsync(new Exception("Redis连接失败"));

        // Act
        var result = await _controller.GetCacheKeys(query);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<CacheKeyDto>>>>(result);
        var badRequestResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<PageList<CacheKeyDto>>>(badRequestResult.Value);

        response.Status.Should().Be(1);
        response.Msg.Should().Contain("失败");

        _output.WriteLine("✅ 异常处理正确");
    }
}

