// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputImageFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisInputImageFieldAttribute attr) = CreateField<AmisInputImageFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["receiver"] = attr.Receiver;
                field["accept"] = attr.Accept;
                field["maxSize"] = attr.MaxSize;
                field["multiple"] = attr.Multiple;
            }
            return field;
        }
    }
}
