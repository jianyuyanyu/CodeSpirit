# CodeSpirit Cursor 规则文档

本目录包含 CodeSpirit 项目的开发规范文档，用于 Cursor IDE 智能提示和代码生成。

## 📁 文档结构

### 通用规范
- **[all.mdc](./all.mdc)** - 全局规范，始终应用，包含技术栈、项目结构、基本开发规范

### 语言特定规范
- **[cs.mdc](./cs.mdc)** - C# 通用规范：XML注释、时间格式、序列化等通用要求（始终应用）
- **[js.mdc](./js.mdc)** - JavaScript 规范，AMIS 框架使用规范
- **[css.mdc](./css.mdc)** - CSS 样式规范，动效和 AMIS 主题规范
- **[cshtml.mdc](./cshtml.mdc)** - Razor 视图规范
- **[csproj.mdc](./csproj.mdc)** - 项目文件规范

### 按文件类型规范
- **[dto.mdc](./dto.mdc)** - DTO 开发规范：特性、验证、映射和多语言支持
- **[controller.mdc](./controller.mdc)** - 控制器开发规范：API控制器、操作特性、路由
- **[service.mdc](./service.mdc)** - 服务类开发规范：服务接口、实现、生命周期
- **[enum.mdc](./enum.mdc)** - 枚举开发规范：枚举定义、Display特性和多语言支持

### 核心开发规范
- **[naming-conventions.mdc](./naming-conventions.mdc)** - 命名约定：实体、DTO、服务、控制器等命名规则
- **[api-design.mdc](./api-design.mdc)** - API 设计：RESTful、路由、响应格式、操作特性
- **[dependency-injection.mdc](./dependency-injection.mdc)** - 依赖注入：Scrutor 自动注册、生命周期管理
- **[startup-framework.mdc](./startup-framework.mdc)** - 统一启动框架：API 项目配置标准化、中间件插入点
- **[i18n.mdc](./i18n.mdc)** - 多语言国际化：资源文件、本地化、前后端多语言支持
- **[ai-development.mdc](./ai-development.mdc)** - AI 功能开发：AI 表单填充、长任务处理、LLM 集成
- **[performance.mdc](./performance.mdc)** - 性能优化：异步编程、缓存策略、查询优化、分布式场景
- **[security.mdc](./security.mdc)** - 安全规范：权限控制、审计追踪、数据保护
- **[testing.mdc](./testing.mdc)** - 测试开发规范：单元测试、集成测试、Mock使用

### 项目结构
- **[project-structure.mdc](./project-structure.mdc)** - 完整项目结构、目录树和组件分类

### 特定组件规范
- **[amis-cards.mdc](./amis-cards.mdc)** - AMIS 卡片组件使用规范

## 🎯 规则应用范围

### 始终应用 (alwaysApply: true)
- `all.mdc` - 全局规范，对所有文件生效
- `cs.mdc` - C# 通用规范，对所有 C# 文件生效

### 按文件类型应用 (alwaysApply: false)
规则文件通过 `globs` 字段指定适用的文件类型（支持数组格式，精确匹配）：

| 规则文件 | 适用文件 | 说明 |
|---------|---------|------|
| **语言规范** |
| js.mdc | `*.js` | JavaScript 文件 |
| css.mdc | `*.css` | CSS 样式文件 |
| cshtml.mdc | `*.cshtml` | Razor 视图文件 |
| csproj.mdc | `*.csproj` | 项目文件 |
| **按文件类型** |
| dto.mdc | `*Dto.cs`, `**/Dtos/**/*.cs` | DTO 文件 |
| controller.mdc | `*Controller.cs`, `**/Controllers/**/*.cs` | 控制器文件 |
| service.mdc | `*Service.cs`, `**/Services/**/*.cs` | 服务类文件 |
| enum.mdc | `*Enum.cs`, `**/Enums/**/*.cs` | 枚举文件 |
| **专项规范** |
| naming-conventions.mdc | `*.cs` | 命名约定规范 |
| api-design.mdc | `*Controller.cs`, `**/Controllers/**/*.cs` | API 设计规范（控制器） |
| dependency-injection.mdc | `*Service.cs`, `**/Services/**/*.cs` | 依赖注入规范（服务） |
| startup-framework.mdc | `*.cs` | 启动框架规范 |
| i18n.mdc | `*Dto.cs`, `*Enum.cs`, `*Controller.cs`, `**/Resources/**/*.cs` | 国际化规范（多文件类型） |
| ai-development.mdc | `*.cs` | AI 开发规范 |
| performance.mdc | `*.cs` | 性能优化规范 |
| security.mdc | `*Controller.cs`, `**/Controllers/**/*.cs` | 安全规范（控制器） |
| testing.mdc | `*Test.cs`, `*Tests.cs`, `**/Tests/**/*.cs` | 测试规范 |
| **项目结构** |
| project-structure.mdc | - | 项目结构详细说明（无 globs） |

