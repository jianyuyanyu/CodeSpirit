using CodeSpirit.Audit.Extensions;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Audit.Startup;

/// <summary>
/// 带审计功能的 API 配置基类
/// </summary>
/// <remarks>
/// 继承此类的 API 服务将自动配置审计元数据过滤器。
/// 如果 API 服务不需要审计功能，可以直接继承 <see cref="BaseApiConfiguration"/>。
/// </remarks>
public abstract class AuditAwareApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 配置审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <remarks>
    /// 自动检测 Audit:EnableMetadataFilter 配置，如果为 true 则自动添加审计元数据过滤器。
    /// 这样 API 服务无需手动配置审计过滤器。
    /// </remarks>
    protected override void ConfigureAuditMetadataFilter(IServiceCollection services, IConfiguration configuration)
    {
        // 检查是否启用审计元数据过滤器（默认启用）
        var enableMetadataFilter = configuration.GetValue<bool>("Audit:EnableMetadataFilter", defaultValue: true);
        
        if (enableMetadataFilter)
        {
            var mvcBuilder = services.AddControllers();
            mvcBuilder.AddAuditMetadataFilter();
        }
    }
}
