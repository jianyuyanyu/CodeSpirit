# CodeSpirit AI Framework | [简体中文](README.zh-CN.md)

## Framework Overview

CodeSpirit is a revolutionary full-stack low-code + AI development framework that achieves **backend-driven full-stack development paradigm** through **intelligent code generation engine and deep AI collaboration**. Built on .NET 10 and Aspire 13.0 technology stack, it provides enterprise-level technical depth and cloud-native scalability, supporting the entire lifecycle from UI generation and business logic orchestration to system operations.

**Return Full-Stack Development to Engineering Essence**

- **Backend-Driven Development Paradigm · Enterprise-Grade Open Architecture · AI-Enhanced Engineering Loop**

***CodeSpirit, bringing elegant simplicity to complex system development!***

### 🌟 Core Innovation Highlights

#### Revolutionary AI Integration 🤖
- **Zero-Configuration AI Endpoint Generation**: Industry-first! Just one attribute tag automatically generates AI-fill API endpoints without writing any controller code
- **Smart Form Filling**: Automatically analyzes DTO structure to generate prompts, automatically parses AI responses and fills forms, boosting development efficiency by 10x+
- **AI Long-Running Task Handling**: Complete AI task management and progress tracking with multi-step interface display and real-time polling
- **Unified LLM Interface**: Seamlessly switch between multiple large language models including OpenAI, Alibaba Qwen, DeepSeek

#### Ultimate Development Experience 🚀
- **Unified Startup Framework**: Program.cs requires only 2 lines of code to complete API project configuration with extreme standardization
- **Automatic Dependency Injection**: Zero-configuration batch service registration based on Scrutor, automatically identifying lifecycle through interface tags
- **Multi-Database Support**: Flexible switching between MySQL and SQL Server with independent migration management and completely decoupled business code
- **Smart Chart Recommendations**: Automatically recommends optimal visualization solutions based on data characteristics with zero frontend code chart generation

#### Enterprise-Grade Architecture 🏢
- **Event-Driven Architecture**: RabbitMQ-based distributed event system with tenant-awareness and cross-service communication
- **RBAC+ABAC Hybrid Permissions**: Fine-grained permission control with permission inheritance and data scope restrictions
- **Comprehensive Multi-Tenancy**: Data isolation, configuration isolation, and tenant-aware event system
- **Distributed Component Library**: Out-of-the-box distributed locks, file storage, image processing, PDF generation

### Core Value Propositions

- **Full-Stack Intelligent Generation**: Eliminate 80% repetitive coding through backend model-driven frontend UI generation
- **Deeply Controllable Architecture**: Generated code is fully open and controllable, supporting smooth evolution from rapid prototyping to complex systems
- **Enterprise-Grade Engineering Capabilities**: Built-in permission system, audit tracking, distributed architecture support, out-of-the-box
- **AI-Collaborative Programming**: Requirement Description → Prototype Generation → Code Verification → Deployment Monitoring
- **Cloud-Native Foundation**: Native Kubernetes support, one-click deployment to multi-cloud environments

## Technical Architecture Overview

### Architecture Diagram

```mermaid
flowchart TD
    classDef uiLayer fill:#f9d1d1,stroke:#333,stroke-width:1px
    classDef backendLayer fill:#d1f9d1,stroke:#333,stroke-width:1px
    classDef cloudLayer fill:#d1d1f9,stroke:#333,stroke-width:1px
    
    subgraph UI["Intelligent UI Generation Engine"]
        direction LR
        A1["🧭 Dynamic Navigation"] --> A2["📝 Smart Forms"]
        A2 --> A3["📊 Smart Tables"]
        A3 --> A4["📦 Batch Processing"]
    end
    
    subgraph Backend["Enterprise Backend Architecture"]
        direction LR
        B1["🔐 Permission System"] --> B2["💾 ORM Extensions"]
        B2 --> B3["🏢 Multi-tenancy"]
        B3 --> B4["📋 Audit Service"]
    end
    
    subgraph Cloud["Cloud-Native Foundation"]
        direction LR
        C1["🚀 .NET Aspire"] --> C2["⚙️ Config Center"]
        C2 --> C3["☸️ K8s Support"]
        C3 --> C4["📦 Distributed Cache"]
    end
    
    UI --> Backend
    Backend --> Cloud
    
    class UI uiLayer
    class Backend backendLayer
    class Cloud cloudLayer
```

