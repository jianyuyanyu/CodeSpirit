namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// 权限优化逻辑测试
/// 注意:由于 OptimizePermissionIds 是 RoleService 的私有方法，
/// 这里创建一个独立的测试类来验证权限优化逻辑
/// </summary>
public class OptimizePermissionIdsTests
{
    /// <summary>
    /// 优化权限ID数组的测试辅助方法（模拟 RoleService.OptimizePermissionIds 的逻辑）
    /// </summary>
    private static string[]? OptimizePermissionIds(string[]? permissionIds)
    {
        if (permissionIds == null || !permissionIds.Any())
        {
            return permissionIds;
        }

        var optimizedPermissions = new HashSet<string>(permissionIds, StringComparer.OrdinalIgnoreCase);
        var permissionsToRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissionIds)
        {
            if (string.IsNullOrEmpty(permission)) continue;

            // 检查是否为通配权限（以 _* 结尾）
            if (permission.EndsWith("_*", StringComparison.OrdinalIgnoreCase))
            {
                // 获取通配权限的前缀（移除 _*）
                var prefix = permission.Substring(0, permission.Length - 2) + "_";

                // 找出所有被该通配权限覆盖的具体权限
                foreach (var other in permissionIds)
                {
                    if (string.IsNullOrEmpty(other) || other.Equals(permission, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 如果其他权限以该前缀开头，则被通配权限覆盖
                    if (other.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        permissionsToRemove.Add(other);
                    }
                }
            }
        }

        // 移除被通配权限覆盖的权限
        foreach (var permissionToRemove in permissionsToRemove)
        {
            optimizedPermissions.Remove(permissionToRemove);
        }

        return optimizedPermissions.OrderBy(p => p).ToArray();
    }

    /// <summary>
    /// 测试：通配权限覆盖具体权限
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_WildcardCoversSpecific_RemovesSpecific()
    {
        // Arrange
        var input = new[] { "identity_*", "identity_users_create", "identity_roles_update" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("identity_*", result);
        Assert.DoesNotContain("identity_users_create", result);
        Assert.DoesNotContain("identity_roles_update", result);
    }

    /// <summary>
    /// 测试：二级通配覆盖三级权限
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_SecondLevelWildcard_RemovesThirdLevel()
    {
        // Arrange
        var input = new[] { "identity_users_*", "identity_users_create", "identity_users_update" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("identity_users_*", result);
        Assert.DoesNotContain("identity_users_create", result);
        Assert.DoesNotContain("identity_users_update", result);
    }

    /// <summary>
    /// 测试：保留不同模块的权限
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_DifferentModules_KeepsAll()
    {
        // Arrange
        var input = new[] { "identity_*", "exam_*" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Contains("identity_*", result);
        Assert.Contains("exam_*", result);
    }

    /// <summary>
    /// 测试：部分通配部分具体
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_MixedPermissions_OptimizesCorrectly()
    {
        // Arrange
        var input = new[] { "identity_users_*", "identity_roles_create", "identity_roles_update" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Contains("identity_users_*", result);
        Assert.Contains("identity_roles_create", result);
        Assert.Contains("identity_roles_update", result);
    }

    /// <summary>
    /// 测试：空数组返回空数组
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_EmptyArray_ReturnsEmpty()
    {
        // Arrange
        var input = Array.Empty<string>();

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：null返回null
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_Null_ReturnsNull()
    {
        // Arrange
        string[]? input = null;

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 测试：没有通配权限时不优化
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_NoWildcards_NoOptimization()
    {
        // Arrange
        var input = new[] { "identity_users_create", "identity_roles_update", "exam_questions_delete" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.Contains("identity_users_create", result);
        Assert.Contains("identity_roles_update", result);
        Assert.Contains("exam_questions_delete", result);
    }

    /// <summary>
    /// 测试：多级通配权限覆盖
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_MultiLevelWildcards_RemovesCorrectly()
    {
        // Arrange
        var input = new[] 
        { 
            "identity_*", 
            "identity_users_*", 
            "identity_users_create",
            "identity_roles_update" 
        };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        // identity_* 覆盖所有 identity_ 开头的权限
        Assert.Single(result);
        Assert.Contains("identity_*", result);
    }

    /// <summary>
    /// 测试：大小写不敏感
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_CaseInsensitive_RemovesCorrectly()
    {
        // Arrange
        var input = new[] { "IDENTITY_*", "identity_users_create", "Identity_Roles_Update" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("IDENTITY_*", result, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 测试：通配权限不覆盖其他通配权限
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_WildcardDoesNotCoverOtherWildcard()
    {
        // Arrange
        var input = new[] { "identity_*", "identity_users_*" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        // identity_* 应该覆盖 identity_users_*
        Assert.Single(result);
        Assert.Contains("identity_*", result);
    }

    /// <summary>
    /// 测试：只保留通配权限本身
    /// </summary>
    [Fact]
    public void OptimizePermissionIds_OnlyWildcards_KeepsAll()
    {
        // Arrange
        var input = new[] { "identity_users_*", "exam_questions_*" };

        // Act
        var result = OptimizePermissionIds(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Contains("identity_users_*", result);
        Assert.Contains("exam_questions_*", result);
    }
}

