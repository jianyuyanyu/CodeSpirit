// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputImageFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisInputImageFieldAttribute).IsAssignableFrom(attributeType);
        }

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
