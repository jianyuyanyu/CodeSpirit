using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.PathfinderApi.Data;

/// <summary>
/// MySQL数据库上下文
/// </summary>
public class MySqlPathfinderDbContext : PathfinderDbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public MySqlPathfinderDbContext(
        DbContextOptions<MySqlPathfinderDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor)
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }
}

