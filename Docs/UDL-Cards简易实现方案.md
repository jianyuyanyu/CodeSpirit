# UDL Cards 简易实现方案（基于CodeSpirit.Amis扩展）

## 概述

UDL Cards简易实现方案是对CodeSpirit.Amis智能界面生成引擎的轻量级扩展，通过最小化改动现有架构，实现基于API服务的卡片自动生成功能。该方案专注于复用现有组件和特性系统，确保开发者零学习成本。

## 设计原则

### 1. 复用现有架构
- 扩展AmisGenerator，添加卡片生成方法
- 复用现有的Helper类（ColumnHelper、ButtonHelper等）
- 扩展现有特性定义，而非重新创建
- 保持与CRUD表格生成的一致性

### 2. API驱动生成
- 基于Controller和DTO自动生成卡片
- 复用现有的反射机制和特性解析
- 支持多种卡片类型

### 3. 简化使用
- 通过特性标记指定卡片类型
- 最少配置，开发者友好

## 实现方案

### 1. 扩展AmisGenerator - 添加卡片生成功能

```csharp
// 在现有的AmisGenerator类中添加卡片生成方法
public partial class AmisGenerator
{
    /// <summary>
    /// 为指定的控制器生成卡片配置
    /// </summary>
    public JObject GenerateCardConfig(Endpoint endpoint, CardType cardType = CardType.Auto)
    {
        Type controllerType = GetAndValidateControllerType(endpoint);
        if (controllerType == null) return null;

        string cacheKey = _cachingHelper.GenerateCacheKey($"{controllerType.FullName}_card_{cardType}");
        
        // 尝试从缓存获取
        if (_cachingHelper.TryGetValue(cacheKey, out JObject cachedConfig))
        {
            return cachedConfig;
        }

        // 生成卡片配置
        var cardBuilder = _serviceProvider.GetRequiredService<AmisCardConfigBuilder>();
        var cardConfig = cardBuilder.GenerateCardConfig(cardType);

        // 缓存配置
        if (cardConfig != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
            _cachingHelper.Set(cacheKey, cardConfig, cacheOptions);
        }

        return cardConfig;
    }

    /// <summary>
    /// 为指定DTO类型生成卡片
    /// </summary>
    public JObject GenerateCardForDto<T>(CardLayoutType layoutType = CardLayoutType.Info)
    {
        var cardBuilder = new AmisCardConfigBuilder(_amisContext, _serviceProvider);
        return cardBuilder.GenerateDtoCard<T>(layoutType);
    }
}
```

### 2. 新增AmisCardConfigBuilder - 卡片配置构建器

