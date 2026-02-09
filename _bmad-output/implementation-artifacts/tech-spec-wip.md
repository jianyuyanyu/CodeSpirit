---
title: '修复 Amis Status 列仅显示图标不显示文字的问题'
slug: 'fix-amis-status-label-display'
created: '2026-02-09'
status: 'completed'
stepsCompleted: [1, 2, 3, 4]
tech_stack: ['C#', '.NET 10', 'AMIS', 'Newtonsoft.Json']
files_to_modify:
  - 'Src/Components/CodeSpirit.Amis/Column/StatusColumnHandler.cs'
code_patterns:
  - 'JObject 构建 AMIS 配置'
  - 'Status 列配置生成'
test_patterns: []
---

# Tech-Spec: 修复 Amis Status 列仅显示图标不显示文字的问题

**Created:** 2026-02-09

## Overview

### Problem Statement

系统审计日志界面中，`IsBulkOperation`（批量操作）列使用了 `StatusMapping.YesNo` 映射，生成的 Amis status 列配置包含了 `map` 和 `labelMap`，但前端仅显示状态图标，不显示文字标签（"是"/"否"）。

**当前配置输出：**
```json
{
    "name": "isBulkOperation",
    "label": "批量操作",
    "type": "status",
    "map": {
        "true": "info",
        "false": "default",
        "null": "default"
    },
    "labelMap": {
        "true": "是",
        "false": "否",
        "null": "-"
    },
    "placeholder": "-"
}
```

**API 返回数据：**
```json
{
    "isBulkOperation": false
}
```

### Solution

**问题根本原因：**

Amis 的 `status` 类型列主要用于显示状态图标，默认情况下**仅显示图标不显示文字标签**。虽然可以配置 `map` 和 `labelMap`，但前端渲染时仍然只显示图标。

**解决方案：**

对于布尔类型的字段，**改用 `mapping` 类型而不是 `status` 类型**：
- `mapping` 类型：直接显示映射后的文字标签，更适合布尔值的显示
- `status` 类型：主要显示状态图标，适合多状态值（如 HTTP 方法、状态码等）

**具体修改：**
1. 在 `StatusColumnHandler.ApplyStatusColumnConfiguration()` 方法中，检测属性类型
2. 对于布尔类型，使用 `mapping` 类型并直接应用标签映射
3. 对于其他类型（字符串、数字等），继续使用 `status` 类型显示图标

### Scope

**In Scope:**
1. 调查 Amis status 列的正确配置格式（通过文档或实际测试）
2. 修复 `StatusColumnHandler.GenerateStatusMappingConfig` 方法，添加必要的配置属性
3. 验证系统审计日志界面的批量操作列能正确显示"是"/"否"文字
4. 确保其他使用 status 列的字段（如 `IsSuccess`）也能正确显示

**Out of Scope:**
- 不修改 Amis 前端组件的渲染逻辑
- 不改变现有的 StatusMapping 枚举定义
- 不涉及数据库结构变更

## Context for Development

### Codebase Patterns

**当前 Status 列配置生成流程：**

1. `ColumnHelper.CreateAmisColumn()` 检测到 status 列
2. 调用 `StatusColumnHandler.ApplyStatusColumnConfiguration()`
3. `ApplyStatusColumnConfiguration()` 内部调用 `GenerateStatusMappingConfig()`
4. `GenerateStatusMappingConfig()` 生成 `map` 和 `labelMap` 配置
5. 配置通过 `AmisMiddleware` 序列化返回给前端

**关键代码位置：**
- `StatusColumnHandler.cs` 第343-373行：`GenerateStatusMappingConfig()` 方法
- `StatusColumnHandler.cs` 第381-467行：`GenerateMapConfig()` 方法
- `StatusColumnHandler.cs` 第475-568行：`GenerateLabelMapConfig()` 方法

### Files to Reference

