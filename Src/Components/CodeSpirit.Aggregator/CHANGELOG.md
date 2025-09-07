# CodeSpirit.Aggregator 更新日志

## 版本 2.0.0 - 全局聚合器注册功能

### 新增功能

#### 🎉 全局聚合器注册系统
- **IGlobalAggregatorConfigurationService**: 全局聚合器配置服务接口
- **GlobalAggregatorConfigurationService**: 全局聚合器配置服务实现
- **全局规则注册**: 支持为特定字段名注册全局聚合规则
- **自动应用**: 对于没有 `AggregateFieldAttribute` 特性的属性，自动检查并应用全局规则

#### 🔧 增强的服务注册
- **AddCodeSpiritAggregator**: 支持配置委托参数，可在服务注册时配置全局规则
- **ConfigureCommonGlobalRules**: 预定义的常用全局规则配置扩展方法
- **优先级机制**: `AggregateFieldAttribute` 特性优先级高于全局规则

#### 📝 预定义规则
- **CreatedBy**: 自动从用户服务获取创建者姓名
- **UpdatedBy**: 自动从用户服务获取更新者姓名
- **UserId**: 自动从用户服务获取用户姓名

### 使用示例

#### 基本配置
```csharp
// 使用预定义规则
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    globalConfig.ConfigureCommonGlobalRules();
});

// 自定义规则
builder.Services.AddCodeSpiritAggregator(globalConfig =>
{
    globalConfig.RegisterGlobalRule(
        "CreatedBy", 
        "http://identity/api/identity/internal/users/{value}.data.name", 
        "{field}");
});
```

#### DTO 类简化
```csharp
// 之前需要为每个字段添加特性
public class DocumentDto
{
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string CreatedBy { get; set; }
    
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public string UpdatedBy { get; set; }
}

// 现在可以自动应用全局规则
public class DocumentDto
{
    // 自动应用全局规则，无需特性
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}
```

### 技术实现

#### 核心组件
1. **IGlobalAggregatorConfigurationService**: 管理全局聚合规则的接口
2. **GlobalAggregatorConfigurationService**: 使用 `ConcurrentDictionary` 实现线程安全的规则存储
3. **AggregationHeaderService**: 扩展以支持全局规则检查和应用
4. **ServiceCollectionExtensions**: 提供便捷的服务注册和配置方法

#### 工作流程
1. **服务注册**: 在应用启动时注册全局聚合器服务并配置规则
2. **规则收集**: `AggregationHeaderService` 收集特性规则和全局规则
3. **优先级处理**: 特性规则优先，全局规则作为补充
4. **头部生成**: 合并所有规则生成最终的聚合头部

### 向后兼容性

- ✅ 完全向后兼容现有的 `AggregateFieldAttribute` 特性
- ✅ 现有代码无需修改即可继续工作
- ✅ 特性规则优先级高于全局规则，不会产生冲突

### 性能优化

- 🚀 使用 `ConcurrentDictionary` 确保线程安全和高性能
- 🚀 全局规则在应用启动时配置，运行时查询高效
- 🚀 智能规则合并，避免重复处理

### 文档和示例

- 📚 完整的 README.md 使用指南
- 📚 更新的聚合器使用指南文档
- 📚 丰富的代码示例和演示程序
- 📚 单元测试覆盖核心功能

### 未来规划

- 🔮 支持基于条件的动态规则
- 🔮 支持规则继承和组合
- 🔮 支持规则的热更新
- 🔮 支持更复杂的字段匹配模式

---

这个版本大大简化了聚合器的使用，特别是对于常用字段（如 CreatedBy、UpdatedBy）的处理，开发者不再需要在每个 DTO 类中重复添加相同的特性，提高了开发效率和代码的可维护性。
