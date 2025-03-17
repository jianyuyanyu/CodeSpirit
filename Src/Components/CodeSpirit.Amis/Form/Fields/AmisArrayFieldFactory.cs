using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    /// <summary>
    /// AMIS 数组输入字段工厂类
    /// </summary>
    public class AmisArrayFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            (JObject field, AmisArrayFieldAttribute attr) = CreateField<AmisArrayFieldAttribute>(member, utilityHelper);
            if (field != null)
            {
                if (!string.IsNullOrWhiteSpace(attr.Items))
                {
                    field["items"] = JObject.Parse(attr.Items);
                }

                field["addable"] = attr.Addable;
                field["removable"] = attr.Removable;
                field["draggable"] = attr.Draggable;
                field["copyable"] = attr.Copyable;
                field["showIndex"] = attr.ShowIndex;
                field["syncFields"] = attr.SyncFields;
                field["conditions"] = attr.Conditions;

                if (attr.MinLength > 0)
                {
                    field["minLength"] = attr.MinLength;
                }

                if (attr.MaxLength > 0)
                {
                    field["maxLength"] = attr.MaxLength;
                }

                if (!string.IsNullOrEmpty(attr.AddButtonText))
                {
                    field["addButtonText"] = attr.AddButtonText;
                }

                if (!string.IsNullOrEmpty(attr.DeleteButtonText))
                {
                    field["deleteButtonText"] = attr.DeleteButtonText;
                }

                if (!string.IsNullOrEmpty(attr.CopyButtonText))
                {
                    field["copyButtonText"] = attr.CopyButtonText;
                }

                if (!string.IsNullOrEmpty(attr.IndexLabel))
                {
                    field["indexLabel"] = attr.IndexLabel;
                }

                if (!string.IsNullOrEmpty(attr.Size))
                {
                    field["size"] = attr.Size;
                }
            }
            return field;
        }
    }
} 