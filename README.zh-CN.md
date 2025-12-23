# CodeSpirit（码灵）AI框架 | [English](README.md)

## 框架概览

CodeSpirit（码灵）是一款革命性的全栈低代码+AI（智能体）开发框架，通过**智能代码生成引擎与AI深度协同**，实现**后端驱动式全栈开发范式**。基于 .NET 10 和 Aspire 13.0 技术栈构建，具备企业级技术深度与云原生扩展能力，提供从界面生成、业务逻辑编排到系统运维的全生命周期支持。

**让全栈开发回归工程本质**

- **后端驱动式开发范式 · 企业级开放架构 · AI增强工程闭环**

[![立即体验](https://img.shields.io/badge/%E7%AB%8B%E5%8D%B3%E4%BD%93%E9%AA%8C-%E4%B8%93%E4%B8%9A%E7%89%88-brightgreen)](https://codespirit-app.xin-lai.com/)
*请关注下方公众号获取最新的体验账号及密码。*

***CodeSpirit，让复杂系统开发回归优雅本质！***

*码灵源自我一本未出世的科幻小说，如有兴趣，可开源更新。*



### 🌟 核心创新亮点

#### 革命性AI集成 🤖
- **零配置AI端点生成**: 业界首创！仅需一个特性标记，自动生成AI填充API端点，无需编写任何控制器代码
- **智能表单填充**: 自动分析DTO结构生成提示词，自动解析AI响应并填充表单，开发效率提升10倍+
- **AI导入向导**: 革命性的AI辅助数据导入，智能文本解析、批量AI审核、可视化预览，支持多种格式自动识别和错误修正
- **LLM审计追踪**: 完整的AI决策追溯，记录提示词、响应、成本、性能指标，满足合规要求，支持Elasticsearch和GreptimeDB双存储
- **AI智能工具系统**: 四阶段渐进式工具选择流程(关键词→向量搜索→LLM分类→LLM精选)，智能工具推荐和配置生成
- **AI伙伴系统**: 服务端智能对话平台，支持Function Calling和工具调用，场景化AI助手(考情智析官、命题智创官等)
- **AI长任务处理**: 完善的AI任务管理和进度跟踪，支持多步骤界面展示和实时轮询
- **统一LLM接口**: 支持OpenAI、阿里云通义千问、DeepSeek等多种大语言模型无缝切换，结构化任务处理、批量处理、智能JSON修复

#### 极致开发体验 🚀
- **统一启动框架**: Program.cs仅需2行代码即可完成API项目配置，标准化程度极高
- **自动依赖注入**: 基于Scrutor的零配置批量服务注册，通过接口标记自动识别生命周期
- **多数据库支持**: MySQL和SQL Server灵活切换，独立迁移管理，业务代码完全解耦
- **智能图表推荐**: 基于数据特征自动推荐最佳可视化方案，零前端代码生成图表

#### 企业级架构 🏢
- **事件驱动架构**: 基于RabbitMQ的分布式事件系统，支持租户感知和跨服务通信
- **RBAC+ABAC混合权限**: 细粒度权限控制，支持权限继承和数据范围限制
- **完善的多租户**: 数据隔离、配置隔离、租户感知事件系统
- **分布式组件库**: 分布式锁、文件存储、图片处理、PDF生成等开箱即用

### 核心价值主张

- **全栈智能生成**：通过后端模型驱动前端界面生成，消除 80% 重复编码工作
- **深度可控架构**：生成代码完全开放可控，支持从快速原型到复杂系统的平滑演进
- **企业级工程能力**：内置权限体系、审计追踪、分布式架构支持，开箱即用
- **AI 协同编程**：需求描述 → 原型生成 → 代码验证 → 部署监控
- **云原生底座**：Kubernetes 原生支持，一键部署到多云环境

## 技术架构全景

### 核心技术栈

| 类别         | 技术选型                                    |
| :----------- | :------------------------------------------ |
| **框架**     | .NET 10                                     |
| **语言**     | C# 13（支持Primary Constructor等新特性）    |
| **后端架构** | Clean Architecture + DDD                    |
| **ORM**      | Entity Framework Core（含软删除、审计追踪） |
| **前端生成** | AMIS（动态表单/表格生成）                   |
| **UI描述语言** | UDL（统一UI描述语言 + Cards组件库）       |
| **微服务**   | .NET Aspire 13.0（服务发现、健康检查）           |
| **数据库**   | MySQL 8.0 / SQL Server 2022（多数据库支持） |
| **时序数据库** | GreptimeDB（用于审计日志）                |
| **容器编排** | Kubernetes（支持自动扩缩容）                |
| **身份认证** | JWT + OAuth2.0（RBAC/ABAC混合模型）         |
| **数据访问** | Repository Pattern + CQRS（部分模块）       |

## 项目结构

### 核心架构

```
Src/
├── ApiServices/                          # API服务层
│   ├── CodeSpirit.AiCardsApi/           # AI卡片API服务
│   ├── CodeSpirit.ApprovalApi/          # 审批工作流API服务
│   ├── CodeSpirit.ConfigCenter/         # 配置中心API
│   ├── CodeSpirit.ExamApi/              # 考试系统API
│   ├── CodeSpirit.FileStorageApi/       # 文件存储API
│   ├── CodeSpirit.IdentityApi/          # 身份认证API
│   ├── CodeSpirit.MessagingApi/         # 消息服务API
│   ├── CodeSpirit.PathfinderApi/        # Pathfinder AI目标管理API
│   └── CodeSpirit.SurveyApi/            # 问卷调查API
├── Components/                           # 独立组件库
│   ├── CodeSpirit.Aggregator/           # 数据聚合器组件
│   ├── CodeSpirit.AiFormFill/           # AI表单智能填充组件
│   ├── CodeSpirit.Amis/                 # AMIS界面生成引擎
│   ├── CodeSpirit.Audit/                # 审计追踪组件
│   ├── CodeSpirit.Authorization/        # 权限管理组件
│   ├── CodeSpirit.Caching/              # 分布式缓存组件
│   ├── CodeSpirit.Charts/               # 智能图表组件
│   ├── CodeSpirit.ConfigCenter.Client/ # 配置中心客户端
│   ├── CodeSpirit.LLM/                  # 大语言模型集成组件
│   ├── CodeSpirit.Messaging/            # 消息队列组件
│   ├── CodeSpirit.MultiTenant/          # 多租户组件
│   ├── CodeSpirit.Navigation/           # 导航组件
│   ├── CodeSpirit.PdfGeneration/        # PDF生成组件
│   ├── CodeSpirit.ScheduledTasks/       # 定时任务组件
│   ├── CodeSpirit.Settings/             # 设置管理组件
│   ├── CodeSpirit.Shared/               # 组件共享库
│   └── CodeSpirit.UdlCards/             # UDL卡片组件
├── CodeSpirit.AppHost/                   # Aspire应用宿主（启动项目）
├── CodeSpirit.Core/                      # 核心框架定义
├── CodeSpirit.ServiceDefaults/           # 服务默认配置
├── CodeSpirit.Shared/                    # 全局共享库
├── CodeSpirit.Web/                       # Web前端项目
└── Tests/                                # 测试项目集合
    ├── ApiServices/                      # API服务测试
    │   ├── CodeSpirit.IdentityApi.Tests/
    │   └── CodeSpirit.ExamApi.Tests/
    ├── Components/                       # 组件测试
    │   ├── CodeSpirit.Aggregator.Tests/
    │   ├── CodeSpirit.Authorization.Tests/
    │   ├── CodeSpirit.Caching.Tests/
    │   ├── CodeSpirit.Charts.Tests/
    │   └── ... (其他组件测试)
    ├── Shared/                           # 共享测试基础设施
    │   ├── CodeSpirit.Shared.Tests/
    │   └── CodeSpirit.Components.TestsBase/
    ├── Infrastructure/                   # 基础设施测试
    │   └── CodeSpirit.PdfGeneration.Tests/
    └── LoadTests/                        # 性能负载测试
        └── CodeSpirit.ExamApi.LoadTests/
```

## 功能架构全景

### 一、AI增强特性 🤖

CodeSpirit 提供完整的 AI 集成解决方案，涵盖从基础 LLM 调用到智能业务应用的全链路支持。

**核心组件**:
- **CodeSpirit.LLM**: 统一 LLM 接口，支持 OpenAI、阿里云通义千问、DeepSeek 等多种模型，提供结构化任务处理、批量处理、智能 JSON 修复等高级特性
- **AI表单智能填充** ⭐: 业界首创！仅需一个特性标记 `[AiFormFill]`，自动生成 AI 填充端点，无需编写控制器代码，开发效率提升 10 倍+
- **AI导入向导**: 智能文本解析、批量 AI 审核、可视化预览，支持多种题目格式的自动识别和修正
- **LLM审计追踪**: 完整的 AI 决策追溯，记录提示词、响应、成本、性能指标，支持 Elasticsearch 和 GreptimeDB 双存储
- **AI智能工具系统**: 四阶段渐进式工具选择（关键词→向量搜索→LLM分类→LLM精选），智能工具推荐和配置生成
- **AI伙伴系统**: 服务端智能对话平台，支持 Function Calling，提供考情智析官、命题智创官等场景化 AI 助手
- **AI长任务处理**: 完善的任务管理和进度跟踪，支持多步骤界面展示和实时轮询

**性能优化策略**:
- 🚀 多级缓存策略（L1 内存 + L2 Redis），智能降级机制
- 📦 请求合并与批处理，降低 API 调用成本
- 🌊 流式响应优化，提升用户体验
- ⚡ 智能分批与并发控制，支持大规模数据处理

详见 [CodeSpirit AI特色功能详解](./Docs/CodeSpirit-AI特色功能详解.md)

### 二、智能界面生成引擎

#### 1. 动态导航系统

- 智能权限适配：自动同步RBAC权限模型，实现动态菜单渲染
- 多级导航支持：支持全局导航(*vNext*)/局部导航混合架构

#### 2. 零代码CRUD生成

| 功能模块     | 实现能力                                                   |
| :----------- | :--------------------------------------------------------- |
| 智能表单     | 支持20+字段类型自动映射，包含图片上传、Excel导入等复杂场景 |
| 智能表格     | 嵌套数据呈现、列配置热加载、实时快速编辑                   |
| 批量处理     | Excel模板导入/导出、多格式数据校验、可视化数据修正         |
| 扩展操作体系 | 自定义操作按钮、多步骤审批流、基于权限的上下文敏感操作     |

*注意：这里的零代码指的是零前端代码。*

#### 3. 智能图表分析模块

- 动态图表引擎：根据数据特征自动匹配最佳可视化方案
- SQL2API：根据SQL生成API接口
- SQL2Chart：基于SQL生成图表
- 智能时间维度：支持同比/环比自动计算，时间颗粒度智能适配
- 多数据源聚合：SQL/NoSQL混合数据源联合分析

#### 4. 零代码H5生成（*VNext*）

- 智能表单
- 智能图表

#### 5. UDL（UI描述语言）引擎 🆕

UDL（UI Description Language）是 CodeSpirit 框架中的统一 UI 描述语言，实现"一次定义，处处使用"的跨平台 UI 一致性开发。

**核心特性**：
- **统一描述规范**: 标准化 UI 描述格式，支持 Web、移动端、大屏等多模态输出
- **智能元数据生成**: 基于 API Controller 自动生成 UI 配置，零前端编码
- **UDL Cards 组件**: 预定义卡片模板库（考生信息卡、统计卡、操作卡、答题卡等）
- **多平台渲染**: 统一配置，自动适配 AMIS、MAUI 等不同渲染引擎
- **应用场景**: 监考大屏、考试客户端、管理后台的数据面板和操作界面自动生成

### 三、企业级后端架构

#### 1. 统一启动框架 🚀
Program.cs 仅需 2 行代码完成 API 项目配置，基于 Scrutor 自动服务注册，标准化配置类模式，提供 3 个精确的中间件插入点，自动数据库迁移。

#### 2. 自动依赖注入 🔍
零配置批量服务注册，通过标记接口（`IScopedDependency`、`ISingletonDependency`）自动识别生命周期，约定优于配置。

#### 3. 多数据库支持 🗄️
MySQL 和 SQL Server 灵活切换，独立迁移管理（BaseDbContext → MySqlDbContext / SqlServerDbContext），业务代码完全解耦，自动迁移应用。

#### 4. 事件驱动架构 📡
基于 RabbitMQ 的分布式事件系统，支持租户感知、异步解耦、事件溯源、跨服务通信，可靠消息传递保证。

#### 5. 核心框架特性
- **云原生底座**: k8s 原生支持，深度集成 .NET Aspire，原生支持 Dapr 分布式架构
- **安全体系**: 四层防御体系（认证/授权/审计/加密）
- **高性能保障**: 分布式缓存、二级自动缓存、智能查询优化

#### 6. 关键功能组件
权限系统（RBAC+ABAC）、ORM扩展（软删除/审计/多租户）、数据筛选器、审计服务、健康检查、分布式锁、配置中心、聚合器、PDF生成、时间处理、图片处理、文件存储等开箱即用

### 三、开箱即用功能模块

- **用户中心**: 多因子认证、组织架构管理、细粒度权限控制（RBAC+ABAC混合模型）
- **审计中心**: 操作日志追溯、数据变更追踪、安全合规报告（Elasticsearch存储）
- **配置中心**: 多环境配置管理、版本控制、动态更新（内置实现）
- **考试系统**: 完整的考试管理、题库管理、成绩分析、AI出题（完整案例）
- **问卷调查**: 问卷设计、数据收集、统计分析

## 核心优势

- ✅ **全代码开放**: 生成代码完全可控，支持底层架构定制
- ✅ **原生性能**: 无解释执行损耗，原生代码级性能
- ✅ **极致效率**: 前后端联调 8x 提升，表单开发 12x 提升，权限系统开箱即用
- ✅ **云原生部署**: 混合云/本地部署，Kubernetes 原生支持

## 立即体验

https://codespirit-app.xin-lai.com/

请关注"CodeSpirit-码灵"公众号获取最新的体验账号及密码。

## 快速开始

1. 安装并启动 Docker Desktop

2. 将CodeSpirit.AppHost设为启动项目

3. 启动（启动时会拉取redis、seq、rabbitmq等镜像，如无法拉取，请采取方式进行加速）

   **注意：当前基于.NET Aspire 13.0简化了分布式应用开发时的编排，服务发现、环境变量、容器设置的配置，以便更轻松地在开发阶段进行管理。**

## 开发文档

- Github：[xin-lai/CodeSpirit](https://github.com/xin-lai/CodeSpirit) **（定期推送）**
- Gitee：[magicodes/CodeSpirit](https://gitee.com/magicodes/code-spirit) **（优先推送）**

### 📘 核心文档

1. [🤖 CodeSpirit AI特色功能详解](./Docs/CodeSpirit-AI特色功能详解.md) - 详细介绍框架中AI相关的核心特性和创新点 ⭐
2. [💎 CodeSpirit框架核心亮点](./Docs/CodeSpirit框架核心亮点.md) - 框架整体技术架构、核心组件和最佳实践 ⭐
3. [🏗️ 总体技术体系说明](./Docs/01-Core-Docs/总体技术体系说明.md) - 技术架构和设计理念
4. [🏛️ 后端架构](./Docs/01-Core-Docs/后端架构.md) - 后端架构设计说明
5. [🏗️ 项目整体架构设计](./Docs/01-Core-Docs/项目整体架构设计.md) - 整体项目架构设计和原则
6. [🔧 开发环境搭建指南](./Docs/01-Core-Docs/开发环境搭建指南.md) - 完整的开发环境配置指南
7. [☁️ 阿里云通义千问免费体验指南](./Docs/01-Core-Docs/阿里云通义千问免费体验指南.md) - 零成本体验 AI 能力，详细注册指南、配置教程和成本分析 ⭐
8. [💎 CodeSpirit.Core核心框架](./Docs/01-Core-Docs/CodeSpirit.Core核心框架.md) - 核心框架组件和架构
9. [⚠️ 统一异常处理指南](./Docs/01-Core-Docs/CodeSpirit统一异常处理指南.md) - 企业级异常处理机制和Amis API兼容性
10. [💻 CRUD开发示例](./Docs/01-Core-Docs/CRUD开发示例.md) - 基于题目分类管理模块的完整CRUD开发示例 ⭐

### 🎨 界面生成引擎

11. [🎯 AMIS界面生成引擎](./Docs/02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md) - 智能界面生成核心组件
12. [📊 AMIS列自动推断功能](./Docs/02-UI-Generation/AMIS列自动推断功能说明.md) - 智能表格列生成详解
13. [📝 表单默认值设置](./Docs/02-UI-Generation/CodeSpirit.Amis表单默认值使用指南.md) - 表单默认值配置指南
14. [📋 表单项组使用指南](./Docs/02-UI-Generation/CodeSpirit.Amis表单项组使用指南.md) - 表单字段分组和组织，包含可视化示例和最佳实践
15. [📈 智能图表组件](./Docs/02-UI-Generation/CodeSpirit.Charts智能图表使用指南.md) - 数据可视化解决方案
16. [⏰ 日期时间列优化](./Docs/02-UI-Generation/日期时间列优化功能总结.md) - 时间字段智能处理
17. [🃏 UDL Cards卡片使用指南](./Docs/02-UI-Generation/CodeSpirit.UDL-Cards卡片使用指南.md) - 统一卡片系统使用说明和最佳实践
18. [🛠️ UDL Cards SDK使用指南](./Docs/02-UI-Generation/CodeSpirit.UdlCards.SDK使用指南.md) - C# SDK详细使用指南和API参考
19. [🎨 UDL UI描述语言设计方案](./Docs/02-UI-Generation/UDL-UI描述语言设计方案.md) - 统一UI描述语言架构设计
20. [🎯 UDL Cards详细实现方案](./Docs/02-UI-Generation/UDL-Cards详细实现方案.md) - 卡片组件库实现指南
21. [🎮 UDL Cards简易实现方案](./Docs/02-UI-Generation/UDL-Cards简易实现方案.md) - UDL Cards快速实现指南
22. [🔗 AMIS侧边栏联动功能使用指南](./Docs/02-UI-Generation/CodeSpirit.Amis侧边栏联动功能使用指南.md) - 侧边栏联动功能，支持动态过滤和导航
23. [🤖 AI智能表单使用指南](./Docs/02-UI-Generation/CodeSpirit.Amis.AiForm智能表单使用指南.md) - AI驱动的长时间任务处理框架
24. [⚙️ OperationAttribute配置使用指南](./Docs/02-UI-Generation/OperationAttribute-Actions配置使用指南.md) - 操作特性配置和动作按钮自定义
25. [📦 增强批量导入组件使用指南](./Docs/02-UI-Generation/增强批量导入组件使用指南.md) - 批量数据导入功能增强

### 🔧 核心组件

26. [🤖 AI表单智能填充组件使用指南](./Docs/03-Core-Components/CodeSpirit.AI表单智能填充组件使用指南.md) - 独立AI表单填充组件，支持全局和字段触发模式，零代码自动端点生成，NuGet就绪架构
27. [🧭 Navigation导航组件](./Docs/03-Core-Components/CodeSpirit.Navigation导航组件使用指南.md) - 智能导航系统，支持多平台、权限过滤和上下文感知
28. [🔗 聚合器使用指南](./Docs/03-Core-Components/CodeSpirit.Aggregator聚合器使用指南.md) - 数据聚合和字段替换
29. [⚙️ 设置管理组件](./Docs/03-Core-Components/CodeSpirit.Settings设置管理组件使用指南.md) - 配置管理解决方案
30. [🔒 分布式锁使用指南](./Docs/03-Core-Components/CodeSpirit分布式锁使用指南.md) - 分布式锁实现和使用
31. [📄 PDF生成组件](./Docs/03-Core-Components/CodeSpirit.PdfGeneration使用指南.md) - PDF文档生成服务
32. [🕒 时间处理机制](./Docs/03-Core-Components/CodeSpirit时间处理机制.md) - 统一时间处理方案
33. [🌐 客户端IP服务](./Docs/03-Core-Components/ClientIpService使用指南.md) - 客户端IP获取和处理
34. [📋 审计组件集成使用指南](./Docs/03-Core-Components/CodeSpirit.Audit审计组件集成使用指南.md) - 完整的审计系统集成和使用
35. [🤖 LLM审计组件设计方案](./Docs/03-Core-Components/CodeSpirit.LLM.Audit-LLM审计组件设计方案.md) - LLM调用审计追踪设计
36. [📊 LLM审计使用指南](./Docs/03-Core-Components/CodeSpirit.LLM.Audit-使用指南.md) - LLM审计组件使用方法
37. [⏰ 定时任务组件README](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks-README.md) - 定时任务组件概览
38. [📅 定时任务组件使用指南](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks定时任务组件使用指南.md) - 定时任务组件详细使用说明
39. [🏗️ 定时任务技术设计文档](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks技术设计文档.md) - 定时任务组件架构设计
40. [🚫 NoAudit特性使用指南](./Docs/03-Core-Components/NoAuditAttribute-README.md) - 审计排除特性使用说明
41. [🚀 统一启动框架使用指南](./Docs/03-Core-Components/CodeSpirit统一启动框架使用指南.md) - 统一API项目启动框架，简化项目创建和配置
42. [🏗️ 统一启动框架核心架构](./Docs/03-Core-Components/CodeSpirit统一启动框架核心架构.md) - 统一启动框架的架构设计和实现原理
43. [📦 API配置类开发指南](./Docs/03-Core-Components/CodeSpirit.API配置类开发指南.md) - API配置类的开发规范和最佳实践
44. [🔌 中间件插入点使用指南](./Docs/03-Core-Components/CodeSpirit中间件插入点使用指南.md) - 中间件插入点的使用方法和扩展机制
45. [🔄 统一启动框架迁移指南](./Docs/03-Core-Components/CodeSpirit统一启动框架迁移指南.md) - 现有项目迁移到统一启动框架的详细指南
46. [📝 Scrutor依赖注入集成指南](./Docs/03-Core-Components/Scrutor依赖注入集成指南.md) - Scrutor库在框架中的集成和使用方法
47. [🖼️ 图片处理服务集成指南](./Docs/03-Core-Components/CodeSpirit.ImageProcessingService图片处理服务集成指南.md) - 图片处理服务的集成和使用
48. [📎 实体文件引用事件处理器使用指南](./Docs/03-Core-Components/CodeSpirit.EntityFileReferenceHandler实体文件引用事件处理器使用指南.md) - 实体文件引用的自动管理
49. [🏷️ 资源管理组件使用指南](./Docs/03-Core-Components/ResourceTagHelper资源管理组件使用指南.md) - 资源标签助手的使用方法

### 🔐 身份认证与权限

50. [🔑 身份认证服务](./Docs/04-Identity-Auth/CodeSpirit.IdentityApi身份认证服务.md) - 完整的身份认证服务架构
51. [👮 权限组件详解](./Docs/04-Identity-Auth/CodeSpirit.Authorization权限组件详解.md) - 基于RBAC+ABAC混合模型的综合权限系统
52. [🎫 前端认证管理器](./Docs/04-Identity-Auth/CodeSpirit.TokenManager前端认证管理器使用指南.md) - 前端认证管理和令牌处理
53. [⚙️ 可设置用户接口指南](./Docs/04-Identity-Auth/ISettableCurrentUser可设置用户接口使用指南.md) - 可设置当前用户接口，支持动态用户上下文场景
54. [👥 职工管理及组织结构管理功能说明](./Docs/04-Identity-Auth/职工管理及组织结构管理功能说明.md) - 职工和组织架构管理功能
55. [🏢 部门管理AI快速初始化功能说明](./Docs/04-Identity-Auth/部门管理AI快速初始化功能说明.md) - AI辅助部门结构初始化

### 🏢 多租户架构

56. [🏗️ 多租户组件整改计划](./Docs/05-Multi-Tenancy/CodeSpirit多租户组件整改计划.md) - 多租户架构改进和实施计划
57. [🎯 租户解析器使用指南](./Docs/05-Multi-Tenancy/CodeSpirit.TenantResolver租户解析器使用指南.md) - 租户解析和上下文管理
58. [🔍 数据筛选器使用指南](./Docs/05-Multi-Tenancy/CodeSpirit.DataFilter数据筛选器使用指南.md) - 数据过滤和租户隔离机制
59. [🗄️ 多租户数据库上下文架构](./Docs/05-Multi-Tenancy/CodeSpirit 多租户数据库上下文架构.md) - 多租户数据库架构设计
60. [🖥️ 多租户登录页面使用指南](./Docs/05-Multi-Tenancy/多租户登录页面使用指南.md) - 多租户登录界面实现
61. [🎯 租户感知事件系统设计](./Docs/05-Multi-Tenancy/CodeSpirit 租户感知事件系统设计.md) - 全面的租户感知事件总线架构和实现方案

### 🚀 基础设施与运维

62. [🐰 RabbitMQ集成指南](./Docs/06-Infrastructure/RabbitMQ-Aspire-Integration.md) - 消息队列集成方案
63. [🔧 RabbitMQ故障排除](./Docs/06-Infrastructure/RabbitMQ故障排除指南.md) - 常见问题解决方案
64. [🔍 Elasticsearch迁移总结](./Docs/06-Infrastructure/Elasticsearch-Aspire-Migration-Summary.md) - 搜索引擎集成指南
65. [🌐 跨域策略配置指南](./Docs/06-Infrastructure/CodeSpirit跨域策略配置指南.md) - CORS跨域资源共享配置和安全策略
66. [📁 文件存储服务方案实现](./Docs/06-Infrastructure/CodeSpirit文件存储服务方案实现.md) - 文件存储服务架构设计和实现方案
67. [🌐 API地址配置指南](./Docs/06-Infrastructure/API地址配置指南.md) - API服务地址配置说明
68. [🔧 API路径前缀配置指南](./Docs/06-Infrastructure/API路径前缀配置指南.md) - API路径前缀配置方法
69. [🚀 Aspire9.5优化指南](./Docs/06-Infrastructure/CodeSpirit.AppHost-Aspire9.5优化指南.md) - Aspire 9.5版本优化实践
70. [🗄️ Aspire数据库集成实现指南](./Docs/06-Infrastructure/CodeSpirit.Aspire数据库集成实现指南.md) - Aspire数据库集成详细实现
71. [🎯 Aspire数据库集成统一方案](./Docs/06-Infrastructure/CodeSpirit.Aspire数据库集成统一方案.md) - Aspire数据库集成统一架构
72. [💾 统一缓存组件指南](./Docs/06-Infrastructure/CodeSpirit.Caching统一缓存组件指南.md) - 分布式缓存组件使用说明
73. [📄 PuppeteerSharp问题解决指南](./Docs/06-Infrastructure/CodeSpirit.PdfGeneration-PuppeteerSharp问题解决指南.md) - PDF生成组件常见问题解决
74. [🗄️ 多数据库DbContext架构使用指南](./Docs/06-Infrastructure/多数据库DbContext架构使用指南.md) - 多数据库支持架构设计

### 🌐 API与通信

75. [🔗 通用API跳转机制使用指南](./Docs/07-API-Communication/CodeSpirit通用API跳转机制使用指南.md) - 通用API路由和通信机制

### 📊 项目管理

76. [📋 技术债管理文档](./Docs/08-Project-Management/技术债管理文档.md) - 技术债跟踪和管理规范

### 📝 考试系统

77. [📚 考试系统概览](./Docs/09-Exam-System/README.md) - 完整的考试系统文档导航和快速参考
78. [🏗️ 考试系统技术架构](./Docs/09-Exam-System/考试系统完整说明文档.md) - 全面的技术架构、API设计和安全机制
79. [📋 考试系统业务功能](./Docs/09-Exam-System/考试系统业务功能清单.md) - 完整业务功能清单，涵盖12大模块200+功能特性

### 📋 问卷调查系统

80. [📊 问卷调查模块方案设计](./Docs/09-Survey-System/问卷调查模块方案设计.md) - 问卷调查系统架构设计方案
81. [📝 题目类型特定字段实现说明](./Docs/09-Survey-System/题目类型特定字段实现说明.md) - 题目类型字段实现细节

### 🚀 赞助与技术支持

#### 开源项目支持

CodeSpirit 是一个完全开源的项目，我们致力于为开发者社区提供高质量的低代码开发框架。如果这个项目对您有帮助，请考虑给我们一个 ⭐ Star，或者通过以下方式支持项目的持续发展：

#### 💰 赞助方式

**个人赞助**
- 支持项目持续更新和维护
- 推动新功能开发和性能优化
- 帮助建设更好的开发者社区

**赞助回馈**
- 💬 **赞助100元**：获得一次一对一沟通指导机会，解答技术问题，提供架构建议
- 🎯 **赞助2000元**：获得免费商业授权，可用于商业项目开发和部署

**感谢每一位支持者和贡献者！您的支持是我们持续前进的动力！** 🙏

### 💬 联系我

![wechat](Res/wechat.png)