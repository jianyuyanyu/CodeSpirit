# CodeSpirit Development Environment Setup Guide

## Overview

This guide will help you quickly set up the development environment for CodeSpirit (码灵), a low-code development framework. CodeSpirit is built on .NET 10 and Aspire 13.0, and you can start the complete development environment with just a few simple steps.

**Last Updated**: December 22, 2025  
**Framework Version**: v2.0.0

![image-20251218200855805](/img/image-20251218200855805.png)

## Quick Start

### Prerequisites

- **Operating System**: Windows 10/11, macOS 12+, or Linux (Ubuntu 20.04+)
- **CPU**: Intel i5 or AMD Ryzen 5 and above (i7/Ryzen 7 recommended)
- **Memory**: 16GB RAM (32GB recommended)
- **Storage**: At least 20GB free space (SSD recommended)

> **Note**: CodeSpirit uses GreptimeDB by default for audit log storage and search. Elasticsearch is an optional component. Please refer to the relevant configuration documentation if needed.

### 1. Install .NET 10 SDK

#### Windows
```powershell
# Install using winget
winget install Microsoft.DotNet.SDK.10

# Or download installer
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### macOS
```bash
# Using Homebrew
brew install --cask dotnet-sdk

# Or download installer
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### Linux (Ubuntu)
```bash
# Add Microsoft package source
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

#### Verify Installation
```bash
dotnet --version
# Should display 10.x.x
```

### 2. Install Development Tools

#### Visual Studio 2026 (Recommended)
- Download: https://visualstudio.microsoft.com/vs/
- Select workload: **ASP.NET and Web Development**

#### Or Visual Studio Code
```bash
# Windows
winget install Microsoft.VisualStudioCode

# macOS
brew install --cask visual-studio-code

# Linux
sudo snap install code --classic
```

Required VS Code extensions:
```bash
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
```

### 3. Install Docker Desktop

- Download: https://www.docker.com/products/docker-desktop
- Start Docker Desktop after installation

Verify installation:
```bash
docker --version
```

## Project Startup

### 1. Clone Project

```bash
git clone https://gitee.com/magicodes/code-spirit.git
cd code-spirit
```

### 2. Start Base Services

CodeSpirit uses Aspire to automatically manage all dependent services, no need to manually start Docker containers:

```bash
# Aspire will automatically start the following services:
# - MySQL/SQL Server (selected based on configuration, ports: 3306/1433)
# - Redis (port: 6380)
# - RabbitMQ (port: 5672, management interface: 15672)
# - GreptimeDB (ports: 4000/4001)
# - Seq logging service (port: 5341)
```

> **Service Description**:
> - **MySQL/SQL Server**: Main database storage (selected based on DatabaseType configuration)
> - **Redis**: Cache and session storage (port: 6380)
> - **RabbitMQ**: Message queue service (management interface port: 15672)
> - **GreptimeDB**: Time-series database for audit log storage (HTTP port: 4000, gRPC port: 4001)
> - **Seq**: Structured logging service (port: 5341)

### 3. Run Project

#### Using .NET Aspire (Recommended)

```bash
# Navigate to AppHost project directory
cd Src/CodeSpirit.AppHost

