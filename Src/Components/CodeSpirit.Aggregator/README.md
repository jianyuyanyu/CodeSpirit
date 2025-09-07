# CodeSpirit.Aggregator 全局聚合器功能

## 概述

CodeSpirit.Aggregator 现已支持全局聚合器注册功能，允许开发者为特定字段名（如 `CreatedBy`、`UpdatedBy` 等）自动配置聚合规则，无需在每个 DTO 类中重复添加 `AggregateFieldAttribute` 特性。

## 功能特性

- **全局规则注册**：为常用字段（如 CreatedBy、UpdatedBy）注册全局聚合规则
- **自动应用**：对于没有 `AggregateFieldAttribute` 特性的属性，自动检查并应用全局规则
- **优先级机制**：`AggregateFieldAttribute` 特性优先级高于全局规则
- **灵活配置**：支持自定义数据源和模板

## 使用方式

### 1. 基本配置

在 `Program.cs` 或 `Startup.cs` 中注册聚合器服务并配置全局规则：

```csharp
// 方式一：使用预定义的常用规则
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    globalConfig.ConfigureCommonGlobalRules();
});

// 方式二：自定义全局规则
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    // 配置CreatedBy字段的全局规则
    globalConfig.RegisterGlobalRule(
        "CreatedBy", 
        "http://identity/api/identity/internal/users/{value}.data.name", 
        "{field}");
    
    // 配置UpdatedBy字段的全局规则
    globalConfig.RegisterGlobalRule(
        "UpdatedBy", 
        "http://identity/api/identity/internal/users/{value}.data.name", 
        "{field}");
});
```

### 2. DTO 类使用示例

有了全局规则后，DTO 类中的 `CreatedBy` 和 `UpdatedBy` 字段将自动应用聚合规则：

```csharp
public class DocumentDto
{
    public string Id { get; set; }
    
    public string Title { get; set; }
    
    // 这个字段将自动应用全局聚合规则
    // 无需添加 [AggregateField] 特性
    public string CreatedBy { get; set; }
    
    // 这个字段也将自动应用全局聚合规则
    public string UpdatedBy { get; set; }
    
    // 如果需要特殊处理，仍可使用特性覆盖全局规则
    [AggregateField(dataSource: "/api/custom/{value}.displayName", template: "自定义: {field}")]
    public string CustomField { get; set; }
}
```

### 3. 预定义的常用规则

`ConfigureCommonGlobalRules()` 扩展方法提供了以下预定义规则：

- **CreatedBy**: 从用户服务获取创建者姓名
- **UpdatedBy**: 从用户服务获取更新者姓名  
- **UserId**: 从用户服务获取用户姓名

所有规则都使用以下配置：
- 数据源：`http://identity/api/identity/internal/users/{value}.data.name`
- 模板：`{field}`（直接显示用户姓名）

### 4. 高级配置

#### 动态注册规则

```csharp
// 在运行时动态注册规则
public class SomeService
{
    private readonly IGlobalAggregatorConfigurationService _globalConfig;
    
    public SomeService(IGlobalAggregatorConfigurationService globalConfig)
    {
        _globalConfig = globalConfig;
    }
    
    public void ConfigureCustomRules()
    {
        // 注册新的全局规则
        _globalConfig.RegisterGlobalRule(
            "DepartmentId", 
            "/api/departments/{value}.name", 
            "部门: {field}");
        
        // 移除现有规则
        _globalConfig.RemoveGlobalRule("UserId");
        
        // 获取所有规则
        var allRules = _globalConfig.GetAllGlobalRules();
    }
}
```

#### 自定义规则模板

```csharp
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    // 静态模板（不需要数据源）
    globalConfig.RegisterGlobalRule(
        "Status", 
        null, 
        "状态: {value}");
    
    // 动态替换（替换原值）
    globalConfig.RegisterGlobalRule(
        "CategoryId", 
        "/api/categories/{value}.name", 
        null);
    
    // 动态补充（保留原值并添加描述）
    globalConfig.RegisterGlobalRule(
        "ProductId", 
        "/api/products/{value}.name", 
        "{value} ({field})");
});
```

## 工作原理

1. **规则收集**：`AggregationHeaderService` 在生成聚合头部时，首先收集带有 `AggregateFieldAttribute` 特性的属性
2. **全局规则检查**：对于没有特性的属性，检查是否存在匹配的全局规则
3. **优先级处理**：特性规则优先级高于全局规则，不会重复应用
4. **规则生成**：将所有规则合并生成最终的聚合头部

## 注意事项

- 全局规则仅对没有 `AggregateFieldAttribute` 特性的属性生效
- 字段名匹配不区分大小写
- 全局规则在应用启动时配置，运行时修改需要重新注入服务
- 建议将常用的全局规则配置在应用启动时，避免运行时频繁修改

## 迁移指南

### 从特性方式迁移到全局规则

**迁移前：**
```csharp
public class DocumentDto
{
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string CreatedBy { get; set; }
    
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string UpdatedBy { get; set; }
}
```

**迁移后：**
```csharp
// 在 Program.cs 中配置全局规则
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    globalConfig.ConfigureCommonGlobalRules();
});

// DTO 类简化
public class DocumentDto
{
    // 自动应用全局规则，无需特性
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}
```

这样可以大大减少重复代码，提高开发效率。
