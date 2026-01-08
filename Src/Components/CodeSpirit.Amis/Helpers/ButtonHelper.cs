using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Handlers;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeSpirit.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly AmisContext amisContext;
        private readonly ApiRouteHelper apiRouteHelper;
        private readonly AmisApiHelper amisApiHelper;
        private readonly FormFieldHelper formFieldHelper;
        private readonly ILogger<ButtonHelper> _logger;
        private readonly IStringLocalizerFactory _localizerFactory;
        private readonly CultureResolver _cultureResolver;
        private readonly IServiceProvider _serviceProvider;
        private CrudDialogHandler _crudDialogHandler;
        
        /// <summary>
        /// 缓存 SharedResources 类型，避免重复反射查找
        /// </summary>
        private static Type _sharedResourcesType;
        
        /// <summary>
        /// 用于缓存类型的锁对象
        /// </summary>
        private static readonly object _typeLock = new object();

        public ButtonHelper(IHasPermissionService permissionService, AmisContext amisContext, ApiRouteHelper apiRouteHelper, AmisApiHelper amisApiHelper, FormFieldHelper formFieldHelper, ILogger<ButtonHelper> logger, IStringLocalizerFactory localizerFactory, CultureResolver cultureResolver, IServiceProvider serviceProvider)
        {
            _permissionService = permissionService;
            this.amisContext = amisContext;
            this.apiRouteHelper = apiRouteHelper;
            this.amisApiHelper = amisApiHelper;
            this.formFieldHelper = formFieldHelper;
            _logger = logger;
            _localizerFactory = localizerFactory;
            _cultureResolver = cultureResolver;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取 CrudDialogHandler（延迟解析以避免循环依赖）
        /// </summary>
        private CrudDialogHandler CrudDialogHandler
        {
            get
            {
                if (_crudDialogHandler == null)
                {
                    _crudDialogHandler = _serviceProvider.GetRequiredService<CrudDialogHandler>();
                }
                return _crudDialogHandler;
            }
        }

        /// <summary>
        /// 将 DialogSize 枚举转换为字符串
        /// </summary>
        /// <param name="dialogSize">对话框大小枚举</param>
        /// <returns>对话框大小字符串</returns>
        private static string ConvertDialogSizeToString(DialogSize dialogSize)
        {
            return dialogSize switch
            {
                DialogSize.XS => "xs",
                DialogSize.SM => "sm",
                DialogSize.MD => "md",
                DialogSize.LG => "lg",
                DialogSize.XL => "xl",
                DialogSize.Full => "full",
                DialogSize.Custom => "custom",
                _ => "md"
            };
        }

        /// <summary>
        /// 获取本地化的文本
        /// 优先从资源文件加载，如果资源不存在则使用回退文本
        /// </summary>
        /// <param name="resourceKey">资源键</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="fallbackText">回退文本</param>
        /// <returns>本地化文本</returns>
        public string GetLocalizedText(string resourceKey, Type resourceType, string fallbackText)
        {
            // 如果未指定资源键或资源类型，直接返回回退文本
            if (string.IsNullOrEmpty(resourceKey) || resourceType == null)
                return fallbackText;
            
            try
            {
                // 通过 CultureResolver 获取当前语言文化信息（而不是直接使用 CurrentUICulture）
                var currentCulture = _cultureResolver.GetCurrentCulture();
                
                // 获取 ResourceManager 属性
                var resourceManagerProp = resourceType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.Static);
                
                if (resourceManagerProp != null)
                {
                    var resourceManager = resourceManagerProp.GetValue(null) as System.Resources.ResourceManager;
                    if (resourceManager != null)
                    {
                        // 先获取默认资源文件中的值（用于比较）
                        var defaultCulture = new System.Globalization.CultureInfo("zh-CN");
                        var defaultText = resourceManager.GetString(resourceKey, defaultCulture);
                        
                        // 使用当前文化获取本地化文本
                        var localizedText = resourceManager.GetString(resourceKey, currentCulture);
                        
                        // 如果获取到了本地化文本
                        if (!string.IsNullOrEmpty(localizedText))
                        {
                            // 如果当前文化是英文，且返回的文本与默认文本不同，说明找到了英文资源文件
                            if (currentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                            {
                                // 如果返回的文本与默认文本不同，说明找到了英文资源文件
                                if (localizedText != defaultText)
                                {
                                    return localizedText;
                                }
                                
                                // 如果相同，可能回退到了默认资源文件，尝试直接使用 "en" 文化
                                var enCulture = new System.Globalization.CultureInfo("en");
                                var enText = resourceManager.GetString(resourceKey, enCulture);
                                if (!string.IsNullOrEmpty(enText) && enText != defaultText)
                                {
                                    return enText;
                                }
                            }
                            else
                            {
                                // 非英文文化，直接返回获取到的文本
                                return localizedText;
                            }
                        }
                    }
                }
                
                // 如果 ResourceManager 属性不可用，手动创建 ResourceManager
                // 这是针对占位类（placeholder class）的处理，如 OperationsResources
                string baseName = resourceType.Name;
                if (baseName.EndsWith("Resources"))
                {
                    baseName = baseName.Substring(0, baseName.Length - "Resources".Length);
                }
                
                var resourceNamespace = resourceType.Namespace;
                var resourceFullName = $"{resourceNamespace}.{baseName}";
                
                // 手动创建 ResourceManager
                var manualResourceManager = new System.Resources.ResourceManager(
                    resourceFullName,
                    resourceType.Assembly);
                
                // 先获取默认资源文件中的值（用于比较）
                var defaultCulture2 = new System.Globalization.CultureInfo("zh-CN");
                var defaultText2 = manualResourceManager.GetString(resourceKey, defaultCulture2);
                
                // 使用当前文化获取本地化文本
                var localizedText2 = manualResourceManager.GetString(resourceKey, currentCulture);
                
                // 如果获取到了本地化文本
                if (!string.IsNullOrEmpty(localizedText2))
                {
                    // 如果当前文化是英文，且返回的文本与默认文本不同，说明找到了英文资源文件
                    if (currentCulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    {
                        // 如果返回的文本与默认文本不同，说明找到了英文资源文件
                        if (localizedText2 != defaultText2)
                        {
                            return localizedText2;
                        }
                        
                        // 如果相同，可能回退到了默认资源文件，尝试直接使用 "en" 文化
                        var enCulture2 = new System.Globalization.CultureInfo("en");
                        var enText2 = manualResourceManager.GetString(resourceKey, enCulture2);
                        if (!string.IsNullOrEmpty(enText2) && enText2 != defaultText2)
                        {
                            return enText2;
                        }
                        
                        // 如果还是没有找到英文资源，使用回退文本
                        if (!string.IsNullOrEmpty(fallbackText))
                        {
                            return fallbackText;
                        }
                    }
                    else
                    {
                        // 非英文文化，直接返回获取到的文本
                        return localizedText2;
                    }
                }
                
                // 如果资源不存在，使用回退文本
                if (!string.IsNullOrEmpty(fallbackText))
                    return fallbackText;
                
                return localizedText2 ?? resourceKey;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get localized text for key: {ResourceKey} from type: {ResourceType}", 
                    resourceKey, resourceType?.FullName);
                return fallbackText;
            }
        }

        /// <summary>
        /// 获取 SharedResources 类型（通过反射动态加载，避免循环依赖）
        /// 使用缓存机制提升性能
        /// </summary>
        public static Type GetSharedResourcesType()
        {
            // 双重检查锁定模式，确保线程安全且性能最优
            if (_sharedResourcesType == null)
            {
                lock (_typeLock)
                {
                    if (_sharedResourcesType == null)
                    {
                        _sharedResourcesType = Type.GetType("CodeSpirit.Localization.Resources.SharedResources, CodeSpirit.Localization") 
                            ?? typeof(object);
                    }
                }
            }
            
            return _sharedResourcesType;
        }

        // 创建一个通用的按钮模板
        private JObject CreateButton(string label, string actionType, JObject dialogOrDrawer = null, JObject api = null, string confirmText = null, bool? download = null, string visibleOn = null, DialogSize dialogSize = DialogSize.MD)
        {
            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = label,
                ["actionType"] = actionType
            };

            CreateIcon(label, button);

            if (dialogOrDrawer != null)
            {
                // 如果对话框配置中没有指定size，则使用传入的dialogSize
                if (dialogOrDrawer["size"] == null)
                {
                    dialogOrDrawer["size"] = ConvertDialogSizeToString(dialogSize);
                }

                button["dialog"] = dialogOrDrawer;
            }

            if (api != null)
            {
                button["api"] = api;
            }

            if (!string.IsNullOrEmpty(confirmText))
            {
                button["confirmText"] = confirmText;
            }

            if (download.HasValue && download.Value)
            {
                button["download"] = true;
            }

            if (!string.IsNullOrEmpty(visibleOn))
            {
                button["visibleOn"] = visibleOn;
            }

            return button;
        }

        /// <summary>
        /// 创建图标
        /// </summary>
        /// <param name="label"></param>
        /// <param name="button"></param>
        private static void CreateIcon(string label, JObject button)
        {
            // 根据按钮文字和操作类型添加对应图标
            string icon = label switch
            {
                // 基础操作
                "新增" or "添加" => "fa fa-plus",
                "编辑" or "修改" => "fa fa-edit",
                "删除" or "移除" => "fa fa-trash",
                "查看" or "详情" => "fa fa-eye",
                "导入" => "fa fa-upload",
                "导出" or "导出全部" => "fa fa-download",
                "关闭" => "fa fa-times",

                // 状态操作
                "解锁" => "fa fa-unlock",
                "锁定" => "fa fa-lock",
                "启用" => "fa fa-check-circle",
                "禁用" => "fa fa-ban",
                "激活" => "fa fa-check",
                "冻结" => "fa fa-snowflake-o",
                "审核" or "审批" => "fa fa-gavel",
                "驳回" => "fa fa-times-circle",

                // 账户相关
                "重置密码" => "fa fa-key",
                "修改密码" => "fa fa-lock",
                "登录" => "fa fa-sign-in",
                "退出" or "注销" => "fa fa-sign-out",

                // 数据操作
                "刷新" => "fa fa-refresh",
                "保存" => "fa fa-save",
                "搜索" or "查询" => "fa fa-search",
                "清空" or "清除" => "fa fa-eraser",
                "复制" => "fa fa-copy",
                "打印" => "fa fa-print",
                "下载" => "fa fa-download",

                // 流程操作
                "提交" => "fa fa-check",
                "确定" => "fa fa-check",
                "取消" => "fa fa-times",
                "返回" => "fa fa-arrow-left",
                "下一步" => "fa fa-arrow-right",
                "上一步" => "fa fa-arrow-left",

                // 配置操作
                "设置" or "配置" => "fa fa-cog",
                "权限" => "fa fa-shield",
                "分配" or "分派" => "fa fa-share-square",
                "排序" => "fa fa-sort",
                "置顶" => "fa fa-arrow-up",

                // 文件操作
                "上传" => "fa fa-upload",
                "预览" => "fa fa-eye",
                "附件" => "fa fa-paperclip",
                "归档" => "fa fa-archive",

                // 消息操作
                "发送" => "fa fa-paper-plane",
                "通知" => "fa fa-bell",
                "消息" => "fa fa-envelope",

                // 其他常用操作
                "同步" => "fa fa-sync",
                "统计" => "fa fa-chart-bar",
                "更多" => "fa fa-ellipsis-h",
                "帮助" => "fa fa-question-circle",
                "收藏" => "fa fa-star",
                "点赞" => "fa fa-thumbs-up",

                // 新增的批量操作
                "批量删除" => "fa fa-trash-o",
                "导出当前页" => "fa fa-file-export",
                "模拟登录" => "fa fa-user-secret",
                "发布" => "fa fa-cloud-upload",
                "取消发布" => "fa fa-cloud-download",
                "历史版本" => "fa fa-history",
                "版本记录" => "fa fa-clock-o",
                "批量导出" => "fa fa-files-o",
                "批量启用" => "fa fa-check-circle-o",
                "批量禁用" => "fa fa-ban",
                "批量审核" => "fa fa-gavel",
                "批量通过" => "fa fa-thumbs-up",
                "批量驳回" => "fa fa-thumbs-down",
                "发送通知" => "fa fa-bullhorn",
                "版本对比" => "fa fa-code-fork",
                "回滚版本" => "fa fa-undo",
                "续期" => "fa fa-calendar-plus-o",
                "撤销" => "fa fa-times-circle",
                "重新生成" => "fa fa-refresh",
                "快速创建" => "fa fa-magic",
                "进入登录" => "fa fa-sign-in",
                
                // 英文映射
                "Add" or "Create" => "fa fa-plus",
                "Edit" or "Update" => "fa fa-edit",
                "Delete" or "Remove" => "fa fa-trash",
                "View" or "Detail" => "fa fa-eye",
                "Reset Password" => "fa fa-key",
                "Unlock" => "fa fa-unlock",
                "Impersonate Login" => "fa fa-user-secret",
                "Batch Delete" => "fa fa-trash-o",
                "Enable" => "fa fa-check-circle",
                "Disable" => "fa fa-ban",
                "Renew" => "fa fa-calendar-plus-o",
                "Revoke" => "fa fa-times-circle",
                "Regenerate" => "fa fa-refresh",
                "Quick Create" => "fa fa-magic",
                "Redirect Login" => "fa fa-sign-in",
                "Close" => "fa fa-times",
                "Save" => "fa fa-save",
                "Cancel" => "fa fa-times",
                "Submit" => "fa fa-check",
                "Confirm" => "fa fa-check",

                _ => null // 其他情况不设置图标
            };

            if (icon != null)
            {
                button["icon"] = icon;
            }
        }

        // 创建"新增"按钮
        public JObject CreateHeaderButton(string title = "新增", ApiRouteInfo route = null, IEnumerable<ParameterInfo> formParameters = null, string size = null, DialogSize dialogSize = DialogSize.MD, string customActions = null, MethodInfo method = null)
        {
            // 尝试本地化标题
            string localizedTitle = GetLocalizedText("Common.Add", GetSharedResourcesType(), title);
            
            // 如果没有指定size，则使用dialogSize参数
            string dialogSizeString = size ?? ConvertDialogSizeToString(dialogSize);

            JObject dialogBody = new()
            {
                ["title"] = localizedTitle,
                ["size"] = dialogSizeString,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(formParameters, method))
                },
            };

            // 处理自定义 actions 配置
            if (!string.IsNullOrEmpty(customActions))
            {
                try
                {
                    var actions = JsonConvert.DeserializeObject<JArray>(customActions);
                    dialogBody["actions"] = actions;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("解析 Header Button Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                }
            }
            else if (customActions == "")
            {
                // 空字符串表示不显示底部按钮
                dialogBody["actions"] = new JArray();
            }
            // 如果 customActions 为 null，则使用默认按钮（不设置 actions 属性）

            JObject button = CreateButton(localizedTitle, "dialog", dialogOrDrawer: dialogBody, dialogSize: dialogSize);
            
            // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
            if (amisContext.HasTabCountService)
            {
                button["reload"] = "tabCountService";
            }
            
            return button;
        }

        // 创建"编辑"按钮
        public JObject CreateEditButton(ApiRouteInfo updateRoute, IEnumerable<ParameterInfo> updateParameters, DialogSize dialogSize = DialogSize.MD, MethodInfo method = null)
        {
            string title = GetLocalizedText("Common.Edit", GetSharedResourcesType(), "编辑");
            string dialogSizeString = ConvertDialogSizeToString(dialogSize);

            JObject drawerBody = new()
            {
                ["title"] = title,
                ["size"] = dialogSizeString,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = updateRoute.ApiPath,
                        ["method"] = updateRoute.HttpMethod
                    },
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(updateParameters, method))
                }
            };
            JObject button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody, dialogSize: dialogSize);
            
            // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
            if (amisContext.HasTabCountService)
            {
                button["reload"] = "tabCountService";
            }
            
            return button;
        }

        public JObject CreateDetailButton(ApiRouteInfo detailRoute, IEnumerable<PropertyInfo> detailPropertites, DialogSize dialogSize = DialogSize.LG, Type dtoType = null)
        {
            string title = GetLocalizedText("Common.View", GetSharedResourcesType(), "查看");
            string closeText = GetLocalizedText("Common.Close", GetSharedResourcesType(), "关闭");
            JArray controls = [];

            List<JObject> formFields = GetFormFieldsWithAiSupport(detailPropertites, dtoType, isReadOnly: true);

            // 遍历字段,在每个字段后面添加分割线(最后一个字段除外)
            for (int i = 0; i < formFields.Count(); i++)
            {
                JObject field = formFields[i];

                // 检查是否为图片或头像类型
                if (field["type"]?.ToString() == "image" || field["type"]?.ToString() == "avatar")
                {
                    // 创建control包裹
                    JObject controlWrapper = new()
                    {
                        ["type"] = "control",
                        ["label"] = field["label"],
                        ["body"] = new JArray { field }
                    };
                    // 移除原始的label，因为已经移到control层级
                    field.Remove("label");
                    controls.Add(controlWrapper);
                }
                else
                {
                    controls.Add(field);
                }

                // 如果不是最后一个字段,添加分割线
                if (i < formFields.Count() - 1)
                {
                    controls.Add(new JObject
                    {
                        ["type"] = "divider"
                    });
                }
            }

            string dialogSizeString = ConvertDialogSizeToString(dialogSize);

            JObject drawerBody = new()
            {
                ["title"] = title,
                ["size"] = dialogSizeString,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = detailRoute.ApiPath,
                        ["method"] = detailRoute.HttpMethod
                    },
                    ["controls"] = controls,
                    ["mode"] = "horizontal",
                    ["static"] = true,
                    ["submitText"] = "",
                    ["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = closeText,
                            ["actionType"] = "close"
                        }
                    }
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody, dialogSize: dialogSize);
        }

        // 创建"删除"按钮
        public JObject CreateDeleteButton(ApiRouteInfo deleteRoute)
        {
            string label = GetLocalizedText("Common.Delete", GetSharedResourcesType(), "删除");
            string confirmText = GetLocalizedText("Common.ConfirmDelete", GetSharedResourcesType(), "确定要删除吗？");
            
            JObject api = new()
            {
                ["url"] = deleteRoute.ApiPath,
                ["method"] = deleteRoute.HttpMethod
            };

            JObject button = CreateButton(label, "ajax", api: api, confirmText: confirmText);
            
            // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
            if (amisContext.HasTabCountService)
            {
                button["reload"] = "tabCountService";
            }
            
            return button;
        }

        // 获取自定义操作按钮
        public List<JObject> GetCustomOperationsButtons<TOperation>(bool isBulkOperation = false, bool isHeader = false) where TOperation : OperationAttribute
        {
            List<JObject> buttons = [];
            // 获取当前类型的所有方法
            MethodInfo[] methods = amisContext.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            // 查找带有 [Operation] 特性的所有方法
            foreach (MethodInfo method in methods)
            {
                // 使用 GetCustomAttributes（复数）以支持一个方法有多个相同类型的特性
                var operations = method.GetCustomAttributes<TOperation>(inherit: false);
                var hasHeaderOp = method.GetCustomAttribute<HeaderOperationAttribute>(inherit: false) != null;
                
                // 遍历方法上的所有操作特性
                foreach (TOperation op in operations)
                {
                    // 如果是查找头部操作，只处理真正的HeaderOperationAttribute
                    if (isHeader && typeof(TOperation) == typeof(HeaderOperationAttribute))
                    {
                        // 确保方法确实有HeaderOperationAttribute
                        if (op == null || !hasHeaderOp)
                        {
                            continue;
                        }
                    }
                    // 如果不是查找头部操作，排除有HeaderOperationAttribute的方法
                    else if (!isHeader && hasHeaderOp)
                    {
                        continue;
                    }

                    if (op != null && op.IsBulkOperation == isBulkOperation)
                    {
                        var permissionCode = _permissionService.GetPermissionCode(method);
                        var hasPermission = _permissionService.HasPermission(permissionCode);

                        _logger.LogWarning("[ButtonHelper权限检查] 方法: {MethodName}, 权限代码: {PermissionCode}, 检查结果: {HasPermission}",
                            method.Name, permissionCode, hasPermission);

                        if (hasPermission)
                        {
                            // 为每个操作方法创建按钮
                            JObject button = CreateCustomOperationButton(op, method);

                            // Add redirect configuration if specified
                            if (op.ActionType == "ajax" && !string.IsNullOrEmpty(op.Redirect))
                            {
                                button["redirect"] = op.Redirect;
                            }
                            buttons.Add(button);
                            _logger.LogWarning("[ButtonHelper权限检查] 按钮已添加: {ButtonLabel}", op.Label);
                        }
                        else
                        {
                            _logger.LogWarning("[ButtonHelper权限检查] 权限不足，按钮未添加: {ButtonLabel}", op.Label);
                        }
                    }
                }
            }

            return buttons;
        }

        // 获取批量操作按钮
        public List<JObject> GetBulkOperationButtons()
        {
            return GetCustomOperationsButtons<OperationAttribute>(true);
        }

        // 获取顶部操作按钮
        public List<JObject> GetHeaderOperationButtons()
        {
            // 直接只查找HeaderOperationAttribute
            List<JObject> buttons = [];
            var methods = amisContext.ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (MethodInfo method in methods)
            {
                // 使用 GetCustomAttributes（复数）以支持一个方法有多个相同类型的特性
                var headerOperations = method.GetCustomAttributes<HeaderOperationAttribute>(inherit: false);
                foreach (var headerOp in headerOperations)
                {
                    if (headerOp != null && !headerOp.IsBulkOperation)
                    {
                        var permissionCode = _permissionService.GetPermissionCode(method);
                        var hasPermission = _permissionService.HasPermission(permissionCode);

                        if (hasPermission)
                        {
                            JObject button = CreateCustomOperationButton(headerOp, method);
                            if (headerOp.ActionType == "ajax" && !string.IsNullOrEmpty(headerOp.Redirect))
                            {
                                button["redirect"] = headerOp.Redirect;
                            }
                            buttons.Add(button);
                        }
                    }
                }
            }
            
            return buttons;
        }

        // 创建自定义操作按钮
        public JObject CreateCustomOperationButton(OperationAttribute op, MethodInfo method)
        {
            // 获取本地化的按钮标签
            string label = GetLocalizedText(op.LabelResourceKey, op.LabelResourceType, op.Label);
            
            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = label,
                ["actionType"] = op.ActionType
            };

            // 处理不同的操作类型
            if (op.ActionType == "link")
            {
                // 对于链接类型，使用 link 属性而不是 api
                string url = op.Api ?? apiRouteHelper.GetApiRouteInfoForMethod(method).ApiPath;
                button["link"] = url;
                button["blank"] = op.Blank;
            }
            else if (op.ActionType == "ajax")
            {
                // 对于 ajax 类型，使用 api 属性
                JObject api = amisApiHelper.CreateApiForMethod(method);
                if (api["url"] == null)
                {
                    api["url"] = op.Api;
                }

                if (op.IsBulkOperation)
                {
                    api["data"] = new JObject()
                    {
                        ["ids"] = "${ids|split}"
                    };
                }

                button["api"] = api;
                
                // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
                if (amisContext.HasTabCountService)
                {
                    button["reload"] = "tabCountService";
                }

                // 添加反馈弹框配置
                if (!string.IsNullOrEmpty(op.FeedbackTitleResourceKey) || (!string.IsNullOrEmpty(op.FeedbackTitle) && !op.FeedbackBodyTpl.IsNullOrWhiteSpace()))
                {
                    string feedbackTitle = GetLocalizedText(op.FeedbackTitleResourceKey, op.FeedbackTitleResourceType, op.FeedbackTitle);
                    
                    if (op.FeedbackBodyTpl.StartsWith("{"))
                    {
                        button["feedback"] = new JObject
                        {
                            ["title"] = feedbackTitle,
                            ["body"] = JObject.Parse(op.FeedbackBodyTpl)
                        };
                    }
                    else
                    {
                        button["feedback"] = new JObject
                        {
                            ["title"] = feedbackTitle,
                            ["body"] = op.FeedbackBodyTpl
                        };
                    }

                    // 优先使用新的DialogSizeEnum，如果没有则使用已弃用的FeedBackSize
                    if (op.DialogSize != DialogSize.MD)
                    {
                        button["feedback"]["size"] = op.DialogSizeString;
                    }
                }
            }
            //输入表单
            else if (op.ActionType == "form")
            {
                string title = label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                var formOptions = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(method.GetParameters(), method))
                };

                if (!string.IsNullOrEmpty(op.Data))
                {
                    formOptions["data"] = JsonConvert.DeserializeObject<JObject>(op.Data);
                }

                if (!op.InitApi.IsNullOrWhiteSpace())
                {
                    formOptions["initApi"] = op.InitApi;
                }

                JObject drawerBody = new()
                {
                    ["title"] = title,
                    ["size"] = op.DialogSizeString,
                    ["body"] = formOptions
                };

                // 处理自定义 actions 配置
                if (!string.IsNullOrEmpty(op.Actions))
                {
                    try
                    {
                        var customActions = JsonConvert.DeserializeObject<JArray>(op.Actions);
                        drawerBody["actions"] = customActions;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("解析 Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                    }
                }
                else if (op.Actions == "")
                {
                    // 空字符串表示不显示底部按钮
                    drawerBody["actions"] = new JArray();
                }
                // 如果 Actions 为 null，则使用默认按钮（不设置 actions 属性）

                button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody, visibleOn: op.VisibleOn, dialogSize: op.DialogSize);
                if (!string.IsNullOrEmpty(op.Redirect))
                {
                    button["redirect"] = op.Redirect;
                }
                
                // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
                if (amisContext.HasTabCountService)
                {
                    button["reload"] = "tabCountService";
                }
            }
            //动态表单
            else if (op.ActionType == "service")
            {
                // 对于 service 类型，创建一个 service 弹窗
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                button = CreateServiceDialogButton(label, route, op.DialogSize, op.Actions);
                
                // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
                if (amisContext.HasTabCountService)
                {
                    button["reload"] = "tabCountService";
                }
            }
            // CRUD对话框
            else if (op.ActionType == "crudDialog")
            {
                // 对于 crudDialog 类型，创建一个 CRUD 弹窗
                // 检查是否是 CrudDialogOperationAttribute
                if (op is CrudDialogOperationAttribute crudDialogOp)
                {
                    var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                    button = CreateCrudDialogButton(crudDialogOp, route, label);
                }
                else
                {
                    // 向后兼容：如果使用的是 OperationAttribute，尝试转换
                    throw new InvalidOperationException($"CrudDialog 操作类型必须使用 CrudDialogOperationAttribute，而不是 OperationAttribute。方法: {method.DeclaringType?.Name}.{method.Name}");
                }
            }
            //出参表单
            else if (op.ActionType == "return-form")
            {
                string title = label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                JObject drawerBody = new()
                {
                    ["title"] = title,
                    ["size"] = op.DialogSizeString,
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : null,
                        ["static"] = true,
                        ["initApi"] = new JObject
                        {
                            ["url"] = route.ApiPath,
                            ["method"] = route.HttpMethod
                        },
                        ["controls"] = new JArray(GetFormFieldsWithAiSupport(method.ReturnParameter.ParameterType?.GetUnderlyingDataType().GetProperties(), method.ReturnParameter.ParameterType?.GetUnderlyingDataType(), isReadOnly: true))
                    }
                };

                // 处理自定义 actions 配置
                if (!string.IsNullOrEmpty(op.Actions))
                {
                    try
                    {
                        var customActions = JsonConvert.DeserializeObject<JArray>(op.Actions);
                        drawerBody["actions"] = customActions;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("解析 Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                    }
                }
                else if (op.Actions == "")
                {
                    // 空字符串表示不显示底部按钮
                    drawerBody["actions"] = new JArray();
                }
                // 如果 Actions 为 null，则使用默认按钮（不设置 actions 属性）

                button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody, dialogSize: op.DialogSize);
            }
            // AI表单
            else if (op.ActionType == "aiForm")
            {
                string title = label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                button = CreateAiFormButton(op, title, route, method);
                
                // 如果存在 Tab Count Service，添加 reload 配置以刷新 Tab 数量
                if (amisContext.HasTabCountService)
                {
                    button["reload"] = "tabCountService";
                }
            }

            // 添加其他通用配置
            if (!string.IsNullOrEmpty(op.ConfirmTextResourceKey) || !string.IsNullOrEmpty(op.ConfirmText))
            {
                button["confirmText"] = GetLocalizedText(op.ConfirmTextResourceKey, op.ConfirmTextResourceType, op.ConfirmText);
            }

            if (op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                button["download"] = true;
            }

            if (!string.IsNullOrEmpty(op.VisibleOn))
            {
                button["visibleOn"] = op.VisibleOn;
            }

            // 优先使用自定义图标，如果没有则使用默认图标映射
            if (!string.IsNullOrEmpty(op.Icon))
            {
                button["icon"] = op.Icon;
            }
            else
            {
                CreateIcon(op.Label, button);
            }
            return button;
        }

        /// <summary>
        /// 获取行操作按钮（用于卡片和表格行）
        /// </summary>
        /// <returns>操作按钮列表</returns>
        public List<JObject> GetOperationButtons()
        {
            return GetCustomOperationsButtons<OperationAttribute>(false);
        }

        /// <summary>
        /// 创建一个Service弹窗按钮
        /// </summary>
        /// <param name="title">按钮和弹窗标题</param>
        /// <param name="route">API路由信息</param>
        /// <param name="dialogSize">对话框大小</param>
        /// <param name="customActions">自定义底部按钮配置</param>
        /// <returns>按钮配置对象</returns>
        public JObject CreateServiceDialogButton(string title, ApiRouteInfo route, DialogSize dialogSize = DialogSize.LG, string customActions = null)
        {
            ArgumentNullException.ThrowIfNull(route);

            string sizeString = ConvertDialogSizeToString(dialogSize);

            JObject serviceBody = new()
            {
                ["title"] = title,
                ["size"] = sizeString,
                ["closeOnEsc"] = true,
                ["closeOnOutside"] = false,
                ["showCloseButton"] = true,
                ["body"] = new JObject
                {
                    ["type"] = "service",
                    ["schemaApi"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod,
                        ["data"] = new JObject
                        {
                            ["&"] = "$$" // 传递当前行数据
                        }
                    },
                    ["body"] = "${body}" // 使用Service返回的body内容
                }
            };

            // 处理自定义 actions 配置
            if (!string.IsNullOrEmpty(customActions))
            {
                try
                {
                    var actions = JsonConvert.DeserializeObject<JArray>(customActions);
                    serviceBody["actions"] = actions;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("解析 Service Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                    // 使用默认关闭按钮
                    serviceBody["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "关闭",
                            ["actionType"] = "close",
                            ["level"] = "default"
                        }
                    };
                }
            }
            else if (customActions == "")
            {
                // 空字符串表示不显示底部按钮
                serviceBody["actions"] = new JArray();
            }
            else
            {
                // 默认显示关闭按钮
                serviceBody["actions"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "button",
                        ["label"] = "关闭",
                        ["actionType"] = "close",
                        ["level"] = "default"
                    }
                };
            }

            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = title,
                ["actionType"] = "dialog",
                ["dialog"] = serviceBody,
                ["level"] = "info"
            };

            CreateIcon(title, button);
            return button;
        }

        /// <summary>
        /// 创建 CRUD 对话框按钮
        /// </summary>
        /// <param name="operation">CRUD对话框操作特性配置</param>
        /// <param name="route">API路由信息</param>
        /// <param name="title">对话框标题</param>
        /// <returns>按钮配置对象</returns>
        public JObject CreateCrudDialogButton(CrudDialogOperationAttribute operation, ApiRouteInfo route, string title)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentNullException.ThrowIfNull(route);

            string sizeString = ConvertDialogSizeToString(operation.DialogSize);

            // 使用 CrudDialogHandler 生成 schema
            var schema = CrudDialogHandler.GenerateCrudDialogSchema(operation);

            // 构建 Service 弹窗（与 Service 类型类似，但使用生成的 schema）
            JObject serviceBody = new()
            {
                ["title"] = title,
                ["size"] = sizeString,
                ["closeOnEsc"] = true,
                ["closeOnOutside"] = false,
                ["showCloseButton"] = true,
                ["body"] = new JObject
                {
                    ["type"] = "service",
                    ["schemaApi"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod,
                        ["data"] = new JObject
                        {
                            ["&"] = "$$" // 传递当前行数据
                        }
                    },
                    ["body"] = "${body}" // 使用Service返回的body内容
                }
            };

            // 处理自定义 actions 配置
            if (!string.IsNullOrEmpty(operation.Actions))
            {
                try
                {
                    var actions = JsonConvert.DeserializeObject<JArray>(operation.Actions);
                    serviceBody["actions"] = actions;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("解析 CrudDialog Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                    // 使用默认关闭按钮
                    serviceBody["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "关闭",
                            ["actionType"] = "close",
                            ["level"] = "default"
                        }
                    };
                }
            }
            else if (operation.Actions == "")
            {
                // 空字符串表示不显示底部按钮
                serviceBody["actions"] = new JArray();
            }
            else
            {
                // 默认显示关闭按钮
                serviceBody["actions"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "button",
                        ["label"] = "关闭",
                        ["actionType"] = "close",
                        ["level"] = "default"
                    }
                };
            }

            return CreateButton(title, "dialog", dialogOrDrawer: serviceBody, dialogSize: operation.DialogSize);
        }

        /// <summary>
        /// 创建AI表单按钮
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="title">按钮标题</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>按钮配置对象</returns>

        /// <summary>
        /// 创建AI表单按钮（使用外部配置文件）
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="title">按钮标题</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>按钮配置对象</returns>
        private JObject CreateAiFormButton(OperationAttribute op, string title, ApiRouteInfo route, MethodInfo method)
        {
            // 获取表单字段
            var formFields = GetFormFieldsWithAiSupport(method.GetParameters(), method).ToList();

            // 创建对话框配置，包含Service组件
            var dialogBody = new JObject
            {
                ["title"] = title,
                ["size"] = op.DialogSizeString,
                ["closeOnEsc"] = false,
                ["closeOnOutside"] = false,
                ["showCloseButton"] = true,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["name"] = "aiForm",
                    ["title"] = "",
                    ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : new JObject(),
                    ["body"] = new JArray
                    {
                            // Service组件加载外部配置文件
                        new JObject
                        {
                            ["type"] = "service",
                            ["name"] = $"aiFormWizardService_{Guid.NewGuid():N}",
                                ["data"] = new JObject
                                {
                                            // 通过数据链设置配置参数
                                            ["aiFormConfig"] = new JObject
                                            {
                                                ["title"] = title,
                                                ["dialogSize"] = op.DialogSizeString,
                                                ["apiPath"] = route.ApiPath,
                                                ["httpMethod"] = route.HttpMethod,
                                                ["statusApi"] = op.StatusApi,
                                                ["pollingInterval"] = op.PollingInterval,
                                                ["formFields"] = new JArray(formFields),
                                                ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : new JObject(),
                                                ["crudComponentName"] = amisContext.CrudComponentName
                                            }
                                        },
                                        ["schemaApi"] = "js:/amis/configs/ai/ai-form-wizard.js"
                          }
                     }
                    },
                ["actions"] = CreateAiFormDialogActions(op)
            };

            return CreateButton(title, "dialog", dialogOrDrawer: dialogBody, visibleOn: op.VisibleOn, dialogSize: op.DialogSize);
        }

        /// <summary>
        /// 创建AI表单对话框的底部按钮配置
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <returns>按钮配置数组</returns>
        private JArray CreateAiFormDialogActions(OperationAttribute op)
        {
            // 处理自定义 actions 配置
            if (!string.IsNullOrEmpty(op.Actions))
            {
                try
                {
                    var customActions = JsonConvert.DeserializeObject<JArray>(op.Actions);
                    return customActions;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("解析 AI Form Actions 配置失败: {Error}, 使用默认配置", ex.Message);
                }
            }
            else if (op.Actions == "")
            {
                // 空字符串表示不显示底部按钮
                return new JArray();
            }

            // 默认不显示底部按钮（AI表单有自己的步骤控制）
            return new JArray();
        }


        /// <summary>
        /// 获取带AI支持的表单字段（从方法参数）
        /// </summary>
        /// <param name="parameters">方法参数</param>
        /// <param name="method">方法信息（可选，用于推断DTO类型）</param>
        /// <returns>表单字段配置列表</returns>
        private List<JObject> GetFormFieldsWithAiSupport(IEnumerable<ParameterInfo> parameters, MethodInfo method = null)
        {
            Console.WriteLine($"[ButtonHelper调试] GetFormFieldsWithAiSupport - 方法: {method?.Name ?? "NULL"}");

            if (parameters == null) return new List<JObject>();

            // 尝试从方法参数中推断输入DTO类型
            Type dtoType = null;
            if (method != null)
            {
                var paramTypes = parameters.Select(p => p.ParameterType.Name).ToArray();
                Console.WriteLine($"[ButtonHelper调试] 方法参数类型: [{string.Join(", ", paramTypes)}]");

                // 首先查找带有 AiFormFillAttribute 的参数类型
                var inputDtoParam = parameters.FirstOrDefault(p =>
                {
                    var aiAttr = p.ParameterType.GetCustomAttribute<AiFormFillAttribute>();
                    Console.WriteLine($"[ButtonHelper调试] 检查参数 {p.Name} (类型: {p.ParameterType.Name}) - AI特性: {(aiAttr != null ? "有" : "无")}");
                    return aiAttr != null;
                });

                if (inputDtoParam != null)
                {
                    dtoType = inputDtoParam.ParameterType;
                    Console.WriteLine($"[ButtonHelper调试] 找到AI输入DTO: {dtoType.Name}");
                }
                else
                {
                    // 如果没有 AI 特性，尝试查找带有 FormGroupAttribute 的复杂类型参数
                    // 这样可以确保表单项组功能仍然生效
                    var complexParam = parameters.FirstOrDefault(p =>
                        p.ParameterType.IsClass &&
                        p.ParameterType != typeof(string) &&
                        p.ParameterType.HasFormGroups());

                    if (complexParam != null)
                    {
                        dtoType = complexParam.ParameterType;
                        Console.WriteLine($"[ButtonHelper调试] 找到带表单项组的DTO: {dtoType.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"[ButtonHelper调试] 未找到带AI特性或表单项组的DTO类型");
                    }
                }
            }

            // 如果找到了DTO类型（有AI特性或有表单项组），使用带类型的方法以支持完整功能
            if (dtoType != null)
            {
                Console.WriteLine($"[ButtonHelper调试] 使用完整功能的表单字段生成，DTO类型: {dtoType.Name}");
                return formFieldHelper.GetAmisFormFieldsFromParameters(parameters, dtoType);
            }

            // 否则使用原有方法（不启用AI填充和表单项组）
            Console.WriteLine("[ButtonHelper调试] 使用原有方法（不启用AI填充和表单项组）");
            return formFieldHelper.GetAmisFormFieldsFromParameters(parameters);
        }

        /// <summary>
        /// 获取带AI支持的表单字段（从属性）
        /// </summary>
        /// <param name="properties">属性集合</param>
        /// <param name="dtoType">DTO类型（可选）</param>
        /// <param name="isReadOnly">是否为只读表单（查看表单）</param>
        /// <returns>表单字段配置列表</returns>
        private List<JObject> GetFormFieldsWithAiSupport(IEnumerable<PropertyInfo> properties, Type dtoType = null, bool isReadOnly = false)
        {
            if (properties == null) return new List<JObject>();

            // 如果是只读表单（查看表单），不启用AI填充功能，但仍然支持表单项组
            if (isReadOnly)
            {
                if (dtoType != null)
                {
                    // 使用支持表单项组但不启用AI的方法
                    return formFieldHelper.GetAmisFormFieldsFromPropertiesWithGroups(properties, dtoType);
                }
                return formFieldHelper.GetAmisFormFieldsFromProperties(properties);
            }

            Console.WriteLine($"[ButtonHelper调试] GetFormFieldsWithAiSupport(属性) - 原始DTO类型: {dtoType?.Name ?? "NULL"}");

            // 如果没有指定DTO类型，尝试从第一个属性推断
            if (dtoType == null)
            {
                var firstProperty = properties.FirstOrDefault();
                if (firstProperty != null)
                {
                    dtoType = firstProperty.DeclaringType;
                    Console.WriteLine($"[ButtonHelper调试] 从属性推断DTO类型: {dtoType?.Name ?? "NULL"}");
                }
            }

            if (dtoType != null)
            {
                var aiAttr = dtoType.GetCustomAttribute<AiFormFillAttribute>();
                Console.WriteLine($"[ButtonHelper调试] DTO类型 {dtoType.Name} - AI特性: {(aiAttr != null ? "有" : "无")}");

                if (aiAttr != null)
                {
                    Console.WriteLine($"[ButtonHelper调试] 使用AI支持的表单字段生成（属性），DTO类型: {dtoType.Name}");
                    return formFieldHelper.GetAmisFormFieldsFromProperties(properties, dtoType);
                }
            }

            // 否则使用原有方法（不启用AI填充），但仍然支持表单项组
            Console.WriteLine("[ButtonHelper调试] 使用原有方法（属性，不启用AI填充）");
            if (dtoType != null)
            {
                // 使用支持表单项组但不启用AI的方法
                return formFieldHelper.GetAmisFormFieldsFromPropertiesWithGroups(properties, dtoType);
            }
            return formFieldHelper.GetAmisFormFieldsFromProperties(properties);
        }
    }
}

