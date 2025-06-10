using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 数字输入字段工厂类
    /// </summary>
    public class AmisNumberFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisNumberFieldAttribute).IsAssignableFrom(attributeType);
        }

        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisNumberFieldAttribute attr) = CreateField<AmisNumberFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                // 只有当Min被明确设置时才添加min属性（不是NaN）
                if (!double.IsNaN(attr.Min))
                {
                    field["min"] = attr.Min;
                }
                
                // 只有当Max被明确设置时才添加max属性（不是NaN）
                if (!double.IsNaN(attr.Max))
                {
                    field["max"] = attr.Max;
                }
                
                field["step"] = attr.Step;
                field["precision"] = attr.Precision;
                field["showSteps"] = attr.ShowSteps;
                field["bigNumber"] = attr.BigNumber;
                field["kilobitwise"] = attr.Kilobitwise;
                field["keyboard"] = attr.KeyboardMode;
                field["isCurrency"] = attr.IsCurrency;
                
                if (!string.IsNullOrEmpty(attr.Unit))
                {
                    field["unit"] = attr.Unit;
                }
                
                if (!string.IsNullOrEmpty(attr.Prefix))
                {
                    field["prefix"] = attr.Prefix;
                }
                
                if (!string.IsNullOrEmpty(attr.Suffix))
                {
                    field["suffix"] = attr.Suffix;
                }
            }
            return field;
        }
    }
} 