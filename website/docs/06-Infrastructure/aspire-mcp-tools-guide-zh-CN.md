# Aspire MCP 工具使用指南

## 概述

Aspire MCP（Model Context Protocol）工具是一套强大的命令行工具集，用于管理、监控和调试 .NET Aspire 应用。这些工具通过 AI 助手（如 Cursor）集成，让开发者可以通过自然语言与 Aspire 应用交互。

## 前置条件

- .NET Aspire 应用已配置并可运行
- 已安装 `aspire` CLI 工具
- Aspire AppHost 项目正确配置

## 快速开始

### 启动 Aspire 应用

在使用任何 MCP 工具之前，首先需要启动 Aspire 应用：

```bash
aspire run
```

**说明**：
- 如果已有实例运行，系统会提示停止现有实例
- 只有修改了 `apphost.cs` 文件才需要重启应用
- 遇到问题时，重启可以重置到初始状态

### 访问 Aspire Dashboard

启动后，可以通过以下地址访问 Aspire Dashboard：

```
https://localhost:17109/
```

Dashboard 提供可视化的资源监控、日志查看和性能分析功能。

## MCP 工具详解

### 1. 资源管理工具

#### 1.1 列出所有资源 (list_resources)

**用途**：查看应用中定义的所有资源及其状态

**使用场景**：
- 检查应用启动后的资源状态
- 确认所有服务是否正常运行
- 获取服务的 HTTP 端点地址
- 查看资源的健康状态

**返回信息**：
- 资源名称
- 资源类型（.NET 项目、容器、可执行文件）
- 运行状态（Running、Stopped、Failed）
- HTTP 端点
- 健康状态
- 配置的环境变量
- 资源关系依赖

**示例输出**：
```
资源列表：
- CodeSpirit.IdentityApi [.NET Project]
  状态: Running
  端点: https://localhost:7001
  健康: Healthy
  
- CodeSpirit.ExamApi [.NET Project]
  状态: Running
  端点: https://localhost:7002
  健康: Healthy
  
- redis [Container]
  状态: Running
  端口: 6379
  健康: Healthy
  
- sqlserver [Container]
  状态: Running
  端口: 1433
  健康: Healthy
```

**最佳实践**：
- 在进行任何代码修改前，先检查资源状态
- 定期检查确保所有依赖服务正常运行
- 获取端点地址用于 API 测试或前端配置

#### 1.2 执行资源命令 (execute_resource_command)

**用途**：对资源执行管理操作

**参数**：
- `resourceName`: 资源名称
- `commandName`: 命令名称
  - `resource-start`: 启动资源
  - `resource-stop`: 停止资源
  - `resource-restart`: 重启资源

**使用场景**：
- 重启出现问题的服务
- 停止不需要的资源以节省系统资源
- 启动之前停止的服务

**示例**：
```
# 重启 Identity API
execute_resource_command(
  resourceName: "CodeSpirit.IdentityApi",
  commandName: "resource-restart"
)

# 停止 Exam API
execute_resource_command(
  resourceName: "CodeSpirit.ExamApi",
  commandName: "resource-stop"
)

# 启动 Redis
execute_resource_command(
  resourceName: "redis",
  commandName: "resource-start"
)
```

**注意事项**：
- 如果资源已停止且需要重启，使用 `resource-start` 而不是 `resource-restart`
- 重启资源会中断现有连接
- 某些资源有依赖关系，停止前需要检查

### 2. 日志查看工具

#### 2.1 控制台日志 (list_console_logs)

**用途**：查看资源的标准输出和命令执行日志

**参数**：
- `resourceName`: 要查看日志的资源名称

**使用场景**：
- 查看应用启动日志
- 检查资源启动失败的原因
- 查看容器的输出信息
- 调试资源启动问题

**日志类型**：
- 标准输出（stdout）
- 标准错误（stderr）
- 资源命令执行记录（start、stop、restart）

**示例**：
```
# 查看 Identity API 的控制台日志
list_console_logs(resourceName: "CodeSpirit.IdentityApi")

# 查看 SQL Server 容器日志
list_console_logs(resourceName: "sqlserver")
```

**最佳实践**：
- 资源无法启动时，第一时间查看控制台日志
- 检查是否有端口冲突、连接失败等错误信息
- 容器启动失败时，控制台日志通常会显示镜像拉取或配置问题