```csharp
/// <summary>
/// AMIS卡片配置构建器 - 复用现有Helper架构
/// </summary>
public class AmisCardConfigBuilder
{
    private readonly AmisContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ColumnHelper _columnHelper;
    private readonly ButtonHelper _buttonHelper;
    private readonly ApiRouteHelper _apiRouteHelper;
    private readonly UtilityHelper _utilityHelper;

    public AmisCardConfigBuilder(AmisContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _columnHelper = new ColumnHelper(context, serviceProvider);
        _buttonHelper = new ButtonHelper(context, serviceProvider);
        _apiRouteHelper = serviceProvider.GetRequiredService<ApiRouteHelper>();
        _utilityHelper = new UtilityHelper();
    }

    /// <summary>
    /// 生成卡片配置
    /// </summary>
    public JObject GenerateCardConfig(CardType cardType = CardType.Auto)
    {
        if (cardType == CardType.Auto)
        {
            cardType = DetectCardType();
        }

        return cardType switch
        {
            CardType.Info => GenerateInfoCard(),
            CardType.Stat => GenerateStatCard(),
            CardType.List => GenerateListCard(),
            CardType.Action => GenerateActionCard(),
            _ => GenerateInfoCard()
        };
    }

    /// <summary>
    /// 为DTO生成卡片
    /// </summary>
    public JObject GenerateDtoCard<T>(CardLayoutType layoutType)
    {
        var dtoType = typeof(T);
        var properties = dtoType.GetProperties();
        
        var card = new JObject
        {
            ["type"] = "card",
            ["className"] = $"udl-card udl-{layoutType.ToString().ToLower()}-card"
        };

        // 生成标题
        var titleAttr = dtoType.GetCustomAttribute<CardTitleAttribute>();
        if (titleAttr != null)
        {
            card["header"] = new JObject
            {
                ["title"] = titleAttr.Title,
                ["subTitle"] = titleAttr.SubTitle
            };
        }

        // 生成内容
        var body = new JArray();
        
        if (layoutType == CardLayoutType.Grid)
        {
            body.Add(GenerateGridLayout(properties));
        }
        else if (layoutType == CardLayoutType.Stat)
        {
            body.Add(GenerateStatLayout(properties));
        }
        else
        {
            body.Add(GenerateFlexLayout(properties));
        }

        card["body"] = body;
        return card;
    }

    /// <summary>
    /// 生成信息展示卡片
    /// </summary>
    private JObject GenerateInfoCard()
    {
        var dataType = GetPrimaryDataType();
        if (dataType == null) return null;

        return GenerateDtoCard(dataType, CardLayoutType.Info);
    }

    /// <summary>
    /// 生成统计卡片
    /// </summary>
    private JObject GenerateStatCard()
    {
        var dataType = GetPrimaryDataType();
        if (dataType == null) return null;

        var properties = dataType.GetProperties();
        var statProperties = properties.Where(p => p.GetCustomAttribute<StatValueAttribute>() != null).ToList();

        if (!statProperties.Any())
        {
            // 如果没有明确的统计属性，尝试自动检测数值属性
            statProperties = properties.Where(p => IsNumericType(p.PropertyType)).ToList();
        }

        var cards = new JArray();
        
        foreach (var prop in statProperties)
        {
            var statAttr = prop.GetCustomAttribute<StatValueAttribute>();
            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            
            cards.Add(new JObject
            {
                ["type"] = "card",
                ["className"] = "stat-card",
                ["body"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "tpl",
                        ["className"] = "stat-content",
                        ["tpl"] = GenerateStatTemplate(prop, statAttr, displayAttr)
                    }
                }
            });
        }

        return new JObject
        {
            ["type"] = "grid",
            ["columns"] = cards
        };
    }

    /// <summary>
    /// 生成列表卡片
    /// </summary>
    private JObject GenerateListCard()
    {
        var listRoute = _apiRouteHelper.GetApiRoutes()?.Read;
        if (string.IsNullOrEmpty(listRoute)) return null;

        var itemType = GetListItemType();
        if (itemType == null) return null;

        return new JObject
        {
            ["type"] = "service",
            ["api"] = listRoute,
            ["body"] = new JArray
            {
                new JObject
                {
                    ["type"] = "each",
                    ["name"] = "items",
                    ["items"] = GenerateDtoCard(itemType, CardLayoutType.Compact)
                }
            }
        };
    }

    /// <summary>
    /// 生成网格布局
    /// </summary>
    private JObject GenerateGridLayout(PropertyInfo[] properties)
    {
        var layoutAttr = properties.FirstOrDefault()?.DeclaringType?.GetCustomAttribute<CardLayoutAttribute>();
        var columns = layoutAttr?.Columns ?? 2;

        var grid = new JObject
        {
            ["type"] = "grid",
            ["columns"] = new JArray()
        };

        var gridColumns = (JArray)grid["columns"];
        
        foreach (var prop in properties)
        {
            if (ShouldIncludeProperty(prop))
            {
                gridColumns.Add(GenerateFieldConfig(prop));
            }
        }

        return grid;
    }

    /// <summary>
    /// 生成字段配置 - 复用ColumnHelper的逻辑
    /// </summary>
    private JObject GenerateFieldConfig(PropertyInfo property)
    {
        // 复用现有的列配置逻辑
        var columnConfig = _columnHelper.CreateColumn(property);
        
        // 转换为卡片字段格式
        var cardFieldAttr = property.GetCustomAttribute<CardFieldAttribute>();
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        
        var label = displayAttr?.Name ?? columnConfig["label"]?.ToString() ?? property.Name;
        var icon = cardFieldAttr?.Icon ?? "";
        var featured = cardFieldAttr?.Featured ?? false;

        return new JObject
        {
            ["type"] = "tpl",
            ["className"] = $"card-field {(featured ? "featured" : "")}",
            ["tpl"] = $"<div class=\"field-item\">" +
                     $"{(string.IsNullOrEmpty(icon) ? "" : $"<i class=\"{icon}\"></i> ")}" +
                     $"<span class=\"field-label\">{label}：</span>" +
                     $"<span class=\"field-value\">${{{property.Name}}}</span>" +
                     $"</div>"
        };
    }

    /// <summary>
    /// 检测卡片类型
    /// </summary>
    private CardType DetectCardType()
    {
        var dataType = GetPrimaryDataType();
        if (dataType == null) return CardType.Info;

        // 检查是否有统计属性
        var hasStatProperties = dataType.GetProperties()
            .Any(p => p.GetCustomAttribute<StatValueAttribute>() != null || IsNumericType(p.PropertyType));
        
        if (hasStatProperties)
        {
            return CardType.Stat;
        }

        // 检查是否是列表数据
        if (_context.Actions?.List != null)
        {
            var returnType = _context.Actions.List.ReturnType;
            if (IsListType(returnType))
            {
                return CardType.List;
            }
        }

        return CardType.Info;
    }

    /// <summary>
    /// 获取主要数据类型
    /// </summary>
    private Type GetPrimaryDataType()
    {
        if (_context.Actions?.List != null)
        {
            return _utilityHelper.GetDataTypeFromMethod(_context.Actions.List);
        }
        return _context.ListDataType;
    }

    /// <summary>
    /// 检查属性是否应该包含在卡片中
    /// </summary>
    private bool ShouldIncludeProperty(PropertyInfo property)
    {
        // 复用现有的列过滤逻辑
        return property.GetCustomAttribute<IgnoreColumnAttribute>() == null &&
               !property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查是否为数值类型
    /// </summary>
    private bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(decimal) ||
               type == typeof(double) || type == typeof(float) || type == typeof(short) ||
               type == typeof(int?) || type == typeof(long?) || type == typeof(decimal?) ||
               type == typeof(double?) || type == typeof(float?) || type == typeof(short?);
    }

    /// <summary>
    /// 生成统计模板
    /// </summary>
    private string GenerateStatTemplate(PropertyInfo property, StatValueAttribute statAttr, DisplayAttribute displayAttr)
    {
        var label = statAttr?.Label ?? displayAttr?.Name ?? property.Name;
        var unit = statAttr?.Unit ?? "";
        var color = statAttr?.Color ?? "primary";

        return $@"
            <div class=""stat-item"">
                <div class=""stat-value text-{color}"">${{{property.Name}}}{unit}</div>
                <div class=""stat-label"">{label}</div>
            </div>";
    }
}
```

