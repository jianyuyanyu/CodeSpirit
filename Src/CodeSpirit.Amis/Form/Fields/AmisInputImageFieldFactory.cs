// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputImageFieldFactory : IAmisFieldFactory
    {
        public JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            // 使用扩展方法尝试获取 AmisFieldAttribute 及相关信息
            if (!member.TryGetAmisFieldData(utilityHelper, out AmisInputImageFieldAttribute attr, out string displayName, out string fieldName))
                return null;

            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = attr.Label ?? displayName,
                ["type"] = attr.Type,
                ["receiver"] = attr.Receiver,
                ["accept"] = attr.Accept,
                ["maxSize"] = attr.MaxSize,
                ["multiple"] = attr.Multiple,
                ["placeholder"] = attr.Placeholder
            };

            utilityHelper.HandleAdditionalConfig(attr.AdditionalConfig, field);

            return field;
        }
    }
}
