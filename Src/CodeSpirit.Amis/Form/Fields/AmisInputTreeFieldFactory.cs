// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputTreeFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, attr) = CreateField<AmisInputTreeFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["source"] = attr.DataSource;
                field["labelField"] = attr.LabelField;
                field["valueField"] = attr.ValueField;
                field["multiple"] = attr.Multiple;
                field["joinValues"] = attr.JoinValues;
                field["extractValue"] = attr.ExtractValue;
            }
            return field;
        }
    }
}