### 3. 扩展特性定义

```csharp
// 在 Attributes 目录下新增卡片相关特性

/// <summary>
/// 卡片标题特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CardTitleAttribute : Attribute
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Icon { get; set; }

    public CardTitleAttribute(string title, string subTitle = null, string icon = null)
    {
        Title = title;
        SubTitle = subTitle;
        Icon = icon;
    }
}

/// <summary>
/// 卡片布局特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CardLayoutAttribute : Attribute
{
    public CardLayoutType LayoutType { get; set; }
    public int Columns { get; set; } = 2;

    public CardLayoutAttribute(CardLayoutType layoutType = CardLayoutType.Flex, int columns = 2)
    {
        LayoutType = layoutType;
        Columns = columns;
    }
}

/// <summary>
/// 卡片字段特性 - 扩展现有的显示逻辑
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CardFieldAttribute : Attribute
{
    public string Icon { get; set; }
    public bool Featured { get; set; } // 是否为重点字段
    public int Order { get; set; }

    public CardFieldAttribute(string icon = null, bool featured = false, int order = 0)
    {
        Icon = icon;
        Featured = featured;
        Order = order;
    }
}

/// <summary>
/// 统计值特性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class StatValueAttribute : Attribute
{
    public string Label { get; set; }
    public string Unit { get; set; }
    public string Color { get; set; }

    public StatValueAttribute(string label, string unit = null, string color = null)
    {
        Label = label;
        Unit = unit;
        Color = color;
    }
}

/// <summary>
/// 卡片类型枚举
/// </summary>
public enum CardType
{
    Auto,     // 自动检测
    Info,     // 信息展示卡片
    Stat,     // 统计卡片
    List,     // 列表卡片
    Action    // 操作卡片
}

/// <summary>
/// 卡片布局类型
/// </summary>
public enum CardLayoutType
{
    Flex,     // 弹性布局
    Grid,     // 网格布局
    Info,     // 信息布局
    Stat,     // 统计布局
    Compact   // 紧凑布局
}
```

