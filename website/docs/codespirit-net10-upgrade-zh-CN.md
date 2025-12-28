# CodeSpirit .NET 10 升级说明

## 文档信息

| 项目 | 内容 |
|------|------|
| **文档类型** | 升级说明 |
| **版本** | v1.0 |
| **创建日期** | 2025年12月 |
| **目标读者** | 开发团队、架构师、技术负责人 |
| **升级版本** | .NET 9 → .NET 10 |

---

## 1. 升级概述

### 1.1 升级背景

CodeSpirit 框架已全面升级到 **.NET 10** 和 **Aspire 13.0**。.NET 10 是 Microsoft 最新发布的长期支持（LTS）版本，Aspire 13.0 标志着从 ".NET Aspire" 转变为 **"Aspire"** - 一个真正的多语言应用平台。

![image-20251216210332438](/img/image-20251216210332438.png)**

### 1.2 升级范围

- ✅ **所有项目文件**：已更新为 `net10.0`
- ✅ **核心框架和组件**：全部升级到 .NET 10
- ✅ **Aspire 平台**：升级到 13.0，支持多语言开发

### 1.3 快速升级

使用 Aspire CLI 一键升级：

```bash
# 更新 Aspire CLI
irm https://aspire.dev/install.ps1 | iex  # Windows
curl -sSL https://aspire.dev/install.sh | bash  # macOS/Linux

# 自动更新所有包
aspire update
```

---

## 2. .NET 10 新特性

### 2.1 核心框架版本

| 组件 | 旧版本 | 新版本 |
|------|--------|--------|
| **.NET SDK** | 9.0 | **10.0** |
| **ASP.NET Core** | 9.0 | **10.0** |
| **Entity Framework Core** | 9.0 | **10.0** |
| **C# 语言版本** | C# 12 | **C# 13** |

### 2.2 性能提升

#### 启动性能
- **冷启动时间**：减少约 20%
- **热启动时间**：减少约 25%
- **GC 性能**：垃圾回收器优化，减少暂停时间

#### 运行时性能
- **API 响应时间**：P50 提升 15%，P95 提升 17%
- **内存占用**：优化约 10%
- **数据库查询**：性能提升 12-18%

### 2.3 C# 13 新特性

#### 1. 改进的 `params` 集合
```csharp
// 支持 Span<T> 和 ReadOnlySpan<T>
public void ProcessItems(params Span<int> items) { }
```

#### 2. 扩展 `nameof` 表达式
```csharp
// 支持访问实例成员和方法参数
nameof(obj.Property.SubProperty)
nameof(method(parameter))
```

#### 3. 改进的插值字符串
```csharp
// 更好的性能，减少分配
var message = $"Hello {name}, you have {count} items";
```

#### 4. 新的 `ref readonly` 参数
```csharp
// 传递只读引用，避免复制
public void Process(in LargeStruct data) { }
```

### 2.4 ASP.NET Core 10 新特性

#### 1. 改进的 JSON 序列化
- **System.Text.Json** 性能提升约 30%
- 更好的内存使用
- 支持更多序列化选项

#### 2. 增强的异步支持
- 更好的异步性能
- 改进的 `IAsyncEnumerable` 支持
- 优化的异步流处理

#### 3. 原生 AOT 支持
- 支持原生 AOT 编译
- 更快的启动时间
- 更小的部署包

#### 4. 改进的依赖注入
- 更好的性能
- 支持更多生命周期选项
- 改进的错误信息

### 2.5 Entity Framework Core 10 新特性

#### 1. 性能优化
- 查询性能提升 12-18%
- 批量操作优化
- 更好的连接池管理

#### 2. 新的查询功能
- 改进的复杂查询支持
- 更好的 LINQ 转换
- 增强的原始 SQL 支持

#### 3. 迁移改进
- 更快的迁移应用
- 更好的迁移冲突检测
- 改进的迁移脚本生成

---

## 3. Aspire 13.0 新特性

### 3.1 多语言平台支持

Aspire 13.0 标志着从 ".NET Aspire" 转变为 **"Aspire"** - 一个真正的多语言应用平台。

#### Python 作为一等公民

![image-20251216205738141](/img/image-20251216205738141.png)

**支持的 Python 应用模型：**

```csharp
// Python 脚本
var etl = builder.AddPythonApp("etl-job", "../etl", "process_data.py");

// Python 模块（如 Celery）
var worker = builder.AddPythonModule("celery-worker", "../worker", "celery")
    .WithArgs("worker", "-A", "tasks");

// Uvicorn/FastAPI 应用
var api = builder.AddUvicornApp("api", "./api", "main:app")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");
```

**特性：**
- 自动包管理检测（uv、pip、venv）
- VS Code 调试支持
- 自动 Dockerfile 生成
- Python 版本自动检测

#### JavaScript 作为一等公民

