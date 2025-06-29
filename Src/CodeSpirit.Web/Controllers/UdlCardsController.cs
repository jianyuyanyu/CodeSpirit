using Microsoft.AspNetCore.Mvc;
using CodeSpirit.Core;
using CodeSpirit.UdlCards.Models;
using CodeSpirit.UdlCards.Core;

namespace CodeSpirit.Web.Controllers;

/// <summary>
/// UDL Cards 演示控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class UdlCardsController : ApiControllerBase
{
    private readonly UdlCardsGenerator _cardsGenerator;
    private readonly ILogger<UdlCardsController> _logger;

    /// <summary>
    /// 初始化 UDL Cards 控制器
    /// </summary>
    /// <param name="cardsGenerator">UDL Cards 生成器</param>
    /// <param name="logger">日志记录器</param>
    public UdlCardsController(
        UdlCardsGenerator cardsGenerator,
        ILogger<UdlCardsController> logger)
    {
        _cardsGenerator = cardsGenerator ?? throw new ArgumentNullException(nameof(cardsGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取监控场景的卡片配置
    /// </summary>
    /// <param name="scenario">监控场景：exam, system, resource, security</param>
    /// <returns>卡片配置列表</returns>
    [HttpGet("monitor/{scenario}")]
    public async Task<ActionResult<ApiResponse<List<Dictionary<string, object>>>>> GetMonitorCards(string scenario)
    {
        try
        {
            _logger.LogInformation("获取监控场景卡片配置: {Scenario}", scenario);

            var cards = scenario.ToLower() switch
            {
                "exam" => await CreateExamMonitorCards(),
                "system" => await CreateSystemStatusCards(),
                "resource" => await CreateResourceMonitorCards(),
                "security" => await CreateSecurityMonitorCards(),
                _ => throw new ArgumentException($"不支持的监控场景: {scenario}")
            };

            var generatedCards = new List<Dictionary<string, object>>();
            foreach (var card in cards)
            {
                var generatedCard = _cardsGenerator.GenerateCard(card);
                generatedCards.Add(generatedCard);
            }

            _logger.LogInformation("成功生成 {Count} 个监控卡片", generatedCards.Count);
            return ApiResponse<List<Dictionary<string, object>>>.Success(generatedCards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取监控卡片配置失败: {Scenario}", scenario);
            return ApiResponse<List<Dictionary<string, object>>>.Error(500, $"获取监控数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建考试监控卡片
    /// </summary>
    private async Task<List<UdlCardConfig>> CreateExamMonitorCards()
    {
        var random = new Random();
        
        // 生成随机统计数据
        var totalParticipants = random.Next(120, 200);
        var onlineCount = random.Next((int)(totalParticipants * 0.8), totalParticipants);
        var suspiciousCount = random.Next(0, 15);
        var studentsTableCount = Math.Min(onlineCount, 20); // 表格显示数量
        
        // 生成模拟考生数据
        var studentsData = GenerateMockStudentsData(studentsTableCount);

        return new List<UdlCardConfig>
        {
            // 考试基本信息网格
            new InfoGridCardConfig
            {
                Id = "exam-info-grid",
                Title = "考试基本信息",
                Description = "实时更新的考试概览数据",
                Theme = "info",
                Grid = new InfoGridConfig
                {
                    Columns = 4,
                    Gap = "1.25rem",
                    Responsive = true,
                    ResponsiveColumns = new Dictionary<string, int>
                    {
                        ["xs"] = 2,
                        ["sm"] = 3,
                        ["md"] = 4
                    }
                },
                Items = new List<InfoGridItem>
                {
                    new()
                    {
                        Title = "考试编号",
                        Value = "EX001",
                        Icon = new InfoGridIconConfig { Name = "fa-id-card", Color = "#9b59b6" }
                    },
                    new()
                    {
                        Title = "学校机构",
                        Value = "CodeSpirit 教育",
                        Icon = new InfoGridIconConfig { Name = "fa-university", Color = "#f39c12" }
                    },
                    new()
                    {
                        Title = "考试状态",
                        Value = "进行中",
                        Icon = new InfoGridIconConfig { Name = "fa-play-circle", Color = "#27ae60" }
                    },
                    new()
                    {
                        Title = "在线情况",
                        Value = $"{onlineCount}/{totalParticipants}",
                        Icon = new InfoGridIconConfig { Name = "fa-users", Color = "#e67e22" },
                        Highlight = true
                    }
                }
            },

            // 参考人数统计
            new StatCardConfig
            {
                Id = "total-participants",
                Title = "参考人数",
                Description = "考试总体参与统计",
                Theme = "info",
                Data = new StatDataConfig
                {
                    Value = totalParticipants,
                    Label = "总人数",
                    Unit = "人",
                    Formatter = "integer"
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-users",
                    Color = "#17a2b8",
                    Size = "lg",
                    Position = "left"
                }
            },

            // 在线人数统计
            new StatCardConfig
            {
                Id = "online-count",
                Title = "在线人数",
                Description = "实时在线统计",
                Theme = "success",
                Data = new StatDataConfig
                {
                    Value = onlineCount,
                    Label = "在线",
                    Unit = "人",
                    Formatter = "integer",
                    ShowSeparator = true
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-wifi",
                    Color = "#28a745",
                    Size = "lg",
                    Position = "left"
                }
            },

            // 风险预警统计
            new StatCardConfig
            {
                Id = "suspicious-count",
                Title = "风险预警",
                Description = "异常行为检测",
                Theme = "danger",
                Data = new StatDataConfig
                {
                    Value = suspiciousCount,
                    Label = "风险用户",
                    Unit = "人",
                    Formatter = "integer"
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-exclamation-triangle",
                    Color = "#dc3545",
                    Size = "lg",
                    Position = "left"
                }
            },

            // 考生监控表格
            new TableCardConfig
            {
                Id = "students-table",
                Title = "考生监控",
                Description = "实时监控考生状态和答题进度",
                Theme = "primary",
                Table = new TableConfig
                {
                    Columns = new List<TableColumn>
                    {
                        new()
                        {
                            Name = "name",
                            Label = "姓名",
                            Type = "text",
                            Width = "100px"
                        },
                        new()
                        {
                            Name = "studentNumber",
                            Label = "学号",
                            Type = "text",
                            Width = "120px"
                        },
                        new()
                        {
                            Name = "ipAddress",
                            Label = "IP地址",
                            Type = "text",
                            Width = "130px"
                        },
                        new()
                        {
                            Name = "status",
                            Label = "状态",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["考试中"] = "<span class=\"label label-info\">考试中</span>",
                                ["已提交"] = "<span class=\"label label-success\">已提交</span>",
                                ["未开始"] = "<span class=\"label label-warning\">未开始</span>"
                            }
                        },
                        new()
                        {
                            Name = "progress",
                            Label = "进度",
                            Type = "progress",
                            Width = "120px",
                            Template = "${progress}%"
                        },
                        new()
                        {
                            Name = "screenSwitchCount",
                            Label = "切屏次数",
                            Type = "text",
                            Width = "100px",
                            Template = "<span class=\"${screenSwitchCount > 5 ? 'text-danger' : screenSwitchCount > 2 ? 'text-warning' : 'text-success'}\">${screenSwitchCount}</span>"
                        },
                        new()
                        {
                            Name = "startTime",
                            Label = "开始时间",
                            Type = "date",
                            Width = "160px",
                            Format = new TableColumnFormat
                            {
                                DateFormat = "MM-dd HH:mm"
                            }
                        }
                    },
                    ShowIndex = false,
                    ShowSelection = false,
                    ShowBorder = true,
                    ShowStripe = true,
                    ShowHover = true,
                    Pagination = new TablePaginationConfig
                    {
                        Enabled = true,
                        Position = "bottom",
                        PageSizeOptions = new List<int> { 10, 15, 20, 50 },
                        ShowQuickJumper = true,
                        ShowTotal = true
                    }
                },
                Data = new TableDataConfig
                {
                    StaticData = studentsData,
                    PageSize = 15,
                    DefaultSort = "startTime",
                    DefaultSortOrder = "desc"
                }
            },

            // 答题进度趋势图
            new ChartCardConfig
            {
                Id = "exam-progress-trend",
                Title = "答题进度趋势",
                Description = "考生答题进度时间分布",
                Theme = "info",
                Chart = new ChartConfig
                {
                    Type = "line",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["grid"] = new { left = "10%", right = "10%", bottom = "15%", top = "15%" },
                        ["xAxis"] = new 
                        { 
                            type = "category", 
                            name = "时间",
                            axisLabel = new { rotate = 45 }
                        },
                        ["yAxis"] = new { type = "value", name = "进度(%)" },
                        ["tooltip"] = new { trigger = "axis" },
                        ["legend"] = new { top = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateProgressTrendData()
                }
            },

            // 考生状态分析饼图
            new ChartCardConfig
            {
                Id = "exam-status-pie",
                Title = "考生状态分析",
                Description = "当前考生状态分布",
                Theme = "success",
                Chart = new ChartConfig
                {
                    Type = "pie",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["tooltip"] = new 
                        { 
                            trigger = "item",
                            formatter = "{a} <br/>{b}: {c} ({d}%)"
                        },
                        ["legend"] = new { bottom = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateExamStatusData(onlineCount, totalParticipants - onlineCount, suspiciousCount)
                }
            }
        };
    }

    /// <summary>
    /// 生成答题进度趋势数据
    /// </summary>
    /// <returns>进度趋势数据</returns>
    private static List<Dictionary<string, object>> GenerateProgressTrendData()
    {
        var random = new Random();
        var data = new List<Dictionary<string, object>>();
        var baseTime = DateTime.Now.AddHours(-2);

        for (int i = 0; i <= 24; i++) // 每5分钟一个数据点，2小时
        {
            var time = baseTime.AddMinutes(i * 5);
            var avgProgress = Math.Min(100, (double)i * 4 + random.Next(-5, 8));
            var topProgress = Math.Min(100, avgProgress + random.Next(10, 25));
            var slowProgress = Math.Max(0, avgProgress - random.Next(8, 20));

            data.Add(new Dictionary<string, object>
            {
                ["time"] = time.ToString("HH:mm"),
                ["平均进度"] = Math.Round(avgProgress, 1),
                ["最快进度"] = Math.Round(topProgress, 1),
                ["最慢进度"] = Math.Round(slowProgress, 1)
            });
        }

        return data;
    }

    /// <summary>
    /// 生成考生状态分析数据
    /// </summary>
    /// <param name="onlineCount">在线人数</param>
    /// <param name="offlineCount">离线人数</param>
    /// <param name="suspiciousCount">可疑人数</param>
    /// <returns>状态分析数据</returns>
    private static List<Dictionary<string, object>> GenerateExamStatusData(int onlineCount, int offlineCount, int suspiciousCount)
    {
        var normalCount = onlineCount - suspiciousCount;
        
        return new List<Dictionary<string, object>>
        {
            new() { ["name"] = "正常考试", ["value"] = normalCount },
            new() { ["name"] = "风险预警", ["value"] = suspiciousCount },
            new() { ["name"] = "离线状态", ["value"] = offlineCount }
        };
    }

    /// <summary>
    /// 生成模拟考生数据
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>考生数据列表</returns>
    private static List<Dictionary<string, object>> GenerateMockStudentsData(int count)
    {
        var students = new List<Dictionary<string, object>>();
        var names = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十", "郑十一", "王十二" };
        var statuses = new[] { "考试中", "已提交", "未开始" };
        var random = new Random();

        for (int i = 1; i <= count; i++)
        {
            var randomName = names[random.Next(names.Length)];
            var studentNumber = $"STU{i:D4}";
            var status = statuses[random.Next(statuses.Length)];
            var progress = random.Next(0, 101);
            var switchCount = random.Next(0, 10);
            var startTime = DateTime.Now.AddMinutes(-random.Next(1, 120));

            students.Add(new Dictionary<string, object>
            {
                ["id"] = i,
                ["name"] = $"{randomName}{i}",
                ["studentNumber"] = studentNumber,
                ["ipAddress"] = $"192.168.1.{100 + (i % 155)}",
                ["status"] = status,
                ["progress"] = progress,
                ["screenSwitchCount"] = switchCount,
                ["startTime"] = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["deviceInfo"] = $"Windows 10 Chrome {90 + random.Next(10)}"
            });
        }

        return students;
    }

    /// <summary>
    /// 创建系统状态监控卡片
    /// </summary>
    private async Task<List<UdlCardConfig>> CreateSystemStatusCards()
    {
        var random = new Random();
        
        // 生成随机统计数据
        var activeSessions = random.Next(800, 2000);
        
        // 生成系统服务数据
        var systemServices = GenerateSystemServicesData();

        return new List<UdlCardConfig>
        {
            // 系统状态信息网格
            new InfoGridCardConfig
            {
                Id = "system-status-grid",
                Title = "系统运行状态",
                Description = "服务器健康状况实时监控",
                Theme = "success",
                Grid = new InfoGridConfig
                {
                    Columns = 4,
                    Gap = "1.5rem",
                    Responsive = true
                },
                Items = new List<InfoGridItem>
                {
                    new()
                    {
                        Title = "系统版本",
                        Value = "CodeSpirit v2.0",
                        Icon = new InfoGridIconConfig { Name = "fa-code-branch", Color = "#3498db" }
                    },
                    new()
                    {
                        Title = "运行环境",
                        Value = ".NET 9.0",
                        Icon = new InfoGridIconConfig { Name = "fa-server", Color = "#9b59b6" }
                    },
                    new()
                    {
                        Title = "运行时长",
                        Value = "15天 8小时",
                        Icon = new InfoGridIconConfig { Name = "fa-clock", Color = "#f39c12" }
                    },
                    new()
                    {
                        Title = "系统状态",
                        Value = "正常运行",
                        Icon = new InfoGridIconConfig { Name = "fa-check-circle", Color = "#27ae60" }
                    }
                }
            },

            // 活跃会话统计
            new StatCardConfig
            {
                Id = "active-sessions",
                Title = "活跃会话",
                Description = "当前在线用户数",
                Theme = "primary",
                Data = new StatDataConfig
                {
                    Value = activeSessions,
                    Label = "在线用户",
                    Unit = "个",
                    Formatter = "integer"
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-wifi",
                    Color = "#007bff",
                    Size = "lg"
                }
            },

            // 系统服务监控表格
            new TableCardConfig
            {
                Id = "services-table",
                Title = "系统服务监控",
                Description = "微服务架构各组件运行状态",
                Theme = "success",
                Table = new TableConfig
                {
                    Columns = new List<TableColumn>
                    {
                        new()
                        {
                            Name = "name",
                            Label = "服务名称",
                            Type = "text",
                            Width = "120px"
                        },
                        new()
                        {
                            Name = "status",
                            Label = "状态",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["运行中"] = "<span class=\"label label-success\">运行中</span>",
                                ["停止"] = "<span class=\"label label-danger\">停止</span>",
                                ["维护中"] = "<span class=\"label label-warning\">维护中</span>"
                            }
                        },
                        new()
                        {
                            Name = "port",
                            Label = "端口",
                            Type = "text",
                            Width = "80px"
                        },
                        new()
                        {
                            Name = "cpu",
                            Label = "CPU使用率",
                            Type = "text",
                            Width = "100px",
                            Template = "<span class=\"${cpu.replace('%', '') > 80 ? 'text-danger' : cpu.replace('%', '') > 50 ? 'text-warning' : 'text-success'}\">${cpu}</span>"
                        },
                        new()
                        {
                            Name = "memory",
                            Label = "内存使用",
                            Type = "text",
                            Width = "100px"
                        },
                        new()
                        {
                            Name = "uptime",
                            Label = "运行时长",
                            Type = "text",
                            Width = "100px"
                        }
                    },
                    ShowIndex = false,
                    ShowSelection = false,
                    ShowBorder = true,
                    ShowStripe = true,
                    ShowHover = true,
                    Pagination = new TablePaginationConfig
                    {
                        Enabled = false
                    }
                },
                Data = new TableDataConfig
                {
                    StaticData = systemServices,
                    PageSize = 20
                }
            },

            // 系统性能监控柱状图
            new ChartCardConfig
            {
                Id = "system-performance-bar",
                Title = "系统性能监控",
                Description = "各服务CPU和内存使用情况",
                Theme = "primary",
                Chart = new ChartConfig
                {
                    Type = "bar",
                    Height = 400,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["grid"] = new { left = "3%", right = "4%", bottom = "3%", containLabel = true },
                        ["xAxis"] = new { type = "category", name = "服务" },
                        ["yAxis"] = new { type = "value", name = "使用率(%)" },
                        ["tooltip"] = new { trigger = "axis" },
                        ["legend"] = new { top = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateSystemPerformanceData()
                }
            },

            // 服务响应时间趋势图
            new ChartCardConfig
            {
                Id = "response-time-trend",
                Title = "服务响应时间",
                Description = "系统服务响应时间趋势监控",
                Theme = "success",
                Chart = new ChartConfig
                {
                    Type = "line",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["grid"] = new { left = "10%", right = "10%", bottom = "15%", top = "15%" },
                        ["xAxis"] = new { type = "category", name = "时间" },
                        ["yAxis"] = new { type = "value", name = "响应时间(ms)" },
                        ["tooltip"] = new { trigger = "axis" },
                        ["legend"] = new { top = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateResponseTimeData()
                }
            }
        };
    }

    /// <summary>
    /// 生成系统服务数据
    /// </summary>
    /// <returns>系统服务数据列表</returns>
    private static List<Dictionary<string, object>> GenerateSystemServicesData()
    {
        var random = new Random();
        var services = new[]
        {
            new { Name = "IdentityApi", Port = 8001, BaseMemory = 256 },
            new { Name = "ExamApi", Port = 8002, BaseMemory = 512 },
            new { Name = "ConfigCenter", Port = 8003, BaseMemory = 128 },
            new { Name = "MessagingApi", Port = 8004, BaseMemory = 384 },
            new { Name = "Web", Port = 8000, BaseMemory = 768 },
            new { Name = "Database", Port = 5432, BaseMemory = 1200 },
            new { Name = "Redis", Port = 6379, BaseMemory = 64 },
            new { Name = "RabbitMQ", Port = 5672, BaseMemory = 256 }
        };

        return services.Select(service => new Dictionary<string, object>
        {
            ["name"] = service.Name,
            ["status"] = "运行中",
            ["port"] = service.Port,
            ["cpu"] = $"{random.Next(5, 35)}%",
            ["memory"] = service.BaseMemory > 1000 ? 
                $"{Math.Round(random.NextDouble() * 0.5 + service.BaseMemory / 1000.0, 1)}GB" : 
                $"{random.Next((int)(service.BaseMemory * 0.8), (int)(service.BaseMemory * 1.2))}MB",
            ["uptime"] = $"{random.Next(10, 35)}天"
        }).ToList();
    }

    /// <summary>
    /// 生成系统性能数据
    /// </summary>
    /// <returns>系统性能数据</returns>
    private static List<Dictionary<string, object>> GenerateSystemPerformanceData()
    {
        var random = new Random();
        var services = new[] { "IdentityApi", "ExamApi", "ConfigCenter", "MessagingApi", "Web", "Database" };
        
        return services.Select(service => new Dictionary<string, object>
        {
            ["service"] = service,
            ["CPU使用率"] = random.Next(5, 35),
            ["内存使用率"] = random.Next(30, 70)
        }).ToList();
    }

    /// <summary>
    /// 生成响应时间趋势数据
    /// </summary>
    /// <returns>响应时间趋势数据</returns>
    private static List<Dictionary<string, object>> GenerateResponseTimeData()
    {
        var random = new Random();
        var data = new List<Dictionary<string, object>>();
        var baseTime = DateTime.Now.AddMinutes(-30);

        for (int i = 0; i <= 30; i++) // 每分钟一个数据点，30分钟
        {
            var time = baseTime.AddMinutes(i);
            
            data.Add(new Dictionary<string, object>
            {
                ["time"] = time.ToString("HH:mm"),
                ["API响应"] = random.Next(20, 150),
                ["数据库查询"] = random.Next(50, 300),
                ["缓存访问"] = random.Next(5, 25)
            });
        }

        return data;
    }

    /// <summary>
    /// 创建资源监控卡片
    /// </summary>
    private async Task<List<UdlCardConfig>> CreateResourceMonitorCards()
    {
        var random = new Random();
        
        // 生成随机统计数据
        var cpuUsage = Math.Round(random.NextDouble() * 40 + 40, 1); // 40-80%
        
        // 生成资源详情数据
        var resourceDetails = GenerateResourceDetailsData();

        return new List<UdlCardConfig>
        {
            // CPU 使用率统计
            new StatCardConfig
            {
                Id = "cpu-usage",
                Title = "CPU 使用率",
                Description = "处理器负载监控",
                Theme = "warning",
                Data = new StatDataConfig
                {
                    Value = (decimal)cpuUsage,
                    Label = "CPU",
                    Unit = "%",
                    Formatter = "number",
                    DecimalPlaces = 1
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-microchip",
                    Color = "#e67e22",
                    Size = "lg"
                }
            },

            // 详细资源监控表格
            new TableCardConfig
            {
                Id = "resource-details-table",
                Title = "详细资源监控",
                Description = "系统各组件资源使用详情",
                Theme = "warning",
                Table = new TableConfig
                {
                    Columns = new List<TableColumn>
                    {
                        new()
                        {
                            Name = "resource",
                            Label = "资源组件",
                            Type = "text",
                            Width = "120px"
                        },
                        new()
                        {
                            Name = "usage",
                            Label = "使用率",
                            Type = "text",
                            Width = "100px",
                            Template = "<span class=\"${usage.replace('%', '') > 80 ? 'text-danger' : usage.replace('%', '') > 60 ? 'text-warning' : 'text-success'}\">${usage}</span>"
                        },
                        new()
                        {
                            Name = "status",
                            Label = "状态",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["正常"] = "<span class=\"label label-success\">正常</span>",
                                ["健康"] = "<span class=\"label label-info\">健康</span>",
                                ["警告"] = "<span class=\"label label-warning\">警告</span>",
                                ["异常"] = "<span class=\"label label-danger\">异常</span>"
                            }
                        },
                        new()
                        {
                            Name = "load",
                            Label = "负载等级",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["轻量"] = "<span class=\"label label-success\">轻量</span>",
                                ["中等"] = "<span class=\"label label-warning\">中等</span>",
                                ["重载"] = "<span class=\"label label-danger\">重载</span>"
                            }
                        },
                        new()
                        {
                            Name = "temperature",
                            Label = "温度",
                            Type = "text",
                            Width = "80px",
                            Template = "<span class=\"${temperature.replace('°C', '') > 70 ? 'text-danger' : temperature.replace('°C', '') > 50 ? 'text-warning' : 'text-success'}\">${temperature}</span>"
                        }
                    },
                    ShowIndex = false,
                    ShowSelection = false,
                    ShowBorder = true,
                    ShowStripe = true,
                    ShowHover = true,
                    Pagination = new TablePaginationConfig
                    {
                        Enabled = true,
                        Position = "bottom",
                        PageSizeOptions = new List<int> { 8, 15, 20 },
                        ShowQuickJumper = false,
                        ShowTotal = true
                    }
                },
                Data = new TableDataConfig
                {
                    StaticData = resourceDetails,
                    PageSize = 8
                }
            },

            // 内存使用率趋势图
            new ChartCardConfig
            {
                Id = "memory-usage-trend",
                Title = "内存使用趋势",
                Description = "系统内存使用率变化趋势",
                Theme = "warning",
                Chart = new ChartConfig
                {
                    Type = "area",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["grid"] = new { left = "10%", right = "10%", bottom = "15%", top = "15%" },
                        ["xAxis"] = new { type = "category", name = "时间" },
                        ["yAxis"] = new { type = "value", name = "内存使用率(%)", max = 100 },
                        ["tooltip"] = new { trigger = "axis" },
                        ["legend"] = new { top = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateMemoryUsageData()
                }
            },

            // 磁盘I/O性能雷达图
            new ChartCardConfig
            {
                Id = "disk-io-radar",
                Title = "磁盘I/O性能",
                Description = "各磁盘读写性能对比",
                Theme = "danger",
                Chart = new ChartConfig
                {
                    Type = "radar",
                    Height = 400,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["tooltip"] = new { trigger = "item" },
                        ["legend"] = new { bottom = "5%" },
                        ["radar"] = new
                        {
                            indicator = new[]
                            {
                                new { name = "读取速度", max = 100 },
                                new { name = "写入速度", max = 100 },
                                new { name = "队列深度", max = 100 },
                                new { name = "响应时间", max = 100 },
                                new { name = "吞吐量", max = 100 },
                                new { name = "IOPS", max = 100 }
                            }
                        }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateDiskIOData()
                }
            }
        };
    }

    /// <summary>
    /// 生成资源详情数据
    /// </summary>
    /// <returns>资源详情数据列表</returns>
    private static List<Dictionary<string, object>> GenerateResourceDetailsData()
    {
        var random = new Random();
        var resources = new[]
        {
            new { Name = "CPU Core 1", Status = "正常", TempRange = new { Min = 55, Max = 70 } },
            new { Name = "CPU Core 2", Status = "正常", TempRange = new { Min = 58, Max = 72 } },
            new { Name = "CPU Core 3", Status = "正常", TempRange = new { Min = 52, Max = 68 } },
            new { Name = "CPU Core 4", Status = "正常", TempRange = new { Min = 56, Max = 71 } },
            new { Name = "内存 Bank 1", Status = "正常", TempRange = new { Min = 40, Max = 50 } },
            new { Name = "内存 Bank 2", Status = "正常", TempRange = new { Min = 42, Max = 52 } },
            new { Name = "系统盘 SSD", Status = "健康", TempRange = new { Min = 35, Max = 45 } },
            new { Name = "数据盘 HDD", Status = "健康", TempRange = new { Min = 30, Max = 42 } },
            new { Name = "网卡 eth0", Status = "正常", TempRange = new { Min = 30, Max = 40 } },
            new { Name = "GPU 显存", Status = "正常", TempRange = new { Min = 45, Max = 65 } }
        };

        return resources.Select(resource =>
        {
            var usage = random.Next(25, 85);
            var temperature = random.Next(resource.TempRange.Min, resource.TempRange.Max);
            var load = usage switch
            {
                < 40 => "轻量",
                < 70 => "中等",
                _ => "重载"
            };

            return new Dictionary<string, object>
            {
                ["resource"] = resource.Name,
                ["usage"] = $"{usage}%",
                ["status"] = resource.Status,
                ["load"] = load,
                ["temperature"] = $"{temperature}°C"
            };
        }).ToList();
    }

    /// <summary>
    /// 生成内存使用趋势数据
    /// </summary>
    /// <returns>内存使用趋势数据</returns>
    private static List<Dictionary<string, object>> GenerateMemoryUsageData()
    {
        var random = new Random();
        var data = new List<Dictionary<string, object>>();
        var baseTime = DateTime.Now.AddHours(-1);

        for (int i = 0; i <= 60; i++) // 每分钟一个数据点，1小时
        {
            var time = baseTime.AddMinutes(i);
            
            data.Add(new Dictionary<string, object>
            {
                ["time"] = time.ToString("HH:mm"),
                ["系统内存"] = random.Next(45, 75),
                ["缓存内存"] = random.Next(15, 35),
                ["应用内存"] = random.Next(25, 50)
            });
        }

        return data;
    }

    /// <summary>
    /// 生成磁盘I/O性能数据
    /// </summary>
    /// <returns>磁盘I/O性能数据</returns>
    private static List<Dictionary<string, object>> GenerateDiskIOData()
    {
        var random = new Random();
        
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["name"] = "系统盘 SSD",
                ["value"] = new[]
                {
                    random.Next(70, 95), // 读取速度
                    random.Next(65, 90), // 写入速度  
                    random.Next(20, 40), // 队列深度
                    random.Next(80, 95), // 响应时间（越高越好，这里表示性能好）
                    random.Next(75, 90), // 吞吐量
                    random.Next(80, 95)  // IOPS
                }
            },
            new()
            {
                ["name"] = "数据盘 HDD",
                ["value"] = new[]
                {
                    random.Next(40, 65), // 读取速度
                    random.Next(35, 60), // 写入速度
                    random.Next(30, 60), // 队列深度
                    random.Next(50, 75), // 响应时间
                    random.Next(45, 70), // 吞吐量
                    random.Next(40, 65)  // IOPS
                }
            }
        };
    }

    /// <summary>
    /// 创建安全监控卡片
    /// </summary>
    private async Task<List<UdlCardConfig>> CreateSecurityMonitorCards()
    {
        var random = new Random();
        
        // 生成随机统计数据
        var securityEventsCount = random.Next(20, 80);
        
        // 生成安全事件数据
        var securityEvents = GenerateSecurityEventsData();

        return new List<UdlCardConfig>
        {
            // 安全事件统计
            new StatCardConfig
            {
                Id = "security-events",
                Title = "安全事件",
                Description = "实时安全威胁统计",
                Theme = "danger",
                Data = new StatDataConfig
                {
                    Value = securityEventsCount,
                    Label = "攻击拦截",
                    Unit = "次/小时",
                    Formatter = "integer"
                },
                Icon = new StatIconConfig
                {
                    Name = "fa-shield-alt",
                    Color = "#e74c3c",
                    Size = "lg"
                }
            },

            // 安全事件监控表格
            new TableCardConfig
            {
                Id = "security-events-table",
                Title = "安全事件监控",
                Description = "实时安全威胁和攻击事件记录",
                Theme = "danger",
                Table = new TableConfig
                {
                    Columns = new List<TableColumn>
                    {
                        new()
                        {
                            Name = "time",
                            Label = "发生时间",
                            Type = "text",
                            Width = "140px"
                        },
                        new()
                        {
                            Name = "type",
                            Label = "事件类型",
                            Type = "text",
                            Width = "100px"
                        },
                        new()
                        {
                            Name = "severity",
                            Label = "严重等级",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["高"] = "<span class=\"label label-danger\">高</span>",
                                ["中"] = "<span class=\"label label-warning\">中</span>",
                                ["低"] = "<span class=\"label label-info\">低</span>"
                            }
                        },
                        new()
                        {
                            Name = "source",
                            Label = "来源IP",
                            Type = "text",
                            Width = "120px"
                        },
                        new()
                        {
                            Name = "description",
                            Label = "事件描述",
                            Type = "text",
                            Width = "150px"
                        },
                        new()
                        {
                            Name = "status",
                            Label = "处理状态",
                            Type = "mapping",
                            Width = "100px",
                            Mapping = new Dictionary<string, object>
                            {
                                ["已处理"] = "<span class=\"label label-success\">已处理</span>",
                                ["已拦截"] = "<span class=\"label label-info\">已拦截</span>",
                                ["已扫描"] = "<span class=\"label label-secondary\">已扫描</span>",
                                ["已记录"] = "<span class=\"label label-light\">已记录</span>",
                                ["待处理"] = "<span class=\"label label-warning\">待处理</span>"
                            }
                        }
                    },
                    ShowIndex = false,
                    ShowSelection = false,
                    ShowBorder = true,
                    ShowStripe = true,
                    ShowHover = true,
                    Pagination = new TablePaginationConfig
                    {
                        Enabled = true,
                        Position = "bottom",
                        PageSizeOptions = new List<int> { 8, 15, 20 },
                        ShowQuickJumper = false,
                        ShowTotal = true
                    }
                },
                Data = new TableDataConfig
                {
                    StaticData = securityEvents,
                    PageSize = 8,
                    DefaultSort = "time",
                    DefaultSortOrder = "desc"
                }
            },

            // 安全事件趋势图
            new ChartCardConfig
            {
                Id = "security-trend-line",
                Title = "安全事件趋势",
                Description = "最近24小时安全事件变化趋势",
                Theme = "danger",
                Chart = new ChartConfig
                {
                    Type = "line",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["grid"] = new { left = "10%", right = "10%", bottom = "15%", top = "15%" },
                        ["xAxis"] = new { type = "category", name = "时间" },
                        ["yAxis"] = new { type = "value", name = "事件数量" },
                        ["tooltip"] = new { trigger = "axis" },
                        ["legend"] = new { top = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateSecurityTrendData()
                }
            },

            // 攻击类型分布饼图
            new ChartCardConfig
            {
                Id = "attack-type-pie",
                Title = "攻击类型分布",
                Description = "各类型安全威胁占比分析",
                Theme = "warning",
                Chart = new ChartConfig
                {
                    Type = "pie",
                    Height = 350,
                    Theme = "default",
                    Responsive = true,
                    Options = new Dictionary<string, object>
                    {
                        ["tooltip"] = new 
                        { 
                            trigger = "item",
                            formatter = "{a} <br/>{b}: {c} ({d}%)"
                        },
                        ["legend"] = new { bottom = "5%" }
                    }
                },
                Data = new ChartDataConfig
                {
                    StaticData = GenerateAttackTypeData()
                }
            }
        };
    }

    /// <summary>
    /// 生成安全事件趋势数据
    /// </summary>
    /// <returns>安全事件趋势数据</returns>
    private static List<Dictionary<string, object>> GenerateSecurityTrendData()
    {
        var random = new Random();
        var data = new List<Dictionary<string, object>>();
        var baseTime = DateTime.Now.AddHours(-24);

        for (int i = 0; i <= 24; i++) // 每小时一个数据点，24小时
        {
            var time = baseTime.AddHours(i);
            
            data.Add(new Dictionary<string, object>
            {
                ["time"] = time.ToString("HH:mm"),
                ["登录异常"] = random.Next(0, 8),
                ["SQL注入"] = random.Next(0, 5),
                ["XSS攻击"] = random.Next(0, 6),
                ["暴力破解"] = random.Next(0, 10),
                ["DDoS攻击"] = random.Next(0, 3)
            });
        }

        return data;
    }

    /// <summary>
    /// 生成攻击类型分布数据
    /// </summary>
    /// <returns>攻击类型分布数据</returns>
    private static List<Dictionary<string, object>> GenerateAttackTypeData()
    {
        var random = new Random();
        
        return new List<Dictionary<string, object>>
        {
            new() { ["name"] = "暴力破解", ["value"] = random.Next(15, 30) },
            new() { ["name"] = "SQL注入", ["value"] = random.Next(8, 20) },
            new() { ["name"] = "XSS攻击", ["value"] = random.Next(6, 18) },
            new() { ["name"] = "登录异常", ["value"] = random.Next(10, 25) },
            new() { ["name"] = "DDoS攻击", ["value"] = random.Next(3, 12) },
            new() { ["name"] = "文件上传", ["value"] = random.Next(2, 8) },
            new() { ["name"] = "端口扫描", ["value"] = random.Next(5, 15) },
            new() { ["name"] = "权限提升", ["value"] = random.Next(1, 6) }
        };
    }

    /// <summary>
    /// 生成安全事件数据
    /// </summary>
    /// <returns>安全事件数据列表</returns>
    private static List<Dictionary<string, object>> GenerateSecurityEventsData()
    {
        var baseTime = DateTime.Now;
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["time"] = baseTime.AddMinutes(-5).ToString("MM-dd HH:mm:ss"),
                ["type"] = "登录异常",
                ["severity"] = "高",
                ["source"] = "192.168.1.100",
                ["description"] = "异地登录尝试",
                ["status"] = "已处理"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-10).ToString("MM-dd HH:mm:ss"),
                ["type"] = "SQL注入",
                ["severity"] = "高",
                ["source"] = "203.45.67.89",
                ["description"] = "尝试SQL注入攻击",
                ["status"] = "已拦截"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-15).ToString("MM-dd HH:mm:ss"),
                ["type"] = "XSS攻击",
                ["severity"] = "中",
                ["source"] = "124.56.78.90",
                ["description"] = "跨站脚本攻击尝试",
                ["status"] = "已拦截"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-20).ToString("MM-dd HH:mm:ss"),
                ["type"] = "暴力破解",
                ["severity"] = "中",
                ["source"] = "180.23.45.67",
                ["description"] = "密码暴力破解",
                ["status"] = "已拦截"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-25).ToString("MM-dd HH:mm:ss"),
                ["type"] = "文件上传",
                ["severity"] = "低",
                ["source"] = "192.168.1.88",
                ["description"] = "可疑文件上传",
                ["status"] = "已扫描"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-30).ToString("MM-dd HH:mm:ss"),
                ["type"] = "DDoS攻击",
                ["severity"] = "高",
                ["source"] = "多个IP",
                ["description"] = "分布式拒绝服务攻击",
                ["status"] = "已拦截"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-35).ToString("MM-dd HH:mm:ss"),
                ["type"] = "权限提升",
                ["severity"] = "高",
                ["source"] = "内部网络",
                ["description"] = "异常权限提升尝试",
                ["status"] = "待处理"
            },
            new()
            {
                ["time"] = baseTime.AddMinutes(-40).ToString("MM-dd HH:mm:ss"),
                ["type"] = "端口扫描",
                ["severity"] = "中",
                ["source"] = "89.12.34.56",
                ["description"] = "系统端口扫描",
                ["status"] = "已记录"
            }
        };
    }
} 