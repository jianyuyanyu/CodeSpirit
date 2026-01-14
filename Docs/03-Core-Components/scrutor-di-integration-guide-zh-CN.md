# Scrutor依赖注入集成指南

## 📋 概述

本文档记录了CodeSpirit项目中Scrutor依赖注入框架的集成过程和使用指南。Scrutor是一个基于.NET原生DI容器的程序集扫描库，提供了自动服务注册、装饰器模式等高级功能。

## 🎯 集成目标

- **简化服务注册**：通过标记接口自动注册服务，减少样板代码
- **保持兼容性**：与现有的手动注册方式完美共存
- **提升开发效率**：减少重复的服务注册代码
- **支持高级功能**：装饰器模式、条件注册等

## 🔧 集成步骤

### 1. 添加Scrutor包引用

在`CodeSpirit.Shared`项目中添加Scrutor包：

```xml
<PackageReference Include="Scrutor" Version="4.2.2" />
```

### 2. 创建Scrutor扩展方法

在`CodeSpirit.Shared/DependencyInjection/ServiceCollectionExtensions.cs`中添加：

```csharp
/// <summary>
/// 使用Scrutor进行依赖注入自动注册
/// </summary>
/// <param name="services">IServiceCollection</param>
/// <param name="assemblies">要扫描的程序集</param>
/// <returns>IServiceCollection</returns>
public static IServiceCollection AddDependencyInjectionWithScrutor(
    this IServiceCollection services, 
    params Assembly[] assemblies)
{
    if (assemblies == null || assemblies.Length == 0)
    {
        assemblies = new[] { Assembly.GetCallingAssembly() };
    }

    // 注册 Scoped 服务
    services.Scan(scan => scan
        .FromAssemblies(assemblies)
        .AddClasses(classes => classes.AssignableTo<IScopedDependency>())
        .AsImplementedInterfaces()
        .WithScopedLifetime());

    // 注册 Transient 服务
    services.Scan(scan => scan
        .FromAssemblies(assemblies)
        .AddClasses(classes => classes.AssignableTo<ITransientDependency>())
        .AsImplementedInterfaces()
        .WithTransientLifetime());

    // 注册 Singleton 服务
    services.Scan(scan => scan
        .FromAssemblies(assemblies)
        .AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
        .AsImplementedInterfaces()
        .WithSingletonLifetime());

    return services;
}
```

### 3. 在API项目中集成

#### 3.1 IdentityApi集成示例

```csharp
public static IServiceCollection AddCustomServices(this IServiceCollection services)
{
    // 使用Scrutor自动注册标记接口的服务
    services.AddDependencyInjectionWithScrutor(Assembly.GetExecutingAssembly());

    // 手动注册特殊配置的服务（会覆盖自动注册）
    services.AddScoped<IJwtTokenHandler, JwtTokenHandler>();
    services.AddScoped<ILoginLogRepository, LoginLogRepository>();

    // 添加 DbContext 基类的解析
    services.AddScoped<DbContext>(provider =>
        provider.GetRequiredService<ApplicationDbContext>());

    // 注册泛型仓储
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // ... 其他配置
    return services;
}
```

#### 3.2 ExamApi集成示例

```csharp
public static IServiceCollection AddExamApiServices(this IServiceCollection services, IConfiguration configuration)
{
    // 使用Scrutor自动注册标记接口的服务
    services.AddDependencyInjectionWithScrutor(Assembly.GetExecutingAssembly());

    // 注册具体的解析器（没有接口的类需要手动注册）
    services.AddScoped<SingleChoiceQuestionParser>();
    services.AddScoped<MultipleChoiceQuestionParser>();
    services.AddScoped<TrueFalseQuestionParser>();
    services.AddScoped<QuestionTextParserV2>();

    // 以下服务已通过Scrutor自动注册，无需手动注册
    // services.AddScoped<IQuestionService, QuestionService>();
    // services.AddScoped<IExamSettingService, ExamSettingService>();
    // ... 其他服务

    // ... 其他配置
    return services;
}
```

## 📝 服务接口标记

### 已标记的服务接口

以下服务接口已添加了依赖注入标记：

#### IdentityApi
- `IAuthService : IScopedDependency`
- `IUserService : IScopedDependency`
- `IRoleService : IScopedDependency`
- `ITenantService : IScopedDependency`
- `ITenantDataInitializationService : IScopedDependency`
- `ILoginLogService : IScopedDependency`
- `IAuditLogService : IScopedDependency`

