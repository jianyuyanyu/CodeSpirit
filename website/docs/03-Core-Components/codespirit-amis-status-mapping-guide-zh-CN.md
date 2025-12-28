# CodeSpirit.Amis 状态映射功能使用指南

## 📋 概述

本文档介绍了 CodeSpirit.Amis 组件中的状态映射功能，该功能基于 [Amis Status 组件](https://aisuda.bce.baidu.com/amis/zh-CN/components/status) 规范，为前端提供丰富的状态展示效果。

## 🆕 核心组件

### 1. AmisStatusColumnAttribute 专用特性

新增了专门的 `AmisStatusColumnAttribute` 特性，继承自 `AmisColumnAttribute`，专门用于配置状态列：

```csharp
[AmisStatusColumn(StatusMapping.Boolean)]
public bool IsActive { get; set; }
```

**特点：**
- 自动设置 `Type = "status"`
- 提供多种构造函数和工厂方法
- 简化状态列配置

### 2. StatusColumnHandler 处理器

专门的状态列处理器类，负责：
- 智能识别状态字段
- 应用状态映射配置
- 生成前端配置

### 3. AmisColumnAttribute 扩展

扩展了 `AmisColumnAttribute` 特性，新增以下状态映射相关属性：

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `StatusMapping` | `StatusMapping` | 预定义状态映射类型 |
| `CustomStatusMap` | `string` | 自定义状态映射（JSON格式） |
| `StatusLabelMap` | `string` | 状态标签文本映射（JSON格式） |
| `ShowStatusIcon` | `bool` | 是否显示状态图标（默认true） |
| `StatusPlaceholder` | `string` | 状态列占位符文本（默认"-"） |

### 预定义状态映射类型

#### 1. HttpStatusCode - HTTP状态码映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.HttpStatusCode)]
public int StatusCode { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.HttpStatusCode()]
public int StatusCode { get; set; }

// 传统用法（仍然支持）
[AmisColumn(Type = "status", StatusMapping = StatusMapping.HttpStatusCode)]
public int StatusCode { get; set; }
```

**映射规则：**
- `2xx` → `success` (绿色) - 成功状态
- `3xx` → `info` (蓝色) - 重定向状态  
- `4xx` → `warning` (橙色) - 客户端错误
- `5xx` → `danger` (红色) - 服务器错误

#### 2. Boolean - 布尔值映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.Boolean)]
public bool IsSuccess { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.Boolean()]
public bool IsSuccess { get; set; }
```

**映射规则：**
- `true/1/yes/on/enabled` → `success` (成功)
- `false/0/no/off/disabled` → `fail` (失败)

#### 3. AuditOperationType - 审计操作类型映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.AuditOperationType)]
public string OperationType { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.AuditOperationType()]
public string OperationType { get; set; }
```

**映射规则：**
- `create/add/insert/login` → `success` (绿色)
- `update/modify/edit/import/export/upload/setting` → `info` (蓝色)
- `delete/remove` → `danger` (红色)
- `query/select/read/get/download` → `default` (默认)
- `batch/authorize` → `warning` (橙色)
- `logout` → `info` (蓝色)

#### 4. CommonStatus - 通用状态映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.CommonStatus)]
public string Status { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.CommonStatus()]
public string Status { get; set; }
```

**映射规则：**
- `active/enabled/success/completed/approved` → `success`
- `inactive/disabled/fail/failed/rejected` → `fail`
- `pending/processing/running/in-progress` → `info`
- `warning/caution` → `warning`
- `error/danger/critical` → `danger`
- `draft/new` → `default`
- `cancelled/canceled` → `secondary`

#### 5. HttpMethod - HTTP请求方法映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.HttpMethod)]
public string RequestMethod { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.HttpMethod()]
public string RequestMethod { get; set; }
```

**映射规则：**
- `GET` → `info` (蓝色) - 查询操作
- `POST` → `success` (绿色) - 创建操作
- `PUT` → `warning` (橙色) - 更新操作
- `DELETE` → `danger` (红色) - 删除操作
- `PATCH` → `warning` (橙色) - 部分更新
- `HEAD/OPTIONS` → `default` (默认) - 其他操作

#### 6. NumericStatus - 数字状态映射
```csharp
// 推荐用法 - 使用专用特性
[AmisStatusColumn(StatusMapping.NumericStatus)]
public int Status { get; set; }

// 或者使用工厂方法
[AmisStatusColumn.NumericStatus()]
public int Status { get; set; }
```

**映射规则：**
- `1` → `success`
- `0` → `fail`
- `-1` → `warning`
- `2` → `info`

## 🎨 使用示例

### 基础用法

```csharp
public class UserDto
{
    /// <summary>
    /// 用户状态
    /// </summary>
    [DisplayName("用户状态")]
    [AmisStatusColumn(StatusMapping.Boolean)]
    public bool IsActive { get; set; }

    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("状态码")]
    [AmisStatusColumn(StatusMapping.HttpStatusCode)]
    public int StatusCode { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [DisplayName("请求方法")]
    [AmisStatusColumn.HttpMethod()]
    public string RequestMethod { get; set; }
}
```

### 工厂方法用法

```csharp
public class SystemStatusDto
{
    [AmisStatusColumn.Boolean()]
    public bool IsOnline { get; set; }

    [AmisStatusColumn.HttpStatusCode()]
    public int HealthStatus { get; set; }

    [AmisStatusColumn.CommonStatus()]
    public string ServiceStatus { get; set; }

    [AmisStatusColumn.AuditOperationType()]
    public string LastOperation { get; set; }

    [AmisStatusColumn.NumericStatus()]
    public int Priority { get; set; }
}
```

### 自定义状态映射

```csharp
public class OrderDto
{
    /// <summary>
    /// 订单状态 - 使用专用特性
    /// </summary>
    [DisplayName("订单状态")]
    [AmisStatusColumn(
        customStatusMap: "{\"pending\":\"info\",\"paid\":\"success\",\"cancelled\":\"danger\",\"refunded\":\"warning\"}",
        customLabelMap: "{\"info\":\"待支付\",\"success\":\"已支付\",\"danger\":\"已取消\",\"warning\":\"已退款\"}"
    )]
    public string OrderStatus { get; set; }

    /// <summary>
    /// 订单状态 - 使用工厂方法
    /// </summary>
    [DisplayName("订单状态")]
    [AmisStatusColumn.Custom(
        "{\"pending\":\"info\",\"paid\":\"success\",\"cancelled\":\"danger\",\"refunded\":\"warning\"}",
        "{\"info\":\"待支付\",\"success\":\"已支付\",\"danger\":\"已取消\",\"warning\":\"已退款\"}"
    )]
    public string OrderStatus2 { get; set; }

    /// <summary>
    /// 传统用法（仍然支持）
    /// </summary>
    [DisplayName("订单状态")]
    [AmisColumn(
        Type = "status",
        CustomStatusMap = "{\"pending\":\"info\",\"paid\":\"success\",\"cancelled\":\"danger\",\"refunded\":\"warning\"}",
        StatusLabelMap = "{\"info\":\"待支付\",\"success\":\"已支付\",\"danger\":\"已取消\",\"warning\":\"已退款\"}"
    )]
    public string OrderStatus3 { get; set; }
}
```

### 高级配置

```csharp
public class TaskDto
{
    /// <summary>
    /// 任务状态 - 使用专用特性
    /// </summary>
    [DisplayName("任务状态")]
    [AmisStatusColumn(
        StatusMapping.CommonStatus,
        ShowStatusIcon = false,
        StatusPlaceholder = "未知状态"
    )]
    public string TaskStatus { get; set; }

    /// <summary>
    /// 任务状态 - 传统用法（仍然支持）
    /// </summary>
    [DisplayName("任务状态")]
    [AmisColumn(
        Type = "status",
        StatusMapping = StatusMapping.CommonStatus,
        ShowStatusIcon = false,
        StatusPlaceholder = "未知状态"
    )]
    public string TaskStatus2 { get; set; }
}
```

### 智能推断功能

`StatusColumnHandler` 提供智能推断功能，可以根据属性名称和类型自动识别状态字段：

```csharp
public class SmartDto
{
    // 自动识别为布尔状态列
    public bool IsActive { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsValid { get; set; }

    // 自动识别为通用状态列
    public string Status { get; set; }
    public string State { get; set; }
    public string UserStatus { get; set; }

    // 自动识别为HTTP方法列（如果属性名包含method且包含request或http）
    public string RequestMethod { get; set; }
    public string HttpMethod { get; set; }

    // 自动识别为HTTP状态码列（如果属性名包含status和code）
    public int StatusCode { get; set; }
    public int HttpStatusCode { get; set; }

    // 自动识别为数字状态列
    public int Priority { get; set; }
    public int Level { get; set; }
}
```

## 🔧 StatusMappingHelper 辅助类

### 主要方法

#### GetStatusValue
```csharp
// 获取状态值
string statusValue = StatusMappingHelper.GetStatusValue(
    value: 200,
    mapping: StatusMapping.HttpStatusCode,
    customMap: null
);
// 返回: "success"
```

#### GetStatusLabel
```csharp
// 获取状态标签
string label = StatusMappingHelper.GetStatusLabel(
    statusValue: "success",
    labelMap: "{\"success\":\"成功\",\"fail\":\"失败\"}",
    originalValue: true
);
// 返回: "成功"
```

#### GenerateStatusConfig
```csharp
// 生成Amis Status组件配置
var config = StatusMappingHelper.GenerateStatusConfig(attribute, value);
// 返回: { "type": "status", "value": "success", "label": "成功" }
```

### 预定义映射获取

```csharp
// 获取HTTP状态码映射
var httpMappings = StatusMappingHelper.GetHttpStatusCodeMappings();
// 返回: Dictionary<int, string> { [200] = "success", [404] = "warning", ... }

// 获取HTTP状态码描述
var descriptions = StatusMappingHelper.GetHttpStatusCodeDescriptions();
// 返回: Dictionary<int, string> { [200] = "成功", [404] = "未找到", ... }
```

## 📊 实际应用场景

### 1. 审计日志状态展示

```csharp
public class AuditLogDto
{
    [AmisStatusColumn(StatusMapping.Boolean)]
    public bool IsSuccess { get; set; }

    [AmisStatusColumn(StatusMapping.HttpStatusCode)]
    public int StatusCode { get; set; }

    [AmisStatusColumn(StatusMapping.AuditOperationType)]
    public string OperationType { get; set; }

    [AmisStatusColumn(StatusMapping.HttpMethod)]
    public string RequestMethod { get; set; }

    [AmisStatusColumn(StatusMapping.Boolean)]
    public bool IsBulkOperation { get; set; }
}
```

### 2. 用户管理状态

```csharp
public class UserDto
{
    [AmisStatusColumn(StatusMapping.Boolean)]
    public bool IsActive { get; set; }

    [AmisStatusColumn.Custom(
        "{\"online\":\"success\",\"offline\":\"secondary\",\"busy\":\"warning\"}",
        "{\"success\":\"在线\",\"secondary\":\"离线\",\"warning\":\"忙碌\"}"
    )]
    public string OnlineStatus { get; set; }
}
```

### 3. 系统监控状态

```csharp
public class ServiceStatusDto
{
    [AmisStatusColumn(StatusMapping.CommonStatus)]
    public string ServiceStatus { get; set; }

    [AmisStatusColumn(StatusMapping.HttpStatusCode)]
    public int HealthCheckStatus { get; set; }

    [AmisStatusColumn.Custom(
        "{\"high\":\"danger\",\"medium\":\"warning\",\"low\":\"info\",\"normal\":\"success\"}",
        "{\"danger\":\"高负载\",\"warning\":\"中负载\",\"info\":\"低负载\",\"success\":\"正常\"}"
    )]
    public string LoadLevel { get; set; }
}
```

## 🎨 前端展示效果

### Status 组件样式

根据 [Amis Status 组件文档](https://aisuda.bce.baidu.com/amis/zh-CN/components/status)，不同状态值对应的视觉效果：

| 状态值 | 颜色 | 图标 | 说明 |
|--------|------|------|------|
| `success` | 绿色 | ✓ | 成功状态 |
| `fail` | 红色 | ✗ | 失败状态 |
| `info` | 蓝色 | ℹ | 信息状态 |
| `warning` | 橙色 | ⚠ | 警告状态 |
| `danger` | 红色 | ⚠ | 危险状态 |
| `default` | 灰色 | - | 默认状态 |
| `secondary` | 灰色 | - | 次要状态 |

### JSON 配置示例

```json
{
  "type": "status",
  "name": "status",
  "label": "状态",
  "map": {
    "1": "success",
    "0": "fail"
  },
  "labelMap": {
    "success": "正常",
    "fail": "异常"
  }
}
```

## 🔄 迁移指南

### 从旧版本迁移

**旧写法：**
```csharp
[AmisColumn(Type = "status")]
public bool IsSuccess { get; set; }
```

**推荐新写法：**
```csharp
// 方式1：使用专用特性
[AmisStatusColumn(StatusMapping.Boolean)]
public bool IsSuccess { get; set; }

// 方式2：使用工厂方法
[AmisStatusColumn.Boolean()]
public bool IsSuccess { get; set; }

// 方式3：传统方式（仍然支持）
[AmisColumn(Type = "status", StatusMapping = StatusMapping.Boolean)]
public bool IsSuccess { get; set; }
```

### 兼容性说明

- ✅ **完全向后兼容** - 未指定 `StatusMapping` 的字段保持原有行为
- ✅ **渐进式升级** - 可以逐步为字段添加状态映射
- ✅ **自定义优先** - `CustomStatusMap` 优先级高于预定义映射
- ✅ **智能推断** - `StatusColumnHandler` 可以自动识别常见状态字段
- ✅ **多种用法** - 支持专用特性、工厂方法和传统方式

## 🎯 最佳实践

### 1. 选择合适的映射类型
- HTTP状态码 → 使用 `AmisStatusColumn.HttpStatusCode()`
- 布尔值 → 使用 `AmisStatusColumn.Boolean()`
- HTTP请求方法 → 使用 `AmisStatusColumn.HttpMethod()`
- 审计操作类型 → 使用 `AmisStatusColumn.AuditOperationType()`
- 业务状态 → 使用 `AmisStatusColumn.CommonStatus()` 或自定义映射

### 2. 优先使用专用特性和工厂方法
```csharp
// 推荐：使用工厂方法，简洁明了
[AmisStatusColumn.Boolean()]
public bool IsEnabled { get; set; }

// 推荐：使用专用特性，支持更多配置
[AmisStatusColumn(
    StatusMapping.Boolean,
    customLabelMap: "{\"success\":\"启用\",\"fail\":\"禁用\"}"
)]
public bool IsActive { get; set; }
```

### 3. 合理使用图标和占位符
```csharp
[AmisStatusColumn(
    StatusMapping.CommonStatus,
    ShowStatusIcon = true,
    StatusPlaceholder = "状态未知"
)]
public string Status { get; set; }
```

### 4. 利用智能推断功能
```csharp
// 无需特性，自动识别为布尔状态列
public bool IsActive { get; set; }

// 无需特性，自动识别为HTTP方法列
public string RequestMethod { get; set; }

// 无需特性，自动识别为HTTP状态码列
public int StatusCode { get; set; }
```

## 🔧 实现原理

状态映射功能通过以下组件协同工作：

1. **AmisStatusColumnAttribute**: 专用状态列特性
2. **StatusColumnHandler**: 专门的状态列处理器
3. **StatusMappingHelper**: 状态映射辅助类
4. **ColumnHelper**: 列配置生成器

### 配置生成流程

```
属性定义 → AmisStatusColumnAttribute/智能推断 → StatusColumnHandler → StatusMappingHelper → Amis JSON配置
```

#### 详细处理流程：

1. **属性识别阶段**
   - `StatusColumnHandler.CanHandle()` 检查是否为状态列
   - 支持显式特性标记和智能推断两种方式

2. **配置生成阶段**
   - `StatusColumnHandler.Handle()` 处理状态列配置
   - 调用 `StatusMappingHelper` 生成映射配置
   - 应用自定义配置（优先级更高）

3. **智能推断逻辑**
   - 布尔类型属性自动识别为布尔状态列
   - 包含"status"、"state"的字符串属性识别为通用状态列
   - 包含"method"且包含"request"或"http"的属性识别为HTTP方法列
   - 包含"status"和"code"的整数属性识别为HTTP状态码列

4. **配置合并**
   - 将状态配置合并到最终的 Amis 列 JSON 中
   - 生成 `map`、`labelMap` 等配置项

### 生成的配置示例

对于 HTTP 状态码映射，会生成如下配置：

```json
{
    "name": "statusCode",
    "label": "HTTP状态码", 
    "type": "status",
    "map": {
        "200": "success",
        "400": "warning", 
        "500": "danger"
    },
    "labelMap": {
        "success": "成功",
        "warning": "客户端错误",
        "danger": "服务器错误"
    }
}
```

## 🎉 总结

通过状态映射功能，CodeSpirit.Amis 组件现在能够：

1. **自动化状态展示** - 根据数据值自动选择合适的状态样式
2. **标准化视觉效果** - 统一的状态颜色和图标规范
3. **灵活的自定义** - 支持业务特定的状态映射
4. **友好的用户体验** - 直观的状态标签和视觉反馈
5. **智能推断能力** - 自动识别常见状态字段，减少配置工作
6. **多种使用方式** - 支持专用特性、工厂方法和传统方式
7. **完全向后兼容** - 渐进式升级，不影响现有代码

### 核心优势

- **🎯 专用特性**: `AmisStatusColumnAttribute` 提供更简洁的API
- **🏭 工厂方法**: 类型安全的快速配置方式
- **🧠 智能推断**: 自动识别状态字段，零配置使用
- **🔧 专门处理器**: `StatusColumnHandler` 提供专业的状态列处理
- **📊 丰富映射**: 支持HTTP状态码、布尔值、HTTP方法等多种映射类型
- **🎨 视觉统一**: 标准化的状态展示效果

这些增强使得前端状态展示更加专业、统一和用户友好！🚀
