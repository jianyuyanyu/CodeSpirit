using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeSpirit.Core;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// 唯一性验证服务实现
/// </summary>
public class UniqueValidationService : IUniqueValidationService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public UniqueValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 验证字段值的唯一性
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="value">字段值</param>
    /// <param name="excludeId">排除的实体ID（用于更新时排除自身）</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns>如果唯一则返回true，否则返回false</returns>
    public async Task<bool> IsUniqueAsync(
        Type entityType,
        string fieldName,
        string value,
        long? excludeId = null,
        bool ignoreCase = false)
    {
        try
        {
            // 获取对应的DbContext
            var dbContext = GetDbContext(entityType);
            if (dbContext == null)
            {
                throw new InvalidOperationException($"无法找到实体类型 {entityType.Name} 对应的DbContext");
            }

            // 获取DbSet
            var dbSetProperty = GetDbSetProperty(dbContext, entityType);
            if (dbSetProperty == null)
            {
                throw new InvalidOperationException($"无法找到实体类型 {entityType.Name} 对应的DbSet属性");
            }

            var dbSet = dbSetProperty.GetValue(dbContext);
            if (dbSet == null)
            {
                throw new InvalidOperationException($"DbSet for {entityType.Name} is null");
            }

            // 构建查询表达式
            var queryExpression = BuildUniqueQueryExpression(
                entityType, 
                fieldName, 
                value, 
                excludeId, 
                ignoreCase);

            // 执行查询 - 使用泛型方法调用
            var exists = await ExecuteAnyAsyncGeneric(dbSet, queryExpression, entityType);

            return !exists; // 如果不存在则唯一
        }
        catch (Exception ex)
        {
            throw new AppServiceException(500, $"唯一性验证失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取对应的DbContext
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>DbContext实例</returns>
    private DbContext? GetDbContext(Type entityType)
    {
        // 根据实体类型的命名空间推断对应的DbContext
        var entityNamespace = entityType.Namespace;
        if (string.IsNullOrEmpty(entityNamespace))
        {
            return null;
        }

        return _serviceProvider.GetService<DbContext>();
    }

    /// <summary>
    /// 获取DbSet属性
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="entityType">实体类型</param>
    /// <returns>DbSet属性信息</returns>
    private static PropertyInfo? GetDbSetProperty(DbContext dbContext, Type entityType)
    {
        return dbContext.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                p.PropertyType.GetGenericArguments()[0] == entityType);
    }

    /// <summary>
    /// 构建唯一性查询表达式
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="fieldName">字段名称</param>
    /// <param name="value">字段值</param>
    /// <param name="excludeId">排除的ID</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns>查询表达式</returns>
    private LambdaExpression BuildUniqueQueryExpression(
        Type entityType,
        string fieldName,
        string value,
        long? excludeId,
        bool ignoreCase)
    {
        var parameter = Expression.Parameter(entityType, "e");
        Expression? predicate = null;

        // 构建字段值比较表达式
        var fieldProperty = entityType.GetProperty(fieldName);
        if (fieldProperty == null)
        {
            throw new ArgumentException($"实体类型 {entityType.Name} 中不存在属性 {fieldName}");
        }

        var fieldAccess = Expression.Property(parameter, fieldProperty);
        Expression fieldComparison;

        if (ignoreCase && fieldProperty.PropertyType == typeof(string))
        {
            // 忽略大小写的字符串比较
            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
            var fieldToLower = Expression.Call(fieldAccess, toLowerMethod!);
            var valueToLower = Expression.Constant(value.ToLower());
            fieldComparison = Expression.Equal(fieldToLower, valueToLower);
        }
        else
        {
            // 直接比较
            var valueConstant = Expression.Constant(value, fieldProperty.PropertyType);
            fieldComparison = Expression.Equal(fieldAccess, valueConstant);
        }

        predicate = fieldComparison;

        // 如果需要排除特定ID
        if (excludeId.HasValue)
        {
            var idProperty = entityType.GetProperty("Id");
            if (idProperty != null)
            {
                var idAccess = Expression.Property(parameter, idProperty);
                var idValue = Expression.Constant(excludeId.Value, idProperty.PropertyType);
                var idNotEqual = Expression.NotEqual(idAccess, idValue);
                predicate = Expression.AndAlso(predicate, idNotEqual);
            }
        }

        // 创建Lambda表达式
        var delegateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        return Expression.Lambda(delegateType, predicate, parameter);
    }

    /// <summary>
    /// 执行AnyAsync查询 - 使用泛型方法
    /// </summary>
    /// <param name="dbSet">DbSet对象</param>
    /// <param name="queryExpression">查询表达式</param>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否存在匹配的记录</returns>
    private async Task<bool> ExecuteAnyAsyncGeneric(object dbSet, LambdaExpression queryExpression, Type entityType)
    {
        try
        {
            // 使用反射调用泛型方法
            var method = GetType().GetMethod(nameof(ExecuteAnyAsyncTyped), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method!.MakeGenericMethod(entityType);
            var task = (Task<bool>)genericMethod.Invoke(this, new object[] { dbSet, queryExpression })!;
            return await task;
        }
        catch (Exception ex)
        {
            throw new AppServiceException(500, $"执行唯一性查询失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 执行AnyAsync查询的泛型实现
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="dbSet">DbSet对象</param>
    /// <param name="queryExpression">查询表达式</param>
    /// <returns>是否存在匹配的记录</returns>
    private async Task<bool> ExecuteAnyAsyncTyped<T>(object dbSet, LambdaExpression queryExpression) where T : class
    {
        try
        {
            var typedDbSet = (DbSet<T>)dbSet;
            var typedExpression = (Expression<Func<T, bool>>)queryExpression;
            return await typedDbSet.AnyAsync(typedExpression);
        }
        catch (Exception ex)
        {
            throw new AppServiceException(500, $"执行类型化查询失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 执行AnyAsync查询 - 备用反射方法
    /// </summary>
    /// <param name="dbSet">DbSet对象</param>
    /// <param name="queryExpression">查询表达式</param>
    /// <param name="entityType">实体类型</param>
    /// <returns>是否存在匹配的记录</returns>
    private async Task<bool> ExecuteAnyAsync(object dbSet, LambdaExpression queryExpression, Type entityType)
    {
        try
        {
            // 构建IQueryable<T>类型
            var queryableType = typeof(IQueryable<>).MakeGenericType(entityType);
            var predicateType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(entityType, typeof(bool)));
            
            // 查找AnyAsync方法：AnyAsync<TSource>(IQueryable<TSource>, Expression<Func<TSource, bool>>)
            var anyAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .Where(m => m.Name == "AnyAsync")
                .Where(m => m.IsGenericMethodDefinition)
                .Where(m => m.GetParameters().Length == 2)
                .FirstOrDefault(m => 
                {
                    var parameters = m.GetParameters();
                    var firstParam = parameters[0].ParameterType;
                    var secondParam = parameters[1].ParameterType;
                    
                    return firstParam.IsGenericType &&
                           firstParam.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                           secondParam.IsGenericType &&
                           secondParam.GetGenericTypeDefinition() == typeof(Expression<>);
                });

            if (anyAsyncMethod == null)
            {
                throw new InvalidOperationException("无法找到合适的AnyAsync方法");
            }

            // 创建泛型方法
            var genericMethod = anyAsyncMethod.MakeGenericMethod(entityType);
            
            // 调用方法
            var task = (Task<bool>)genericMethod.Invoke(null, new object[] { dbSet, queryExpression })!;
            return await task;
        }
        catch (Exception ex)
        {
            throw new AppServiceException(500, $"执行唯一性查询失败：{ex.Message}");
        }
    }
}