## 📚 快速导航

### 新项目开发
1. 阅读 **[all.mdc](./all.mdc)** 了解整体架构
2. 参考 **[naming-conventions.mdc](./naming-conventions.mdc)** 规划命名
3. 使用 **[startup-framework.mdc](./startup-framework.mdc)** 创建 API 项目
4. 遵循 **[api-design.mdc](./api-design.mdc)** 和 **[controller.mdc](./controller.mdc)** 设计 API
5. 参考 **[dependency-injection.mdc](./dependency-injection.mdc)** 和 **[service.mdc](./service.mdc)** 注册服务

### 功能开发
- **CRUD 开发**: 
  - DTO: [dto.mdc](./dto.mdc)
  - 服务: [service.mdc](./service.mdc)
  - 控制器: [controller.mdc](./controller.mdc)
  - API设计: [api-design.mdc](./api-design.mdc)
- **枚举定义**: [enum.mdc](./enum.mdc)
- **AI 功能**: [ai-development.mdc](./ai-development.mdc)
- **多语言支持**: [i18n.mdc](./i18n.mdc)
- **性能优化**: [performance.mdc](./performance.mdc)
- **安全加固**: [security.mdc](./security.mdc)
- **测试开发**: [testing.mdc](./testing.mdc)

### 前端开发
- **AMIS 界面**: js.mdc → amis-cards.mdc
- **样式开发**: css.mdc

## 🔄 规则更新流程

1. 修改或新增规则文件
2. 在 Cursor IDE 中重新加载规则（重启 IDE 或重新打开项目）
3. 测试规则是否正确应用
4. 提交到版本控制

## 📖 规则文件格式

每个规则文件采用 Markdown 格式，包含 YAML front matter：

```markdown
---
description: 规则描述
globs: 
  - "*.cs"
  - "**/Controllers/**/*.cs"
alwaysApply: false
---

# 规则标题

规则内容...
```

### Front Matter 说明
- **description**: 规则文件描述（必填）
- **globs**: 适用的文件类型模式，支持字符串或数组格式
  - 字符串格式：`globs: *.cs`
  - 数组格式：`globs: ["*Dto.cs", "**/Dtos/**/*.cs"]`
  - 空值：不指定 globs 表示手动引用（如 project-structure.mdc）
- **alwaysApply**: 是否始终应用（true/false）
  - `true`: 规则始终生效（如 all.mdc, cs.mdc）
  - `false`: 仅匹配 globs 的文件生效

## 💡 最佳实践

1. **保持规则简明扼要**：重点突出，避免冗长描述
2. **提供示例代码**：正确示例和错误示例对比，示例应使用多语言写法
3. **使用精确的 globs**：避免使用 `*.cs` 等过于宽泛的模式，使用精确匹配（如 `*Dto.cs`, `*Controller.cs`）
4. **及时更新规则**：随项目演进更新规范
5. **遵循规则优先级**：全局规则 → 语言规则 → 文件类型规则 → 专项规则
6. **定期审查规则**：确保规则与实际开发保持一致
7. **多语言优先**：所有示例代码应展示多语言写法，而非硬编码中文

## 🔗 相关文档

- [项目文档目录](../../Docs/documentation-catalog-zh-CN.md)
- [开发环境搭建](../../Docs/01-Core-Docs/03-development-environment-setup-zh-CN.md)
- [CodeSpirit 核心框架](../../Docs/01-Core-Docs/04-codespirit-core-framework-zh-CN.md)

---

**注意**：本规则库持续更新中，如有疑问或建议，请联系项目维护者。

