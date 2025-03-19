using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// 树形选择器字段工厂类
    /// </summary>
    public class AmisTreeSelectFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisTreeSelectFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建字段
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">工具助手</param>
        /// <returns>字段JSON对象</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, attr) = CreateField<AmisTreeSelectFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                field["source"] = attr.DataSource;
                field["labelField"] = attr.LabelField;
                field["valueField"] = attr.ValueField;
                field["multiple"] = attr.Multiple;
                field["joinValues"] = attr.JoinValues;
                field["extractValue"] = attr.ExtractValue;
                field["cascade"] = attr.Cascade;
                field["searchable"] = attr.Searchable;
                field["deferField"] = attr.DeferField;
                field["clearable"] = attr.Clearable;
                field["showIcon"] = attr.ShowIcon;
                field["showOutline"] = attr.ShowOutline;
            }
            return field;
        }
    }
} 