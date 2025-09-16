# CodeSpirit.ApprovalApi - 审批系统API

## 项目概述

CodeSpirit.ApprovalApi 是 CodeSpirit 框架中的审批系统微服务，提供完整的工作流审批功能。

## 功能特性

### 核心功能
- **工作流定义管理**: 创建、更新、删除工作流定义
- **审批实例管理**: 发起审批、查看审批状态、撤回审批
- **审批任务处理**: 处理待办任务、查看已办任务、任务转交、加签
- **审批日志**: 完整的审批过程记录

### 工作流支持
- **多种节点类型**: 开始节点、审批节点、条件节点、并行网关、排他网关、抄送节点、结束节点
- **灵活审批模式**: 串行审批、并行审批、会签、或签
- **动态审批人**: 支持指定用户、角色、部门、发起人、发起人上级、动态表达式
- **条件分支**: 支持复杂的条件判断和流程分支

### 数据模型

#### 核心实体
- `WorkflowDefinition`: 工作流定义
- `WorkflowNode`: 工作流节点
- `WorkflowNodeApprover`: 节点审批人
- `WorkflowNodeCondition`: 节点条件
- `ApprovalInstance`: 审批实例
- `ApprovalTask`: 审批任务
- `ApprovalLog`: 审批日志

#### 枚举类型
- `WorkflowNodeType`: 工作流节点类型
- `ApprovalMode`: 审批模式
- `ApproverType`: 审批人类型
- `ApprovalStatus`: 审批状态
- `ApprovalTaskStatus`: 任务状态
- `ApprovalResult`: 审批结果
- `ApprovalLogType`: 日志类型

## API 接口

### 工作流定义管理
- `GET /api/workflow-definitions` - 获取工作流定义列表
- `GET /api/workflow-definitions/{id}` - 获取工作流定义详情
- `POST /api/workflow-definitions` - 创建工作流定义
- `PUT /api/workflow-definitions/{id}` - 更新工作流定义
- `DELETE /api/workflow-definitions/{id}` - 删除工作流定义
- `PUT /api/workflow-definitions/{id}/enable` - 启用工作流
- `PUT /api/workflow-definitions/{id}/disable` - 禁用工作流
- `POST /api/workflow-definitions/{id}/copy` - 复制工作流

### 审批实例管理
- `GET /api/approval-instances` - 获取审批实例列表
- `GET /api/approval-instances/{id}` - 获取审批实例详情
- `POST /api/approval-instances` - 发起审批
- `PUT /api/approval-instances/{id}/withdraw` - 撤回审批
- `GET /api/approval-instances/my-applications` - 获取我发起的审批

### 审批任务管理
- `GET /api/approval-tasks` - 获取审批任务列表
- `GET /api/approval-tasks/{id}` - 获取审批任务详情
- `PUT /api/approval-tasks/{id}/process` - 处理审批任务
- `GET /api/approval-tasks/my-pending` - 获取我的待办任务
- `GET /api/approval-tasks/my-completed` - 获取我的已办任务
- `POST /api/approval-tasks/{id}/add-sign` - 加签
- `POST /api/approval-tasks/{id}/transfer` - 转交任务

## 技术栈

- **.NET 9**: 基础框架
- **ASP.NET Core**: Web API框架
- **Entity Framework Core**: ORM框架
- **SQL Server / MySQL**: 多数据库支持
- **AutoMapper**: 对象映射
- **Swagger**: API文档
- **多租户支持**: 基于CodeSpirit.MultiTenant
- **统一启动框架**: 基于CodeSpirit.Shared.Startup

## 项目结构

```
CodeSpirit.ApprovalApi/
├── Controllers/           # 控制器
├── Services/             # 服务接口
├── Data/                 # 数据访问层
├── Models/               # 数据模型
├── Dtos/                 # 数据传输对象
├── Configuration/        # 配置类
├── Constants/            # 常量定义
├── MappingProfiles/      # AutoMapper配置
└── Properties/           # 项目属性
```

## 配置说明

### 数据库连接
```json
{
  "ConnectionStrings": {
    "approval-api": "Server=(localdb)\\mssqllocaldb;Database=CodeSpirit.ApprovalApi;Trusted_Connection=true;MultipleActiveResultSets=true",
    "approval-api-mysql": "Server=localhost;Database=CodeSpirit_ApprovalApi;Uid=root;Pwd=;CharSet=utf8mb4;"
  },
  "Database": {
    "Provider": "SqlServer",
    "MigrationsAssembly": "CodeSpirit.ApprovalApi"
  }
}
```

### 审批配置
```json
{
  "ApprovalApi": {
    "EnableIntelligentApproval": true,
    "DefaultTimeoutHours": 72,
    "EnableAutoReminder": true,
    "ReminderIntervalHours": 24,
    "EnableApprovalLog": true
  }
}
```

## 部署说明

1. 确保 SQL Server 可用
2. 更新 `appsettings.json` 中的连接字符串
3. 运行数据库迁移：`dotnet ef database update`
4. 启动应用：`dotnet run`

## 多数据库支持

项目支持 SQL Server 和 MySQL 两种数据库：

### 切换数据库类型
在 `appsettings.json` 中修改 `Database.Provider` 配置：
- `SqlServer`: 使用 SQL Server
- `MySql`: 使用 MySQL

### 数据库迁移
```bash
# SQL Server 迁移
dotnet ef migrations add InitialCreate --context SqlServerApprovalDbContext

# MySQL 迁移  
dotnet ef migrations add InitialCreate --context MySqlApprovalDbContext
```

## 开发计划

- [x] ✅ 创建项目基础结构
- [x] ✅ 定义核心数据模型
- [x] ✅ 实现多数据库支持
- [x] ✅ 集成多租户功能
- [x] ✅ 创建API控制器
- [ ] 🔄 实现服务层具体逻辑
- [ ] 🔄 添加数据库迁移
- [ ] 🔄 集成智能审批功能
- [ ] 🔄 添加单元测试
- [ ] 🔄 完善文档

## 注意事项

1. ✅ 项目已支持多数据库（SQL Server/MySQL）
2. ✅ 已集成多租户支持
3. ✅ 使用统一的API启动框架
4. 🔄 服务接口已定义，需要实现具体业务逻辑
5. 🔄 需要根据实际业务需求实现工作流引擎
6. 🔄 智能审批功能需要集成 LLM 服务
