using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Amis.Attributes;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// 负责生成 AMIS CRUD 配置的构建器。
    /// </summary>
    public class AmisCRUDConfigBuilder
    {
        // 依赖注入的助手类
        private readonly ApiRouteHelper _apiRouteHelper;
        private readonly ColumnHelper _columnHelper;
        private readonly ButtonHelper _buttonHelper;
        private readonly SearchFieldHelper _searchFieldHelper;
        private readonly AmisContext _amisContext;
        private readonly UtilityHelper _utilityHelper;
        private readonly AmisApiHelper _amisApiHelper;
        private readonly AsideHelper _asideHelper;
        private readonly CardHelper _cardHelper;

        /// <summary>
        /// 构造函数，初始化所需的助手类。
        /// </summary>
        public AmisCRUDConfigBuilder(ApiRouteHelper apiRouteHelper, ColumnHelper columnHelper, ButtonHelper buttonHelper,
                                 SearchFieldHelper searchFieldHelper, AmisContext amisContext, UtilityHelper utilityHelper, AmisApiHelper amisApiHelper, AsideHelper asideHelper, CardHelper cardHelper)
        {
            _apiRouteHelper = apiRouteHelper;
            _columnHelper = columnHelper;
            _buttonHelper = buttonHelper;
            _searchFieldHelper = searchFieldHelper;
            _amisContext = amisContext;
            _utilityHelper = utilityHelper;
            _amisApiHelper = amisApiHelper;
            _asideHelper = asideHelper;
            _cardHelper = cardHelper;
        }

        /// <summary>
        /// 生成 AMIS 的 CRUD 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称</param>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="actions">CRUD 操作类型</param>
        /// <returns>返回 AMIS 配置的 JSON 对象</returns>
        public JObject GenerateAmisCrudConfig(string controllerName, Type controllerType, CrudActions actions)
        {
            // 获取基础路由信息
            string baseRoute = _apiRouteHelper.GetRoute();
            _amisContext.BaseRoute = baseRoute;

            ApiRoutesInfo apiRoutes = _apiRouteHelper.GetApiRoutes();
            _amisContext.ApiRoutes = apiRoutes;

            // 获取读取数据的类型，如果类型为空，则返回空
            Type dataType = _utilityHelper.GetDataTypeFromMethod(actions.List);
            if (dataType == null)
            {
                return null;
            }

            _amisContext.ListDataType = dataType;

            // 检查数据类型是否为PageList<>
            bool isPaginated = IsPageListType(actions.List.ReturnType);

            // 检查是否支持卡片模式
            bool isCardModeSupported = _cardHelper.IsCardModeSupported(controllerType);

            // 获取列配置和搜索字段
            List<JObject> columns = _columnHelper.GetAmisColumns();
            List<JObject> searchFields = _searchFieldHelper.GetAmisSearchFields(actions.List);

            string crudName = _amisContext.CrudComponentName; // CRUD组件名称
            // 构建 CRUD 配置
            JObject crudConfig = new()
            {
                ["type"] = "crud",  // 设置类型为 CRUD
                ["name"] = crudName,  // 设置配置名称
                ["api"] = _amisApiHelper.CreateApi(apiRoutes.Read),  // 设置 API 配置
                ["quickSaveApi"] = _amisApiHelper.CreateApi(apiRoutes.QuickSave),
                ["headerToolbar"] = BuildHeaderToolbar(isCardModeSupported),  // 设置头部工具栏
                ["bulkActions"] = new JArray(_buttonHelper.GetBulkOperationButtons()), //设置批量操作
            };

            // 配置卡片模式或表格模式
            if (isCardModeSupported)
            {
                ConfigureCardMode(crudConfig, controllerType, dataType);
            }
            else
            {
                ConfigureTableMode(crudConfig, columns);
            }

            // 只有分页数据才配置分页工具栏
            if (isPaginated)
            {
                crudConfig["footerToolbar"] = new JArray()
                {
                    "switch-per-page",
                    "pagination",
                    "statistics"
                };
            }
            else
            {
                // 非分页数据使用简化的工具栏
                crudConfig["footerToolbar"] = new JArray()
                {
                    "statistics"
                };

                // 对于非分页数据，设置一次性加载
                crudConfig["loadDataOnce"] = true;
            }

            // 如果有搜索字段，加入筛选配置
            if (searchFields.Any())
            {
                crudConfig["filter"] = BuildFilterConfig(searchFields);
            }

            // 检查是否需要生成aside配置
            Type queryDtoType = _utilityHelper.GetQueryDtoTypeFromMethod(actions.List);
            
            JObject asideConfig = _asideHelper.GenerateAsideConfig(queryDtoType, crudName);

            // 构建页面配置
            JObject pageConfig = new()
            {
                ["type"] = "page",
                ["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",
                ["body"] = new JArray()
                {
                    crudConfig
                },
                ["data"] = new JObject()
                {
                    ["ROOT_API"] = _apiRouteHelper.GetRootApi(),
                    ["BASE_API"] = $"{_apiRouteHelper.GetRootApi()}/{_apiRouteHelper.GetRoute().TrimStart('/')}"
                }
            };

            // 如果有aside配置，添加到页面中
            if (asideConfig != null)
            {
                pageConfig["aside"] = asideConfig;
            }

            return pageConfig;
        }

        #region 辅助方法
        /// <summary>
        /// 检查给定类型是否为分页列表类型(PageList<>)或包含分页数据结构
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <returns>如果类型是或包含PageList则返回true，否则返回false</returns>
        private bool IsPageListType(Type type)
        {
            if (type == null)
                return false;

            // 首先处理 Task 和 ActionResult
            Type unwrappedType = _utilityHelper.GetUnderlyingType(type) ?? type;

            // 递归检查是否包含PageList类型
            while (unwrappedType != null && unwrappedType.IsGenericType)
            {
                Type genericTypeDef = unwrappedType.GetGenericTypeDefinition();

                // 直接检查是否为PageList<>类型
                if (genericTypeDef == typeof(PageList<>))
                    return true;

                // 处理 ApiResponse<T>，继续检查内部类型
                if (genericTypeDef == typeof(ApiResponse<>))
                {
                    unwrappedType = unwrappedType.GetGenericArguments()[0];
                    continue;
                }

                // 如果是其他集合类型但不是PageList，则不算分页
                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IReadOnlyList<>) ||
                    genericTypeDef == typeof(IReadOnlyCollection<>))
                {
                    return false;
                }

                // 处理其他未知的泛型类型
                break;
            }

            return false;
        }

        /// <summary>
        /// 构建头部工具栏配置。
        /// </summary>
        /// <param name="isCardMode">是否为卡片模式</param>
        private JArray BuildHeaderToolbar(bool isCardMode = false)
        {
            JArray buttons = ["bulkActions"];
            
            // 获取所有自定义头部按钮，用于检查是否有重复的标准操作
            var headerCustomButtons = _buttonHelper.GetHeaderOperationButtons();
            
            // 检查是否有自定义的新增操作
            bool hasCustomCreateOperation = HasCustomHeaderOperationWithLabel(headerCustomButtons, "新增") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "添加") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "创建");
            
            // 检查是否有自定义的导入操作
            bool hasCustomImportOperation = HasCustomHeaderOperationWithLabel(headerCustomButtons, "导入") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "批量导入");

            // 添加新增按钮（如果没有自定义的新增操作）
            if (_amisContext.ApiRoutes.Create != null && _amisContext.Actions.Create != null && !hasCustomCreateOperation)
            {
                if (_amisContext.Actions.Create.GetCustomAttribute<HeaderOperationAttribute>() == null)
                {
                    buttons.Add(_buttonHelper.CreateHeaderButton("新增", _amisContext.ApiRoutes.Create, _amisContext.Actions.Create?.GetParameters(), method: _amisContext.Actions.Create));
                }
            }

            // 卡片模式不支持导出按钮
            if (!isCardMode)
            {
                buttons.Add(new JObject()
                {
                    ["type"] = "export-excel",
                    ["label"] = "导出当前页",
                    //["filename"] = ""
                });

                if (_amisContext.Actions.Export != null)
                {
                    buttons.Add(new JObject()
                    {
                        ["type"] = "export-excel",
                        ["label"] = "导出全部",
                        ["api"] = new JObject
                        {
                            ["url"] = _amisContext.ApiRoutes.Export.ApiPath,
                            ["method"] = _amisContext.ApiRoutes.Export.HttpMethod
                        },
                    });
                }
            }

            // 添加导入按钮（如果没有自定义的导入操作）
            if (_amisContext.ApiRoutes.Import != null && _amisContext.Actions.Import != null && !hasCustomImportOperation)
            {
                buttons.Add(_buttonHelper.CreateHeaderButton("导入", _amisContext.ApiRoutes.Import, _amisContext.Actions.Import?.GetParameters(), size: "lg", method: _amisContext.Actions.Import));
            }

            // 添加自定义顶部按钮
            if (headerCustomButtons != null && headerCustomButtons.Any())
            {
                foreach (var button in headerCustomButtons)
                {
                    buttons.Add(button);
                }
            }
            return buttons;
        }

        /// <summary>
        /// 检查自定义头部操作按钮中是否包含指定标签的操作
        /// </summary>
        /// <param name="customButtons">自定义按钮列表</param>
        /// <param name="label">要检查的标签</param>
        /// <returns>如果存在指定标签的操作则返回true，否则返回false</returns>
        private bool HasCustomHeaderOperationWithLabel(List<JObject> customButtons, string label)
        {
            return customButtons?.Any(btn => 
                btn["label"]?.ToString().Equals(label, StringComparison.OrdinalIgnoreCase) == true) == true;
        }

        /// <summary>
        /// 构建筛选配置对象。
        /// </summary>
        private JObject BuildFilterConfig(IEnumerable<JObject> searchFields)
        {
            return new JObject
            {
                ["title"] = "筛选",
                ["mode"] = "horizontal",
                ["columnCount"] = 4, // 一行最多显示4列
                ["autoFocus"] = false,
                ["body"] = new JArray(searchFields),
                ["actions"] = new JArray  // 添加操作按钮
                {
                    new JObject
                    {
                        ["type"] = "submit",
                        ["label"] = "搜索",
                        ["level"] = "primary"
                    },
                    new JObject
                    {
                        ["type"] = "reset",
                        ["label"] = "重置"
                    }
                }
            };
        }

        /// <summary>
        /// 配置卡片模式
        /// </summary>
        /// <param name="crudConfig">CRUD配置对象</param>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="dataType">数据类型</param>
        private void ConfigureCardMode(JObject crudConfig, Type controllerType, Type dataType)
        {
            var cardAttribute = controllerType.GetCustomAttribute<AmisCardAttribute>();
            if (cardAttribute == null) return;

            // 设置卡片模式
            crudConfig["mode"] = "cards";
            crudConfig["switchPerPage"] = cardAttribute.SwitchPerPage;
            crudConfig["placeholder"] = cardAttribute.Placeholder;
            crudConfig["columnsCount"] = cardAttribute.ColumnsCount;

            var defaultParams = _cardHelper.GetCardModeDefaultParams(cardAttribute);
            crudConfig["defaultParams"] = defaultParams;

            // 生成卡片配置
            var cardConfig = _cardHelper.GenerateCardConfig(controllerType, dataType);
            if (cardConfig != null)
            {
                crudConfig["card"] = cardConfig;
            }
        }

        /// <summary>
        /// 配置表格模式
        /// </summary>
        /// <param name="crudConfig">CRUD配置对象</param>
        /// <param name="columns">列配置</param>
        private void ConfigureTableMode(JObject crudConfig, List<JObject> columns)
        {
            crudConfig["showIndex"] = true;  // 显示索引列
            crudConfig["columns"] = new JArray(columns);  // 设置列
        }

        internal JObject GenerateAmisCrudConfig()
        {
            return GenerateAmisCrudConfig(_amisContext.ControllerName, _amisContext.ControllerType, _amisContext.Actions);
        }

        #endregion
    }
}