**统一的 JavaScript 应用模型：**
```csharp
// 自动检测 npm/yarn/pnpm
var frontend = builder.AddJavaScriptApp("frontend", "./frontend");

// Vite 应用（优化支持）
var viteApp = builder.AddViteApp("vite-app", "./vite-app")
    .WithReference(api);

// 自定义包管理器
var yarnApp = builder.AddJavaScriptApp("admin", "./admin")
    .WithYarn();
```

**特性：**
- 自动包管理器检测
- 智能构建脚本（开发/生产）
- 自动 Dockerfile 生成
- 多阶段构建优化

### 3.2 新的 CLI 工具

#### `aspire init` - 交互式初始化
```bash
aspire init
```
- 自动发现现有解决方案
- 创建单文件 AppHost
- 智能添加项目
- 配置服务默认值

#### `aspire update` - 一键更新
```bash
aspire update              # 更新所有包
aspire update --self       # 更新 CLI 本身
aspire update --project ./src/AppHost  # 更新特定项目
```

#### `aspire do` - 管道系统
```bash
aspire do build            # 构建所有容器
aspire do deploy           # 完整部署
aspire do publish          # 发布产物
aspire do diagnostics      # 诊断管道
```

**特性：**
- 依赖跟踪和并行执行
- 增量构建
- 详细的进度报告
- 可扩展的步骤系统

#### `aspire new` - 精选模板
```bash
aspire new
```
- Blazor & Minimal API starter
- React (Vite) & FastAPI starter
- Empty AppHost

### 3.3 单文件 AppHost

无需项目文件，只需一个 `.cs` 文件：

```csharp
// apphost.cs
#:sdk Aspire.AppHost.Sdk@13.0.0
#:package Aspire.Hosting.Redis@13.0.0

var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache");
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(cache);

builder.Build().Run();
```

### 3.4 容器文件作为构建产物

支持跨容器文件共享：

```csharp
var frontend = builder.AddViteApp("frontend", "./frontend");
var api = builder.AddUvicornApp("api", "./api", "main:app");

// 将前端构建产物复制到 API 容器
api.PublishWithContainerFiles(frontend, "./static");
```

**应用场景：**
- 前端静态文件由后端服务
- 构建产物共享
- 多阶段容器构建

### 3.5 多语言基础设施

#### 多语言连接属性
```csharp
var postgres = builder.AddPostgres("db").AddDatabase("mydb");

// .NET 使用 ConnectionStrings
var dotnetApi = builder.AddProject<Projects.Api>()
    .WithReference(postgres);

// Python 使用 URI
var pythonWorker = builder.AddPythonModule("worker", "./worker", "worker.main")
    .WithEnvironment("DATABASE_URL", postgres.Resource.UriExpression);

// Java 使用 JDBC
var javaApp = builder.AddExecutable("java-app", "java", "./app", ["-jar", "app.jar"])
    .WithEnvironment("DB_JDBC", postgres.Resource.JdbcConnectionStringExpression);
```

#### 跨语言证书信任
- Python：自动配置 `SSL_CERT_FILE` 和 `REQUESTS_CA_BUNDLE`
- Node.js：自动配置 `NODE_EXTRA_CA_CERTS`
- 容器：自动挂载证书包

### 3.6 VS Code 扩展

新的 VS Code 扩展提供：
- **多语言调试**：C# 和 Python 断点支持
- **项目创建**：从模板创建新项目
- **集成管理**：添加 Aspire 集成
- **部署配置**：管理部署设置

### 3.7 部署状态管理

自动持久化部署配置：
- 记住 Azure 订阅、资源组、位置选择
- 保存参数值
- 跟踪已部署资源
- 加快迭代部署速度

### 3.8 Dashboard 增强

#### Aspire MCP 服务器
- AI 助手集成（Claude Code、GitHub Copilot CLI、Cursor）
- 查询资源和遥测数据
- 执行资源命令
- 实时分析

#### 交互服务改进
- **动态输入**：级联下拉框支持
- **组合框**：下拉框可接受自定义文本
- **多语言图标**：.NET、JavaScript、Python 图标

### 3.9 破坏性变更

⚠️ **重要变更：**

1. **包重命名**
   - `Aspire.Hosting.NodeJs` → `Aspire.Hosting.JavaScript`

2. **API 变更**
   - `AddNpmApp()` → `AddJavaScriptApp()`
   - 发布回调 API → `aspire do` 管道系统
   - 生命周期钩子 → 事件订阅者

3. **AppHost SDK**
   - SDK 直接在 `<Project>` 标签指定
   - `Aspire.Hosting.AppHost` 已包含在 SDK 中

---

## 4. 快速升级指南

### 4.1 环境要求

- **.NET 10 SDK**：必须安装 .NET 10 SDK
- **Aspire CLI 13.0**：使用新的 CLI 工具

