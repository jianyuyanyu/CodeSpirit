# CodeSpirit 文档分类摘要

## 📚 文档总览

本文档库包含 **96+** 个主要文档，按照 **11** 个核心类别进行分类，涵盖从核心架构到具体实现的完整技术体系。
所有文档已按功能分类组织到对应的目录中，便于查找和管理。

## 🗂️ 目录结构

```
Docs/
├── 01-Core-Docs/                          # 📘 核心文档 (8个文件)
├── 02-UI-Generation/                      # 🎨 界面生成引擎 (17个文件)  
├── 03-Core-Components/                    # 🔧 核心组件 (35个文件)
├── 04-Identity-Auth/                      # 🔐 身份认证与权限 (9个文件)
├── 05-Multi-Tenancy/                      # 🏢 多租户架构 (7个文件)
├── 06-Infrastructure/                     # 🚀 基础设施与运维 (13个文件)
├── 07-API-Communication/                  # 🌐 API与通信 (1个文件)
├── 08-Project-Management/                 # 📊 项目管理 (1个文件)
├── 09-Exam-System/                        # 📝 考试系统 (3个文件)
├── 09-Survey-System/                      # 📋 问卷调查系统 (2个文件)
├── 10-Pathfinder-Project/                 # 🎯 Pathfinder项目 (4个文件)
├── codespirit-ai-features-zh-CN.md        # AI特色功能总览
├── codespirit-framework-highlights-zh-CN.md  # 框架核心亮点
├── codespirit-net10-upgrade-zh-CN.md      # .NET 10升级说明
├── documentation-catalog-zh-CN.md         # 本文件 - 文档分类摘要
└── 配置示例.json                          # 配置文件示例
```

## 🏗️ 分类结构详解

### 📘 01-Core-Docs (8个文件)
**核心文档**：项目架构、开发环境、核心框架等基础文档
- 01-project-architecture-zh-CN.md (项目整体架构设计)
- 01-project-architecture-en-US.md
- 02-technical-system-overview-zh-CN.md (总体技术体系说明)
- 02-technical-system-overview-en-US.md
- 03-development-environment-setup-zh-CN.md (开发环境搭建指南)
- 03-development-environment-setup-en-US.md
- 04-codespirit-core-framework-zh-CN.md (CodeSpirit.Core核心框架)
- 04-codespirit-core-framework-en-US.md
- 05-unified-exception-handling-zh-CN.md (统一异常处理指南)
- 05-unified-exception-handling-en-US.md
- 06-crud-development-example-zh-CN.md (CRUD开发示例)
- 06-crud-development-example-en-US.md
- 07-i18n-localization-guide-zh-CN.md (多语言国际化使用指南)
- 07-i18n-localization-guide-en-US.md
- 08-aliyun-qwen-free-trial-guide-zh-CN.md (阿里云通义千问免费体验指南)
- 08-aliyun-qwen-free-trial-guide-en-US.md

### 🎨 02-UI-Generation (18个文件)
**界面生成引擎**：AMIS引擎、智能图表、UDL系统等前端生成相关
- AMIS列自动推断功能说明.md
- CodeSpirit.Amis.AiForm智能表单使用指南.md ⭐ **新增**
- CodeSpirit.Amis侧边栏联动功能使用指南.md
- CodeSpirit.Amis卡片模式使用指南.md
- CodeSpirit.Amis智能界面生成引擎.md
- CodeSpirit.Amis表单项组使用指南.md
- CodeSpirit.Amis表单默认值使用指南.md
- CodeSpirit.Charts智能图表使用指南.md
- CodeSpirit.SettingsPage设置页自动生成指南.md ⭐ **新增**
- CodeSpirit.UDL-Cards卡片使用指南.md
- CodeSpirit.UdlCards.SDK使用指南.md
- CrudDialogOperation使用指南.md ⭐ **新增**
- OperationAttribute-Actions配置使用指南.md ⭐ **新增**
- UDL-Cards简易实现方案.md
- UDL-Cards详细实现方案.md
- UDL-UI描述语言设计方案.md
- 增强批量导入组件使用指南.md ⭐ **新增**
- 日期时间列优化功能总结.md

