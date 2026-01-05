using CodeSpirit.Core;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Jwt;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Services.ThirdParty;
using CodeSpirit.IdentityApi.Tests.TestBase;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Services;

/// <summary>
/// AuthService第三方登录测试
/// </summary>
public class AuthServiceThirdPartyLoginTests : ServiceTestBase
{
    private readonly Mock<IThirdPartyApiService> _mockThirdPartyApiService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IIdGenerator> _mockIdGenerator;
    private readonly Mock<IDataProtectionProvider> _mockDataProtectionProvider;
    private readonly Mock<IDataProtector> _mockDataProtector;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<IJwtTokenHandler> _mockJwtHandler;
    private readonly Mock<ILoginLogRepository> _mockLoginLogRepository;
    private readonly Mock<IRepository<RefreshToken>> _mockRefreshTokenRepository;
    private readonly Mock<ISmsCodeService> _mockSmsCodeService;
    private readonly AuthService _authService;
    private readonly Mock<ICurrentUser> _mockCurrentUserForService;

    public AuthServiceThirdPartyLoginTests()
        : base()
    {
        _mockThirdPartyApiService = new Mock<IThirdPartyApiService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockIdGenerator = new Mock<IIdGenerator>();
        _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
        _mockDataProtector = new Mock<IDataProtector>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockRoleService = new Mock<IRoleService>();
        _mockJwtHandler = new Mock<IJwtTokenHandler>();
        _mockLoginLogRepository = new Mock<ILoginLogRepository>();
        _mockRefreshTokenRepository = new Mock<IRepository<RefreshToken>>();
        _mockSmsCodeService = new Mock<ISmsCodeService>();
        _mockCurrentUserForService = new Mock<ICurrentUser>();

        // 配置DataProtectionProvider
        _mockDataProtectionProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_mockDataProtector.Object);

        // Mock IDataProtector的Protect(byte[])方法而不是扩展方法Protect(string)
        _mockDataProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(bytes => System.Text.Encoding.UTF8.GetBytes($"encrypted_{System.Text.Encoding.UTF8.GetString(bytes)}"));

        // 配置IdGenerator
        _mockIdGenerator.Setup(g => g.NewId()).Returns(1000);

        // 配置Configuration
        var configSection = new Mock<IConfigurationSection>();
        configSection.Setup(s => s["AppId"]).Returns("test_appid");
        configSection.Setup(s => s["AppSecret"]).Returns("test_secret");
        _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(configSection.Object);

