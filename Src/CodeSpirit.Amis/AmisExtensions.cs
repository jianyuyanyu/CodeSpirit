using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeSpirit.Amis
{
    public static class AmisExtensions
    {
        public static IServiceCollection AddAmisServices(this IServiceCollection services, IConfiguration configuration, Assembly apiAssembly = null)
        {
            services.AddScoped<CachingHelper>();
            services.AddScoped<ControllerHelper>();
            services.AddScoped<CrudHelper>();
            services.AddSingleton<UtilityHelper>();
            services.AddScoped<AmisApiHelper>();
            services.AddScoped<ApiRouteHelper>();
            services.AddScoped<ColumnHelper>();
            services.AddScoped<ButtonHelper>();
            services.AddScoped<FormFieldHelper>();
            services.AddScoped<SearchFieldHelper>();
            services.AddScoped<AmisConfigBuilder>();
            services.AddScoped<StatisticsConfigBuilder>();
            services.AddScoped<AmisContext>();

            // 注册工厂
            services.AddTransient<IAmisFieldFactory, AmisInputImageFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisSelectFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputTreeFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputExcelFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisFieldAttributeFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTextareaFieldFactory>();

            // 注册 AmisGenerator，并传递可选的 apiAssembly
            services.AddScoped<AmisGenerator>();

            // 注册 FluentValidation 验证器
            // services.AddValidatorsFromAssemblyContaining<PageValidator>();

            return services;
        }

        public static IApplicationBuilder UseAmis(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AmisMiddleware>();
        }
    }

}
