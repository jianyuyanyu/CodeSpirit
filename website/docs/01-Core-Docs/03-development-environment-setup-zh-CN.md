# CodeSpirit 开发环境搭建及启动指南

## 概述

本指南将帮助您快速搭建CodeSpirit（码灵）低代码框架的开发环境。CodeSpirit基于 .NET 10 和 Aspire 13.0 构建，通过简单的几个步骤即可启动完整的开发环境。

**最后更新**: 2025年12月22日  
**框架版本**: v2.0.0

![image-20251218200855805](/img/image-20251218200855805.png)

## 快速开始

### 前置要求

- **操作系统**: Windows 10/11, macOS 12+, 或 Linux (Ubuntu 20.04+)
- **CPU**: Intel i5 或 AMD Ryzen 5 及以上（推荐i7/Ryzen 7）
- **内存**: 16GB RAM（推荐32GB）
- **存储**: 至少20GB可用空间（SSD推荐）

> **注意**: CodeSpirit默认使用GreptimeDB进行审计日志存储和搜索。Elasticsearch为可选组件，如需使用请参考相关配置文档。

### 1. 安装 .NET 10 SDK

#### Windows
```powershell
# 使用 winget 安装
winget install Microsoft.DotNet.SDK.10

# 或下载安装包
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### macOS
```bash
# 使用 Homebrew
brew install --cask dotnet-sdk

# 或下载安装包
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### Linux (Ubuntu)
```bash
# 添加微软包源
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

#### 验证安装
```bash
dotnet --version
# 应显示 10.x.x
```

### 2. 安装开发工具

#### Visual Studio 2026 (推荐)
- 下载地址: https://visualstudio.microsoft.com/vs/
- 选择工作负载：**ASP.NET 和 Web 开发**

#### 或者 Visual Studio Code
```bash
# Windows
winget install Microsoft.VisualStudioCode

# macOS
brew install --cask visual-studio-code

# Linux
sudo snap install code --classic
```

VS Code必需扩展：
```bash
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
```

### 3. 安装Docker Desktop

- 下载地址: https://www.docker.com/products/docker-desktop
- 安装后启动Docker Desktop

验证安装：
```bash
docker --version
```

## 项目启动

### 1. 克隆项目

```bash
git clone https://gitee.com/magicodes/code-spirit.git
cd code-spirit
```

### 2. 启动基础服务

CodeSpirit使用Aspire自动管理所有依赖服务，无需手动启动Docker容器：

```bash
# Aspire会自动启动以下服务：
# - MySQL/SQL Server (根据配置选择，端口: 3306/1433)
# - Redis (端口: 6380)
# - RabbitMQ (端口: 5672, 管理界面: 15672)
# - GreptimeDB (端口: 4000/4001)
# - Seq日志服务 (端口: 5341)
```

> **服务说明**:
>
> - **MySQL/SQL Server**: 主数据库存储（根据DatabaseType配置选择）
> - **Redis**: 缓存和会话存储（端口: 6380）
> - **RabbitMQ**: 消息队列服务（管理界面端口: 15672）
> - **GreptimeDB**: 时序数据库，用于审计日志存储（HTTP端口: 4000, gRPC端口: 4001）
> - **Seq**: 结构化日志服务（端口: 5341）

### 3. 运行项目

#### 使用.NET Aspire（推荐）

```bash
# 进入AppHost项目目录
cd Src/CodeSpirit.AppHost

# 运行Aspire应用
dotnet run
```

如果是正常启动，将看到以下缤纷的控制台输出：

![image-20251218200658849](/img/image-20251218200658849.png)

启动后访问：

- **Aspire Dashboard**: `http://localhost:17109`（自动打开）
- **Web应用**: `https://localhost:7120`（启动后显示具体端口）

> **注意**: 

> 1. 实际端口号可能因系统配置而异，请查看Aspire Dashboard获取准确的端口信息。
> 1. 如何登录页没有正常呈现，请按照下面的必填参数配置进行配置。

#### 或者使用Visual Studio

1. 打开 `CodeSpirit.sln`

2. 设置 `CodeSpirit.AppHost` 为启动项目

3. 按 F5 运行

   ![image-20251218194717769](/img/image-20251218194717769.png)

   注意，需确保以下服务均正常启动：

   ![image-20251218195227522](/img/image-20251218195227522.png)

## 项目结构

