// 文件路径: CodeSpirit.Amis.Form/AmisFieldAttributeFactoryBase.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public abstract class AmisFieldAttributeFactoryBase : IAmisFieldFactory
    {
        /// <summary>
        /// 创建 AMIS 字段配置，基于 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>AMIS 字段的 JSON 对象，如果没有 AmisFieldAttribute 则返回 null。</returns>
        public virtual JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, fieldAttr) = CreateField<AmisFormFieldAttribute>(member, utilityHelper);
            return field;
        }

        /// <summary>
        /// 创建 AMIS 字段配置，基于 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>AMIS 字段的 JSON 对象，如果没有 AmisFieldAttribute 则返回 null。</returns>
        public virtual (JObject, T) CreateField<T>(ICustomAttributeProvider member, UtilityHelper utilityHelper) where T : AmisFormFieldAttribute
        {
            // 使用扩展方法尝试获取 AmisFieldAttribute 及相关信息
            if (!member.TryGetAmisFieldData(utilityHelper, out T fieldAttr, out string displayName, out string fieldName))
                return (null, fieldAttr);

            if (fieldAttr == null) return (null, null);
            // 计算是否为必填字段
            bool isRequired = fieldAttr.Required || !utilityHelper.IsNullable(utilityHelper.GetMemberType(member));

            // 创建字段配置
            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = fieldAttr.Label ?? displayName,
                ["required"] = isRequired,
                ["type"] = fieldAttr.Type,
                ["placeholder"] = fieldAttr.Placeholder,
                ["visibleOn"] = fieldAttr.VisibleOn
            };

            // 处理额外的自定义配置
            utilityHelper.HandleAdditionalConfig(fieldAttr.AdditionalConfig, field);

            return (field, fieldAttr);
        }
    }
}