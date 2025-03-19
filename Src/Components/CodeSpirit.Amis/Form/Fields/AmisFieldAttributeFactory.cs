// 文件路径: CodeSpirit.Amis.Form/AmisFieldAttributeFactory.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// 工厂类，用于根据 AmisFieldAttribute 创建 AMIS 字段配置。
    /// </summary>
    public class AmisFieldAttributeFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisFormFieldAttribute) == attributeType;
        }

        /// <summary>
        /// 创建字段配置
        /// </summary>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisFormFieldAttribute attr) = CreateField<AmisFormFieldAttribute>(member, utilityHelper);
            return field;
        }
    }
}