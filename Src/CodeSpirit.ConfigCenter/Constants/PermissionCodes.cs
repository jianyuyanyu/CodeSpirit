namespace CodeSpirit.ConfigCenter.Constants;

/// <summary>
/// 配置中心权限代码常量
/// </summary>
public static class PermissionCodes
{
    /// <summary>
    /// 应用管理
    /// </summary>
    public const string AppManagement = "config:app:manage";

    /// <summary>
    /// 配置管理
    /// </summary>
    public const string ConfigItemManagement = "config:item:manage";

    /// <summary>
    /// 发布管理
    /// </summary>
    public const string PublishManagement = "config:publish:manage";

    /// <summary>
    /// 配置读取
    /// </summary>
    public const string ConfigRead = "config:item:read";

    /// <summary>
    /// 配置写入
    /// </summary>
    public const string ConfigWrite = "config:item:write";

    /// <summary>
    /// 配置发布
    /// </summary>
    public const string ConfigPublish = "config:publish:write";

    /// <summary>
    /// 配置回滚
    /// </summary>
    public const string ConfigRollback = "config:publish:rollback";

    /// <summary>
    /// 配置审计日志
    /// </summary>
    public const string ConfigAuditLog = "config:audit:read";
} 