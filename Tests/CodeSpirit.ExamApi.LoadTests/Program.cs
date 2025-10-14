using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Serilog;
using CodeSpirit.ExamApi.LoadTests.Scenarios;
using CodeSpirit.ExamApi.LoadTests.Services;
using CodeSpirit.ExamApi.LoadTests.Models;

namespace CodeSpirit.ExamApi.LoadTests;

/// <summary>
/// NBomber 负载测试程序入口
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/loadtest-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddSerilog(Log.Logger));
        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            logger.LogInformation("开始初始化负载测试...");

            // 加载配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 创建测试场景
            var scenarios = await CreateTestScenariosAsync(configuration, logger);

            // 选择要运行的场景
            var selectedScenarios = SelectScenarios(scenarios, args, logger);

            // 执行负载测试
            ExecuteLoadTestAsync(selectedScenarios, configuration, logger);

            logger.LogInformation("负载测试完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "负载测试执行失败");
            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 创建测试场景
    /// </summary>
    private static async Task<Dictionary<string, NBomber.Contracts.ScenarioProps>> CreateTestScenariosAsync(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger)
    {
        logger.LogInformation("正在创建测试场景...");

        // 初始化测试用户
        var userService = new TestUserService();
        var userCount = await userService.LoadUsersFromFileAsync("testusers.json");
        logger.LogInformation("已加载 {UserCount} 个测试用户", userCount);

        var scenarioLogger = LoggerFactory.Create(builder => builder.AddSerilog()).CreateLogger<ExamClientScenarios>();
        var examScenarios = new ExamClientScenarios(configuration, scenarioLogger, userService);

        var scenarios = new Dictionary<string, NBomber.Contracts.ScenarioProps>
        {
            ["get-profile"] = examScenarios.CreateGetProfileScenario(),
            ["get-available-exams"] = examScenarios.CreateGetAvailableExamsScenario(),
            ["get-exam-basic-info"] = examScenarios.CreateGetExamBasicInfoScenario(),
            ["get-exam-light-info"] = examScenarios.CreateGetExamLightInfoScenario(),
            ["start-exam"] = examScenarios.CreateStartExamScenario(),
            ["record-screen-switch"] = examScenarios.CreateRecordScreenSwitchScenario(),
            ["mixed-operations"] = examScenarios.CreateMixedOperationsScenario(),
            ["full-exam-flow"] = examScenarios.CreateFullExamFlowScenario()
        };

        logger.LogInformation("已创建 {ScenarioCount} 个测试场景", scenarios.Count);
        return scenarios;
    }

    /// <summary>
    /// 选择要运行的场景
    /// </summary>
    private static List<NBomber.Contracts.ScenarioProps> SelectScenarios(Dictionary<string, NBomber.Contracts.ScenarioProps> scenarios, string[] args, Microsoft.Extensions.Logging.ILogger logger)
    {
        var selectedScenarios = new List<NBomber.Contracts.ScenarioProps>();

        if (args.Length > 0)
        {
            foreach (var scenarioName in args)
            {
                if (scenarios.TryGetValue(scenarioName, out var scenario))
                {
                    selectedScenarios.Add(scenario);
                    logger.LogInformation("已选择场景: {ScenarioName}", scenarioName);
                }
                else
                {
                    logger.LogWarning("未找到场景: {ScenarioName}", scenarioName);
                }
            }
        }

        if (selectedScenarios.Count == 0)
        {
            // 默认运行完整考试流程场景
            selectedScenarios.Add(scenarios["full-exam-flow"]);
            logger.LogInformation("使用默认场景: full-exam-flow");
        }

        return selectedScenarios;
    }

    /// <summary>
    /// 执行负载测试
    /// </summary>
    private static void ExecuteLoadTestAsync(List<NBomber.Contracts.ScenarioProps> scenarios, IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger)
    {
        logger.LogInformation("开始执行负载测试，场景数量: {Count}", scenarios.Count);

        // 读取负载测试场景配置
        var loadTestScenarios = configuration.GetSection("LoadTestScenarios").Get<LoadTestScenarios>();
        if (loadTestScenarios == null)
        {
            logger.LogWarning("未找到负载测试场景配置，使用默认配置");
            loadTestScenarios = new LoadTestScenarios();
        }

        var testName = $"CodeSpirit_ExamClient_LoadTest_{DateTime.Now:yyyyMMdd_HHmmss}";
        logger.LogInformation("测试名称: {TestName}", testName);

        // 选择要执行的负载场景（可以通过命令行参数指定）
        var selectedLoadScenario = SelectLoadScenario(loadTestScenarios, logger);
        
        logger.LogInformation("负载场景配置: 持续时间={Duration}, 速率={Rate}RPS", 
            selectedLoadScenario.Duration, selectedLoadScenario.Rate);

        // 解析持续时间
        if (!TimeSpan.TryParse(selectedLoadScenario.Duration, out var duration))
        {
            logger.LogWarning("无法解析持续时间 {Duration}，使用默认值 1 分钟", selectedLoadScenario.Duration);
            duration = TimeSpan.FromMinutes(1);
        }

        // 为每个场景应用负载配置
        var configuredScenarios = scenarios.Select(scenario => 
            scenario.WithLoadSimulations(
                Simulation.Inject(rate: selectedLoadScenario.Rate, interval: TimeSpan.FromSeconds(1), during: duration)
            )
        ).ToArray();

        // 配置并运行负载测试
        var stats = NBomberRunner
            .RegisterScenarios(configuredScenarios)
            .WithReportFolder("reports")
            .WithReportFileName(testName)
            .Run();
        
        logger.LogInformation("负载测试执行完成");
        logger.LogInformation("测试报告已生成到 reports 目录");
    }

    /// <summary>
    /// 选择负载场景
    /// </summary>
    private static LoadTestScenario SelectLoadScenario(LoadTestScenarios scenarios, Microsoft.Extensions.Logging.ILogger logger)
    {
        // 可以通过环境变量指定负载场景
        var loadScenarioName = Environment.GetEnvironmentVariable("LOAD_SCENARIO") ?? "NormalLoad";
        
        var selectedScenario = loadScenarioName.ToLower() switch
        {
            "warmup" => scenarios.WarmUp,
            "normalload" => scenarios.NormalLoad,
            "peakload" => scenarios.PeakLoad,
            "stresstest" => scenarios.StressTest,
            _ => scenarios.NormalLoad
        };
        
        logger.LogInformation("使用负载场景: {ScenarioName}", loadScenarioName);
        return selectedScenario;
    }
}