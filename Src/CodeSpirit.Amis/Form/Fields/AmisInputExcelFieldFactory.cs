// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static JArray ExtractPropertyInfo(Type targetType)
        {
            if (targetType == null) return null;
            JArray fieldArray = [];
            // 获取所有公共属性
            var properties = targetType.GetProperties();

            foreach (var property in properties)
            {
                JObject fieldInfo = [];

                // 获取字段名称
                var jsonPropertyName = property.GetCustomAttribute<JsonPropertyAttribute>();
                string fieldName = jsonPropertyName?.PropertyName ?? property.Name; // 如果有JsonPropertyName则使用它，否则使用默认名称
                fieldInfo["name"] = fieldName;

                // 获取标签信息
                fieldInfo["label"] = fieldName;

                // 获取类型
                var fieldType = property.PropertyType.Name.ToLower(); // 获取字段的类型
                fieldInfo["type"] = fieldType.Contains("list") ? "input-table" : "input-text"; // 判断是否为List类型

                fieldArray.Add(fieldInfo);
            }

            return fieldArray;
        }
    }
}
