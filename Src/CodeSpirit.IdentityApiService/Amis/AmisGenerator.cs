using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using CodeSpirit.IdentityApi.Authorization;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.IdentityApi.Amis
{
    /// <summary>
    /// 用于生成 AMIS（阿里云前端框架）所需的 JSON 配置的生成器类。
    /// </summary>
    public partial class AmisGenerator
    {
        private readonly Assembly _assembly;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermissionService _permissionService;
        private readonly IMemoryCache _cache;

        /// <summary>
        /// 定义排除的分页和排序参数名称列表。
        /// </summary>
        private static readonly HashSet<string> ExcludedQueryParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "page",
            "pageSize",
            "limit",
            "offset",
            "perPage",
            "sort",
            "order",
            "orderBy",
            "orderDir",
            "sortBy",
            "sortOrder"
        };

        /// <summary>
        /// 构造函数，初始化依赖项。
        /// </summary>
        /// <param name="assembly">包含控制器的程序集。</param>
        /// <param name="httpContextAccessor">用于访问当前 HTTP 上下文。</param>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="cache">内存缓存，用于缓存生成的 AMIS JSON。</param>
        public AmisGenerator(Assembly assembly, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, IMemoryCache cache)
        {
            _assembly = assembly;
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
            _cache = cache;
        }

        /// <summary>
        /// 生成指定控制器的 AMIS JSON 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 "Controller" 后缀）。</param>
        /// <returns>AMIS 定义的 JSON 对象，如果控制器不存在或不支持则返回 null。</returns>
        public JObject GenerateAmisJsonForController(string controllerName)
        {
            var cacheKey = GenerateCacheKey(controllerName);
            if (_cache.TryGetValue(cacheKey, out JObject cachedAmisJson))
            {
                return cachedAmisJson;
            }

            var controllerType = GetControllerType(controllerName);
            if (controllerType == null)
                return null;

            if (!HasCrudActions(controllerType, out var actions))
                return null;

            var crudConfig = GenerateAmisCrudConfig(controllerName, controllerType, actions);
            if (crudConfig != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(cacheKey, crudConfig, cacheEntryOptions);
            }

            return crudConfig;
        }

        #region 缓存键生成

        /// <summary>
        /// 生成用于缓存的键值。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <returns>生成的缓存键。</returns>
        private string GenerateCacheKey(string controllerName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var permissionsHash = GetUserPermissionsHash(user);
            return $"AmisJson_{controllerName.ToLower()}_{permissionsHash.GetHashCode()}";
        }

        /// <summary>
        /// 获取当前用户的权限哈希值。
        /// </summary>
        /// <param name="user">当前用户的 ClaimsPrincipal。</param>
        /// <returns>权限集合的哈希字符串。</returns>
        private string GetUserPermissionsHash(System.Security.Claims.ClaimsPrincipal user)
        {
            var userPermissions = user?.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .OrderBy(p => p)
                .ToList() ?? new List<string>();

            return string.Join(",", userPermissions);
        }

        #endregion

        #region 控制器获取

        /// <summary>
        /// 获取指定名称的控制器类型。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 "Controller" 后缀）。</param>
        /// <returns>控制器的 Type 对象，如果未找到则返回 null。</returns>
        private Type GetControllerType(string controllerName)
        {
            return _assembly.GetTypes()
                            .FirstOrDefault(t => IsValidController(t, controllerName));
        }

        /// <summary>
        /// 判断给定的 Type 是否为有效的控制器。
        /// </summary>
        /// <param name="type">要检查的 Type。</param>
        /// <param name="controllerName">控制器名称。</param>
        /// <returns>如果是有效控制器则返回 true，否则返回 false。</returns>
        private bool IsValidController(Type type, string controllerName)
        {
            return type.IsClass
                && !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && type.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取控制器的基本路由。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>基本路由字符串。</returns>
        private string GetRoute(Type controller)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", GetControllerName(controller)) ?? string.Empty;
        }

        /// <summary>
        /// 从控制器类型中提取控制器名称（不含 "Controller" 后缀）。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>控制器名称。</returns>
        private string GetControllerName(Type controller)
        {
            return controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region CRUD 操作检查

        /// <summary>
        /// 检查控制器是否包含所有 CRUD 操作，并获取这些操作的方法信息。
        /// 优先根据命名约定提取 CRUD 操作。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <param name="actions">输出参数，包含 CRUD 操作的方法信息。</param>
        /// <returns>如果包含所有 CRUD 操作则返回 true，否则返回 false。</returns>
        private bool HasCrudActions(Type controller, out CrudActions actions)
        {
            actions = new CrudActions();

            var methods = GetControllerMethods(controller);

            // 根据命名约定提取 CRUD 操作
            actions.Create = methods.FirstOrDefault(m => IsCreateMethod(m));
            actions.Read = methods.FirstOrDefault(m => IsReadMethod(m));
            actions.Update = methods.FirstOrDefault(m => IsUpdateMethod(m));
            actions.Delete = methods.FirstOrDefault(m => IsDeleteMethod(m));

            return actions.Create != null && actions.Read != null && actions.Update != null && actions.Delete != null;
        }

        /// <summary>
        /// 获取控制器中所有公开的实例方法，排除继承的方法。
        /// </summary>
        /// <param name="controller">控制器的 Type 对象。</param>
        /// <returns>方法信息的集合。</returns>
        private IEnumerable<MethodInfo> GetControllerMethods(Type controller)
        {
            return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        /// <summary>
        /// 判断方法是否为 Create 操作，根据命名约定（以 "Create" 或 "Add" 开头）。
        /// </summary>
        /// <param name="method">方法的信息。</param>
        /// <returns>如果是 Create 操作则返回 true，否则返回 false。</returns>
        private bool IsCreateMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Add", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断方法是否为 Read 操作，根据命名约定（以 "Get" 开头）。
        /// </summary>
        /// <param name="method">方法的信息。</param>
        /// <returns>如果是 Read 操作则返回 true，否则返回 false。</returns>
        private bool IsReadMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断方法是否为 Update 操作，根据命名约定（以 "Update" 或 "Modify" 开头）。
        /// </summary>
        /// <param name="method">方法的信息。</param>
        /// <returns>如果是 Update 操作则返回 true，否则返回 false。</returns>
        private bool IsUpdateMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Modify", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断方法是否为 Delete 操作，根据命名约定（以 "Delete" 或 "Remove" 开头）。
        /// </summary>
        /// <param name="method">方法的信息。</param>
        /// <returns>如果是 Delete 操作则返回 true，否则返回 false。</returns>
        private bool IsDeleteMethod(MethodInfo method)
        {
            return method.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                   method.Name.StartsWith("Remove", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region AMIS CRUD 配置生成

        /// <summary>
        /// 生成 AMIS CRUD 配置的 JSON 对象。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="controllerType">控制器的 Type 对象。</param>
        /// <param name="actions">控制器的 CRUD 操作方法信息。</param>
        /// <returns>AMIS CRUD 配置的 JSON 对象，如果生成失败则返回 null。</returns>
        private JObject GenerateAmisCrudConfig(string controllerName, Type controllerType, CrudActions actions)
        {
            var baseRoute = GetRoute(controllerType);
            var apiRoutes = GetApiRoutes(baseRoute, actions);
            var dataType = GetDataTypeFromAction(actions.Read);
            if (dataType == null)
                return null;

            var columns = GetAmisColumns(dataType, controllerName, apiRoutes);
            var searchFields = GetAmisSearchFields(actions.Read);

            var crud = new JObject
            {
                ["type"] = "crud",
                ["name"] = $"{controllerName.ToLower()}Crud",
                ["api"] = new JObject
                {
                    ["url"] = apiRoutes.ReadRoute,
                    ["method"] = "get"
                },
                ["columns"] = new JArray(columns),
                ["createApi"] = new JObject
                {
                    ["url"] = apiRoutes.CreateRoute,
                    ["method"] = "post"
                },
                ["updateApi"] = new JObject
                {
                    ["url"] = apiRoutes.UpdateRoute,
                    ["method"] = "put"
                },
                ["deleteApi"] = new JObject
                {
                    ["url"] = apiRoutes.DeleteRoute,
                    ["method"] = "delete"
                },
                //["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",
                ["headerToolbar"] = new JArray
                {
                    CreateHeaderButton(apiRoutes.CreateRoute, actions.Create?.GetParameters())
                }
            };

            // 添加搜索字段（如果有）
            if (searchFields.Any())
            {
                crud["filter"] = new JObject
                {
                    ["title"] = "筛选",
                    ["body"] = new JArray(searchFields)
                };
            }

            // 创建 Page 组件并将 CRUD 组件作为其子组件
            var page = new JObject
            {
                ["type"] = "page",
                ["title"] = controllerType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? $"{controllerName} 管理",
                ["body"] = crud
            };

            return page;
        }

        /// <summary>
        /// 获取 CRUD 操作对应的 API 路由。
        /// </summary>
        /// <param name="baseRoute">控制器的基本路由。</param>
        /// <param name="actions">控制器的 CRUD 操作方法信息。</param>
        /// <returns>包含各 CRUD 操作路由的元组。</returns>
        private (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute) GetApiRoutes(string baseRoute, CrudActions actions)
        {
            string Combine(string template) => BuildAbsoluteUrl(CombineRoutes(baseRoute, template));

            var createRouteTemplate = GetRouteTemplate(actions.Create, "POST");
            var readRouteTemplate = GetRouteTemplate(actions.Read, "GET");
            var updateRouteTemplate = GetRouteTemplate(actions.Update, "PUT");
            var deleteRouteTemplate = GetRouteTemplate(actions.Delete, "DELETE");

            return (
                Combine(createRouteTemplate),
                Combine(readRouteTemplate),
                Combine(updateRouteTemplate),
                Combine(deleteRouteTemplate)
            );
        }

        /// <summary>
        /// 获取指定 HTTP 方法的路由模板。
        /// </summary>
        /// <param name="method">方法的信息。</param>
        /// <param name="httpMethod">HTTP 方法类型（如 "GET", "POST"）。</param>
        /// <returns>路由模板字符串，如果未定义则返回空字符串。</returns>
        private string GetRouteTemplate(MethodInfo method, string httpMethod)
        {
            if (method == null)
                return string.Empty;

            var attribute = method.GetCustomAttributes()
                                  .OfType<HttpMethodAttribute>()
                                  .FirstOrDefault(a => a.HttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase));
            return attribute?.Template ?? string.Empty;
        }

        /// <summary>
        /// 从控制器操作方法中提取数据类型。
        /// </summary>
        /// <param name="readMethod">读取数据的控制器方法信息。</param>
        /// <returns>数据类型的 Type 对象，如果提取失败则返回 null。</returns>
        private Type GetDataTypeFromAction(MethodInfo readMethod)
        {
            if (readMethod == null)
                return null;

            var returnType = readMethod.ReturnType;
            return ExtractDataType(GetUnderlyingType(returnType));
        }

        /// <summary>
        /// 获取方法返回类型的底层类型，处理异步和包装类型。
        /// </summary>
        /// <param name="type">方法的返回类型。</param>
        /// <returns>底层数据类型的 Type 对象，如果未找到则返回 null。</returns>
        private Type GetUnderlyingType(Type type)
        {
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ActionResult<>))
                {
                    return type.GetGenericArguments()[0];
                }
                if (genericDef == typeof(Task<>))
                {
                    var taskInnerType = type.GetGenericArguments()[0];
                    if (taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                    {
                        return taskInnerType.GetGenericArguments()[0];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 从包装类型中提取实际的数据类型。
        /// </summary>
        /// <param name="type">包装类型的 Type 对象。</param>
        /// <returns>实际的数据类型的 Type 对象，如果未找到则返回 null。</returns>
        private Type ExtractDataType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(ApiResponse<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(ListData<>))
                    {
                        return innerType.GetGenericArguments()[0];
                    }
                    return innerType;
                }
                if (genericDef == typeof(ListData<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return type;
        }

        #endregion

        #region AMIS 表格列和字段

        /// <summary>
        /// 生成 AMIS 表格的列配置。
        /// </summary>
        /// <param name="dataType">数据类型的 Type 对象。</param>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="apiRoutes">包含 CRUD 操作路由的元组。</param>
        /// <returns>AMIS 表格列的列表。</returns>
        private List<JObject> GetAmisColumns(Type dataType, string controllerName, (string CreateRoute, string ReadRoute, string UpdateRoute, string DeleteRoute) apiRoutes)
        {
            var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = properties
                .Where(p => !IsIgnoredProperty(p) && HasViewPermission(p))
                .Select(p => CreateAmisColumn(p))
                .ToList();

            // 创建操作列（如编辑、删除按钮）
            var operations = CreateOperationsColumn(controllerName, dataType, apiRoutes.UpdateRoute, apiRoutes.DeleteRoute);
            if (operations != null)
            {
                columns.Add(operations);
            }

            return columns;
        }

        /// <summary>
        /// 判断属性是否应被忽略（例如密码字段或 Id 字段）。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果应忽略则返回 true，否则返回 false。</returns>
        private bool IsIgnoredProperty(PropertyInfo prop)
        {
            return prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)
                || prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
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
            var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? ToTitleCase(prop.Name);
            var fieldName = ToCamelCase(prop.Name);

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

            return column;
        }

        /// <summary>
        /// 根据属性类型确定 AMIS 列的显示类型。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 列的类型字符串。</returns>
        private string GetColumnType(PropertyInfo prop)
        {
            return prop.PropertyType switch
            {
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
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

        #endregion

        #region AMIS CRUD 配置生成

        /// <summary>
        /// 创建 AMIS 表头的“新增”按钮配置。
        /// </summary>
        /// <param name="createRoute">新增操作的 API 路由。</param>
        /// <param name="createParameters">新增操作的方法参数。</param>
        /// <returns>AMIS 按钮的 JSON 对象。</returns>
        private JObject CreateHeaderButton(string createRoute, IEnumerable<ParameterInfo> createParameters)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "新增",
                ["level"] = "primary",
                ["actionType"] = "dialog",
                ["dialog"] = new JObject
                {
                    ["title"] = "新增",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["api"] = new JObject
                        {
                            ["url"] = createRoute,
                            ["method"] = "post"
                        },
                        ["body"] = new JArray(GetAmisFormFieldsFromParameters(createParameters))
                    }
                }
            };
        }

        #endregion

        #region AMIS 操作列

        /// <summary>
        /// 创建 AMIS 表格的操作列，包括编辑和删除按钮。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="dataType">数据类型，用于生成表单字段。</param>
        /// <param name="updateRoute">更新操作的 API 路由。</param>
        /// <param name="deleteRoute">删除操作的 API 路由。</param>
        /// <returns>AMIS 操作列的 JSON 对象，如果没有按钮则返回 null。</returns>
        private JObject CreateOperationsColumn(string controllerName, Type dataType, string updateRoute, string deleteRoute)
        {
            var buttons = new JArray();

            // 如果用户有编辑权限，则添加编辑按钮
            if (_permissionService.HasPermission($"{controllerName}Edit"))
            {
                buttons.Add(CreateEditButton(controllerName, updateRoute, dataType));
            }

            // 如果用户有删除权限，则添加删除按钮
            if (_permissionService.HasPermission($"{controllerName}Delete"))
            {
                buttons.Add(CreateDeleteButton(deleteRoute));
            }

            // 添加自定义操作按钮
            var customButtons = GetCustomOperationsButtons(controllerName, dataType, updateRoute, deleteRoute);
            foreach (var btn in customButtons)
            {
                buttons.Add(btn);
            }

            // 如果没有任何按钮，则不添加操作列
            if (buttons.Count == 0)
                return null;

            return new JObject
            {
                ["label"] = "操作",
                ["type"] = "operation",
                ["buttons"] = buttons
            };
        }

        /// <summary>
        /// 创建编辑按钮的 AMIS 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="updateRoute">编辑操作的 API 路由。</param>
        /// <param name="dataType">数据类型，用于生成表单字段。</param>
        /// <returns>编辑按钮的 JSON 对象。</returns>
        private JObject CreateEditButton(string controllerName, string updateRoute, Type dataType)
        {
            // 查找更新方法以获取其参数
            var updateMethod = _assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase))
                ?.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => IsUpdateMethod(m));

            var updateParameters = updateMethod?.GetParameters() ?? Enumerable.Empty<ParameterInfo>();

            return new JObject
            {
                ["type"] = "button",
                ["label"] = "编辑",
                ["actionType"] = "drawer",
                ["drawer"] = new JObject
                {
                    ["title"] = "编辑",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["api"] = new JObject
                        {
                            ["url"] = updateRoute,
                            ["method"] = "put"
                        },
                        ["body"] = new JArray(GetAmisFormFieldsFromParameters(updateParameters))
                    }
                }
            };
        }

        /// <summary>
        /// 创建删除按钮的 AMIS 配置。
        /// </summary>
        /// <param name="deleteRoute">删除操作的 API 路由。</param>
        /// <returns>删除按钮的 JSON 对象。</returns>
        private JObject CreateDeleteButton(string deleteRoute)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "删除",
                ["actionType"] = "ajax",
                ["confirmText"] = "确定要删除吗？",
                ["api"] = new JObject
                {
                    ["url"] = deleteRoute,
                    ["method"] = "delete"
                }
            };
        }

        /// <summary>
        /// 获取控制器中所有自定义操作按钮的配置。
        /// </summary>
        /// <param name="controllerName">控制器名称。</param>
        /// <param name="dataType">数据类型，用于生成表单字段。</param>
        /// <param name="updateRoute">更新操作的 API 路由。</param>
        /// <param name="deleteRoute">删除操作的 API 路由。</param>
        /// <returns>自定义操作按钮的列表。</returns>
        private List<JObject> GetCustomOperationsButtons(string controllerName, Type dataType, string updateRoute, string deleteRoute)
        {
            var buttons = new List<JObject>();
            var controllerType = GetControllerType(controllerName);
            if (controllerType == null)
                return buttons;

            // 查找控制器中标记了 [Operation] 特性的操作方法
            var operationMethods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                 .Where(m => m.GetCustomAttributes<OperationAttribute>().Any());

            foreach (var method in operationMethods)
            {
                var operationAttrs = method.GetCustomAttributes<OperationAttribute>();
                foreach (var op in operationAttrs)
                {
                    buttons.Add(CreateCustomOperationButton(op));
                }
            }

            return buttons;
        }

        /// <summary>
        /// 创建自定义操作按钮的 AMIS 配置。
        /// </summary>
        /// <param name="op">操作属性的信息。</param>
        /// <returns>自定义操作按钮的 JSON 对象。</returns>
        private JObject CreateCustomOperationButton(OperationAttribute op)
        {
            var button = new JObject
            {
                ["type"] = "button",
                ["label"] = op.Label,
                ["actionType"] = op.ActionType
            };

            // 如果定义了 API，则添加 API 配置
            if (!string.IsNullOrEmpty(op.Api))
            {
                button["api"] = new JObject
                {
                    ["url"] = op.Api,
                    ["method"] = op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase) ? "get" : "post"
                };
            }

            // 如果定义了确认文本，则添加确认提示
            if (!string.IsNullOrEmpty(op.ConfirmText))
            {
                button["confirmText"] = op.ConfirmText;
            }

            // 如果操作类型为下载，则添加下载配置
            if (op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                button["download"] = true;
            }

            return button;
        }

        #endregion

        #region AMIS 搜索字段

        /// <summary>
        /// 生成 AMIS 表格的搜索字段配置。
        /// </summary>
        /// <param name="readMethod">获取列表数据的控制器方法信息。</param>
        /// <returns>AMIS 搜索字段的列表。</returns>
        private List<JObject> GetAmisSearchFields(MethodInfo readMethod)
        {
            if (readMethod == null)
                return new List<JObject>();

            var parameters = readMethod.GetParameters();
            var searchFields = new List<JObject>();

            foreach (var param in parameters.Where(p => p.GetCustomAttribute<FromQueryAttribute>() != null))
            {
                // 排除分页和排序参数
                if (IsExcludedParameter(param.Name))
                    continue;

                // 检查用户是否有权限使用该搜索字段
                if (!HasSearchPermission(param))
                    continue;

                // 创建搜索字段的 AMIS 配置
                searchFields.AddRange(CreateSearchFieldsFromParameter(param));
            }

            return searchFields;
        }

        /// <summary>
        /// 判断参数是否应被排除（如分页和排序参数）。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>如果应排除则返回 true，否则返回 false。</returns>
        private bool IsExcludedParameter(string param)
        {
            return ExcludedQueryParameters.Contains(param, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 创建搜索字段的 AMIS 配置，从参数中提取。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>搜索字段的 JSON 对象列表。</returns>
        private List<JObject> CreateSearchFieldsFromParameter(ParameterInfo param)
        {
            var fields = new List<JObject>();

            if (IsSimpleType(param.ParameterType))
            {
                fields.Add(CreateSearchField(param));
            }
            else if (IsComplexType(param.ParameterType))
            {
                var properties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    // 排除分页和排序参数
                    if (IsExcludedParameter(prop.Name))
                        continue;

                    // 检查用户是否有权限使用该搜索字段
                    if (!HasSearchPermission(prop))
                        continue;

                    fields.Add(CreateSearchFieldFromProperty(prop, param.Name));
                }
            }

            return fields;
        }

        /// <summary>
        /// 创建单个搜索字段的 AMIS 配置基于参数。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>搜索字段的 JSON 对象。</returns>
        private JObject CreateSearchField(ParameterInfo param)
        {
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? ToTitleCase(param.Name);
            var fieldName = ToCamelCase(param.Name);
            var fieldType = DetermineSearchFieldType(param.ParameterType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            // 如果是枚举类型，则添加枚举选项
            if (fieldType == "select" && (param.ParameterType.IsEnum || IsNullableEnum(param.ParameterType)))
            {
                field["options"] = GetEnumOptions(param.ParameterType);
            }

            // 如果是日期类型，则添加日期格式
            if (fieldType == "date")
            {
                field["format"] = "YYYY-MM-DD";
            }

            return field;
        }

        /// <summary>
        /// 创建单个搜索字段的 AMIS 配置基于属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <param name="parentName">父参数名称（用于嵌套字段名）。</param>
        /// <returns>搜索字段的 JSON 对象。</returns>
        private JObject CreateSearchFieldFromProperty(PropertyInfo prop, string parentName)
        {
            var label = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? ToTitleCase(prop.Name);
            var fieldName = ToCamelCase($"{parentName}.{prop.Name}");
            var fieldType = DetermineSearchFieldType(prop.PropertyType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["type"] = fieldType
            };

            // 如果是枚举类型，则添加枚举选项
            if (fieldType == "select" && (prop.PropertyType.IsEnum || IsNullableEnum(prop.PropertyType)))
            {
                field["options"] = GetEnumOptions(prop.PropertyType);
            }

            // 如果是日期类型，则添加日期格式
            if (fieldType == "date")
            {
                field["format"] = "YYYY-MM-DD";
            }

            return field;
        }

        /// <summary>
        /// 确定搜索字段的类型，根据参数的类型进行映射。
        /// </summary>
        /// <param name="type">参数的类型。</param>
        /// <returns>AMIS 搜索字段的类型字符串。</returns>
        private string DetermineSearchFieldType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return "input-number";
            if (type == typeof(bool) || type == typeof(bool?))
                return "switch";
            if (type.IsEnum || IsNullableEnum(type))
                return "select";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "date";

            return "input-text";
        }

        /// <summary>
        /// 判断当前用户是否有权限使用指定的搜索参数。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasSearchPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 判断当前用户是否有权限使用指定的搜索属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasSearchPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 获取枚举类型的选项列表，用于 AMIS 的下拉选择框。
        /// </summary>
        /// <param name="type">枚举类型或可空枚举类型。</param>
        /// <returns>AMIS 枚举选项的 JSON 数组。</returns>
        private JArray GetEnumOptions(Type type)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            var enumOptions = Enum.GetValues(enumType).Cast<object>().Select(e => new JObject
            {
                ["label"] = e.ToString(),
                ["value"] = e.ToString()
            });

            return new JArray(enumOptions);
        }

        #endregion

        #region AMIS 表单字段生成

        /// <summary>
        /// 生成 AMIS 表单的字段配置基于方法参数。
        /// </summary>
        /// <param name="parameters">方法的参数信息。</param>
        /// <returns>AMIS 表单字段的列表。</returns>
        private List<JObject> GetAmisFormFieldsFromParameters(IEnumerable<ParameterInfo> parameters)
        {
            var fields = new List<JObject>();

            foreach (var param in parameters)
            {
                // 检查用户是否有权限编辑该参数
                if (!HasEditPermission(param))
                    continue;

                if (IsSimpleType(param.ParameterType))
                {
                    if (!IsIgnoredParameter(param))
                    {
                        fields.Add(CreateAmisFormField(param));
                    }
                }
                else if (IsComplexType(param.ParameterType))
                {
                    // 如果参数是复杂类型（类），则解析其属性
                    var nestedProperties = param.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in nestedProperties)
                    {
                        if (!IsIgnoredProperty(prop) && HasEditPermission(prop))
                        {
                            fields.Add(CreateAmisFormFieldFromProperty(prop, param.Name));
                        }
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// 判断参数是否应被忽略（例如 Id 参数）。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>如果应忽略则返回 true，否则返回 false。</returns>
        private bool IsIgnoredParameter(ParameterInfo param)
        {
            return param.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑指定的参数。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasEditPermission(ParameterInfo param)
        {
            var permissionAttr = param.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 创建单个表单字段的 AMIS 配置基于参数。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <returns>表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormField(ParameterInfo param)
        {
            var label = param.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? ToTitleCase(param.Name);
            var fieldName = ToCamelCase(param.Name);
            var isRequired = !IsNullable(param.ParameterType);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(param.ParameterType)
            };

            // 根据参数添加验证规则
            AddValidationRulesFromParameter(param, field);

            return field;
        }

        /// <summary>
        /// 创建单个表单字段的 AMIS 配置基于属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <param name="parentName">父参数名称（用于嵌套字段名）。</param>
        /// <returns>表单字段的 JSON 对象。</returns>
        private JObject CreateAmisFormFieldFromProperty(PropertyInfo prop, string parentName)
        {
            var label = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? ToTitleCase(prop.Name);
            //var fieldName = ToCamelCase($"{parentName}.{prop.Name}");
            var fieldName = ToCamelCase($"{prop.Name}");
            var isRequired = !IsNullable(prop);

            var field = new JObject
            {
                ["name"] = fieldName,
                ["label"] = label,
                ["required"] = isRequired,
                ["type"] = GetFormFieldType(prop)
            };

            // 根据属性添加验证规则
            AddValidationRules(prop, field);

            return field;
        }

        /// <summary>
        /// 判断当前用户是否有权限编辑指定的属性。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果有权限则返回 true，否则返回 false。</returns>
        private bool HasEditPermission(PropertyInfo prop)
        {
            var permissionAttr = prop.GetCustomAttribute<PermissionAttribute>();
            return permissionAttr == null || _permissionService.HasPermission(permissionAttr.Permission);
        }

        /// <summary>
        /// 判断类型是否为简单类型（基元类型、字符串、枚举等）。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>如果是简单类型则返回 true，否则返回 false。</returns>
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || new Type[]
                {
                    typeof(string),
                    typeof(decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type)
                || type.IsEnum
                || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)));
        }

        /// <summary>
        /// 判断类型是否为复杂类型（类但非字符串）。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>如果是复杂类型则返回 true，否则返回 false。</returns>
        private bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        /// <summary>
        /// 判断类型是否为可空的枚举类型。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>如果是可空枚举类型则返回 true，否则返回 false。</returns>
        private bool IsNullableEnum(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            return underlying != null && underlying.IsEnum;
        }

        /// <summary>
        /// 确定表单字段的类型，根据属性的类型进行映射。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>AMIS 表单字段的类型字符串。</returns>
        private string GetFormFieldType(PropertyInfo prop)
        {
            return prop.PropertyType switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                Type t when t.IsEnum => "select",
                _ => "input-text"
            };
        }

        /// <summary>
        /// 确定表单字段的类型，根据参数的类型进行映射。
        /// </summary>
        /// <param name="type">参数的类型。</param>
        /// <returns>AMIS 表单字段的类型字符串。</returns>
        private string GetFormFieldType(Type type)
        {
            return type switch
            {
                Type t when t == typeof(string) => "input-text",
                Type t when t == typeof(int) || t == typeof(long) ||
                           t == typeof(float) || t == typeof(double) => "input-number",
                Type t when t == typeof(bool) => "switch",
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => "datetime",
                Type t when t.IsEnum => "select",
                _ => "input-text"
            };
        }

        /// <summary>
        /// 判断属性是否可为空。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <returns>如果可为空则返回 true，否则返回 false。</returns>
        private bool IsNullable(PropertyInfo prop)
        {
            if (!prop.PropertyType.IsValueType)
                return true; // 引用类型默认可为空

            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                return true; // Nullable<T> 可为空

            return false; // 非 Nullable 的值类型不可为空
        }

        /// <summary>
        /// 判断类型是否为可空的。
        /// </summary>
        /// <param name="type">要检查的类型。</param>
        /// <returns>如果可为空则返回 true，否则返回 false。</returns>
        private bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true; // 引用类型默认可为空

            if (Nullable.GetUnderlyingType(type) != null)
                return true; // Nullable<T> 可为空

            return false; // 非 Nullable 的值类型不可为空
        }

        /// <summary>
        /// 根据属性添加 AMIS 表单字段的验证规则。
        /// </summary>
        /// <param name="prop">属性的信息。</param>
        /// <param name="field">表单字段的 JSON 对象。</param>
        private void AddValidationRules(PropertyInfo prop, JObject field)
        {
            var validationRules = new JObject();

            // 检查字符串长度限制
            var stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            // 检查数值范围限制
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            // 如果有验证规则，则添加到字段配置中
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            // 如果是枚举类型，则添加枚举选项
            if (prop.PropertyType.IsEnum || IsNullableEnum(prop.PropertyType))
            {
                field["options"] = GetEnumOptions(prop.PropertyType);
            }
        }

        /// <summary>
        /// 根据参数添加 AMIS 表单字段的验证规则。
        /// </summary>
        /// <param name="param">参数的信息。</param>
        /// <param name="field">表单字段的 JSON 对象。</param>
        private void AddValidationRulesFromParameter(ParameterInfo param, JObject field)
        {
            var validationRules = new JObject();

            // 检查是否为必填字段
            if (param.GetCustomAttribute<RequiredAttribute>() != null)
            {
                field["required"] = true;
                validationRules["required"] = true;
            }

            // 检查字符串长度限制
            var stringLengthAttr = param.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                if (stringLengthAttr.MaximumLength > 0)
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
            }

            // 检查数值范围限制
            var rangeAttr = param.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                    validationRules["minimum"] = Convert.ToDouble(rangeAttr.Minimum);
                if (rangeAttr.Maximum != null)
                    validationRules["maximum"] = Convert.ToDouble(rangeAttr.Maximum);
            }

            // 如果有验证规则，则添加到字段配置中
            if (validationRules.HasValues)
            {
                field["validations"] = validationRules;
            }

            // 如果是枚举类型，则添加枚举选项
            if (param.ParameterType.IsEnum || IsNullableEnum(param.ParameterType))
            {
                field["options"] = GetEnumOptions(param.ParameterType);
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 将字符串转换为标题大小写（首字母大写）。
        /// </summary>
        /// <param name="str">要转换的字符串。</param>
        /// <returns>转换后的字符串。</returns>
        private string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// 将字符串转换为 camelCase（首字母小写）。
        /// </summary>
        /// <param name="str">要转换的字符串。</param>
        /// <returns>转换后的字符串。</returns>
        private string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length == 1)
                return str.ToLower();

            return char.ToLower(str[0]) + str.Substring(1);
        }

        #endregion

        #region API 路由助手

        /// <summary>
        /// 构建完整的绝对 URL。
        /// </summary>
        /// <param name="relativePath">相对路径。</param>
        /// <returns>完整的绝对 URL。</returns>
        private string BuildAbsoluteUrl(string relativePath)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return relativePath; // 如果没有请求上下文，则返回相对路径

            var host = request.Host.Value; // 例如 "localhost:5000"
            var scheme = request.Scheme; // "http" 或 "https"

            return $"{scheme}://{host}/{relativePath.TrimStart('/')}";
        }

        /// <summary>
        /// 组合基础路由和模板路由，处理占位符。
        /// </summary>
        /// <param name="baseRoute">基础路由。</param>
        /// <param name="template">模板路由。</param>
        /// <returns>组合后的路由。</returns>
        private string CombineRoutes(string baseRoute, string template)
        {
            template = template?.Replace("{id}", "${id}") ?? string.Empty;
            if (string.IsNullOrEmpty(template))
                return baseRoute;

            return $"{baseRoute}/{template}".Replace("//", "/");
        }

        #endregion
    }
}