### UDL Integration Architecture

UDL serves as the core UI description language of the CodeSpirit framework, providing unified interface description and rendering capabilities:

```mermaid
graph TB
    subgraph "CodeSpirit Framework Architecture"
        subgraph "Frontend Layer"
            WebApp["Web Application"]
            MobileApp["Mobile Application"]
            LargeScreen["Large Screen Display"]
        end
        
        subgraph "UDL Engine Layer"
            UDLSpec["UDL Specification<br/>规范定义"]
            UDLParser["UDL Parser<br/>解析器"]
            MetadataExtractor["Metadata Extractor<br/>元数据提取器"]
            CardEngine["Card Engine<br/>卡片引擎"]
        end
        
        subgraph "Rendering Layer"
            AmisRenderer["AMIS Renderer<br/>Web渲染器"]
            MauiRenderer["MAUI Renderer<br/>桌面/移动渲染器"]
            CustomRenderer["Custom Renderer<br/>自定义渲染器"]
        end
        
        subgraph "Template Library"
            StudentCard["Student Profile Card<br/>考生信息卡片"]
            StatCard["Statistics Card<br/>统计卡片"]
            ActionCard["Action Card<br/>操作卡片"]
            AnswerCard["Answer Card<br/>答题卡"]
        end
        
        subgraph "Backend Services"
            ExamAPI["Exam API<br/>考试服务"]
            UserAPI["User API<br/>用户服务"]
            MonitorAPI["Monitor API<br/>监控服务"]
        end
    end
    
    %% UDL Engine Flow
    UDLParser --> MetadataExtractor
    MetadataExtractor --> CardEngine
    UDLSpec --> UDLParser
    
    %% UDL to Renderers
    CardEngine --> AmisRenderer
    CardEngine --> MauiRenderer
    CardEngine --> CustomRenderer
    
    %% Template Library
    CardEngine --> StudentCard
    CardEngine --> StatCard
    CardEngine --> ActionCard
    CardEngine --> AnswerCard
    
    %% Renderers to Frontend
    AmisRenderer --> WebApp
    MauiRenderer --> MobileApp
    CustomRenderer --> LargeScreen
    
    %% Backend Integration
    MetadataExtractor --> ExamAPI
    MetadataExtractor --> UserAPI
    MetadataExtractor --> MonitorAPI
    
    style UDLSpec fill:#ffeb3b
    style UDLParser fill:#ffeb3b
    style MetadataExtractor fill:#ffeb3b
    style CardEngine fill:#ffeb3b
    style StudentCard fill:#4caf50
    style StatCard fill:#4caf50
    style ActionCard fill:#4caf50
    style AnswerCard fill:#4caf50
```

### Core Technology Stack

| Category | Technology Selection |
| :--- | :--- |
| **Framework** | .NET 10 |
| **Language** | C# 13 (Supporting Primary Constructor and other new features) |
| **Backend Architecture** | Clean Architecture + DDD |
| **ORM** | Entity Framework Core (with soft delete, audit tracking) |
| **Frontend Generation** | AMIS (Dynamic form/table generation) |
| **UI Description Language** | UDL (Unified UI Description Language + Cards Library) |
| **Microservices** | .NET Aspire 13.0 (Service discovery, health checks) |
| **Database** | MySQL 8.0 / SQL Server 2022 (Multi-database support) |
| **Time-Series DB** | GreptimeDB (For audit logging) |
| **Container Orchestration** | Kubernetes (Supporting auto-scaling) |
| **Identity Authentication** | JWT + OAuth2.0 (RBAC/ABAC hybrid model) |
| **Data Access** | Repository Pattern + CQRS (partial modules) |

## Project Structure

### Core Architecture

