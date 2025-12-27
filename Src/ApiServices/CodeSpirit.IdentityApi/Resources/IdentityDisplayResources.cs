#nullable enable
using System.Globalization;
using System.Resources;

namespace CodeSpirit.IdentityApi.Resources
{
    /// <summary>
    /// 身份认证显示名称资源类
    /// 资源文件: IdentityDisplay.resx / IdentityDisplay.en.resx
    /// </summary>
    public class IdentityDisplayResources
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
                        "CodeSpirit.IdentityApi.Resources.IdentityDisplay",
                        typeof(IdentityDisplayResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// 姓名
        /// </summary>
        public static string Name
        {
            get { return ResourceManager.GetString("Name", CultureInfo.CurrentUICulture) ?? "姓名"; }
        }
        
        /// <summary>
        /// 用户名
        /// </summary>
        public static string UserName
        {
            get { return ResourceManager.GetString("UserName", CultureInfo.CurrentUICulture) ?? "用户名"; }
        }
        
        /// <summary>
        /// 身份证
        /// </summary>
        public static string IdNo
        {
            get { return ResourceManager.GetString("IdNo", CultureInfo.CurrentUICulture) ?? "身份证"; }
        }
        
        /// <summary>
        /// 头像
        /// </summary>
        public static string AvatarUrl
        {
            get { return ResourceManager.GetString("AvatarUrl", CultureInfo.CurrentUICulture) ?? "头像"; }
        }
        
        /// <summary>
        /// 电子邮箱
        /// </summary>
        public static string Email
        {
            get { return ResourceManager.GetString("Email", CultureInfo.CurrentUICulture) ?? "电子邮箱"; }
        }
        
        /// <summary>
        /// 分配角色
        /// </summary>
        public static string Roles
        {
            get { return ResourceManager.GetString("Roles", CultureInfo.CurrentUICulture) ?? "分配角色"; }
        }
        
        /// <summary>
        /// 性别
        /// </summary>
        public static string Gender
        {
            get { return ResourceManager.GetString("Gender", CultureInfo.CurrentUICulture) ?? "性别"; }
        }
        
        /// <summary>
        /// 手机号码
        /// </summary>
        public static string PhoneNumber
        {
            get { return ResourceManager.GetString("PhoneNumber", CultureInfo.CurrentUICulture) ?? "手机号码"; }
        }
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public static string IsActive
        {
            get { return ResourceManager.GetString("IsActive", CultureInfo.CurrentUICulture) ?? "是否激活"; }
        }
        
        /// <summary>
        /// 最后登录时间
        /// </summary>
        public static string LastLoginTime
        {
            get { return ResourceManager.GetString("LastLoginTime", CultureInfo.CurrentUICulture) ?? "最后登录时间"; }
        }
        
        /// <summary>
        /// 启用锁定
        /// </summary>
        public static string LockoutEnabled
        {
            get { return ResourceManager.GetString("LockoutEnabled", CultureInfo.CurrentUICulture) ?? "启用锁定"; }
        }
        
        /// <summary>
        /// 锁定结束时间
        /// </summary>
        public static string LockoutEnd
        {
            get { return ResourceManager.GetString("LockoutEnd", CultureInfo.CurrentUICulture) ?? "锁定结束时间"; }
        }
        
        /// <summary>
        /// 访问失败次数
        /// </summary>
        public static string AccessFailedCount
        {
            get { return ResourceManager.GetString("AccessFailedCount", CultureInfo.CurrentUICulture) ?? "访问失败次数"; }
        }
        
    /// <summary>
    /// 角色
    /// </summary>
    public static string Role
    {
        get { return ResourceManager.GetString("Role", CultureInfo.CurrentUICulture) ?? "角色"; }
    }
    
    /// <summary>
    /// 租户
    /// </summary>
    public static string Tenant
    {
        get { return ResourceManager.GetString("Tenant", CultureInfo.CurrentUICulture) ?? "租户"; }
    }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public static string CreatedAt
    {
        get { return ResourceManager.GetString("CreatedAt", CultureInfo.CurrentUICulture) ?? "创建时间"; }
    }
    
    /// <summary>
    /// 分隔符
    /// </summary>
    public static string Separator
    {
        get { return ResourceManager.GetString("Separator", CultureInfo.CurrentUICulture) ?? "-"; }
    }
    
    /// <summary>
    /// 租户ID
    /// </summary>
    public static string TenantId
    {
        get { return ResourceManager.GetString("TenantId", CultureInfo.CurrentUICulture) ?? "租户ID"; }
    }
    
    /// <summary>
    /// 租户名称
    /// </summary>
    public static string TenantName
    {
        get { return ResourceManager.GetString("TenantName", CultureInfo.CurrentUICulture) ?? "租户名称"; }
    }
    
    /// <summary>
    /// 租户显示名称
    /// </summary>
    public static string TenantDisplayName
    {
        get { return ResourceManager.GetString("TenantDisplayName", CultureInfo.CurrentUICulture) ?? "租户显示名称"; }
    }
    
    /// <summary>
    /// 总用户数
    /// </summary>
    public static string TotalUsers
    {
        get { return ResourceManager.GetString("TotalUsers", CultureInfo.CurrentUICulture) ?? "总用户数"; }
    }
    
    /// <summary>
    /// 活跃用户数
    /// </summary>
    public static string ActiveUsers
    {
        get { return ResourceManager.GetString("ActiveUsers", CultureInfo.CurrentUICulture) ?? "活跃用户数"; }
    }
    
    /// <summary>
    /// 禁用用户数
    /// </summary>
    public static string InactiveUsers
    {
        get { return ResourceManager.GetString("InactiveUsers", CultureInfo.CurrentUICulture) ?? "禁用用户数"; }
    }
    
    /// <summary>
    /// 管理员用户数
    /// </summary>
    public static string AdminUsers
    {
        get { return ResourceManager.GetString("AdminUsers", CultureInfo.CurrentUICulture) ?? "管理员用户数"; }
    }
    
    /// <summary>
    /// 普通用户数
    /// </summary>
    public static string NormalUsers
    {
        get { return ResourceManager.GetString("NormalUsers", CultureInfo.CurrentUICulture) ?? "普通用户数"; }
    }
    
    /// <summary>
    /// 本月新增用户数
    /// </summary>
    public static string NewUsersThisMonth
    {
        get { return ResourceManager.GetString("NewUsersThisMonth", CultureInfo.CurrentUICulture) ?? "本月新增用户数"; }
    }
    
    /// <summary>
    /// 本月活跃用户数
    /// </summary>
    public static string ActiveUsersThisMonth
    {
        get { return ResourceManager.GetString("ActiveUsersThisMonth", CultureInfo.CurrentUICulture) ?? "本月活跃用户数"; }
    }
    
    /// <summary>
    /// 最后活跃时间
    /// </summary>
    public static string LastActiveTime
    {
        get { return ResourceManager.GetString("LastActiveTime", CultureInfo.CurrentUICulture) ?? "最后活跃时间"; }
    }
    
    /// <summary>
    /// 用户增长率
    /// </summary>
    public static string GrowthRate
    {
        get { return ResourceManager.GetString("GrowthRate", CultureInfo.CurrentUICulture) ?? "用户增长率"; }
    }
    
    /// <summary>
    /// 用户活跃度
    /// </summary>
    public static string ActivityRate
    {
        get { return ResourceManager.GetString("ActivityRate", CultureInfo.CurrentUICulture) ?? "用户活跃度"; }
    }
}
}

