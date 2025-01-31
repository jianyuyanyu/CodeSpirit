// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    public static class CustomAttributeProviderExtensions
    {
        /// <summary>
        /// 尝试获取 AmisFieldAttribute 及相关信息。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <param name="attr">输出的 AmisFieldAttribute 实例。</param>
        /// <param name="displayName">输出的显示名称。</param>
        /// <param name="fieldName">输出的字段名称。</param>
        /// <returns>如果成功获取则返回 true，否则返回 false。</returns>
        public static bool TryGetAmisFieldData(this ICustomAttributeProvider member, UtilityHelper utilityHelper, out AmisFieldAttribute attr, out string displayName, out string fieldName)
        {
            return member.TryGetAmisFieldData<AmisFieldAttribute>(utilityHelper, out attr, out displayName, out fieldName);
        }

        /// <summary>
        /// 尝试获取 AmisFieldAttribute 及相关信息。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <param name="attr">输出的 AmisFieldAttribute 实例。</param>
        /// <param name="displayName">输出的显示名称。</param>
        /// <param name="fieldName">输出的字段名称。</param>
        /// <returns>如果成功获取则返回 true，否则返回 false。</returns>
        public static bool TryGetAmisFieldData<T>(this ICustomAttributeProvider member, UtilityHelper utilityHelper, out T attr, out string displayName, out string fieldName) where T : AmisFieldAttribute
        {
            displayName = null;
            fieldName = null;

            switch (member)
            {
                case MemberInfo m:
                    attr = m.GetCustomAttribute<T>();
                    if (attr != null)
                    {
                        displayName = utilityHelper.GetDisplayName(m);
                        fieldName = utilityHelper.GetFieldName(m, null);
                        return true;
                    }
                    break;

                case ParameterInfo p:
                    attr = p.GetCustomAttribute<T>();
                    if (attr != null)
                    {
                        displayName = utilityHelper.GetDisplayName(p);
                        fieldName = utilityHelper.GetFieldName(p, null);
                        return true;
                    }
                    break;

                default:
                    break;
            }
            attr = null;
            return false;
        }
    }
}