```
Src/
├── ApiServices/                          # API Service Layer
│   ├── CodeSpirit.AiCardsApi/           # AI Cards API Service
│   ├── CodeSpirit.ApprovalApi/          # Approval Workflow API Service
│   ├── CodeSpirit.ConfigCenter/         # Configuration Center API
│   ├── CodeSpirit.ExamApi/              # Exam System API
│   ├── CodeSpirit.FileStorageApi/       # File Storage API
│   ├── CodeSpirit.IdentityApi/          # Identity Authentication API
│   ├── CodeSpirit.MessagingApi/         # Messaging Service API
│   ├── CodeSpirit.PathfinderApi/        # Pathfinder AI Goal Management API
│   └── CodeSpirit.SurveyApi/            # Survey System API
├── Components/                           # Independent Component Library
│   ├── CodeSpirit.Aggregator/           # Data Aggregator Component
│   ├── CodeSpirit.AiFormFill/           # AI Form Fill Component
│   ├── CodeSpirit.Amis/                 # AMIS UI Generation Engine
│   ├── CodeSpirit.Audit/                # Audit Tracking Component
│   ├── CodeSpirit.Authorization/        # Authorization Management Component
│   ├── CodeSpirit.Caching/              # Distributed Caching Component
│   ├── CodeSpirit.Charts/               # Smart Chart Component
│   ├── CodeSpirit.ConfigCenter.Client/ # Configuration Center Client
│   ├── CodeSpirit.LLM/                  # Large Language Model Integration
│   ├── CodeSpirit.Messaging/            # Message Queue Component
│   ├── CodeSpirit.MultiTenant/          # Multi-Tenancy Component
│   ├── CodeSpirit.Navigation/           # Navigation Component
│   ├── CodeSpirit.PdfGeneration/        # PDF Generation Component
│   ├── CodeSpirit.ScheduledTasks/       # Scheduled Tasks Component
│   ├── CodeSpirit.Settings/             # Settings Management Component
│   ├── CodeSpirit.Shared/               # Component Shared Library
│   └── CodeSpirit.UdlCards/             # UDL Cards Component
├── CodeSpirit.AppHost/                   # Aspire Application Host (Startup Project)
├── CodeSpirit.Core/                      # Core Framework Definitions
├── CodeSpirit.ServiceDefaults/           # Service Default Configuration
├── CodeSpirit.Shared/                    # Global Shared Library
├── CodeSpirit.Web/                       # Web Frontend Project
└── Tests/                                # Test Project Collection
    ├── ApiServices/                      # API Service Tests
    │   ├── CodeSpirit.IdentityApi.Tests/
    │   └── CodeSpirit.ExamApi.Tests/
    ├── Components/                       # Component Tests
    │   ├── CodeSpirit.Aggregator.Tests/
    │   ├── CodeSpirit.Authorization.Tests/
    │   ├── CodeSpirit.Caching.Tests/
    │   ├── CodeSpirit.Charts.Tests/
    │   └── ... (Other Component Tests)
    ├── Shared/                           # Shared Test Infrastructure
    │   ├── CodeSpirit.Shared.Tests/
    │   └── CodeSpirit.Components.TestsBase/
    ├── Infrastructure/                   # Infrastructure Tests
    │   └── CodeSpirit.PdfGeneration.Tests/
    └── LoadTests/                        # Performance Load Tests
        └── CodeSpirit.ExamApi.LoadTests/
```

## Functional Architecture Overview

### I. Intelligent UI Generation Engine

#### 1. Dynamic Navigation System

- Intelligent Permission Adaptation: Automatic synchronization with RBAC permission model for dynamic menu rendering
- Multi-level Navigation Support: Global navigation(*vNext*)/local navigation hybrid architecture

#### 2. Zero-Code CRUD Generation

| Module | Capabilities |
| :--- | :--- |
| Smart Forms | 20+ field type automatic mapping, including complex scenarios like image upload and Excel import |
| Smart Tables | Nested data presentation, column configuration hot reload, real-time quick editing |
| Batch Processing | Excel template import/export, multi-format data validation, visual data correction |
| Extended Operation System | Custom operation buttons, multi-step approval flows, permission-based context-sensitive operations |

*Note: Zero-code here refers to zero frontend code.*

#### 3. Intelligent Chart Analysis Module

- Dynamic Chart Engine: Automatically matches optimal visualization solutions based on data characteristics
- SQL2API: Generate API interfaces from SQL
- SQL2Chart: Generate charts based on SQL
- Intelligent Time Dimension: Support automatic year-over-year/month-over-month calculations, intelligent time granularity adaptation
- Multi-data Source Aggregation: SQL/NoSQL hybrid data source joint analysis

#### 4. Zero-Code H5 Generation (*VNext*)

- Smart Forms
- Smart Charts

#### 5. UDL (UI Description Language) Engine 🆕

