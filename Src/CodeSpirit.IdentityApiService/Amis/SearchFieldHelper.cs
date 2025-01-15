using System.ComponentModel;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    public class SearchFieldHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;

        // ExcludedQueryParameters moved here
        private static readonly HashSet<string> ExcludedQueryParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "page", "pageSize", "limit", "offset", "perPage", "sort", "order", "orderBy", "orderDir", "sortBy", "sortOrder"
        };

        public SearchFieldHelper(PermissionService permissionService, UtilityHelper utilityHelper)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
        }

        public List<JObject> GetAmisSearchFields(MethodInfo readMethod)
        {
            if (readMethod == null)
                return new List<JObject>();

            var parameters = readMethod.GetParameters();
            var searchFields = new List<JObject>();

            foreach (var param in parameters.Where(p => p.GetCustomAttribute<FromQueryAttribute>() != null))
            {
                if (IsExcludedParameter(param.Name))
                    continue;

                if (!HasSearchPermission(param))
                    continue;

                searchFields.AddRange(CreateSearchFieldsFromParameter(param));
            }

            return searchFields;
        }

        // Moved IsExcludedParameter here
        private bool IsExcludedParameter(string paramName)
        {
            return ExcludedQueryParameters.Contains(paramName, StringComparer.OrdinalIgnoreCase);
        }

        private bool HasSearchPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        private bool HasSearchPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        private List<JObject> CreateSearchFieldsFromParameter(ParameterInfo param)
        {
            var fields = new List<JObject>();

            if (IsSimpleType(param.ParameterType))
            {
                fields.Add(CreateSearchField(param));
            }
            else if (IsComplexType(param.ParameterType))
            {
                var properties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (IsExcludedParameter(prop.Name))
                        continue;

                    if (!HasSearchPermission(prop))
                        continue;

                    fields.Add(CreateSearchFieldFromProperty(prop, param.Name));
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

        private JObject CreateSearchField(ParameterInfo param)
        {
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(param.Name);
            var fieldName = _utilityHelper.ToCamelCase(param.Name);
            var fieldType = DetermineSearchFieldType(param.ParameterType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            if (fieldType == "select" && (param.ParameterType.IsEnum || IsNullableEnum(param.ParameterType)))
            {
                field["options"] = GetEnumOptions(param.ParameterType);
            }

            if (fieldType == "date")
            {
                field["format"] = "YYYY-MM-DD";
            }

            return field;
        }

        private JObject CreateSearchFieldFromProperty(PropertyInfo prop, string parentName)
        {
            var label = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            var fieldName = _utilityHelper.ToCamelCase($"{parentName}.{prop.Name}");
            var fieldType = DetermineSearchFieldType(prop.PropertyType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            if (fieldType == "select" && (prop.PropertyType.IsEnum || IsNullableEnum(prop.PropertyType)))
            {
                field["options"] = GetEnumOptions(prop.PropertyType);
            }

            if (fieldType == "date")
            {
                field["format"] = "YYYY-MM-DD";
            }

            return field;
        }

        private string DetermineSearchFieldType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return "input-number";
            if (type == typeof(bool) || type == typeof(bool?))
                return "switch";
            if (type.IsEnum || IsNullableEnum(type))
                return "select";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "date";

            return "input-text";
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

