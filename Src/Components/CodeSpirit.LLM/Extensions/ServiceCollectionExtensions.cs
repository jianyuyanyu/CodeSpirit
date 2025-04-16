using CodeSpirit.LLM.Caching;
using CodeSpirit.LLM.Models;
using CodeSpirit.LLM.Services.Implementations;
using CodeSpirit.LLM.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Anthropic;
using Microsoft.SemanticKernel.Plugins.Core;

namespace CodeSpirit.LLM.Extensions
{
    /// <summary>
    /// LLM服务扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加LLM服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLLMService(this IServiceCollection services, Action<LLMOptions> configureOptions)
        {
            // 配置选项
            services.Configure<LLMOptions>(options => configureOptions(options));
            services.Configure<LLMCacheOptions>(options => { });
            
            // 注册缓存
            services.AddMemoryCache();
            services.AddSingleton<ILLMCacheService, MemoryLLMCacheService>();
            
            // 构建并注册Kernel
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LLMOptions>>().Value;
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                
                var builder = Kernel.CreateBuilder();
                builder.Services.AddLogging(loggingBuilder => 
                {
                    loggingBuilder.AddProvider(loggerFactory);
                });
                
                // 根据服务类型配置不同的LLM提供商
                switch (options.ServiceType)
                {
                    case LLMServiceType.OpenAI:
                        builder.AddOpenAIChatCompletion(
                            modelId: options.DefaultModel,
                            apiKey: options.ApiKey
                        );
                        
                        // 如果需要嵌入服务，添加OpenAI嵌入
                        builder.AddOpenAITextEmbeddingGeneration("text-embedding-3-small", options.ApiKey);
                        break;
                        
                    case LLMServiceType.AzureOpenAI:
                        builder.AddAzureOpenAIChatCompletion(
                            deploymentName: options.DeploymentName,
                            endpoint: options.Endpoint,
                            apiKey: options.ApiKey
                        );
                        
                        // 如果需要嵌入服务，添加Azure OpenAI嵌入
                        // 注意：需要具有嵌入模型的部署
                        try
                        {
                            builder.AddAzureOpenAITextEmbeddingGeneration(
                                deploymentName: "text-embedding-ada-002", // 可能需要从选项中获取
                                endpoint: options.Endpoint,
                                apiKey: options.ApiKey
                            );
                        }
                        catch (Exception ex)
                        {
                            // 嵌入服务添加失败，继续
                            var logger = loggerFactory.CreateLogger("LLMService");
                            logger.LogWarning(ex, "Azure OpenAI嵌入服务初始化失败");
                        }
                        break;
                        
                    case LLMServiceType.Anthropic:
                        // Anthropic Claude模型
                        builder.AddAnthropicChatCompletion(
                            modelId: options.DefaultModel,
                            apiKey: options.ApiKey
                        );
                        break;
                        
                    default:
                        throw new ArgumentOutOfRangeException(nameof(options.ServiceType), "不支持的LLM服务类型");
                }
                
                // 添加核心插件
                builder.Plugins.AddFromType<ConversationSummaryPlugin>();
                builder.Plugins.AddFromType<TextPlugin>();
                builder.Plugins.AddFromType<TimePlugin>();
                
                return builder.Build();
            });
            
            // 注册服务实现
            services.AddScoped<ILLMService, SemanticKernelLLMService>();
            
            return services;
        }
        
        /// <summary>
        /// 添加LLM服务（从配置中读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLLMService(this IServiceCollection services, IConfiguration configuration, string sectionName = "LLM")
        {
            services.Configure<LLMOptions>(configuration.GetSection(sectionName));
            services.Configure<LLMCacheOptions>(configuration.GetSection($"{sectionName}:Cache"));
            
            return services.AddLLMService(options => { });
        }
        
        /// <summary>
        /// 添加到WebApplication构建器
        /// </summary>
        /// <param name="builder">WebApplication构建器</param>
        /// <returns>WebApplication构建器</returns>
        public static WebApplicationBuilder AddLLMService(this WebApplicationBuilder builder)
        {
            builder.Services.AddLLMService(builder.Configuration);
            return builder;
        }
        
        /// <summary>
        /// 配置LLM中间件
        /// </summary>
        /// <param name="app">Web应用程序</param>
        /// <returns>Web应用程序</returns>
        public static IApplicationBuilder UseLLMService(this IApplicationBuilder app)
        {
            // 这里可以添加一些中间件，例如监控或日志
            return app;
        }
    }
} 