# Run Aspire application
dotnet run
```

If startup is successful, you will see colorful console output like this:

![image-20251218200658849](/img/image-20251218200658849.png)

After startup, access:
- **Aspire Dashboard**: http://localhost:17109 (opens automatically)
- **Web Application**: https://localhost:7120 (specific port displayed after startup)

> **Note**: 
>
> 1. Actual port numbers may vary based on system configuration. Please check the Aspire Dashboard for accurate port information.
> 2. If the login page is not displayed properly, please configure according to the Required Parameters Configuration section below.

#### Or Using Visual Studio

1. Open `CodeSpirit.sln`

2. Set `CodeSpirit.AppHost` as startup project

3. Press F5 to run

   ![image-20251218194717769](/img/image-20251218194717769.png)

   Note: Ensure all the following services start normally:

   ![image-20251218195227522](/img/image-20251218195227522.png)

## Project Structure

CodeSpirit adopts Clean Architecture design with the following project structure:

```
CodeSpirit/
├── Src/
│   ├── ApiServices/                     # API Services (Solution Folder)
│   │   ├── CodeSpirit.IdentityApi/      # Identity Authentication API
│   │   ├── CodeSpirit.ExamApi/          # Exam System API  
│   │   ├── CodeSpirit.MessagingApi/     # Messaging Service API
│   │   ├── CodeSpirit.ConfigCenter/    # Config Center API
│   │   ├── CodeSpirit.FileStorageApi/  # File Storage API
│   │   ├── CodeSpirit.SurveyApi/        # Survey API
│   │   ├── CodeSpirit.ApprovalApi/      # Approval Workflow API
│   │   ├── CodeSpirit.PathfinderApi/    # AI Goal Management API
│   │   └── CodeSpirit.AiCardsApi/       # AI Cards API
│   ├── Components/                     # Component Library
│   │   ├── CodeSpirit.Aggregator/       # Aggregator Component
│   │   ├── CodeSpirit.AiFormFill/       # AI Form Smart Fill Component
│   │   ├── CodeSpirit.Amis/             # UI Generation Engine
│   │   ├── CodeSpirit.Authorization/    # Authorization Component
│   │   ├── CodeSpirit.Audit/            # Audit Component
│   │   ├── CodeSpirit.Caching/          # Distributed Cache Component
│   │   ├── CodeSpirit.Charts/           # Smart Charts Component
│   │   ├── CodeSpirit.ConfigCenter.Client/ # Config Center Client
│   │   ├── CodeSpirit.LLM/              # Large Language Model Component
│   │   ├── CodeSpirit.Messaging/        # Message Queue Component
│   │   ├── CodeSpirit.MultiTenant/      # Multi-Tenant Component
│   │   ├── CodeSpirit.Navigation/       # Navigation Component
│   │   ├── CodeSpirit.PdfGeneration/    # PDF Generation Component
│   │   ├── CodeSpirit.ScheduledTasks/   # Scheduled Tasks Component
│   │   ├── CodeSpirit.Settings/         # Settings Management Component
│   │   ├── CodeSpirit.Shared/           # Component Shared Library
│   │   └── CodeSpirit.UdlCards/         # UDL Cards Component
│   ├── CodeSpirit.AppHost/              # Aspire Application Host
│   ├── CodeSpirit.Core/                 # Core Framework Definitions
│   ├── CodeSpirit.ServiceDefaults/      # Service Default Configuration
│   ├── CodeSpirit.Shared/               # Global Shared Library
│   └── CodeSpirit.Web/                  # Web Frontend Project
├── Tests/                               # Test Projects
├── Docs/                                # Project Documentation
├── k8s/                                 # Kubernetes Deployment Files
└── CodeSpirit.sln                       # Solution File
```

## Default Configuration

The project uses the following default configurations, automatically managed by .NET Aspire:

### Database Connections
- **Database Type**: Supports both MySQL and SQL Server (selected via `DatabaseType` configuration)

- **MySQL**: Port 3306, automatically configured by Aspire

  You can access the management UI (phpmyadmin) from the resource panel:

  ![image-20251218195543876](/img/image-20251218195543876.png)

  ![image-20251218195454570](/img/image-20251218195454570.png)

- **SQL Server**: Port 1433, automatically configured by Aspire

- **Database**: Automatically created and migrated

- **Connection String**: Automatically managed by Aspire

### Cache and Message Queue
- **Redis**: `localhost:6380` (see management UI for specific port)

- **RabbitMQ**: `localhost:5672` (Management interface: http://localhost:15672, username/password: admin/Password123)

  ![image-20251218195618899](/img/image-20251218195618899.png)

### Other Service Ports
- **GreptimeDB**: 
  
  - HTTP port: `localhost:4000`
  - gRPC port: `localhost:4001`
  - Health check: http://localhost:4000/health
  
- **Seq Logging Service**: `localhost:5341` (see resource panel for specific port)

  ![image-20251218195323985](/img/image-20251218195323985.png)

- **Redis Commander**: Access via Aspire Dashboard

  ![image-20251218195358167](/img/image-20251218195358167.png)

## Required Parameters Configuration

CodeSpirit uses .NET Aspire's parameter management mechanism to configure sensitive information and environment-related parameters. Before the first startup, you need to configure the following required parameters. The prompt UI is shown below:

![image-20251222221113504](/img/image-20251222221113504.png)

![image-20251222221245897](/img/image-20251222221245897.png)

### Parameter Configuration Methods

Aspire supports two parameter configuration methods. The configuration system reads them in the following priority order (higher priority overrides lower priority):

1. **User Secrets** (Recommended for development environment, avoids committing sensitive information to code repository)
2. **appsettings.json** (Alternative for development environment)

> **Tip**: For sensitive information (such as API keys), it is strongly recommended to use User Secrets to avoid committing keys to the code repository.

### Required Parameters List

#### LLM Configuration Parameters

The following parameters are used to configure general LLM services (such as AI cards, intelligent approval, etc.):

| Parameter Name | Description | Required | Default Value |
|---------------|-------------|----------|---------------|
| `llm-ApiKey` | LLM API Key | ✅ **Required** | None |
| `llm-ApiBaseUrl` | LLM API Base URL | Optional | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| `llm-ModelName` | LLM Model Name | Optional | `qwen-flash` |
| `llm-TimeoutSeconds` | Request Timeout (seconds) | Optional | `120` |
| `llm-MaxTokens` | Maximum Tokens | Optional | `2048` |
| `llm-UseProxy` | Use Proxy | Optional | `false` |
| `llm-ProxyAddress` | Proxy Address | Optional | Empty string |

#### AI Form Fill LLM Configuration Parameters

The following parameters are used to configure AI form intelligent fill functionality:

| Parameter Name | Description | Required | Default Value |
|---------------|-------------|----------|---------------|
| `ai-form-fill-llm-ApiKey` | AI Form Fill LLM API Key | ✅ **Required** | None |
| `ai-form-fill-llm-ApiBaseUrl` | API Base URL | Optional | `https://dashscope.aliyuncs.com/compatible-mode/v1` |
| `ai-form-fill-llm-ModelName` | Model Name | Optional | `qwen3-max-preview` |
| `ai-form-fill-llm-DisableThinking` | Disable Thinking Mode | Optional | `true` |
| `ai-form-fill-llm-ResponseFormatType` | Response Format Type | Optional | `json_object` |
| `ai-form-fill-llm-Temperature` | Temperature Parameter | Optional | `0.1` |
| `ai-form-fill-llm-TopP` | TopP Parameter | Optional | `0.9` |
| `ai-form-fill-llm-EnableStreaming` | Enable Streaming Response | Optional | `true` |

