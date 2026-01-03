// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisSelectFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisSelectFieldAttribute).IsAssignableFrom(attributeType);
        }

        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisSelectFieldAttribute attr) = CreateField<AmisSelectFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                if(!string.IsNullOrEmpty(attr.Source))
                    field["source"] = attr.Source;
                if(!string.IsNullOrEmpty(attr.LabelField))
                    field["labelField"] = attr.LabelField;
                if(!string.IsNullOrEmpty(attr.ValueField))
                    field["valueField"] = attr.ValueField;
                
                // 处理静态Options属性：将 "value1:label1,value2:label2" 转换为 options 数组
                if (!string.IsNullOrEmpty(attr.Options))
                {
                    var options = new JArray();
                    var optionPairs = attr.Options.Split(',');
                    foreach (var pair in optionPairs)
                    {
                        var parts = pair.Split(':');
                        if (parts.Length == 2)
                        {
                            options.Add(new JObject
                            {
                                ["label"] = parts[1].Trim(),
                                ["value"] = parts[0].Trim()
                            });
                        }
                    }
                    field["options"] = options;
                }
                
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
