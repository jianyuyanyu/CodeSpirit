using System.Globalization;
using System.Resources;

namespace CodeSpirit.Navigation.Resources
{
    /// <summary>
    /// 导航资源类
    /// 资源文件: NavigationResources.resx / NavigationResources.en.resx
    /// </summary>
    public class NavigationResources
    {
        private static ResourceManager _resourceManager;
        
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
                        "CodeSpirit.Navigation.Resources.NavigationResources",
                        typeof(NavigationResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// 用户中心模块
        /// </summary>
        public static string Module_Identity
        {
            get { return ResourceManager.GetString("Module.Identity", CultureInfo.CurrentUICulture) ?? "用户中心"; }
        }
        
        /// <summary>
        /// 用户管理控制器
        /// </summary>
        public static string Controller_Users
        {
            get { return ResourceManager.GetString("Controller.Users", CultureInfo.CurrentUICulture) ?? "用户管理"; }
        }
        
        /// <summary>
        /// 角色管理控制器
        /// </summary>
        public static string Controller_Roles
        {
            get { return ResourceManager.GetString("Controller.Roles", CultureInfo.CurrentUICulture) ?? "角色管理"; }
        }
        
        /// <summary>
        /// API密钥管理控制器
        /// </summary>
        public static string Controller_ApiKeys
        {
            get { return ResourceManager.GetString("Controller.ApiKeys", CultureInfo.CurrentUICulture) ?? "API密钥管理"; }
        }
        
        /// <summary>
        /// 租户管理控制器
        /// </summary>
        public static string Controller_Tenants
        {
            get { return ResourceManager.GetString("Controller.Tenants", CultureInfo.CurrentUICulture) ?? "租户管理"; }
        }
        
        /// <summary>
        /// 部门管理控制器
        /// </summary>
        public static string Controller_Departments
        {
            get { return ResourceManager.GetString("Controller.Departments", CultureInfo.CurrentUICulture) ?? "部门管理"; }
        }
        
        /// <summary>
        /// 职工管理控制器
        /// </summary>
        public static string Controller_Employees
        {
            get { return ResourceManager.GetString("Controller.Employees", CultureInfo.CurrentUICulture) ?? "职工管理"; }
        }
        
        /// <summary>
        /// 权限管理控制器
        /// </summary>
        public static string Controller_Permissions
        {
            get { return ResourceManager.GetString("Controller.Permissions", CultureInfo.CurrentUICulture) ?? "权限管理"; }
        }
        
        /// <summary>
        /// 用户统计控制器
        /// </summary>
        public static string Controller_UserStatistics
        {
            get { return ResourceManager.GetString("Controller.UserStatistics", CultureInfo.CurrentUICulture) ?? "用户统计"; }
        }
        
        /// <summary>
        /// 登录日志控制器
        /// </summary>
        public static string Controller_LoginLogs
        {
            get { return ResourceManager.GetString("Controller.LoginLogs", CultureInfo.CurrentUICulture) ?? "登录日志"; }
        }
        
        /// <summary>
        /// 系统用户管理控制器
        /// </summary>
        public static string Controller_SystemUsers
        {
            get { return ResourceManager.GetString("Controller.SystemUsers", CultureInfo.CurrentUICulture) ?? "用户管理"; }
        }
        
        /// <summary>
        /// 系统角色管理控制器
        /// </summary>
        public static string Controller_SystemRoles
        {
            get { return ResourceManager.GetString("Controller.SystemRoles", CultureInfo.CurrentUICulture) ?? "角色管理"; }
        }
        
        /// <summary>
        /// 系统权限管理控制器
        /// </summary>
        public static string Controller_SystemPermissions
        {
            get { return ResourceManager.GetString("Controller.SystemPermissions", CultureInfo.CurrentUICulture) ?? "权限管理"; }
        }
        
        /// <summary>
        /// 租户管理模块
        /// </summary>
        public static string Module_Tenant
        {
            get { return ResourceManager.GetString("Module.Tenant", CultureInfo.CurrentUICulture) ?? "租户管理"; }
        }
    }
}

