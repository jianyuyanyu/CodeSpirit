using System;

namespace CodeSpirit.Audit.Attributes;

/// <summary>
/// 禁用审计特性，用于标记不需要审计的控制器或方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NoAuditAttribute : Attribute
{
    /// <summary>
    /// 禁用原因（可选）
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NoAuditAttribute()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="reason">禁用原因</param>
    public NoAuditAttribute(string reason)
    {
        Reason = reason;
    }
}