### 4. 扩展现有Controller - 添加卡片API

```csharp
// 扩展现有的AmisController或创建新的CardController
[ApiController]
[Route("api/amis")]
public partial class AmisController : ControllerBase
{
    /// <summary>
    /// 获取卡片配置
    /// </summary>
    [HttpGet("card/{controllerName}")]
    public async Task<ActionResult<JObject>> GetCardConfig(
        string controllerName, 
        [FromQuery] CardType cardType = CardType.Auto)
    {
        var endpoint = GetEndpoint(controllerName);
        var config = _amisGenerator.GenerateCardConfig(endpoint, cardType);
        
        if (config == null)
        {
            return NotFound($"Controller {controllerName} not found or not supported");
        }

        return Ok(config);
    }

    /// <summary>
    /// 获取DTO卡片配置
    /// </summary>
    [HttpGet("card/dto/{dtoTypeName}")]
    public ActionResult<JObject> GetDtoCard(
        string dtoTypeName, 
        [FromQuery] CardLayoutType layout = CardLayoutType.Info)
    {
        var dtoType = GetDtoType(dtoTypeName);
        if (dtoType == null)
        {
            return NotFound($"DTO type {dtoTypeName} not found");
        }

        var cardBuilder = new AmisCardConfigBuilder(_amisContext, _serviceProvider);
        var config = cardBuilder.GenerateDtoCard(dtoType, layout);
        
        return Ok(config);
    }

    /// <summary>
    /// 获取端点信息
    /// </summary>
    private Endpoint GetEndpoint(string controllerName)
    {
        // 实现获取端点逻辑，复用现有方法
        // 这里可以复用现有的控制器查找逻辑
        return null; // 待实现
    }

    /// <summary>
    /// 获取DTO类型
    /// </summary>
    private Type GetDtoType(string dtoTypeName)
    {
        // 实现DTO类型查找逻辑
        // 可以通过反射在当前程序集中查找
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name.Equals(dtoTypeName, StringComparison.OrdinalIgnoreCase));
    }
}
```

### 5. 扩展服务注册

```csharp
// 在现有的AddAmis方法中添加卡片支持
public static IServiceCollection AddAmis(this IServiceCollection services)
{
    // 现有的注册代码...
    
    // 添加卡片配置构建器
    services.AddScoped<AmisCardConfigBuilder>();
    
    return services;
}
```

## 使用示例

### 1. 基于现有Controller自动生成卡片

```csharp
[ApiController]
[Route("api/[controller]")]
[DisplayName("考生管理")]
public class StudentController : ControllerBase
{
    [HttpGet("profile/{id}")]
    public async Task<StudentProfileDto> GetProfile(int id)
    {
        // 返回考生信息
    }

    [HttpGet("stats")]
    public async Task<ExamStatsDto> GetStats()
    {
        // 返回考试统计
    }
}
```

### 2. DTO定义 - 复用现有特性并添加卡片特性

```csharp
[CardTitle("考生信息", "基本资料")]
[CardLayout(CardLayoutType.Grid, Columns = 2)]
public class StudentProfileDto
{
    [IgnoreColumn] // 复用现有特性
    public int Id { get; set; }

    [Display(Name = "姓名")] // 复用现有特性
    [CardField(icon: "fa-user", Featured = true)] // 新增卡片特性
    public string Name { get; set; }

    [Display(Name = "学号")]
    [CardField(icon: "fa-id-card")]
    public string StudentNumber { get; set; }

    [Display(Name = "性别")]
    [CardField(icon: "fa-venus-mars")]
    public string Gender { get; set; }

    [Display(Name = "准考证号")]
    [CardField(icon: "fa-ticket")]
    public string AdmissionTicket { get; set; }

    [Display(Name = "头像")]
    [AvatarColumn] // 复用现有特性
    public string Avatar { get; set; }
}

[CardTitle("考试统计")]
public class ExamStatsDto
{
    [StatValue("总人数", color: "blue")]
    public int TotalStudents { get; set; }

    [StatValue("在线人数", color: "green")]
    public int OnlineStudents { get; set; }

    [StatValue("已交卷", color: "orange")]
    public int SubmittedCount { get; set; }

    [StatValue("异常行为", color: "red")]
    public int CheatingCount { get; set; }
}
```

