using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ApprovalApi.Configuration;

/// <summary>
/// 审批系统API服务配置
/// </summary>
public class ApprovalApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "approval";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "approval-api";
    
    /// <summary>
    /// 配置审批系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
        
        // 配置标准数据库服务（多数据库支持、仓储模式）
        this.ConfigureStandardDatabaseServices<ApprovalDbContext, MySqlApprovalDbContext, SqlServerApprovalDbContext>(
            services, configuration);
        
        // 配置标准基础设施服务（事件总线、HTTP客户端）+ 可选组件（多租户）
        this.ConfigureStandardInfrastructureServices(services, configuration, (s, c) =>
        {
            s.AddCodeSpiritMultiTenant(c);
        });
        
        // 添加审批服务
        services.AddApprovalServices(configuration);
        
        // 添加审批事件处理器
        services.AddApprovalEventHandlers();
        
        // 添加LLM服务（使用统一配置）
        services.AddLLMServices();

        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
    }
    
    /// <summary>
    /// 配置审批系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 配置标准中间件（聚合器）+ 可选组件（多租户、AI表单填充）
        await this.ConfigureStandardMiddlewareAsync(app, a =>
        {
            a.UseCodeSpiritMultiTenant();
            a.UseAiFormFillEndpoints();
        });
    }
    
    /// <summary>
    /// 审批系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 使用标准数据库初始化方法
        await this.InitializeStandardDatabaseAsync<ApprovalDbContext, MySqlApprovalDbContext, SqlServerApprovalDbContext>(
            app, "ApprovalApi");
    }
}