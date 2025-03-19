using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 文本域字段工厂类
    /// </summary>
    public class AmisTextareaFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisTextareaFieldAttribute).IsAssignableFrom(attributeType);
        }

        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisTextareaFieldAttribute attr) = CreateField<AmisTextareaFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["minRows"] = attr.MinRows;
                field["maxRows"] = attr.MaxRows;
                field["trim"] = attr.Trim;
                field["showCounter"] = attr.ShowCounter;
                field["maxLength"] = attr.MaxLength;
                field["resizable"] = attr.Resizable;
            }
            return field;
        }
    }
} 