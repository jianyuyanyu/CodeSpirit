using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace CodeSpirit.Amis.Column
{
    /// <summary>
    /// 帮助类，用于生成 AMIS 表格的列配置。
    /// </summary>
    public class ColumnHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly UtilityHelper _utilityHelper;
        private readonly AmisContext amisContext;
        private readonly ButtonHelper buttonHelper;

        /// <summary>
        /// 初始化 <see cref="ColumnHelper"/> 的新实例。
        /// </summary>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="utilityHelper">实用工具类，提供辅助方法。</param>
        public ColumnHelper(IHasPermissionService permissionService, UtilityHelper utilityHelper, AmisContext amisContext, ButtonHelper buttonHelper)
        {
            _permissionService = permissionService;
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
        public List<JObject> GetAmisColumns(Type dataType, string controllerName, ApiRoutesInfo apiRoutes, CrudActions actions)
        {
            // 获取数据类型的所有公共实例属性
            List<PropertyInfo> properties = _utilityHelper.GetOrderedProperties(dataType);

            // 过滤出不被忽略且用户有查看权限的属性，并生成对应的 AMIS 列配置
            List<JObject> columns = properties
                .Where(p => !IsIgnoredProperty(p) && HasViewPermission(p))
                .Select(p => CreateAmisColumn(p))
                .ToList();

            // 创建操作列（如编辑、删除按钮）
            JObject operations = CreateOperationsColumn(controllerName, dataType, apiRoutes, actions);
            if (operations != null)
            {
                columns.Add(operations);
            }

            return columns.Count == 1 && columns[0] == operations
                ? throw new AppServiceException(-100, "请检查返回参数的定义是否为ApiResponse<ListData<T>>>!")
                : columns;
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
            IgnoreColumnAttribute ignoreAttr = prop.GetCustomAttribute<IgnoreColumnAttribute>();
            return ignoreAttr != null;
        }

        /// <summary>
        /// 判断当前用户是否有权限查看该属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasViewPermission(PropertyInfo prop)
        {
            PermissionAttribute permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Code);
        }

        /// <summary>
        /// 创建 AMIS 表格的单个列配置。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 表格列的 JSON 对象。</returns>
        private JObject CreateAmisColumn(PropertyInfo prop)
        {
            // 获取属性的显示名称，优先使用 DisplayNameAttribute
            string displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name.ToTitleCase();
            // 将属性名称转换为 camelCase 以符合 AMIS 的命名约定
            string fieldName = prop.Name.ToCamelCase();

            JObject column = new()
            {
                ["name"] = fieldName,
                ["label"] = displayName,
                ["sortable"] = true,
                ["type"] = GetColumnType(prop)
            };

            AmisColumnAttribute columnAttr = (AmisColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(AmisColumnAttribute));
            if (columnAttr != null)
            {
                if (!string.IsNullOrEmpty(columnAttr.Name))
                {
                    column["name"] = columnAttr.Name;
                }

                if (!string.IsNullOrEmpty(columnAttr.Label))
                {
                    column["label"] = columnAttr.Label;
                }

                column["sortable"] = columnAttr.Sortable;

                if (!string.IsNullOrEmpty(columnAttr.Type))
                {
                    column["type"] = columnAttr.Type;
                }

                column["quickEdit"] = columnAttr.QuickEdit;

                if (!string.IsNullOrEmpty(columnAttr.Remark))
                {
                    column["remark"] = columnAttr.Remark;
                }

                column["copyable"] = columnAttr.Copyable;

                if (!string.IsNullOrEmpty(columnAttr.Fixed))
                {
                    column["fixed"] = columnAttr.Fixed;
                }

                column["hidden"] = columnAttr.Hidden;

                if (!columnAttr.Toggled)
                {
                    column["toggled"] = columnAttr.Toggled;
                }

                // 添加背景色阶配置
                if (columnAttr.BackgroundScaleColors?.Length >= 2)
                {
                    column["backgroundScale"] = new JObject
                    {
                        ["min"] = columnAttr.BackgroundScaleMin,
                        ["max"] = columnAttr.BackgroundScaleMax,
                        ["colors"] = new JArray(columnAttr.BackgroundScaleColors)
                    };
                }
            }

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
                string columnType = GetImageColumnType(prop);
                column["type"] = columnType;

                // 处理 Avatar 特定配置
                if (columnType == "avatar")
                {
                    AvatarColumnAttribute avatarAttr = prop.GetCustomAttribute<AvatarColumnAttribute>();
                    if (avatarAttr != null)
                    {
                        // Avatar 基本配置
                        if (!string.IsNullOrEmpty(avatarAttr.Text))
                        {
                            column["text"] = avatarAttr.Text;
                        }
                        if (!string.IsNullOrEmpty(avatarAttr.Icon))
                        {
                            column["icon"] = avatarAttr.Icon;
                        }
                        if (!string.IsNullOrEmpty(avatarAttr.OnError))
                        {
                            column["onError"] = avatarAttr.OnError;
                        }
                        if (!string.IsNullOrEmpty(avatarAttr.Fit))
                        {
                            column["fit"] = avatarAttr.Fit;
                        }
                        if (!string.IsNullOrEmpty(avatarAttr.Shape))
                        {
                            column["shape"] = avatarAttr.Shape;
                        }
                        if (avatarAttr.Size != null)
                        {
                            column["size"] = avatarAttr.Size;
                        }
                        if (avatarAttr.Gap.HasValue)
                        {
                            column["gap"] = avatarAttr.Gap.Value;
                        }
                    }
                    else
                    {
                        column["src"] = $"${{{fieldName}}}"; // 设置图片的来源字段
                        column["altText"] = displayName;
                    }


                }
            }

            // Badge 配置
            BadgeAttribute badgeAttr = prop.GetCustomAttribute<BadgeAttribute>();
            if (badgeAttr != null)
            {
                JObject badge = [];

                if (!string.IsNullOrEmpty(badgeAttr.Mode))
                {
                    badge["mode"] = badgeAttr.Mode;
                }
                if (!string.IsNullOrEmpty(badgeAttr.Text))
                {
                    badge["text"] = badgeAttr.Text;
                }
                if (!string.IsNullOrEmpty(badgeAttr.ClassName))
                {
                    badge["className"] = badgeAttr.ClassName;
                }
                if (badgeAttr.Size != default)
                {
                    badge["size"] = badgeAttr.Size;
                }
                if (!string.IsNullOrEmpty(badgeAttr.Level))
                {
                    badge["level"] = badgeAttr.Level;
                }
                if (badgeAttr.OverflowCount != default)
                {
                    badge["overflowCount"] = badgeAttr.OverflowCount;
                }
                if (!string.IsNullOrEmpty(badgeAttr.Position))
                {
                    badge["position"] = badgeAttr.Position;
                }
                if (badgeAttr.OffsetX != default && badgeAttr.OffsetY != default)
                {
                    badge["offset"] = new JArray(badgeAttr.OffsetX, badgeAttr.OffsetY);
                }
                if (badgeAttr.Animation)
                {
                    badge["animation"] = badgeAttr.Animation;
                }
                if (!string.IsNullOrEmpty(badgeAttr.VisibleOn))
                {
                    badge["visibleOn"] = badgeAttr.VisibleOn;
                }

                column["badge"] = badge;
            }
            // 如果属性是 List 类型，生成 List 配置
            if (IsListProperty(prop))
            {
                column["type"] = "list";
                column["placeholder"] = "-"; // 可以根据需要修改 placeholder 内容
                column["listItem"] = CreateListItemConfiguration(prop);
            }

            // 处理 Tpl 列
            TplColumnAttribute tplAttr = prop.GetCustomAttribute<TplColumnAttribute>();
            if (tplAttr != null)
            {
                column["type"] = "tpl";
                column["tpl"] = tplAttr.Template;
            }

            // 处理 Link 列
            LinkColumnAttribute linkAttr = prop.GetCustomAttribute<LinkColumnAttribute>();
            if (linkAttr != null)
            {
                column["type"] = "link";
                if (!string.IsNullOrEmpty(linkAttr.Href))
                {
                    column["href"] = linkAttr.Href;
                }
                if (!string.IsNullOrEmpty(linkAttr.Blank))
                {
                    column["blank"] = linkAttr.Blank;
                }
                if (!string.IsNullOrEmpty(linkAttr.Icon))
                {
                    column["icon"] = linkAttr.Icon;
                }
                if (!string.IsNullOrEmpty(linkAttr.Label))
                {
                    column["label"] = linkAttr.Label;
                }
            }

            // 处理 Date 列
            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?) ||
                prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTimeOffset?))
            {
                DateColumnAttribute dateAttr = prop.GetCustomAttribute<DateColumnAttribute>();
                if (dateAttr != null)
                {
                    column["type"] = "date";
                    if (!string.IsNullOrEmpty(dateAttr.Format))
                    {
                        column["format"] = dateAttr.Format;
                    }
                    if (!string.IsNullOrEmpty(dateAttr.InputFormat))
                    {
                        column["inputFormat"] = dateAttr.InputFormat;
                    }
                    if (!string.IsNullOrEmpty(dateAttr.Placeholder))
                    {
                        column["placeholder"] = dateAttr.Placeholder;
                    }
                    if (dateAttr.FromNow)
                    {
                        column["fromNow"] = dateAttr.FromNow;
                    }
                }
                else
                {
                    // 默认日期格式
                    column["type"] = "date";
                    column["format"] = "YYYY-MM-DD HH:mm:ss";
                }
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
                Type genericType = prop.PropertyType.GetGenericTypeDefinition();

                // 检查是否为 List<T> 类型
                if (genericType == typeof(List<>) || genericType == typeof(IEnumerable<>))
                {
                    Type elementType = prop.PropertyType.GetGenericArguments()[0];

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
            JObject listItem = [];

            // 获取 ListItemAttribute 特性，如果存在
            ListColumnAttribute listItemAttr = prop.GetCustomAttribute<ListColumnAttribute>();
            // 使用特性中的配置字段，若特性没有配置，则使用默认值
            listItem["title"] = $"${{{listItemAttr?.Title.ToCamelCase() ?? "titile"}}}";
            listItem["subTitle"] = $"${{{listItemAttr?.SubTitle ?? "subTitile".ToCamelCase()}}}";
            listItem["placeholder"] = listItemAttr?.Placeholder ?? "-";
            return listItem;
        }

        private static readonly ConcurrentDictionary<Type, JObject> EnumMappingCache = new();

        private JObject GetEnumMapping(Type type)
        {
            Type enumType = Nullable.GetUnderlyingType(type) ?? type;
            return EnumMappingCache.GetOrAdd(enumType, CreateEnumMapping);
        }

        /// <summary>
        /// 创建枚举映射。
        /// </summary>
        /// <param name="enumType">枚举类型。</param>
        /// <returns>AMIS 枚举映射的 JSON 对象。</returns>
        private JObject CreateEnumMapping(Type enumType)
        {
            IEnumerable<object> enumValues = Enum.GetValues(enumType).Cast<object>();
            JObject mapping = [];

            foreach (object e in enumValues)
            {
                // 获取枚举的实际值（根据基础类型动态转换）
                Type underlyingType = Enum.GetUnderlyingType(enumType);
                string value = Convert.ChangeType(e, underlyingType, CultureInfo.InvariantCulture).ToString();
                string label = enumType.GetEnumDisplayName(e);
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

            // 检查是否有 TplColumn 特性
            TplColumnAttribute tplAttr = prop.GetCustomAttribute<TplColumnAttribute>();
            if (tplAttr != null)
            {
                return "tpl";
            }

            // 检查是否有 LinkColumn 特性
            LinkColumnAttribute linkAttr = prop.GetCustomAttribute<LinkColumnAttribute>();
            return linkAttr != null
                ? "link"
                : prop.PropertyType switch
                {
                    Type t when t == typeof(bool) => "switch",
                    Type t when t == typeof(DateTime) || t == typeof(DateTime?) || t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?) => "date",
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
            DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null && dataTypeAttr.DataType == DataType.ImageUrl)
            {
                return true;
            }

            // 另外，可以根据属性名称包含 "Image" 或 "Avatar" 来判断
            return prop.Name.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 获取 AMIS 图片或头像列的类型，根据属性名称或其他逻辑决定是 'image' 还是 'avatar'。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 列的类型字符串。</returns>
        private string GetImageColumnType(PropertyInfo prop)
        {
            return prop.GetCustomAttribute<AvatarColumnAttribute>() != null
                ? "avatar"
                : prop.Name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0 ? "avatar" : "image";
        }

        /// <summary>
        /// 创建 AMIS 表格的操作列，包括编辑和删除按钮。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="dataType">数据类型，用于生成表单字段。</param>
        /// <returns>AMIS 操作列的 JSON 对象，如果没有按钮则返回 null。</returns>
        private JObject CreateOperationsColumn(string controllerName, Type dataType, ApiRoutesInfo apiRoute, CrudActions actions)
        {
            JArray buttons = [];
            if (actions.Detail != null)
            {
                if (apiRoute.Detail != null && actions.Detail != null)
                {
                    Type actualType = actions.Detail.ReturnType.GetUnderlyingDataType();
                    PropertyInfo[] properties = actualType.GetProperties();

                    JObject detailButton = buttonHelper.CreateDetailButton(apiRoute.Detail, properties);
                    buttons.Add(detailButton);
                }
            }

            // 如果用户有编辑权限，则添加编辑按钮
            //if (_permissionService.HasPermission($"{controllerName}Edit"))
            if (actions.Update != null)
            {
                if (apiRoute.Update != null && actions.Update != null)
                {
                    JObject editButton = buttonHelper.CreateEditButton(apiRoute.Update, actions.Update?.GetParameters());
                    buttons.Add(editButton);
                }
            }

            // 如果用户有删除权限，则添加删除按钮
            if (actions.Delete != null
                //&& _permissionService.HasPermission($"{controllerName}Delete")
                )
            {
                OperationAttribute operationAttribute = actions.Delete.GetCustomAttribute<OperationAttribute>();
                if (operationAttribute == null)
                {
                    JObject deleteButton = buttonHelper.CreateDeleteButton(apiRoute.Delete);
                    buttons.Add(deleteButton);
                }
            }

            // 添加自定义操作按钮
            List<JObject> customButtons = buttonHelper.GetCustomOperationsButtons();
            foreach (JObject btn in customButtons)
            {
                buttons.Add(btn);
            }

            // 如果没有任何按钮，则不添加操作列
            return buttons.Count == 0
                ? null
                : new JObject
                {
                    ["name"] = "operation",
                    ["label"] = "操作",
                    ["type"] = "operation",
                    ["buttons"] = buttons,
                    ["fixed"] = "right"
                };
        }
    }
}
