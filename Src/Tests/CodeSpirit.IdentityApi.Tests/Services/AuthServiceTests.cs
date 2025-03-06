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
using CodeSpirit.IdentityApi.Tests.TestBase;
using System.Threading;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class AuthServiceTests : ServiceTestBase
    {
        private readonly AuthService _authService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IRepository<LoginLog>> _mockLoginLogRepository;

        public AuthServiceTests()
            : base()
        {
            // 设置额外依赖
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLoginLogRepository = new Mock<IRepository<LoginLog>>();
            
            // 配置模拟登录日志仓储
            _mockLoginLogRepository.Setup(x => x.AddAsync(It.IsAny<LoginLog>(), default))
                .ReturnsAsync((LoginLog log, CancellationToken _) => log);
            
            // 配置JWT设置
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x.Value).Returns("your-test-secret-key-with-sufficient-length-for-testing");
            jwtSection.Setup(x => x["SecretKey"]).Returns("your-test-secret-key-with-sufficient-length-for-testing");
            jwtSection.Setup(x => x["Issuer"]).Returns("test-issuer");
            jwtSection.Setup(x => x["Audience"]).Returns("test-audience");
            jwtSection.Setup(x => x["ExpirationMinutes"]).Returns("30");

            _mockConfiguration.Setup(x => x["Jwt:SecretKey"]).Returns("your-test-secret-key-with-sufficient-length-for-testing");
            _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
            _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("test-audience");
            _mockConfiguration.Setup(x => x["Jwt:ExpirationMinutes"]).Returns("30");
            _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);
            
            // 初始化AuthService - 使用真实的 UserManager 和 SignInManager
            _authService = new AuthService(
                UserManager, // 使用真实的 UserManager
                SignInManager, // 使用真实的 SignInManager
                _mockLoginLogRepository.Object,
                _mockConfiguration.Object,
                Mapper,
                MockHttpContextAccessor.Object
            );
            
            // 准备测试数据
            SeedTestData();
        }
        
        /// <summary>
        /// 准备认证测试数据
        /// </summary>
        protected override void SeedTestData()
        {
            var roleName = "User";
            
            // 使用 RoleManager 创建角色
            if (!RoleManager.RoleExistsAsync(roleName).Result)
            {
                var role = new ApplicationRole
                {
                    Id = 1,
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    RolePermission = new RolePermission
                    {
                        PermissionIds = new[] { "permission1" }
                    }
                };
                
                var result = RoleManager.CreateAsync(role).Result;
                if (!result.Succeeded)
                {
                    throw new Exception($"创建角色失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            
            // 创建测试用户
            var testUser = new ApplicationUser
            {
                Id = 1,
                UserName = "testuser",
                Email = "test@example.com",
                IsActive = true,
                Name = "Test User",
                NormalizedUserName = "TESTUSER",
                NormalizedEmail = "TEST@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            // 创建非活跃用户
            var inactiveUser = new ApplicationUser
            {
                Id = 2,
                UserName = "inactiveuser",
                Email = "inactive@example.com",
                IsActive = false,
                Name = "Inactive User",
                NormalizedUserName = "INACTIVEUSER",
                NormalizedEmail = "INACTIVE@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            // 创建密码哈希
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            testUser.PasswordHash = passwordHasher.HashPassword(testUser, "testpassword");
            inactiveUser.PasswordHash = passwordHasher.HashPassword(inactiveUser, "testpassword");
            
            // 保存用户
            if (UserManager.FindByNameAsync(testUser.UserName).Result == null)
            {
                var result = UserManager.CreateAsync(testUser).Result;
                if (!result.Succeeded)
                {
                    throw new Exception($"创建用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
                
                // 分配角色给用户
                UserManager.AddToRoleAsync(testUser, roleName).Wait();
            }
            
            if (UserManager.FindByNameAsync(inactiveUser.UserName).Result == null)
            {
                var result = UserManager.CreateAsync(inactiveUser).Result;
                if (!result.Succeeded)
                {
                    throw new Exception($"创建非活跃用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            
            // 使用真实映射，不再模拟Mapper
        }

        /// <summary>
        /// 在每个测试方法执行前自动清理数据库上下文
        /// </summary>
        protected void Setup()
        {
            ClearDbContext();
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
        {
            // Arrange
            Setup();
            string userName = "testuser";
            string password = "testpassword";

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
            Setup();
            string userName = "inactiveuser";
            string password = "testpassword";

            // Act
            var result = await _authService.LoginAsync(userName, password);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.UserInfo);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailureResult()
        {
            // Arrange
            // 清理数据库上下文，避免实体跟踪冲突
            Setup();
            string userName = "testuser";
            string password = "wrongpassword";

            // Act
            var result = await _authService.LoginAsync(userName, password);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Message);
            Assert.Null(result.Token);
            Assert.Null(result.UserInfo);
        }

        [Fact]
        public async Task ImpersonateLoginAsync_WithValidUser_ReturnsSuccessResult()
        {
            // Arrange
            Setup();
            string userName = "testuser";
            
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