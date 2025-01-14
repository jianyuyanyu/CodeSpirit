using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc.Routing;
using System.ComponentModel.DataAnnotations;

public class AmisGenerator
{
    private readonly Assembly _assembly;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AmisGenerator(IHttpContextAccessor httpContextAccessor)
    {
        _assembly = typeof(AmisGenerator).Assembly;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 生成指定控制器的 AMIS JSON
    /// </summary>
    /// <param name="controllerName">控制器名称（不含 "Controller" 后缀）</param>
    /// <returns>AMIS 定义的 JSON 对象，如果控制器不存在或不支持则返回 null</returns>
    public JObject GenerateAmisJsonForController(string controllerName)
    {
        // 查找控制器类型
        var controllerType = GetControllerType(controllerName);
        if (controllerType == null)
            return null;

        var baseRoute = GetRoute(controllerType);
        var actions = GetControllerActions(controllerType);

        // 检查是否包含 CRUD 操作
        if (!HasCrudActions(actions))
            return null;

        // 生成 AMIS CRUD 配置
        var crudConfig = GenerateAmisCrudConfig(controllerName, baseRoute, actions);
        return crudConfig;
    }

    private Type GetControllerType(string controllerName)
    {
        // 查找以指定名称结尾的控制器类
        return _assembly.GetTypes()
                        .FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ControllerBase)) && t.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase));
    }

    private string GetRoute(Type controller)
    {
        var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
        return routeAttr?.Template?.Replace("[controller]", controller.Name.Replace("Controller", "")) ?? "";
    }

    private IEnumerable<MethodInfo> GetControllerActions(Type controller)
    {
        return controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => m.GetCustomAttribute<HttpMethodAttribute>() != null);
    }

    private bool HasCrudActions(IEnumerable<MethodInfo> actions)
    {
        var httpMethods = actions.SelectMany(a => a.GetCustomAttributes<HttpMethodAttribute>()
                                                     .SelectMany(attr => attr.HttpMethods));
        return httpMethods.Contains("GET") && httpMethods.Contains("POST") &&
               httpMethods.Contains("PUT") && httpMethods.Contains("DELETE");
    }

    private JObject GenerateAmisCrudConfig(string controllerName, string baseRoute, IEnumerable<MethodInfo> actions)
    {
        // 获取 CRUD 操作对应的路由模板
        var getListRouteTemplate = actions.FirstOrDefault(a => a.GetCustomAttributes<HttpGetAttribute>().Any())?.GetCustomAttribute<HttpGetAttribute>()?.Template ?? "";
        var createRouteTemplate = actions.FirstOrDefault(a => a.GetCustomAttributes<HttpPostAttribute>().Any())?.GetCustomAttribute<HttpPostAttribute>()?.Template ?? "";
        var updateRouteTemplate = actions.FirstOrDefault(a => a.GetCustomAttributes<HttpPutAttribute>().Any())?.GetCustomAttribute<HttpPutAttribute>()?.Template ?? "";
        var deleteRouteTemplate = actions.FirstOrDefault(a => a.GetCustomAttributes<HttpDeleteAttribute>().Any())?.GetCustomAttribute<HttpDeleteAttribute>()?.Template ?? "";

        // 构建完整的 API 路径
        var getListRoute = BuildAbsoluteUrl(CombineRoutes(baseRoute, getListRouteTemplate));
        var createRoute = BuildAbsoluteUrl(CombineRoutes(baseRoute, createRouteTemplate));
        var updateRoute = BuildAbsoluteUrl(CombineRoutes(baseRoute, updateRouteTemplate));
        var deleteRoute = BuildAbsoluteUrl(CombineRoutes(baseRoute, deleteRouteTemplate));

        // 获取返回类型以生成表格列和表单字段
        var getListAction = actions.FirstOrDefault(a => a.GetCustomAttributes<HttpGetAttribute>().Any());
        if (getListAction == null)
            return null;

        var returnType = getListAction.ReturnType;
        var dataType = GetDataTypeFromReturnType(returnType);
        if (dataType == null)
            return null;

        var columns = GetAmisColumns(dataType, controllerName, updateRoute, deleteRoute);
        var searchFields = GetAmisSearchFields(getListAction);

        var crud = new JObject
        {
            ["type"] = "crud",
            ["name"] = $"{controllerName.ToLower()}Crud",
            ["api"] = new JObject
            {
                ["url"] = getListRoute,
                ["method"] = "get"
            },
            ["columns"] = new JArray(columns),
            ["searchConfig"] = new JObject
            {
                ["submitText"] = "搜索",
                ["resetText"] = "重置",
                ["controls"] = new JArray(searchFields)
            },
            ["createApi"] = new JObject
            {
                ["url"] = createRoute,
                ["method"] = "post"
            },
            ["updateApi"] = new JObject
            {
                ["url"] = updateRoute + "/$id",
                ["method"] = "put"
            },
            ["deleteApi"] = new JObject
            {
                ["url"] = deleteRoute + "/$id",
                ["method"] = "delete"
            },
            ["title"] = $"{controllerName} 管理"
        };

        return crud;
    }

    private string BuildAbsoluteUrl(string relativePath)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
            return relativePath; // 如果没有请求上下文，则返回相对路径

        var host = request.Host.Value; // 例如 "localhost:5000"
        var scheme = request.Scheme; // "http" 或 "https"

        return $"{scheme}://{host}/{relativePath.TrimStart('/')}";
    }

    private string CombineRoutes(string baseRoute, string template)
    {
        if (string.IsNullOrEmpty(template))
            return baseRoute;

        return $"{baseRoute}/{template}".Replace("//", "/");
    }

    private Type GetDataTypeFromReturnType(Type returnType)
    {
        // 处理 ActionResult<T>, Task<ActionResult<T>>, etc.
        if (returnType.IsGenericType)
        {
            var genericDef = returnType.GetGenericTypeDefinition();
            if (genericDef == typeof(ActionResult<>))
            {
                var innerType = returnType.GetGenericArguments()[0];
                return ExtractDataType(innerType);
            }
            if (genericDef == typeof(Task<>))
            {
                var taskInnerType = returnType.GetGenericArguments()[0];
                if (taskInnerType.IsGenericType && taskInnerType.GetGenericTypeDefinition() == typeof(ActionResult<>))
                {
                    var innerType = taskInnerType.GetGenericArguments()[0];
                    return ExtractDataType(innerType);
                }
            }
        }

        return null;
    }

    private Type ExtractDataType(Type type)
    {
        // 假设 ApiResponse<T> 或 ListData<T>
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(ApiResponse<>))
            {
                var innerType = type.GetGenericArguments()[0];
                // 如果是列表数据
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

    private List<JObject> GetAmisColumns(Type dataType, string controllerName, string updateRoute, string deleteRoute)
    {
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<JObject>();

        foreach (var prop in properties)
        {
            // 忽略某些不需要显示的字段（如密码）
            if (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase))
                continue;

            var column = new JObject
            {
                ["name"] = prop.Name,
                ["label"] = ToTitleCase(prop.Name),
                ["sortable"] = true
            };

            // 根据属性类型，决定列的类型
            if (prop.PropertyType == typeof(bool))
            {
                column["type"] = "switch";
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                column["type"] = "datetime";
                column["format"] = "YYYY-MM-DD HH:mm:ss";
            }
            else
            {
                column["type"] = "text";
            }

            // 识别主键
            if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                column["type"] = "text"; // 保持为文本显示
            }

            columns.Add(column);
        }


        // 编辑按钮权限
        //if (_permissionService.HasPermission($"{controllerName}Edit"))
        //{ }
        // 添加操作列
        var operations = new JObject
        {
            ["label"] = "操作",
            ["type"] = "operation",
            ["buttons"] = new JArray
            {
                new JObject
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
                                ["url"] = $"{updateRoute}/$id",
                                ["method"] = "put"
                            },
                            ["body"] = new JArray(GetAmisFormFields(dataType))
                        }
                    }
                },
                new JObject
                {
                    ["type"] = "button",
                    ["label"] = "删除",
                    ["actionType"] = "ajax",
                    ["confirmText"] = "确定要删除吗？",
                    ["api"] = new JObject
                    {
                        ["url"] = $"{deleteRoute}/$id",
                        ["method"] = "delete"
                    }
                }
            }
        };

        // 只有存在按钮时才添加操作列
        if (operations["buttons"].HasValues)
        {
            columns.Add(operations);
        }

        return columns;
    }

    private List<JObject> GetAmisSearchFields(MethodInfo getListAction)
    {
        var parameters = getListAction.GetParameters();
        var searchFields = new List<JObject>();

        foreach (var param in parameters)
        {
            if (param.GetCustomAttribute<FromQueryAttribute>() != null)
            {
                var paramType = param.ParameterType;
                var field = new JObject
                {
                    ["name"] = param.Name,
                    ["label"] = ToTitleCase(param.Name)
                };

                // 根据参数类型调整搜索控件类型
                if (paramType == typeof(int) || paramType == typeof(int?))
                {
                    field["type"] = "input-number";
                }
                else if (paramType == typeof(bool) || paramType == typeof(bool?))
                {
                    field["type"] = "select";
                    field["options"] = new JArray
                    {
                        new JObject { ["label"] = "是", ["value"] = true },
                        new JObject { ["label"] = "否", ["value"] = false }
                    };
                }
                else if (paramType.IsEnum || (Nullable.GetUnderlyingType(paramType)?.IsEnum ?? false))
                {
                    field["type"] = "select";
                    var enumType = Nullable.GetUnderlyingType(paramType) ?? paramType;
                    var enumOptions = Enum.GetValues(enumType).Cast<object>().Select(e => new JObject
                    {
                        ["label"] = e.ToString(),
                        ["value"] = e.ToString()
                    }).ToArray();
                    field["options"] = new JArray(enumOptions);
                }
                else if (paramType == typeof(DateTime) || paramType == typeof(DateTime?))
                {
                    field["type"] = "date";
                    field["format"] = "YYYY-MM-DD";
                }
                else
                {
                    field["type"] = "input-text";
                }

                searchFields.Add(field);
            }
        }

        return searchFields;
    }

    private List<JObject> GetAmisFormFields(Type dataType)
    {
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var formFields = new List<JObject>();

        foreach (var prop in properties)
        {
            // 忽略某些不需要显示的字段（如密码）
            if (prop.Name.Equals("Password", StringComparison.OrdinalIgnoreCase))
                continue;

            var field = new JObject
            {
                ["name"] = prop.Name,
                ["label"] = ToTitleCase(prop.Name),
                ["required"] = !IsNullable(prop)
            };

            // 根据属性类型，决定表单控件的类型
            if (prop.PropertyType == typeof(string))
            {
                field["type"] = "input-text";
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(long) ||
                     prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
            {
                field["type"] = "input-number";
            }
            else if (prop.PropertyType == typeof(bool))
            {
                field["type"] = "switch";
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                field["type"] = "datetime";
                field["format"] = "YYYY-MM-DD HH:mm:ss";
            }
            else if (prop.PropertyType.IsEnum)
            {
                field["type"] = "select";
                var enumOptions = Enum.GetValues(prop.PropertyType).Cast<object>().Select(e => new JObject
                {
                    ["label"] = e.ToString(),
                    ["value"] = e.ToString()
                }).ToArray();
                field["options"] = new JArray(enumOptions);
            }
            //else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            //{
            //    // 假设这是一个关联实体，生成级联选择器
            //    field["type"] = "cascader";
            //    // 添加相关配置，例如数据源 API
            //    field["source"] = $"https://yourdomain.com/api/{prop.PropertyType.Name.ToLower()}/options";
            //}
            else
            {
                field["type"] = "input-text";
            }

            // 表单验证
            var validationRules = new JObject();

            var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttr != null)
            {
                field["required"] = true;
                validationRules["required"] = true;
            }

            var stringLengthAttr = prop.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (stringLengthAttr.MinimumLength > 0)
                {
                    validationRules["minLength"] = stringLengthAttr.MinimumLength;
                }
                if (stringLengthAttr.MaximumLength > 0)
                {
                    validationRules["maxLength"] = stringLengthAttr.MaximumLength;
                }
            }

            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr != null)
            {
                if (rangeAttr.Minimum != null)
                {
                    validationRules["min"] = Convert.ToDouble(rangeAttr.Minimum);
                }
                if (rangeAttr.Maximum != null)
                {
                    validationRules["max"] = Convert.ToDouble(rangeAttr.Maximum);
                }
            }

            // 其他验证属性可以在此添加

            if (validationRules.HasValues)
            {
                field["validate"] = validationRules;
            }

            formFields.Add(field);
        }

        return formFields;
    }

    private bool IsNullable(PropertyInfo prop)
    {
        if (!prop.PropertyType.IsValueType)
            return true; // 引用类型默认可为空

        if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
            return true; // Nullable<T> 可为空

        return false; // 非 Nullable 的值类型不可为空
    }

    private string ToTitleCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpper(str[0]) + str.Substring(1);
    }
}