### 🔧 03-Core-Components (35个文件)
**核心组件**：导航系统、数据处理、服务组件、AI智能填充、审计系统、定时任务等
- ClientIpService使用指南.md
- CodeSpirit.Aggregator聚合器使用指南.md
- CodeSpirit.AI表单智能填充组件使用指南.md
- CodeSpirit.Amis图标列使用指南.md
- CodeSpirit.Amis图标字段特性使用指南.md
- CodeSpirit.Amis状态映射功能使用指南.md
- CodeSpirit.API配置类开发指南.md
- CodeSpirit.Approval审批模块实现方案.md
- CodeSpirit.Audit-GreptimeDB集成指南.md
- CodeSpirit.Audit分布式审计完整指南.md
- CodeSpirit.Audit审计组件集成使用指南.md
- CodeSpirit.BaseCRUDService使用指南.md
- CodeSpirit.EntityFileReferenceHandler实体文件引用事件处理器使用指南.md
- CodeSpirit.ImageProcessingService图片处理服务集成指南.md
- CodeSpirit.LLM.Audit-LLM审计组件设计方案.md ⭐ **新增**
- CodeSpirit.LLM.Audit-使用指南.md ⭐ **新增**
- CodeSpirit.LLM.Audit-配置示例.json ⭐ **新增**
- CodeSpirit.LLM大语言模型组件使用指南.md
- CodeSpirit.Navigation导航组件使用指南.md
- CodeSpirit.PdfGeneration使用指南.md
- CodeSpirit.ScheduledTasks-README.md ⭐ **新增**
- CodeSpirit.ScheduledTasks定时任务组件使用指南.md ⭐ **新增**
- CodeSpirit.ScheduledTasks技术设计文档.md ⭐ **新增**
- CodeSpirit.Settings设置管理组件使用指南.md
- CodeSpirit.UniqueValidation唯一验证特性使用指南.md
- CodeSpirit中间件插入点使用指南.md
- CodeSpirit分布式锁使用指南.md
- CodeSpirit时间处理机制.md
- CodeSpirit统一启动框架使用指南.md
- CodeSpirit统一启动框架核心架构.md
- CodeSpirit统一启动框架迁移指南.md
- ExampleValueAttribute使用指南.md
- NoAuditAttribute-README.md ⭐ **新增**
- ResourceTagHelper资源管理组件使用指南.md
- Scrutor依赖注入集成指南.md

### 🔐 04-Identity-Auth (9个文件)
**身份认证与权限**：身份认证、权限管理、前端集成、权限继承机制、组织结构管理、第三方登录、短信验证码登录
- CodeSpirit.Authorization权限组件详解.md
- CodeSpirit.Authorization权限继承使用指南.md
- CodeSpirit.IdentityApi身份认证服务.md
- CodeSpirit.TokenManager前端认证管理器使用指南.md
- ISettableCurrentUser可设置用户接口使用指南.md
- 第三方登录通用化架构.md ⭐ **新增**
- 短信验证码登录.md ⭐ **新增**
- 职工管理及组织结构管理功能说明.md
- 部门管理AI快速初始化功能说明.md

### 🏢 05-Multi-Tenancy (7个文件)
**多租户架构**：多租户设计、租户解析、数据隔离、租户感知事件系统等
- 多租户登录页面使用指南.md
- CodeSpirit 多租户数据库上下文架构.md
- CodeSpirit 租户感知事件系统设计.md
- CodeSpirit.DataFilter数据筛选器使用指南.md
- CodeSpirit.TenantResolver租户解析器使用指南.md
- CodeSpirit多租户组件整改计划.md
- 租户事件系统配置示例.json

### 🚀 06-Infrastructure (13个文件)
**基础设施与运维**：消息队列、搜索引擎、网络配置、数据库集成、缓存组件等
- API地址配置指南.md ⭐ **新增**
- API路径前缀配置指南.md ⭐ **新增**
- CodeSpirit.AppHost-Aspire9.5优化指南.md ⭐ **新增**
- CodeSpirit.Aspire数据库集成实现指南.md ⭐ **新增**
- CodeSpirit.Aspire数据库集成统一方案.md ⭐ **新增**
- CodeSpirit.Caching统一缓存组件指南.md ⭐ **新增**
- CodeSpirit.PdfGeneration-PuppeteerSharp问题解决指南.md ⭐ **新增**
- CodeSpirit文件存储服务方案实现.md
- CodeSpirit跨域策略配置指南.md
- Elasticsearch-Aspire-Migration-Summary.md
- RabbitMQ-Aspire-Integration.md
- RabbitMQ故障排除指南.md
- 多数据库DbContext架构使用指南.md ⭐ **新增**