#### Other Optional Parameters

The following parameters have default values and usually do not need to be modified:

| Parameter Name | Description | Default Value |
|---------------|-------------|---------------|
| `jwt-SecretKey` | JWT Secret Key | `ECBF8FA013844D77AE041A6800D7FF8F` |
| `jwt-Issuer` | JWT Issuer | `codespirit.com` |
| `jwt-Audience` | JWT Audience | `CodeSpirit` |
| `mysql-password` | MySQL Password | `Password123` |
| `sqlserver-password` | SQL Server Password | `P@ssword123456` |
| `rabbitmq-username` | RabbitMQ Username | `admin` |
| `rabbitmq-password` | RabbitMQ Password | `Password123` |

### Configuration Methods

#### Method 1: Using User Secrets (Recommended for Development Environment)

Use .NET User Secrets to securely store sensitive information without worrying about committing to the code repository:

```bash
# Navigate to AppHost project directory
cd Src/CodeSpirit.AppHost

# Initialize User Secrets (if not already initialized)
dotnet user-secrets init

# Set LLM API Key
dotnet user-secrets set "llm-ApiKey" "your-llm-api-key-here"

# Set AI Form Fill LLM API Key
dotnet user-secrets set "ai-form-fill-llm-ApiKey" "your-ai-form-fill-llm-api-key-here"

# Clear all secrets
# dotnet user-secrets clear
```

#### Method 2: Using appsettings.json (Alternative for Development Environment)

