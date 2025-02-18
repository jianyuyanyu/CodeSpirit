using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Configuration;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.MappingProfiles;
using CodeSpirit.Amis.Middleware;
using CodeSpirit.Amis.Services;
using CodeSpirit.Amis.Validators;
using FluentValidation;
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

            services.AddScoped<ISiteConfigurationService, SiteConfigurationService>();

            // 注册 AutoMapper 并扫描指定的程序集中的配置文件
            services.AddAutoMapper(typeof(PageMappingProfile));

            // 注册 PageValidator
            services.AddTransient<IValidator<Page>, PageValidator>();
            // 注册 PageCollector
            services.AddScoped<IPageCollector, PageCollector>();
            // 配置读取 PagesConfiguration 部分
            services.Configure<PagesConfiguration>(configuration.GetSection("PagesConfiguration"));

            // 注册 FluentValidation 验证器
            // services.AddValidatorsFromAssemblyContaining<PageValidator>();

            // 注册特定验证器
            services.AddTransient<IValidator<Page>, PageValidator>();
            services.AddScoped<IPageCollector, PageCollector>();
            return services;
        }

        public static IApplicationBuilder UseAmis(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AmisMiddleware>();
        }
    }

}