### 3. 前端使用 - 基于现有API模式

```javascript
// 获取考生信息卡片配置
const studentCard = await fetch('/api/amis/card/student?cardType=Info')
    .then(res => res.json());

// 获取考试统计卡片配置
const statsCard = await fetch('/api/amis/card/student?cardType=Stat')
    .then(res => res.json());

// 在现有的AMIS页面中使用
const examPage = {
    type: 'page',
    body: [
        {
            type: 'service',
            api: '/api/amis/card/student?cardType=Info',
            body: '${body}' // 直接使用生成的卡片配置
        },
        {
            type: 'service', 
            api: '/api/amis/card/student?cardType=Stat',
            body: '${body}'
        }
    ]
};
```

### 4. 监考大屏应用

```javascript
// 监考大屏 - 统计卡片
const monitorDashboard = {
    type: 'page',
    title: '监考大屏',
    body: [
        {
            type: 'service',
            api: '/exam/api/exam/monitor/stats/${examId}',
            interval: 10000, // 10秒刷新
            body: {
                type: 'service',
                api: '/api/amis/card/exammonitor?cardType=Stat',
                body: '${body}'
            }
        }
    ]
};
```

### 5. 考试客户端应用

```javascript
// 考试客户端 - 考生信息卡片
const examClient = {
    type: 'page',
    body: [
        {
            type: 'service',
            api: '/exam/api/exam/client/profile',
            body: {
                type: 'service',
                api: '/api/amis/card/dto/StudentProfileDto?layout=Grid',
                body: '${body}'
            }
        }
    ]
};
```

## 实现步骤

### 第一步：扩展特性定义（1-2天）
1. 在`Src/Components/CodeSpirit.Amis/Attributes/`目录下创建卡片特性类
2. 定义`CardTitleAttribute`、`CardLayoutAttribute`、`CardFieldAttribute`、`StatValueAttribute`

### 第二步：创建卡片构建器（3-5天）
1. 创建`AmisCardConfigBuilder`类
2. 实现各种卡片类型的生成逻辑
3. 复用现有Helper类的功能

### 第三步：扩展AmisGenerator（1-2天）
1. 在现有的`AmisGenerator`类中添加卡片生成方法
2. 集成缓存机制

### 第四步：扩展API控制器（1-2天）
1. 在现有的`AmisController`中添加卡片API端点
2. 实现控制器和DTO类型查找逻辑

### 第五步：测试和集成（2-3天）
1. 编写单元测试
2. 在监考大屏和考试客户端中集成测试
3. 性能优化和错误处理

## 技术要求

### 依赖项
- 复用现有的`CodeSpirit.Amis`所有依赖
- 无需额外的NuGet包

### 兼容性
- 与现有CRUD表格生成功能完全兼容
- 支持.NET 9和ASP.NET Core
- 前端继续使用AMIS框架

## 优势总结

### 1. 最小化改动
- 复用现有AmisGenerator架构
- 扩展而非重写现有Helper类
- 保持与CRUD表格生成的一致性

### 2. 开发效率
- 基于现有特性定义系统
- 自动生成卡片配置
- 与现有开发流程无缝集成

### 3. 维护性好
- 统一的架构模式
- 共享的缓存和工具类
- 一致的错误处理机制

### 4. 扩展性强
- 支持自定义卡片类型
- 可配置的布局模式
- 易于添加新特性

### 5. 学习成本低
- 开发者继续使用熟悉的特性标记方式
- 无需学习新的API或概念
- 与现有CRUD开发模式一致

这个简易方案通过最小化扩展现有CodeSpirit.Amis架构，实现了卡片生成功能，既保持了系统一致性，又大大降低了开发和维护成本。总开发工期预计7-14天，可以快速投入使用。 