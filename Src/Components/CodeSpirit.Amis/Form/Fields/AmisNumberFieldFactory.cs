using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 数字输入字段工厂类
    /// </summary>
    public class AmisNumberFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisNumberFieldAttribute attr) = CreateField<AmisNumberFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["min"] = attr.Min;
                field["max"] = attr.Max;
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