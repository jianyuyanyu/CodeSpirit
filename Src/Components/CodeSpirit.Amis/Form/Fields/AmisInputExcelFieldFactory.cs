// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Fields
{
    public class AmisInputExcelFieldFactory : AmisFieldAttributeFactoryBase
    {
        public override JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            var (field, attr) = CreateField<AmisInputExcelFieldAttribute>(member, utilityHelper);
            if (field != null && attr != null && attr.CreateInputTable)
            {
                var type = utilityHelper.GetMemberType(member);
                var cols = ExtractPropertyInfo(type.GenericTypeArguments.FirstOrDefault());
                var wrapper = new JObject()
                {
                    ["type"] = "wrapper",
                    ["size"] = "lg",
                    ["body"] = new JArray()
                    {
                        field,
                        new JObject()
                        {
                            ["type"] = "input-table",
                            ["name"] = field["name"],
                            ["showIndex"] = true,
                            ["addable"] = true,
                            ["editable"] = true,
                            ["copyable"] = true,
                            ["removable"] = true,
                            ["visibleOn"] = $"data.{field["name"]}",
                            ["columns"] = cols
                        }
                    }
                };
                return wrapper;
            }
            return field;
        }

        public JArray ExtractPropertyInfo(Type targetType)
        {
            if (targetType == null) return null;
            JArray fieldArray = [];
            // 获取所有公共属性
            var properties = targetType.GetProperties();

            foreach (var property in properties)
            {
                // 获取字段名称
                var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
                string fieldName = jsonProperty?.PropertyName;
                //导入表格使用友好名称作为字段和显示名
                var field = property.CreateFormField(fieldName: fieldName, lableName: fieldName);
                fieldArray.Add(field);
            }

            return fieldArray;
        }
    }
}
