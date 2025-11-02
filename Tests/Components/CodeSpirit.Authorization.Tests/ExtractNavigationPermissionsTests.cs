namespace CodeSpirit.Authorization.Tests;

/// <summary>
/// 导航权限提取逻辑测试
/// 注意：由于 ExtractNavigationPermissions 是 HasPermissionService 的私有方法，
/// 这里创建一个独立的测试类来验证导航权限提取逻辑
/// </summary>
public class ExtractNavigationPermissionsTests
{
    /// <summary>
    /// 提取导航权限的测试辅助方法（模拟 HasPermissionService.ExtractNavigationPermissions 的逻辑）
    /// </summary>
    private static HashSet<string> ExtractNavigationPermissions(ISet<string> permissions)
    {
        if (permissions == null || !permissions.Any())
        {
            return new HashSet<string>();
        }

        var navigationPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in permissions)
        {
            if (string.IsNullOrEmpty(permission)) continue;

            // 检查是否为通配权限（以 _* 结尾）
            if (permission.EndsWith("_*", StringComparison.OrdinalIgnoreCase))
            {
                // 通配权限：保留一级通配和二级通配
                var parts = permission.Split('_');
                
                // parts.Length <= 3 表示：
                // - module_* (2部分：module 和 *)
                // - module_controller_* (3部分：module、controller 和 *)
                if (parts.Length <= 3)
                {
                    navigationPermissions.Add(permission);
                }
            }
            else
            {
                // 具体权限：只保留二级权限（module_controller）
                var parts = permission.Split('_');
                if (parts.Length == 2)
                {
                    navigationPermissions.Add(permission);
                }
                // 三级权限（module_controller_action）不会自动提升为二级导航权限
                // 一级权限（module）也不会被提取，除非是通配权限 module_*
            }
        }

        return navigationPermissions;
    }

    /// <summary>
    /// 测试：只保留二级具体权限
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_OnlySecondLevel_Extracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_users", "identity_roles" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_users", result);
        Assert.Contains("identity_roles", result);
    }

    /// <summary>
    /// 测试：三级权限不提取
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_ThirdLevel_NotExtracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_users_create", "identity_users_update" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：保留通配权限
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_Wildcard_Extracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_*", "identity_users_*" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_*", result);
        Assert.Contains("identity_users_*", result);
    }

    /// <summary>
    /// 测试：混合权限正确提取
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_Mixed_ExtractsCorrectly()
    {
        // Arrange
        var input = new HashSet<string> 
        { 
            "identity_*", 
            "identity_users", 
            "identity_users_create", 
            "exam_questions_update" 
        };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_*", result);
        Assert.Contains("identity_users", result);
        Assert.DoesNotContain("identity_users_create", result);
        Assert.DoesNotContain("exam_questions_update", result);
    }

    /// <summary>
    /// 测试：空集合返回空结果
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_EmptySet_ReturnsEmpty()
    {
        // Arrange
        var input = new HashSet<string>();

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：null集合返回空结果
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_Null_ReturnsEmpty()
    {
        // Arrange
        ISet<string>? input = null;

        // Act
        var result = ExtractNavigationPermissions(input!);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：一级权限不提取（除非是通配）
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_FirstLevel_NotExtracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity", "exam" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：一级通配权限提取
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_FirstLevelWildcard_Extracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_*", "exam_*" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_*", result);
        Assert.Contains("exam_*", result);
    }

    /// <summary>
    /// 测试：二级通配权限提取
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_SecondLevelWildcard_Extracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_users_*", "exam_questions_*" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_users_*", result);
        Assert.Contains("exam_questions_*", result);
    }

    /// <summary>
    /// 测试：三级通配权限不提取
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_ThirdLevelWildcard_NotExtracted()
    {
        // Arrange
        var input = new HashSet<string> { "identity_users_operations_*" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：大小写不敏感
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_CaseInsensitive_WorksCorrectly()
    {
        // Arrange
        var input = new HashSet<string> { "IDENTITY_USERS", "identity_roles", "Identity_Tenants" };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("IDENTITY_USERS", result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("identity_roles", result, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Identity_Tenants", result, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 测试：包含空字符串的集合
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_ContainsEmptyString_IgnoresEmpty()
    {
        // Arrange
        var input = new HashSet<string?> { "", "identity_users", null, "exam_questions" };

        // Act
        var result = ExtractNavigationPermissions(input!);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("identity_users", result);
        Assert.Contains("exam_questions", result);
    }

    /// <summary>
    /// 测试：全是三级权限时返回空
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_AllThirdLevel_ReturnsEmpty()
    {
        // Arrange
        var input = new HashSet<string> 
        { 
            "identity_users_create", 
            "identity_users_update",
            "exam_questions_delete",
            "exam_exams_start"
        };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// 测试：复杂混合场景
    /// </summary>
    [Fact]
    public void ExtractNavigationPermissions_ComplexMix_ExtractsCorrectly()
    {
        // Arrange
        var input = new HashSet<string> 
        { 
            "identity",                     // 一级具体，不提取
            "identity_*",                   // 一级通配，提取
            "identity_users",               // 二级具体，提取
            "identity_users_*",             // 二级通配，提取
            "identity_users_create",        // 三级具体，不提取
            "identity_roles_update_special_*", // 四级通配，不提取
            "exam_questions",               // 二级具体，提取
            "exam_exams_start"             // 三级具体，不提取
        };

        // Act
        var result = ExtractNavigationPermissions(input);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains("identity_*", result);
        Assert.Contains("identity_users", result);
        Assert.Contains("identity_users_*", result);
        Assert.Contains("exam_questions", result);
        
        Assert.DoesNotContain("identity", result);
        Assert.DoesNotContain("identity_users_create", result);
        Assert.DoesNotContain("identity_roles_update_special_*", result);
        Assert.DoesNotContain("exam_exams_start", result);
    }
}

