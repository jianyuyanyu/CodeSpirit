using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    /// <summary>
    /// 帮助类，用于根据方法参数动态生成 AMIS 搜索字段。
    /// </summary>
    public class SearchFieldHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;
        private readonly IEnumerable<IAmisFieldFactory> _fieldFactories;

        /// <summary>
        /// 定义排除在搜索参数之外的查询参数集合，忽略大小写。
        /// </summary>
        private static readonly HashSet<string> ExcludedQueryParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            "page", "pageSize", "limit", "offset", "perPage", "sort", "order", "orderBy", "orderDir", "sortBy", "sortOrder"
        };

        /// <summary>
        /// 构造函数，注入权限服务和工具辅助类。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">工具辅助类，提供辅助方法。</param>
        /// <param name="fieldFactories">AMIS 字段工厂集合。</param>
        public SearchFieldHelper(
            IHasPermissionService permissionService,
            UtilityHelper utilityHelper,
            IEnumerable<IAmisFieldFactory> fieldFactories)
        {
            _permissionService = permissionService;
            _utilityHelper = utilityHelper;
            _fieldFactories = fieldFactories?.ToList() ?? throw new ArgumentNullException(nameof(fieldFactories));
        }

        /// <summary>
        /// 根据给定的方法信息生成 AMIS 搜索字段列表。
        /// </summary>
        /// <param name="readMethod">需要解析的读取方法信息。</param>
        /// <returns>包含搜索字段的 JSON 对象列表。</returns>
        public List<JObject> GetAmisSearchFields(MethodInfo readMethod)
        {
            if (readMethod == null)
            {
                return [];
            }

            ParameterInfo[] parameters = readMethod.GetParameters();
            List<JObject> searchFields = [];

            // 遍历所有带有 [FromQuery] 特性的参数
            foreach (ParameterInfo param in parameters.Where(p => p.GetCustomAttribute<FromQueryAttribute>() != null))
            {
                // 跳过被排除的参数
                if (IsExcludedParameter(param.Name))
                {
                    continue;
                }

                // 检查当前参数是否有搜索权限
                if (!HasSearchPermission(param))
                {
                    continue;
                }

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
            PermissionAttribute permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Name);
        }

        /// <summary>
        /// 检查属性是否具有搜索权限。
        /// </summary>
        /// <param name="prop">属性信息。</param>
        /// <returns>如果有权限或未定义权限属性则返回 true，否则返回 false。</returns>
        private bool HasSearchPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Name);
        }

        /// <summary>
        /// 根据参数信息创建相应的搜索字段列表。
        /// </summary>
        /// <param name="param">参数信息。</param>
        /// <returns>包含搜索字段的 JSON 对象列表。</returns>
        private List<JObject> CreateSearchFieldsFromParameter(ParameterInfo param)
        {
            List<JObject> fields = [];

            if (_utilityHelper.IsSimpleType(param.ParameterType))
            {
                JObject factoryField = CreateFieldUsingFactories(param);
                if (factoryField != null)
                {
                    fields.Add(factoryField);
                }
                else
                {
                    fields.Add(CreateDefaultSearchField(param));
                }
            }
            else if (_utilityHelper.IsComplexType(param.ParameterType))
            {
                PropertyInfo[] properties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in properties)
                {
                    if (IsExcludedParameter(prop.Name))
                    {
                        continue;
                    }

                    if (!HasSearchPermission(prop))
                    {
                        continue;
                    }

                    JObject factoryField = CreateFieldUsingFactories(prop);
                    if (factoryField != null)
                    {
                        fields.Add(factoryField);
                    }
                    else
                    {
                        fields.Add(CreateDefaultSearchField(prop, param.Name));
                    }
                }
            }

            return fields;
        }

        private JObject CreateFieldUsingFactories(ICustomAttributeProvider member)
        {
            return _fieldFactories
                .Select(factory => factory.CreateField(member, _utilityHelper))
                .FirstOrDefault(field => field != null);
        }

        private JObject CreateDefaultSearchField(ICustomAttributeProvider member, string parentName = null)
        {
            string label = member.GetDisplayName();
            string fieldName = member.GetFieldName(parentName);
            string fieldType = DetermineSearchFieldType(member.GetMemberType());

            JObject field = new()
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType,
                ["clearable"] = true
            };

            if (fieldType == "switch")
            {
                field["trueValue"] = true;
                field["falseValue"] = false;
            }

            // 处理枚举类型
            if (fieldType == "select")
            {
                Type memberType = member.GetMemberType();
                if (memberType.IsEnum || _utilityHelper.IsNullableEnum(memberType))
                {
                    field["options"] = memberType.GetEnumOptions();
                }
            }

            // 处理日期类型
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
            {
                return "input-number";
            }

            if (type == typeof(bool) || type == typeof(bool?))
            {
                return "switch";
            }

            return type.IsEnum || _utilityHelper.IsNullableEnum(type)
                ? "select"
                : type == typeof(DateTime) || type == typeof(DateTime?)
                ? "date"
                : type == typeof(DateTime[]) ? "input-date-range" : "input-text";
        }
    }
}