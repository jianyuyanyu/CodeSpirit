# BMAD 工作流使用指南

## 概述

BMAD (Breakthrough Method of Agile AI-Driven Development) 是一个结构化的 AI 驱动软件开发方法，提供从需求到实现的完整工作流。

本指南介绍如何在 CodeSpirit 项目中使用 BMAD 工作流。

## 快速开始

### Quick Flow（快速流程）

适用于小型任务和 Bug 修复：

```bash
# 1. 创建技术规范
/quick-spec 修复用户登录时租户ID丢失的问题

# 2. 实现变更
/quick-dev

# 3. 代码审查
/code-review
```

### Full Flow（完整流程）

适用于完整功能开发：

```bash
# 1. 产品需求简报
/product-brief 在 ExamApi 中添加题目标签管理功能

# 2. 创建 PRD
/create-prd

# 3. 架构设计
/create-architecture

# 4. 拆分为 Epic 和 Story
/create-epics-and-stories

# 5. Sprint 规划
/sprint-planning

# 6. 实现 Story
/dev-story {story-id}

# 7. 代码审查
/code-review

# 8. 复盘
/retrospective
```

## 工作流详解

### 1. Quick Flow

#### `/quick-spec` - 创建技术规范

生成技术规范文档，包含：
- 问题描述
- 技术方案
- 实现步骤
- 测试计划

**示例**:
```
/quick-spec 修复用户登录时租户ID丢失的问题
```

#### `/quick-dev` - 实现变更

基于技术规范实现代码变更。

**注意事项**:
- 自动遵循 CodeSpirit 规范
- 自动应用命名约定
- 自动添加 XML 注释
- 自动配置依赖注入

#### `/code-review` - 代码审查

执行代码审查，包括：
- BMAD 标准审查
- CodeSpirit 特定审查
- 安全审查
- 性能审查

### 2. Full Flow

#### `/product-brief` - 产品需求简报

创建产品需求简报，包含：
- 功能概述
- 用户故事
- 成功标准
- 技术约束

**示例**:
```
/product-brief 在 ExamApi 中添加题目标签管理功能，支持标签的增删改查，题目可以关联多个标签
```

#### `/create-prd` - 创建 PRD

基于产品简报创建详细的产品需求文档。

**CodeSpirit 特定部分**:
- 多租户考量
- 多数据库支持
- AI 功能需求
- 国际化需求
- 权限设计

#### `/create-architecture` - 架构设计

创建架构设计文档，包含：
- 数据库设计
- API 设计
- 服务层设计
- 依赖注入策略
- 缓存策略

**CodeSpirit 特定设计**:
- 数据库特定的 DbContext 设计
- 多数据库迁移策略
- 自动注册接口选择
- L1/L2 缓存决策

#### `/create-epics-and-stories` - 拆分 Story

将功能拆分为 Epic 和 Story。

**Story 包含**:
- 实体定义
- DTO 设计
- 服务接口
- 控制器端点
- 数据库迁移需求
- 多语言资源需求
- 测试要求

#### `/sprint-planning` - Sprint 规划

规划 Sprint 任务和优先级。

#### `/dev-story` - 实现 Story

实现 Story 中的所有任务。

**自动应用**:
- CodeSpirit CRUD 开发流程（如涉及）
- 数据库迁移流程（如涉及）
- 所有代码规范

**与现有技能协同**:
- 如果涉及 CRUD，自动调用 `api-crud-development` 技能
- 如果涉及数据库变更，自动调用 `db-migration` 技能

#### `/code-review` - 代码审查

执行多层级代码审查：
1. BMAD 标准审查
2. CodeSpirit 特定审查（`code-review` 技能）
3. 提交前验证（`pre-commit-validation` 技能）

#### `/retrospective` - 复盘

回顾 Sprint 完成情况，总结经验教训。

## CodeSpirit 规范自动应用

BMAD 工作流已配置为自动遵循 CodeSpirit 的所有开发规范：

