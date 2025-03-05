// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisSelectFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisSelectFieldAttribute attr) = CreateField<AmisSelectFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["source"] = attr.Source;
                field["labelField"] = attr.LabelField;
                field["valueField"] = attr.ValueField;
                field["multiple"] = attr.Multiple;
                field["joinValues"] = attr.JoinValues;
                field["extractValue"] = attr.ExtractValue;
                field["searchable"] = attr.Searchable;
                field["clearable"] = attr.Clearable;
            }
            return field;
        }
    }
}
