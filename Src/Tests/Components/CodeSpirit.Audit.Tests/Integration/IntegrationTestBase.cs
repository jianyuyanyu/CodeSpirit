using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using CodeSpirit.Audit.Extensions;

namespace CodeSpirit.Audit.Tests.Integration
{
    public class IntegrationTestBase
    {
        protected readonly ILogger<IntegrationTestBase> _logger;
        protected readonly ITestOutputHelper _output;

        public IntegrationTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        // 配置中间件
        protected virtual void ConfigureAuditMiddleware(IApplicationBuilder app)
        {
            // 使用审计中间件
            app.UseAudit();
            
            _output.WriteLine("已配置审计中间件");
        }
    }
} 