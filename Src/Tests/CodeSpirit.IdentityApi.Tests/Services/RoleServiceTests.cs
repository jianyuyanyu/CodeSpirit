using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.IdentityApi.Tests.TestBase;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class RoleServiceTests : ServiceTestBase
    {
        private readonly RoleService _roleService;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly IIdGenerator _idGenerator;

        public RoleServiceTests()
            : base()
        {
            // 设置额外依赖
            _mockCache = new Mock<IDistributedCache>();
            _idGenerator = new SnowflakeIdGenerator();
            
            // 初始化RoleService
            _roleService = new RoleService(
                RoleRepository,
                Mapper,
                _mockCache.Object,
                MockRoleServiceLogger.Object,
                _idGenerator
            );
            
            // 准备测试数据
            SeedTestData();
        }
        
        /// <summary>
        /// 准备角色测试数据
        /// </summary>
        protected override void SeedTestData()
        {
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "Administrator",
                    RolePermission = new RolePermission
                    {
                        PermissionIds = new[] { "permission1", "permission2" }
                    }
                },
                new ApplicationRole
                {
                    Id = 2,
                    Name = "User",
                    Description = "Regular User",
                    RolePermission = new RolePermission
                    {
                        PermissionIds = new[] { "permission3" }
                    }
                }
            };
            
            SeedRoles(roles.ToArray());
            
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
        public async Task CreateAsync_WithValidRole_ReturnsRoleDto()
        {
            // Arrange
            Setup();
            var createDto = new RoleCreateDto
            {
                Name = "TestRole",
                Description = "Test Role Description",
                PermissionAssignments = new List<string> { "permission1", "permission2" }
            };

            // Act
            var result = await _roleService.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Name, result.Name);
            Assert.Equal(createDto.Description, result.Description);
            Assert.Equal(createDto.PermissionAssignments, result.PermissionIds);
        }

        [Fact]
        public async Task UpdateAsync_WithValidRole_UpdatesRole()
        {
            // Arrange
            Setup();
            var roleId = 1L;
            var updateDto = new RoleUpdateDto
            {
                Name = "Updated Role",
                Description = "Updated Description",
                PermissionIds = new List<string> { "permission3" }
            };
            
            // Act
            await _roleService.UpdateAsync(roleId, updateDto);
            
            // 从数据库中获取更新后的角色
            var updatedRole = await DbContext.Set<ApplicationRole>().FindAsync(roleId);
            
            // Assert
            Assert.NotNull(updatedRole);
            Assert.Equal("Updated Role", updatedRole.Name);
            Assert.Equal("Updated Description", updatedRole.Description);
            Assert.Contains("permission3", updatedRole.RolePermission.PermissionIds);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesRole()
        {
            // Arrange
            Setup();
            long roleId = 2;
            
            // Act
            await _roleService.DeleteAsync(roleId);
            
            // Assert
            var role = await RoleManager.FindByIdAsync(roleId.ToString());
            Assert.Null(role);
        }

        [Fact]
        public async Task GetAsync_WithValidId_ReturnsRoleDto()
        {
            // Arrange
            Setup();
            long roleId = 1;
            
            // Act
            var result = await _roleService.GetAsync(roleId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId.ToString(), result.Id);
        }

        [Fact]
        public async Task GetRolesAsync_ReturnsPagedList()
        {
            // Arrange
            Setup();
            var queryDto = new RoleQueryDto { Page = 1, PerPage = 10 };
            
            // Act
            var result = await _roleService.GetRolesAsync(queryDto);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Total);  // 应该有2个角色，不是1个
            Assert.Equal(2, result.Items.Count);  // 应该返回2个项目，不是1个
        }
    }
} 