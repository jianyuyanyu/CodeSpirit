using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using CodeSpirit.Navigation.Services;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Core.Enums;
using CodeSpirit.Core.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Xunit;

namespace CodeSpirit.Navigation.Tests;

/// <summary>
/// Navigation组件平台过滤测试
/// </summary>
public class NavigationPlatformFilterTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<NavigationService>> _mockLogger;
    private readonly Mock<IActionDescriptorCollectionProvider> _mockActionProvider;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHasPermissionService> _mockPermissionService;
    private readonly NavigationService _navigationService;

    public NavigationPlatformFilterTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<NavigationService>>();
        _mockActionProvider = new Mock<IActionDescriptorCollectionProvider>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockPermissionService = new Mock<IHasPermissionService>();

        _navigationService = new NavigationService(
            _mockActionProvider.Object,
            _mockCache.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    #region 平台类型过滤测试

    [Fact]
    public void FilterNodesByPlatform_WithSystemPlatform_ShouldReturnOnlySystemNodes()
    {
        // Arrange
        var nodes = CreateTestNavigationNodes();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.System);

        // Assert
        Assert.Equal(2, result.Count); // System + Both (Universal) 节点
        
        var titles = result.Select(n => n.Title).ToList();
        Assert.Contains("System Module", titles);
        Assert.Contains("Universal Module", titles); // Both 类型应该在 System 查询中包含
        
        // 验证平台类型
        var systemNode = result.First(n => n.Title == "System Module");
        var universalNode = result.First(n => n.Title == "Universal Module");
        Assert.Equal(PlatformType.System, systemNode.PlatformType);
        Assert.Equal(PlatformType.Both, universalNode.PlatformType);
    }

    [Fact]
    public void FilterNodesByPlatform_WithTenantPlatform_ShouldReturnOnlyTenantNodes()
    {
        // Arrange
        var nodes = CreateTestNavigationNodes();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.Tenant);

        // Assert
        Assert.Equal(2, result.Count); // Tenant + Both (Universal) 节点
        
        var titles = result.Select(n => n.Title).ToList();
        Assert.Contains("Tenant Module", titles);
        Assert.Contains("Universal Module", titles); // Both 类型应该在 Tenant 查询中包含
        
        // 验证平台类型
        var tenantNode = result.First(n => n.Title == "Tenant Module");
        var universalNode = result.First(n => n.Title == "Universal Module");
        Assert.Equal(PlatformType.Tenant, tenantNode.PlatformType);
        Assert.Equal(PlatformType.Both, universalNode.PlatformType);
    }

    [Fact]
    public void FilterNodesByPlatform_WithBothPlatform_ShouldReturnSystemAndTenantNodes()
    {
        // Arrange
        var nodes = CreateTestNavigationNodes();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.Both);

        // Assert
        Assert.Equal(3, result.Count); // System + Tenant + Both
        
        var titles = result.Select(n => n.Title).ToList();
        Assert.Contains("System Module", titles);
        Assert.Contains("Tenant Module", titles);
        Assert.Contains("Universal Module", titles);
    }

    [Fact]
    public void FilterNodesByPlatform_WithNonePlatform_ShouldReturnEmpty()
    {
        // Arrange
        var nodes = CreateTestNavigationNodes();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterNodesByPlatform_WithEmptyNodes_ShouldReturnEmpty()
    {
        // Arrange
        var nodes = new List<NavigationNode>();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.System);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterNodesByPlatform_WithNullNodes_ShouldReturnEmpty()
    {
        // Arrange
        List<NavigationNode> nodes = null;

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.System);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region 递归子节点过滤测试

    [Fact]
    public void FilterNodesByPlatform_WithChildNodes_ShouldFilterRecursively()
    {
        // Arrange
        var nodes = CreateTestNavigationNodesWithChildren();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.System);

        // Assert
        var parentNode = result.First();
        Assert.Equal("Mixed Module", parentNode.Title);
        Assert.Single(parentNode.Children); // 只有系统子节点应该保留
        Assert.Equal("System Child", parentNode.Children[0].Title);
    }

    [Fact]
    public void FilterNodesByPlatform_WithMixedChildren_ShouldPreserveCorrectChildren()
    {
        // Arrange
        var nodes = CreateTestNavigationNodesWithChildren();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.Tenant);

        // Assert
        var parentNode = result.First();
        Assert.Equal("Mixed Module", parentNode.Title);
        Assert.Single(parentNode.Children); // 只有租户子节点应该保留
        Assert.Equal("Tenant Child", parentNode.Children[0].Title);
    }

    [Fact]
    public void FilterNodesByPlatform_WithBothType_ShouldPreserveAllValidChildren()
    {
        // Arrange
        var nodes = CreateTestNavigationNodesWithChildren();

        // Act
        var result = _navigationService.FilterNodesByPlatform(nodes, PlatformType.Both);

        // Assert
        var parentNode = result.First();
        Assert.Equal("Mixed Module", parentNode.Title);
        Assert.Equal(2, parentNode.Children.Count); // 系统和租户子节点都应该保留
        
        var childTitles = parentNode.Children.Select(c => c.Title).ToList();
        Assert.Contains("System Child", childTitles);
        Assert.Contains("Tenant Child", childTitles);
    }

    #endregion

    #region 权限过滤测试

    [Fact]
    public void FilterNodesByPermission_WithValidPermission_ShouldReturnAllowedNodes()
    {
        // Arrange
        var nodes = CreateTestNavigationNodesWithPermissions();
        _mockPermissionService.Setup(x => x.HasNavigationPermission("user_management"))
            .Returns(true);
        _mockPermissionService.Setup(x => x.HasNavigationPermission("admin_panel"))
            .Returns(false);

        // Act
        var result = _navigationService.FilterNodesByPermission(nodes, _mockPermissionService.Object);

        // Assert
        Assert.Single(result);
        Assert.Equal("User Management", result[0].Title);
    }

    [Fact]
    public void FilterNodesByPermission_WithNullPermissionService_ShouldReturnAllNodes()
    {
        // Arrange
        var nodes = CreateTestNavigationNodesWithPermissions();

        // Act
        var result = _navigationService.FilterNodesByPermission(nodes, null);

        // Assert
        Assert.Equal(2, result.Count); // 所有节点都应该保留
    }

    [Fact]
    public void FilterNodesByPermission_WithEmptyPermission_ShouldReturnNode()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("test", "Test Node", "/test")
            {
                Permission = string.Empty // 无权限要求
            }
        };

        _mockPermissionService.Setup(x => x.HasNavigationPermission(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = _navigationService.FilterNodesByPermission(nodes, _mockPermissionService.Object);

        // Assert
        Assert.Single(result); // 无权限要求的节点应该保留
    }

    #endregion

    #region 上下文过滤测试

    [Fact]
    public void FilterNodesByContext_WithSystemContext_ShouldApplyAllFilters()
    {
        // Arrange
        var nodes = CreateComplexNavigationNodes();
        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.System,
            IsAuthenticated = true,
            IsDevelopment = false,
            DeviceType = "desktop"
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("System Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithUnauthenticatedUser_ShouldFilterAuthRequiredNodes()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("public", "Public Feature", "/public")
            {
                RequireAuth = false,
                PlatformType = PlatformType.Both
            },
            new NavigationNode("private", "Private Feature", "/private")
            {
                RequireAuth = true,
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            IsAuthenticated = false
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Public Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithMobileDevice_ShouldFilterDesktopOnlyNodes()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("mobile", "Mobile Feature", "/mobile")
            {
                SupportedDevices = new[] { "mobile", "tablet" },
                PlatformType = PlatformType.Both
            },
            new NavigationNode("desktop", "Desktop Feature", "/desktop")
            {
                SupportedDevices = new[] { "desktop" },
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            DeviceType = "mobile",
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Mobile Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithExperimentalFeatures_ShouldFilterInProduction()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("stable", "Stable Feature", "/stable")
            {
                IsExperimental = false,
                PlatformType = PlatformType.Both
            },
            new NavigationNode("experimental", "Experimental Feature", "/experimental")
            {
                IsExperimental = true,
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            IsDevelopment = false, // 生产环境
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Stable Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithVersionConstraints_ShouldFilterCorrectly()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("v1", "Version 1 Feature", "/v1")
            {
                MaxVersion = "2.0.0",
                PlatformType = PlatformType.Both
            },
            new NavigationNode("v2", "Version 2 Feature", "/v2")
            {
                MinVersion = "2.0.0",
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            CurrentVersion = "1.5.0",
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Version 1 Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithGroupFilter_ShouldFilterByGroup()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("admin", "Admin Feature", "/admin")
            {
                Group = "Administration",
                PlatformType = PlatformType.Both
            },
            new NavigationNode("user", "User Feature", "/user")
            {
                Group = "User",
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            GroupFilter = new[] { "Administration" },
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Admin Feature", result[0].Title);
    }

    [Fact]
    public void FilterNodesByContext_WithTagsFilter_ShouldFilterByTags()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("feature1", "Feature 1", "/feature1")
            {
                Tags = new[] { "admin", "management" },
                PlatformType = PlatformType.Both
            },
            new NavigationNode("feature2", "Feature 2", "/feature2")
            {
                Tags = new[] { "user", "public" },
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            UserTags = new[] { "admin" },
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result);
        Assert.Equal("Feature 1", result[0].Title);
    }

    #endregion

    #region 继承和排序测试

    [Fact]
    public void FilterNodesByContext_ShouldSortByOrderAndPriority()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("c", "Feature C", "/c")
            {
                Order = 2,
                Priority = 1,
                PlatformType = PlatformType.Both
            },
            new NavigationNode("a", "Feature A", "/a")
            {
                Order = 1,
                Priority = 0,
                PlatformType = PlatformType.Both
            },
            new NavigationNode("b", "Feature B", "/b")
            {
                Order = 1,
                Priority = 1,
                PlatformType = PlatformType.Both
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            IsAuthenticated = true
        };

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Feature B", result[0].Title); // Order=1, Priority=1 (最高)
        Assert.Equal("Feature A", result[1].Title); // Order=1, Priority=0
        Assert.Equal("Feature C", result[2].Title); // Order=2, Priority=1
    }

    [Fact]
    public void FilterNodesByContext_WithParentHasPermissionButChildDoesNot_ShouldIncludeParent()
    {
        // Arrange
        var nodes = new List<NavigationNode>
        {
            new NavigationNode("parent", "Parent", "/parent")
            {
                PlatformType = PlatformType.Both,
                Children = new List<NavigationNode>
                {
                    new NavigationNode("child1", "Child 1", "/child1")
                    {
                        Permission = "child1_permission",
                        PlatformType = PlatformType.Both
                    },
                    new NavigationNode("child2", "Child 2", "/child2")
                    {
                        Permission = "child2_permission",
                        PlatformType = PlatformType.Both
                    }
                }
            }
        };

        var context = new NavigationFilterContext
        {
            PlatformType = PlatformType.Both,
            IsAuthenticated = true,
            PermissionService = _mockPermissionService.Object
        };

        _mockPermissionService.Setup(x => x.HasNavigationPermission("child1_permission"))
            .Returns(true);
        _mockPermissionService.Setup(x => x.HasNavigationPermission("child2_permission"))
            .Returns(false);

        // Act
        var result = _navigationService.FilterNodesByContext(nodes, context);

        // Assert
        Assert.Single(result); // 父节点应该保留
        Assert.Single(result[0].Children); // 只有有权限的子节点保留
        Assert.Equal("Child 1", result[0].Children[0].Title);
    }

    #endregion

    #region 辅助方法

    private List<NavigationNode> CreateTestNavigationNodes()
    {
        return new List<NavigationNode>
        {
            new NavigationNode("system", "System Module", "/system")
            {
                PlatformType = PlatformType.System
            },
            new NavigationNode("tenant", "Tenant Module", "/tenant")
            {
                PlatformType = PlatformType.Tenant
            },
            new NavigationNode("universal", "Universal Module", "/universal")
            {
                PlatformType = PlatformType.Both
            },
            new NavigationNode("none", "None Module", "/none")
            {
                PlatformType = PlatformType.None
            }
        };
    }

    private List<NavigationNode> CreateTestNavigationNodesWithChildren()
    {
        return new List<NavigationNode>
        {
            new NavigationNode("mixed", "Mixed Module", "/mixed")
            {
                PlatformType = PlatformType.Both,
                Children = new List<NavigationNode>
                {
                    new NavigationNode("system-child", "System Child", "/mixed/system")
                    {
                        PlatformType = PlatformType.System
                    },
                    new NavigationNode("tenant-child", "Tenant Child", "/mixed/tenant")
                    {
                        PlatformType = PlatformType.Tenant
                    }
                }
            }
        };
    }

    private List<NavigationNode> CreateTestNavigationNodesWithPermissions()
    {
        return new List<NavigationNode>
        {
            new NavigationNode("user-mgmt", "User Management", "/users")
            {
                Permission = "user_management",
                PlatformType = PlatformType.Both
            },
            new NavigationNode("admin", "Admin Panel", "/admin")
            {
                Permission = "admin_panel",
                PlatformType = PlatformType.Both
            }
        };
    }

    private List<NavigationNode> CreateComplexNavigationNodes()
    {
        return new List<NavigationNode>
        {
            new NavigationNode("system-feature", "System Feature", "/system-feature")
            {
                PlatformType = PlatformType.System,
                RequireAuth = true,
                IsExperimental = false,
                SupportedDevices = new[] { "desktop" }
            },
            new NavigationNode("tenant-feature", "Tenant Feature", "/tenant-feature")
            {
                PlatformType = PlatformType.Tenant,
                RequireAuth = true,
                IsExperimental = true,
                SupportedDevices = new[] { "mobile" }
            }
        };
    }

    #endregion
} 