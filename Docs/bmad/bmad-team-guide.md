# BMAD 团队培训指南

## 概述

本指南帮助团队成员快速了解和使用 BMAD AI 工作流，提高开发效率和代码质量。

## BMAD 基础概念

### 什么是 BMAD？

BMAD (Breakthrough Method of Agile AI-Driven Development) 是一个结构化的 AI 驱动软件开发方法，提供：

- **标准化工作流**: 从需求到实现的完整流程
- **自动化规范应用**: 自动遵循项目开发规范
- **知识沉淀**: 所有文档和设计都成为项目知识库
- **质量保证**: 多层级的代码审查和验证

### BMAD 的核心价值

1. **减少上下文切换**: 在 AI 辅助开发的同时，无需手动查阅规范文档
2. **提高代码质量**: 自动应用规范，多层级的审查确保代码质量
3. **知识沉淀**: PRD、架构设计、Story 文档都成为项目知识库
4. **团队协作**: 标准化的工作流和文档格式提升团队协作效率

## BMAD 工作流介绍

### Quick Flow（快速流程）

适用于：
- 小型任务
- Bug 修复
- 简单变更

流程：
```
/quick-spec → /quick-dev → /code-review
```

### Full Flow（完整流程）

适用于：
- 新功能开发
- 复杂变更
- 架构调整

流程：
```
/product-brief → /create-prd → /create-architecture → 
/create-epics-and-stories → /sprint-planning → 
/dev-story → /code-review → /retrospective
```

## CodeSpirit 项目中的 BMAD 使用

### 自动规范应用

BMAD 已配置为自动遵循 CodeSpirit 的所有开发规范：

- ✅ **命名约定**: 实体、DTO、服务、控制器命名
- ✅ **代码规范**: XML 注释、UTC 时间、Newtonsoft.Json
- ✅ **数据库规范**: 多数据库支持、迁移流程
- ✅ **API 设计**: RESTful 标准、统一响应格式
- ✅ **多语言支持**: 资源文件、本地化
- ✅ **依赖注入**: 自动注册、生命周期管理

### 与现有技能协同

BMAD 会自动调用相关技能：

- **api-crud-development**: CRUD 功能开发时自动调用
- **db-migration**: 数据库变更时自动调用
- **code-review**: 代码审查时自动调用
- **pre-commit-validation**: 提交前自动验证

## 实际案例演示

### 案例 1：新增商品分类管理功能

**需求**: 在 MallApi 中添加商品分类的增删改查功能

**步骤**:

1. **创建产品简报**
   ```
   /product-brief 在 MallApi 中添加商品分类管理功能，支持分类的增删改查，商品可以关联分类
   ```

2. **创建 PRD**
   ```
   /create-prd
   ```
   BMAD 自动考虑：
   - 多租户支持（分类需要租户隔离）
   - 多数据库支持（MySQL 和 SQL Server）
   - 权限设计（分类管理权限）
   - 国际化需求（中文/英文）

3. **架构设计**
   ```
   /create-architecture
   ```
   BMAD 自动设计：
   - 数据库特定的 DbContext
   - RESTful API 端点
   - 依赖注入策略
   - 缓存策略

4. **拆分 Story**
   ```
   /create-epics-and-stories
   ```
   生成 Story，包含：
   - 实体定义（Category）
   - DTO 设计（CreateCategoryDto, UpdateCategoryDto 等）
   - 服务接口（ICategoryService）
   - 控制器端点（CategoriesController）
   - 数据库迁移需求

5. **实现 Story**
   ```
   /dev-story {story-id}
   ```
   BMAD 自动：
   - 调用 `api-crud-development` 技能
   - 创建实体、DTO、服务、控制器
   - 配置数据库迁移
   - 添加 XML 注释
   - 配置多语言资源

6. **代码审查**
   ```
   /code-review
   ```
   执行多层级审查：
   - BMAD 标准审查
   - CodeSpirit 特定审查
   - 提交前验证

