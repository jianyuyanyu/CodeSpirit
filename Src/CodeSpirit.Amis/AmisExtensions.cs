using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Configuration;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.MappingProfiles;
using CodeSpirit.Amis.Services;
using CodeSpirit.Amis.Validators;
using CodeSpirit.Core.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeSpirit.Amis
{
    public static class AmisExtensions
    {
        public static IServiceCollection AddAmisServices(this IServiceCollection services, IConfiguration configuration,Assembly apiAssembly = null)
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
            services.AddScoped<AmisContext>();

            // 注册工厂
            services.AddTransient<IAmisFieldFactory, AmisInputImageFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisSelectFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputTreeFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisFieldAttributeFactory>();

            // 注册 AmisGenerator，并传递可选的 apiAssembly
            services.AddScoped<AmisGenerator>(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var permissionService = sp.GetRequiredService<IPermissionService>();
                var amisContext = sp.GetRequiredService<AmisContext>();
                var cachingHelper = sp.GetRequiredService<CachingHelper>();
                var controllerHelper = sp.GetRequiredService<ControllerHelper>();
                var crudHelper = sp.GetRequiredService<CrudHelper>();
                var serviceProvider = sp;

                // 如果未提供 apiAssembly，则使用调用程序集
                var assembly = apiAssembly ?? Assembly.GetCallingAssembly();

                return new AmisGenerator(httpContextAccessor, permissionService, amisContext, cachingHelper, controllerHelper, crudHelper, serviceProvider, assembly);
            });

            services.AddScoped<ISiteConfigurationService, SiteConfigurationService>();

            // 注册 AutoMapper 并扫描指定的程序集中的配置文件
            services.AddAutoMapper(typeof(PageMappingProfile));

            // 注册 PageValidator
            services.AddTransient<IValidator<Page>, PageValidator>();
            // 注册 PageCollector
            services.AddScoped<IPageCollector, PageCollector>();
            // 配置读取 PagesConfiguration 部分
            services.Configure<PagesConfiguration>(configuration.GetSection("PagesConfiguration"));
            return services;
        }
    }
}
