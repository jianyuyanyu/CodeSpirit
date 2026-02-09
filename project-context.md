# CodeSpirit 项目上下文

## 项目概述

CodeSpirit 是一个基于 .NET 10 + Aspire 13.0 的企业级多租户 SaaS 平台，提供完整的业务功能模块和开发框架。

## 技术栈

- **后端框架**: .NET 10 + Aspire 13.0
- **数据库**: MySQL 8.0 / SQL Server 2022 + GreptimeDB（审计）
- **缓存/消息**: Redis + RabbitMQ
- **前端**: React + AMIS (AntD 主题)
- **AI**: OpenAI、通义千问、DeepSeek

## 项目结构

```
Src/
├── ApiServices/        # 14个API服务（Identity, Exam, Survey, AI等）
├── Components/         # 多个核心组件（Amis, LLM, Caching等）
├── CodeSpirit.AppHost/ # Aspire应用宿主
├── CodeSpirit.Core/    # 核心框架
└── CodeSpirit.Web/     # Web前端
```

## 核心架构模式

### 多数据库支持
- 使用数据库特定的 DbContext（`SqlServer{Service}DbContext` / `MySql{Service}DbContext`）
- 迁移按数据库类型分目录创建（`Data/Migrations/SqlServer/` 和 `Data/Migrations/MySql/`）
- 必须使用数据库特定的 DbContext 进行迁移操作

### 分布式架构
- 高可用、容错、降级、最终一致性
- 多级缓存（L1/L2）
- 消息队列（RabbitMQ）

### 多租户架构
- 实体实现 `IMultiTenant` 接口
- 自动应用租户过滤器
- 租户隔离数据访问

### AI 功能集成
- LLM 集成（`ILLMClientFactory`）
- AI 表单填充（`[AiFormFill]` 特性）
- AI 长任务处理（`HeaderOperation` + `aiForm`）

## 开发规范

所有开发规范位于 `.cursor/rules/` 目录，包括：

### 通用规范
- **[C#通用规范](.cursor/rules/cs.mdc)**: XML注释、时间格式（UTC）、序列化（Newtonsoft.Json）
- **[命名约定](.cursor/rules/naming-conventions.mdc)**: 实体、DTO、服务、控制器命名规范

### 按文件类型
- **[DTO规范](.cursor/rules/dto.mdc)**: DTO特性、验证、映射
- **[控制器规范](.cursor/rules/controller.mdc)**: API控制器、操作特性
- **[服务类规范](.cursor/rules/service.mdc)**: 服务接口、实现、生命周期
- **[枚举规范](.cursor/rules/enum.mdc)**: 枚举定义、多语言支持

### 专项规范
- **[API设计](.cursor/rules/api-design.mdc)**: RESTful、路由、响应格式（ApiResponse）
- **[依赖注入](.cursor/rules/dependency-injection.mdc)**: Scrutor自动注册（IScopedDependency等）
- **[启动框架](.cursor/rules/startup-framework.mdc)**: Program.cs配置（2行代码 + BaseApiConfiguration）
- **[数据库迁移](.cursor/rules/database.mdc)**: 多数据库、DbContext、迁移命令
- **[多语言](.cursor/rules/i18n.mdc)**: 资源文件、本地化
- **[AI开发](.cursor/rules/ai-development.mdc)**: AI表单、长任务、LLM
- **[性能优化](.cursor/rules/performance.mdc)**: 异步、缓存、查询
- **[安全规范](.cursor/rules/security.mdc)**: 权限、审计、加密

## 关键约束

### 必须遵循
1. **所有公共成员必须添加 XML 文档注释**
   - 使用 `<summary>`、`<param>`、`<returns>`、`<exception>` 标签
   - 注释应清晰描述功能、参数和返回值

2. **时间使用 UTC 格式**
   - 数据库存储使用 UTC 时间
   - 前端显示时转换为本地时间

3. **序列化使用 Newtonsoft.Json**
   - 统一使用 `Newtonsoft.Json` 而非 `System.Text.Json`
   - 配置使用 `JsonConvert.SerializeObject` 和 `JsonConvert.DeserializeObject`

4. **依赖注入遵循自动注册规范**
   - 接口继承标记接口：`IScopedDependency` / `ITransientDependency` / `ISingletonDependency`
   - 无需手动注册，Scrutor 自动扫描

5. **数据库迁移必须同时支持 MySQL 和 SQL Server**
   - 使用数据库特定的 DbContext（`SqlServer{Service}DbContext` / `MySql{Service}DbContext`）
   - 迁移文件分目录存放
   - 使用雪花 ID 的实体必须配置 `ValueGeneratedNever()`

6. **所有面向用户的文本必须支持多语言**
   - 使用资源文件（`DisplayResources`、`ValidationResources` 等）
   - DTO 验证特性使用多语言资源

7. **异步编程规范**
   - 所有 I/O 操作使用 `async/await`
   - 禁止 `Task.Result` 和 `Task.Wait()`

## 代码质量要求

- 一个 .cs 文件一个顶级类型
- XML 文档注释：`<summary>`, `<param>`, `<returns>`, `<exception>`
- 复杂业务逻辑必须添加行内注释

## 组件使用

- **AMIS**: `antd` 主题，CSS 类用 `antd-` 前缀，特性驱动
- **LLM**: 使用 `ILLMClientFactory`，提示词指定 JSON 输出

## 调试运行

- **启动**: `CodeSpirit.AppHost` (Aspire 协调) → `aspire run` 或 F5
- **Dashboard**: Aspire 管理面板
- **健康检查**: `/health`

## BMAD 工作流集成

本项目已集成 BMAD (Breakthrough Method of Agile AI-Driven Development) 完整工作流，用于结构化的软件开发生命周期管理。

### BMAD 与 CodeSpirit 规范集成

BMAD 工作流已配置为自动遵循 CodeSpirit 的所有开发规范：

- **Analysis 阶段**: 参考 `ai-development.mdc` 的 AI 功能需求分析
- **Planning 阶段**: 遵循 `api-design.mdc` 和 `dto.mdc`
- **Solutioning 阶段**: 参考 `database.mdc` 和 `dependency-injection.mdc`
- **Implementation 阶段**: 遵循所有规范（`cs.mdc`, `controller.mdc`, `service.mdc` 等）
- **Review 阶段**: 执行 CodeSpirit 特定的审查清单

### BMAD 工作流使用

1. **小型任务/Bug 修复** (Quick Flow):
   - `/quick-spec` - 创建技术规范
   - `/quick-dev` - 实现变更
   - `/code-review` - 代码审查

2. **完整功能开发** (Full Flow):
   - `/product-brief` - 产品需求简报
   - `/create-prd` - 创建 PRD
   - `/create-architecture` - 架构设计
   - `/create-epics-and-stories` - 拆分为 Epic 和 Story
   - `/sprint-planning` - Sprint 规划
   - `/dev-story` - 实现 Story
   - `/code-review` - 代码审查
   - `/retrospective` - 复盘

详细使用指南请参考：
- **[BMAD 使用教程](Docs/bmad/bmad-tutorial.md)** - 完整的综合教程（推荐新手阅读）
- [BMAD 工作流指南](Docs/bmad/bmad-workflow-guide.md) - 详细的工作流使用指南
- [BMAD 团队培训指南](Docs/bmad/bmad-team-guide.md) - 团队培训材料
