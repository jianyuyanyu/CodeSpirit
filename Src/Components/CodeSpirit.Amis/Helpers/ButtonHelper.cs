using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis.Helpers
{
    public class ButtonHelper
    {
        private readonly IHasPermissionService _permissionService;
        private readonly AmisContext amisContext;
        private readonly ApiRouteHelper apiRouteHelper;
        private readonly AmisApiHelper amisApiHelper;
        private readonly FormFieldHelper formFieldHelper;

        public ButtonHelper(IHasPermissionService permissionService, AmisContext amisContext, ApiRouteHelper apiRouteHelper, AmisApiHelper amisApiHelper, FormFieldHelper formFieldHelper)
        {
            _permissionService = permissionService;
            this.amisContext = amisContext;
            this.apiRouteHelper = apiRouteHelper;
            this.amisApiHelper = amisApiHelper;
            this.formFieldHelper = formFieldHelper;
        }

        // 创建一个通用的按钮模板
        private JObject CreateButton(string label, string actionType, JObject dialogOrDrawer = null, JObject api = null, string confirmText = null, bool? download = null, string visibleOn = null)
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

                _ => null // 其他情况不设置图标
            };

            if (icon != null)
            {
                button["icon"] = icon;
            }
        }

        // 创建"新增"按钮
        public JObject CreateHeaderButton(string title = "新增", ApiRouteInfo route = null, IEnumerable<ParameterInfo> formParameters = null, string size = null)
        {
            JObject dialogBody = new()
            {
                ["title"] = title,
                ["size"] = size,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromParameters(formParameters))
                },
            };

            return CreateButton(title, "dialog", dialogOrDrawer: dialogBody);
        }

        // 创建"编辑"按钮
        public JObject CreateEditButton(ApiRouteInfo updateRoute, IEnumerable<ParameterInfo> updateParameters)
        {
            string title = "编辑";
            JObject drawerBody = new()
            {
                ["title"] = title,
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["api"] = new JObject
                    {
                        ["url"] = updateRoute.ApiPath,
                        ["method"] = updateRoute.HttpMethod
                    },
                    ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromParameters(updateParameters))
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        public JObject CreateDetailButton(ApiRouteInfo detailRoute, IEnumerable<PropertyInfo> detailPropertites)
        {
            string title = "查看";
            JArray controls = [];

            List<JObject> formFields = formFieldHelper.GetAmisFormFieldsFromProperties(detailPropertites);

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

            JObject drawerBody = new()
            {
                ["title"] = title,
                ["size"] = "lg",
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
                    ["horizontal"] = new JObject
                    {
                        ["left"] = 3,
                        ["right"] = 9
                    },
                    ["static"] = true,
                    ["submitText"] = "",
                    ["actions"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button",
                            ["label"] = "关闭",
                            ["actionType"] = "close"
                        }
                    }
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        // 创建"删除"按钮
        public JObject CreateDeleteButton(ApiRouteInfo deleteRoute)
        {
            JObject api = new()
            {
                ["url"] = deleteRoute.ApiPath,
                ["method"] = deleteRoute.HttpMethod
            };

            return CreateButton("删除", "ajax", api: api, confirmText: "确定要删除吗？");
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
                TOperation op = method.GetCustomAttribute<TOperation>(inherit: false);
                if (op != null && !isHeader && op is HeaderOperationAttribute)
                {
                    continue;
                }

                if (op != null && op.IsBulkOperation == isBulkOperation)
                {
                    // 为每个操作方法创建按钮
                    JObject button = CreateCustomOperationButton(op, method);

                    // Add redirect configuration if specified
                    if (op.ActionType == "ajax" && !string.IsNullOrEmpty(op.Redirect))
                    {
                        button["redirect"] = op.Redirect;
                    }
                    buttons.Add(button);
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
            return GetCustomOperationsButtons<HeaderOperationAttribute>(isBulkOperation: false, isHeader: true);
        }

        // 创建自定义操作按钮
        public JObject CreateCustomOperationButton(OperationAttribute op, MethodInfo method)
        {
            JObject button = new()
            {
                ["type"] = "button",
                ["label"] = op.Label,
                ["actionType"] = op.ActionType
            };

            // 优先使用自定义图标，如果没有则使用默认图标映射
            if (!string.IsNullOrEmpty(op.Icon))
            {
                button["icon"] = op.Icon;
            }
            else
            {
                CreateIcon(op.Label, button);
            }

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
            }
            //输入表单
            else if (op.ActionType == "form")
            {
                string title = op.Label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                JObject drawerBody = new()
                {
                    ["title"] = title,
                    ["size"] = "lg",
                    ["body"] = new JObject
                    {
                        ["type"] = "form",
                        ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : null,
                        ["api"] = new JObject
                        {
                            ["url"] = route.ApiPath,
                            ["method"] = route.HttpMethod
                        },
                        ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromParameters(method.GetParameters()))
                    }
                };
                
                button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
                if (!string.IsNullOrEmpty(op.VisibleOn))
                {
                    button["visibleOn"] = op.VisibleOn;
                }
                return button;
            }
            //动态表单
            else if (op.ActionType == "service")
            {
                // 对于 service 类型，创建一个 service 弹窗
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                return CreateServiceDialogButton(op.Label, route);
            }
            //出参表单
            else if (op.ActionType == "return-form")
            {
                string title = op.Label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                JObject drawerBody = new()
                {
                    ["title"] = title,
                    ["size"] = "lg",
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
                        ["controls"] = new JArray(formFieldHelper.GetAmisFormFieldsFromProperties(method.ReturnParameter.ParameterType?.GetUnderlyingDataType().GetProperties()))
                    }
                };
                return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
            }

            // 添加其他通用配置
            if (!string.IsNullOrEmpty(op.ConfirmText))
            {
                button["confirmText"] = op.ConfirmText;
            }

            if (op.ActionType.Equals("download", StringComparison.OrdinalIgnoreCase))
            {
                button["download"] = true;
            }

            if (!string.IsNullOrEmpty(op.VisibleOn))
            {
                button["visibleOn"] = op.VisibleOn;
            }

            return button;
        }

        /// <summary>
        /// 创建一个Service弹窗按钮
        /// </summary>
        /// <param name="title">按钮和弹窗标题</param>
        /// <param name="route">API路由信息</param>
        /// <returns>按钮配置对象</returns>
        public JObject CreateServiceDialogButton(string title, ApiRouteInfo route)
        {
            ArgumentNullException.ThrowIfNull(route);

            JObject serviceBody = new()
            {
                ["title"] = title,
                ["size"] = "lg",
                ["body"] = new JObject
                {
                    ["type"] = "service",
                    ["schemaApi"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["body"] = "${body}" // 使用Service返回的body内容
                }
            };

            return CreateButton(title, "dialog", dialogOrDrawer: serviceBody);
        }
    }
}
