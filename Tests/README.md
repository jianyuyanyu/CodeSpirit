# CodeSpirit 测试项目

本目录包含 CodeSpirit 框架的所有测试项目，按功能类型组织。

## 📁 目录结构

```
Tests/
├── ApiServices/           # API 服务层测试
├── Components/            # 组件测试
├── Shared/               # 共享测试基础设施
├── Infrastructure/       # 基础设施测试
└── LoadTests/            # 性能负载测试
```

## 🧪 测试项目概览

### API 服务测试 (`ApiServices/`)

API 服务层的单元测试和集成测试。

| 项目 | 描述 | 测试内容 |
|------|------|----------|
| **CodeSpirit.IdentityApi.Tests** | 身份认证API测试 | 用户管理、角色权限、认证授权、审计日志 |
| **CodeSpirit.ExamApi.Tests** | 考试系统API测试 | 题目管理、试卷生成、成绩评定、缓存策略 |

### 组件测试 (`Components/`)

可复用组件的单元测试。

| 项目 | 描述 | 核心功能 |
|------|------|----------|
| **CodeSpirit.Aggregator.Tests** | 数据聚合器测试 | 聚合服务、中间件、全局配置 |
| **CodeSpirit.Authorization.Tests** | 权限组件测试 | 权限验证、当前用户、HTTP方法辅助 |
| **CodeSpirit.Amis.Tests** | AMIS界面生成测试 | 字段工厂、表单生成示例 |
| **CodeSpirit.Audit.Tests** | 审计追踪测试 | 审计特性、服务、控制器集成 |
| **CodeSpirit.Caching.Tests** | 缓存组件测试 | 分布式缓存、序列化、键管理 |
| **CodeSpirit.Charts.Tests** | 图表组件测试 | ECharts集成、数据处理、性能测试 |
| **CodeSpirit.ScheduledTasks.Tests** | 定时任务测试 | Cron表达式、任务调度、超时处理 |
| **CodeSpirit.Settings.Tests** | 设置管理测试 | 配置服务、基础测试框架 |

### 共享测试基础设施 (`Shared/`)

跨项目共享的测试基础设施和工具。

| 项目 | 描述 |
|------|------|
| **CodeSpirit.Components.TestsBase** | 组件测试基类 | 提供通用的测试基础设施和辅助方法 |
| **CodeSpirit.Shared.Tests** | 共享库测试 | HTTP异常过滤器、核心功能测试 |

### 基础设施测试 (`Infrastructure/`)

基础设施相关的测试。

| 项目 | 描述 |
|------|------|
| **CodeSpirit.PdfGeneration.Tests** | PDF生成测试 | PDF生成服务、扩展方法测试 |

### 性能负载测试 (`LoadTests/`)

系统性能和负载测试。

| 项目 | 描述 | 工具 |
|------|------|------|
| **CodeSpirit.ExamApi.LoadTests** | 考试系统负载测试 | 使用 NBomber 进行负载测试和性能评估 |

## 🚀 运行测试

### 运行所有测试

```bash
# 在项目根目录执行
dotnet test
```

### 运行特定类别的测试

```bash
# API 服务测试
dotnet test Tests/ApiServices/**/*.csproj

# 组件测试
dotnet test Tests/Components/**/*.csproj

# 特定项目测试
dotnet test Tests/ApiServices/CodeSpirit.IdentityApi.Tests/
```

### 生成测试覆盖率报告

```bash
# 运行测试并收集覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 使用 ReportGenerator 生成报告（需要先安装）
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report
```

### 运行负载测试

```bash
cd Tests/LoadTests/CodeSpirit.ExamApi.LoadTests
dotnet run
```

详见各负载测试项目的 README 文件。

## 📊 测试技术栈

### 测试框架
- **xUnit** - 主要测试框架
- **Moq** - Mock 框架
- **FluentAssertions** - 流畅断言库（部分项目）

### 测试工具
- **Microsoft.EntityFrameworkCore.InMemory** - 内存数据库测试
- **Microsoft.AspNetCore.Mvc.Testing** - ASP.NET Core 集成测试
- **NBomber** - 负载测试工具（LoadTests）

### 代码覆盖率
- **coverlet.collector** - 代码覆盖率收集器

## 🏗️ 测试项目结构规范

每个测试项目应遵循以下结构：

```
ProjectName.Tests/
├── Controllers/        # 控制器测试
├── Services/          # 服务层测试
├── Models/            # 模型测试
├── Integration/       # 集成测试
├── TestBase/          # 测试基类
├── Examples/          # 使用示例
└── ProjectName.Tests.csproj
```

## 📝 编写测试指南

### 命名规范

```csharp
// 测试类命名: {待测试类名}Tests
public class UserServiceTests { }

// 测试方法命名: {方法名}_{场景}_{预期结果}
[Fact]
public void GetUser_WhenUserExists_ReturnsUser() { }

// 中文命名也可以（更清晰）
[Fact]
public void GetUser_用户存在时_应该返回用户信息() { }
```

### 测试结构 (AAA 模式)

```csharp
[Fact]
public void TestMethod()
{
    // Arrange - 准备测试数据和依赖
    var userId = 1;
    var mockService = new Mock<IUserService>();
    
    // Act - 执行被测试的操作
    var result = await controller.GetUser(userId);
    
    // Assert - 验证结果
    Assert.NotNull(result);
    Assert.Equal(userId, result.Id);
}
```

### 使用测试基类

```csharp
// 继承共享的测试基类
public class MyServiceTests : ServiceTestBase
{
    private readonly MyService _service;
    
    public MyServiceTests()
    {
        _service = new MyService(DbContext);
    }
    
    // ... 测试方法
}
```

## 🔍 测试覆盖率目标

- **核心业务逻辑**: 目标覆盖率 ≥ 80%
- **API 控制器**: 目标覆盖率 ≥ 70%
- **公共组件**: 目标覆盖率 ≥ 85%
- **工具类/辅助方法**: 目标覆盖率 ≥ 90%

## 🐛 调试测试

### Visual Studio / Rider
- 在测试方法上设置断点
- 右键点击测试方法 → "调试测试"

### VS Code
- 安装 C# 扩展
- 使用 `.vscode/launch.json` 配置调试

### 命令行
```bash
# 运行特定测试
dotnet test --filter "FullyQualifiedName~UserServiceTests"

# 显示详细输出
dotnet test --logger "console;verbosity=detailed"
```

## 🔄 持续集成

测试在以下场景自动运行：
- Pull Request 创建或更新时
- 合并到主分支时
- 定时构建（每日）

详见项目根目录的 CI/CD 配置文件。

## 📚 相关文档

- [开发环境搭建指南](../Docs/01-Core-Docs/开发环境搭建指南.md)
- [项目整体架构设计](../Docs/01-Core-Docs/项目整体架构设计.md)
- [CodeSpirit.Core核心框架](../Docs/01-Core-Docs/CodeSpirit.Core核心框架.md)

## 🤝 贡献指南

### 添加新测试项目

1. 在相应类别目录下创建测试项目
2. 更新 `CodeSpirit.sln` 添加项目引用
3. 确保项目引用路径正确（相对于根目录）
4. 遵循现有项目的包版本

### 包版本要求

为保持一致性，请使用以下版本：
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
```

## 📞 支持

如有问题或建议，请：
- 提交 Issue
- 联系团队成员
- 查阅相关文档

---

**注意**: 所有测试应该是独立的、可重复的，不应依赖外部服务或特定的执行顺序。

