#nullable enable
using System.Globalization;
using System.Resources;

namespace CodeSpirit.IdentityApi.Resources
{
    /// <summary>
    /// 身份认证错误消息资源类
    /// 资源文件: IdentityErrors.resx / IdentityErrors.en.resx
    /// </summary>
    public class IdentityErrorsResources
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
                        "CodeSpirit.IdentityApi.Resources.IdentityErrors",
                        typeof(IdentityErrorsResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// 用户名已存在！
        /// </summary>
        public static string UserNameExists
        {
            get { return ResourceManager.GetString("UserNameExists", CultureInfo.CurrentUICulture) ?? "用户名已存在！"; }
        }
        
        /// <summary>
        /// 邮箱已存在！
        /// </summary>
        public static string EmailExists
        {
            get { return ResourceManager.GetString("EmailExists", CultureInfo.CurrentUICulture) ?? "邮箱已存在！"; }
        }
        
        /// <summary>
        /// 没有数据可供导出
        /// </summary>
        public static string NoDataToExport
        {
            get { return ResourceManager.GetString("NoDataToExport", CultureInfo.CurrentUICulture) ?? "没有数据可供导出"; }
        }
        
        /// <summary>
        /// 用户不存在！
        /// </summary>
        public static string UserNotFound
        {
            get { return ResourceManager.GetString("UserNotFound", CultureInfo.CurrentUICulture) ?? "用户不存在！"; }
        }
        
        /// <summary>
        /// 只有超级管理员可以使用模拟登录功能！
        /// </summary>
        public static string OnlyAdminCanImpersonate
        {
            get { return ResourceManager.GetString("OnlyAdminCanImpersonate", CultureInfo.CurrentUICulture) ?? "只有超级管理员可以使用模拟登录功能！"; }
        }
        
        /// <summary>
        /// 无法模拟已禁用的用户！
        /// </summary>
        public static string CannotImpersonateDisabledUser
        {
            get { return ResourceManager.GetString("CannotImpersonateDisabledUser", CultureInfo.CurrentUICulture) ?? "无法模拟已禁用的用户！"; }
        }
        
        /// <summary>
        /// 用户已激活
        /// </summary>
        public static string UserActivated
        {
            get { return ResourceManager.GetString("UserActivated", CultureInfo.CurrentUICulture) ?? "用户已激活"; }
        }
        
        /// <summary>
        /// 用户已停用
        /// </summary>
        public static string UserDeactivated
        {
            get { return ResourceManager.GetString("UserDeactivated", CultureInfo.CurrentUICulture) ?? "用户已停用"; }
        }
        
        /// <summary>
        /// 密码重置成功
        /// </summary>
        public static string PasswordResetSuccess
        {
            get { return ResourceManager.GetString("PasswordResetSuccess", CultureInfo.CurrentUICulture) ?? "密码重置成功"; }
        }
        
        /// <summary>
        /// 用户已解锁
        /// </summary>
        public static string UserUnlocked
        {
            get { return ResourceManager.GetString("UserUnlocked", CultureInfo.CurrentUICulture) ?? "用户已解锁"; }
        }
        
        /// <summary>
        /// 批量导入成功
        /// </summary>
        public static string BatchImportSuccess
        {
            get { return ResourceManager.GetString("BatchImportSuccess", CultureInfo.CurrentUICulture) ?? "批量导入成功"; }
        }
        
        /// <summary>
        /// 批量删除成功
        /// </summary>
        public static string BatchDeleteSuccess
        {
            get { return ResourceManager.GetString("BatchDeleteSuccess", CultureInfo.CurrentUICulture) ?? "批量删除成功"; }
        }
        
        /// <summary>
        /// 批量删除部分成功
        /// </summary>
        public static string BatchDeletePartialSuccess
        {
            get { return ResourceManager.GetString("BatchDeletePartialSuccess", CultureInfo.CurrentUICulture) ?? "批量删除部分成功"; }
        }
        
        /// <summary>
        /// 密码仅显示一次
        /// </summary>
        public static string PasswordDisplayOnce
        {
            get { return ResourceManager.GetString("PasswordDisplayOnce", CultureInfo.CurrentUICulture) ?? "密码仅显示一次"; }
        }
        
        /// <summary>
        /// 确认重置密码
        /// </summary>
        public static string ConfirmResetPassword
        {
            get { return ResourceManager.GetString("ConfirmResetPassword", CultureInfo.CurrentUICulture) ?? "确认重置密码"; }
        }
        
        /// <summary>
        /// 确认解锁用户
        /// </summary>
        public static string ConfirmUnlockUser
        {
            get { return ResourceManager.GetString("ConfirmUnlockUser", CultureInfo.CurrentUICulture) ?? "确认解锁用户"; }
        }
        
        /// <summary>
        /// 确认模拟用户
        /// </summary>
        public static string ConfirmImpersonateUser
        {
            get { return ResourceManager.GetString("ConfirmImpersonateUser", CultureInfo.CurrentUICulture) ?? "确认模拟用户"; }
        }
        
        /// <summary>
        /// 确认批量删除
        /// </summary>
        public static string ConfirmBatchDelete
        {
            get { return ResourceManager.GetString("ConfirmBatchDelete", CultureInfo.CurrentUICulture) ?? "确认批量删除"; }
        }
        
        /// <summary>
        /// 重置密码结果
        /// </summary>
        public static string ResetPasswordResult
        {
            get { return ResourceManager.GetString("ResetPasswordResult", CultureInfo.CurrentUICulture) ?? "重置密码结果"; }
        }
    }
}

