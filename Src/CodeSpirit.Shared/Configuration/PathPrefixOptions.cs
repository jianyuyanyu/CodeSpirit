using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Shared.Configuration;

/// <summary>
/// 路径前缀配置选项
/// </summary>
public class PathPrefixOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "PathPrefix";
    
    /// <summary>
    /// 是否启用路径前缀功能
    /// </summary>
    /// <remarks>
    /// 默认值为false，确保向后兼容性
    /// </remarks>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// 路径前缀字符串
    /// </summary>
    /// <remarks>
    /// 例如：设置为"exam"时，原路径"/api/exam/settings"将变为"/exam/api/exam/settings"
    /// 前缀不应包含开头和结尾的斜杠，框架会自动处理
    /// </remarks>
    [StringLength(50, MinimumLength = 1, ErrorMessage = "路径前缀长度必须在1-50个字符之间")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "路径前缀只能包含字母、数字、连字符和下划线")]
    public string? Prefix { get; set; }
    
    /// <summary>
    /// 是否对健康检查端点应用前缀
    /// </summary>
    /// <remarks>
    /// 默认为false，通常健康检查端点不需要前缀以便负载均衡器访问
    /// </remarks>
    public bool ApplyToHealthChecks { get; set; } = false;
    
    /// <summary>
    /// 获取格式化的前缀路径
    /// </summary>
    /// <returns>格式化后的前缀路径，确保以斜杠开头但不以斜杠结尾</returns>
    public string GetFormattedPrefix()
    {
        if (!Enabled || string.IsNullOrWhiteSpace(Prefix))
        {
            return string.Empty;
        }
        
        var trimmedPrefix = Prefix.Trim().Trim('/');
        return string.IsNullOrEmpty(trimmedPrefix) ? string.Empty : $"/{trimmedPrefix}";
    }
    
    /// <summary>
    /// 验证配置选项的有效性
    /// </summary>
    /// <returns>验证结果</returns>
    public ValidationResult Validate()
    {
        if (!Enabled)
        {
            return ValidationResult.Success!;
        }
        
        if (string.IsNullOrWhiteSpace(Prefix))
        {
            return new ValidationResult("启用路径前缀时必须提供前缀字符串");
        }
        
        var trimmedPrefix = Prefix.Trim();
        if (trimmedPrefix.Length > 50)
        {
            return new ValidationResult("路径前缀长度不能超过50个字符");
        }
        
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedPrefix, @"^[a-zA-Z0-9\-_]+$"))
        {
            return new ValidationResult("路径前缀只能包含字母、数字、连字符和下划线");
        }
        
        return ValidationResult.Success!;
    }
}
