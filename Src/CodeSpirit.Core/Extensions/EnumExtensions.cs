using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace CodeSpirit.Core.Extensions
{
    /// <summary>
    /// 枚举扩展方法
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举值的显示名称（支持 ResourceType 多语言资源解析）
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>显示名称，如果未设置则返回null</returns>
        public static string GetDisplayName(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            if (fieldInfo == null)
                return null;

            var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute == null)
                return null;

            // 当指定了 ResourceType 时，从资源文件中获取本地化文本
            if (displayAttribute.ResourceType != null && !string.IsNullOrEmpty(displayAttribute.Name))
            {
                try
                {
                    var resourceManagerProp = displayAttribute.ResourceType.GetProperty(
                        "ResourceManager", BindingFlags.Public | BindingFlags.Static);
                    if (resourceManagerProp != null)
                    {
                        var resourceManager = resourceManagerProp.GetValue(null) as ResourceManager;
                        var localizedText = resourceManager?.GetString(
                            displayAttribute.Name, CultureInfo.CurrentUICulture);
                        if (!string.IsNullOrEmpty(localizedText))
                            return localizedText;
                    }
                }
                catch
                {
                    // 资源获取失败时回退到 Name
                }
            }

            return displayAttribute.Name;
        }
    }
}
