// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    public class AmisSelectFieldFactory : IAmisFieldFactory
    {
        public JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            // 使用扩展方法尝试获取 AmisFieldAttribute 及相关信息
            if (!member.TryGetAmisFieldData<AmisSelectFieldAttribute>(utilityHelper, out AmisSelectFieldAttribute attr, out string displayName, out string fieldName))
                return null;

            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = attr.Label ?? displayName,
                ["type"] = attr.Type,
                ["source"] = attr.Source,
                ["valueField"] = attr.ValueField,
                ["labelField"] = attr.LabelField,
                ["multiple"] = attr.Multiple,
                ["joinValues"] = attr.JoinValues,
                ["extractValue"] = attr.ExtractValue,
                ["searchable"] = attr.Searchable,
                ["clearable"] = attr.Clearable,
                ["placeholder"] = attr.Placeholder
            };

            utilityHelper.HandleAdditionalConfig(attr.AdditionalConfig, field);

            return field;
        }
    }
}
