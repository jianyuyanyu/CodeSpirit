using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using CodeSpirit.ServiceDefaults.Messaging;

namespace CodeSpirit.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string appName) where TBuilder : IHostApplicationBuilder
    {
        builder.AddSeqEndpoint(connectionName: "seq");
        builder.AddRedisDistributedCache(connectionName: "cache", (settings) =>
        {
            settings.DisableHealthChecks = true;
            settings.DisableTracing = true;
        });

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        // 添加多个键控RabbitMQ客户端以支持不同用途
        builder.AddRabbitMQClients();

        //if (builder.Environment.IsProduction())
        //{
        //    //k8s
        //    builder.Services.AddServiceDiscoveryCore();
        //    builder.Services.AddDnsSrvServiceEndpointProvider();
        //}
        //else
        {
            // 添加服务发现
            builder.Services.AddServiceDiscovery();
        }
        
        // 配置 HttpClient 默认使用服务发现
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            //http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        //builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        //{
        //    //options.AllowedSchemes = ["https"];
        //});

        // 添加基础本地化服务（确保 IStringLocalizerFactory 始终可用）
        builder.Services.AddLocalization();

        // 添加Settings和本地化服务（通过反射动态加载，避免循环依赖）
        TryAddSettingsAndLocalizationServices(builder);

        return builder;
    }

    /// <summary>
    /// 尝试添加Settings和本地化服务（通过反射，避免循环依赖）
    /// </summary>
    private static void TryAddSettingsAndLocalizationServices(IHostApplicationBuilder builder)
    {
        try
        {
            // 1. 先尝试加载 Settings 服务（本地化服务依赖它）
            try
            {
                var settingsAssembly = System.Reflection.Assembly.Load("CodeSpirit.Settings");
                var settingsExtensionsType = settingsAssembly.GetType("CodeSpirit.Settings.Extensions.SettingsExtensions");
                if (settingsExtensionsType != null)
                {
                    // 获取所有 AddSettingsManagerWithDatabase 方法（有重载）
                    var methods = settingsExtensionsType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        .Where(m => m.Name == "AddSettingsManagerWithDatabase")
                        .ToArray();
                    
                    // 查找只需要 IServiceCollection 和 IConfiguration 的重载
                    var method = methods.FirstOrDefault(m =>
                    {
                        var parameters = m.GetParameters();
                        return parameters.Length == 2 &&
                               parameters[0].ParameterType == typeof(IServiceCollection) &&
                               parameters[1].ParameterType == typeof(IConfiguration);
                    });
                    
                    // 如果找不到，使用有3个参数的版本，第三个参数传null
                    if (method == null)
                    {
                        method = methods.FirstOrDefault(m => m.GetParameters().Length >= 2);
                    }
                    
                    if (method != null)
                    {
                        var parameters = method.GetParameters();
                        var args = new object[parameters.Length];
                        args[0] = builder.Services;
                        args[1] = builder.Configuration;
                        // 剩余参数设为null（可选参数）
                        for (int i = 2; i < parameters.Length; i++)
                        {
                            args[i] = null;
                        }
                        method.Invoke(null, args);
                    }
                }
            }
            catch
            {
                // Settings 服务加载失败，继续尝试加载本地化服务（可能不需要Settings）
            }
            
            // 2. 然后加载 Localization 服务
            var localizationAssembly = System.Reflection.Assembly.Load("CodeSpirit.Localization");
            var extensionsType = localizationAssembly.GetType("CodeSpirit.Localization.Extensions.LocalizationExtensions");
            if (extensionsType != null)
            {
                // AddCodeSpiritLocalization 方法签名: IServiceCollection AddCodeSpiritLocalization(this IServiceCollection services, IConfiguration configuration)
                var method = extensionsType.GetMethod("AddCodeSpiritLocalization", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(IServiceCollection), typeof(IConfiguration) },
                    null);
                if (method != null)
                {
                    method.Invoke(null, new object[] { builder.Services, builder.Configuration });
                }
            }
        }
        catch
        {
            // 如果 Settings 或 Localization 组件未加载，忽略错误
            // 各个服务项目可以在自己的 Program.cs 中单独注册
            // 例如：
            // builder.Services.AddSettingsManagerWithDatabase(builder.Configuration);
            // builder.Services.AddCodeSpiritLocalization(builder.Configuration);
        }
    }

    /// <summary>
    /// 添加多个键控RabbitMQ客户端
    /// </summary>
    /// <typeparam name="TBuilder">构建器类型</typeparam>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>构建器</returns>
    public static TBuilder AddRabbitMQClients<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // 默认RabbitMQ客户端（主要用于向后兼容）
        builder.AddRabbitMQClient(
            "rabbitmq",
            static settings => settings.DisableHealthChecks = true);

        //// 事件总线专用客户端
        //builder.AddKeyedRabbitMQClient("eventbus", settings =>
        //{
        //    // 可以为事件总线配置特定的参数
        //    settings.DisableHealthChecks = true;
        //    settings.DisableTracing = false; // 事件总线需要跟踪
        //});

        //// 审计服务专用客户端
        //builder.AddKeyedRabbitMQClient("audit", settings =>
        //{
        //    // 审计服务可能需要不同的配置
        //    settings.DisableHealthChecks = true;
        //    settings.DisableTracing = true; // 审计不需要跟踪以避免循环
        //});

        //// 通用消息服务专用客户端
        //builder.AddKeyedRabbitMQClient("messaging", settings =>
        //{
        //    settings.DisableHealthChecks = true;
        //    settings.DisableTracing = false; // 通用消息需要跟踪
        //});

        // 注册RabbitMQ服务工厂
        builder.Services.AddSingleton<IRabbitMQServiceFactory, RabbitMQServiceFactory>();

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                Console.WriteLine($"Application Name: {builder.Environment.ApplicationName}");
                tracing.AddSource(builder.Environment.ApplicationName)  // ApplicationName can be configured via ASPNETCORE_APPLICATIONNAME environment variable
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // 为每个请求添加应用程序ID标签
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("app.id", builder.Environment.ApplicationName);
                            // activity.SetTag("app.service", builder.Environment.ApplicationName);
                        };
                        
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("app.id", builder.Environment.ApplicationName);
                        };
                    })
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }


        return app;
    }
}