UDL (UI Description Language) is a unified UI description language in the CodeSpirit framework, achieving "define once, use everywhere" cross-platform UI consistency development.

**Core Features**:
- **Unified Description Specification**: Standardized UI description format supporting Web, mobile, large screen and other multi-modal outputs
- **Intelligent Metadata Generation**: Automatically generate UI configuration based on API Controllers with zero frontend coding
- **UDL Cards Components**: Predefined card template library for rapid construction of information display, statistical analysis, and interactive interfaces
- **Multi-platform Rendering**: Unified configuration, automatically adapting to different rendering engines like AMIS, MAUI

**UDL Cards Predefined Templates**:
- Student Profile Card (student-profile-card): Display basic information with avatar and field icons support
- Statistics Card (stat-card): Data statistics display with progress bars, percentages, and status indicators
- Action Card (action-card): Interactive operation panels with multi-level operations and permission control
- Answer Card (answer-card): Exam scenario specific, answer status visualization

**Technical Architecture**:
```mermaid
graph TB
    subgraph "UDL Architecture Layers"
        UDLSpec["UDL Specification"] --> UDLParser["UDL Parser"]
        UDLParser --> RenderEngine["Render Engine"]
        RenderEngine --> AmisRenderer["AMIS Renderer"]
        RenderEngine --> MauiRenderer["MAUI Renderer"]
        ApiMetadata["API Metadata"] --> UDLParser
        CardTemplates["Cards Template Library"] --> RenderEngine
    end
```

**Application Scenarios**:
- Exam Monitoring Dashboard: Real-time statistics, status monitoring, anomaly alerts
- Exam Client: Student information display, answer progress tracking
- Management Backend: Data panels, operation interface auto-generation

### II. Enterprise-Grade Backend Architecture

#### 1. Core Framework Features

- **Cloud-Native Foundation**: Native k8s support, deep integration with .NET Aspire, native support for Dapr distributed architecture
- **Security System**: Four-layer defense system (Authentication/Authorization/Audit/Encryption)
- **High-Performance Guarantee**: Distributed caching, automatic second-level caching, intelligent query optimization

#### 2. Key Functional Components

- **Permission System**: RBAC+ABAC hybrid model, fine-grained permission control
- **ORM Extensions**: Soft delete, audit tracking, multi-tenancy support
- **Multi-tenancy**: Data isolation, configuration isolation
- **Data Filters**: Global filters, automatic injection
- **Audit Service**: Full-chain operation tracking, data change recording
- **Health Checks**: Service status monitoring, automatic failover
- **Event Bus**: Distributed event processing, message queue integration
- **Distributed Lock**: Redis distributed lock, duplicate submission prevention
- **Configuration Center**: Multi-environment configuration management, dynamic configuration updates
- **Aggregator**: Data aggregation, dynamic field replacement
- **PDF Generation**: Template-based PDF document generation
- **Time Processing**: Unified time processing mechanism, timezone support

### III. Out-of-the-Box Functional Modules

| Module Name | Core Features | Technical Characteristics |
| :--- | :--- | :--- |
| User Center | Multi-factor authentication, Organization management(*VNext*), Fine-grained permission control | RBAC+ABAC hybrid model |
| Audit Center | Operation log tracing, Data change tracking, Security compliance reporting | Elasticsearch storage |
| Configuration Center | Multi-environment configuration management, Version control, Dynamic updates | Built-in implementation |
| Order Center | Order management, Status flow, Payment integration | Event-driven architecture |

### IV. Full-Stack Generation Engine

- **Code Feedback**: Automatically generate backend repository and controller code based on frontend operations

- **AI-Assisted Design**:

  - Natural language requirement description → Automatic page prototype generation
  - Screenshot page → Automatic DTO structure inference
  - Voice commands → Real-time modification of table and form configurations

  Imagine this scenario:

  ***"Spirit, add a birthday field to the user table, use a calendar component, and display it as age in the list page"***

  AI assistant completes instantly:

  ✅ Modify DTO model

  ✅ Regenerate frontend

  ✅ Write database migration script

Concept Diagram:

```mermaid
sequenceDiagram
  Developer->>+AI Engine: Input natural language requirements
  AI Engine->>+Code Analyzer: Parse semantic intent
  Code Analyzer->>+Architecture Validator: Check compatibility
  Architecture Validator-->>-AI Engine: Return constraints
  AI Engine->>+Code Generator: Generate candidate solutions
  Code Generator-->>-Developer: Return executable code
```