#### ExamApi
- `IExamSettingService : IScopedDependency`
- `IClientService : IScopedDependency`
- 其他服务接口（通过继承基础服务接口自动获得标记）

### 标记接口说明

```csharp
// 作用域注入 - 在同一个请求中是同一个实例
public interface IScopedDependency { }

// 瞬态注入 - 每次请求都创建新实例
public interface ITransientDependency { }

// 单例注入 - 整个应用程序生命周期中只有一个实例
public interface ISingletonDependency { }
```

## 🔄 迁移策略

### 阶段1：试点集成（已完成）
- ✅ 在`CodeSpirit.Shared`中添加Scrutor支持
- ✅ 在`IdentityApi`项目中试点使用
- ✅ 验证构建和功能正常

### 阶段2：逐步迁移（已完成）
- ✅ 为服务接口添加标记接口
- ✅ 在`ExamApi`项目中集成Scrutor
- ✅ 注释掉重复的手动注册

### 阶段3：优化和扩展（进行中）
- 🔄 监控运行时性能
- 📋 考虑使用装饰器模式
- 📋 实现条件注册

## 💡 最佳实践

### 1. 混合使用策略

```csharp
public static IServiceCollection AddCustomServices(this IServiceCollection services)
{
    // 1. 首先使用Scrutor自动注册
    services.AddDependencyInjectionWithScrutor(Assembly.GetExecutingAssembly());

    // 2. 然后手动注册特殊配置的服务（会覆盖自动注册）
    services.AddScoped<DbContext>(provider =>
        provider.GetRequiredService<ApplicationDbContext>());

    // 3. 注册泛型服务
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // 4. 注册需要特殊配置的服务
    services.AddScoped<IEmailService>(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
        return new EmailService(smtpSettings);
    });

    return services;
}
```

### 2. 服务接口设计

```csharp
// 推荐：继承标记接口
public interface IUserService : IBaseCRUDIService<ApplicationUser, UserDto, long, CreateUserDto, UpdateUserDto, UserBatchImportItemDto>, IScopedDependency
{
    // 服务方法定义
}

// 或者：直接继承标记接口
public interface IClientService : IScopedDependency
{
    // 服务方法定义
}
```

### 3. 条件注册示例

```csharp
// 根据环境注册不同实现
services.Scan(scan => scan
    .FromAssemblyOf<IUserService>()
    .AddClasses(classes => classes
        .Where(type => environment.IsDevelopment() 
            ? type.Name.Contains("Mock") 
            : !type.Name.Contains("Mock")))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

## 📊 集成效果

### 代码减少量
- **IdentityApi**: 减少了约15行手动注册代码
- **ExamApi**: 减少了约20行手动注册代码
- **总体**: 预计可减少30-40%的服务注册代码

### 性能影响
- **启动时间**: 基本无影响（程序集扫描缓存）
- **运行时性能**: 与手动注册完全相同
- **内存使用**: 略微增加（缓存类型信息）

### 维护性提升
- ✅ 新服务只需添加标记接口即可自动注册
- ✅ 减少了忘记注册服务的风险
- ✅ 统一的注册模式，代码更清晰

## 🚨 注意事项

### 1. 不适用的场景
- 需要特殊配置的服务（如工厂模式）
- 泛型服务（如`IRepository<>`）
- 没有接口的具体类

### 2. 调试建议
- 使用`enableLogging`参数查看注册详情
- 检查服务是否被正确注册
- 注意服务生命周期的选择

### 3. 兼容性
- 与现有手动注册完全兼容
- 手动注册会覆盖自动注册
- 支持渐进式迁移

## 🔮 未来计划

### 短期目标
- [ ] 完成所有API项目的迁移
- [ ] 添加装饰器模式支持
- [ ] 优化日志记录

### 长期目标
- [ ] 考虑AOP支持
- [ ] 实现更复杂的条件注册
- [ ] 性能监控和优化

## 📚 参考资料

- [Scrutor GitHub](https://github.com/khellang/Scrutor)
- [.NET依赖注入最佳实践](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [CodeSpirit.Core核心框架文档](../01-Core-Docs/04-codespirit-core-framework-zh-CN.md)

---

**更新日期**: 2024年12月
**维护者**: CodeSpirit开发团队
