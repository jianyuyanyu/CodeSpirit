using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Reflection;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// 负责生成 AMIS CRUD 配置的构建器。
    /// </summary>
    public class AmisConfigBuilder
    {
        // 依赖注入的助手类
        private readonly ApiRouteHelper _apiRouteHelper;
        private readonly ColumnHelper _columnHelper;
        private readonly ButtonHelper _buttonHelper;
        private readonly SearchFieldHelper _searchFieldHelper;
        private readonly AmisContext amisContext;
        private readonly UtilityHelper utilityHelper;
        private readonly AmisApiHelper amisApiHelper;

        /// <summary>
        /// 构造函数，初始化所需的助手类。
        /// </summary>
        public AmisConfigBuilder(ApiRouteHelper apiRouteHelper, ColumnHelper columnHelper, ButtonHelper buttonHelper,
                                 SearchFieldHelper searchFieldHelper, AmisContext amisContext, UtilityHelper utilityHelper, AmisApiHelper amisApiHelper)
        {
            _apiRouteHelper = apiRouteHelper;
            _columnHelper = columnHelper;
            _buttonHelper = buttonHelper;
            _searchFieldHelper = searchFieldHelper;
            this.amisContext = amisContext;
            this.utilityHelper = utilityHelper;
            this.amisApiHelper = amisApiHelper;
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
            amisContext.BaseRoute = baseRoute;

            ApiRoutesInfo apiRoutes = _apiRouteHelper.GetApiRoutes();
            amisContext.ApiRoutes = apiRoutes;

            // 获取读取数据的类型，如果类型为空，则返回空
            Type dataType = utilityHelper.GetDataTypeFromMethod(actions.List);
            if (dataType == null)
            {
                return null;
            }

            amisContext.ListDataType = dataType;

            // 获取列配置和搜索字段
            List<JObject> columns = _columnHelper.GetAmisColumns();
            List<JObject> searchFields = _searchFieldHelper.GetAmisSearchFields(actions.List);

            // 构建 CRUD 配置
            JObject crudConfig = new()
            {
                ["type"] = "crud",  // 设置类型为 CRUD
                ["name"] = $"{controllerName.ToLower()}Crud",  // 设置配置名称
                ["showIndex"] = true,  // 显示索引列
                ["api"] = amisApiHelper.CreateApi(apiRoutes.Read),  // 设置 API 配置
                ["quickSaveApi"] = amisApiHelper.CreateApi(apiRoutes.QuickSave),
                ["columns"] = new JArray(columns),  // 设置列
                ["headerToolbar"] = BuildHeaderToolbar(),  // 设置头部工具栏
                ["bulkActions"] = new JArray(_buttonHelper.GetBulkOperationButtons()), //设置批量操作
                ["footerToolbar"] = new JArray()
                {
                    "switch-per-page",
                    "pagination",
                    "statistics"
                }
            };

            // 如果有搜索字段，加入筛选配置
            if (searchFields.Any())
            {
                crudConfig["filter"] = BuildFilterConfig(searchFields);
            }

            // 构建页面配置
            JObject pageConfig = new()
            {
                ["type"] = "page",  // 设置页面类型
                ["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",  // 设置页面标题
                ["body"] = new JArray()
                {
                    //new JObject {
                    //    ["type"] = "chart",
                    //    ["api"] = "${API_HOST}/api/userStatistics/usergrowth-and-active-users"
                    //},
                    crudConfig
                },
                ["data"] = new JObject()
                {
                    ["ROOT_API"] = "https://localhost:62144"
                }
            };

            return pageConfig;
        }

        #region 辅助方法

        /// <summary>
        /// 构建头部工具栏配置。
        /// </summary>
        private JArray BuildHeaderToolbar()
        {
            JArray buttons = ["bulkActions"];
            if (amisContext.ApiRoutes.Create != null && amisContext.Actions.Create != null)
            {
                buttons.Add(_buttonHelper.CreateHeaderButton("新增", amisContext.ApiRoutes.Create, amisContext.Actions.Create?.GetParameters()));
            }
            buttons.Add(new JObject()
            {
                ["type"] = "export-excel",
                ["label"] = "导出当前页",
                //["filename"] = ""
            });

            if (amisContext.Actions.Export != null)
            {
                buttons.Add(new JObject()
                {
                    ["type"] = "export-excel",
                    ["label"] = "导出全部",
                    ["api"] = new JObject
                    {
                        ["url"] = amisContext.ApiRoutes.Export.ApiPath,
                        ["method"] = amisContext.ApiRoutes.Export.HttpMethod
                    },
                });
            }
            if (amisContext.ApiRoutes.Import != null && amisContext.Actions.Import != null)
            {
                buttons.Add(_buttonHelper.CreateHeaderButton("导入", amisContext.ApiRoutes.Import, amisContext.Actions.Import?.GetParameters(), size: "lg"));
            }
            return buttons;
        }

        /// <summary>
        /// 构建筛选配置对象。
        /// </summary>
        private JObject BuildFilterConfig(IEnumerable<JObject> searchFields)
        {
            return new JObject
            {
                ["title"] = "筛选",  // 筛选标题
                ["body"] = new JObject
                {
                    ["type"] = "group",  // 筛选类型为组合
                    ["body"] = new JArray(searchFields)  // 添加搜索字段
                }
            };
        }

        internal JObject GenerateAmisCrudConfig()
        {
            return GenerateAmisCrudConfig(amisContext.ControllerName, amisContext.ControllerType, amisContext.Actions);
        }

        #endregion
    }
}
