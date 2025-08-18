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
                if (!string.IsNullOrEmpty(attr.DataSource))
                    field["source"] = attr.DataSource;

                field["labelField"] = attr.LabelField;
                field["valueField"] = attr.ValueField;

                if (!attr.Multiple)
                    field["multiple"] = attr.Multiple;

                if (!attr.JoinValues)
                    field["joinValues"] = attr.JoinValues;

                if (attr.ExtractValue)
                    field["extractValue"] = attr.ExtractValue;

                if (!string.IsNullOrEmpty(attr.DeferApi))
                    field["deferApi"] = attr.DeferApi;

                field["expand"] = attr.Expand;

                if (!attr.ShowIcon)
                    field["showIcon"] = attr.ShowIcon;

                if (attr.Cascade)
                    field["cascade"] = attr.Cascade;

                if (!attr.AutoCheckChildren)
                    field["autoCheckChildren"] = attr.AutoCheckChildren;

                if (attr.Searchable)
                    field["searchable"] = attr.Searchable;
                if (attr.ShowOutline)
                    field["showOutline"] = attr.ShowOutline;
                if (attr.ShowOutline)
                    field["clearable"] = attr.Clearable;
                if (attr.ChildrenField != "children")
                    field["childrenField"] = attr.ChildrenField;
                if (!attr.DeferField.IsNullOrWhiteSpace())
                    field["deferField"] = attr.DeferField;
                if (attr.OnlyLeaf)
                    field["onlyLeaf"] = attr.OnlyLeaf;

                if (attr.HeightAuto)
                    field["heightAuto"] = attr.HeightAuto;

                if (attr.SelectFirst)
                    field["selectFirst"] = attr.SelectFirst;

                if (attr.InputOnly)
                    field["inputOnly"] = attr.InputOnly;
            }
            return field;
        }
    }
}
