// 文件路径: CodeSpirit.Amis.Helpers/FormFieldHelper.cs

using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Authorization;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CodeSpirit.Amis.Form
{
    /// <summary>
    /// 帮助类，用于生成 AMIS 表单的字段配置。
    /// </summary>
    public class FormFieldHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;
        private readonly IEnumerable<IAmisFieldFactory> _fieldFactories;

        /// <summary>
        /// 初始化 <see cref="FormFieldHelper"/> 的新实例。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">实用工具类，提供辅助方法。</param>
        /// <param name="fieldFactories">AMIS 字段工厂集合。</param>
        public FormFieldHelper(IPermissionService permissionService, UtilityHelper utilityHelper, IEnumerable<IAmisFieldFactory> fieldFactories)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
            _fieldFactories = fieldFactories;
        }

        /// <summary>
        /// 根据方法参数生成 AMIS 表单字段的配置列表。
        /// </summary>
        /// <param name="parameters">控制器方法的参数列表。</param>
        /// <returns>AMIS 表单字段的 JSON 对象列表。</returns>
        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            List<JObject> fields = [];

            if (parameters == null)
                return fields;

            foreach (ParameterInfo param in parameters)
            {
                if (!CanProcessMember(param))
                    continue;

                // 尝试使用工厂创建字段
                JObject field = CreateFieldUsingFactories(param);
                if (field != null)
                {
                    fields.Add(field);
                    continue;
                }

                if (_utilityHelper.IsSimpleType(param.ParameterType) && !IsIgnoredParameter(param))
                {
                    fields.Add(CreateAmisFormField(param));
                }
                else if (_utilityHelper.IsComplexType(param.ParameterType))
                {
                    fields.AddRange(ProcessComplexType(param));
                }
            }

            return fields;
        }

        /// <summary>
        /// 检查成员（参数或属性）是否可以处理（权限和忽略检查）。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <returns>如果可以处理则返回 true，否则返回 false。</returns>
        private bool CanProcessMember(ICustomAttributeProvider member)
        {
            return member switch
            {
                ParameterInfo param => HasEditPermission(param) && !IsIgnoredParameter(param),
                PropertyInfo prop => HasEditPermission(prop) && !IsIgnoredProperty(prop),
                _ => false
            };
        }

        /// <summary>
        /// 尝试使用工厂创建字段配置。
        /// </summary>
        /// <param name="member">参数或属性的信息。</param>
        /// <returns>如果成功创建则返回字段配置，否则返回 null。</returns>
        private JObject CreateFieldUsingFactories(ICustomAttributeProvider member)
        {
            foreach (IAmisFieldFactory factory in _fieldFactories)
            {
                JObject field = factory.CreateField(member, _utilityHelper);
                if (field != null)
                    return field;
            }
            return null;
        }

        /// <summary>
        /// 处理复杂类型的参数，生成嵌套字段配置。
        /// </summary>
        private IEnumerable<JObject> ProcessComplexType(ParameterInfo param)
        {
            List<JObject> fields = [];
            PropertyInfo[] nestedProperties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in nestedProperties)
            {
                if (!CanProcessMember(prop))
                    continue;

                JObject field = CreateFieldUsingFactories(prop);
                if (field != null)
                {
                    // 添加父级名称
                    AddParentName(field, null);
                    fields.Add(field);
                    continue;
                }

                fields.Add(CreateAmisFormFieldFromProperty(prop, null));
            }

            return fields;
        }

        /// <summary>
        /// 在字段名称中添加父级名称（用于嵌套对象）。
        /// </summary>
        private void AddParentName(JObject field, string parentName)
        {
            if (field["name"] != null && parentName != null)
            {
                string originalName = field["name"].ToString();
                field["name"] = _utilityHelper.ToCamelCase($"{parentName}.{originalName}");
            }
        }


        /// <summary>
        /// 判断类型是否为复杂类型（如类类型，不包括字符串）。
        /// </summary>
        /// <param name="type">参数或属性的类型。</param>
        /// <returns>如果是复杂类型则返回 true，否则返回 false。</returns>
        public bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        /// <summary>
        /// 创建 AMIS 表单的通用字段配置，基于属性信息。
        /// </summary>
        private JObject CreateAmisFormFieldFromProperty(PropertyInfo prop, string parentName)
        {
            string displayName = _utilityHelper.GetDisplayName(prop);
            string fieldName = _utilityHelper.GetFieldName(prop, parentName);
            bool isRequired = !_utilityHelper.IsNullable(prop.PropertyType) || prop.GetCustomAttribute<RequiredAttribute>() != null;

            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = displayName,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(prop.PropertyType)
            };

            AddValidationRules(prop, field);

            return field;
        }

        /// <summary>
        /// 创建 AMIS 表单的单个字段配置（通用）。
        /// </summary>
        private JObject CreateAmisFormField(ParameterInfo param)
        {
            string label = _utilityHelper.GetDisplayName(param);
            string fieldName = _utilityHelper.GetFieldName(param, parentName: null);
            bool isRequired = !_utilityHelper.IsNullable(param.ParameterType) || param.GetCustomAttribute<RequiredAttribute>() != null;

            JObject field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(param.ParameterType)
            };

            AddValidationRulesFromParameter(param, field);

            return field;
        }

        /// <summary>
        /// 根据类型确定 AMIS 表单字段的类型。
        /// </summary>
        private string GetFormFieldType(Type type)
        {
            // 处理枚举类型
            if (type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum == true)
                return "select";

            return type switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) ||
                           t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?) => "datetime",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.Password => "input-password",
                Type t when t.GetCustomAttribute<DataTypeAttribute>()?.DataType == DataType.ImageUrl => "image",
                _ => "input-text"
            };
        }

        /// <summary>
        /// 添加属性级别的验证规则到字段配置中。
        /// </summary>
        private void AddValidationRules(PropertyInfo prop, JObject field)
        {
            JObject validationRules = [];
            JObject validationErrors = [];

            // 处理 [Required] 特性
            RequiredAttribute requiredAttribute = prop.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttribute != null)
            {
                field["required"] = true;
            }

            DescriptionAttribute descriptionAttribute = prop.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                field["description"] = descriptionAttribute.Description;
            }

            // 处理 [StringLength] 特性
            StringLengthAttribute stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;

                if (!string.IsNullOrEmpty(stringLengthAttr.ErrorMessage))
                {
                    validationErrors["minLength"] = stringLengthAttr.ErrorMessage;
                    validationErrors["maxLength"] = stringLengthAttr.ErrorMessage;
                }
            }

            // 处理 [Range] 特性
            RangeAttribute rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);

                if (!string.IsNullOrEmpty(rangeAttr.ErrorMessage))
                {
                    validationErrors["minimum"] = rangeAttr.ErrorMessage;
                    validationErrors["maximum"] = rangeAttr.ErrorMessage;
                }
            }

            // 处理 [DataType] 特性
            DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null)
            {
                AddDataTypeValidation(dataTypeAttr, validationRules, field);
            }

            // 添加正则匹配规则
            RegularExpressionAttribute regexAttr = prop.GetCustomAttribute<RegularExpressionAttribute>();
            if (regexAttr != null)
            {
                validationRules["matchRegexp"] = regexAttr.Pattern;
                if (!string.IsNullOrEmpty(regexAttr.ErrorMessage))
                {
                    validationErrors["matchRegexp"] = regexAttr.ErrorMessage;
                }
            }

            // 添加验证规则
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            if (validationErrors.HasValues)
            {
                field["validationErrors"] = validationErrors;
            }

            // 处理枚举类型
            if (prop.PropertyType.IsEnum || Nullable.GetUnderlyingType(prop.PropertyType)?.IsEnum == true)
            {
                field["options"] = _utilityHelper.GetEnumOptions(prop.PropertyType);
            }

            // 处理日期类型
            if (IsDateType(prop.PropertyType))
            {
                field["format"] = "YYYY-MM-DD";
            }

            // 处理图片类型
            HandleImageType(dataTypeAttr, field, prop.Name);
        }

        /// <summary>
        /// 添加参数级别的验证规则到字段配置中。
        /// </summary>
        private void AddValidationRulesFromParameter(ParameterInfo param, JObject field)
        {
            JObject validationRules = [];
            JObject validationErrors = [];

            // 处理 [Required] 特性
            RequiredAttribute requiredAttribute = param.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttribute != null)
            {
                field["required"] = true;
            }

            DescriptionAttribute descriptionAttribute = param.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
            {
                field["description"] = descriptionAttribute.Description;
            }

            // 处理 [StringLength] 特性
            StringLengthAttribute stringLengthAttr = param.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;

                if (!string.IsNullOrEmpty(stringLengthAttr.ErrorMessage))
                {
                    validationErrors["minLength"] = stringLengthAttr.ErrorMessage;
                    validationErrors["maxLength"] = stringLengthAttr.ErrorMessage;
                }
            }

            // 处理 [Range] 特性
            RangeAttribute rangeAttr = param.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);

                if (!string.IsNullOrEmpty(rangeAttr.ErrorMessage))
                {
                    validationErrors["minimum"] = rangeAttr.ErrorMessage;
                    validationErrors["maximum"] = rangeAttr.ErrorMessage;
                }
            }

            // 处理 [DataType] 特性
            DataTypeAttribute dataTypeAttr = param.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null)
            {
                AddDataTypeValidation(dataTypeAttr, validationRules, field);
            }

            // 添加正则匹配规则
            RegularExpressionAttribute regexAttr = param.GetCustomAttribute<RegularExpressionAttribute>();
            if (regexAttr != null)
            {
                validationRules["matchRegexp"] = regexAttr.Pattern;
                if (!string.IsNullOrEmpty(regexAttr.ErrorMessage))
                {
                    validationErrors["matchRegexp"] = regexAttr.ErrorMessage;
                }
            }

            // 添加验证规则
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            if (validationErrors.HasValues)
            {
                field["validationErrors"] = validationErrors;
            }

            // 处理枚举类型
            if (param.ParameterType.IsEnum || Nullable.GetUnderlyingType(param.ParameterType)?.IsEnum == true)
            {
                field["options"] = _utilityHelper.GetEnumOptions(param.ParameterType);
            }

            // 处理日期类型
            if (IsDateType(param.ParameterType))
            {
                field["format"] = "YYYY-MM-DD";
            }

            // 处理图片类型
            HandleImageType(dataTypeAttr, field, param.Name);
        }

        /// <summary>
        /// 根据 DataTypeAttribute 添加验证规则和特定配置。
        /// </summary>
        private void AddDataTypeValidation(DataTypeAttribute dataTypeAttr, JObject validationRules, JObject field)
        {
            switch (dataTypeAttr.DataType)
            {
                case DataType.EmailAddress:
                    validationRules["isEmail"] = true;
                    break;
                case DataType.Url:
                    validationRules["isUrl"] = true;
                    break;
                case DataType.PhoneNumber:
                    validationRules["isPhoneNumber"] = true;
                    break;
                case DataType.PostalCode:
                    validationRules["isZipcode"] = true;
                    break;
                case DataType.Custom:
                case DataType.DateTime:
                case DataType.Date:
                case DataType.Time:
                case DataType.Duration:
                case DataType.Currency:
                case DataType.Text:
                case DataType.Html:
                case DataType.MultilineText:
                case DataType.Password:
                    break;
                case DataType.ImageUrl:
                    validationRules["isUrl"] = true;
                    break;
                case DataType.CreditCard:
                case DataType.Upload:
                    break;
            }
        }

        /// <summary>
        /// 处理图片类型的特定字段配置。
        /// </summary>
        private void HandleImageType(DataTypeAttribute dataTypeAttr, JObject field, string memberName)
        {
            if (dataTypeAttr?.DataType == DataType.ImageUrl)
            {
                field["type"] = "image";
                field["src"] = $"${{{field["name"]}}}"; // 设置图片来源字段
                field["altText"] = field["label"];
                field["className"] = "image-field"; // 可选：添加自定义样式类
            }

            if (memberName.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                field["type"] = "avatar";
                field["src"] = $"${{{field["name"]}}}"; // 设置头像来源字段
                field["altText"] = field["label"];
                // field["className"] = "avatar-field"; // 可选：添加自定义样式类
            }
        }

        /// <summary>
        /// 判断类型是否为日期类型。
        /// </summary>
        private bool IsDateType(Type type)
        {
            return type == typeof(DateTime) || type == typeof(DateTime?) ||
                   type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑该参数。
        /// </summary>
        private bool HasEditPermission(ParameterInfo param)
        {
            PermissionAttribute permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑该属性。
        /// </summary>
        private bool HasEditPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 判断是否需要忽略该参数。
        /// </summary>
        private bool IsIgnoredParameter(ParameterInfo param)
        {
            // 示例：忽略名为 "id" 的参数
            return string.Equals(param.Name, "id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断是否需要忽略该属性。
        /// </summary>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            // 示例：忽略名为 "CreatedDate" 的属性
            return string.Equals(prop.Name, "CreatedDate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