### 🌐 07-API-Communication (1个文件)
**API与通信**：通信机制
- CodeSpirit通用API跳转机制使用指南.md

### 📊 08-Project-Management (1个文件)
**项目管理**：技术债管理
- 技术债管理文档.md

### 📝 09-Exam-System (3个文件)
**考试系统**：考试系统技术架构、业务功能清单等
- README.md - 考试系统概览
- 考试系统完整说明文档.md - 技术架构和API设计
- 考试系统业务功能清单.md - 完整业务功能清单

### 📋 09-Survey-System (2个文件)
**问卷调查系统**：问卷调查模块设计和实现
- 问卷调查模块方案设计.md - 问卷系统架构设计
- 题目类型特定字段实现说明.md - 题目类型实现细节

### 🎯 10-Pathfinder-Project (4个文件)
**Pathfinder项目**：AI驱动的目标管理与自动化执行系统实施方案
- README.md - Pathfinder项目文档入口 ⭐ **新增**
- Pathfinder实施方案.md - 完整的技术实施方案 ⭐ **新增**
- 技术路线图.md - 详细的开发时间表与任务拆解 ⭐ **新增**
- 快速参考指南.md - API/代码模板/命令速查手册 ⭐ **新增**

## 📈 文档使用建议

### 🚀 快速入门路径
1. 📘 [项目整体架构设计](./01-Core-Docs/01-project-architecture-zh-CN.md)
2. 🔧 [开发环境搭建指南](./01-Core-Docs/03-development-environment-setup-zh-CN.md)
3. 💎 [CodeSpirit.Core核心框架](./01-Core-Docs/04-codespirit-core-framework-zh-CN.md)
4. 🎯 [AMIS界面生成引擎](./02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md)
5. 📊 [分布式审计完整指南](./03-Core-Components/CodeSpirit.Audit分布式审计完整指南.md)
6. 🚀 [统一启动框架使用指南](./03-Core-Components/CodeSpirit统一启动框架使用指南.md)

### 🎯 功能实现路径
**权限系统实现**：04-Identity-Auth → 05-Multi-Tenancy
**界面开发**：02-UI-Generation → 03-Core-Components (Navigation, AI表单智能填充)
**数据处理**：03-Core-Components (Aggregator, DataFilter) → 05-Multi-Tenancy
**审计系统实现**：03-Core-Components (Audit分布式审计完整指南) → 06-Infrastructure
**运维部署**：06-Infrastructure (Aspire数据库集成、缓存组件) → 01-Core-Docs
**AI功能集成**：03-Core-Components (LLM、AI表单智能填充) → 02-UI-Generation (AiForm)

### 📋 文档维护
- 新增文档请按功能分类放入对应目录
- 更新README.md和README.zh-CN.md中的链接路径
- 保持文档分类摘要的同步更新
- 遵循统一的命名规范和文档格式

## 🔍 查找指引

**按技术栈查找**：
- .NET Core/Aspire → 01-Core-Docs, 06-Infrastructure
- AMIS前端 → 02-UI-Generation  
- 权限系统 → 04-Identity-Auth
- 多租户 → 05-Multi-Tenancy
- AI功能 → 03-Core-Components (LLM相关), 02-UI-Generation (AiForm)
- 定时任务 → 03-Core-Components (ScheduledTasks)

**按开发阶段查找**：
- 项目初期 → 01-Core-Docs, 03-Core-Components (统一启动框架)
- 功能开发 → 02-UI-Generation, 03-Core-Components
- 系统集成 → 04-Identity-Auth, 05-Multi-Tenancy, 03-Core-Components (Messaging)
- 部署运维 → 06-Infrastructure

**按业务场景查找**：
- 考试系统 → 09-Exam-System
- 问卷调查 → 09-Survey-System
- AI目标管理 → 10-Pathfinder-Project
- 文件管理 → 06-Infrastructure (文件存储服务)
- 审批流程 → 03-Core-Components (Approval)