## Roadmap

### Q2 2025

- Intelligent UI Generation Engine
- UDL Cards Basic Implementation
- CodeSpirit Beta Release
- H5 Generation Engine

### Q3 2025

- UDL Metadata Auto-Generation
- UDL Multi-Platform Rendering Support
- Visual Analysis Module
- Deep Integration of LLM Code Generation Capabilities

### Q4 2025

- UDL Ecosystem Completion (Visual Editor, Template Marketplace)
- Full-Stack Generation Engine
- Multi-Cloud Deployment Support
- Java Support

## Framework Advantages Comparison

### Low-Code Framework Comparison

| Dimension | CodeSpirit | Traditional Low-Code Platforms |
| :--- | :--- | :--- |
| Architecture Openness | Fully Open Code | Black Box Generation |
| Performance | Native Code-Level Performance | Interpretation Execution Performance Loss |
| Customization Capability | Customizable Base Architecture | Limited Extensions |
| Technology Stack | Latest .NET Ecosystem | Proprietary Tech Stack |
| Deployment Mode | Hybrid Cloud/On-Premises | SaaS Bound |

### Typical Development Scenario Comparison

| Traditional Mode | CodeSpirit Mode | Efficiency Improvement |
| :--- | :--- | :--- |
| Frontend-Backend Integration 3 hours | Auto-generation Complete | 8x |
| Form Validation Development 0.5 day | Declarative Configuration 5 minutes | 12x |
| Permission System Integration 2 days | Out-of-box + Policy Extension | ∞ |

## Try Now

https://codespirit-app.xin-lai.com/

Please follow "麦扣聊技术" WeChat Official Account for the latest demo account and password.

## Quick Start

1. Install and start Docker Desktop

2. Set CodeSpirit.AppHost as the startup project

3. Start (Docker images like redis, seq, rabbitmq will be pulled during startup. If unable to pull, please use acceleration methods)

   **Note: Currently based on .NET Aspire 13.0 to simplify orchestration, service discovery, environment variables, and container settings configuration for distributed application development, making it easier to manage during the development phase.**

## Development Documentation

