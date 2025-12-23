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

### I. AI-Enhanced Features 🤖

CodeSpirit provides a complete AI integration solution covering the full chain from basic LLM calls to intelligent business applications.

**Core Components**:
- **CodeSpirit.LLM**: Unified LLM interface supporting OpenAI, Alibaba Qwen, DeepSeek and other models, providing advanced features like structured task processing, batch processing, and intelligent JSON repair
- **AI Form Intelligent Fill** ⭐: Industry-first! Just one attribute tag `[AiFormFill]` automatically generates AI fill endpoints without writing controller code, boosting development efficiency by 10x+
- **AI Import Wizard**: Intelligent text parsing, batch AI review, visual preview, supporting automatic recognition and correction of multiple question formats
- **LLM Audit Tracking**: Complete AI decision traceability, recording prompts, responses, costs, performance metrics, supporting dual storage with Elasticsearch and GreptimeDB
- **AI Smart Tool System**: Four-stage progressive tool selection (keywords → vector search → LLM classification → LLM selection), intelligent tool recommendation and configuration generation
- **AI Partner System**: Server-side intelligent dialogue platform supporting Function Calling, providing scenario-based AI assistants (exam analyst, question creator, etc.)
- **AI Long-Running Task Handling**: Complete task management and progress tracking, supporting multi-step interface display and real-time polling

**Performance Optimization Strategies**:
- 🚀 Multi-level caching strategy (L1 memory + L2 Redis), intelligent degradation mechanism
- 📦 Request merging and batch processing, reducing API call costs
- 🌊 Streaming response optimization, improving user experience
- ⚡ Intelligent batching and concurrency control, supporting large-scale data processing

See [CodeSpirit AI Feature Deep Dive](./Docs/CodeSpirit-AI特色功能详解.md) for details

### II. Intelligent UI Generation Engine

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
- **UDL Cards Components**: Predefined card template library (student profile card, statistics card, action card, answer card, etc.)
- **Multi-platform Rendering**: Unified configuration, automatically adapting to different rendering engines like AMIS, MAUI
- **Application Scenarios**: Exam monitoring dashboard, exam client, management backend data panels and operation interface auto-generation

### III. Enterprise-Grade Backend Architecture

#### 1. Unified Startup Framework 🚀
Program.cs requires only 2 lines of code to complete API project configuration, automatic service registration based on Scrutor, standardized configuration class pattern, providing 3 precise middleware injection points, automatic database migration.

#### 2. Automatic Dependency Injection 🔍
Zero-configuration batch service registration, automatically identifying lifecycle through marker interfaces (`IScopedDependency`, `ISingletonDependency`), convention over configuration.

#### 3. Multi-Database Support 🗄️
Flexible switching between MySQL and SQL Server, independent migration management (BaseDbContext → MySqlDbContext / SqlServerDbContext), completely decoupled business code, automatic migration application.

#### 4. Event-Driven Architecture 📡
RabbitMQ-based distributed event system, supporting tenant-awareness, asynchronous decoupling, event sourcing, cross-service communication, reliable message delivery guarantee.

#### 5. Core Framework Features
- **Cloud-Native Foundation**: Native k8s support, deep integration with .NET Aspire, native support for Dapr distributed architecture
- **Security System**: Four-layer defense system (Authentication/Authorization/Audit/Encryption)
- **High-Performance Guarantee**: Distributed caching, automatic second-level caching, intelligent query optimization

#### 6. Key Functional Components
Permission system (RBAC+ABAC), ORM extensions (soft delete/audit/multi-tenancy), data filters, audit service, health checks, distributed locks, configuration center, aggregator, PDF generation, time processing, image processing, file storage, etc. - all out-of-the-box

### IV. Out-of-the-Box Functional Modules

- **User Center**: Multi-factor authentication, organization management, fine-grained permission control (RBAC+ABAC hybrid model)
- **Audit Center**: Operation log tracing, data change tracking, security compliance reporting (Elasticsearch storage)
- **Configuration Center**: Multi-environment configuration management, version control, dynamic updates (built-in implementation)
- **Exam System**: Complete exam management, question bank management, score analysis, AI question generation (complete case)
- **Survey System**: Survey design, data collection, statistical analysis

## Core Advantages

- ✅ **Fully Open Code**: Generated code is fully controllable, supporting base architecture customization
- ✅ **Native Performance**: No interpretation execution overhead, native code-level performance
- ✅ **Ultimate Efficiency**: 8x improvement in frontend-backend integration, 12x improvement in form development, permission system out-of-the-box
- ✅ **Cloud-Native Deployment**: Hybrid cloud/on-premises deployment, native Kubernetes support

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
10. [🎯 AMIS UI Generation Engine](./Docs/02-UI-Generation/CodeSpirit.Amis智能界面生成引擎.md) - Intelligent UI generation core component
11. [👮 Authorization Component](./Docs/04-Identity-Auth/CodeSpirit.Authorization权限组件详解.md) - Comprehensive permission system with RBAC+ABAC hybrid model
12. [🚀 Unified Startup Framework Usage Guide](./Docs/03-Core-Components/CodeSpirit统一启动框架使用指南.md) - Unified API project startup framework, simplifying project creation and configuration
13. [📚 Exam System Technical Architecture](./Docs/09-Exam-System/考试系统完整说明文档.md) - Complete exam system case with 200+ features covering question bank, exams, scores, AI question generation

### 📚 Complete Documentation Navigation

Detailed documentation has been organized by functional modules, containing 80+ technical documents:

- **Core Framework**: Technical architecture, unified exception handling, multi-database support, event-driven, etc.
- **UI Generation**: AMIS engine, UDL Cards, smart charts, form components, etc.
- **Core Components**: AI form fill, navigation, audit, caching, scheduled tasks, PDF generation, etc.
- **Identity & Authorization**: JWT authentication, RBAC/ABAC permissions, organization management, etc.
- **Multi-Tenancy**: Tenant resolver, data isolation, tenant-aware event system, etc.
- **Infrastructure**: RabbitMQ, Elasticsearch, Aspire integration, database migration, etc.

Complete documentation list available in subdirectories:
- [01-Core-Docs](./Docs/01-Core-Docs/) - Core framework documentation
- [02-UI-Generation](./Docs/02-UI-Generation/) - UI generation documentation
- [03-Core-Components](./Docs/03-Core-Components/) - Core components documentation
- [04-Identity-Auth](./Docs/04-Identity-Auth/) - Identity & authorization documentation
- [05-Multi-Tenancy](./Docs/05-Multi-Tenancy/) - Multi-tenancy documentation
- [06-Infrastructure](./Docs/06-Infrastructure/) - Infrastructure documentation

## 🚀 Sponsorship & Technical Support

### Open Source Project Support

CodeSpirit is a fully open-source project. We are committed to providing high-quality low-code development frameworks for the developer community. If this project is helpful to you, please consider giving us a ⭐ Star, or support the continuous development of the project through the following methods:

### 💰 Sponsorship Methods

**Personal Sponsorship**
- Support continuous project updates and maintenance
- Promote new feature development and performance optimization
- Help build a better developer community

**Sponsorship Benefits**
- 💬 **Sponsor $100**: Get one one-on-one communication and guidance opportunity to answer technical questions and provide architecture advice
- 🎯 **Sponsor $1000**: Get free commercial license for commercial project development and deployment

**PayPal Sponsorship**: [https://paypal.me/magicodes](https://paypal.me/magicodes)

**Thank you to every supporter and contributor! Your support is the driving force behind our continuous progress!** 🙏

