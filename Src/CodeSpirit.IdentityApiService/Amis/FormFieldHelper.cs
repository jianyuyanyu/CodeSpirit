using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using CodeSpirit.IdentityApi.Amis.Attributes;
using CodeSpirit.IdentityApi.Authorization;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    /// <summary>
    /// 帮助类，用于生成 AMIS 表单的字段配置。
    /// </summary>
    public class FormFieldHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;

        /// <summary>
        /// 初始化 <see cref="FormFieldHelper"/> 的新实例。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">实用工具类，提供辅助方法。</param>
        public FormFieldHelper(PermissionService permissionService, UtilityHelper utilityHelper)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
        }

        /// <summary>
        /// 根据方法参数生成 AMIS 表单字段的配置列表。
        /// </summary>
        /// <param name="parameters">控制器方法的参数列表。</param>
        /// <returns>AMIS 表单字段的 JSON 对象列表。</returns>
        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            var fields = new List<JObject>();

            if (parameters == null)
                return fields;

            foreach (var param in parameters)
            {
                // 检查当前用户是否有权限编辑该参数
                if (!HasEditPermission(param))
                    continue;

                // 获取自定义的 AmisInputImageFieldAttribute
                var inputImageAttr = param.GetCustomAttribute<AmisInputImageFieldAttribute>();
                if (inputImageAttr != null)
                {
                    fields.Add(CreateAmisInputImageFormFieldFromAttribute(param.Member, inputImageAttr));
                    continue;
                }

                // 获取自定义的 AmisSelectFieldAttribute
                var selectAmisAttr = param.GetCustomAttribute<AmisSelectFieldAttribute>();
                if (selectAmisAttr != null)
                {
                    // 使用自定义特性配置生成 select 字段
                    fields.Add(CreateAmisSelectFormFieldFromAttribute(param.Member, selectAmisAttr));
                    continue;
                }

                // 获取自定义的 AmisFieldAttribute
                var amisAttr = param.GetCustomAttribute<AmisFieldAttribute>();
                if (amisAttr != null)
                {
                    // 使用自定义特性配置生成字段
                    fields.Add(CreateAmisFormFieldFromAttribute(param.Member, amisAttr));
                    continue;
                }

                // 根据参数类型生成相应的表单字段
                if (_utilityHelper.IsSimpleType(param.ParameterType))
                {
                    // 忽略不需要编辑的参数
                    if (!IsIgnoredParameter(param))
                    {
                        fields.Add(CreateAmisFormField(param));
                    }
                }
                else if (_utilityHelper.IsComplexType(param.ParameterType))
                {
                    // 处理复杂类型（如嵌套对象）
                    var nestedProperties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in nestedProperties)
                    {
                        // 忽略不需要编辑的属性
                        if (IsIgnoredProperty(prop))
                            continue;

                        // 检查当前用户是否有权限编辑该属性
                        if (!HasEditPermission(prop))
                            continue;

                        // 获取自定义的 AmisInputImageFieldAttribute
                        var nestedInputImageAttr = prop.GetCustomAttribute<AmisInputImageFieldAttribute>();
                        if (nestedInputImageAttr != null)
                        {
                            fields.Add(CreateAmisInputImageFormFieldFromAttribute(prop, nestedInputImageAttr, parentName: param.Name));
                            continue;
                        }

                        // 获取自定义的 AmisSelectFieldAttribute
                        var nestedSelectAmisAttr = prop.GetCustomAttribute<AmisSelectFieldAttribute>();
                        if (nestedSelectAmisAttr != null)
                        {
                            fields.Add(CreateAmisSelectFormFieldFromAttribute(prop, nestedSelectAmisAttr, parentName: param.Name));
                            continue;
                        }

                        // 获取自定义的 AmisFieldAttribute
                        var nestedAmisAttr = prop.GetCustomAttribute<AmisFieldAttribute>();
                        if (nestedAmisAttr != null)
                        {
                            fields.Add(CreateAmisFormFieldFromAttribute(prop, nestedAmisAttr, parentName: param.Name));
                            continue;
                        }

                        // 默认处理
                        fields.Add(CreateAmisFormFieldFromProperty(prop, param.Name));
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// 创建 AMIS 表单的 select 类型字段配置，基于自定义的 AmisSelectFieldAttribute。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <param name="amisAttr">AmisSelectFieldAttribute 的实例。</param>
        /// <param name="parentName">父级参数名称，用于嵌套对象。</param>
        /// <returns>AMIS 表单 select 字段的 JSON 对象。</returns>
        private JObject CreateAmisSelectFormFieldFromAttribute(MemberInfo member, AmisSelectFieldAttribute amisAttr, string parentName = null)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var displayName = member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(member.Name);
            // 转换为 camelCase，并包含父级名称（如嵌套对象）
            var fieldName = parentName != null ? _utilityHelper.ToCamelCase($"{parentName}.{member.Name}") : _utilityHelper.ToCamelCase(member.Name);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = amisAttr.Label ?? displayName,
                ["type"] = amisAttr.Type,
                ["source"] = amisAttr.Source,
                ["valueField"] = amisAttr.ValueField,
                ["labelField"] = amisAttr.LabelField,
                ["multiple"] = amisAttr.Multiple,
                ["joinValues"] = amisAttr.JoinValues,
                ["extractValue"] = amisAttr.ExtractValue,
                ["searchable"] = amisAttr.Searchable,
                ["clearable"] = amisAttr.Clearable,
                ["required"] = amisAttr.Required,
                ["placeholder"] = amisAttr.Placeholder
            };

            // 处理额外的自定义配置
            if (!string.IsNullOrEmpty(amisAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(amisAttr.AdditionalConfig);
                    foreach (var prop in additionalConfig.Properties())
                    {
                        field[prop.Name] = prop.Value;
                    }
                }
                catch (Exception ex)
                {
                    // 处理 JSON 解析错误（可记录日志或抛出异常）
                    throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
                }
            }

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的通用字段配置，基于自定义的 AmisFieldAttribute。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <param name="amisAttr">AmisFieldAttribute 的实例。</param>
        /// <param name="parentName">父级参数名称，用于嵌套对象。</param>
        /// <returns>AMIS 表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormFieldFromAttribute(MemberInfo member, AmisFieldAttribute amisAttr, string parentName = null)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var displayName = member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(member.Name);
            // 转换为 camelCase，并包含父级名称（如嵌套对象）
            var fieldName = parentName != null ? _utilityHelper.ToCamelCase($"{parentName}.{member.Name}") : _utilityHelper.ToCamelCase(member.Name);
            // 判断是否为必填字段
            var isRequired = amisAttr.Required || !_utilityHelper.IsNullable(_utilityHelper.GetMemberType(member));

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = amisAttr.Label ?? displayName,
                ["required"] = isRequired,
                ["type"] = amisAttr.Type,
                ["placeholder"] = amisAttr.Placeholder
            };

            // 处理额外的自定义配置
            if (!string.IsNullOrEmpty(amisAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(amisAttr.AdditionalConfig);
                    foreach (var prop in additionalConfig.Properties())
                    {
                        field[prop.Name] = prop.Value;
                    }
                }
                catch (Exception ex)
                {
                    // 处理 JSON 解析错误（可记录日志或抛出异常）
                    throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
                }
            }

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的通用字段配置，基于属性信息。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <param name="parentName">父级参数名称，用于嵌套对象。</param>
        /// <returns>AMIS 表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormFieldFromProperty(PropertyInfo prop, string parentName)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            // 转换为 camelCase，并包含父级名称（如嵌套对象）
            var fieldName = parentName != null ? _utilityHelper.ToCamelCase($"{prop.Name}") : _utilityHelper.ToCamelCase(prop.Name);
            // 判断是否为必填字段
            var isRequired = !_utilityHelper.IsNullable(prop.PropertyType) || prop.GetCustomAttribute<RequiredAttribute>() != null;

            // 创建基本的字段配置
            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = displayName,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(prop.PropertyType)
            };

            // 添加验证规则
            AddValidationRules(prop, field);

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的单个字段配置（通用）。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>AMIS 表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormField(ParameterInfo param)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(param.Name);
            // 转换为 camelCase
            var fieldName = _utilityHelper.ToCamelCase(param.Name);
            // 判断是否为必填字段
            var isRequired = !_utilityHelper.IsNullable(param.ParameterType) || param.GetCustomAttribute<RequiredAttribute>() != null;

            // 创建基本的字段配置
            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(param.ParameterType)
            };

            // 添加验证规则
            AddValidationRulesFromParameter(param, field);

            return field;
        }

        /// <summary>
        /// 根据类型确定 AMIS 表单字段的类型。
        /// </summary>
        /// <param name="type">字段的类型。</param>
        /// <returns>AMIS 表单字段的类型字符串。</returns>
        private string GetFormFieldType(Type type)
        {
            // 如果是枚举类型，则返回 'select'
            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
                return "select";

            // 根据具体类型映射到 AMIS 表单字段类型
            return type switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.EmailAddress => "input-email",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.Password => "input-password",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.ImageUrl => "image",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.Url => "input-text",
                _ => "input-text"
            };
        }

        /// <summary>
        /// 添加参数级别的验证规则到字段配置中。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <param name="field">AMIS 表单字段的 JSON 对象。</param>
        private void AddValidationRulesFromParameter(ParameterInfo param, JObject field)
        {
            var validationRules = new JObject();

            // 处理 [Required] 特性
            if (param.GetCustomAttribute<RequiredAttribute>() != null)
            {
                field["required"] = true;
                validationRules["required"] = true;
            }

            // 处理 [StringLength] 特性
            var stringLengthAttr = param.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            // 处理 [Range] 特性
            var rangeAttr = param.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            // 处理 [DataType] 特性（如 Email, Url）
            var dataTypeAttr = param.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null)
            {
                switch (dataTypeAttr.DataType)
                {
                    case DataType.EmailAddress:
                        validationRules["pattern"] = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                        validationRules["errorMessage"] = "请输入有效的电子邮件地址";
                        break;
                    case DataType.Url:
                        validationRules["pattern"] = @"^(http|https)://[^ ""]+$";
                        validationRules["errorMessage"] = "请输入有效的URL";
                        break;
                        // 可根据需要添加更多 DataType 处理
                }
            }

            // 如果有验证规则，则添加到字段配置中
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            // 如果是枚举类型，设置选项
            if (param.ParameterType.IsEnum || Nullable.GetUnderlyingType(param.ParameterType)?.IsEnum == true)
            {
                field["options"] = _utilityHelper.GetEnumOptions(param.ParameterType);
            }

            // 如果是日期类型，设置格式
            if (param.ParameterType == typeof(DateTime) || param.ParameterType == typeof(DateTime?))
            {
                field["format"] = "YYYY-MM-DD";
            }
        }

        /// <summary>
        /// 添加属性级别的验证规则到字段配置中。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <param name="field">AMIS 表单字段的 JSON 对象。</param>
        private void AddValidationRules(PropertyInfo prop, JObject field)
        {
            var validationRules = new JObject();

            // 处理 [Required] 特性
            if (prop.GetCustomAttribute<RequiredAttribute>() != null)
            {
                field["required"] = true;
                validationRules["required"] = true;
            }

            // 处理 [StringLength] 特性
            var stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            // 处理 [Range] 特性
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            // 处理 [DataType] 特性（如 Email, Url, ImageUrl）
            var dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null)
            {
                switch (dataTypeAttr.DataType)
                {
                    case DataType.EmailAddress:
                        validationRules["pattern"] = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                        validationRules["errorMessage"] = "请输入有效的电子邮件地址";
                        break;
                    case DataType.Url:
                        validationRules["pattern"] = @"^(http|https)://[^ ""]+$";
                        validationRules["errorMessage"] = "请输入有效的URL";
                        break;
                        // 可根据需要添加更多 DataType 处理
                }
            }

            // 如果有验证规则，则添加到字段配置中
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            // 如果是枚举类型，设置选项
            if (prop.PropertyType.IsEnum || Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true)
            {
                field["options"] = _utilityHelper.GetEnumOptions(prop.PropertyType);
            }

            // 如果是日期类型，设置格式
            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                field["format"] = "YYYY-MM-DD";
            }

            // 如果是图片类型，设置为图片选择
            if (dataTypeAttr?.DataType == DataType.ImageUrl)
            {
                field["type"] = "image";
                field["src"] = $"${{{field["name"]}}}"; // 设置图片来源字段
                field["altText"] = field["label"];
                field["className"] = "image-field"; // 可选：添加自定义样式类
            }

            // 如果是头像类型，设置为头像选择
            if (prop.Name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                field["type"] = "avatar";
                field["src"] = $"${{{field["name"]}}}"; // 设置头像来源字段
                field["altText"] = field["label"];
                //field["className"] = "avatar-field"; // 可选：添加自定义样式类
            }
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑该参数。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasEditPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑该属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasEditPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 创建 AMIS 表单的 select 类型字段配置，基于自定义的 AmisSelectFieldAttribute。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <param name="amisAttr">AmisSelectFieldAttribute 的实例。</param>
        /// <returns>AMIS 表单 select 字段的 JSON 对象。</returns>
        private JObject CreateAmisSelectFormFieldFromAttribute(MemberInfo member, AmisSelectFieldAttribute amisAttr)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var displayName = member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(member.Name);
            // 转换为 camelCase
            var fieldName = _utilityHelper.ToCamelCase(member.Name);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = amisAttr.Label ?? displayName,
                ["type"] = amisAttr.Type,
                ["source"] = amisAttr.Source,
                ["valueField"] = amisAttr.ValueField,
                ["labelField"] = amisAttr.LabelField,
                ["multiple"] = amisAttr.Multiple,
                ["joinValues"] = amisAttr.JoinValues,
                ["extractValue"] = amisAttr.ExtractValue,
                ["searchable"] = amisAttr.Searchable,
                ["clearable"] = amisAttr.Clearable,
                ["required"] = amisAttr.Required,
                ["placeholder"] = amisAttr.Placeholder
            };

            // 处理额外的自定义配置
            if (!string.IsNullOrEmpty(amisAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(amisAttr.AdditionalConfig);
                    foreach (var prop in additionalConfig.Properties())
                    {
                        field[prop.Name] = prop.Value;
                    }
                }
                catch (Exception ex)
                {
                    // 处理 JSON 解析错误（可记录日志或抛出异常）
                    throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
                }
            }

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的 InputImage 类型字段配置，基于自定义的 AmisInputImageFieldAttribute。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <param name="inputImageAttr">AmisInputImageFieldAttribute 的实例。</param>
        /// <param name="parentName">父级参数名称，用于嵌套对象。</param>
        /// <returns>AMIS 表单 InputImage 字段的 JSON 对象。</returns>
        private JObject CreateAmisInputImageFormFieldFromAttribute(MemberInfo member, AmisInputImageFieldAttribute inputImageAttr, string parentName = null)
        {
            // 获取显示名称，优先使用 DisplayNameAttribute
            var displayName = member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(member.Name);
            // 转换为 camelCase，并包含父级名称（如嵌套对象）
            var fieldName = parentName != null ? _utilityHelper.ToCamelCase($"{parentName}.{member.Name}") : _utilityHelper.ToCamelCase(member.Name);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = inputImageAttr.Label ?? displayName,
                ["type"] = inputImageAttr.Type,
                ["uploadUrl"] = inputImageAttr.UploadUrl,
                ["accept"] = inputImageAttr.Accept,
                ["maxSize"] = inputImageAttr.MaxSize,
                ["multiple"] = inputImageAttr.Multiple,
                ["required"] = inputImageAttr.Required,
                ["placeholder"] = inputImageAttr.Placeholder
            };

            // 处理额外的自定义配置
            if (!string.IsNullOrEmpty(inputImageAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(inputImageAttr.AdditionalConfig);
                    foreach (var prop in additionalConfig.Properties())
                    {
                        field[prop.Name] = prop.Value;
                    }
                }
                catch (Exception ex)
                {
                    // 处理 JSON 解析错误（可记录日志或抛出异常）
                    throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
                }
            }

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的通用字段配置，基于属性信息。
        /// </summary>
        /// <param name="amisAttr">AmisFieldAttribute 的实例。</param>
        /// <param name="fieldName">字段的名称（camelCase）。</param>
        /// <param name="displayName">字段的显示名称。</param>
        /// <returns>AMIS 表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormFieldFromAttribute(AmisFieldAttribute amisAttr, string fieldName, string displayName)
        {
            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = amisAttr.Label ?? displayName,
                ["required"] = amisAttr.Required,
                ["type"] = amisAttr.Type,
                ["placeholder"] = amisAttr.Placeholder
            };

            // 处理额外的自定义配置
            if (!string.IsNullOrEmpty(amisAttr.AdditionalConfig))
            {
                try
                {
                    var additionalConfig = JObject.Parse(amisAttr.AdditionalConfig);
                    foreach (var prop in additionalConfig.Properties())
                    {
                        field[prop.Name] = prop.Value;
                    }
                }
                catch (Exception ex)
                {
                    // 处理 JSON 解析错误（可记录日志或抛出异常）
                    throw new ArgumentException($"Invalid AdditionalConfig JSON: {ex.Message}");
                }
            }

            return field;
        }

        /// <summary>
        /// 判断是否需要忽略该参数。
        /// </summary>
        /// <param name="param">参数信息。</param>
        /// <returns>如果需要忽略则返回 true，否则返回 false。</returns>
        private bool IsIgnoredParameter(ParameterInfo param)
        {
            // 实现您的忽略逻辑，例如根据名称、类型或自定义特性
            // 示例：忽略名为 "id" 的参数
            return string.Equals(param.Name, "id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否需要忽略该属性。
        /// </summary>
        /// <param name="prop">属性信息。</param>
        /// <returns>如果需要忽略则返回 true，否则返回 false。</returns>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            // 实现您的忽略逻辑，例如根据名称、类型或自定义特性
            // 示例：忽略名为 "CreatedDate" 的属性
            return string.Equals(prop.Name, "CreatedDate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