#### 2.2 结构化日志 (list_structured_logs)

**用途**：查看应用的结构化日志记录

**参数**：
- `resourceName`（可选）：指定资源名称，不指定则返回所有资源的日志

**使用场景**：
- 查看详细的应用运行日志
- 分析业务逻辑执行情况
- 调试应用程序错误
- 性能分析和优化

**日志级别**：
- `Trace`: 最详细的跟踪信息
- `Debug`: 调试信息
- `Information`: 常规信息
- `Warning`: 警告信息
- `Error`: 错误信息
- `Critical`: 严重错误

**示例**：
```
# 查看所有资源的结构化日志
list_structured_logs()

# 查看特定资源的日志
list_structured_logs(resourceName: "CodeSpirit.IdentityApi")
```

**日志字段**：
- 时间戳
- 日志级别
- 消息内容
- 类别/命名空间
- 异常信息（如果有）
- 自定义属性（如 UserId、TenantId 等）

**最佳实践**：
- 出现业务逻辑错误时，先查看结构化日志
- 关注 `Error` 和 `Critical` 级别的日志
- 使用资源名称过滤减少信息量
- 结合时间戳分析问题发生的时间线

### 3. 分布式追踪工具

#### 3.1 列出追踪 (list_traces)

**用途**：查看分布式追踪信息，跟踪跨服务的操作

**参数**：
- `resourceName`（可选）：限定特定资源的追踪

**使用场景**：
- 分析跨服务调用链路
- 定位性能瓶颈
- 排查服务间通信问题
- 了解请求的完整执行路径

**追踪信息包含**：
- Trace ID：唯一追踪标识符
- 涉及的资源列表
- 总执行时长
- 是否发生错误

**示例**：
```
# 查看所有追踪
list_traces()

# 查看 Identity API 的追踪
list_traces(resourceName: "CodeSpirit.IdentityApi")
```

**输出示例**：
```
追踪列表：
- Trace ID: abc123...
  资源: [CodeSpirit.Web, CodeSpirit.IdentityApi, sqlserver]
  时长: 245ms
  状态: 成功
  
- Trace ID: def456...
  资源: [CodeSpirit.ExamApi, CodeSpirit.FileStorageApi, redis]
  时长: 1203ms
  状态: 错误
```

#### 3.2 查看追踪日志 (list_trace_structured_logs)

**用途**：查看特定追踪的详细日志

**参数**：
- `traceId`: 追踪 ID（从 list_traces 获取）

**使用场景**：
- 深入分析某个请求的完整执行过程
- 查看追踪中每个步骤的详细日志
- 定位错误发生的具体位置
- 分析性能问题的根因

**日志包含**：
- Span ID：每个操作步骤的标识
- 时间戳
- 日志级别
- 消息内容
- 操作名称
- 父 Span 关系

**示例**：
```
# 查看特定追踪的日志
list_trace_structured_logs(traceId: "abc123...")
```

**最佳实践**：
- 调查分布式系统问题时，先用 `list_traces` 找到问题追踪
- 然后用 `list_trace_structured_logs` 查看详细日志
- 这种方式比直接查看资源日志更有针对性
- 可以清楚看到请求在不同服务间的流转过程

### 4. 集成管理工具

#### 4.1 列出可用集成 (list_integrations)

**用途**：查看所有可用的 Aspire 托管集成包

**使用场景**：
- 需要向 AppHost 添加新的资源（数据库、消息队列等）
- 查找特定服务的集成包
- 确认集成包的版本和兼容性

**返回信息**：
- NuGet 包 ID
- 包版本
- 描述信息
- 支持的服务类型

**示例输出**：
```
可用的 Aspire 集成：
- Aspire.Hosting.Redis (9.0.0)
  描述: Redis 缓存集成
  
- Aspire.Hosting.RabbitMQ (9.0.0)
  描述: RabbitMQ 消息队列集成
  
- Aspire.Hosting.PostgreSQL (9.0.0)
  描述: PostgreSQL 数据库集成
  
- Aspire.Hosting.MongoDB (9.0.0)
  描述: MongoDB 数据库集成
```

**最佳实践**：
- 添加资源前，先用此工具查找合适的集成
- 选择与 `Aspire.AppHost.Sdk` 版本对齐的集成版本
- 某些集成版本可能有 preview 后缀