- Github: [xin-lai/CodeSpirit](https://github.com/xin-lai/CodeSpirit) **(Regular updates)**
- Gitee: [magicodes/CodeSpirit](https://gitee.com/magicodes/code-spirit) **(Priority updates)**

### 📘 Core Documentation

1. [🤖 CodeSpirit AI Feature Deep Dive](./Docs/CodeSpirit-AI特色功能详解.md) - Detailed introduction to AI-related core features and innovations ⭐
2. [💎 CodeSpirit Framework Core Highlights](./Docs/CodeSpirit框架核心亮点.md) - Framework technical architecture, core components and best practices ⭐
3. [🏗️ Overall Technical System Overview](./Docs/01-Core-Docs/Overall%20Technical%20System%20Overview.md) - Technical architecture and design philosophy | [中文版](./Docs/01-Core-Docs/总体技术体系说明.md)
4. [🏛️ Backend Architecture](./Docs/01-Core-Docs/后端架构.md) - Backend architecture design description
5. [🏗️ Project Overall Architecture Design](./Docs/01-Core-Docs/Project%20Overall%20Architecture%20Design.md) - Overall project architecture design and principles | [中文版](./Docs/01-Core-Docs/项目整体架构设计.md)
6. [🔧 Development Environment Setup Guide](./Docs/01-Core-Docs/Development%20Environment%20Setup%20Guide.md) - Complete development environment configuration guide | [中文版](./Docs/01-Core-Docs/开发环境搭建指南.md)
7. [💎 CodeSpirit.Core Core Framework](./Docs/01-Core-Docs/CodeSpirit.Core%20Core%20Framework.md) - Core framework components and architecture | [中文版](./Docs/01-Core-Docs/CodeSpirit.Core核心框架.md)
8. [⚠️ CodeSpirit Unified Exception Handling Guide](./Docs/01-Core-Docs/CodeSpirit%20Unified%20Exception%20Handling%20Guide.md) - Enterprise-level exception handling mechanisms and AMIS API compatibility | [中文版](./Docs/01-Core-Docs/CodeSpirit统一异常处理指南.md)
9. [💻 CRUD Development Example](./Docs/01-Core-Docs/CRUD%20Development%20Example.md) - Complete CRUD development example based on QuestionCategory module ⭐ | [中文版](./Docs/01-Core-Docs/CRUD开发示例.md)

### 🎨 UI Generation Engine

9. [🎯 AMIS UI Generation Engine](./Docs/02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md) - Intelligent UI generation core component
10. [📊 AMIS Column Auto-Inference](./Docs/02-UI-Generation/AMIS列自动推断功能说明.md) - Intelligent table column generation detailed explanation
11. [📝 Form Default Values](./Docs/02-UI-Generation/CodeSpirit.Amis表单默认值使用指南.md) - Form default value configuration guide
12. [📋 Form Group Usage Guide](./Docs/02-UI-Generation/CodeSpirit.Amis表单项组使用指南.md) - Form field grouping and organization with visual examples and best practices
13. [📈 Smart Chart Component](./Docs/02-UI-Generation/CodeSpirit.Charts智能图表使用指南.md) - Data visualization solution
14. [⏰ Date Time Column Optimization](./Docs/02-UI-Generation/日期时间列优化功能总结.md) - Intelligent time field processing
15. [🃏 UDL Cards Usage Guide](./Docs/02-UI-Generation/CodeSpirit.UDL-Cards卡片使用指南.md) - Unified card system usage guide and best practices
16. [🛠️ UDL Cards SDK Guide](./Docs/02-UI-Generation/CodeSpirit.UdlCards.SDK使用指南.md) - C# SDK detailed usage guide and API reference
17. [🎨 UDL UI Description Language Design](./Docs/02-UI-Generation/UDL-UI描述语言设计方案.md) - Unified UI description language architecture design
18. [🎯 UDL Cards Detailed Implementation](./Docs/02-UI-Generation/UDL-Cards详细实现方案.md) - Card component library implementation guide
19. [🎮 UDL Cards Simple Implementation](./Docs/02-UI-Generation/UDL-Cards简易实现方案.md) - Quick implementation guide for UDL Cards
20. [🔗 AMIS Sidebar Linkage Usage Guide](./Docs/02-UI-Generation/CodeSpirit.Amis侧边栏联动功能使用指南.md) - Sidebar linkage feature for dynamic filtering and navigation
21. [🤖 AI Smart Form Usage Guide](./Docs/02-UI-Generation/CodeSpirit.Amis.AiForm智能表单使用指南.md) - AI-driven long-running task processing framework
22. [⚙️ OperationAttribute Configuration Guide](./Docs/02-UI-Generation/OperationAttribute-Actions配置使用指南.md) - Operation attribute configuration and action button customization
23. [📦 Enhanced Batch Import Component Guide](./Docs/02-UI-Generation/增强批量导入组件使用指南.md) - Enhanced batch data import functionality

### 🔧 Core Components

24. [🤖 AI Form Intelligent Fill Component Usage Guide](./Docs/03-Core-Components/CodeSpirit.AI表单智能填充组件使用指南.md) - Independent AI form filling component with global and field-trigger modes, zero-code auto endpoint generation, NuGet-ready architecture
25. [🧭 Navigation Component](./Docs/03-Core-Components/CodeSpirit.Navigation导航组件使用指南.md) - Smart navigation system with multi-platform, permission filtering and context awareness
26. [🔗 Aggregator Usage Guide](./Docs/03-Core-Components/CodeSpirit.Aggregator聚合器使用指南.md) - Data aggregation and field replacement
27. [⚙️ Settings Management Component](./Docs/03-Core-Components/CodeSpirit.Settings设置管理组件使用指南.md) - Configuration management solution
28. [🔒 Distributed Lock Usage Guide](./Docs/03-Core-Components/CodeSpirit分布式锁使用指南.md) - Distributed lock implementation and usage
29. [📄 PDF Generation Component](./Docs/03-Core-Components/CodeSpirit.PdfGeneration使用指南.md) - PDF document generation service
30. [🕒 Time Processing Mechanism](./Docs/03-Core-Components/CodeSpirit时间处理机制.md) - Unified time processing solution
31. [🌐 Client IP Service](./Docs/03-Core-Components/ClientIpService使用指南.md) - Client IP acquisition and processing
32. [📋 Audit Component Integration Guide](./Docs/03-Core-Components/CodeSpirit.Audit审计组件集成使用指南.md) - Complete audit system integration and usage
33. [🤖 LLM Audit Component Design](./Docs/03-Core-Components/CodeSpirit.LLM.Audit-LLM审计组件设计方案.md) - LLM call audit tracking design
34. [📊 LLM Audit Usage Guide](./Docs/03-Core-Components/CodeSpirit.LLM.Audit-使用指南.md) - LLM audit component usage methods
35. [⏰ Scheduled Tasks Component README](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks-README.md) - Scheduled tasks component overview
36. [📅 Scheduled Tasks Component Usage Guide](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks定时任务组件使用指南.md) - Detailed scheduled tasks component usage
37. [🏗️ Scheduled Tasks Technical Design](./Docs/03-Core-Components/CodeSpirit.ScheduledTasks技术设计文档.md) - Scheduled tasks component architecture design
38. [🚫 NoAudit Attribute Usage Guide](./Docs/03-Core-Components/NoAuditAttribute-README.md) - Audit exclusion attribute usage instructions
39. [🚀 Unified Startup Framework Usage Guide](./Docs/03-Core-Components/CodeSpirit统一启动框架使用指南.md) - Unified API project startup framework, simplifying project creation and configuration
40. [🏗️ Unified Startup Framework Core Architecture](./Docs/03-Core-Components/CodeSpirit统一启动框架核心架构.md) - Architecture design and implementation principles of the unified startup framework
41. [📦 API Configuration Class Development Guide](./Docs/03-Core-Components/CodeSpirit.API配置类开发指南.md) - Development standards and best practices for API configuration classes
42. [🔌 Middleware Injection Point Usage Guide](./Docs/03-Core-Components/CodeSpirit中间件插入点使用指南.md) - Usage methods and extension mechanisms for middleware injection points
43. [🔄 Unified Startup Framework Migration Guide](./Docs/03-Core-Components/CodeSpirit统一启动框架迁移指南.md) - Detailed guide for migrating existing projects to the unified startup framework
44. [📝 Scrutor Dependency Injection Integration Guide](./Docs/03-Core-Components/Scrutor依赖注入集成指南.md) - Integration and usage methods of Scrutor library in the framework
45. [🖼️ Image Processing Service Integration Guide](./Docs/03-Core-Components/CodeSpirit.ImageProcessingService图片处理服务集成指南.md) - Integration and usage of image processing services
46. [📎 Entity File Reference Event Handler Usage Guide](./Docs/03-Core-Components/CodeSpirit.EntityFileReferenceHandler实体文件引用事件处理器使用指南.md) - Automatic management of entity file references
47. [🏷️ Resource Management Component Usage Guide](./Docs/03-Core-Components/ResourceTagHelper资源管理组件使用指南.md) - Usage methods of resource tag helpers

### 🔐 Identity & Authorization

48. [🔑 IdentityApi Identity Service](./Docs/04-Identity-Auth/CodeSpirit.IdentityApi身份认证服务.md) - Complete identity authentication service architecture
49. [👮 Authorization Component](./Docs/04-Identity-Auth/CodeSpirit.Authorization权限组件详解.md) - Comprehensive permission system with RBAC+ABAC hybrid model
50. [🎫 TokenManager Frontend Authentication Manager](./Docs/04-Identity-Auth/CodeSpirit.TokenManager前端认证管理器使用指南.md) - Frontend authentication management and token handling
51. [⚙️ ISettableCurrentUser Interface Guide](./Docs/04-Identity-Auth/ISettableCurrentUser可设置用户接口使用指南.md) - Settable current user interface for dynamic user context scenarios
52. [👥 Employee and Organization Management Guide](./Docs/04-Identity-Auth/职工管理及组织结构管理功能说明.md) - Employee and organizational structure management features
53. [🏢 Department AI Quick Initialization Guide](./Docs/04-Identity-Auth/部门管理AI快速初始化功能说明.md) - AI-assisted department structure initialization

### 🏢 Multi-Tenancy Architecture

54. [🏗️ Multi-Tenant Component Refactoring Plan](./Docs/05-Multi-Tenancy/CodeSpirit多租户组件整改计划.md) - Multi-tenancy architecture improvement and implementation plan
55. [🎯 TenantResolver Usage Guide](./Docs/05-Multi-Tenancy/CodeSpirit.TenantResolver租户解析器使用指南.md) - Tenant resolution and context management
56. [🔍 DataFilter Usage Guide](./Docs/05-Multi-Tenancy/CodeSpirit.DataFilter数据筛选器使用指南.md) - Data filtering and tenant isolation mechanisms
57. [🗄️ Multi-Tenant Database Context Architecture](./Docs/05-Multi-Tenancy/CodeSpirit 多租户数据库上下文架构.md) - Database architecture design for multi-tenancy
58. [🖥️ Multi-Tenant Login Page Usage Guide](./Docs/05-Multi-Tenancy/多租户登录页面使用指南.md) - Multi-tenant login interface implementation
59. [🎯 Tenant-Aware Event System Design](./Docs/05-Multi-Tenancy/CodeSpirit 租户感知事件系统设计.md) - Comprehensive tenant-aware event bus architecture and implementation

### 🚀 Infrastructure & DevOps

60. [🐰 RabbitMQ Integration Guide](./Docs/06-Infrastructure/RabbitMQ-Aspire-Integration.md) - Message queue integration solution
61. [🔧 RabbitMQ Troubleshooting](./Docs/06-Infrastructure/RabbitMQ故障排除指南.md) - Common problem solutions
62. [🔍 Elasticsearch Migration Summary](./Docs/06-Infrastructure/Elasticsearch-Aspire-Migration-Summary.md) - Search engine integration guide
63. [🌐 CORS Policy Configuration Guide](./Docs/06-Infrastructure/CodeSpirit跨域策略配置指南.md) - CORS cross-origin resource sharing configuration and security policies
64. [📁 File Storage Service Implementation](./Docs/06-Infrastructure/CodeSpirit文件存储服务方案实现.md) - File storage service architecture design and implementation
65. [🌐 API Address Configuration Guide](./Docs/06-Infrastructure/API地址配置指南.md) - API service address configuration instructions
66. [🔧 API Path Prefix Configuration Guide](./Docs/06-Infrastructure/API路径前缀配置指南.md) - API path prefix configuration methods
67. [🚀 Aspire 9.5 Optimization Guide](./Docs/06-Infrastructure/CodeSpirit.AppHost-Aspire9.5优化指南.md) - Aspire 9.5 version optimization practices
68. [🗄️ Aspire Database Integration Implementation Guide](./Docs/06-Infrastructure/CodeSpirit.Aspire数据库集成实现指南.md) - Detailed Aspire database integration implementation
69. [🎯 Aspire Database Integration Unified Solution](./Docs/06-Infrastructure/CodeSpirit.Aspire数据库集成统一方案.md) - Aspire database integration unified architecture
70. [💾 Unified Caching Component Guide](./Docs/06-Infrastructure/CodeSpirit.Caching统一缓存组件指南.md) - Distributed caching component usage instructions
71. [📄 PuppeteerSharp Problem Solving Guide](./Docs/06-Infrastructure/CodeSpirit.PdfGeneration-PuppeteerSharp问题解决指南.md) - PDF generation component common problem solutions
72. [🗄️ Multi-Database DbContext Architecture Guide](./Docs/06-Infrastructure/多数据库DbContext架构使用指南.md) - Multi-database support architecture design

### 🌐 API & Communication

73. [🔗 Universal API Jump Mechanism Usage Guide](./Docs/07-API-Communication/CodeSpirit通用API跳转机制使用指南.md) - Universal API routing and communication mechanisms

### 📊 Project Management

74. [📋 Technical Debt Management](./Docs/08-Project-Management/技术债管理文档.md) - Technical debt tracking and management standards

### 📝 Exam System

75. [📚 Exam System Overview](./Docs/09-Exam-System/README.md) - Complete exam system documentation navigation and quick reference
76. [🏗️ Exam System Technical Architecture](./Docs/09-Exam-System/考试系统完整说明文档.md) - Comprehensive technical architecture, API design and security mechanisms
77. [📋 Exam System Business Functions](./Docs/09-Exam-System/考试系统业务功能清单.md) - Complete business function list with 200+ features across 12 modules

### 📋 Survey System

78. [📊 Survey Module Design Proposal](./Docs/09-Survey-System/问卷调查模块方案设计.md) - Survey system architecture design proposal
79. [📝 Question Type Specific Fields Implementation](./Docs/09-Survey-System/题目类型特定字段实现说明.md) - Question type field implementation details

