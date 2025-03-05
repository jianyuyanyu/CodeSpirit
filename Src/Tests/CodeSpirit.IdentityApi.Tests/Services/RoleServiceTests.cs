using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using AutoMapper;
using CodeSpirit.Shared.Repositories;
using System.Linq.Expressions;
using CodeSpirit.IdentityApi.Dtos.Role;
using Microsoft.Extensions.Caching.Distributed;
using CodeSpirit.Core.IdGenerator;

namespace CodeSpirit.IdentityApi.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IRepository<ApplicationRole>> _mockRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<IIdGenerator> _mockIdGenerator;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            // 设置RoleManager Mock
            var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                roleStoreMock.Object, null, null, null, null);

            // 设置其他依赖项
            _mockRepository = new Mock<IRepository<ApplicationRole>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<RoleService>>();
            _mockCache = new Mock<IDistributedCache>();
            _mockIdGenerator = new Mock<IIdGenerator>();

            // 初始化RoleService
            _roleService = new RoleService(
                _mockRepository.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger.Object,
                _mockIdGenerator.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WithValidRole_ReturnsRoleDto()
        {
            // Arrange
            var createDto = new RoleCreateDto
            {
                Name = "TestRole",
                Description = "Test Role Description",
                PermissionAssignments = new List<string> { "permission1", "permission2" }
            };

            var role = new ApplicationRole
            {
                Id = 1,
                Name = createDto.Name,
                Description = createDto.Description,
                RolePermission = new RolePermission
                {
                    PermissionIds = createDto.PermissionAssignments.ToArray()
                }
            };

            var roleDto = new RoleDto
            {
                Id = role.Id.ToString(),
                Name = role.Name,
                Description = role.Description,
                PermissionIds = role.RolePermission.PermissionIds.ToList()
            };

            _mockRoleManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockMapper.Setup(x => x.Map<ApplicationRole>(createDto))
                .Returns(role);

            _mockMapper.Setup(x => x.Map<RoleDto>(role))
                .Returns(roleDto);

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
            long roleId = 1;
            var updateDto = new RoleUpdateDto
            {
                Description = "Updated Description",
                PermissionIds = new List<string> { "permission1", "permission2" }
            };

            var existingRole = new ApplicationRole
            {
                Id = roleId,
                Name = "TestRole",
                Description = "Original Description",
                RolePermission = new RolePermission
                {
                    PermissionIds = updateDto.PermissionIds.ToArray()
                }
            };

            _mockRepository.Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(existingRole);

            _mockRoleManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationRole>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _roleService.UpdateAsync(roleId, updateDto);

            // Assert
            Assert.Equal(updateDto.Description, existingRole.Description);
            Assert.Equal(updateDto.PermissionIds, existingRole.RolePermission.PermissionIds.ToList());
            _mockRoleManager.Verify(x => x.UpdateAsync(existingRole), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesRole()
        {
            // Arrange
            long roleId = 1;
            var role = new ApplicationRole { Id = roleId, Name = "TestRole" };

            _mockRepository.Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(role);

            _mockRoleManager.Setup(x => x.DeleteAsync(role))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _roleService.DeleteAsync(roleId);

            // Assert
            _mockRoleManager.Verify(x => x.DeleteAsync(role), Times.Once);
        }

        [Fact]
        public async Task GetAsync_WithValidId_ReturnsRoleDto()
        {
            // Arrange
            long roleId = 1;
            var role = new ApplicationRole
            {
                Id = roleId,
                Name = "TestRole",
                Description = "Test Description",
                RolePermission = new RolePermission
                {
                    PermissionIds = new[] { "permission1" }
                }
            };

            var roleDto = new RoleDto
            {
                Id = role.Id.ToString(),
                Name = role.Name,
                Description = role.Description,
                PermissionIds = role.RolePermission.PermissionIds.ToList()
            };

            _mockRepository.Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(role);

            _mockMapper.Setup(x => x.Map<RoleDto>(role))
                .Returns(roleDto);

            // Act
            var result = await _roleService.GetAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleDto.Id, result.Id);
            Assert.Equal(role.Name, result.Name);
            Assert.Equal(role.Description, result.Description);
            Assert.Equal(role.RolePermission.PermissionIds.ToList(), result.PermissionIds);
        }

        [Fact]
        public async Task GetRolesAsync_ReturnsPagedList()
        {
            // Arrange
            var queryDto = new RoleQueryDto { Page = 1, PerPage = 10 };
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = 1,
                    Name = "Role1",
                    Description = "Description1",
                    RolePermission = new RolePermission { PermissionIds = new[] { "permission1" } }
                },
                new ApplicationRole
                {
                    Id = 2,
                    Name = "Role2",
                    Description = "Description2",
                    RolePermission = new RolePermission { PermissionIds = new[] { "permission2" } }
                }
            };

            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id.ToString(),
                Name = r.Name,
                Description = r.Description,
                PermissionIds = r.RolePermission.PermissionIds.ToList()
            }).ToList();

            _mockRepository.Setup(x => x.CreateQuery())
                .Returns(roles.AsQueryable());

            _mockMapper.Setup(x => x.Map<List<RoleDto>>(It.IsAny<List<ApplicationRole>>()))
                .Returns(roleDtos);

            // Act
            var result = await _roleService.GetRolesAsync(queryDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Total);
            Assert.Equal(2, result.Items.Count);
        }
    }
} 