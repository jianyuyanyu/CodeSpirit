using CodeSpirit.Amis.Attributes.FormFields;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    /// <summary>
    /// 表单项组特性提供者
    /// <para>用于将FormGroupAttribute包装成ICustomAttributeProvider接口</para>
    /// </summary>
    internal class FormGroupAttributeProvider : ICustomAttributeProvider
    {
        private readonly FormGroupAttribute _groupAttribute;

        /// <summary>
        /// 初始化表单项组特性提供者
        /// </summary>
        /// <param name="groupAttribute">表单项组特性</param>
        public FormGroupAttributeProvider(FormGroupAttribute groupAttribute)
        {
            _groupAttribute = groupAttribute ?? throw new ArgumentNullException(nameof(groupAttribute));
        }

        /// <summary>
        /// 获取自定义特性数组
        /// </summary>
        /// <param name="inherit">是否继承</param>
        /// <returns>特性数组</returns>
        public object[] GetCustomAttributes(bool inherit)
        {
            return [_groupAttribute];
        }

        /// <summary>
        /// 获取指定类型的自定义特性数组
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>特性数组</returns>
        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == typeof(FormGroupAttribute) || attributeType.IsAssignableFrom(typeof(FormGroupAttribute)))
            {
                return [_groupAttribute];
            }
            return [];
        }

        /// <summary>
        /// 判断是否定义了指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>是否定义了特性</returns>
        public bool IsDefined(Type attributeType, bool inherit)
        {
            return attributeType == typeof(FormGroupAttribute) || attributeType.IsAssignableFrom(typeof(FormGroupAttribute));
        }
    }
}
