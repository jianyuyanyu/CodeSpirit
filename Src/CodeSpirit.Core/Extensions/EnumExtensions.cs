using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Core.Extensions
{
    /// <summary>
    /// 枚举扩展方法
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举值的显示名称
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>显示名称，如果未设置则返回null</returns>
        public static string GetDisplayName(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            if (fieldInfo == null)
                return null;

            var displayAttribute = fieldInfo.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name;
        }
    }
}
