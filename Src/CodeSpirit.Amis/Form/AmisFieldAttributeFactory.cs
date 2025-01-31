// 文件路径: CodeSpirit.Amis.Form/AmisFieldAttributeFactory.cs

using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    /// <summary>
    /// 工厂类，用于根据 AmisFieldAttribute 创建 AMIS 字段配置。
    /// </summary>
    public class AmisFieldAttributeFactory : IAmisFieldFactory
    {
        /// <summary>
        /// 创建 AMIS 字段配置，基于 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <returns>AMIS 字段的 JSON 对象，如果没有 AmisFieldAttribute 则返回 null。</returns>
        public JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            // 使用扩展方法尝试获取 AmisFieldAttribute 及相关信息
            if (!member.TryGetAmisFieldData(utilityHelper, out var attr, out var displayName, out var fieldName))
                return null;

            // 计算是否为必填字段
            var isRequired = attr.Required || !utilityHelper.IsNullable(utilityHelper.GetMemberType(member));

            // 创建字段配置
            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = attr.Label ?? displayName,
                ["required"] = isRequired,
                ["type"] = attr.Type,
                ["placeholder"] = attr.Placeholder
            };

            // 处理额外的自定义配置
            utilityHelper.HandleAdditionalConfig(attr.AdditionalConfig, field);

            return field;
        }
    }
}