Edit the `Src/CodeSpirit.AppHost/appsettings.json` file and add parameter configuration:

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

> **⚠️ Important Note**: 
> - If using `appsettings.json` to configure sensitive information, ensure the file is added to `.gitignore`
> - Or create an `appsettings.Local.json` file (this file is already in `.gitignore` by default) to avoid committing API keys to the code repository
> - **Strongly recommend using User Secrets method**, which is more secure and won't be accidentally committed

### Obtaining API Keys

#### Alibaba Cloud Tongyi Qianwen (DashScope)

Free quota is sufficient for development phase:
1. Visit [Alibaba Cloud DashScope Console](https://dashscope.console.aliyun.com/)
2. Register/Login to your account
3. Create an API Key
4. Configure the API Key in the parameters above

#### OpenAI (If Using OpenAI-Compatible Interface)

If using an OpenAI-compatible API service, you need to modify the following parameters:

Configure using User Secrets:
```bash
dotnet user-secrets set "llm-ApiBaseUrl" "https://api.openai.com/v1"
dotnet user-secrets set "llm-ModelName" "gpt-4"
dotnet user-secrets set "llm-ApiKey" "your-openai-api-key-here"
```

Or configure using appsettings.json:
```json
{
  "llm-ApiBaseUrl": "https://api.openai.com/v1",
  "llm-ModelName": "gpt-4",
  "llm-ApiKey": "your-openai-api-key-here"
}
```

### Verifying Parameter Configuration

After starting the project, if parameters are configured incorrectly, you will see relevant error messages in the console or Aspire Dashboard. Ensure the following services can start normally:

- ✅ ConfigCenter (Config Center) - Requires LLM parameters
- ✅ Web Frontend - Requires AI Form Fill LLM parameters

> **Tip**: If you don't need to use AI features temporarily, you can set a placeholder value, but some AI-dependent features will not work properly.

## Development Tool Configuration

### Visual Studio Code

Create `.vscode/launch.json`:

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

Create `.vscode/tasks.json`:

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

## Verification

### 1. Check Service Status

Access Aspire Dashboard (http://localhost:17109) to confirm all services are running normally:

- ✅ CodeSpirit.Web (Web Frontend)
- ✅ CodeSpirit.IdentityApi (Identity Authentication)
- ✅ CodeSpirit.ConfigCenter (Config Center)
- ✅ CodeSpirit.MessagingApi (Messaging Service)
- ✅ CodeSpirit.ExamApi (Exam System)
- ✅ CodeSpirit.FileStorageApi (File Storage)
- ✅ CodeSpirit.SurveyApi (Survey)
- ✅ CodeSpirit.ApprovalApi (Approval Workflow)
- ✅ CodeSpirit.PathfinderApi (AI Goal Management)
- ✅ MySQL/SQL Server (Database, based on configuration)
- ✅ Redis (Cache)
- ✅ RabbitMQ (Message Queue)
- ✅ GreptimeDB (Time-series Database)
- ✅ Seq (Logging Service)

### 2. Check Errors

Open the structured logging panel to check for any errors:

![image-20251218195748771](/img/image-20251218195748771.png)

### 3. Access Web Interface

System Platform: https://localhost:7120

Account: systemadmin 

Password: CodeSpirit@2025

![image-20251218195825029](/img/image-20251218195825029.png)

After login, you can see the system platform backend management UI:

![image-20251218200130419](/img/image-20251218200130419.png)

Tenant Platform (default tenant): https://localhost:7120/default/login

Account: admin

Password: 123@Admin

![image-20251218195939141](/img/image-20251218195939141.png)

![image-20251218200156112](/img/image-20251218200156112.png)

## Common Issues

### Unable to Open Web Pages

This is usually caused by the following situations:

1. Unable to pull images, which can usually be seen in the Docker panel or Aspire management panel logs. It is recommended to configure image sources or use a VPN.

2. Critical service failure, such as Web service failure.

3. Port conflicts or network errors. Check the startup console for errors:

   ![image-20251218200528327](/img/image-20251218200528327.png)

### Port Conflicts
If encountering port conflicts, modify port configuration in `Src/CodeSpirit.AppHost/Program.cs`.

### Docker Service Startup Failure
Since the project uses .NET Aspire to manage services, if encountering service startup issues:

```bash
# Restart Aspire application
cd Src/CodeSpirit.AppHost
dotnet run --force

# Check service status in Aspire Dashboard
# Access http://localhost:17109
```

### GreptimeDB Startup Failure
```bash
# Check GreptimeDB status in Aspire Dashboard
# If memory insufficient, adjust GreptimeDB configuration in Program.cs

# Check system resource usage
# GreptimeDB requires at least 512MB memory
```

### SSL Certificate Issues
```bash
# Trust development certificate
dotnet dev-certs https --trust
```

### Database Connection Issues
```bash
# Check database container status (based on configured database type)
docker ps | grep mysql    # MySQL
docker ps | grep sqlserver # SQL Server

# Restart database container
docker restart mysql      # MySQL
docker restart sqlserver  # SQL Server

# Or check database status and connection information in Aspire Dashboard
```

### Insufficient Memory
If system memory is insufficient, you can:
1. Close unnecessary applications
2. Adjust GreptimeDB memory settings (in Program.cs)
3. Consider upgrading system memory to recommended configuration (16GB recommended, 32GB better)

### LLM API Key Not Configured

If you encounter the following errors or services cannot start normally during startup:

- ConfigCenter service startup failure
- Web frontend cannot access AI features
- Console prompts missing LLM configuration parameters

**Solution**:

1. **Check if parameters are configured**:
   ```bash
   # View User Secrets (if using)
   cd Src/CodeSpirit.AppHost
   dotnet user-secrets list
   ```

2. **Configure missing parameters**:
   - Refer to the [Required Parameters Configuration](#required-parameters-configuration) section
   - Ensure at least `llm-ApiKey` and `ai-form-fill-llm-ApiKey` are configured

3. **Verify configuration**:
   - After restarting the application, check service status in Aspire Dashboard
   - View service logs to confirm parameters are loaded correctly

> **Tip**: If you don't need to use AI features temporarily, you can set a placeholder value (such as `placeholder-key`), but related AI features will not work properly.

## Development Mode

### Hot Reload Development

```bash
# Enable hot reload
cd Src/CodeSpirit.AppHost
dotnet watch run
```

### Debug Mode

Set breakpoints in Visual Studio or VS Code, press F5 to start debugging.

## Production Deployment

### Using Kubernetes Deployment

The project provides complete Kubernetes deployment files:

```bash
# Deploy to Kubernetes cluster
kubectl apply -f k8s/

# Check deployment status
kubectl get pods -n code-spirit-release
```

### Using Docker Deployment

```bash
# Build Docker images for all services
dotnet publish CodeSpirit.sln -c Release

# Build images using project-provided Dockerfiles
docker build -f Src/CodeSpirit.Web/Dockerfile -t codespirit-web:latest .
docker build -f Src/CodeSpirit.IdentityApi/Dockerfile -t codespirit-identity:latest .
```

### Configuration Management

Production environment configuration is managed through:
- **Kubernetes ConfigMap**: Store application configuration
- **Kubernetes Secret**: Store sensitive information
- **Config Center**: Dynamic configuration management

## Next Steps

After environment setup is complete, you can:

1. 📖 Read [Project Overall Architecture Design](./Project%20Overall%20Architecture%20Design.md)
2. 🔧 Learn about [CodeSpirit.Core Core Framework](./CodeSpirit.Core%20Core%20Framework.md)
3. 📋 Review [Overall Technical System Overview](./Overall%20Technical%20System%20Overview.md)
4. 🔐 Study [Unified Exception Handling Guide](./CodeSpirit%20Unified%20Exception%20Handling%20Guide.md)
5. 💻 Reference [CRUD Development Example](./CRUD%20Development%20Example.md) to start development

## Get Help

If you encounter issues, please refer to:
- [GitHub Issues](https://github.com/your-org/code-spirit/issues)
- [Project Wiki](https://github.com/your-org/code-spirit/wiki)
- [Discussion Forum](https://github.com/your-org/code-spirit/discussions)

Happy coding! 🚀
