using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Handlers
{
    /// <summary>
    /// CrudDialog 处理器，用于生成 CRUD 对话框的 AMIS schema
    /// </summary>
    public class CrudDialogHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CrudDialogHandler> _logger;
        private ColumnHelper _columnHelper;

        /// <summary>
        /// 初始化 CrudDialogHandler
        /// </summary>
        /// <param name="serviceProvider">服务提供者（用于延迟解析 ColumnHelper）</param>
        /// <param name="logger">日志记录器</param>
        public CrudDialogHandler(IServiceProvider serviceProvider, ILogger<CrudDialogHandler> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取 ColumnHelper（延迟解析以避免循环依赖）
        /// </summary>
        private ColumnHelper ColumnHelper
        {
            get
            {
                if (_columnHelper == null)
                {
                    _columnHelper = _serviceProvider.GetRequiredService<ColumnHelper>();
                }
                return _columnHelper;
            }
        }

        /// <summary>
        /// 生成 CrudDialog 的 AMIS schema
        /// </summary>
        /// <param name="operation">CRUD对话框操作特性配置</param>
        /// <returns>包含 body 字段的 schema 对象</returns>
        public JObject GenerateCrudDialogSchema(CrudDialogOperationAttribute operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (string.IsNullOrEmpty(operation.DataApi))
                throw new ArgumentException("DataApi 不能为空", nameof(operation));
            if (operation.DataType == null)
                throw new ArgumentException("DataType 不能为空", nameof(operation));

            // 构建 CRUD 组件的 body schema
            var crudBody = new JObject
            {
                ["type"] = "crud",
                ["api"] = BuildApiConfig(operation.DataApi),
                ["syncLocation"] = false
            };

            // 配置分页
            if (operation.EnablePagination)
            {
                crudBody["perPage"] = operation.PerPage;
                if (operation.PerPageOptions != null && operation.PerPageOptions.Length > 0)
                {
                    crudBody["perPageAvailable"] = new JArray(operation.PerPageOptions);
                }
            }
            else
            {
                crudBody["perPage"] = 0; // 不分页
            }

            // 配置工具栏
            var headerToolbar = new List<string>();
            if (operation.EnableRefresh)
            {
                headerToolbar.Add("reload");
            }
            if (operation.EnableSearch)
            {
                headerToolbar.Add("filter-toggler");
            }
            if (operation.EnableExport)
            {
                headerToolbar.Add("export");
            }
            if (headerToolbar.Count > 0)
            {
                crudBody["headerToolbar"] = new JArray(headerToolbar);
            }

            // 生成列配置
            var columns = GenerateColumns(operation, operation.DataType);
            if (columns != null && columns.Count > 0)
            {
                crudBody["columns"] = new JArray(columns);
            }

            // 返回包含 body 字段的对象（Service 组件期望的格式）
            return new JObject
            {
                ["body"] = crudBody
            };
        }

        /// <summary>
        /// 构建 API 配置
        /// </summary>
        private JObject BuildApiConfig(string dataApi)
        {
            // 支持模板变量，如 ${page}, ${perPage} 等
            var apiConfig = new JObject
            {
                ["method"] = "get",
                ["url"] = dataApi
            };

            // 添加查询参数
            var data = new JObject
            {
                ["page"] = "${page}",
                ["perPage"] = "${perPage}"
            };

            // 支持排序参数
            data["orderBy"] = "${orderBy}";
            data["orderDir"] = "${orderDir}";

            apiConfig["data"] = data;

            return apiConfig;
        }

        /// <summary>
        /// 生成列配置（支持三级优先级：手动 > 特性 > 自动）
        /// </summary>
        private List<JObject> GenerateColumns(CrudDialogOperationAttribute operation, Type dataType)
        {
            List<JObject> columns;

            // 优先级1: 如果提供了自定义列配置，直接使用
            if (!string.IsNullOrEmpty(operation.CustomColumns))
            {
                try
                {
                    var customColumnsArray = JsonConvert.DeserializeObject<JArray>(operation.CustomColumns);
                    if (customColumnsArray != null)
                    {
                        columns = customColumnsArray.Cast<JObject>().ToList();
                        _logger.LogDebug("使用自定义列配置");
                        return columns;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "解析 CustomColumns 配置失败，将使用自动生成列");
                }
            }

            // 优先级2 & 3: 直接基于属性生成列（跳过权限检查，因为这只是生成 schema）
            try
            {
                // 直接基于属性生成列配置，不进行权限检查
                // 权限检查应该在数据访问时进行，而不是在生成 schema 时
                columns = GenerateColumnsFromProperties(dataType);

                // 如果配置了行操作按钮，添加操作列
                if (!string.IsNullOrEmpty(operation.RowActions))
                {
                    try
                    {
                        var rowActionsArray = JsonConvert.DeserializeObject<JArray>(operation.RowActions);
                        if (rowActionsArray != null && rowActionsArray.Count > 0)
                        {
                            var operationColumn = new JObject
                            {
                                ["label"] = "操作",
                                ["type"] = "operation",
                                ["buttons"] = rowActionsArray
                            };
                            columns.Add(operationColumn);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "解析 RowActions 配置失败");
                    }
                }

                _logger.LogDebug("使用自动生成的列配置，共 {Count} 列", columns.Count);
                return columns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成列配置时发生错误");
                return new List<JObject>();
            }
        }

        /// <summary>
        /// 直接从属性生成列配置（不进行权限检查）
        /// </summary>
        private List<JObject> GenerateColumnsFromProperties(Type dataType)
        {
            var columns = new List<JObject>();
            
            // 延迟解析 ColumnHelper，但只用于获取属性列表和基本配置，不进行权限检查
            try
            {
                // 使用 ColumnHelper 的 UtilityHelper 来获取有序属性列表
                var utilityHelper = _serviceProvider.GetRequiredService<CodeSpirit.Amis.Helpers.UtilityHelper>();
                var properties = utilityHelper.GetOrderedProperties(dataType);

                foreach (var prop in properties)
                {
                    // 跳过忽略的属性（通过特性检查）
                    var ignoreAttr = prop.GetCustomAttribute<CodeSpirit.Amis.Attributes.Columns.IgnoreColumnAttribute>();
                    if (ignoreAttr != null)
                        continue;

                    // 跳过 Id 字段（通常不需要显示）
                    if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 创建列配置
                    var column = CreateColumnFromProperty(prop);
                    if (column != null)
                    {
                        columns.Add(column);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "使用 ColumnHelper 生成列配置失败，将使用简化版本");
                // 降级到简化版本
                var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 获取字段名：优先使用 JsonProperty 特性的 PropertyName，确保与 JSON 序列化一致
                    var jsonPropertyAttr = prop.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
                    string fieldName = jsonPropertyAttr?.PropertyName ?? prop.Name.ToCamelCase();

                    var column = new JObject
                    {
                        ["name"] = fieldName,
                        ["label"] = GetDisplayName(prop),
                        ["type"] = "text"
                    };
                    columns.Add(column);
                }
            }

            return columns;
        }

        /// <summary>
        /// 从属性创建列配置
        /// </summary>
        private JObject CreateColumnFromProperty(PropertyInfo prop)
        {
            // 检查是否有 AmisColumnAttribute
            var amisColumnAttr = prop.GetCustomAttribute<CodeSpirit.Amis.Attributes.Columns.AmisColumnAttribute>();
            if (amisColumnAttr != null)
            {
                var column = new JObject();
                
                // 获取字段名：优先使用 JsonProperty 特性的 PropertyName，确保与 JSON 序列化一致
                var jsonPropertyAttr = prop.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
                string fieldName = amisColumnAttr.Name ?? jsonPropertyAttr?.PropertyName ?? prop.Name.ToCamelCase();
                
                // 使用特性中的配置
                column["name"] = fieldName;
                column["label"] = amisColumnAttr.Label ?? GetDisplayName(prop);
                column["type"] = amisColumnAttr.Type ?? "text";
                column["sortable"] = amisColumnAttr.Sortable;
                column["hidden"] = amisColumnAttr.Hidden;
                
                if (amisColumnAttr.Hidden)
                    return null; // 隐藏的列不添加
                
                return column;
            }

            // 获取字段名：优先使用 JsonProperty 特性的 PropertyName，确保与 JSON 序列化一致
            var jsonPropertyAttr2 = prop.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>();
            string fieldName2 = jsonPropertyAttr2?.PropertyName ?? prop.Name.ToCamelCase();

            // 没有特性，使用默认配置
            var columnObj = new JObject
            {
                ["name"] = fieldName2,
                ["label"] = GetDisplayName(prop),
                ["type"] = "text",
                ["sortable"] = true
            };

            // 根据属性类型设置列类型
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (propType == typeof(DateTime) || propType == typeof(DateTimeOffset))
            {
                columnObj["type"] = "datetime";
                columnObj["format"] = "YYYY-MM-DD HH:mm:ss";
            }
            else if (propType == typeof(bool))
            {
                columnObj["type"] = "switch";
            }
            else if (propType == typeof(int) || propType == typeof(long) || propType == typeof(decimal) || propType == typeof(double) || propType == typeof(float))
            {
                columnObj["type"] = "text";
            }

            return columnObj;
        }

        /// <summary>
        /// 获取属性的显示名称
        /// </summary>
        private string GetDisplayName(PropertyInfo prop)
        {
            var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttr != null)
                return displayNameAttr.DisplayName;

            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Name))
                return displayAttr.Name;

            return prop.Name;
        }
    }
}

