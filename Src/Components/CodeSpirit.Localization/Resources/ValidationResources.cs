using System.Globalization;
using System.Resources;

namespace CodeSpirit.Localization.Resources
{
    /// <summary>
    /// 验证消息资源类（自动生成）
    /// 资源文件: Validation.resx / Validation.en.resx
    /// </summary>
    public class ValidationResources
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
                        "CodeSpirit.Localization.Resources.Validation",
                        typeof(ValidationResources).Assembly);
                    _resourceManager = resourceManager;
                }
                return _resourceManager;
            }
        }
        
        /// <summary>
        /// {0}不能为空
        /// </summary>
        public static string Required
        {
            get { return ResourceManager.GetString("Required", CultureInfo.CurrentUICulture) ?? "{0}不能为空"; }
        }
        
        /// <summary>
        /// {0}最多{1}字符
        /// </summary>
        public static string StringLengthMax
        {
            get { return ResourceManager.GetString("StringLengthMax", CultureInfo.CurrentUICulture) ?? "{0}最多{1}字符"; }
        }
        
        /// <summary>
        /// {0}长度必须在{1}到{2}之间
        /// </summary>
        public static string StringLengthRange
        {
            get { return ResourceManager.GetString("StringLengthRange", CultureInfo.CurrentUICulture) ?? "{0}长度必须在{1}到{2}之间"; }
        }
        
        /// <summary>
        /// {0}必须在{1}到{2}之间
        /// </summary>
        public static string Range
        {
            get { return ResourceManager.GetString("Range", CultureInfo.CurrentUICulture) ?? "{0}必须在{1}到{2}之间"; }
        }
        
        /// <summary>
        /// {0}格式不正确
        /// </summary>
        public static string EmailAddress
        {
            get { return ResourceManager.GetString("EmailAddress", CultureInfo.CurrentUICulture) ?? "{0}格式不正确"; }
        }
        
        /// <summary>
        /// {0}格式不正确
        /// </summary>
        public static string Phone
        {
            get { return ResourceManager.GetString("Phone", CultureInfo.CurrentUICulture) ?? "{0}格式不正确"; }
        }
        
        /// <summary>
        /// {0}与{1}不匹配
        /// </summary>
        public static string Compare
        {
            get { return ResourceManager.GetString("Compare", CultureInfo.CurrentUICulture) ?? "{0}与{1}不匹配"; }
        }
    }
}
