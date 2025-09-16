using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ApprovalApi.Data;

/// <summary>
/// MySQL 审批数据库上下文工厂
/// 用于设计时迁移生成
/// </summary>
public class MySqlApprovalDbContextFactory : IDesignTimeDbContextFactory<MySqlApprovalDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">参数</param>
    /// <returns>数据库上下文</returns>
    public MySqlApprovalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySqlApprovalDbContext>();
        
        // 使用默认MySQL连接字符串（设计时不需要真实连接）
        optionsBuilder.UseMySql("Server=localhost;Database=CodeSpirit_ApprovalApi;Uid=root;Pwd=;CharSet=utf8mb4;",
            ServerVersion.Parse("8.0.0"));
        
        // 创建模拟的服务提供者和当前用户
        var serviceProvider = new ServiceCollection()
            .AddScoped<ICurrentUser, DesignTimeCurrentUser>()
            .AddScoped<IHttpContextAccessor, DesignTimeHttpContextAccessor>()
            .BuildServiceProvider();
        
        var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        
        return new MySqlApprovalDbContext(optionsBuilder.Options, serviceProvider, currentUser, httpContextAccessor);
    }
}
