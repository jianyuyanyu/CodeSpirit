// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Resources;

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
                        displayName = m.GetDisplayName(utilityHelper);
                        fieldName = m.GetFieldName(null);
                        return true;
                    }
                    break;

                case ParameterInfo p:
                    attr = p.GetCustomAttribute<T>();
                    if (attr != null)
                    {
                        displayName = p.GetDisplayName(utilityHelper);
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

        public static JObject CreateFormField(
            this ICustomAttributeProvider member, 
            string fieldName = null, 
            string lableName = null,
            UtilityHelper? utilityHelper = null)
        {
            (string name, Type type, string label) = member.GetMemberMetadata(utilityHelper);
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
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">实用工具类（可选，用于获取当前语言）</param>
        public static (string name, Type type, string label) GetMemberMetadata(
            this ICustomAttributeProvider member, 
            UtilityHelper? utilityHelper = null)
        {
            return member switch
            {
                ParameterInfo p => (
                    p.GetFieldName(null),
                    p.ParameterType,
                    p.GetDisplayName(utilityHelper)
                ),
                PropertyInfo prop => (
                    prop.GetFieldName(null),
                    prop.PropertyType,
                    prop.GetDisplayName(utilityHelper)
                ),
                _ => throw new NotSupportedException("不支持除参数和属性外的其他成员类型")
            };
        }

        /// <summary>
        /// 获取成员的显示名称，优先使用 DisplayAttribute（支持 ResourceType 多语言），然后使用 DisplayNameAttribute。
        /// </summary>
        /// <param name="member">成员信息</param>
        /// <param name="utilityHelper">实用工具类（可选，用于获取当前语言）</param>
        public static string GetDisplayName(this ICustomAttributeProvider member, UtilityHelper? utilityHelper = null)
        {
            // 获取当前语言文化信息，优先从 UtilityHelper 获取（如果提供）
            CultureInfo currentCulture = utilityHelper != null 
                ? utilityHelper.GetCurrentCulture() 
                : GetCurrentCulture();
            
            // 优先检查 DisplayAttribute（支持多语言资源）
            var displayAttr = member switch
            {
                MemberInfo m => m.GetCustomAttribute<DisplayAttribute>(),
                ParameterInfo p => p.GetCustomAttribute<DisplayAttribute>(),
                _ => null
            };

            if (displayAttr != null)
            {
                // 如果指定了 ResourceType，从资源文件中获取本地化文本
                if (displayAttr.ResourceType != null && !string.IsNullOrEmpty(displayAttr.Name))
                {
                    try
                    {
                        var resourceType = displayAttr.ResourceType;
                        
                        // 优先使用 ResourceManager 并传入明确的 CultureInfo
                        // 这样可以确保使用正确的语言，而不依赖于线程的 CultureInfo.CurrentUICulture
                        var resourceManagerProp = resourceType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
                        if (resourceManagerProp != null)
                        {
                            var resourceManager = resourceManagerProp.GetValue(null) as ResourceManager;
                            if (resourceManager != null)
                            {
                                // 使用明确的 CultureInfo 获取本地化文本
                                // ResourceManager 会自动查找对应的资源文件（如 Display.en.resx）
                                // 注意：如果 currentCulture 是 "en"，ResourceManager 会查找 Display.en.resx
                                
                                // 先获取默认资源文件中的值（用于比较）
                                var defaultCulture = new CultureInfo("zh-CN");
                                var defaultText = resourceManager.GetString(displayAttr.Name, defaultCulture);
                                
                                // 使用当前文化获取本地化文本
                                var localizedText = resourceManager.GetString(displayAttr.Name, currentCulture);
                                
                                // 如果获取到了本地化文本
                                if (!string.IsNullOrEmpty(localizedText))
                                {
                                    // 如果当前文化是英文，且返回的文本与默认文本不同，说明找到了英文资源文件
                                    if (currentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // 如果返回的文本与默认文本不同，说明找到了英文资源文件
                                        if (localizedText != defaultText)
                                        {
                                            return localizedText;
                                        }
                                        
                                        // 如果相同，可能回退到了默认资源文件，尝试直接使用 "en" 文化
                                        var enCulture = new CultureInfo("en");
                                        var enText = resourceManager.GetString(displayAttr.Name, enCulture);
                                        if (!string.IsNullOrEmpty(enText) && enText != defaultText)
                                        {
                                            return enText;
                                        }
                                    }
                                    else
                                    {
                                        // 非英文文化，直接返回获取到的文本
                                        return localizedText;
                                    }
                                }
                            }
                        }
                        
                        // 如果 ResourceManager 不可用，尝试通过静态属性获取
                        var staticProp = resourceType.GetProperty(displayAttr.Name, BindingFlags.Public | BindingFlags.Static);
                        if (staticProp != null && staticProp.PropertyType == typeof(string))
                        {
                            // 临时设置 CultureInfo 以确保静态属性使用正确的语言
                            var originalCulture = CultureInfo.CurrentUICulture;
                            try
                            {
                                CultureInfo.CurrentUICulture = currentCulture;
                                var value = staticProp.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    return value;
                                }
                            }
                            finally
                            {
                                CultureInfo.CurrentUICulture = originalCulture;
                            }
                        }
                    }
                    catch
                    {
                        // 如果资源获取失败，回退到 Name 属性
                    }
                }
                
                // 如果没有 ResourceType 或资源获取失败，使用 Name 属性
                if (!string.IsNullOrEmpty(displayAttr.Name))
                {
                    return displayAttr.Name;
                }
            }

            // 回退到 DisplayNameAttribute
            return member switch
            {
                MemberInfo m => m.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? m.Name.ToTitleCase(),
                ParameterInfo p => p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name.ToTitleCase(),
                _ => member?.ToString()?.ToTitleCase()
            };
        }

        /// <summary>
        /// 获取当前请求的语言文化信息（当没有 UtilityHelper 时使用）
        /// </summary>
        private static CultureInfo GetCurrentCulture()
        {
            // 直接使用 CultureInfo.CurrentUICulture
            // 这应该由 UseCodeSpiritRequestLocalization 中间件设置
            // 中间件执行顺序：UseCodeSpiritRequestLocalization (第212行) -> UseAmis (第227行)
            // 所以当 AMIS 中间件执行时，CultureInfo.CurrentUICulture 应该已经正确设置
            // 如果仍然显示中文，可能是中间件设置的文化信息不正确，或者资源文件没有正确加载
            return CultureInfo.CurrentUICulture;
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
