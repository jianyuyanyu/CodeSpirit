---
name: bmad-integration
description: BMAD AI 工作流与 CodeSpirit 项目规范的集成技能。引导用户使用 BMAD 工作流，确保 BMAD 生成的文档和代码符合 CodeSpirit 规范。当用户提到 BMAD 工作流、需要完整开发流程管理、或执行 BMAD 命令时使用。
---

# BMAD 集成 Skill

## 概述

本技能用于将 BMAD (Breakthrough Method of Agile AI-Driven Development) AI 工作流与 CodeSpirit 项目的开发规范无缝集成，确保 BMAD 生成的所有文档和代码都符合项目标准。

## 触发条件

在以下情况下使用此技能：

- 用户提到 BMAD 工作流
- 用户需要完整的开发流程管理（从需求到实现）
- 用户执行 BMAD 命令（如 `/quick-spec`、`/create-prd`、`/dev-story` 等）
- 用户需要结构化的软件开发流程

## BMAD 工作流概览

### Quick Flow（快速流程）

适用于小型任务和 Bug 修复：

1. `/quick-spec` - 创建技术规范
2. `/quick-dev` - 实现变更
3. `/code-review` - 代码审查

### Full Flow（完整流程）

适用于完整功能开发：

1. `/product-brief` - 产品需求简报
2. `/create-prd` - 创建 PRD（产品需求文档）
3. `/create-architecture` - 架构设计
4. `/create-epics-and-stories` - 拆分为 Epic 和 Story
5. `/sprint-planning` - Sprint 规划
6. `/dev-story` - 实现 Story
7. `/code-review` - 代码审查
8. `/retrospective` - 复盘

## CodeSpirit 规范注入

BMAD 在每个阶段都会自动加载对应的 CodeSpirit 规范：

### Analysis 阶段（需求分析）

- 参考 `.cursor/rules/ai-development.mdc` 的 AI 功能需求分析
- 考虑多租户、多数据库、AI 功能等项目特性
- 识别国际化需求

### Planning 阶段（规划）

- 遵循 `.cursor/rules/api-design.mdc` 的 RESTful 标准
- 遵循 `.cursor/rules/dto.mdc` 的 DTO 设计规范
- 考虑统一响应格式 `ApiResponse<T>`

### Solutioning 阶段（方案设计）

- 参考 `.cursor/rules/database.mdc` 的多数据库支持
- 参考 `.cursor/rules/dependency-injection.mdc` 的依赖注入策略
- 考虑缓存策略（L1/L2）
- 设计审计追踪方案

### Implementation 阶段（实现）

- 遵循 `.cursor/rules/cs.mdc` 的 C# 通用规范（XML 注释、UTC 时间、Newtonsoft.Json）
- 遵循 `.cursor/rules/controller.mdc` 的控制器规范
- 遵循 `.cursor/rules/service.mdc` 的服务类规范
- 遵循 `.cursor/rules/naming-conventions.mdc` 的命名约定
- 应用 `.cursor/rules/dto.mdc` 的 DTO 验证和映射

### Review 阶段（审查）

- 执行 BMAD 的 `/code-review` 工作流
- 执行 CodeSpirit 特定的审查清单（参考 `code-review` 技能）
- 执行提交前验证（参考 `pre-commit-validation` 技能）

## 与现有技能的协同

### api-crud-development 技能

在 BMAD `/dev-story` 阶段，如果 Story 涉及 CRUD 功能：

1. 使用 BMAD 生成 Story 文档
2. 在实现时调用 `api-crud-development` 技能
3. 确保遵循 CodeSpirit CRUD 开发流程

### db-migration 技能

在涉及数据库变更的 Story 中：

1. BMAD Story 文档中标注数据库变更需求
2. 实现时调用 `db-migration` 技能
3. 确保同时为 MySQL 和 SQL Server 创建迁移

### code-review 技能

在 BMAD `/code-review` 后：

1. 执行 BMAD 的标准代码审查
2. 执行 CodeSpirit 特定的审查清单
3. 检查是否符合所有项目规范

