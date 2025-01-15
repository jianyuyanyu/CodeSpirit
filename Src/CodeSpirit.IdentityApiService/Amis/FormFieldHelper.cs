using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class FormFieldHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;

        public FormFieldHelper(PermissionService permissionService, UtilityHelper utilityHelper)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
        }

        public List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            var fields = new List<JObject>();

            if (parameters == null)
                return fields;

            foreach (var param in parameters)
            {
                if (!HasEditPermission(param))
                    continue;

                if (IsSimpleType(param.ParameterType))
                {
                    if (!IsIgnoredParameter(param))
                    {
                        fields.Add(CreateAmisFormField(param));
                    }
                }
                else if (IsComplexType(param.ParameterType))
                {
                    var nestedProperties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in nestedProperties)
                    {
                        if (!IsIgnoredProperty(prop) && HasEditPermission(prop))
                        {
                            fields.Add(CreateAmisFormFieldFromProperty(prop, param.Name));
                        }
                    }
                }
            }

            return fields;
        }

        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || new Type[]
                {
                    typeof(string), typeof(decimal), typeof(DateTime),
                    typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
                }.Contains(type)
                || type.IsEnum
                || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)));
        }

        private bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            return prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIgnoredParameter(ParameterInfo param)
        {
            return param.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasEditPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        private bool HasEditPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        private JObject CreateAmisFormField(ParameterInfo param)
        {
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(param.Name);
            var fieldName = _utilityHelper.ToCamelCase(param.Name);
            var isRequired = !IsNullable(param.ParameterType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(param.ParameterType)
            };

            AddValidationRulesFromParameter(param, field);

            return field;
        }

        private JObject CreateAmisFormFieldFromProperty(PropertyInfo prop, string parentName)
        {
            var label = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            var fieldName = _utilityHelper.ToCamelCase($"{prop.Name}");
            var isRequired = !IsNullable(prop);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(prop)
            };

            AddValidationRules(prop, field);

            return field;
        }

        private string GetFormFieldType(PropertyInfo prop)
        {
            return prop.PropertyType switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                Type t when t.IsEnum => "select",
                _ => "input-text"
            };
        }

        private string GetFormFieldType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                Type t when t.IsEnum => "select",
                _ => "input-text"
            };
        }

        private bool IsNullable(PropertyInfo prop)
        {
            if (!prop.PropertyType.IsValueType)
                return true;

            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                return true;

            return false;
        }

        private bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true;

            if (Nullable.GetUnderlyingType(type) != null)
                return true;

            return false;
        }

        private void AddValidationRules(PropertyInfo prop, JObject field)
        {
            var validationRules = new JObject();

            var stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            if (prop.PropertyType.IsEnum || IsNullableEnum(prop.PropertyType))
            {
                field["options"] = GetEnumOptions(prop.PropertyType);
            }
        }

        private void AddValidationRulesFromParameter(ParameterInfo param, JObject field)
        {
            var validationRules = new JObject();

            if (param.GetCustomAttribute<RequiredAttribute>() != null)
            {
                field["required"] = true;
                validationRules["required"] = true;
            }

            var stringLengthAttr = param.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            var rangeAttr = param.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            if (param.ParameterType.IsEnum || IsNullableEnum(param.ParameterType))
            {
                field["options"] = GetEnumOptions(param.ParameterType);
            }
        }

        private bool IsNullableEnum(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            return underlying != null && underlying.IsEnum;
        }

        private JArray GetEnumOptions(Type type)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            var enumOptions = Enum.GetValues(enumType).Cast<object>().Select(e => new JObject
            {
                ["label"] = e.ToString(),
                ["value"] = e.ToString()
            });

            return new JArray(enumOptions);
        }
    }
}