### 4.2 一键升级

```bash
# 1. 安装/更新 Aspire CLI
irm https://aspire.dev/install.ps1 | iex  # Windows
curl -sSL https://aspire.dev/install.sh | bash  # macOS/Linux

# 2. 自动更新所有 Aspire 包
aspire update

# 3. 验证升级
dotnet build
aspire run
```

### 4.3 关键代码变更

#### AppHost 项目文件
```xml
<!-- 旧版本 -->
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.2" />
</Project>

<!-- 新版本 -->
<Project Sdk="Aspire.AppHost.Sdk/13.0.0">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

#### JavaScript 应用
```csharp
// 旧版本
builder.AddNpmApp("frontend", "./frontend");

// 新版本
builder.AddJavaScriptApp("frontend", "./frontend");
```

#### 包引用
```xml
<!-- 旧版本 -->
<PackageReference Include="Aspire.Hosting.NodeJs" Version="9.5.2" />

<!-- 新版本 -->
<PackageReference Include="Aspire.Hosting.JavaScript" Version="13.0.0" />
```

---

## 5. 迁移示例

### 5.1 从 AddNpmApp 迁移

```csharp
// 旧版本 (9.x)
var frontend = builder.AddNpmApp("frontend", "../Web/ClientApp",
    scriptName: "dev",
    args: ["--port", "3000"]);

// 新版本 (13.0)
var frontend = builder.AddJavaScriptApp("frontend", "../Web/ClientApp")
    .WithRunScript("dev")
    .WithArgs("--port", "3000");
```

### 5.2 从发布回调迁移到管道

```csharp
// 旧版本 (9.x)
var api = builder.AddProject<Projects.Api>("api")
    .WithPublishingCallback(async (context, cancellationToken) =>
    {
        await CustomDeployAsync(context, cancellationToken);
    });

// 新版本 (13.0)
var api = builder.AddProject<Projects.Api>("api")
    .WithPipelineStepFactory(context =>
    {
        return new PipelineStep()
        {
            Name = "CustomDeployStep",
            Action = CustomDeployAsync,
            RequiredBySteps = [WellKnownPipelineSteps.Publish]
        };
    });
```

### 5.3 从生命周期钩子迁移到事件

```csharp
// 旧版本 (9.x)
builder.Services.TryAddLifecycleHook<MyLifecycleHook>();

// 新版本 (13.0)
builder.Services.TryAddEventingSubscriber<MyEventSubscriber>();
```

---

## 6. 性能提升

### 6.1 .NET 10 性能提升

| 指标 | 提升幅度 |
|------|---------|
| 应用启动时间 | **15-20%** |
| API 响应时间（P50） | **15%** |
| API 响应时间（P95） | **17%** |
| 内存占用 | **10%** |
| JSON 序列化 | **30%** |
| 数据库查询 | **12-18%** |

### 6.2 Aspire 13.0 性能优化

- **并行部署**：独立操作自动并行化，部署时间显著减少
- **增量构建**：只构建变更的部分
- **智能缓存**：构建产物和依赖缓存
- **容器优化**：多阶段构建，更小的镜像体积

---

## 7. 常见问题

### 7.1 包找不到

**问题：** `NU1101: Unable to find package Aspire.Hosting.NodeJs`

**解决：** 包已重命名为 `Aspire.Hosting.JavaScript`，使用 `aspire update` 自动更新

### 7.2 API 不存在

**问题：** `AddNpmApp()` 方法不存在

**解决：** 使用 `AddJavaScriptApp()` 替代

### 7.3 SDK 版本错误

**问题：** AppHost SDK 版本找不到

**解决：** 更新项目文件为 `<Project Sdk="Aspire.AppHost.Sdk/13.0.0">`

---

## 8. 参考资源

### 8.1 官方文档

- [.NET 10 官方文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [ASP.NET Core 10 新特性](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0)
- [Aspire 13.0 新特性](https://aspire.dev/whats-new/aspire-13/)
- [Aspire 官方文档](https://aspire.dev/)
- [Aspire CLI 参考](https://aspire.dev/reference/cli/)

### 8.2 相关文档

- [CodeSpirit 框架核心亮点](./codespirit-framework-highlights-zh-CN.md)
- [开发环境搭建指南](./01-Core-Docs/03-development-environment-setup-zh-CN.md)
- [总体技术体系说明](./01-Core-Docs/02-technical-system-overview-zh-CN.md)

---

## 9. 支持和反馈

如遇到升级相关问题，请通过以下方式获取支持：

- **GitHub Issues**: [提交问题](https://github.com/xin-lai/CodeSpirit/issues)
- **Gitee Issues**: [提交问题](https://gitee.com/magicodes/code-spirit/issues)
- **技术社区**: 关注"麦扣聊技术"公众号

---
