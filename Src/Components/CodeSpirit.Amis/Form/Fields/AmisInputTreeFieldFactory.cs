// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputTreeFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisInputTreeFieldAttribute).IsAssignableFrom(attributeType);
        }

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
                field["deferApi"] = attr.DeferApi;
                field["expand"] = attr.Expand;
                field["showIcon"] = attr.ShowIcon;
            }
            return field;
        }
    }
}
