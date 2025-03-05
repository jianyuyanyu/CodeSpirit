using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    public class RolesControllerTests
    {
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly RolesController _controller;

        public RolesControllerTests()
        {
            _mockRoleService = new Mock<IRoleService>();
            _controller = new RolesController(_mockRoleService.Object);
        }

        [Fact]
        public async Task GetRoles_ReturnsSuccessResponse()
        {
            // Arrange
            var queryDto = new RoleQueryDto { Page = 1, PerPage = 10 };
            var expectedRoles = new PageList<RoleDto>
            {
                Items = new List<RoleDto>
                {
                    new RoleDto { Id = "1", Name = "Admin", Description = "管理员" },
                    new RoleDto { Id = "2", Name = "User", Description = "普通用户" }
                },
                Total = 2
            };

            _mockRoleService.Setup(x => x.GetRolesAsync(queryDto))
                .ReturnsAsync(expectedRoles);

            // Act
            var result = await _controller.GetRoles(queryDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<RoleDto>>>>(result);
            var response = Assert.IsType<ApiResponse<PageList<RoleDto>>>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal(2, response.Data.Items.Count);
            Assert.Equal(2, response.Data.Total);
        }

        [Fact]
        public async Task GetRole_ReturnsSuccessResponse()
        {
            // Arrange
            long roleId = 1;
            var expectedRole = new RoleDto
            {
                Id = "1",
                Name = "Admin",
                Description = "管理员"
            };

            _mockRoleService.Setup(x => x.GetAsync(roleId))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _controller.GetRole(roleId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<RoleDto>>>(result);
            var response = Assert.IsType<ApiResponse<RoleDto>>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal(expectedRole.Id, response.Data.Id);
            Assert.Equal(expectedRole.Name, response.Data.Name);
            Assert.Equal(expectedRole.Description, response.Data.Description);
        }

        [Fact]
        public async Task Create_ReturnsSuccessResponse()
        {
            // Arrange
            var createDto = new RoleCreateDto
            {
                Name = "NewRole",
                Description = "新角色"
            };

            var expectedRole = new RoleDto
            {
                Id = "1",
                Name = createDto.Name,
                Description = createDto.Description
            };

            _mockRoleService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(expectedRole);

            // Act
            var result = await _controller.Create(createDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<RoleDto>>>(result);
            var response = Assert.IsType<ApiResponse<RoleDto>>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal(createDto.Name, response.Data.Name);
            Assert.Equal(createDto.Description, response.Data.Description);
        }

        [Fact]
        public async Task Update_ReturnsSuccessResponse()
        {
            // Arrange
            long roleId = 1;
            var updateDto = new RoleUpdateDto
            {
                Description = "更新的角色描述"
            };

            _mockRoleService.Setup(x => x.UpdateAsync(roleId, updateDto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(roleId, updateDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            _mockRoleService.Verify(x => x.UpdateAsync(roleId, updateDto), Times.Once);
        }

        [Fact]
        public async Task Delete_ReturnsSuccessResponse()
        {
            // Arrange
            long roleId = 1;
            _mockRoleService.Setup(x => x.DeleteAsync(roleId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(roleId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            _mockRoleService.Verify(x => x.DeleteAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task BatchImport_WithAllSuccess_ReturnsSuccessResponse()
        {
            // Arrange
            var importDto = new BatchImportDtoBase<RoleBatchImportItemDto>
            {
                ImportData = new List<RoleBatchImportItemDto>
                {
                    new RoleBatchImportItemDto { Name = "Role1", Description = "描述1" },
                    new RoleBatchImportItemDto { Name = "Role2", Description = "描述2" }
                }
            };

            _mockRoleService.Setup(x => x.BatchImportRolesAsync(importDto.ImportData))
                .ReturnsAsync((2, new List<string>()));

            // Act
            var result = await _controller.BatchImport(importDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal("角色批量导入成功，共导入2个角色", response.Msg);
        }

        [Fact]
        public async Task BatchImport_WithPartialFailure_ReturnsPartialSuccessResponse()
        {
            // Arrange
            var importDto = new BatchImportDtoBase<RoleBatchImportItemDto>
            {
                ImportData = new List<RoleBatchImportItemDto>
                {
                    new RoleBatchImportItemDto { Name = "Role1", Description = "描述1" },
                    new RoleBatchImportItemDto { Name = "Role2", Description = "描述2" }
                }
            };

            _mockRoleService.Setup(x => x.BatchImportRolesAsync(importDto.ImportData))
                .ReturnsAsync((1, new List<string> { "Role2" }));

            // Act
            var result = await _controller.BatchImport(importDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
            var response = Assert.IsType<ApiResponse>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal("角色批量导入完成。成功：1个，失败：1个。失败的角色：Role2", response.Msg);
        }
    }
} 