// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS ListSelect 字段工厂，用于创建 list-select 类型的表单字段。
    /// </summary>
    public class AmisListSelectFieldFactory : AmisFieldAttributeFactoryBase
    {
        /// <summary>
        /// 判断是否能处理指定类型的特性
        /// </summary>
        /// <param name="attributeType">特性类型</param>
        /// <returns>是否能处理</returns>
        public override bool CanHandle(Type attributeType)
        {
            return typeof(AmisListSelectFieldAttribute).IsAssignableFrom(attributeType);
        }

        /// <summary>
        /// 创建 AMIS list-select 字段配置
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）</param>
        /// <param name="utilityHelper">实用工具类</param>
        /// <returns>AMIS 字段的 JSON 对象</returns>
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisListSelectFieldAttribute attr) = CreateField<AmisListSelectFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                // 设置数据源相关属性
                if (!string.IsNullOrEmpty(attr.Source))
                    field["source"] = attr.Source;

                if (!string.IsNullOrEmpty(attr.LabelField))
                    field["labelField"] = attr.LabelField;

                if (!string.IsNullOrEmpty(attr.ValueField))
                    field["valueField"] = attr.ValueField;

                // 设置选择相关属性
                field["multiple"] = attr.Multiple;
                field["joinValues"] = attr.JoinValues;
                field["extractValue"] = attr.ExtractValue;

                // 设置交互相关属性
                field["searchable"] = attr.Searchable;
                field["clearable"] = attr.Clearable;

                // 设置 list-select 特有属性
                if (!string.IsNullOrEmpty(attr.ImageField))
                    field["imageField"] = attr.ImageField;

                if (!string.IsNullOrEmpty(attr.DescField))
                    field["descField"] = attr.DescField;

                if (attr.ListHeight > 0)
                    field["listHeight"] = attr.ListHeight;
            }
            return field;
        }
    }
}