### pre-commit-validation 技能

在完成 sprint 前：

1. 执行 BMAD 的回顾（`/retrospective`）
2. 执行 `pre-commit-validation` 技能进行综合验证
3. 确保代码质量和功能完整性

## PRD 模板扩展

BMAD PRD 模板已扩展，包含 CodeSpirit 特定部分：

- **多租户考量**: 功能是否需要租户隔离
- **多数据库支持**: 是否需要同时支持 MySQL 和 SQL Server
- **AI 功能需求**: 是否需要集成 LLM、AI 表单填充或 AI 长任务
- **国际化需求**: 多语言资源文件配置
- **权限设计**: `[RequirePermission]` 特性需求

## Architecture 模板扩展

架构文档模板确保包含：

- **数据库选型**: MySQL vs SQL Server 的技术决策
- **DbContext 配置**: 数据库特定的 DbContext 设计
- **依赖注入策略**: 自动注册接口的选择（IScopedDependency 等）
- **缓存策略**: L1/L2 缓存的使用决策
- **API 设计**: RESTful 标准和统一响应格式（ApiResponse）
- **审计追踪**: GreptimeDB 审计配置

## Story 模板扩展

Story 文件包含：

- **实体定义**: Entity 类设计
- **DTO 设计**: CreateDto, UpdateDto, QueryDto, ResultDto
- **服务接口**: IService 接口定义
- **控制器端点**: RESTful API 端点规范
- **数据库迁移**: 是否需要创建迁移
- **多语言资源**: 需要添加的资源键
- **测试要求**: 单元测试和集成测试清单

## Dev Agent 提示词定制

BMAD 的开发代理已配置为：

- 自动遵循 CodeSpirit 的命名约定
- 使用正确的 XML 文档注释格式
- 应用正确的数据库迁移流程
- 实现正确的 DTO 验证和映射
- 使用 Newtonsoft.Json 而非 System.Text.Json
- 遵循异步编程规范（async/await）

## 使用示例

### 示例 1：快速修复 Bug

```
用户: /quick-spec 修复用户登录时租户ID丢失的问题

AI: 
1. 使用 BMAD /quick-spec 生成技术规范
2. 确保规范考虑多租户架构
3. 在 /quick-dev 阶段应用 CodeSpirit 规范
4. 在 /code-review 阶段执行 CodeSpirit 审查清单
```

### 示例 2：完整功能开发

```
用户: /product-brief 在 ExamApi 中添加题目标签管理功能

AI:
1. 使用 BMAD Full Flow 流程
2. 在 PRD 阶段考虑多租户、多数据库、权限设计
3. 在 Architecture 阶段设计数据库特定的 DbContext
4. 在 Story 阶段包含完整的 CRUD 实现清单
5. 在 Dev 阶段调用 api-crud-development 技能
6. 在 Review 阶段执行 CodeSpirit 审查
```

## 规范映射文档

详细的规范映射请参考：[codespirit-standards-mapping.md](codespirit-standards-mapping.md)

## 项目上下文

BMAD 会自动加载项目上下文文档：`project-context.md`

该文档包含：
- 项目概述和技术栈
- 核心架构模式
- 所有开发规范引用
- 关键约束和代码质量要求

## 注意事项

1. **始终遵循 CodeSpirit 规范**: BMAD 生成的内容必须符合项目规范
2. **多数据库支持**: 涉及数据库变更时，必须同时考虑 MySQL 和 SQL Server
3. **多租户隔离**: 新功能必须考虑租户隔离
4. **多语言支持**: 所有面向用户的文本必须支持多语言
5. **XML 注释**: 所有公共成员必须添加 XML 文档注释
6. **异步编程**: 所有 I/O 操作使用 async/await

## 相关资源

- [BMAD 工作流指南](../../Docs/bmad-workflow-guide.md)
- [项目上下文文档](../../project-context.md)
- [CodeSpirit 规范文档](../../.cursor/rules/)
