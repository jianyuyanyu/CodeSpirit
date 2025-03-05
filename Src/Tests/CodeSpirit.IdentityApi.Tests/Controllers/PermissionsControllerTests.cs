using AutoMapper;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Permission;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CodeSpirit.IdentityApi.Tests.Controllers
{
    public class PermissionsControllerTests
    {
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PermissionsController _controller;

        public PermissionsControllerTests()
        {
            _mockPermissionService = new Mock<IPermissionService>();
            _mockMapper = new Mock<IMapper>();
            _controller = new PermissionsController(_mockPermissionService.Object, _mockMapper.Object);
        }

        [Fact]
        public void GetPermissions_ReturnsSuccessResponse()
        {
            // Arrange
            var permissionNodes = new List<PermissionNode>
            {
                new PermissionNode(
                    name: "users",
                    description: "用户管理模块",
                    parent: "",
                    path: "/api/users",
                    requestMethod: "GET",
                    displayName: "用户管理")
                {
                    Children = new List<PermissionNode>
                    {
                        new PermissionNode(
                            name: "users.create",
                            description: "创建用户功能",
                            parent: "users",
                            path: "/api/users",
                            requestMethod: "POST",
                            displayName: "创建用户")
                    }
                }
            };

            var permissionDtos = new List<PermissionDto>
            {
                new PermissionDto
                {
                    Id = "1",
                    Name = "users",
                    DisplayName = "用户管理",
                    Path = "/api/users",
                    RequestMethod = "GET",
                    Children = new List<PermissionDto>
                    {
                        new PermissionDto
                        {
                            Id = "2",
                            Name = "users.create",
                            DisplayName = "创建用户",
                            Path = "/api/users",
                            RequestMethod = "POST"
                        }
                    }
                }
            };

            _mockPermissionService.Setup(x => x.GetPermissionTree())
                .Returns(permissionNodes);

            _mockMapper.Setup(x => x.Map<List<PermissionDto>>(permissionNodes))
                .Returns(permissionDtos);

            // Act
            var result = _controller.GetPermissions();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<PageList<PermissionDto>>>>(result);
            var response = Assert.IsType<ApiResponse<PageList<PermissionDto>>>(((ObjectResult)actionResult.Result).Value);
            Assert.Equal(0, response.Status);
            Assert.Equal(1, response.Data.Items.Count);
            Assert.Equal(1, response.Data.Total);
            Assert.Equal("users", response.Data.Items[0].Name);
            Assert.Equal(1, response.Data.Items[0].Children.Count);
        }

        [Fact]
        public void GetPermissionTree_ReturnsSuccessResponse()
        {
            // Arrange
            var permissionNodes = new List<PermissionNode>
            {
                new PermissionNode(
                    name: "users",
                    description: "用户管理模块",
                    parent: "",
                    path: "/api/users",
                    requestMethod: "GET",
                    displayName: "用户管理")
                {
                    Children = new List<PermissionNode>
                    {
                        new PermissionNode(
                            name: "users.create",
                            description: "创建用户功能",
                            parent: "users",
                            path: "/api/users",
                            requestMethod: "POST",
                            displayName: "创建用户")
                    }
                }
            };

            var permissionTreeDtos = new List<PermissionTreeDto>
            {
                new PermissionTreeDto
                {
                    Id = "1",
                    Label = "用户管理",
                    Children = new List<PermissionTreeDto>
                    {
                        new PermissionTreeDto
                        {
                            Id = "2",
                            Label = "创建用户"
                        }
                    }
                }
            };

            _mockPermissionService.Setup(x => x.GetPermissionTree())
                .Returns(permissionNodes);

            _mockMapper.Setup(x => x.Map<List<PermissionTreeDto>>(permissionNodes))
                .Returns(permissionTreeDtos);

            // Act
            var result = _controller.GetPermissionTree();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<PermissionTreeDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var treeData = Assert.IsType<List<PermissionTreeDto>>(okResult.Value);
            Assert.Single(treeData);
            Assert.Equal("用户管理", treeData[0].Label);
            Assert.Single(treeData[0].Children);
            Assert.Equal("创建用户", treeData[0].Children[0].Label);
        }
    }
} 