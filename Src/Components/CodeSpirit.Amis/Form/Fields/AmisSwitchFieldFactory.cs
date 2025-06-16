using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 开关字段工厂类
    /// </summary>
    public class AmisSwitchFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisSwitchFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建开关字段配置
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具类</param>
        /// <returns>AMIS字段配置</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisSwitchFieldAttribute attr) = CreateField<AmisSwitchFieldAttribute>(member, utilityHelper);
            
            if (field != null && attr != null)
            {
                // 设置开关特有属性
                if (!string.IsNullOrEmpty(attr.OnText))
                {
                    field["onText"] = attr.OnText;
                }

                if (!string.IsNullOrEmpty(attr.OffText))
                {
                    field["offText"] = attr.OffText;
                }

                // 处理自定义值
                if (attr.TrueValue != null && !attr.TrueValue.Equals(true))
                {
                    field["trueValue"] = JToken.FromObject(attr.TrueValue);
                }

                if (attr.FalseValue != null && !attr.FalseValue.Equals(false))
                {
                    field["falseValue"] = JToken.FromObject(attr.FalseValue);
                }

                // 设置尺寸
                if (!string.IsNullOrEmpty(attr.Size))
                {
                    field["size"] = attr.Size;
                }
            }

            return field;
        }
    }
} 