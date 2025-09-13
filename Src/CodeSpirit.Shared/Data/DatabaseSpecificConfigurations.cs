using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CodeSpirit.Shared.Data;

/// <summary>
/// 数据库特定配置的静态工具类
/// 提供MySQL和SQL Server的通用配置方法
/// </summary>
public static class DatabaseSpecificConfigurations
{
    /// <summary>
    /// 应用MySQL特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    public static void ApplyMySqlConfigurations(ModelBuilder modelBuilder)
    {
        Console.WriteLine($"正在应用 MySQL 特定配置");
        
        // MySQL特定配置
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // DateTime类型配置为datetime(6)以支持微秒精度
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime(6)");
                    
                    // 修复SQL Server默认值语法为MySQL语法
                    var defaultValueSql = property.GetDefaultValueSql();
                    if (!string.IsNullOrEmpty(defaultValueSql))
                    {
                        if (defaultValueSql.Contains("GETUTCDATE()") || defaultValueSql.Contains("GETDATE()"))
                        {
                            property.SetDefaultValueSql("CURRENT_TIMESTAMP(6)");
                        }
                    }
                }
                
                // MySQL字符串类型配置
                if (property.ClrType == typeof(string))
                {
                    var maxLength = property.GetMaxLength();
                    
                    // 特殊处理Identity相关字段，确保它们有合适的长度以支持索引
                    var propertyName = property.Name;
                    if (propertyName == "NormalizedName" || propertyName == "NormalizedUserName" || 
                        propertyName == "NormalizedEmail" || propertyName == "ConcurrencyStamp")
                    {
                        if (!maxLength.HasValue || maxLength.Value > 256)
                        {
                            property.SetMaxLength(256);
                            maxLength = 256;
                        }
                    }
                    
                    if (maxLength.HasValue && maxLength.Value > 255)
                    {
                        // 长文本使用TEXT类型，但Identity字段限制在256以内
                        if (maxLength.Value <= 1000)
                        {
                            property.SetColumnType($"varchar({maxLength.Value}) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                        }
                        else
                        {
                            property.SetColumnType("text");
                        }
                    }
                    else
                    {
                        // 短文本使用VARCHAR并设置字符集
                        property.SetColumnType($"varchar({maxLength ?? 255}) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                    }
                }
                
                // MySQL布尔类型配置
                if (property.ClrType == typeof(bool) || property.ClrType == typeof(bool?))
                {
                    property.SetColumnType("tinyint(1)");
                }
            }
        }
    }

    /// <summary>
    /// 应用SQL Server特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    public static void ApplySqlServerConfigurations(ModelBuilder modelBuilder)
    {
        Console.WriteLine($"正在应用 SQL Server 特定配置");
        
        // SQL Server特定配置 - 使用默认配置，避免复杂的类型映射
        // SQL Server的EF Core提供程序已经有很好的默认配置
        
        // 只处理Identity相关字段的长度限制
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    var propertyName = property.Name;
                    if (propertyName == "NormalizedName" || propertyName == "NormalizedUserName" || 
                        propertyName == "NormalizedEmail" || propertyName == "ConcurrencyStamp")
                    {
                        var maxLength = property.GetMaxLength();
                        if (!maxLength.HasValue || maxLength.Value > 256)
                        {
                            property.SetMaxLength(256);
                        }
                    }
                }
            }
        }
    }
}
