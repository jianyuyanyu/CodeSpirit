using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using CodeSpirit.Shared.Services;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using CodeSpirit.Shared.Repositories;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IRepository<LoginLog>> _mockLoginLogRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // 设置UserManager Mock
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // 设置SignInManager Mock
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null, null, null, null);

            // 设置其他依赖项
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockLoginLogRepository = new Mock<IRepository<LoginLog>>();
            _mockMapper = new Mock<IMapper>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // 配置JWT设置
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["SecretKey"]).Returns("your-test-secret-key-with-sufficient-length-for-testing");
            jwtSection.Setup(x => x["Issuer"]).Returns("test-issuer");
            jwtSection.Setup(x => x["Audience"]).Returns("test-audience");
            jwtSection.Setup(x => x["ExpirationMinutes"]).Returns("30");

            _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

            // 初始化AuthService
            _authService = new AuthService(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockLoginLogRepository.Object,
                _mockConfiguration.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object
            );
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
        {
            // Arrange
            string userName = "testuser";
            string password = "testpassword";

            var user = new ApplicationUser
            {
                Id = 1,
                UserName = userName,
                Email = "test@example.com",
                IsActive = true,
                UserRoles = new List<ApplicationUserRole>
                {
                    new ApplicationUserRole
                    {
                        Role = new ApplicationRole
                        {
                            Name = "User",
                            RolePermission = new RolePermission
                            {
                                PermissionIds = new[] { "permission1" }
                            }
                        }
                    }
                }
            };

            _mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Success);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };

            _mockMapper.Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            var result = await _authService.LoginAsync(userName, password);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("登录成功", result.Message);
            Assert.NotNull(result.Token);
            Assert.Equal(userName, result.UserInfo.UserName);
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ReturnsFailureResult()
        {
            // Arrange
            string userName = "inactiveuser";
            string password = "testpassword";

            var user = new ApplicationUser
            {
                Id = 1,
                UserName = userName,
                Email = "test@example.com",
                IsActive = false
            };

            _mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser>().AsQueryable());

            // Act
            var result = await _authService.LoginAsync(userName, password);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorMessages.InvalidCredentials, result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.UserInfo);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailureResult()
        {
            // Arrange
            string userName = "testuser";
            string password = "wrongpassword";

            var user = new ApplicationUser
            {
                Id = 1,
                UserName = userName,
                Email = "test@example.com",
                IsActive = true
            };

            _mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(userName, password);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorMessages.InvalidCredentials, result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.UserInfo);
        }

        [Fact]
        public async Task ImpersonateLoginAsync_WithValidUser_ReturnsSuccessResult()
        {
            // Arrange
            string userName = "testuser";

            var user = new ApplicationUser
            {
                Id = 1,
                UserName = userName,
                Email = "test@example.com",
                IsActive = true,
                UserRoles = new List<ApplicationUserRole>
                {
                    new ApplicationUserRole
                    {
                        Role = new ApplicationRole
                        {
                            Name = "User",
                            RolePermission = new RolePermission
                            {
                                PermissionIds = new[] { "permission1" }
                            }
                        }
                    }
                }
            };

            _mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser> { user }.AsQueryable());

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };

            _mockMapper.Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            var result = await _authService.ImpersonateLoginAsync(userName);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("登录成功", result.Message);
            Assert.NotNull(result.Token);
            Assert.Equal(userName, result.UserInfo.UserName);
        }
    }
} 