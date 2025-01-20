using System.ComponentModel;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    /// <summary>
    /// 帮助类，用于根据方法参数动态生成 AMIS 搜索字段。
    /// </summary>
    public class SearchFieldHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;

        /// <summary>
        /// 定义排除在搜索参数之外的查询参数集合，忽略大小写。
        /// </summary>
        private static readonly HashSet<string> ExcludedQueryParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "page", "pageSize", "limit", "offset", "perPage", "sort", "order", "orderBy", "orderDir", "sortBy", "sortOrder"
        };

        /// <summary>
        /// 构造函数，注入权限服务和工具辅助类。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">工具辅助类，提供辅助方法。</param>
        public SearchFieldHelper(IPermissionService permissionService, UtilityHelper utilityHelper)
        {
            _permissionService = (PermissionService)permissionService;
            _utilityHelper = utilityHelper;
        }

        /// <summary>
        /// 根据给定的方法信息生成 AMIS 搜索字段列表。
        /// </summary>
        /// <param name="readMethod">需要解析的读取方法信息。</param>
        /// <returns>包含搜索字段的 JSON 对象列表。</returns>
        public List<JObject> GetAmisSearchFields(MethodInfo readMethod)
        {
            if (readMethod == null)
                return new List<JObject>();

            var parameters = readMethod.GetParameters();
            var searchFields = new List<JObject>();

            // 遍历所有带有 [FromQuery] 特性的参数
            foreach (var param in parameters.Where(p => p.GetCustomAttribute<FromQueryAttribute>() != null))
            {
                // 跳过被排除的参数
                if (IsExcludedParameter(param.Name))
                    continue;

                // 检查当前参数是否有搜索权限
                if (!HasSearchPermission(param))
                    continue;

                // 根据参数生成对应的搜索字段
                searchFields.AddRange(CreateSearchFieldsFromParameter(param));
            }

            return searchFields;
        }

        /// <summary>
        /// 检查参数名称是否在排除的查询参数集合中。
        /// </summary>
        /// <param name="paramName">参数名称。</param>
        /// <returns>如果参数被排除则返回 true，否则返回 false。</returns>
        private bool IsExcludedParameter(string paramName)
        {
            return ExcludedQueryParameters.Contains(paramName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查参数是否具有搜索权限。
        /// </summary>
        /// <param name="param">参数信息。</param>
        /// <returns>如果有权限或未定义权限属性则返回 true，否则返回 false。</returns>
        private bool HasSearchPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 检查属性是否具有搜索权限。
        /// </summary>
        /// <param name="prop">属性信息。</param>
        /// <returns>如果有权限或未定义权限属性则返回 true，否则返回 false。</returns>
        private bool HasSearchPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 根据参数信息创建相应的搜索字段列表。
        /// </summary>
        /// <param name="param">参数信息。</param>
        /// <returns>包含搜索字段的 JSON 对象列表。</returns>
        private List<JObject> CreateSearchFieldsFromParameter(ParameterInfo param)
        {
            var fields = new List<JObject>();

            if (_utilityHelper.IsSimpleType(param.ParameterType))
            {
                // 简单类型直接创建一个搜索字段
                fields.Add(CreateSearchField(param));
            }
            else if (_utilityHelper.IsComplexType(param.ParameterType))
            {
                // 复杂类型则遍历其属性并为每个合适的属性创建搜索字段
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

        /// <summary>
        /// 根据参数信息创建单个搜索字段的 JSON 对象。
        /// </summary>
        /// <param name="param">参数信息。</param>
        /// <returns>包含搜索字段信息的 JSON 对象。</returns>
        private JObject CreateSearchField(ParameterInfo param)
        {
            // 获取显示名称，如果未定义则转换参数名为标题格式
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(param.Name);
            // 转换参数名为驼峰命名
            var fieldName = _utilityHelper.ToCamelCase(param.Name);
            // 确定字段类型
            var fieldType = DetermineSearchFieldType(param.ParameterType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            // 如果是下拉选择框并且是枚举类型，添加选项
            if (fieldType == "select" && (param.ParameterType.IsEnum || _utilityHelper.IsNullableEnum(param.ParameterType)))
            {
                field["options"] = _utilityHelper.GetEnumOptions(param.ParameterType);
            }

            // 如果是日期类型，设置日期格式
            if (fieldType == "date" || fieldType == "input-date-range")
            {
                field["format"] = "YYYY-MM-DD";
            }

            return field;
        }

        /// <summary>
        /// 根据属性信息和父参数名创建单个搜索字段的 JSON 对象。
        /// </summary>
        /// <param name="prop">属性信息。</param>
        /// <param name="parentName">父参数名称，用于构建嵌套字段名。</param>
        /// <returns>包含搜索字段信息的 JSON 对象。</returns>
        private JObject CreateSearchFieldFromProperty(PropertyInfo prop, string parentName)
        {
            // 获取显示名称，如果未定义则转换属性名为标题格式
            var label = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            // 构建嵌套字段名，例如 parent.property
            var fieldName = _utilityHelper.ToCamelCase($"{prop.Name}");
            // 确定字段类型
            var fieldType = DetermineSearchFieldType(prop.PropertyType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            if (fieldType == "switch")
            {
                field["trueValue"] = true;
                field["falseValue"] = false;
            }

            // 如果是下拉选择框并且是枚举类型，添加选项
            if (fieldType == "select" && (prop.PropertyType.IsEnum || _utilityHelper.IsNullableEnum(prop.PropertyType)))
            {
                field["options"] = _utilityHelper.GetEnumOptions(prop.PropertyType);
                field["clearable"] = true;
            }

            // 如果是日期类型，设置日期格式
            if (fieldType == "date" || fieldType == "input-date-range")
            {
                field["format"] = "YYYY-MM-DD";
            }
            return field;
        }

        /// <summary>
        /// 确定搜索字段的类型，根据参数或属性的类型映射到 AMIS 支持的类型。
        /// </summary>
        /// <param name="type">参数或属性的类型。</param>
        /// <returns>AMIS 支持的字段类型字符串。</returns>
        private string DetermineSearchFieldType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return "input-number";
            if (type == typeof(bool) || type == typeof(bool?))
                return "switch";
            if (type.IsEnum || _utilityHelper.IsNullableEnum(type))
                return "select";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "date";
            if (type == typeof(DateTime[]))
                return "input-date-range";

            return "input-text";
        }
    }
}