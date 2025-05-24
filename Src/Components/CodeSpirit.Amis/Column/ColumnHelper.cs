using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Attributes;
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
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Name);
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

            // 首先处理 AmisColumnAttribute 配置（最高优先级）
            AmisColumnAttribute columnAttr = (AmisColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(AmisColumnAttribute));
            if (columnAttr != null)
            {
                if (!string.IsNullOrEmpty(columnAttr.Name))
                {
                    column["name"] = columnAttr.Name;
                    fieldName = columnAttr.Name; // 更新fieldName用于后续处理
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

                if (columnAttr.Disabled)
                    column["disabled"] = columnAttr.Disabled;

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

                // 如果AmisColumnAttribute明确指定了类型，则直接返回，不进行其他自动推断
                if (!string.IsNullOrEmpty(columnAttr.Type))
                {
                    // 如果是主键，依然需要隐藏该列
                    if (IsPrimaryKey(prop))
                    {
                        column["hidden"] = true;
                    }

                    return column;
                }
            }

            // 处理日期列特性
            DateColumnAttribute dateAttr = prop.GetCustomAttribute<DateColumnAttribute>();
            if (dateAttr != null)
            {
                column["type"] = "date";
                
                // 设置日期格式
                if (!string.IsNullOrEmpty(dateAttr.Format))
                {
                    column["format"] = dateAttr.Format;
                }
                else
                {
                    // 根据属性类型设置默认格式
                    Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (propType == typeof(DateTime) || propType == typeof(DateTimeOffset))
                    {
                        if (prop.Name.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prop.Name.IndexOf("Created", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prop.Name.IndexOf("Updated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prop.Name.IndexOf("Modified", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            column["format"] = "YYYY-MM-DD HH:mm:ss";
                        }
                        else
                        {
                            column["format"] = "YYYY-MM-DD";
                        }
                    }
                }

                // 设置输入格式
                if (!string.IsNullOrEmpty(dateAttr.InputFormat))
                {
                    column["inputFormat"] = dateAttr.InputFormat;
                }

                // 设置占位符
                if (!string.IsNullOrEmpty(dateAttr.Placeholder))
                {
                    column["placeholder"] = dateAttr.Placeholder;
                }

                // 设置相对时间显示
                if (dateAttr.FromNow)
                {
                    column["fromNow"] = true;
                }
            }

            // 处理没有DateColumnAttribute但是是日期时间类型的属性
            Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (dateAttr == null && (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset)))
            {
                // 应用智能日期格式化和优化配置
                ApplyDateTimeColumnOptimization(column, prop, fieldName, underlyingType);
            }

            // 处理 Tags 列 - 检查是显式的TagsColumnAttribute还是要自动应用
            TagsColumnAttribute tagsAttr = prop.GetCustomAttribute<TagsColumnAttribute>();
            bool isTagsField = prop.Name.Equals("Tags", StringComparison.OrdinalIgnoreCase);
            bool isStringArrayType = IsStringArrayProperty(prop);

            // 如果有TagsColumnAttribute特性或者是符合自动应用条件的属性
            if (tagsAttr != null || (isTagsField && isStringArrayType))
            {
                // 如果是自动应用的标签列，使用默认配置创建TagsColumnAttribute
                tagsAttr ??= new TagsColumnAttribute();

                // 应用标签列配置
                JObject tagsColumn = CreateTagsColumn(column, fieldName, tagsAttr);
                
                // 应用AmisColumnAttribute的配置（如果有的话）
                if (columnAttr != null)
                {
                    ApplyAmisColumnAttributeToColumn(tagsColumn, columnAttr);
                }
                
                // 如果是主键，依然需要隐藏该列
                if (IsPrimaryKey(prop))
                {
                    tagsColumn["hidden"] = true;
                }
                
                return tagsColumn;
            }

            // 处理所有的字符串数组类型（List<string>、string[]等）
            if (isStringArrayType)
            {
                JObject stringArrayColumn = CreateStringArrayColumn(column, fieldName);
                
                // 应用AmisColumnAttribute的配置（如果有的话）
                if (columnAttr != null)
                {
                    ApplyAmisColumnAttributeToColumn(stringArrayColumn, columnAttr);
                }
                
                // 如果是主键，依然需要隐藏该列
                if (IsPrimaryKey(prop))
                {
                    stringArrayColumn["hidden"] = true;
                }
                
                return stringArrayColumn;
            }

            // 处理枚举类型的映射列
            if (_utilityHelper.IsEnumProperty(prop))
            {
                column["type"] = "mapping";
                column["map"] = GetEnumMapping(prop.PropertyType);
            }

            // 处理图片或头像字段
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
                        if (!string.IsNullOrEmpty(avatarAttr.Size))
                        {
                            column["size"] = avatarAttr.Size;
                        }
                        
                        if (!string.IsNullOrEmpty(avatarAttr.Shape))
                        {
                            column["shape"] = avatarAttr.Shape;
                        }
                        
                        if (!string.IsNullOrEmpty(avatarAttr.Icon))
                        {
                            column["icon"] = avatarAttr.Icon;
                        }
                        
                        if (!string.IsNullOrEmpty(avatarAttr.Text))
                        {
                            column["text"] = avatarAttr.Text;
                        }

                        if (!string.IsNullOrEmpty(avatarAttr.Fit))
                        {
                            column["fit"] = avatarAttr.Fit;
                        }

                        if (avatarAttr.Gap.HasValue)
                        {
                            column["gap"] = avatarAttr.Gap.Value;
                        }

                        if (!string.IsNullOrEmpty(avatarAttr.OnError))
                        {
                            column["onError"] = avatarAttr.OnError;
                        }
                    }
                    else
                    {
                        // 设置默认头像配置
                        column["size"] = "default";
                        column["shape"] = "circle";
                    }
                }
                else if (columnType == "image")
                {
                    // 设置默认图片配置
                    column["enlargeAble"] = true;
                    column["thumbMode"] = "cover";
                    column["thumbRatio"] = "1:1";
                }
            }

            // 处理时长字段（支持所有数值类型）
            if (IsDurationField(prop))
            {
                ApplyDurationColumnFormat(column, prop, fieldName);
            }
            
            // 处理数值类型的格式化
            Type numericUnderlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (numericUnderlyingType == typeof(decimal) || numericUnderlyingType == typeof(double) || numericUnderlyingType == typeof(float))
            {
                // 如果时长字段已经处理过，则跳过
                if (IsDurationField(prop))
                {
                    // 时长字段已处理，跳过
                }
                // 如果是金额相关字段，添加货币格式
                else if (prop.Name.IndexOf("Amount", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Price", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Cost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Fee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Money", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    column["type"] = "tpl";
                    column["tpl"] = "¥${" + fieldName + "|number:2}";
                }
                else if (prop.Name.IndexOf("Rate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         prop.Name.IndexOf("Percent", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         prop.Name.IndexOf("Ratio", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // 百分比显示
                    column["type"] = "tpl";
                    column["tpl"] = "${" + fieldName + "|percent}";
                }
            }

            // 处理状态类字段的映射
            if (prop.Name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("State", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (numericUnderlyingType == typeof(string))
                {
                    column["type"] = "status";
                }
            }

            // 处理链接字段
            if (prop.Name.IndexOf("Url", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Link", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Website", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
                if (dataTypeAttr?.DataType == DataType.Url)
                {
                    column["type"] = "link";
                    column["href"] = "${" + fieldName + "}";
                    column["body"] = "${" + fieldName + "}";
                }
            }

            // 处理电子邮件字段
            if (prop.Name.IndexOf("Email", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Mail", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
                if (dataTypeAttr?.DataType == DataType.EmailAddress)
                {
                    column["type"] = "link";
                    column["href"] = "mailto:${" + fieldName + "}";
                    column["body"] = "${" + fieldName + "}";
                }
            }

            // 处理电话号码字段
            if (prop.Name.IndexOf("Phone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Tel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
                if (dataTypeAttr?.DataType == DataType.PhoneNumber)
                {
                    // 使用增强的电话号码显示格式
                    column["type"] = "tpl";
                    column["tpl"] = "<a href=\"tel:${" + fieldName + "}\" class=\"text-primary\"><i class=\"fa fa-phone\"></i> ${" + fieldName + "}</a>";
                    column["copyable"] = true; // 支持复制
                }
                else
                {
                    // 即使没有DataType特性，也要优化显示
                    column["type"] = "tpl"; 
                    column["tpl"] = "<span class=\"text-muted\"><i class=\"fa fa-phone\"></i></span> <a href=\"tel:${" + fieldName + "}\" class=\"text-primary\">${" + fieldName + "}</a>";
                    column["copyable"] = true;
                }
                column["sortable"] = true;
            }

            // 处理密码字段（通常应该隐藏或脱敏）
            if (prop.Name.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Pwd", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                column["type"] = "tpl";
                column["tpl"] = "******";
                column["sortable"] = false;
            }

            // 处理文本长度较长的字段，自动截断显示
            if (numericUnderlyingType == typeof(string))
            {
                if (prop.Name.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Content", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Note", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Remark", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prop.Name.IndexOf("Comment", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    column["type"] = "text";
                    column["breakWord"] = true;
                    // 添加省略号显示
                    var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                    if (maxLengthAttr != null && maxLengthAttr.Length > 50)
                    {
                        column["type"] = "tpl";
                        column["tpl"] = "${" + fieldName + " | truncate:50}";
                        column["popOver"] = new JObject
                        {
                            ["body"] = "${" + fieldName + "}"
                        };
                    }
                }
            }

            // 处理 List 类型属性
            if (IsListProperty(prop))
            {
                column["type"] = "list";
                
                // 获取 List 类型的配置
                JObject listItem = CreateListItemConfiguration(prop);
                if (listItem != null)
                {
                    column["listItem"] = listItem;
                }
            }

            // 处理 Each 类型列
            EachColumnAttribute eachAttr = prop.GetCustomAttribute<EachColumnAttribute>();
            if (eachAttr != null)
            {
                column["type"] = "each";
                
                // 设置数据来源
                if (!string.IsNullOrEmpty(eachAttr.Source))
                {
                    column["source"] = eachAttr.Source;
                }
                else
                {
                    column["source"] = "${" + fieldName + "}";
                }

                // 设置循环项变量名
                if (!string.IsNullOrEmpty(eachAttr.ItemVariable))
                {
                    column["itemVariable"] = eachAttr.ItemVariable;
                }

                // 设置索引变量名
                if (!string.IsNullOrEmpty(eachAttr.IndexVariable))
                {
                    column["indexVariable"] = eachAttr.IndexVariable;
                }

                // 设置每项的渲染模板
                if (!string.IsNullOrEmpty(eachAttr.ItemTemplate))
                {
                    column["items"] = new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = eachAttr.ItemTemplate
                    };
                }
                else
                {
                    // 默认显示为文本
                    column["items"] = new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = "${" + eachAttr.ItemVariable + "}"
                    };
                }
            }

            // 如果任何时候列的类型被设置为switch，并且不存在QuickSave方法，则设置disabled为true
            if (column["type"]?.ToString() == "switch" && amisContext.Actions.QuickSave == null)
            {
                column["disabled"] = true;
            }

            // 如果是主键，则隐藏该列
            if (IsPrimaryKey(prop))
            {
                column["hidden"] = true;
            }

            // 最后再次应用AmisColumnAttribute的配置（确保覆盖所有自动推断的设置）
            if (columnAttr != null)
            {
                ApplyAmisColumnAttributeToColumn(column, columnAttr);
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

            // 获取 ListColumnAttribute 特性，如果存在
            ListColumnAttribute listItemAttr = prop.GetCustomAttribute<ListColumnAttribute>();
            
            // 修复拼写错误并提供更好的默认值
            string titleField = listItemAttr?.Title ?? "title";
            string subTitleField = listItemAttr?.SubTitle ?? "subTitle";
            
            // 确保字段名是camelCase格式
            listItem["title"] = $"${{{titleField.ToCamelCase()}}}";
            listItem["subTitle"] = $"${{{subTitleField.ToCamelCase()}}}";
            
            // 尝试智能推断头像字段
            listItem["avatar"] = "${avatar || profileImage || image || ''}";
            
            // 设置占位符
            listItem["placeholder"] = listItemAttr?.Placeholder ?? "暂无数据";
            
            // 添加描述字段支持 - 根据常见命名模式推断
            listItem["desc"] = "${description || desc || remark || ''}";
            
            // 添加默认的操作按钮配置
            listItem["actions"] = new JArray
            {
                new JObject
                {
                    ["type"] = "button",
                    ["label"] = "查看",
                    ["level"] = "link",
                    ["actionType"] = "dialog",
                    ["dialog"] = new JObject
                    {
                        ["title"] = "详情",
                        ["body"] = "${title || name}的详细信息"
                    }
                }
            };
            
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
            // 优先检查枚举类型
            if (_utilityHelper.IsEnumProperty(prop))
            {
                return "mapping";
            }

            // 检查是否有显式的列类型特性
            TplColumnAttribute tplAttr = prop.GetCustomAttribute<TplColumnAttribute>();
            if (tplAttr != null)
            {
                return "tpl";
            }

            LinkColumnAttribute linkAttr = prop.GetCustomAttribute<LinkColumnAttribute>();
            if (linkAttr != null)
            {
                return "link";
            }

            DateColumnAttribute dateAttr = prop.GetCustomAttribute<DateColumnAttribute>();
            if (dateAttr != null)
            {
                return "date";
            }

            TagsColumnAttribute tagsAttr = prop.GetCustomAttribute<TagsColumnAttribute>();
            if (tagsAttr != null)
            {
                return "each";
            }

            EachColumnAttribute eachAttr = prop.GetCustomAttribute<EachColumnAttribute>();
            if (eachAttr != null)
            {
                return "each";
            }

            AvatarColumnAttribute avatarAttr = prop.GetCustomAttribute<AvatarColumnAttribute>();
            if (avatarAttr != null)
            {
                return "avatar";
            }

            // 检查是否为图片字段
            if (IsImageField(prop))
            {
                return GetImageColumnType(prop);
            }

            // 检查是否为 List 类型
            if (IsListProperty(prop))
            {
                return "list";
            }

            // 根据属性类型进行基础映射
            Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            return underlyingType switch
            {
                // 布尔类型映射为开关
                Type t when t == typeof(bool) => "switch",
                
                // 日期时间类型
                Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset) => "date",
                Type t when t == typeof(TimeSpan) => "text", // TimeSpan 显示为文本
                
                // 数值类型
                Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) || 
                           t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) ||
                           t == typeof(byte) || t == typeof(sbyte) => "text",
                           
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => "text",
                
                // 浮点数类型（Oracle特定）
                Type t when t.Name == "BINARY_FLOAT" || t.Name == "BINARY_DOUBLE" => "text",
                
                // Guid 类型
                Type t when t == typeof(Guid) => "text",
                
                // 字符串类型 - 根据字段名称和特性判断具体类型
                Type t when t == typeof(string) => GetStringColumnType(prop),
                
                // 字节数组（通常用于存储文件或图片）
                Type t when t == typeof(byte[]) => "text",
                
                // JSON 类型（如果项目支持）
                Type t when t.Name == "JsonElement" || t.Name == "JObject" || t.Name == "JToken" => "json",
                
                // 数组和集合类型
                Type t when t.IsArray => IsStringArrayProperty(prop) ? "each" : "text",
                Type t when t.IsGenericType && (
                    t.GetGenericTypeDefinition() == typeof(List<>) ||
                    t.GetGenericTypeDefinition() == typeof(IList<>) ||
                    t.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    t.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ) => GetCollectionColumnType(prop),
                
                // 复杂对象类型默认显示为JSON
                Type t when t.IsClass && t != typeof(string) && !t.IsArray => "json",
                
                // 其他值类型（结构体等）也显示为JSON
                Type t when t.IsValueType && !t.IsPrimitive && !t.IsEnum && 
                           t != typeof(decimal) && t != typeof(DateTime) && t != typeof(DateTimeOffset) &&
                           t != typeof(TimeSpan) && t != typeof(Guid) => "json",
                
                // 默认为文本类型
                _ => "text"
            };
        }

        /// <summary>
        /// 根据字符串字段的特征确定列类型
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>列类型</returns>
        private string GetStringColumnType(PropertyInfo prop)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 检查 DataType 特性
            DataTypeAttribute dataTypeAttr = prop.GetCustomAttribute<DataTypeAttribute>();
            if (dataTypeAttr != null)
            {
                return dataTypeAttr.DataType switch
                {
                    DataType.Url => "link",
                    DataType.EmailAddress => "link",
                    DataType.PhoneNumber => "tpl", // 在CreateAmisColumn中会进一步处理为电话号码格式
                    DataType.ImageUrl => "image",
                    DataType.Password => "text", // 密码字段通常需要特殊处理
                    DataType.MultilineText => "text",
                    DataType.Html => "html",
                    DataType.Date => "date",
                    DataType.DateTime => "date",
                    DataType.Time => "text",
                    DataType.Duration => "text",
                    DataType.Currency => "text",
                    DataType.PostalCode => "text",
                    DataType.CreditCard => "text",
                    _ => "text"
                };
            }

            // 根据字段名称模式推断类型
            if (propName.Contains("url") || propName.Contains("link") || propName.Contains("website"))
            {
                return "link";
            }

            if (propName.Contains("email") || propName.Contains("mail"))
            {
                return "link";
            }

            if (propName.Contains("phone") || propName.Contains("tel") || propName.Contains("mobile"))
            {
                return "tpl"; // 在CreateAmisColumn中会进一步处理为电话号码格式
            }

            if (propName.Contains("status") || propName.Contains("state"))
            {
                return "status";
            }

            if (propName.Contains("password") || propName.Contains("pwd"))
            {
                return "text"; // 密码字段在 CreateAmisColumn 中会被特殊处理
            }

            if (propName.Contains("description") || propName.Contains("content") || 
                propName.Contains("note") || propName.Contains("remark") || propName.Contains("comment"))
            {
                // 长文本字段，可能需要截断或弹窗显示
                var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLengthAttr != null && maxLengthAttr.Length > 100)
                {
                    return "tpl"; // 在 CreateAmisColumn 中会添加截断逻辑
                }
                return "text";
            }

            if (propName.Contains("json") || propName.Contains("config") || propName.Contains("setting"))
            {
                return "json";
            }

            if (propName.Contains("html") || propName.Contains("rich"))
            {
                return "html";
            }

            if (propName.Contains("color") || propName.Contains("colour"))
            {
                return "color";
            }

            return "text";
        }

        /// <summary>
        /// 根据集合类型确定列类型
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>列类型</returns>
        private string GetCollectionColumnType(PropertyInfo prop)
        {
            Type collectionType = prop.PropertyType;
            
            // 检查是否为字符串数组/集合
            if (IsStringArrayProperty(prop))
            {
                return "each"; // 字符串数组通常显示为标签
            }

            // 检查是否为基础类型的集合
            if (collectionType.IsGenericType)
            {
                Type elementType = collectionType.GetGenericArguments()[0];
                Type underlyingElementType = Nullable.GetUnderlyingType(elementType) ?? elementType;

                if (underlyingElementType.IsPrimitive || underlyingElementType == typeof(string) || 
                    underlyingElementType == typeof(decimal) || underlyingElementType == typeof(DateTime) ||
                    underlyingElementType == typeof(Guid))
                {
                    return "each"; // 基础类型集合显示为循环
                }
                
                if (underlyingElementType.IsClass && underlyingElementType != typeof(string))
                {
                    return "list"; // 复杂对象集合显示为列表
                }
            }

            return "text"; // 默认显示为文本
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
                if (apiRoute.Detail != null && actions.Detail != null && _permissionService.HasPermission(_permissionService.GetPermissionCode(actions.Detail)))
                {
                    Type actualType = actions.Detail.ReturnType.GetUnderlyingDataType();
                    PropertyInfo[] properties = actualType.GetProperties();

                    JObject detailButton = buttonHelper.CreateDetailButton(apiRoute.Detail, properties);
                    buttons.Add(detailButton);
                }
            }

            if (actions.Update != null && _permissionService.HasPermission(_permissionService.GetPermissionCode(actions.Update)))
            {
                if (apiRoute.Update != null && actions.Update != null)
                {
                    JObject editButton = buttonHelper.CreateEditButton(apiRoute.Update, actions.Update?.GetParameters());
                    buttons.Add(editButton);
                }
            }

            // 如果用户有删除权限，则添加删除按钮
            if (actions.Delete != null && _permissionService.HasPermission(_permissionService.GetPermissionCode(actions.Delete)))
            {
                OperationAttribute operationAttribute = actions.Delete.GetCustomAttribute<OperationAttribute>();
                if (operationAttribute == null)
                {
                    JObject deleteButton = buttonHelper.CreateDeleteButton(apiRoute.Delete);
                    buttons.Add(deleteButton);
                }
            }

            // 添加自定义操作按钮
            List<JObject> customButtons = buttonHelper.GetCustomOperationsButtons<OperationAttribute>();
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

        /// <summary>
        /// 判断属性是否为字符串数组类型
        /// </summary>
        /// <param name="prop">属性的信息</param>
        /// <returns>如果是字符串数组类型则返回true，否则返回false</returns>
        private bool IsStringArrayProperty(PropertyInfo prop)
        {
            Type type = prop.PropertyType;

            // 检查是否为普通数组类型
            if (type.IsArray && type.GetElementType() == typeof(string))
            {
                return true;
            }

            // 检查是否为泛型集合类型（如List<string>、IEnumerable<string>等）
            if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IEnumerable<>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    return genericArgs.Length == 1 && genericArgs[0] == typeof(string);
                }
            }

            return false;
        }

        // 处理 Tags 列的逻辑现在移到这个公共方法
        private JObject CreateTagsColumn(JObject column, string fieldName, TagsColumnAttribute tagsAttr)
        {
            column["type"] = "each";
            column["source"] = $"${fieldName}";

            // 构建标签模板
            string tagTemplate = $"<span class='{tagsAttr.CssClass} {tagsAttr.CssClass}-{tagsAttr.Color} {tagsAttr.ExtraClass}'>${{item}}</span>";

            JObject items = new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = tagTemplate
            };

            column["items"] = items;

            // 添加最大显示数量配置
            if (tagsAttr.MaxTags > 0)
            {
                column["maxLength"] = tagsAttr.MaxTags;
                column["overflowConfig"] = new JObject
                {
                    ["maxLength"] = tagsAttr.MaxTags,
                    ["overflowText"] = tagsAttr.OverflowTemplate
                };
            }

            // 添加占位符配置
            if (tagsAttr.ShowPlaceholder)
            {
                column["placeholder"] = tagsAttr.Placeholder;
            }

            return column;
        }

        /// <summary>
        /// 创建字符串数组列配置（用于List<string>、string[]等）
        /// </summary>
        /// <param name="column">基础列对象</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>配置好的列对象</returns>
        private JObject CreateStringArrayColumn(JObject column, string fieldName)
        {
            column["type"] = "each";
            column["source"] = $"${fieldName}";

            // 构建简洁的标签模板，使用默认的标签样式
            string tagTemplate = "<span class=\"badge badge-secondary mr-1\">${item}</span>";

            JObject items = new JObject
            {
                ["type"] = "tpl",
                ["tpl"] = tagTemplate
            };

            column["items"] = items;

            // 设置默认的最大显示数量
            column["maxLength"] = 10;
            column["overflowConfig"] = new JObject
            {
                ["maxLength"] = 10,
                ["overflowText"] = "...还有${total - 10}项"
            };

            // 设置占位符
            column["placeholder"] = "暂无数据";

            return column;
        }

        /// <summary>
        /// 将AmisColumnAttribute的配置应用到列对象
        /// </summary>
        /// <param name="column">列对象</param>
        /// <param name="columnAttr">AmisColumnAttribute特性</param>
        private void ApplyAmisColumnAttributeToColumn(JObject column, AmisColumnAttribute columnAttr)
        {
            if (!string.IsNullOrEmpty(columnAttr.Name))
            {
                column["name"] = columnAttr.Name;
            }

            if (!string.IsNullOrEmpty(columnAttr.Label))
            {
                column["label"] = columnAttr.Label;
            }

            if (!string.IsNullOrEmpty(columnAttr.Type))
            {
                column["type"] = columnAttr.Type;
            }

            if (!string.IsNullOrEmpty(columnAttr.Remark))
            {
                column["remark"] = columnAttr.Remark;
            }

            if (!string.IsNullOrEmpty(columnAttr.Fixed))
            {
                column["fixed"] = columnAttr.Fixed;
            }

            // 布尔属性直接设置
            column["sortable"] = columnAttr.Sortable;
            column["quickEdit"] = columnAttr.QuickEdit;
            column["copyable"] = columnAttr.Copyable;
            column["hidden"] = columnAttr.Hidden;
            
            if (!columnAttr.Toggled)
            {
                column["toggled"] = columnAttr.Toggled;
            }

            if (columnAttr.Disabled)
            {
                column["disabled"] = columnAttr.Disabled;
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

        /// <summary>
        /// 判断属性是否为时长字段
        /// </summary>
        /// <param name="prop">属性的信息</param>
        /// <returns>如果是时长字段则返回true，否则返回false</returns>
        private bool IsDurationField(PropertyInfo prop)
        {
            // 获取基础类型（处理可空类型）
            Type underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            
            // 检查是否为数值类型
            if (underlyingType != typeof(int) && underlyingType != typeof(long) && 
                underlyingType != typeof(short) && underlyingType != typeof(uint) && 
                underlyingType != typeof(ulong) && underlyingType != typeof(ushort) && 
                underlyingType != typeof(byte) && underlyingType != typeof(sbyte) &&
                underlyingType != typeof(decimal) && underlyingType != typeof(double) && 
                underlyingType != typeof(float))
            {
                return false;
            }
            
            // 检查是否为时长字段
            return prop.Name.IndexOf("Duration", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Elapsed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Delay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Latency", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Milliseconds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Seconds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Minutes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.Name.IndexOf("Hours", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 应用时长字段格式
        /// </summary>
        /// <param name="column">列对象</param>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        private void ApplyDurationColumnFormat(JObject column, PropertyInfo prop, string fieldName)
        {
            column["type"] = "tpl";
            
            // 根据字段名称智能判断单位
            if (prop.Name.IndexOf("Milliseconds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                prop.Name.IndexOf("Duration", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                column["tpl"] = "${" + fieldName + "} ms";
            }
            else if (prop.Name.IndexOf("Seconds", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                column["tpl"] = "${" + fieldName + "} s";
            }
            else if (prop.Name.IndexOf("Minutes", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                column["tpl"] = "${" + fieldName + "} min";
            }
            else if (prop.Name.IndexOf("Hours", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                column["tpl"] = "${" + fieldName + "} h";
            }
            else
            {
                // 默认使用毫秒单位
                column["tpl"] = "${" + fieldName + "} ms";
            }
        }

        /// <summary>
        /// 应用智能日期格式化和优化配置
        /// </summary>
        /// <param name="column">列对象</param>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="underlyingType">基础类型</param>
        private void ApplyDateTimeColumnOptimization(JObject column, PropertyInfo prop, string fieldName, Type underlyingType)
        {
            column["type"] = "date";
            
            // 智能推断日期格式
            string format = GetIntelligentDateFormat(prop, underlyingType);
            column["format"] = format;
            
            // 根据属性名称模式设置相对时间显示
            if (ShouldShowRelativeTime(prop))
            {
                column["fromNow"] = true;
                // 为相对时间显示设置更新频率（可选）
                column["updateFrequency"] = 60000; // 每分钟更新一次
            }
            
            // 设置默认占位符
            column["placeholder"] = GetDateTimePlaceholder(prop);
            
            // 为某些特殊日期字段添加特定配置
            ApplySpecialDateFieldConfig(column, prop, fieldName);
        }

        /// <summary>
        /// 智能推断日期格式
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <param name="underlyingType">基础类型</param>
        /// <returns>日期格式字符串</returns>
        private string GetIntelligentDateFormat(PropertyInfo prop, Type underlyingType)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 包含时间相关词汇的字段，显示完整的日期时间
            if (propName.Contains("time") || propName.Contains("created") ||
                propName.Contains("createdAt") || propName.Contains("updatedAt") || propName.Contains("deletedAt") ||
                propName.Contains("updated") || propName.Contains("modified") ||
                propName.Contains("login") || propName.Contains("accessed") ||
                propName.Contains("expired") || propName.Contains("finished") ||
                propName.Contains("started") || propName.Contains("ended"))
            {
                return "YYYY-MM-DD HH:mm:ss";
            }
            
            // 仅包含日期相关词汇的字段，只显示日期
            if (propName.Contains("date") || propName.Contains("birth") ||
                propName.Contains("anniversary") || propName.Contains("deadline"))
            {
                return "YYYY-MM-DD";
            }
            
            // 特殊时间格式
            if (propName.Contains("year"))
            {
                return "YYYY";
            }
            
            if (propName.Contains("month"))
            {
                return "YYYY-MM";
            }
            
            // 默认根据类型决定
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            {
                // 检查属性的默认值或上下文来判断是否需要时间部分
                return "YYYY-MM-DD HH:mm:ss";
            }
            
            return "YYYY-MM-DD";
        }

        /// <summary>
        /// 判断是否应该显示相对时间
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>是否显示相对时间</returns>
        private bool ShouldShowRelativeTime(PropertyInfo prop)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 这些字段适合显示相对时间
            return propName.Contains("created") || propName.Contains("updated") ||
                   propName.Contains("modified") || propName.Contains("last") ||
                   propName.Contains("recent") || propName.Contains("login") ||
                   propName.Contains("accessed") || propName.Contains("published");
        }

        /// <summary>
        /// 获取日期时间占位符
        /// </summary>
        /// <param name="prop">属性信息</param>
        /// <returns>占位符文本</returns>
        private string GetDateTimePlaceholder(PropertyInfo prop)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            if (propName.Contains("birth"))
                return "出生日期";
            if (propName.Contains("deadline"))
                return "截止日期";
            if (propName.Contains("start"))
                return "开始时间";
            if (propName.Contains("end"))
                return "结束时间";
            if (propName.Contains("created"))
                return "创建时间";
            if (propName.Contains("updated"))
                return "更新时间";
            if (propName.Contains("login"))
                return "登录时间";
            if (propName.Contains("expired"))
                return "过期时间";
                
            return "请选择日期";
        }

        /// <summary>
        /// 为特殊日期字段应用特定配置
        /// </summary>
        /// <param name="column">列对象</param>
        /// <param name="prop">属性信息</param>
        /// <param name="fieldName">字段名</param>
        private void ApplySpecialDateFieldConfig(JObject column, PropertyInfo prop, string fieldName)
        {
            string propName = prop.Name.ToLowerInvariant();
            
            // 为创建时间/更新时间等添加排序优化
            if (propName.Contains("created") || propName.Contains("updated") || 
                propName.Contains("modified"))
            {
                column["sortable"] = true;
                // 可以考虑默认按创建时间降序排列
                if (propName.Contains("created"))
                {
                    column["defaultSort"] = "desc";
                }
            }
        }
    }
}