CodeSpirit采用Clean Architecture设计，项目结构如下：

```
CodeSpirit/
├── Src/
│   ├── ApiServices/                     # API服务（解决方案文件夹）
│   │   ├── CodeSpirit.IdentityApi/      # 身份认证API
│   │   ├── CodeSpirit.ExamApi/          # 考试系统API  
│   │   ├── CodeSpirit.MessagingApi/     # 消息服务API
│   │   ├── CodeSpirit.ConfigCenter/    # 配置中心API
│   │   ├── CodeSpirit.FileStorageApi/  # 文件存储API
│   │   ├── CodeSpirit.SurveyApi/        # 问卷调查API
│   │   ├── CodeSpirit.ApprovalApi/      # 审批工作流API
│   │   ├── CodeSpirit.PathfinderApi/    # AI目标管理API
│   │   └── CodeSpirit.AiCardsApi/       # AI卡片API
│   ├── Components/                     # 组件库
│   │   ├── CodeSpirit.Aggregator/       # 聚合器组件
│   │   ├── CodeSpirit.AiFormFill/       # AI表单智能填充组件
│   │   ├── CodeSpirit.Amis/             # UI生成引擎
│   │   ├── CodeSpirit.Authorization/    # 权限组件
│   │   ├── CodeSpirit.Audit/            # 审计组件
│   │   ├── CodeSpirit.Caching/          # 分布式缓存组件
│   │   ├── CodeSpirit.Charts/           # 智能图表组件
│   │   ├── CodeSpirit.ConfigCenter.Client/ # 配置中心客户端
│   │   ├── CodeSpirit.LLM/              # 大语言模型组件
│   │   ├── CodeSpirit.Messaging/        # 消息队列组件
│   │   ├── CodeSpirit.MultiTenant/      # 多租户组件
│   │   ├── CodeSpirit.Navigation/       # 导航组件
│   │   ├── CodeSpirit.PdfGeneration/    # PDF生成组件
│   │   ├── CodeSpirit.ScheduledTasks/   # 定时任务组件
│   │   ├── CodeSpirit.Settings/         # 设置管理组件
│   │   ├── CodeSpirit.Shared/           # 组件共享库
│   │   └── CodeSpirit.UdlCards/         # UDL卡片组件
│   ├── CodeSpirit.AppHost/              # Aspire应用宿主
│   ├── CodeSpirit.Core/                 # 核心定义
│   ├── CodeSpirit.ServiceDefaults/      # 服务默认配置
│   ├── CodeSpirit.Shared/               # 全局共享库
│   └── CodeSpirit.Web/                  # Web前端项目
├── Tests/                               # 测试项目
├── Docs/                                # 项目文档
├── k8s/                                 # Kubernetes部署文件
└── CodeSpirit.sln                       # 解决方案文件
```

## 默认配置

项目使用以下默认配置，由.NET Aspire自动管理：

### 数据库连接
- **数据库类型**: 支持MySQL和SQL Server两种数据库（通过`DatabaseType`配置选择）

- **MySQL**: 端口3306，由Aspire自动配置

  可以从资源面板访问管理UI（phpmyadmin）：

  ![image-20251218195543876](/img/image-20251218195543876.png)

  ![image-20251218195454570](/img/image-20251218195454570.png)

- **SQL Server**: 端口1433，由Aspire自动配置

- **数据库**: 自动创建和迁移

- **连接字符串**: 由Aspire自动管理

### 缓存和消息队列
- **Redis**: `localhost:6380`（具体见管理UI）

