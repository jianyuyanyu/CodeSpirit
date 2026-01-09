using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Form.Factories;
using CodeSpirit.Amis.Form.Fields;
using CodeSpirit.Amis.Handlers;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// Amis扩展方法
    /// </summary>
    public static class AmisExtensions
    {
        public static IServiceCollection AddAmisServices(this IServiceCollection services, IConfiguration configuration, Assembly apiAssembly = null)
        {
            // 注册 CultureResolver（统一的文化信息解析器）
            services.AddScoped<CultureResolver>();
            
            // 注册 CachingHelper（使用 CultureResolver）
            services.AddScoped<CachingHelper>();
            
            services.AddScoped<ControllerHelper>();
            services.AddScoped<CrudHelper>();
            
            // 注册 UtilityHelper（使用 CultureResolver）
            services.AddScoped<UtilityHelper>();
            services.AddScoped<AmisApiHelper>();
            services.AddScoped<ApiRouteHelper>();
            // 使用延迟解析避免循环依赖：
            // ColumnHelper -> ButtonHelper -> CrudDialogHandler -> ColumnHelper
            // CrudDialogHandler 和 ButtonHelper 都使用 IServiceProvider 延迟解析依赖
            services.AddScoped<ColumnHelper>();
            services.AddScoped<CrudDialogHandler>();
            services.AddScoped<ButtonHelper>();
            services.AddScoped<FormFieldHelper>();
            services.AddScoped<SearchFieldHelper>();
            services.AddScoped<AsideHelper>();
            services.AddScoped<TabsHelper>();
            services.AddScoped<CardHelper>();
            services.AddScoped<StatisticsCardsHelper>();
            services.AddScoped<AmisCRUDConfigBuilder>();
            services.AddScoped<StatisticsConfigBuilder>();
            services.AddScoped<SettingsPageConfigBuilder>();
            services.AddScoped<AmisContext>();

            // 注册AI表单增强器
            services.AddScoped<AiFormFieldEnhancer>();

            // 注册工厂 - 注意：更具体的工厂要先注册，通用工厂要后注册
            services.AddTransient<IAmisFieldFactory, AmisInputImageFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputTextFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisSelectFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisListSelectFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputTreeFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTreeSelectFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisInputExcelFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisEnhancedImportFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisIconFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTextareaFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisNumberFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTransferFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisArrayFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTableFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisDateFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisTimeFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisDatetimeFieldFactory>();
            services.AddTransient<IAmisFieldFactory, AmisSwitchFieldFactory>();
            services.AddTransient<IAmisFieldFactory, FormGroupFieldFactory>();
            // 通用工厂放在最后，作为兜底
            services.AddTransient<IAmisFieldFactory, AmisFieldAttributeFactory>();

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
