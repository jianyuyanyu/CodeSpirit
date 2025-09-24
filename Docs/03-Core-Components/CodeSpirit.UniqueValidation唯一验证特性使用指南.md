# CodeSpirit.UniqueValidation 唯一验证特性使用指南

## 概述

CodeSpirit.UniqueValidation 提供了一个统一的唯一性验证特性 `UniqueAttribute`，可以自动验证字段值在数据库中的唯一性，由于DbContext已启用租户筛选器，会自动处理多租户数据隔离。

## 核心组件

### 1. UniqueAttribute 特性

位于 `CodeSpirit.Core.Attributes.UniqueAttribute`，用于标记需要进行唯一性验证的属性。

### 2. IUniqueValidationService 接口

位于 `CodeSpirit.Core.IUniqueValidationService`，定义了唯一性验证服务的接口。

### 3. UniqueValidationService 实现

位于 `CodeSpirit.Shared.Services.UniqueValidationService`，提供了唯一性验证的具体实现。

## 使用方法

### 1. 服务注册

在 `Program.cs` 或 `Startup.cs` 中注册唯一性验证服务：

```csharp
using CodeSpirit.Shared.Extensions;

// 在服务注册区域添加
builder.Services.AddUniqueValidation();
```

### 2. 在 DTO 中使用

在需要验证唯一性的属性上添加 `[Unique]` 特性：

```csharp
using CodeSpirit.Core.Attributes;
using CodeSpirit.ApprovalApi.Models;

public class CreateWorkflowDefinitionDto
{
    /// <summary>
    /// 工作流名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [Unique(typeof(WorkflowDefinition))]
    [DisplayName("工作流名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码
    /// </summary>
    [Required]
    [StringLength(50)]
    [Unique(typeof(WorkflowDefinition))]
    [DisplayName("工作流代码")]
    public string Code { get; set; } = string.Empty;
}
```

### 3. 在更新 DTO 中使用

对于包含 ID 的更新 DTO，验证时会自动排除当前实体：

```csharp
public class WorkflowDefinitionDiffDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工作流名称
    /// </summary>
    [StringLength(100)]
    [Unique(typeof(WorkflowDefinition))]
    [DisplayName("工作流名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码
    /// </summary>
    [StringLength(50)]
    [Unique(typeof(WorkflowDefinition))]
    [DisplayName("工作流代码")]
    public string? Code { get; set; }
}
```

## UniqueAttribute 参数说明

### 构造函数参数

- `entityType` (Type, 必需): 要验证的实体类型

### 属性参数

- `FieldName` (string, 可选): 指定验证的字段名，默认使用属性名
- `IgnoreCase` (bool, 可选): 是否忽略大小写，默认为 false

## 使用示例

### 基本用法

```csharp
[Unique(typeof(User))]
public string Username { get; set; }
```

### 指定字段名

```csharp
[Unique(typeof(User), FieldName = "EmailAddress")]
public string Email { get; set; }
```

### 忽略大小写

```csharp
[Unique(typeof(User), IgnoreCase = true)]
public string Username { get; set; }
```


### 完整配置示例

```csharp
[Unique(typeof(WorkflowDefinition), 
        FieldName = "Code", 
        IgnoreCase = false)]
public string WorkflowCode { get; set; }
```

## 工作机制

### 1. 验证流程

1. 当模型验证运行时，`UniqueAttribute` 会被触发
2. 特性通过依赖注入获取 `IUniqueValidationService` 实例
3. 服务获取当前的 DbContext（已启用租户筛选器）
4. 构建查询表达式，包含字段值比较、ID排除等条件
5. 执行数据库查询，检查是否存在重复值（租户筛选器自动处理多租户隔离）
6. 返回验证结果

### 2. 多租户支持

由于DbContext已经启用了租户筛选器，所有查询都会自动按当前租户过滤，无需额外配置。

### 3. 更新时排除自身

对于包含 `Id` 属性的 DTO，验证时会自动排除当前实体：

```csharp
// 自动添加的条件
entity.Id != currentDto.Id
```

## 错误信息

验证失败时会返回格式化的错误消息：

```
"{DisplayName}"{value}"已存在，请使用其他值
```

例如：
- 工作流名称"请假审批流程"已存在，请使用其他值
- 工作流代码"LEAVE_APPROVAL"已存在，请使用其他值

## 注意事项

### 1. 性能考虑

- 每次验证都会执行数据库查询，建议在必要的字段上使用
- 验证是同步执行的，避免在高频操作中过度使用

### 2. 数据库连接

- 服务会自动从依赖注入容器获取当前的 DbContext
- 确保相关的 DbContext 已正确注册到依赖注入容器

### 3. 错误处理

- 验证异常会被捕获并转换为友好的错误消息
- 如果数据库连接失败，会返回验证失败的结果

### 4. 数据库支持

服务会自动从依赖注入容器中获取当前上下文的 DbContext，无需手动配置映射关系。

## 扩展开发

### 自定义验证逻辑

可以继承 `UniqueAttribute` 实现自定义的验证逻辑：

```csharp
public class CustomUniqueAttribute : UniqueAttribute
{
    public CustomUniqueAttribute(Type entityType) : base(entityType)
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // 添加自定义验证逻辑
        // ...
        
        return base.IsValid(value, validationContext);
    }
}
```

## 总结

`UniqueAttribute` 提供了一个简单而强大的方式来实现字段唯一性验证，由于DbContext已启用租户筛选器，能够自动处理多租户数据隔离，同时支持自定义字段名、大小写敏感等配置选项。通过统一的验证机制，可以有效减少重复代码，提高开发效率。
