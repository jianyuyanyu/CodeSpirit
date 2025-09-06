using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
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
                ["addButtonText"] = attr.AddButtonText,
                ["showIndex"] = attr.ShowIndex,
                ["editOnAdd"] = attr.EditOnAdd,
                ["confirmMode"] = attr.ConfirmMode,
                ["editable"] = attr.Editable,
                ["copyable"] = attr.Copyable
            };

            if (attr.Perpage != default)
            {
                field["perPage"] = attr.Perpage;
            }

            // 添加条件显示支持
            if (!string.IsNullOrEmpty(attr.VisibleOn))
            {
                field["visibleOn"] = attr.VisibleOn;
            }
            // 获取集合元素类型
            var elementType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
            if (elementType != null)
            {
                // 自动生成列配置
                var columns = GenerateColumns(elementType, utilityHelper, attr);
                field["columns"] = JArray.FromObject(columns);
            }

            return field;
        }

        private List<JObject> GenerateColumns(Type elementType, UtilityHelper utilityHelper, AmisTableFieldAttribute tableFieldAttr)
        {
            var columns = new List<JObject>();

            foreach (var prop in elementType.GetProperties())
            {
                // 检查AmisFormField特性，如果Hidden为true则跳过该列
                var amisFormFieldAttr = prop.GetCustomAttribute<AmisFormFieldAttribute>();
                if (amisFormFieldAttr?.Hidden == true)
                {
                    continue; // 跳过隐藏字段
                }

                var column = new JObject
                {
                    ["name"] = prop.Name.ToCamelCase(),
                    ["label"] = prop.GetDisplayName()
                };

                // 只有在启用快速编辑时才添加快速编辑配置
                if (tableFieldAttr?.QuickEdit == true)
                {
                    column["quickEdit"] = GetQuickEditConfig(prop, utilityHelper);
                }

                // 针对枚举类型和布尔类型添加映射
                var enumType = prop.PropertyType;
                var isNullableEnum = Nullable.GetUnderlyingType(enumType)?.IsEnum == true;
                var boolType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                
                if (enumType.IsEnum || isNullableEnum)
                {
                    // 如果是可空枚举，获取其基础枚举类型
                    if (isNullableEnum)
                    {
                        enumType = Nullable.GetUnderlyingType(enumType);
                    }
                    
                    // 添加映射类型
                    column["type"] = "mapping";
                    
                    // 创建枚举映射
                    var mapping = new JObject();
                    foreach (var enumValue in Enum.GetValues(enumType))
                    {
                        // 获取枚举的实际值（根据基础类型动态转换）
                        var underlyingType = Enum.GetUnderlyingType(enumType);
                        string value = Convert.ChangeType(enumValue, underlyingType, System.Globalization.CultureInfo.InvariantCulture).ToString();
                        
                        // 获取枚举的显示名称
                        string label = enumType.GetEnumDisplayName(enumValue);
                        mapping[value] = label;
                    }
                    
                    // 如果是可空枚举，添加空值映射
                    if (isNullableEnum)
                    {
                        mapping[""] = ""; // 可以自定义空值的显示文本
                    }
                    
                    // 设置映射
                    column["map"] = mapping;
                }
                else if (boolType == typeof(bool))
                {
                    // 添加布尔类型映射
                    column["type"] = "status";
                }

                columns.Add(column);
            }

            return columns;
        }

        private JObject GetQuickEditConfig(PropertyInfo prop, UtilityHelper utilityHelper)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // 处理枚举类型
            if (type.IsEnum)
            {
                return new JObject
                {
                    ["type"] = "select",
                    ["options"] = JArray.FromObject(type.GetEnumOptions())
                };
            }

            // 处理数值类型
            if (type == typeof(int) || type == typeof(long))
            {
                var config = new JObject
                {
                    ["type"] = "input-number",
                    ["precision"] = 0  // 整数不需要小数位
                };

                // 获取Range特性的限制
                var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null)
                {
                    config["min"] = Convert.ToInt64(rangeAttr.Minimum);
                    config["max"] = Convert.ToInt64(rangeAttr.Maximum);
                }

                return config;
            }

            // 处理浮点数类型
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                var config = new JObject
                {
                    ["type"] = "input-number",
                    ["precision"] = 2  // 默认保留2位小数
                };

                // 获取Range特性的限制
                var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null)
                {
                    config["min"] = Convert.ToDouble(rangeAttr.Minimum);
                    config["max"] = Convert.ToDouble(rangeAttr.Maximum);
                }

                return config;
            }

            // 处理布尔类型
            if (type == typeof(bool))
            {
                return new JObject
                {
                    ["type"] = "switch"
                };
            }

            // 处理日期时间类型
            if (type == typeof(DateTime))
            {
                return new JObject
                {
                    ["type"] = "input-datetime"
                };
            }

            // 处理日期类型
            if (type == typeof(DateOnly))
            {
                return new JObject
                {
                    ["type"] = "input-date"
                };
            }

            // 处理时间类型
            if (type == typeof(TimeOnly))
            {
                return new JObject
                {
                    ["type"] = "input-time"
                };
            }

            // 获取字符串相关的验证特性
            if (type == typeof(string))
            {
                var config = new JObject
                {
                    ["type"] = "input-text"
                };

                // 处理最大长度限制
                var maxLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
                if (maxLengthAttr != null)
                {
                    config["maxLength"] = maxLengthAttr.MaximumLength;
                }

                // 处理必填验证
                var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttr != null)
                {
                    config["required"] = true;
                    config["requiredErrorMsg"] = requiredAttr.ErrorMessage;
                }

                return config;
            }

            // 默认使用文本输入
            return new JObject
            {
                ["type"] = "input-text"
            };
        }
    }
}