### 案例 2：修复 Bug

**需求**: 修复用户登录时租户ID丢失的问题

**步骤**:

1. **创建技术规范**
   ```
   /quick-spec 修复用户登录时租户ID丢失的问题
   ```

2. **实现变更**
   ```
   /quick-dev
   ```
   BMAD 自动应用 CodeSpirit 规范

3. **代码审查**
   ```
   /code-review
   ```

## 常见问题解答

### Q1: BMAD 命令在哪里输入？

A: 在 Cursor IDE 的 AI 聊天窗口中输入 BMAD 命令。

### Q2: BMAD 会覆盖我现有的代码吗？

A: 不会。BMAD 会生成新代码或修改现有代码，但会遵循 Git 工作流，你可以审查和提交更改。

### Q3: BMAD 生成的代码符合项目规范吗？

A: 是的。BMAD 已配置为自动遵循 CodeSpirit 的所有开发规范，包括命名约定、代码规范、数据库规范等。

### Q4: 我可以修改 BMAD 生成的代码吗？

A: 可以。BMAD 生成的代码是起点，你可以根据实际需求进行修改和完善。

### Q5: BMAD 如何处理数据库迁移？

A: BMAD 会自动调用 `db-migration` 技能，确保同时为 MySQL 和 SQL Server 创建迁移。

### Q6: BMAD 支持哪些开发场景？

A: BMAD 支持所有开发场景，包括：
- CRUD 功能开发
- AI 功能集成
- 数据库变更
- Bug 修复
- 架构调整

### Q7: BMAD 生成的文档在哪里？

A: BMAD 生成的所有文档（PRD、架构设计、Story）都会保存到项目中，成为项目知识库。

### Q8: 如何学习 BMAD 的更多功能？

A: 参考以下资源：
- [BMAD 工作流指南](bmad-workflow-guide.md)
- [BMAD 集成技能](.cursor/skills/bmad-integration/SKILL.md)
- 输入 `/bmad-help` 获取上下文相关的指导

## 最佳实践

### 1. 选择合适的流程

- **Quick Flow**: 小型任务、Bug 修复、简单变更
- **Full Flow**: 新功能开发、复杂变更、架构调整

### 2. 充分利用自动规范应用

BMAD 已配置为自动遵循 CodeSpirit 规范，无需手动查阅规范文档。

### 3. 审查生成的代码

虽然 BMAD 自动应用规范，但仍需审查生成的代码，确保符合实际需求。

### 4. 文档沉淀

BMAD 生成的所有文档都成为项目知识库，有助于团队协作和知识传承。

### 5. 持续改进

使用 `/retrospective` 回顾工作流，持续改进开发流程。

## 培训计划

### 第一阶段：基础了解（1小时）

- BMAD 基础概念
- Quick Flow 使用
- 实际案例演示

### 第二阶段：深入使用（2小时）

- Full Flow 使用
- 规范自动应用
- 与现有技能协同

### 第三阶段：最佳实践（1小时）

- 常见场景处理
- 故障排除
- 团队协作

## 相关资源

- **[BMAD 使用教程](bmad-tutorial.md)** - 完整的综合教程（推荐新手阅读）
- [BMAD 工作流指南](bmad-workflow-guide.md) - 详细的工作流使用指南
- [BMAD 集成技能](.cursor/skills/bmad-integration/SKILL.md) - BMAD 与 CodeSpirit 集成
- [CodeSpirit 规范映射](.cursor/skills/bmad-integration/codespirit-standards-mapping.md) - 详细的规范映射
- [项目上下文文档](../project-context.md) - 项目上下文和规范引用
- [CodeSpirit 规范文档](.cursor/rules/) - 所有开发规范文档

## 获取帮助

任何时候，输入 `/bmad-help` 可获取上下文相关的指导。

如有问题，请联系团队负责人或查阅相关文档。
