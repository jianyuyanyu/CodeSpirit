using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Core.Attributes;

/// <summary>
/// 唯一性验证特性
/// 用于验证字段值在数据库中的唯一性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class UniqueAttribute : ValidationAttribute
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// 字段名称（默认使用属性名）
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// 是否忽略大小写（默认为false）
    /// </summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>
    /// 初始化唯一性验证特性
    /// </summary>
    /// <param name="entityType">实体类型</param>
    public UniqueAttribute(Type entityType)
    {
        EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
    }

    /// <summary>
    /// 验证方法
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>验证结果</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // 如果值为空，则跳过唯一性验证（由Required特性处理）
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        try
        {
            // 获取唯一性验证服务
            var uniqueValidationService = validationContext.GetRequiredService<IUniqueValidationService>();

            // 获取字段名（如果没有指定则使用属性名）
            string fieldName = FieldName ?? validationContext.MemberName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return new ValidationResult("无法确定字段名称");
            }

            // 获取当前实体的ID（用于更新时排除自身）
            long? currentId = GetCurrentEntityId(validationContext);

            // 执行唯一性验证
            var isUnique = uniqueValidationService.IsUniqueAsync(
                EntityType,
                fieldName,
                value.ToString()!,
                currentId,
                IgnoreCase
            ).GetAwaiter().GetResult();

            if (!isUnique)
            {
                string displayName = GetDisplayName(validationContext);
                return new ValidationResult($"{displayName}\"{value}\"已存在，请使用其他值");
            }

            return ValidationResult.Success;
        }
        catch (Exception ex)
        {
            // 记录异常但不阻止验证流程
            return new ValidationResult($"唯一性验证失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前实体的ID
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>实体ID，如果是新建则返回null</returns>
    private static long? GetCurrentEntityId(ValidationContext validationContext)
    {
        var instance = validationContext.ObjectInstance;
        if (instance == null) return null;

        // 尝试从DTO中获取ID属性
        var idProperty = instance.GetType().GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(long))
        {
            var idValue = (long?)idProperty.GetValue(instance);
            return idValue > 0 ? idValue : null;
        }

        // 如果没有找到ID属性，尝试从路由或其他地方获取
        if (validationContext.Items.TryGetValue("EntityId", out var entityIdObj) && 
            entityIdObj is long entityId && entityId > 0)
        {
            return entityId;
        }

        return null;
    }

    /// <summary>
    /// 获取字段的显示名称
    /// </summary>
    /// <param name="validationContext">验证上下文</param>
    /// <returns>显示名称</returns>
    private static string GetDisplayName(ValidationContext validationContext)
    {
        return validationContext.DisplayName ?? validationContext.MemberName ?? "字段";
    }
}
