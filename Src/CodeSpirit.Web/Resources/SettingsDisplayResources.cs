#nullable enable
using System.Globalization;
using System.Resources;

namespace CodeSpirit.Web.Resources
{
    /// <summary>
    /// 设置管理显示名称资源类
    /// 资源文件: SettingsDisplay.resx / SettingsDisplay.en.resx
    /// </summary>
    public class SettingsDisplayResources
    {
        private static ResourceManager? _resourceManager;
        
        /// <summary>
        /// 资源管理器
        /// </summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    var resourceManager = new ResourceManager(
                        "CodeSpirit.Web.Resources.SettingsDisplay",
                        typeof(SettingsDisplayResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// ID
        /// </summary>
        public static string Id
        {
            get { return ResourceManager.GetString("Id", CultureInfo.CurrentUICulture) ?? "ID"; }
        }
        
        /// <summary>
        /// 模块
        /// </summary>
        public static string Module
        {
            get { return ResourceManager.GetString("Module", CultureInfo.CurrentUICulture) ?? "模块"; }
        }
        
        /// <summary>
        /// 设置键
        /// </summary>
        public static string Key
        {
            get { return ResourceManager.GetString("Key", CultureInfo.CurrentUICulture) ?? "设置键"; }
        }
        
        /// <summary>
        /// 设置值
        /// </summary>
        public static string Value
        {
            get { return ResourceManager.GetString("Value", CultureInfo.CurrentUICulture) ?? "设置值"; }
        }
        
        /// <summary>
        /// 设置名称
        /// </summary>
        public static string Name
        {
            get { return ResourceManager.GetString("Name", CultureInfo.CurrentUICulture) ?? "设置名称"; }
        }
        
        /// <summary>
        /// 设置描述
        /// </summary>
        public static string Description
        {
            get { return ResourceManager.GetString("Description", CultureInfo.CurrentUICulture) ?? "设置描述"; }
        }
        
        /// <summary>
        /// 设置类型
        /// </summary>
        public static string ValueType
        {
            get { return ResourceManager.GetString("ValueType", CultureInfo.CurrentUICulture) ?? "设置类型"; }
        }
        
        /// <summary>
        /// 设置范围
        /// </summary>
        public static string Scope
        {
            get { return ResourceManager.GetString("Scope", CultureInfo.CurrentUICulture) ?? "设置范围"; }
        }
        
        /// <summary>
        /// 设置分组
        /// </summary>
        public static string Group
        {
            get { return ResourceManager.GetString("Group", CultureInfo.CurrentUICulture) ?? "设置分组"; }
        }
        
        /// <summary>
        /// 系统预设
        /// </summary>
        public static string IsSystemDefault
        {
            get { return ResourceManager.GetString("IsSystemDefault", CultureInfo.CurrentUICulture) ?? "系统预设"; }
        }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public static string CreatedAt
        {
            get { return ResourceManager.GetString("CreatedAt", CultureInfo.CurrentUICulture) ?? "创建时间"; }
        }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public static string UpdatedAt
        {
            get { return ResourceManager.GetString("UpdatedAt", CultureInfo.CurrentUICulture) ?? "更新时间"; }
        }
        
        /// <summary>
        /// 租户ID
        /// </summary>
        public static string TenantId
        {
            get { return ResourceManager.GetString("TenantId", CultureInfo.CurrentUICulture) ?? "租户ID"; }
        }
        
        /// <summary>
        /// 变更原因
        /// </summary>
        public static string Reason
        {
            get { return ResourceManager.GetString("Reason", CultureInfo.CurrentUICulture) ?? "变更原因"; }
        }
    }
}

