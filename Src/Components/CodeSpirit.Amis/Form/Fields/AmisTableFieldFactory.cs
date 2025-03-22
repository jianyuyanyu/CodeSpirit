using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Form.Factories
{
    public class AmisTableFieldFactory : IAmisFieldFactory
    {
        public bool CanHandle(Type attributeType)
        {
            return attributeType == typeof(AmisTableFieldAttribute);
        }

        public JObject CreateField(ICustomAttributeProvider member, UtilityHelper utilityHelper)
        {
            if (member is not PropertyInfo prop)
                return null;

            var attr = prop.GetCustomAttribute<AmisTableFieldAttribute>();
            if (attr == null)
                return null;

            var field = new JObject
            {
                ["type"] = "input-table",
                ["name"] = prop.Name.ToCamelCase(),
                ["label"] = prop.GetDisplayName(),
                ["addable"] = attr.Addable,
                ["removable"] = attr.Removable,
                ["draggable"] = attr.Draggable,
                ["addButtonText"] = attr.AddButtonText
            };

            // 获取集合元素类型
            var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
            if (elementType != null)
            {
                // 自动生成列配置
                var columns = GenerateColumns(elementType, utilityHelper);
                field["columns"] = JArray.FromObject(columns);
            }

            return field;
        }

        private List<JObject> GenerateColumns(Type elementType, UtilityHelper utilityHelper)
        {
            var columns = new List<JObject>();
            
            foreach (var prop in elementType.GetProperties())
            {
                var column = new JObject
                {
                    ["name"] = prop.Name.ToCamelCase(),
                    ["label"] = prop.GetDisplayName(),
                    ["quickEdit"] = GetQuickEditConfig(prop, utilityHelper)
                };
                
                columns.Add(column);
            }

            return columns;
        }

        private JObject GetQuickEditConfig(PropertyInfo prop, UtilityHelper utilityHelper)
        {
            // 根据属性类型返回适当的编辑器配置
            if (prop.PropertyType.IsEnum)
            {
                return new JObject
                {
                    ["type"] = "select",
                    ["options"] = JArray.FromObject(prop.PropertyType.GetEnumOptions())
                };
            }

            return new JObject
            {
                ["type"] = "input-text"  // 默认使用文本输入
            };
        }
    }
} 