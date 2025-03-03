using CodeSpirit.Amis.Column;
using CodeSpirit.Amis.Helpers;
using CodeSpirit.Amis.Helpers.Dtos;
using Microsoft.AspNetCore.Http;
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
        private readonly AmisContext _amisContext;
        private readonly UtilityHelper _utilityHelper;
        private readonly AmisApiHelper _amisApiHelper;

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
            this._amisContext = amisContext;
            this._utilityHelper = utilityHelper;
            this._amisApiHelper = amisApiHelper;
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

            // 获取列配置和搜索字段
            List<JObject> columns = _columnHelper.GetAmisColumns();
            List<JObject> searchFields = _searchFieldHelper.GetAmisSearchFields(actions.List);

            // 构建 CRUD 配置
            JObject crudConfig = new()
            {
                ["type"] = "crud",  // 设置类型为 CRUD
                ["name"] = $"{controllerName.ToLower()}Crud",  // 设置配置名称
                ["showIndex"] = true,  // 显示索引列
                ["api"] = _amisApiHelper.CreateApi(apiRoutes.Read),  // 设置 API 配置
                ["quickSaveApi"] = _amisApiHelper.CreateApi(apiRoutes.QuickSave),
                ["columns"] = new JArray(columns),  // 设置列
                ["headerToolbar"] = BuildHeaderToolbar(),  // 设置头部工具栏
                ["bulkActions"] = new JArray(_buttonHelper.GetBulkOperationButtons()), //设置批量操作
            };

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
                    ["ROOT_API"] = _apiRouteHelper.GetRootApi()
                }
            };

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
        private JArray BuildHeaderToolbar()
        {
            JArray buttons = ["bulkActions"];
            if (_amisContext.ApiRoutes.Create != null && _amisContext.Actions.Create != null)
            {
                buttons.Add(_buttonHelper.CreateHeaderButton("新增", _amisContext.ApiRoutes.Create, _amisContext.Actions.Create?.GetParameters()));
            }
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
            if (_amisContext.ApiRoutes.Import != null && _amisContext.Actions.Import != null)
            {
                buttons.Add(_buttonHelper.CreateHeaderButton("导入", _amisContext.ApiRoutes.Import, _amisContext.Actions.Import?.GetParameters(), size: "lg"));
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
            return GenerateAmisCrudConfig(_amisContext.ControllerName, _amisContext.ControllerType, _amisContext.Actions);
        }

        #endregion
    }
}
