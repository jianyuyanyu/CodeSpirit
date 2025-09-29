// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    public static class CustomAttributeProviderExtensions
    {
        /// <summary>
        /// 尝试获取 AmisFieldAttribute 及相关信息。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <param name="attr">输出的 AmisFieldAttribute 实例。</param>
        /// <param name="displayName">输出的显示名称。</param>
        /// <param name="fieldName">输出的字段名称。</param>
        /// <returns>如果成功获取则返回 true，否则返回 false。</returns>
        public static bool TryGetAmisFieldData(this ICustomAttributeProvider member, UtilityHelper utilityHelper, out AmisFormFieldAttribute attr, out string displayName, out string fieldName)
        {
            return member.TryGetAmisFieldData<AmisFormFieldAttribute>(utilityHelper, out attr, out displayName, out fieldName);
        }

        /// <summary>
        /// 尝试获取 AmisFieldAttribute 及相关信息。
        /// </summary>
        /// <param name="member">成员信息（MemberInfo 或 ParameterInfo）。</param>
        /// <param name="utilityHelper">实用工具类。</param>
        /// <param name="attr">输出的 AmisFieldAttribute 实例。</param>
        /// <param name="displayName">输出的显示名称。</param>
        /// <param name="fieldName">输出的字段名称。</param>
        /// <returns>如果成功获取则返回 true，否则返回 false。</returns>
        public static bool TryGetAmisFieldData<T>(this ICustomAttributeProvider member, UtilityHelper utilityHelper, out T attr, out string displayName, out string fieldName) where T : AmisFormFieldAttribute
        {
            displayName = null;
            fieldName = null;

            switch (member)
            {
                case MemberInfo m:
                    attr = m.GetCustomAttribute<T>();
                    if (attr != null)
                    {
                        displayName = m.GetDisplayName();
                        fieldName = m.GetFieldName(null);
                        return true;
                    }
                    break;

                case ParameterInfo p:
                    attr = p.GetCustomAttribute<T>();
                    if (attr != null)
                    {
                        displayName = p.GetDisplayName();
                        fieldName = p.GetFieldName(null);
                        return true;
                    }
                    break;

                default:
                    break;
            }
            attr = null;
            return false;
        }

        public static JObject CreateFormField(this ICustomAttributeProvider member, string fieldName = null, string lableName = null)
        {
            (string name, Type type, string label) = member.GetMemberMetadata();
            bool isRequired = IsRequired(member);

            JObject field = new()
            {
                ["name"] = fieldName ?? name,
                ["label"] = lableName ?? label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(type)
            };

            AddCommonValidations(member, field);
            AddTypeSpecificConfigurations(member, type, field);

            return field;
        }

        /// <summary>
        /// 获取成员元数据
        /// </summary>
        public static (string name, Type type, string label) GetMemberMetadata(this ICustomAttributeProvider member)
        {
            return member switch
            {
                ParameterInfo p => (
                    p.GetFieldName(null),
                    p.ParameterType,
                    p.GetDisplayName()
                ),
                PropertyInfo prop => (
                    prop.GetFieldName(null),
                    prop.PropertyType,
                    prop.GetDisplayName()
                ),
                _ => throw new NotSupportedException("不支持除参数和属性外的其他成员类型")
            };
        }

        /// <summary>
        /// 获取成员的显示名称，优先使用 DisplayNameAttribute。
        /// </summary>
        public static string GetDisplayName(this ICustomAttributeProvider member)
        {
            return member switch
            {
                MemberInfo m => m.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? m.Name.ToTitleCase(),
                ParameterInfo p => p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name.ToTitleCase(),
                _ => member?.ToString()?.ToTitleCase()
            };
        }

        /// <summary>
        /// 构建字段名称，支持嵌套对象。
        /// </summary>
        public static string GetFieldName(this ICustomAttributeProvider member, string parentName)
        {
            string name = member switch
            {
                MemberInfo m => m.Name,
                ParameterInfo p => p.Name,
                _ => throw new NotSupportedException("Unsupported member type")
            };

            return parentName != null
                ? $"{parentName}.{name}".ToCamelCase()
                : name.ToCamelCase();
        }

        /// <summary>
        /// 判断字段是否必填
        /// </summary>
        public static bool IsRequired(this ICustomAttributeProvider member)
        {
            return member.GetAttribute<RequiredAttribute>() != null
                || (member.GetMemberType()).IsTypeRequired();
        }

        /// <summary>
        /// 获取字段类型映射
        /// </summary>
        public static string GetFormFieldType(this Type type)
        {
            if (type.IsEnumType())
            {
                return "select";
            }

            return type.IsDateType()
                ? "input-date"
                : type switch
                {
                    Type t when t == typeof(string) => "input-text",
                    Type t when t == typeof(bool) || t == typeof(bool?) => "switch",
                    Type t when t.IsNumericType() => "input-number",
                    Type t when t.IsImageType() => "image",
                    _ => "input-text"
                };
        }

        #region 验证规则
        /// <summary>
        /// 添加通用验证规则
        /// </summary>
        private static void AddCommonValidations(ICustomAttributeProvider member, JObject field)
        {
            AddValidationAttributes(member, field);
            AddDescription(member, field);
        }

        /// <summary>
        /// 添加验证特性配置
        /// </summary>
        private static void AddValidationAttributes(ICustomAttributeProvider member, JObject field)
        {
            JObject validations = [];
            JObject errors = [];

            ProcessStringLengthAttribute(member, validations, errors);
            ProcessRangeAttribute(member, validations, errors);
            ProcessRegexAttribute(member, validations, errors);
            ProcessDataTypeAttribute(member, validations, field);

            if (validations.HasValues)
            {
                field["validations"] = validations;
            }

            if (errors.HasValues)
            {
                field["validationErrors"] = errors;
            }
        }

        /// <summary>
        /// 处理字符串长度验证
        /// </summary>
        private static void ProcessStringLengthAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            // 处理 StringLengthAttribute
            StringLengthAttribute stringLengthAttr = member.GetAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                {
                    validations["minLength"] = stringLengthAttr.MinimumLength;
                }

                if (stringLengthAttr.MaximumLength > 0)
                {
                    validations["maxLength"] = stringLengthAttr.MaximumLength;
                }

                if (!string.IsNullOrEmpty(stringLengthAttr.ErrorMessage))
                {
                    errors["minLength"] = stringLengthAttr.ErrorMessage;
                    errors["maxLength"] = stringLengthAttr.ErrorMessage;
                }
            }

            // 处理 MinLengthAttribute
            MinLengthAttribute minLengthAttr = member.GetAttribute<MinLengthAttribute>();
            if (minLengthAttr != null)
            {
                validations["minLength"] = minLengthAttr.Length; // MinLengthAttribute 的 Length 表示最小长度

                if (!string.IsNullOrEmpty(minLengthAttr.ErrorMessage))
                {
                    errors["minLength"] = minLengthAttr.ErrorMessage;
                }
            }

            // 处理 MaxLengthAttribute
            MaxLengthAttribute maxLengthAttr = member.GetAttribute<MaxLengthAttribute>();
            if (maxLengthAttr != null && maxLengthAttr.Length > 0)
            {
                validations["maxLength"] = maxLengthAttr.Length; // MaxLengthAttribute 的 Length 表示最大长度

                if (!string.IsNullOrEmpty(maxLengthAttr.ErrorMessage))
                {
                    errors["maxLength"] = maxLengthAttr.ErrorMessage;
                }
            }
        }


        /// <summary>
        /// 处理数值范围验证
        /// </summary>
        private static void ProcessRangeAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            RangeAttribute attr = member.GetAttribute<RangeAttribute>();
            if (attr == null)
            {
                return;
            }

            if (attr.Minimum != null)
            {
                validations["minimum"] = Convert.ToDouble(attr.Minimum);
            }

            if (attr.Maximum != null)
            {
                validations["maximum"] = Convert.ToDouble(attr.Maximum);
            }

            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                errors["minimum"] = attr.ErrorMessage;
                errors["maximum"] = attr.ErrorMessage;
            }
        }

        /// <summary>
        /// 处理正则表达式验证
        /// </summary>
        private static void ProcessRegexAttribute(ICustomAttributeProvider member, JObject validations, JObject errors)
        {
            RegularExpressionAttribute attr = member.GetAttribute<RegularExpressionAttribute>();
            if (attr == null)
            {
                return;
            }

            validations["matchRegexp"] = attr.Pattern;
            if (!string.IsNullOrEmpty(attr.ErrorMessage))
            {
                errors["matchRegexp"] = attr.ErrorMessage;
            }
        }

        /// <summary>
        /// 处理数据类型验证
        /// </summary>
        private static void ProcessDataTypeAttribute(ICustomAttributeProvider member, JObject validations, JObject field)
        {
            DataTypeAttribute attr = member.GetAttribute<DataTypeAttribute>();
            if (attr == null)
            {
                return;
            }

            switch (attr.DataType)
            {
                case DataType.EmailAddress:
                    validations["isEmail"] = true;
                    break;
                case DataType.Url:
                    validations["isUrl"] = true;
                    break;
                case DataType.ImageUrl:
                    HandleImageType(member, field);
                    break;
            }
        }

        /// <summary>
        /// 添加描述信息
        /// </summary>
        private static void AddDescription(ICustomAttributeProvider member, JObject field)
        {
            string description = member.GetAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrEmpty(description))
            {
                field["description"] = description;
            }
        }
        #endregion

        #region 类型特定配置
        /// <summary>
        /// 添加类型相关特殊配置
        /// </summary>
        private static void AddTypeSpecificConfigurations(ICustomAttributeProvider member, Type type, JObject field)
        {
            if (type.IsEnumType())
            {
                field["options"] = type.GetEnumOptions();
            }

            if (type.IsDateType())
            {
                field["format"] = "YYYY-MM-DD";
            }

            HandleImageType(member, field);
        }

        /// <summary>
        /// 处理图片类型字段的特殊配置
        /// </summary>
        private static void HandleImageType(ICustomAttributeProvider member, JObject field)
        {
            DataTypeAttribute dataTypeAttr = member.GetAttribute<DataTypeAttribute>();
            if (dataTypeAttr?.DataType == DataType.ImageUrl)
            {
                field["type"] = "image";
                field["src"] = $"${{{field["name"]}}}";
                field["altText"] = field["label"];
            }

            if (member.GetMemberName().Contains("Avatar", StringComparison.OrdinalIgnoreCase))
            {
                field["type"] = "avatar";
                field["src"] = $"${{{field["name"]}}}";
            }
        }
        #endregion

        #region 表单项组扩展方法
        /// <summary>
        /// 获取类型上的表单项组特性列表
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>表单项组特性列表</returns>
        public static List<FormGroupAttribute> GetFormGroups(this Type type)
        {
            if (type == null) return [];
            
            return type.GetCustomAttributes<FormGroupAttribute>(true)?.ToList() ?? [];
        }

        /// <summary>
        /// 判断类型是否定义了表单项组
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否定义了表单项组</returns>
        public static bool HasFormGroups(this Type type)
        {
            return type?.GetCustomAttributes<FormGroupAttribute>(true)?.Any() == true;
        }

        /// <summary>
        /// 获取属性所属的表单项组
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>所属的表单项组，如果不属于任何组则返回null</returns>
        public static FormGroupAttribute GetBelongingFormGroup(this PropertyInfo property, Type dtoType)
        {
            if (property == null || dtoType == null) return null;

            var groups = dtoType.GetFormGroups();
            return groups.FirstOrDefault(g => 
                !string.IsNullOrEmpty(g.Fields) && 
                g.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Contains(property.Name, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 判断属性是否属于某个表单项组
        /// </summary>
        /// <param name="property">属性信息</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>是否属于表单项组</returns>
        public static bool BelongsToFormGroup(this PropertyInfo property, Type dtoType)
        {
            return property.GetBelongingFormGroup(dtoType) != null;
        }

        /// <summary>
        /// 获取表单项组中的所有属性
        /// </summary>
        /// <param name="groupAttribute">表单项组特性</param>
        /// <param name="dtoType">DTO类型</param>
        /// <returns>组中的属性列表</returns>
        public static List<PropertyInfo> GetGroupProperties(this FormGroupAttribute groupAttribute, Type dtoType)
        {
            if (groupAttribute == null || dtoType == null || string.IsNullOrEmpty(groupAttribute.Fields))
                return [];

            var fieldNames = groupAttribute.Fields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => fieldNames.Contains(p.Name))
                .ToList();
        }

        ///// <summary>
        ///// 创建表单项组的AMIS配置
        ///// </summary>
        ///// <param name="groupAttribute">表单项组特性</param>
        ///// <param name="groupFields">组内字段配置列表</param>
        ///// <returns>表单项组的AMIS配置</returns>
        //public static JObject CreateFormGroupConfig(this FormGroupAttribute groupAttribute, List<JObject> groupFields = null)
        //{
        //    if (groupAttribute == null) return null;

        //    var groupConfig = new JObject
        //    {
        //        ["type"] = "group"
        //    };

        //    // 设置基础属性
        //    if (!string.IsNullOrEmpty(groupAttribute.Title))
        //        groupConfig["label"] = groupAttribute.Title;

        //    if (!string.IsNullOrEmpty(groupAttribute.Description))
        //        groupConfig["description"] = groupAttribute.Description;

        //    if (!string.IsNullOrEmpty(groupAttribute.Name))
        //        groupConfig["name"] = groupAttribute.Name;

        //    // 设置显示模式
        //    if (groupAttribute.Mode != FormGroupMode.Normal)
        //        groupConfig["mode"] = GetFormGroupModeString(groupAttribute.Mode);

        //    // 设置间距
        //    if (groupAttribute.Gap != FormGroupGap.Normal)
        //        groupConfig["gap"] = GetFormGroupGapString(groupAttribute.Gap);

        //    // 设置方向
        //    if (groupAttribute.Direction == FormGroupDirection.Horizontal)
        //        groupConfig["direction"] = "horizontal";

        //    // 设置其他属性
        //    if (groupAttribute.ShowBorder)
        //        groupConfig["showBorder"] = true;

        //    if (!string.IsNullOrEmpty(groupAttribute.ClassName))
        //        groupConfig["className"] = groupAttribute.ClassName;

        //    if (!string.IsNullOrEmpty(groupAttribute.VisibleOn))
        //        groupConfig["visibleOn"] = groupAttribute.VisibleOn;

        //    if (groupAttribute.Hidden)
        //        groupConfig["hidden"] = true;

        //    if (groupAttribute.Disabled)
        //        groupConfig["disabled"] = true;

        //    if (!string.IsNullOrEmpty(groupAttribute.DisabledOn))
        //        groupConfig["disabledOn"] = groupAttribute.DisabledOn;

        //    // 处理自定义配置
        //    if (!string.IsNullOrEmpty(groupAttribute.AdditionalConfig))
        //    {
        //        try
        //        {
        //            var additionalConfig = JObject.Parse(groupAttribute.AdditionalConfig);
        //            groupConfig.Merge(additionalConfig, new JsonMergeSettings
        //            {
        //                MergeArrayHandling = MergeArrayHandling.Union
        //            });
        //        }
        //        catch (Exception)
        //        {
        //            // 忽略JSON解析错误
        //        }
        //    }

        //    // 设置组内字段
        //    groupConfig["body"] = new JArray(groupFields ?? []);

        //    return groupConfig;
        //}

        /// <summary>
        /// 获取表单项组模式字符串
        /// </summary>
        /// <param name="mode">模式枚举</param>
        /// <returns>模式字符串</returns>
        private static string GetFormGroupModeString(FormGroupMode mode)
        {
            return mode switch
            {
                FormGroupMode.Inline => "inline",
                FormGroupMode.Horizontal => "horizontal",
                _ => "normal"
            };
        }

        /// <summary>
        /// 获取表单项组间距字符串
        /// </summary>
        /// <param name="gap">间距枚举</param>
        /// <returns>间距字符串</returns>
        private static string GetFormGroupGapString(FormGroupGap gap)
        {
            return gap switch
            {
                FormGroupGap.None => "none",
                FormGroupGap.Small => "sm",
                FormGroupGap.Large => "lg",
                _ => "base"
            };
        }
        #endregion
    }
}