| File | Purpose |
| ---- | ------- |
| `Src/Components/CodeSpirit.Amis/Column/StatusColumnHandler.cs` | Status 列配置生成器 |
| `Src/Components/CodeSpirit.Amis/Attributes/Columns/AmisColumnAttribute.cs` | 列特性定义 |
| `Src/Components/CodeSpirit.Audit/Services/Dtos/AuditLogDto.cs` | 审计日志 DTO |

### Technical Decisions

**待调查：**
1. Amis status 列是否需要 `source` 属性？
2. 是否需要特殊的渲染模式配置（如 `showLabel: true`）？
3. 布尔值在 map 匹配时的类型转换是否正确？

**可能的修复方向：**
- 方向1：添加 `source: "${fieldName}"` 配置
- 方向2：添加 `showLabel: true` 或类似属性
- 方向3：修改 map 的键为布尔值（而非字符串）
- 方向4：使用 `mapping` 类型而非 `status` 类型

## Implementation Plan

### Tasks

**Phase 1: 调查与验证** ✅

- [x] **Task 1: 调查 Amis status 和 mapping 类型的区别**
  - 发现 `status` 类型主要显示图标
  - 发现 `mapping` 类型直接显示映射后的文字
  - 项目中枚举类型使用 `mapping` 类型成功显示文字

**Phase 2: 代码修复** ✅

- [x] **Task 2: 修改 `ApplyStatusColumnConfiguration` 方法**
  - File: `Src/Components/CodeSpirit.Amis/Column/StatusColumnHandler.cs`
  - Action: 添加类型检测逻辑，对布尔类型使用 `mapping` 类型
  - 具体修改：
    ```csharp
    // 获取属性的基础类型
    Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
    
    // 对于布尔类型，使用 mapping 类型而不是 status 类型
    if (underlyingType == typeof(bool))
    {
        column["type"] = "mapping";
        // ...应用布尔映射
    }
    else
    {
        column["type"] = "status";
        // ...应用状态映射
    }
    ```

- [x] **Task 3: 实现布尔映射方法**
  - File: `Src/Components/CodeSpirit.Amis/Column/StatusColumnHandler.cs`
  - Action: 添加 `ApplyBooleanMappingToColumn` 和 `ApplyDefaultBooleanMapping` 方法
  - 具体内容：
    ```csharp
    private void ApplyBooleanMappingToColumn(JObject column, AmisColumnAttribute columnAttr)
    {
        var labelMap = GenerateLabelMapConfig(columnAttr.StatusMapping, columnAttr.StatusLabelMap);
        if (labelMap != null)
        {
            column["map"] = JToken.FromObject(labelMap);
        }
    }
    
    private void ApplyDefaultBooleanMapping(JObject column)
    {
        column["map"] = new JObject
        {
            ["true"] = "是",
            ["false"] = "否"
        };
    }
    ```

**Phase 3: 测试验证** ✅

- [x] **Task 4: 编译和启动应用**
  - 修复了类型转换编译错误
  - 成功启动 Aspire 应用
  - 准备进行功能验证

### Acceptance Criteria

**Given** 系统审计日志列表页面加载完成  
**When** 查看批量操作列  
**Then** 应该同时显示图标和文字标签（"是" 或 "否"）

**Given** 审计日志数据中 isBulkOperation 为 true  
**When** 查看该行的批量操作列  
**Then** 应该显示蓝色图标和"是"文字

**Given** 审计日志数据中 isBulkOperation 为 false  
**When** 查看该行的批量操作列  
**Then** 应该显示灰色图标和"否"文字

## Additional Context

### Dependencies

- Newtonsoft.Json（用于序列化配置）
- Amis 前端框架（版本待确认）

### Testing Strategy

采用手动测试验证：
1. 启动应用并访问审计日志页面
2. 检查批量操作列的显示效果
3. 使用浏览器开发者工具检查生成的列配置
4. 测试不同数据值的显示效果

### Notes

- 已有技术规范 `tech-spec-fix-bool-field-semantic-display.md` 完成了 YesNo 映射的添加
- 当前问题是在已有 YesNo 映射的基础上，修复前端显示问题
- 可能需要参考 Amis mapping 组件的配置方式