#### 4.2 获取集成文档 (get_integration_docs)

**用途**：获取特定集成的详细使用文档

**参数**：
- `packageId`: 包 ID（如 `Aspire.Hosting.Redis`）
- `packageVersion`: 包版本（如 `9.0.0`）

**使用场景**：
- 了解如何在 AppHost 中配置集成
- 查看集成的配置选项
- 获取最佳实践和示例代码

**示例**：
```
# 获取 Redis 集成文档
get_integration_docs(
  packageId: "Aspire.Hosting.Redis",
  packageVersion: "9.0.0"
)
```

**最佳实践**：
1. 先用 `list_integrations` 找到目标集成和版本
2. 再用 `get_integration_docs` 获取详细文档
3. 按照文档指引配置 AppHost
4. 测试集成是否正常工作

### 5. AppHost 管理工具

#### 5.1 列出 AppHost (list_apphosts)

**用途**：列出所有检测到的 AppHost 连接

**使用场景**：
- 工作区中有多个 AppHost 项目
- 确认当前活动的 AppHost
- 查看 AppHost 的工作目录范围

**返回信息**：
- AppHost 路径
- 是否在工作目录范围内
- 连接状态

**示例输出**：
```
AppHost 列表：
- d:\repos\code-spirit\Src\CodeSpirit.AppHost\CodeSpirit.AppHost.csproj
  范围: 工作目录内
  状态: 活动
```

#### 5.2 选择 AppHost (select_apphost)

**用途**：在多个 AppHost 之间切换

**参数**：
- `appHostPath`: AppHost 项目路径（绝对路径或工作区相对路径）

**使用场景**：
- 工作区有多个微服务项目，每个都有自己的 AppHost
- 需要切换到不同的 AppHost 进行调试

**示例**：
```
# 使用相对路径
select_apphost(appHostPath: "Src/CodeSpirit.AppHost")

# 使用绝对路径
select_apphost(appHostPath: "d:\repos\code-spirit\Src\CodeSpirit.AppHost")
```

## 调试工作流

### 工作流 1: 启动时资源无法运行

**步骤**：

1. **检查资源状态**
   ```
   list_resources
   ```
   确认哪些资源处于 Failed 或 Stopped 状态

2. **查看控制台日志**
   ```
   list_console_logs(resourceName: "问题资源名")
   ```
   查看启动失败的原因，常见问题：
   - 端口冲突
   - Docker 镜像无法拉取
   - 配置错误
   - 依赖服务未启动

3. **查看结构化日志**
   ```
   list_structured_logs(resourceName: "问题资源名")
   ```
   查看应用级别的错误信息

4. **修复问题后重启资源**
   ```
   execute_resource_command(
     resourceName: "问题资源名",
     commandName: "resource-restart"
   )
   ```

### 工作流 2: 分析性能问题

**步骤**：

1. **列出追踪记录**
   ```
   list_traces()
   ```
   找出执行时间异常的追踪

2. **查看追踪详细日志**
   ```
   list_trace_structured_logs(traceId: "慢追踪的ID")
   ```
   分析每个 Span 的耗时

3. **定位瓶颈资源**
   - 查看哪个服务或数据库查询耗时最长
   - 检查是否有 N+1 查询问题
   - 查看是否有网络延迟

4. **优化代码并验证**
   - 修改代码优化性能
   - 重新测试并对比追踪时间

### 工作流 3: 调查业务逻辑错误

**步骤**：

1. **查看结构化日志**
   ```
   list_structured_logs()
   ```
   筛选 Error 和 Critical 级别的日志

2. **定位问题资源**
   ```
   list_structured_logs(resourceName: "疑似问题的资源")
   ```
   聚焦到特定服务的日志

3. **结合追踪分析**
   ```
   list_traces(resourceName: "问题资源")
   list_trace_structured_logs(traceId: "错误追踪ID")
   ```
   查看完整的调用链路

4. **修复并验证**
   - 修改代码修复 Bug
   - 重新测试并检查日志

### 工作流 4: 添加新的集成

**步骤**：

1. **查找可用集成**
   ```
   list_integrations
   ```
   找到需要的集成包和版本

2. **获取集成文档**
   ```
   get_integration_docs(
     packageId: "Aspire.Hosting.XXX",
     packageVersion: "9.0.0"
   )
   ```

