using CodeSpirit.Core;
using System.Collections.Generic;
using System.Security.Claims;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 模拟当前用户服务，用于测试
/// </summary>
public class MockCurrentUser : ICurrentUser
{
    /// <summary>
    /// 获取用户ID
    /// </summary>
    public long? Id => 123;

    /// <summary>
    /// 获取用户名
    /// </summary>
    public string UserName => "TestUser";

    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    public string[] Roles => new[] { "User", "TestRole" };

    /// <summary>
    /// 判断用户是否已认证
    /// </summary>
    public bool IsAuthenticated => true;

    /// <summary>
    /// 获取用户的所有声明（Claims）
    /// </summary>
    public IEnumerable<Claim> Claims => new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "123"),
        new Claim(ClaimTypes.Name, "TestUser"),
        new Claim(ClaimTypes.Role, "User")
    };

    /// <summary>
    /// 获取用户权限集合
    /// </summary>
    public HashSet<string> Permissions => new HashSet<string> { "read", "write", "test" };

    /// <summary>
    /// 获取当前用户的租户ID
    /// </summary>
    public string? TenantId => "test-tenant";

    /// <summary>
    /// 获取当前用户的租户名称
    /// </summary>
    public string? TenantName => "Test Tenant";

    /// <summary>
    /// 判断用户是否属于指定角色
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <returns>如果用户属于该角色返回true，否则返回false</returns>
    public bool IsInRole(string role)
    {
        return role == "User" || role == "TestRole";
    }

    /// <summary>
    /// 判断用户是否属于指定租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>如果用户属于该租户返回true，否则返回false</returns>
    public bool IsInTenant(string tenantId)
    {
        return tenantId == "test-tenant";
    }
}
