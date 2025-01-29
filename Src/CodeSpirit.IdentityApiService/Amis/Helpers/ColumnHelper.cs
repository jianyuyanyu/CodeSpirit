using CodeSpirit.IdentityApi.Amis.Attributes;
using CodeSpirit.IdentityApi.Authorization;
using CodeSpirit.Shared.Services.Dtos;
using CodeSpirit.Shared;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis.Helpers
{
    /// <summary>
    /// 帮助类，用于生成 AMIS 表格的列配置。
    /// </summary>
    public class ColumnHelper
    {
        private readonly PermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;
        private readonly AmisContext amisContext;
        private readonly ButtonHelper buttonHelper;

        /// <summary>
        /// 初始化 <see cref="ColumnHelper"/> 的新实例。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">实用工具类，提供辅助方法。</param>
        public ColumnHelper(IPermissionService permissionService, UtilityHelper utilityHelper, AmisContext amisContext, ButtonHelper buttonHelper)
        {
            _permissionService = (PermissionService)permissionService;
            _utilityHelper = utilityHelper;
            this.amisContext = amisContext;
            this.buttonHelper = buttonHelper;
        }

        /// <summary>
        /// 生成 AMIS 表格的列配置列表。
        /// </summary>
        /// <param name="dataType">数据类型的 <see cref="Type"/> 对象。</param>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="apiRoutes">包含 CRUD 操作路由的元组。</param>
        /// <returns>AMIS 表格列的列表。</returns>
        public List<JObject> GetAmisColumns(Type dataType, string controllerName, (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute, string QuickSaveRoute) apiRoutes, CrudActions actions)
        {
            // 获取数据类型的所有公共实例属性
            var properties = _utilityHelper.GetOrderedProperties(dataType);

            // 过滤出不被忽略且用户有查看权限的属性，并生成对应的 AMIS 列配置
            var columns = properties
                .Where(p => !IsIgnoredProperty(p) && HasViewPermission(p))
                .Select(p => CreateAmisColumn(p))
                .ToList();

            // 创建操作列（如编辑、删除按钮）
            var operations = CreateOperationsColumn(controllerName, dataType, apiRoutes.UpdateRoute, apiRoutes.DeleteRoute, actions);
            if (operations != null)
            {
                columns.Add(operations);
            }

            if (columns.Count == 1 && columns[0] == operations)
            {
                throw new AppServiceException(-100, "请检查返回参数的定义是否为“ApiResponse<ListData<T>>>”!");
            }
            return columns;
        }

        public List<JObject> GetAmisColumns()
        {
            return GetAmisColumns(amisContext.ListDataType, amisContext.ControllerName, amisContext.ApiRoutes, amisContext.Actions);
        }

        /// <summary>
        /// 判断属性是否应被忽略（例如密码字段、Id 字段或被 IgnoreColumnAttribute 标记的字段）。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果应忽略则返回 true，否则返回 false。</returns>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            // 忽略特定名称的字段
            if (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)
                || prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 检查是否应用了 IgnoreColumnAttribute
            var ignoreAttr = prop.GetCustomAttribute<IgnoreColumnAttribute>();
            if (ignoreAttr != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断当前用户是否有权限查看该属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasViewPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 创建 AMIS 表格的单个列配置。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 表格列的 JSON 对象。</returns>
        private JObject CreateAmisColumn(PropertyInfo prop)
        {
            // 获取属性的显示名称，优先使用 DisplayNameAttribute
            var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? _utilityHelper.ToTitleCase(prop.Name);
            // 将属性名称转换为 camelCase 以符合 AMIS 的命名约定
            var fieldName = _utilityHelper.ToCamelCase(prop.Name);

            var column = new JObject
            {
                ["name"] = fieldName,
                ["label"] = displayName,
                ["sortable"] = true,
                ["type"] = GetColumnType(prop)
            };

            // 如果是主键，则隐藏该列
            if (IsPrimaryKey(prop))
            {
                column["hidden"] = true;
            }

            // 如果属性是枚举类型，设置映射
            if (_utilityHelper.IsEnumProperty(prop))
            {
                column["type"] = "mapping"; // 设置列类型为 mapping
                column["map"] = GetEnumMapping(prop.PropertyType); // 添加枚举映射
            }

            // 如果属性是图片或头像类型，设置为 AMIS 的 image 或 avatar 类型
            if (IsImageField(prop))
            {
                column["type"] = GetImageColumnType(prop);
                column["src"] = $"${{{fieldName}}}"; // 设置图片的来源字段
                column["altText"] = displayName;
                column["className"] = "image-column"; // 可选：添加自定义样式类
            }

            // 如果属性是 List 类型，生成 List 配置
            if (IsListProperty(prop))
            {
                column["type"] = "list";
                column["placeholder"] = "-"; // 可以根据需要修改 placeholder 内容
                column["listItem"] = CreateListItemConfiguration(prop);
            }
            return column;
        }

        /// <summary>
        /// 判断属性是否为 List 类型且列表项是类类型（即 List<T>，T 是类）。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果是 List 类型且列表项是类类型则返回 true，否则返回 false。</returns>
        private bool IsListProperty(PropertyInfo prop)
        {
            if (prop.PropertyType.IsGenericType)
            {
                var genericType = prop.PropertyType.GetGenericTypeDefinition();

                // 检查是否为 List<T> 类型
                if (genericType == typeof(List<>) || genericType == typeof(IEnumerable<>))
                {
                    var elementType = prop.PropertyType.GetGenericArguments()[0];

                    // 排除 string 类型，string 是类类型
                    if (elementType != typeof(string) && elementType.IsClass)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 创建 List 类型列的 listItem 配置。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>List 类型列的 listItem 配置。</returns>
        private JObject CreateListItemConfiguration(PropertyInfo prop)
        {
            var listItem = new JObject();

            // 获取 ListItemAttribute 特性，如果存在
            var listItemAttr = prop.GetCustomAttribute<ListColumnAttribute>();
            // 使用特性中的配置字段，若特性没有配置，则使用默认值
            listItem["title"] = $"${{{_utilityHelper.ToCamelCase(listItemAttr?.Title ?? "titile")}}}";
            listItem["subTitle"] = $"${{{_utilityHelper.ToCamelCase(listItemAttr?.SubTitle ?? "subTitile")}}}";
            listItem["placeholder"] = listItemAttr?.Placeholder ?? "-";
            return listItem;
        }

        private static readonly ConcurrentDictionary<Type, JObject> EnumMappingCache = new ConcurrentDictionary<Type, JObject>();

        private JObject GetEnumMapping(Type type)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            return EnumMappingCache.GetOrAdd(enumType, CreateEnumMapping);
        }

        /// <summary>
        /// 创建枚举映射。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <returns>AMIS 枚举映射的 JSON 对象。</returns>
        private JObject CreateEnumMapping(Type enumType)
        {
            var enumValues = Enum.GetValues(enumType).Cast<object>();
            var mapping = new JObject();

            foreach (var e in enumValues)
            {
                // 获取枚举的实际值（根据基础类型动态转换）
                var underlyingType = Enum.GetUnderlyingType(enumType);
                var value = Convert.ChangeType(e, underlyingType, CultureInfo.InvariantCulture).ToString();
                var label = _utilityHelper.GetEnumDisplayName(enumType, e);
                mapping[value] = label;
            }

            // 检查是否为可空枚举，添加 null 映射
            if (Nullable.GetUnderlyingType(enumType) != null)
            {
                mapping[""] = GetNullEnumDisplayName();
            }

            return mapping;
        }

        /// <summary>
        /// 获取可空枚举的 null 显示名称。
        /// </summary>
        /// <returns>可空枚举的 null 显示名称。</returns>
        private string GetNullEnumDisplayName()
        {
            // 可以扩展为从资源文件中获取显示名称以支持国际化
            return "";
        }

        /// <summary>
        /// 获取 AMIS 列的类型，根据属性的类型进行映射。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 列的类型字符串。</returns>
        private string GetColumnType(PropertyInfo prop)
        {
            if (_utilityHelper.IsEnumProperty(prop))
            {
                return "mapping";
            }

            return prop.PropertyType switch
            {
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) || t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?) => "datetime",
                _ => "text"
            };
        }

        

        /// <summary>
        /// 判断属性是否为主键（假设名称为 "Id"）。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果是主键则返回 true，否则返回 false。</returns>
        private bool IsPrimaryKey(PropertyInfo prop)
        {
            return prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断属性是否为图片或头像字段。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果是图片或头像字段则返回 true，否则返回 false。</returns>
        private bool IsImageField(PropertyInfo prop)
        {
            // 判断属性是否标注了 [DataType(DataType.ImageUrl)]
            var dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null && dataTypeAttr.DataType == DataType.ImageUrl)
            {
                return true;
            }

            // 另外，可以根据属性名称包含 "Image" 或 "Avatar" 来判断
            if (prop.Name.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取 AMIS 图片或头像列的类型，根据属性名称或其他逻辑决定是 'image' 还是 'avatar'。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 列的类型字符串。</returns>
        private string GetImageColumnType(PropertyInfo prop)
        {
            if (prop.Name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "avatar";
            }

            return "image";
        }

        /// <summary>
        /// 创建 AMIS 表格的操作列，包括编辑和删除按钮。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="dataType">数据类型，用于生成表单字段。</param>
        /// <param name="updateRoute">更新操作的 API 路由。</param>
        /// <param name="deleteRoute">删除操作的 API 路由。</param>
        /// <returns>AMIS 操作列的 JSON 对象，如果没有按钮则返回 null。</returns>
        private JObject CreateOperationsColumn(string controllerName, Type dataType, string updateRoute, string deleteRoute, CrudActions actions)
        {
            var buttons = new JArray();
            // 如果用户有编辑权限，则添加编辑按钮
            if (_permissionService.HasPermission($"{controllerName}Edit"))
            {
                var editButton = buttonHelper.CreateEditButton(updateRoute, actions.Update?.GetParameters());
                buttons.Add(editButton);
            }

            // 如果用户有删除权限，则添加删除按钮
            if (_permissionService.HasPermission($"{controllerName}Delete"))
            {
                var deleteButton = buttonHelper.CreateDeleteButton(deleteRoute);
                buttons.Add(deleteButton);
            }

            // 添加自定义操作按钮
            var customButtons = buttonHelper.GetCustomOperationsButtons();
            foreach (var btn in customButtons)
            {
                buttons.Add(btn);
            }

            // 如果没有任何按钮，则不添加操作列
            if (buttons.Count == 0)
                return null;

            return new JObject
            {
                ["name"] = "operation",
                ["label"] = "操作",
                ["type"] = "operation",
                ["buttons"] = buttons
            };
        }
    }
}
