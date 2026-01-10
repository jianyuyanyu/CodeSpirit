using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Amis.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly TabsHelper _tabsHelper;
        private readonly StatisticsCardsHelper _statisticsCardsHelper;
        private readonly ILogger<AmisCRUDConfigBuilder> _logger;

        /// <summary>
        /// 构造函数，初始化所需的助手类。
        /// </summary>
        public AmisCRUDConfigBuilder(ApiRouteHelper apiRouteHelper, ColumnHelper columnHelper, ButtonHelper buttonHelper,
                                 SearchFieldHelper searchFieldHelper, AmisContext amisContext, UtilityHelper utilityHelper, AmisApiHelper amisApiHelper, AsideHelper asideHelper, CardHelper cardHelper, TabsHelper tabsHelper, StatisticsCardsHelper statisticsCardsHelper,
                                 ILogger<AmisCRUDConfigBuilder> logger)
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
            _tabsHelper = tabsHelper;
            _statisticsCardsHelper = statisticsCardsHelper;
            _logger = logger;
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

            // 提前检查是否需要 Tab Count Service（在生成列和按钮之前设置，以便按钮能正确添加 reload 配置）
            Type queryDtoType = _utilityHelper.GetQueryDtoTypeFromMethod(actions.List);
            JObject? countApiConfig = null;
            if (_tabsHelper.ShouldGenerateTabs(queryDtoType))
            {
                countApiConfig = _tabsHelper.CreateCountApiConfig(queryDtoType);
                if (countApiConfig != null)
                {
                    _amisContext.HasTabCountService = true;
                }
            }

            // 获取列配置和搜索字段（必须在 HasTabCountService 设置之后，以便操作按钮能正确添加 reload）
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
            JObject asideConfig = _asideHelper.GenerateAsideConfig(queryDtoType, crudName);

            // 检查是否需要生成Tabs配置
            JObject? tabsConfig = null;
            if (_tabsHelper.ShouldGenerateTabs(queryDtoType))
            {
                tabsConfig = _tabsHelper.GenerateTabsConfig(queryDtoType, crudConfig, crudName);
            }

            // 构建页面配置
            JObject pageConfig = new()
            {
                ["type"] = "page",
                ["title"] = controllerType.GetDisplayName(_utilityHelper),
                ["body"] = new JArray(),
                ["data"] = new JObject()
                {
                    ["ROOT_API"] = _apiRouteHelper.GetRootApi(),
                    ["BASE_API"] = $"{_apiRouteHelper.GetRootApi()}/{_apiRouteHelper.GetRoute().TrimStart('/')}"
                }
            };

            // 构建 Page body 数组
            JArray pageBody = new JArray();
            
            // 检查并添加统计卡片（作为第一个元素）
            var statisticsCards = _statisticsCardsHelper.GenerateStatisticsCardsConfig(
                controllerType, 
                baseRoute);
            
            if (statisticsCards != null)
            {
                pageBody.Add(statisticsCards);
            }

            // 如果有Tabs配置，将Tabs添加到body；否则直接使用CRUD
            if (tabsConfig != null)
            {
                // 如果有CountApi配置，用Service组件包裹Tabs以支持reload刷新Count
                if (countApiConfig != null)
                {
                    JObject serviceConfig = new JObject
                    {
                        ["type"] = "service",
                        ["name"] = "tabCountService",  // 用于CRUD操作后reload刷新Count
                        ["api"] = countApiConfig,
                        ["body"] = tabsConfig
                    };
                    pageBody.Add(serviceConfig);
                }
                else
                {
                    pageBody.Add(tabsConfig);
                }
            }
            else
            {
                pageBody.Add(crudConfig);
            }
            
            pageConfig["body"] = pageBody;

            // 如果有aside配置，添加到页面中
            if (asideConfig != null)
            {
                pageConfig["aside"] = asideConfig;
                
                // 获取 PageAsideAttribute 配置并设置到 page 组件
                var pageAsideAttr = _asideHelper.GetPageAsideAttribute(queryDtoType);
                if (pageAsideAttr != null)
                {
                    // 设置宽度是否可调整
                    pageConfig["asideResizor"] = pageAsideAttr.AsideResizor;
                    
                    // 设置最小宽度(大于0时才设置)
                    if (pageAsideAttr.AsideMinWidth > 0)
                    {
                        pageConfig["asideMinWidth"] = pageAsideAttr.AsideMinWidth;
                    }
                    
                    // 设置最大宽度(大于0时才设置)
                    if (pageAsideAttr.AsideMaxWidth > 0)
                    {
                        pageConfig["asideMaxWidth"] = pageAsideAttr.AsideMaxWidth;
                    }
                    
                    // 设置边栏是否固定
                    pageConfig["asideSticky"] = pageAsideAttr.AsideSticky;
                    
                    // 设置边栏位置
                    pageConfig["asidePosition"] = pageAsideAttr.AsidePosition == AsidePosition.Left ? "left" : "right";
                }
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
            _logger.LogInformation("[BuildHeaderToolbar] 开始构建头部工具栏 - 控制器: {ControllerName}, 卡片模式: {IsCardMode}", 
                _amisContext.ControllerName, isCardMode);

            JArray buttons = ["bulkActions"];
            
            // 获取所有自定义头部按钮，用于检查是否有重复的标准操作
            var headerCustomButtons = _buttonHelper.GetHeaderOperationButtons();
            _logger.LogInformation("[BuildHeaderToolbar] 自定义头部按钮数量: {Count}, 按钮: {Buttons}", 
                headerCustomButtons?.Count ?? 0, 
                string.Join(", ", headerCustomButtons?.Select(b => b["label"]?.ToString()) ?? Array.Empty<string>()));
            
            // 检查是否有自定义的新增操作
            bool hasCustomCreateOperation = HasCustomHeaderOperationWithLabel(headerCustomButtons, "新增") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "添加") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "创建") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "快速创建") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "Add") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "Create") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "Quick Create") ||
                                           (_amisContext.Actions.Create?.GetCustomAttribute<HeaderOperationAttribute>() != null);

            // 检查是否有自定义的导入操作
            bool hasCustomImportOperation = HasCustomHeaderOperationWithLabel(headerCustomButtons, "导入") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "批量导入") ||
                                           HasCustomHeaderOperationWithLabel(headerCustomButtons, "Import") ||
                                           (_amisContext.Actions.Import?.GetCustomAttribute<HeaderOperationAttribute>() != null);

            _logger.LogInformation("[BuildHeaderToolbar] hasCustomCreateOperation: {HasCustomCreate}, hasCustomImportOperation: {HasCustomImport}", 
                hasCustomCreateOperation, hasCustomImportOperation);

            // 记录 Create 方法的特性检查
            if (_amisContext.Actions.Create != null)
            {
                var createOpAttr = _amisContext.Actions.Create.GetCustomAttribute<OperationAttribute>();
                var createHeaderOpAttr = _amisContext.Actions.Create.GetCustomAttribute<HeaderOperationAttribute>();
                _logger.LogInformation("[BuildHeaderToolbar] Create方法特性检查 - 方法名: {MethodName}, OperationAttribute: {HasOpAttr}, HeaderOperationAttribute: {HasHeaderOpAttr}", 
                    _amisContext.Actions.Create.Name, 
                    createOpAttr != null, 
                    createHeaderOpAttr != null);
            }

            // 记录 Import 方法的特性检查
            if (_amisContext.Actions.Import != null)
            {
                var importOpAttr = _amisContext.Actions.Import.GetCustomAttribute<OperationAttribute>();
                var importHeaderOpAttr = _amisContext.Actions.Import.GetCustomAttribute<HeaderOperationAttribute>();
                _logger.LogInformation("[BuildHeaderToolbar] Import方法特性检查 - 方法名: {MethodName}, OperationAttribute: {HasOpAttr}, HeaderOperationAttribute: {HasHeaderOpAttr}", 
                    _amisContext.Actions.Import.Name, 
                    importOpAttr != null, 
                    importHeaderOpAttr != null);
            }

            // 添加新增按钮（如果没有自定义的新增操作）
            if (_amisContext.ApiRoutes.Create != null && _amisContext.Actions.Create != null && !hasCustomCreateOperation)
            {
                // 检查Create方法是否有Operation特性（如果有，说明它是自定义操作，不应该作为标准新增按钮）
                var hasOperationAttribute = _amisContext.Actions.Create.GetCustomAttribute<OperationAttribute>() != null;
                var hasHeaderOperationAttribute = _amisContext.Actions.Create.GetCustomAttribute<HeaderOperationAttribute>() != null;

                _logger.LogInformation("[BuildHeaderToolbar] 标准新增按钮决策 - ApiRoutes.Create存在: {HasRoute}, Actions.Create存在: {HasAction}, hasCustomCreateOperation: {HasCustom}, hasOperationAttribute: {HasOpAttr}, hasHeaderOperationAttribute: {HasHeaderOpAttr}",
                    _amisContext.ApiRoutes.Create != null,
                    _amisContext.Actions.Create != null,
                    hasCustomCreateOperation,
                    hasOperationAttribute,
                    hasHeaderOperationAttribute);

                // 只有当Create方法没有Operation特性且没有HeaderOperation特性时，才添加标准新增按钮
                if (!hasOperationAttribute && !hasHeaderOperationAttribute)
                {
                    var createButton = _buttonHelper.CreateHeaderButton("新增", _amisContext.ApiRoutes.Create, _amisContext.Actions.Create?.GetParameters(), method: _amisContext.Actions.Create);
                    _logger.LogInformation("[BuildHeaderToolbar] ✓ 添加标准'新增'按钮 - 标签: {Label}", createButton["label"]?.ToString());
                    buttons.Add(createButton);
                }
                else
                {
                    _logger.LogWarning("[BuildHeaderToolbar] ✗ 跳过标准'新增'按钮 - hasOperationAttribute: {HasOpAttr}, hasHeaderOperationAttribute: {HasHeaderOpAttr}",
                        hasOperationAttribute, hasHeaderOperationAttribute);
                }
            }
            else
            {
                _logger.LogInformation("[BuildHeaderToolbar] 跳过新增按钮 - ApiRoutes.Create: {HasRoute}, Actions.Create: {HasAction}, hasCustomCreateOperation: {HasCustom}",
                    _amisContext.ApiRoutes.Create != null,
                    _amisContext.Actions.Create != null,
                    hasCustomCreateOperation);
            }

            // 卡片模式不支持导出按钮
            if (!isCardMode)
            {
                // 获取本地化的导出标签
                string exportCurrentPageLabel = _buttonHelper.GetLocalizedText("Common.ExportCurrentPage", ButtonHelper.GetSharedResourcesType(), "导出当前页");
                _logger.LogInformation("[BuildHeaderToolbar] 添加'导出当前页'按钮 - 标签: {Label}", exportCurrentPageLabel);
                
                buttons.Add(new JObject()
                {
                    ["type"] = "export-excel",
                    ["label"] = exportCurrentPageLabel,
                    //["filename"] = ""
                });

                if (_amisContext.Actions.Export != null)
                {
                    // 获取本地化的导出全部标签
                    string exportAllLabel = _buttonHelper.GetLocalizedText("Common.ExportAll", ButtonHelper.GetSharedResourcesType(), "导出全部");
                    _logger.LogInformation("[BuildHeaderToolbar] 添加'导出全部'按钮 - 标签: {Label}", exportAllLabel);
                    
                    buttons.Add(new JObject()
                    {
                        ["type"] = "export-excel",
                        ["label"] = exportAllLabel,
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
                var importButton = _buttonHelper.CreateHeaderButton("导入", _amisContext.ApiRoutes.Import, _amisContext.Actions.Import?.GetParameters(), size: "lg", method: _amisContext.Actions.Import);
                _logger.LogInformation("[BuildHeaderToolbar] ✓ 添加标准'导入'按钮 - 标签: {Label}", importButton["label"]?.ToString());
                buttons.Add(importButton);
            }
            else
            {
                _logger.LogInformation("[BuildHeaderToolbar] 跳过导入按钮 - ApiRoutes.Import: {HasRoute}, Actions.Import: {HasAction}, hasCustomImportOperation: {HasCustom}",
                    _amisContext.ApiRoutes.Import != null,
                    _amisContext.Actions.Import != null,
                    hasCustomImportOperation);
            }

            // 添加自定义顶部按钮
            if (headerCustomButtons != null && headerCustomButtons.Any())
            {
                _logger.LogInformation("[BuildHeaderToolbar] 添加 {Count} 个自定义按钮", headerCustomButtons.Count);
                foreach (var button in headerCustomButtons)
                {
                    _logger.LogInformation("[BuildHeaderToolbar]  - 添加自定义按钮: {Label}", button["label"]?.ToString());
                    buttons.Add(button);
                }
            }
            else
            {
                _logger.LogInformation("[BuildHeaderToolbar] 没有自定义按钮需要添加");
            }

            // 详细输出所有按钮信息
            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn.Type == JTokenType.Object)
                {
                    _logger.LogInformation("[BuildHeaderToolbar] 按钮[{Index}] - 类型: {Type}, 标签: {Label}",
                        i,
                        btn["type"]?.ToString(),
                        btn["label"]?.ToString());
                }
                else
                {
                    _logger.LogInformation("[BuildHeaderToolbar] 按钮[{Index}] - 类型: {Type}, 值: {Value}",
                        i,
                        btn.Type,
                        btn.ToString());
                }
            }

            _logger.LogInformation("[BuildHeaderToolbar] ✓ 构建完成 - 总按钮数量: {TotalCount}, 按钮列表: [{Labels}]",
                buttons.Count,
                string.Join(", ", buttons.Where(b => b.Type == JTokenType.Object && b["label"] != null).Select(b => b["label"].ToString())));

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
            // 获取本地化文本
            string filterLabel = _buttonHelper.GetLocalizedText("Common.Filter", ButtonHelper.GetSharedResourcesType(), "筛选");
            string searchLabel = _buttonHelper.GetLocalizedText("Common.Search", ButtonHelper.GetSharedResourcesType(), "搜索");
            string resetLabel = _buttonHelper.GetLocalizedText("Common.Reset", ButtonHelper.GetSharedResourcesType(), "重置");
            
            return new JObject
            {
                ["title"] = filterLabel,
                ["mode"] = "horizontal",
                ["columnCount"] = 4, // 一行最多显示4列
                ["autoFocus"] = false,
                ["body"] = new JArray(searchFields),
                ["actions"] = new JArray  // 添加操作按钮
                {
                    new JObject
                    {
                        ["type"] = "submit",
                        ["label"] = searchLabel,
                        ["level"] = "primary"
                    },
                    new JObject
                    {
                        ["type"] = "reset",
                        ["label"] = resetLabel
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