### Analysis 阶段
- 自动考虑多租户、多数据库、AI 功能等项目特性
- 自动识别国际化需求

### Planning 阶段
- 自动遵循 RESTful 标准和统一响应格式
- 自动应用 DTO 设计规范

### Solutioning 阶段
- 自动设计数据库特定的 DbContext
- 自动选择依赖注入策略
- 自动设计缓存策略

### Implementation 阶段
- 自动应用命名约定
- 自动添加 XML 文档注释
- 自动配置依赖注入
- 自动应用数据库迁移流程
- 自动配置多语言资源

### Review 阶段
- 自动执行 CodeSpirit 特定审查清单
- 自动执行提交前验证

## 常见场景示例

### 场景 1：新增 CRUD 功能

```bash
# 1. 创建产品简报
/product-brief 在 MallApi 中添加商品分类管理功能

# 2. 创建 PRD
/create-prd

# 3. 架构设计
/create-architecture

# 4. 拆分 Story
/create-epics-and-stories

# 5. 实现 Story（自动调用 api-crud-development 技能）
/dev-story {story-id}

# 6. 代码审查
/code-review
```

### 场景 2：集成 AI 功能

```bash
# 1. 创建产品简报
/product-brief 在 SurveyApi 中添加 AI 问卷生成功能

# 2. 创建 PRD（自动考虑 AI 功能需求）
/create-prd

# 3. 架构设计（自动设计 LLM 集成）
/create-architecture

# 4. 实现 Story（自动应用 AI 开发规范）
/dev-story {story-id}
```

### 场景 3：数据库变更

```bash
# 1. 创建技术规范
/quick-spec 为 User 表添加手机号字段

# 2. 实现变更（自动调用 db-migration 技能）
/quick-dev

# 3. 代码审查（自动检查多数据库支持）
/code-review
```

## 最佳实践

### 1. 选择合适的流程

- **Quick Flow**: 小型任务、Bug 修复、简单变更
- **Full Flow**: 新功能开发、复杂变更、架构调整

### 2. 充分利用规范自动应用

BMAD 已配置为自动遵循 CodeSpirit 规范，无需手动查阅规范文档。

### 3. 与现有技能协同

BMAD 会自动调用相关技能：
- `api-crud-development`: CRUD 功能开发
- `db-migration`: 数据库迁移
- `code-review`: 代码审查
- `pre-commit-validation`: 提交前验证

### 4. 文档沉淀

BMAD 生成的所有文档（PRD、架构设计、Story）都会保存到项目中，成为项目知识库。

### 5. 迭代改进

使用 `/retrospective` 回顾工作流，持续改进开发流程。

## 故障排除

### BMAD 命令不识别

确保 BMAD 已正确安装：
```bash
npx bmad-method install --modules bmm --tools cursor --yes
```

### 规范未自动应用

检查项目上下文文档是否存在：
- `project-context.md` 应位于项目根目录
- `.bmadconfig.json` 应正确配置

### 技能未自动调用

检查技能文件是否存在：
- `.cursor/skills/bmad-integration/SKILL.md`
- `.cursor/skills/api-crud-development/SKILL.md`
- `.cursor/skills/db-migration/SKILL.md`

## 相关资源

- **[BMAD 使用教程](bmad-tutorial.md)** - 完整的综合教程（推荐新手阅读）
- [BMAD 团队培训指南](bmad-team-guide.md) - 团队培训材料
- [BMAD 集成技能](.cursor/skills/bmad-integration/SKILL.md) - BMAD 与 CodeSpirit 集成
- [CodeSpirit 规范映射](.cursor/skills/bmad-integration/codespirit-standards-mapping.md) - 详细的规范映射
- [项目上下文文档](../project-context.md) - 项目上下文和规范引用
- [CodeSpirit 规范文档](.cursor/rules/) - 所有开发规范文档

## 获取帮助

任何时候，输入 `/bmad-help` 可获取上下文相关的指导。