        // 配置SettingsService返回null（使用appsettings.json）
        _mockSettingsService
            .Setup(s => s.GetTenantSettingAsync<Dtos.Settings.ThirdPartyLoginSettingsDto>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Dtos.Settings.ThirdPartyLoginSettingsDto)null);

        // 配置JwtHandler - 返回一个有效的JWT token格式
        // JWT格式: header.payload.signature (base64编码)
        // 包含jti (JWT ID) claim
        var validJwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJ0ZXN0LWp3dC1pZCIsInN1YiI6IjEiLCJuYW1lIjoidGVzdCB1c2VyIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        _mockJwtHandler
            .Setup(h => h.GenerateTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(validJwtToken);

        // 配置RoleService
        _mockRoleService
            .Setup(r => r.GetUserPermissionsAsync(It.IsAny<long>()))
            .ReturnsAsync(new HashSet<string>());

        // 配置LoginLogRepository
        _mockLoginLogRepository
            .Setup(r => r.AddAsync(It.IsAny<LoginLog>(), It.IsAny<bool>()))
            .ReturnsAsync((LoginLog log, bool saveChanges) => log);

        // 配置RefreshTokenRepository
        _mockRefreshTokenRepository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<bool>()))
            .ReturnsAsync((RefreshToken token, bool saveChanges) => token);
        
        // 配置Configuration的RefreshTokenExpirationDays
        _mockConfiguration
            .Setup(c => c["Jwt:RefreshTokenExpirationDays"])
            .Returns("7");
        
        // 配置Configuration的ThirdParty配置节
        var wechatConfigSection = new Mock<IConfigurationSection>();
        wechatConfigSection.Setup(s => s["AppId"]).Returns("test_appid");
        wechatConfigSection.Setup(s => s["AppSecret"]).Returns("test_secret");
        _mockConfiguration
            .Setup(c => c.GetSection("ThirdParty:WeChat"))
            .Returns(wechatConfigSection.Object);

        // 创建AuthService实例
        _authService = new AuthService(
            UserManager,
            SignInManager,
            Mapper,
            _mockConfiguration.Object,
            _mockRefreshTokenRepository.Object,
            _mockLoginLogRepository.Object,
            _mockJwtHandler.Object,
            MockAuthServiceLogger.Object,
            _mockRoleService.Object,
            DbContext,
            _mockThirdPartyApiService.Object,
            _mockSettingsService.Object,
            _mockCurrentUserForService.Object,
            _mockIdGenerator.Object,
            _mockDataProtectionProvider.Object,
            _mockSmsCodeService.Object);
    }

    [Fact]
    public async Task ThirdPartyLoginAsync_新用户有UnionId_应该创建用户和账号()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = "test_openid",
            UnionId = "test_unionid",
            SessionKey = "test_sessionkey"
        };

        var config = new ThirdPartyPlatformConfig
        {
            AppId = "test_appid",
            AppSecret = "test_secret"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(model.PlatformType, model.Credential, It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.ThirdPartyLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.True(result.Success, $"登录失败: {result.Message}");
        Assert.NotNull(result.Token);
        Assert.NotNull(result.UserInfo);

        // 验证用户已创建
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId);
        Assert.NotNull(user);
        Assert.True(user.UserName.StartsWith("wx_"));

        // 验证第三方账号已创建
        var account = await DbContext.ThirdPartyAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.OpenId == sessionInfo.OpenId);
        Assert.NotNull(account);
        Assert.Equal(sessionInfo.UnionId, account.UnionId);
        Assert.Equal(sessionInfo.OpenId, account.OpenId);
        Assert.True(account.IsPrimary);
    }

    [Fact]
    public async Task ThirdPartyLoginAsync_新用户无UnionId_应该创建用户和账号()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = "test_openid_no_unionid",
            UnionId = null,
            SessionKey = "test_sessionkey"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(model.PlatformType, model.Credential, It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.ThirdPartyLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);

        // 验证账号已创建（无UnionId）
        var account = await DbContext.ThirdPartyAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.OpenId == sessionInfo.OpenId);
        Assert.NotNull(account);
        Assert.Null(account.UnionId);
    }

    [Fact]
    public async Task ThirdPartyLoginAsync_已存在UnionId账号_应该更新账号信息()
    {
        // Arrange
        var tenantId = "test_tenant";
        var unionId = "existing_unionid";
        var openId = "existing_openid";

        // 创建现有用户和账号
        var existingUser = new ApplicationUser
        {
            Id = 1,
            TenantId = tenantId,
            UserName = "existing_user",
            NormalizedUserName = "EXISTING_USER",
            Name = "Existing User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await UserManager.CreateAsync(existingUser);

        var existingAccount = new ThirdPartyAccount
        {
            Id = 1,
            TenantId = tenantId,
            UserId = existingUser.Id,
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            OpenId = openId,
            UnionId = unionId,
            SessionKey = "old_sessionkey",
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 0
        };
        DbContext.ThirdPartyAccounts.Add(existingAccount);
        await DbContext.SaveChangesAsync();
        
        // 清除EF跟踪，避免后续更新时的跟踪冲突
        DbContext.ChangeTracker.Clear();

        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = openId,
            UnionId = unionId,
            SessionKey = "new_sessionkey"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(model.PlatformType, model.Credential, It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.ThirdPartyLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.True(result.Success, $"登录失败: {result.Message}");
        Assert.Equal(existingUser.Id, result.UserInfo.Id);

        // 验证账号信息已更新
        var updatedAccount = await DbContext.ThirdPartyAccounts
            .FirstOrDefaultAsync(a => a.Id == existingAccount.Id);
        Assert.NotNull(updatedAccount);
        // SessionKey应该被加密更新（不再是原始值）
        Assert.NotEqual("old_sessionkey", updatedAccount.SessionKey);
        Assert.NotNull(updatedAccount.SessionKey);
        Assert.NotNull(updatedAccount.LastLoginTime);
    }

    [Fact]
    public async Task ThirdPartyLoginAsync_已存在OpenId账号_应该更新账号信息()
    {
        // Arrange
        var tenantId = "test_tenant";
        var openId = "existing_openid_no_unionid";

        // 创建现有用户和账号（无UnionId）
        var existingUser = new ApplicationUser
        {
            Id = 2,
            TenantId = tenantId,
            UserName = "existing_user2",
            NormalizedUserName = "EXISTING_USER2",
            Name = "Existing User 2",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await UserManager.CreateAsync(existingUser);

        var existingAccount = new ThirdPartyAccount
        {
            Id = 2,
            TenantId = tenantId,
            UserId = existingUser.Id,
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            OpenId = openId,
            UnionId = null,
            SessionKey = "old_sessionkey",
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 0
        };
        DbContext.ThirdPartyAccounts.Add(existingAccount);
        await DbContext.SaveChangesAsync();
        
        // 清除EF跟踪，避免后续更新时的跟踪冲突
        DbContext.ChangeTracker.Clear();

        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = openId,
            UnionId = "new_unionid", // 这次有了UnionId
            SessionKey = "new_sessionkey"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(model.PlatformType, model.Credential, It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.ThirdPartyLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.True(result.Success, $"登录失败: {result.Message}");

        // 验证UnionId已更新
        var updatedAccount = await DbContext.ThirdPartyAccounts
            .FirstOrDefaultAsync(a => a.Id == existingAccount.Id);
        Assert.NotNull(updatedAccount);
        Assert.Equal("new_unionid", updatedAccount.UnionId);
    }

    [Fact]
    public async Task ThirdPartyLoginAsync_用户被禁用_应该返回失败()
    {
        // Arrange
        var tenantId = "test_tenant";
        var openId = "disabled_user_openid";

        // 创建被禁用的用户
        var disabledUser = new ApplicationUser
        {
            Id = 3,
            TenantId = tenantId,
            UserName = "disabled_user",
            NormalizedUserName = "DISABLED_USER",
            Name = "Disabled User",
            IsActive = false, // 被禁用
            CreatedAt = DateTime.UtcNow
        };
        await UserManager.CreateAsync(disabledUser);

        var existingAccount = new ThirdPartyAccount
        {
            Id = 3,
            TenantId = tenantId,
            UserId = disabledUser.Id,
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            OpenId = openId,
            UnionId = null,
            SessionKey = "old_sessionkey",
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 0
        };
        DbContext.ThirdPartyAccounts.Add(existingAccount);
        await DbContext.SaveChangesAsync();

        var model = new ThirdPartyLoginModel
        {
            PlatformType = ThirdPartyPlatformType.WeChatMiniProgram,
            Credential = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = openId,
            UnionId = null,
            SessionKey = "new_sessionkey"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(model.PlatformType, model.Credential, It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.ThirdPartyLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("禁用", result.Message);
    }

    [Fact]
    public async Task WeChatLoginAsync_应该调用ThirdPartyLoginAsync()
    {
        // Arrange
        var tenantId = "test_tenant";
        var model = new WeChatLoginModel
        {
            Code = "test_code",
            TenantId = tenantId
        };

        var sessionInfo = new ThirdPartySessionInfo
        {
            OpenId = "test_openid",
            UnionId = null,
            SessionKey = "test_sessionkey"
        };

        _mockThirdPartyApiService
            .Setup(s => s.GetSessionAsync(
                ThirdPartyPlatformType.WeChatMiniProgram,
                model.Code,
                It.IsAny<ThirdPartyPlatformConfig>()))
            .ReturnsAsync(sessionInfo);

        // Act
        var result = await _authService.WeChatLoginAsync(model, "127.0.0.1", "test_user_agent");

        // Assert
        Assert.True(result.Success);
        _mockThirdPartyApiService.Verify(s => s.GetSessionAsync(
            ThirdPartyPlatformType.WeChatMiniProgram,
            model.Code,
            It.IsAny<ThirdPartyPlatformConfig>()), Times.Once);
    }
}