3. **添加到 AppHost**
   - 安装 NuGet 包
   - 在 `apphost.cs` 中配置集成
   - 设置必要的环境变量和配置

4. **重启 AppHost**
   ```bash
   aspire run
   ```

5. **验证集成**
   ```
   list_resources
   ```
   确认新资源已正确添加并运行

## 最佳实践

### 1. 开发流程建议

**进行任何代码修改前**：
```
1. aspire run              # 启动应用
2. list_resources          # 检查初始状态
3. 进行代码修改
4. 验证修改（通常不需要重启）
5. 出现问题时查看日志
```

### 2. 定期健康检查

建议定期执行：
```
list_resources  # 确保所有服务健康运行
```

特别是在：
- 开始工作前
- 切换分支后
- 拉取最新代码后
- 修改配置文件后

### 3. 日志查看优先级

遇到问题时的日志查看顺序：

1. **先看控制台日志** (`list_console_logs`)
   - 适合启动失败、容器问题
   
2. **再看结构化日志** (`list_structured_logs`)
   - 适合应用逻辑错误
   
3. **最后看追踪日志** (`list_trace_structured_logs`)
   - 适合分布式调用问题

### 4. 持久化容器注意事项

**重要提示**：开发早期避免使用持久化容器

**原因**：
- 避免状态管理问题
- 简化应用重启流程
- 防止数据不一致

**何时使用持久化**：
- 测试环境需要保留数据
- 开发环境需要模拟生产数据
- 数据初始化成本较高

### 5. 只在必要时重启

**无需重启的情况**：
- 修改 API 代码
- 修改前端代码
- 修改配置文件（某些情况下会热重载）

**必须重启的情况**：
- 修改 `apphost.cs`
- 添加/删除资源
- 修改容器配置
- 应用出现严重问题

## 常见问题

### Q1: 资源启动失败显示端口冲突

**解决方案**：
1. 查看控制台日志确认冲突端口
2. 修改 `Src/CodeSpirit.AppHost/Program.cs` 中的端口配置
3. 或者停止占用端口的其他应用

### Q2: Docker 容器无法启动

**可能原因**：
- Docker Desktop 未运行
- 镜像无法拉取（网络问题）
- 磁盘空间不足

**解决方案**：
1. 确保 Docker Desktop 正在运行
2. 配置镜像加速或使用 VPN
3. 清理 Docker 镜像释放空间

### Q3: 看不到日志输出

**解决方案**：
1. 确认资源正在运行：`list_resources`
2. 检查日志级别配置
3. 查看 Aspire Dashboard 的日志面板

### Q4: 追踪数据不完整

**可能原因**：
- 分布式追踪未正确配置
- 某些服务未启用追踪
- 追踪数据未传播

**解决方案**：
1. 检查所有服务的追踪配置
2. 确保 HTTP 头正确传递
3. 查看 Aspire 官方文档的追踪配置指南

## 更新 AppHost

### 更新到最新版本

```bash
aspire update
```

**说明**：
- 更新 AppHost 到最新版本
- 更新部分 Aspire 相关包
- 可能需要手动更新其他包以确保兼容性

### 使用 dotnet-outdated 工具

**安装**：
```bash
dotnet tool install --global dotnet-outdated-tool
```

**使用**：
```bash
dotnet outdated
```

该工具会列出所有过时的 NuGet 包。

### 注意事项

**重要**：Aspire workload 已过时，不要安装或使用。

## 官方资源

- [Aspire 官方网站](https://aspire.dev)
- [Microsoft Learn - .NET Aspire](https://learn.microsoft.com/dotnet/aspire)
- [NuGet 包详情](https://nuget.org)

## 相关文档

- [开发环境搭建指南](../01-Core-Docs/03-development-environment-setup-zh-CN.md)
- [Playwright MCP 服务使用指南](./playwright-mcp-service-guide-zh-CN.md)

## 总结

Aspire MCP 工具提供了强大的应用管理和调试能力：

✅ **资源管理**：查看状态、执行命令  
✅ **日志查看**：控制台日志、结构化日志  
✅ **分布式追踪**：跨服务调用分析  
✅ **集成管理**：查找和配置新集成  
✅ **AppHost 管理**：多项目切换

通过熟练使用这些工具，可以大大提高开发效率和问题排查速度。