- **RabbitMQ**: `localhost:5672` (管理界面: http://localhost:15672, 用户名/密码: admin/Password123)

  ![image-20251218195618899](/img/image-20251218195618899.png)

### 其他服务端口
- **GreptimeDB**: 
  
  - HTTP端口: `localhost:4000`
  - gRPC端口: `localhost:4001`
  - 健康检查: http://localhost:4000/health
  
- **Seq日志服务**: `localhost:5341`（具体端口见资源面板）

  ![image-20251218195323985](/img/image-20251218195323985.png)

- **Redis Commander**: 通过Aspire Dashboard访问

  ![image-20251218195358167](/img/image-20251218195358167.png)

## 必填参数配置

CodeSpirit 使用 .NET Aspire 的参数管理机制来配置敏感信息和环境相关参数。在首次启动前，您需要配置以下必填参数。提示UI如下：
![image-20251222221113504](/img/image-20251222221113504.png)

![image-20251222221245897](/img/image-20251222221245897.png)

### 参数配置方式

Aspire 支持两种参数配置方式，配置系统会按以下优先级读取（高优先级会覆盖低优先级）：

1. **User Secrets**（开发环境推荐，避免提交敏感信息到代码库）
2. **appsettings.json**（开发环境备选方案）

> **提示**: 对于敏感信息（如 API 密钥），强烈推荐使用 User Secrets，避免将密钥提交到代码库。

### 必填参数列表

#### LLM 配置参数

以下参数用于配置通用 LLM 服务（如 AI 卡片、智能审批等功能）：

| 参数名称 | 说明 | 是否必填 | 默认值 |
|---------|------|---------|--------|
| `llm-ApiKey` | LLM API密钥 | ✅ **必填** | 无 |
| `llm-ApiBaseUrl` | LLM API基础地址 | 可选 | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| `llm-ModelName` | LLM模型名称 | 可选 | `qwen-flash` |
| `llm-TimeoutSeconds` | 请求超时时间（秒） | 可选 | `120` |
| `llm-MaxTokens` | 最大Token数 | 可选 | `2048` |
| `llm-UseProxy` | 是否使用代理 | 可选 | `false` |
| `llm-ProxyAddress` | 代理地址 | 可选 | 空字符串 |

#### AI表单填充 LLM 配置参数

以下参数用于配置 AI 表单智能填充功能：

| 参数名称 | 说明 | 是否必填 | 默认值 |
|---------|------|---------|--------|
| `ai-form-fill-llm-ApiKey` | AI表单填充LLM API密钥 | ✅ **必填** | 无 |
| `ai-form-fill-llm-ApiBaseUrl` | API基础地址 | 可选 | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| `ai-form-fill-llm-ModelName` | 模型名称 | 可选 | `qwen3-max-preview` |
| `ai-form-fill-llm-DisableThinking` | 禁用思考模式 | 可选 | `true` |
| `ai-form-fill-llm-ResponseFormatType` | 响应格式类型 | 可选 | `json_object` |
| `ai-form-fill-llm-Temperature` | 温度参数 | 可选 | `0.1` |
| `ai-form-fill-llm-TopP` | TopP参数 | 可选 | `0.9` |
| `ai-form-fill-llm-EnableStreaming` | 启用流式响应 | 可选 | `true` |

#### 其他可选参数

以下参数已有默认值，通常无需修改：

| 参数名称 | 说明 | 默认值 |
|---------|------|--------|
| `jwt-SecretKey` | JWT密钥 | `ECBF8FA013844D77AE041A6800D7FF8F` |
| `jwt-Issuer` | JWT颁发者 | `codespirit.com` |
| `jwt-Audience` | JWT受众 | `CodeSpirit` |
| `mysql-password` | MySQL密码 | `Password123` |
| `sqlserver-password` | SQL Server密码 | `P@ssword123456` |
| `rabbitmq-username` | RabbitMQ用户名 | `admin` |
| `rabbitmq-password` | RabbitMQ密码 | `Password123` |

### 配置方法

#### 方法一：使用 User Secrets（推荐开发环境）

使用 .NET User Secrets 可以安全地存储敏感信息，无需担心提交到代码库：

```bash
# 进入 AppHost 项目目录
cd Src/CodeSpirit.AppHost

# 初始化 User Secrets（如果尚未初始化）
dotnet user-secrets init

# 设置 LLM API 密钥
dotnet user-secrets set "llm-ApiKey" "your-llm-api-key-here"

# 设置 AI 表单填充 LLM API 密钥
dotnet user-secrets set "ai-form-fill-llm-ApiKey" "your-ai-form-fill-llm-api-key-here"

# 清除所有密钥
# dotnet user-secrets clear
```

#### 方法二：使用 appsettings.json（开发环境备选）

编辑 `Src/CodeSpirit.AppHost/appsettings.json` 文件，添加参数配置：

```json
{
  "DatabaseType": "MySql",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  },
  "llm-ApiKey": "your-llm-api-key-here",
  "ai-form-fill-llm-ApiKey": "your-ai-form-fill-llm-api-key-here"
}
```

> **⚠️ 重要提示**: 
> - 如果使用 `appsettings.json` 配置敏感信息，请确保该文件已添加到 `.gitignore` 中
> - 或者创建 `appsettings.Local.json` 文件（该文件默认已在 `.gitignore` 中），避免将 API 密钥提交到代码库
> - **强烈推荐使用 User Secrets 方式**，更安全且不会误提交

### 获取 API 密钥

#### 阿里云通义千问（DashScope）

开发阶段免费额度完全够用：
1. 访问 [阿里云 DashScope 控制台](https://dashscope.console.aliyun.com/)
2. 注册/登录账号
3. 创建 API Key
4. 将 API Key 配置到上述参数中

> 💡 **推荐阅读**：[阿里云通义千问免费体验指南](./阿里云通义千问免费体验指南.md) - 详细的注册指南、配置教程和成本分析，帮助您零成本体验 CodeSpirit 的强大 AI 能力！

#### OpenAI（如使用 OpenAI 兼容接口）

如果使用 OpenAI 兼容的 API 服务，需要修改以下参数：

使用 User Secrets 配置：
```bash
dotnet user-secrets set "llm-ApiBaseUrl" "https://api.openai.com/v1"
dotnet user-secrets set "llm-ModelName" "gpt-4"
dotnet user-secrets set "llm-ApiKey" "your-openai-api-key-here"
```

或使用 appsettings.json 配置：
```json
{
  "llm-ApiBaseUrl": "https://api.openai.com/v1",
  "llm-ModelName": "gpt-4",
  "llm-ApiKey": "your-openai-api-key-here"
}
```

### 验证参数配置

启动项目后，如果参数配置不正确，您会在控制台或 Aspire Dashboard 中看到相关错误信息。确保以下服务能够正常启动：

- ✅ ConfigCenter（配置中心）- 需要 LLM 参数
- ✅ Web 前端 - 需要 AI 表单填充 LLM 参数

> **提示**: 如果暂时不需要使用 AI 功能，可以设置一个占位符值，但某些依赖 AI 的功能将无法正常工作。

## 开发工具配置

### Visual Studio Code

创建 `.vscode/launch.json`：

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch CodeSpirit",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/Src/CodeSpirit.AppHost/bin/Debug/net10.0/CodeSpirit.AppHost.dll",
      "cwd": "${workspaceFolder}/Src/CodeSpirit.AppHost",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

创建 `.vscode/tasks.json`：

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build", "${workspaceFolder}/CodeSpirit.sln"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

## 验证安装

### 1. 检查服务状态

访问Aspire Dashboard (http://localhost:17109) 确认所有服务正常运行：

- ✅ CodeSpirit.Web (Web前端)
- ✅ CodeSpirit.IdentityApi (身份认证)
- ✅ CodeSpirit.ConfigCenter (配置中心)
- ✅ CodeSpirit.MessagingApi (消息服务)
- ✅ CodeSpirit.ExamApi (考试系统)
- ✅ CodeSpirit.FileStorageApi (文件存储)
- ✅ CodeSpirit.SurveyApi (问卷调查)
- ✅ CodeSpirit.ApprovalApi (审批流程)
- ✅ CodeSpirit.PathfinderApi (AI目标管理)
- ✅ MySQL/SQL Server (数据库，根据配置)
- ✅ Redis (缓存)
- ✅ RabbitMQ (消息队列)
- ✅ GreptimeDB (时序数据库)
- ✅ Seq (日志服务)

### 2. 检查错误

打开结构化日志面板，检查是否存在错误：

![image-20251218195748771](/img/image-20251218195748771.png)

### 3. 访问Web界面

系统平台：https://localhost:7120

账号：systemadmin 

密码：CodeSpirit@2025

![image-20251218195825029](/img/image-20251218195825029.png)

登录后可以看到系统平台后台管理UI：

![image-20251218200130419](/img/image-20251218200130419.png)

租户平台（默认租户）：https://localhost:7120/default/login

账号：admin

密码：123@Admin

![image-20251218195939141](/img/image-20251218195939141.png)

![image-20251218200156112](/img/image-20251218200156112.png)

## 常见问题

### 无法打开网页

一般是以下情况导致：

1. 镜像无法拉取，一般在docker面板或Aspire管理面板的日志中可以看到。建议配置镜像源或科学上网。

2. 关键服务故障，比如Web服务出现故障。

3. 端口冲突或网络错误，具体可以看启动控制台错误：

   ![image-20251218200528327](/img/image-20251218200528327.png)

### 端口冲突

如果遇到端口冲突，修改 `Src/CodeSpirit.AppHost/Program.cs` 中的端口配置。

### Docker服务启动失败
由于项目使用.NET Aspire管理服务，如果遇到服务启动问题：

```bash
# 重启Aspire应用
cd Src/CodeSpirit.AppHost
dotnet run --force

# 查看Aspire Dashboard中的服务状态
# 访问 http://localhost:17109
```

### GreptimeDB启动失败
```bash
# 在Aspire Dashboard中查看GreptimeDB状态
# 如果内存不足，可以在Program.cs中调整GreptimeDB配置

# 检查系统资源使用情况
# GreptimeDB需要至少512MB内存
```

### SSL证书问题
```bash
# 信任开发证书
dotnet dev-certs https --trust
```

### 数据库连接问题
```bash
# 检查数据库容器状态（根据配置的数据库类型）
docker ps | grep mysql    # MySQL
docker ps | grep sqlserver # SQL Server

# 重启数据库容器
docker restart mysql      # MySQL
docker restart sqlserver  # SQL Server

# 或在Aspire Dashboard中查看数据库状态和连接信息
```

### 内存不足问题
如果系统内存不足，可以：
1. 关闭不必要的应用程序
2. 调整GreptimeDB内存设置（在Program.cs中）
3. 考虑升级系统内存到推荐配置（16GB推荐，32GB更佳）

### LLM API 密钥未配置

如果启动时遇到以下错误或服务无法正常启动：

- ConfigCenter 服务启动失败
- Web 前端无法访问 AI 功能
- 控制台提示缺少 LLM 配置参数

**解决方案**：

1. **检查参数是否已配置**：
   ```bash
   # 查看 User Secrets（如果使用）
   cd Src/CodeSpirit.AppHost
   dotnet user-secrets list
   ```

2. **配置缺失的参数**：
   - 参考 [必填参数配置](#必填参数配置) 章节
   - 确保至少配置了 `llm-ApiKey` 和 `ai-form-fill-llm-ApiKey` 两个必填参数

3. **验证配置**：
   - 重启应用后，检查 Aspire Dashboard 中的服务状态
   - 查看服务日志确认参数是否正确加载

> **提示**: 如果暂时不需要使用 AI 功能，可以设置占位符值（如 `placeholder-key`），但相关 AI 功能将无法正常工作。

## 开发模式

### 热重载开发

```bash
# 启用热重载
cd Src/CodeSpirit.AppHost
dotnet watch run
```

### 调试模式

在Visual Studio或VS Code中设置断点，按F5启动调试。

## 生产环境部署

### 使用Kubernetes部署

项目提供了完整的Kubernetes部署文件：

```bash
# 部署到Kubernetes集群
kubectl apply -f k8s/

# 查看部署状态
kubectl get pods -n code-spirit-release
```

### 使用Docker部署

```bash
# 构建所有服务的Docker镜像
dotnet publish CodeSpirit.sln -c Release

# 使用项目提供的Dockerfile构建镜像
docker build -f Src/CodeSpirit.Web/Dockerfile -t codespirit-web:latest .
docker build -f Src/CodeSpirit.IdentityApi/Dockerfile -t codespirit-identity:latest .
```

### 配置管理

生产环境配置通过以下方式管理：
- **Kubernetes ConfigMap**: 存储应用配置
- **Kubernetes Secret**: 存储敏感信息
- **配置中心**: 动态配置管理

## 下一步

环境搭建完成后，您可以：

1. 📖 阅读 [项目整体架构设计](./项目整体架构设计.md)
2. 🔧 了解 [CodeSpirit.Core核心框架](./CodeSpirit.Core核心框架.md)
3. 📋 查看 [总体技术体系说明](./总体技术体系说明.md)
4. 🔐 学习 [统一异常处理指南](./CodeSpirit统一异常处理指南.md)
5. 💻 参考 [CRUD开发示例](./CRUD开发示例.md) 开始开发

## 获取帮助

如果遇到问题，请参考：
- [GitHub Issues](https://github.com/your-org/code-spirit/issues)
- [项目Wiki](https://github.com/your-org/code-spirit/wiki)
- [讨论区](https://github.com/your-org/code-spirit/discussions)

祝您开发愉快！🚀