using Microsoft.AspNetCore.Http;
using Moq;
using System.Net.Http;
using System.Security.Claims;
using Xunit;

namespace CodeSpirit.Authorization.Tests
{
    /// <summary>
    /// CurrentUser 类的单元测试
    /// </summary>
    public class CurrentUserTests
    {
        /// <summary>
        /// 测试当用户已认证时，IsAuthenticated 应返回 true
        /// </summary>
        [Fact]
        public void IsAuthenticated_WhenUserIsAuthenticated_ReturnsTrue()
        {
            // Arrange
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var currentUser = new CurrentUser(mockHttpContextAccessor.Object);

            // Act
            var result = currentUser.IsAuthenticated;

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试当 HttpContext 为 null 时，IsAuthenticated 应返回 false
        /// </summary>
        [Fact]
        public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
        {
            // Arrange
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext)null);

            var currentUser = new CurrentUser(mockHttpContextAccessor.Object);

            // Act
            var result = currentUser.IsAuthenticated;

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试当用户具有有效的 NameIdentifier 声明时，Id 应正确返回
        /// </summary>
        [Fact]
        public void Id_WhenNameIdentifierClaimExists_ReturnsUserId()
        {
            // Arrange
            const long expectedUserId = 123;
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var currentUser = new CurrentUser(mockHttpContextAccessor.Object);

            // Act
            var result = currentUser.Id;

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        /// <summary>
        /// 测试当用户具有角色声明时，Roles 应正确返回所有角色
        /// </summary>
        [Fact]
        public void Roles_WhenRoleClaimsExist_ReturnsAllRoles()
        {
            // Arrange
            var expectedRoles = new[] { "Admin", "User", "Manager" };
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var claims = expectedRoles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var currentUser = new CurrentUser(mockHttpContextAccessor.Object);

            // Act
            var result = currentUser.Roles;

            // Assert
            Assert.Equal(expectedRoles, result);
        }

        /// <summary>
        /// 测试 IsInRole 方法，当用户属于指定角色时，应返回 true
        /// </summary>
        [Fact]
        public void IsInRole_WhenUserHasRole_ReturnsTrue()
        {
            // Arrange
            const string role = "Admin";
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, role) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            var currentUser = new CurrentUser(mockHttpContextAccessor.Object);

            // Act
            var result = currentUser.IsInRole(role);

            // Assert
            Assert.True(result);
        }
    }
}