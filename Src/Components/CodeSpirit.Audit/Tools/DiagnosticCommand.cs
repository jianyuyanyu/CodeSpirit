using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Tools;

/// <summary>
/// GreptimeDB诊断命令行工具
/// </summary>
public class DiagnosticCommand
{
    /// <summary>
    /// 运行诊断命令
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("=== CodeSpirit.Audit GreptimeDB 诊断工具 ===");
        Console.WriteLine();

        try
        {
            // 创建配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // 创建服务容器
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<DiagnosticCommand>>();

            // 显示配置信息
            var auditConfig = configuration.GetSection("Audit");
            if (!auditConfig.Exists())
            {
                Console.WriteLine("❌ 未找到 Audit 配置节，请检查 appsettings.json");
                return 1;
            }

            var greptimeDbConfig = auditConfig.GetSection("GreptimeDB");
            if (!greptimeDbConfig.Exists())
            {
                Console.WriteLine("❌ 未找到 Audit:GreptimeDB 配置节");
                return 1;
            }

            // 绑定配置
            var options = new GreptimeDbOptions();
            greptimeDbConfig.Bind(options);

            Console.WriteLine("📋 当前配置:");
            Console.WriteLine($"   URL: {options.Url}");
            Console.WriteLine($"   Database: {options.Database}");
            Console.WriteLine($"   TableName: {options.TableName}");
            Console.WriteLine($"   TablePrefix: {options.TablePrefix}");
            Console.WriteLine($"   Username: {(string.IsNullOrEmpty(options.Username) ? "(未设置)" : "***")}");
            Console.WriteLine($"   Password: {(string.IsNullOrEmpty(options.Password) ? "(未设置)" : "***")}");
            Console.WriteLine($"   TimeoutSeconds: {options.TimeoutSeconds}");
            Console.WriteLine($"   BatchSize: {options.BatchSize}");
            Console.WriteLine();

            // 创建诊断工具
            var diagnosticLogger = serviceProvider.GetRequiredService<ILogger<GreptimeDbDiagnosticTool>>();
            var diagnosticTool = new GreptimeDbDiagnosticTool(diagnosticLogger, configuration);

            // 运行诊断
            Console.WriteLine("🔍 开始运行诊断测试...");
            Console.WriteLine();

            var result = await diagnosticTool.RunFullDiagnosticAsync();

            // 显示结果
            DisplayTestResult("基础连接测试", result.ConnectionTest);
            DisplayTestResult("健康检查端点", result.HealthEndpointTest);
            DisplayTestResult("SQL查询测试", result.SqlQueryTest);
            DisplayTestResult("数据库创建", result.DatabaseCreationTest);
            DisplayTestResult("表创建测试", result.TableCreationTest);
            DisplayTestResult("数据写入测试", result.WriteTest);

            Console.WriteLine();
            Console.WriteLine("📊 诊断总结:");
            
            if (result.AllTestsPassed)
            {
                Console.WriteLine("✅ 所有测试通过！GreptimeDB 连接正常。");
                return 0;
            }
            else
            {
                Console.WriteLine("❌ 存在测试失败，请检查以下问题:");
                
                if (!result.ConnectionTest.Success)
                {
                    Console.WriteLine("   • 检查 GreptimeDB 服务是否正在运行");
                    Console.WriteLine("   • 验证 URL 配置是否正确");
                    Console.WriteLine("   • 检查网络连接");
                }
                
                if (!result.HealthEndpointTest.Success)
                {
                    Console.WriteLine("   • GreptimeDB 健康检查端点不可用");
                }
                
                if (!result.SqlQueryTest.Success)
                {
                    Console.WriteLine("   • SQL 查询功能异常");
                    Console.WriteLine("   • 检查数据库配置");
                }
                
                if (!result.DatabaseCreationTest.Success)
                {
                    Console.WriteLine("   • 数据库创建失败");
                    Console.WriteLine("   • 检查用户权限");
                }
                
                if (!result.TableCreationTest.Success)
                {
                    Console.WriteLine("   • 表创建失败");
                    Console.WriteLine("   • 检查 SQL 语法或权限");
                }
                
                if (!result.WriteTest.Success)
                {
                    Console.WriteLine("   • 数据写入失败");
                    Console.WriteLine("   • 检查表结构和数据格式");
                }
                
                Console.WriteLine();
                Console.WriteLine("💡 建议解决步骤:");
                Console.WriteLine("   1. 确保 GreptimeDB 容器正在运行");
                Console.WriteLine("   2. 检查端口是否正确映射 (默认 4000)");
                Console.WriteLine("   3. 验证配置文件中的连接参数");
                Console.WriteLine("   4. 查看 GreptimeDB 容器日志");
                
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 诊断过程中发生异常: {ex.Message}");
            Console.WriteLine($"异常详情: {ex}");
            return 1;
        }
    }

    /// <summary>
    /// 显示测试结果
    /// </summary>
    private static void DisplayTestResult(string testName, TestResult result)
    {
        var status = result.Success ? "✅" : "❌";
        Console.WriteLine($"{status} {testName}: {result.Message}");
        
        if (!string.IsNullOrEmpty(result.Details) && !result.Success)
        {
            Console.WriteLine($"   详情: {result.Details}");
        }
    }
}

/// <summary>
/// 程序入口点
/// </summary>
public class Program
{
    /// <summary>
    /// 主函数
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        return await DiagnosticCommand.RunAsync(args);
    }
}
