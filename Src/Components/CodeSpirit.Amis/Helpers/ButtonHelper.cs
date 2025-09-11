using CodeSpirit.Amis.Extensions;
using CodeSpirit.Amis.Form;
using CodeSpirit.Amis.Helpers.Dtos;
using CodeSpirit.Core.Attributes;
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
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(formParameters))
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
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(updateParameters))
                }
            };
            return CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
        }

        public JObject CreateDetailButton(ApiRouteInfo detailRoute, IEnumerable<PropertyInfo> detailPropertites)
        {
            string title = "查看";
            JArray controls = [];

            List<JObject> formFields = GetFormFieldsWithAiSupport(detailPropertites, null, isReadOnly: true);

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
                    if (_permissionService.HasPermission(_permissionService.GetPermissionCode(method)))
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

                // 添加反馈弹框配置
                if (!string.IsNullOrEmpty(op.FeedbackTitle) && !op.FeedbackBodyTpl.IsNullOrWhiteSpace())
                {
                    if (op.FeedbackBodyTpl.StartsWith("{"))
                    {
                        button["feedback"] = new JObject
                        {
                            ["title"] = op.FeedbackTitle,
                            ["body"] = JObject.Parse(op.FeedbackBodyTpl)
                        };
                    }
                    else
                    {
                        button["feedback"] = new JObject
                        {
                            ["title"] = op.FeedbackTitle,
                            ["body"] = op.FeedbackBodyTpl
                        };
                    }
                    if (!string.IsNullOrEmpty(op.FeedBackSize))
                    {
                        button["feedback"]["size"] = op.FeedBackSize;
                    }
                }
            }
            //输入表单
            else if (op.ActionType == "form")
            {
                string title = op.Label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                var formOptions = new JObject
                {
                    ["type"] = "form",
                    ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : null,
                    ["api"] = new JObject
                    {
                        ["url"] = route.ApiPath,
                        ["method"] = route.HttpMethod
                    },
                    ["controls"] = new JArray(GetFormFieldsWithAiSupport(method.GetParameters(), method))
                };

                if (!op.InitApi.IsNullOrWhiteSpace())
                {
                    formOptions["initApi"] = op.InitApi;
                }

                JObject drawerBody = new()
                {
                    ["title"] = title,
                    ["size"] = "lg",
                    ["body"] = formOptions
                };

                button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody, visibleOn: op.VisibleOn);
                if (!string.IsNullOrEmpty(op.Redirect))
                {
                    button["redirect"] = op.Redirect;
                }
            }
            //动态表单
            else if (op.ActionType == "service")
            {
                // 对于 service 类型，创建一个 service 弹窗
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                button = CreateServiceDialogButton(op.Label, route);
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
                        ["controls"] = new JArray(GetFormFieldsWithAiSupport(method.ReturnParameter.ParameterType?.GetUnderlyingDataType().GetProperties(), method.ReturnParameter.ParameterType?.GetUnderlyingDataType(), isReadOnly: true))
                    }
                };
                button = CreateButton(title, "dialog", dialogOrDrawer: drawerBody);
            }
            // AI表单
            else if (op.ActionType == "aiForm")
            {
                string title = op.Label;
                var route = apiRouteHelper.GetApiRouteInfoForMethod(method);
                button = CreateAiFormButton(op, title, route, method);
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
        /// <returns>按钮配置对象</returns>
        public JObject CreateServiceDialogButton(string title, ApiRouteInfo route)
        {
            ArgumentNullException.ThrowIfNull(route);

            JObject serviceBody = new()
            {
                ["title"] = title,
                ["size"] = "lg",
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
                },
                ["actions"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "button",
                        ["label"] = "关闭",
                        ["actionType"] = "close",
                        ["level"] = "default"
                    }
                }
            };

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
        /// 创建AI表单按钮
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="title">按钮标题</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>按钮配置对象</returns>
        /// <summary>
        /// 创建AI步骤向导（包含所有步骤和内容）
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>步骤向导配置</returns>
        private JObject CreateAiStepsWizard(OperationAttribute op, ApiRouteInfo route, MethodInfo method)
        {
            return new JObject
            {
                ["type"] = "wizard",
                ["id"] = "aiSteps",
                ["name"] = "aiSteps",
                ["className"] = "mb-4",
                ["mode"] = "horizontal",
                ["actionNextLabel"] = "下一步",
                ["actionPrevLabel"] = "上一步",
                ["actionFinishLabel"] = "完成",
                ["initApi"] = false, // 禁用自动初始化
                //["data"] = new JObject
                //{
                //    ["&"] = "$$",
                //    ["taskId"] = ""
                //},
                ["steps"] = new JArray
                {
                    // 第一步：填写表单
                    new JObject
                    {
                        ["title"] = "填写表单",
                        ["description"] = "填写AI生成所需的参数信息",
                        ["body"] = new JArray { CreateAiFormStep(op, route, method) }
                    },
                    // 第二步：AI处理中
                    new JObject
                    {
                        ["title"] = "AI处理中",
                        ["description"] = "AI正在分析您的需求并生成内容",
                        ["body"] = new JArray { CreateAiProgressStep(op, route) }
                    },
                    // 第三步：查看结果
                    new JObject
                    {
                        ["title"] = "查看结果",
                        ["description"] = "查看AI生成的结果内容",
                        ["body"] = new JArray { CreateAiResultStep(op, route) }
                    }
                },
                ["onEvent"] = new JObject
                {
                    // 任务开始时自动切换到第二步
                    ["taskStarted"] = new JObject
                    {
                        ["actions"] = new JArray
                        {
                            new JObject
                            {
                                ["actionType"] = "goto-step",
                                ["args"] = new JObject { ["step"] = 2 }
                            }
                        }
                    },
                    // 任务完成时自动切换到第三步
                    ["taskCompleted"] = new JObject
                    {
                        ["actions"] = new JArray
                        {
                            new JObject
                            {
                                ["actionType"] = "goto-step",
                                ["args"] = new JObject { ["step"] = 3 }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 创建AI表单步骤内容
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>表单步骤配置</returns>
        private JObject CreateAiFormStep(OperationAttribute op, ApiRouteInfo route, MethodInfo method)
        {
            // 获取表单字段
            var formFields = GetFormFieldsWithAiSupport(method.GetParameters(), method).ToList();

            return new JObject
            {
                ["type"] = "container",
                ["id"] = "aiFormStep",
                ["className"] = "p-3",
                ["body"] = new JArray(
                    formFields
                        .Append(new JObject
                        {
                            ["type"] = "divider",
                            ["className"] = "my-3"
                        })
                        .Append(CreateAiFormSubmitButton(op, route))
                )
            };
        }

        /// <summary>
        /// 创建AI进度步骤内容
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <returns>进度步骤配置</returns>
        private JObject CreateAiProgressStep(OperationAttribute op, ApiRouteInfo route)
        {
            return new JObject
            {
                ["type"] = "container",
                ["id"] = "aiProgressStep",
                ["className"] = "p-3",
                ["body"] = new JArray
                {
                    // TaskId显示控件
                    new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = "<div class='alert alert-info mb-3'><i class='fa fa-info-circle'></i> 任务ID: ${taskId}</div>"
                    },
                    // 进度状态显示
                    new JObject
                    {
                        ["type"] = "alert",
                        ["level"] = "info",
                        ["body"] = new JObject
                        {
                            ["type"] = "tpl",
                            ["tpl"] = "<strong>当前状态：</strong>${statusText || '准备中...'}<br/><strong>进度：</strong>${progress || 0}%"
                        }
                    },
                    // 分隔线
                    new JObject
                    {
                        ["type"] = "divider",
                        ["className"] = "my-3"
                    },
                    // 日志标题
                    new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = "<h5><i class='fa fa-list-alt'></i> 生成日志</h5>",
                        ["className"] = "mb-2"
                    },
                    // 实时日志显示
                    CreateAiLogService(op, route)
                }
            };
        }

        /// <summary>
        /// 创建AI结果步骤内容
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <returns>结果步骤配置</returns>
        private JObject CreateAiResultStep(OperationAttribute op, ApiRouteInfo route)
        {
            return new JObject
            {
                ["type"] = "service",
                ["name"] = "aiResult",
                ["id"] = "aiResultStep",
                ["interval"] = op.PollingInterval,
                ["silentPolling"] = true,
                ["stopAutoRefreshWhen"] = "progress == 100",
                ["initFetch"] = false, // 禁用初始加载
                ["api"] = new JObject
                {
                    ["url"] = !string.IsNullOrEmpty(op.StatusApi) ? op.StatusApi : $"{route.ApiPath}/status",
                    ["method"] = "get",
                    ["data"] = new JObject
                    {
                        ["taskId"] = "${taskId}"
                    }
                },
                ["onEvent"] = new JObject
                {
                    ["aiTaskCompleted"] = new JObject
                    {
                        ["actions"] = new JArray
                        {
                            new JObject
                            {
                                ["actionType"] = "setValue",
                                ["args"] = new JObject
                                {
                                    ["value"] = new JObject
                                    {
                                        ["aiTaskCompleted"] = true
                                    }
                                }
                            },
                            new JObject
                            {
                                ["actionType"] = "setValue",
                                ["args"] = new JObject
                                {
                                    ["value"] = new JObject
                                    {
                                        ["aiTaskCompleted"] = true
                                    }
                                }
                            },
                            // 更新步骤指示器到第三步（查看结果）
                            new JObject
                            {
                                ["actionType"] = "goto-step",
                                ["componentId"] = "aiSteps",
                                ["args"] = new JObject
                                {
                                    ["step"] = 3
                                }
                            }
                        }
                    }
                },
                ["body"] = CreateAiResultPanelBody()
            };
        }


        /// <summary>
        /// 创建AI表单提交按钮及其事件处理
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <returns>提交按钮配置</returns>
        private JObject CreateAiFormSubmitButton(OperationAttribute op, ApiRouteInfo route)
        {
            return new JObject
            {
                ["type"] = "button",
                ["label"] = "开始生成",
                ["level"] = "primary",
                ["actionType"] = "ajax",
                ["icon"] = "fa fa-rocket",
                ["api"] = new JObject
                {
                    ["url"] = route.ApiPath,
                    ["method"] = route.HttpMethod,
                    ["data"] = new JObject
                    {
                        ["&"] = "$$"
                    }
                },
                ["reload"] = "aiLogsService?taskId=${taskId}",
                ["onEvent"] = new JObject
                {
                    ["click"] = new JObject
                    {
                        ["actions"] = CreateAiFormPostSubmitActions(op, route)
                    }
                }
            };
        }

        /// <summary>
        /// 创建AI表单提交后的动作序列（不包含AJAX提交，因为已移至按钮的api属性）
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <returns>动作数组</returns>
        private JArray CreateAiFormPostSubmitActions(OperationAttribute op, ApiRouteInfo route)
        {
            return new JArray
            {
                //new JObject
                //{
                //    ["actionType"] = "ajax",
                //    ["api"] = new JObject
                //    {
                //        ["url"] = route.ApiPath,
                //        ["method"] = route.HttpMethod,
                //        ["data"] = new JObject
                //        {
                //            ["&"] = "$$"
                //        },
                //        ["silent"] = true
                //    },
                //    ["responseData"] = new JObject
                //    {
                //        ["taskId"] = "${taskId}"
                //    }
                //},
                //new JObject
                //{
                //    ["actionType"] = "setValue",
                //    ["componentId"] = "aiSteps",
                //    ["args"] = new JObject
                //    {
                //        ["value"] = new JObject
                //        {
                //            ["taskId"] = "${__rendererData.taskId}"
                //        }
                //    }
                //},
                // 更新步骤指示器到第二步（AI处理中）
                new JObject
                {
                    ["actionType"] = "next",
                    ["componentId"] = "aiSteps",
                }
            };
        }

        /// <summary>
        /// 创建AI日志服务组件
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="route">API路由信息</param>
        /// <returns>日志服务配置</returns>
        private JObject CreateAiLogService(OperationAttribute op, ApiRouteInfo route)
        {
            return new JObject
            {
                ["type"] = "service",
                ["id"] = "aiLogsService",
                ["name"] = "aiLogsService",
                ["interval"] = op.PollingInterval,
                ["silentPolling"] = true,
                ["stopAutoRefreshWhen"] = "${aiTaskCompleted}",
                ["initFetch"] = true,
                ["api"] = new JObject
                {
                    ["url"] = !string.IsNullOrEmpty(op.StatusApi) ? op.StatusApi : $"{route.ApiPath}/status",
                    ["method"] = "get",
                    ["data"] = new JObject
                    {
                        ["taskId"] = "${taskId}"
                    },
                    ["sendOn"] = "taskId != ''"
                },
                ["onEvent"] = new JObject
                {
                    //["init"] = new JObject
                    //{
                    //    ["actions"] = new JArray
                    //    {
                    //        new JObject
                    //        {
                    //            ["actionType"] = "toast",
                    //            ["args"] = new JObject
                    //            {
                    //                ["msg"] = "init...."
                    //            }
                    //        }
                    //    }
                    //},
                    ["aiTaskCompleted"] = new JObject
                    {
                        ["actions"] = new JArray
                        {
                            new JObject
                            {
                                ["actionType"] = "setValue",
                                ["args"] = new JObject
                                {
                                    ["value"] = new JObject
                                    {
                                        ["aiTaskCompleted"] = true
                                    }
                                }
                            }
                        }
                    }
                },
                ["body"] = new JObject
                {
                    ["type"] = "log",
                    ["id"] = "aiLogs",
                    ["height"] = 300,
                    ["source"] = "${logs}",
                    ["autoScroll"] = true,
                    ["encoding"] = "utf-8"
                }
            };
        }


        /// <summary>
        /// 创建AI结果面板内容
        /// </summary>
        /// <returns>结果面板内容配置</returns>
        private JArray CreateAiResultPanelBody()
        {
            return new JArray
            {
                // 状态展示
                new JObject
                {
                    ["type"] = "alert",
                    ["level"] = "${status == 'completed' ? 'success' : (status == 'failed' ? 'danger' : 'info')}",
                    ["body"] = new JObject
                    {
                        ["type"] = "tpl",
                        ["tpl"] = "<strong>状态：</strong>${statusText}<br/><strong>进度：</strong>${progress}%<br/><strong>耗时：</strong>${elapsedTime}"
                    }
                },
                // 结果展示
                new JObject
                {
                    ["type"] = "container",
                    ["visibleOn"] = "${status == 'completed'}",
                    ["body"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "divider"
                        },
                        new JObject
                        {
                            ["type"] = "tpl",
                            ["tpl"] = "<h4>生成结果</h4>"
                        },
                        new JObject
                        {
                            ["type"] = "json",
                            ["name"] = "result",
                            ["source"] = "${result}",
                            ["levelExpand"] = 2
                        }
                    }
                },
                // 操作按钮
                new JObject
                {
                    ["type"] = "container",
                    ["visibleOn"] = "${status == 'completed'}",
                    ["body"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "button-group",
                            ["buttons"] = new JArray
                            {
                                new JObject
                                {
                                    ["type"] = "button",
                                    ["label"] = "查看详情",
                                    ["level"] = "primary",
                                    ["actionType"] = "link",
                                    ["link"] = "${detailUrl}",
                                    ["blank"] = true,
                                    ["visibleOn"] = "${detailUrl}"
                                },
                                new JObject
                                {
                                    ["type"] = "button",
                                    ["label"] = "重新生成",
                                    ["level"] = "default",
                                    ["actionType"] = "custom",
                                    ["script"] = "window.resetAiForm && window.resetAiForm();"
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 创建AI表单按钮（重构后的简洁版本）
        /// </summary>
        /// <param name="op">操作特性</param>
        /// <param name="title">按钮标题</param>
        /// <param name="route">API路由信息</param>
        /// <param name="method">方法信息</param>
        /// <returns>按钮配置对象</returns>
        private JObject CreateAiFormButton(OperationAttribute op, string title, ApiRouteInfo route, MethodInfo method)
        {
            // 创建AI表单弹窗配置
            JObject aiFormDialog = new()
            {
                ["title"] = title,
                ["size"] = "lg", // 使用更大的弹窗
                ["closeOnEsc"] = false,
                ["closeOnOutside"] = false,
                ["showCloseButton"] = true,
                ["name"] = "aiFormDialog",
                ["body"] = new JObject
                {
                    ["type"] = "form",
                    ["name"] = "aiForm",
                    ["title"] = "",
                    ["data"] = !string.IsNullOrEmpty(op.Data) ? JsonConvert.DeserializeObject<JObject>(op.Data) : null,
                    ["body"] = new JArray
                    {
                        // AI步骤向导（包含所有步骤和内容）
                        CreateAiStepsWizard(op, route, method)
                    }
                },
                ["actions"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "button",
                        ["label"] = "关闭",
                        ["actionType"] = "close",
                        ["level"] = "default"
                    }
                }
            };

            // 如果配置了成功跳转，在完成时自动跳转
            if (!string.IsNullOrEmpty(op.SuccessRedirect))
            {
                aiFormDialog["onEvent"] = new JObject
                {
                    ["aiTaskCompleted"] = new JObject
                    {
                        ["actions"] = new JArray
                        {
                            new JObject
                            {
                                ["actionType"] = "url",
                                ["url"] = op.SuccessRedirect
                            }
                        }
                    }
                };
            }

            return CreateButton(title, "dialog", dialogOrDrawer: aiFormDialog, visibleOn: op.VisibleOn);
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

                // 查找带有 AiFormFillAttribute 的参数类型
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

                if (dtoType == null)
                {
                    Console.WriteLine($"[ButtonHelper调试] 未找到合适的输入DTO类型");
                }
            }

            // 只有当找到输入DTO类型时才启用AI支持
            if (dtoType != null)
            {
                Console.WriteLine($"[ButtonHelper调试] 使用AI支持的表单字段生成，DTO类型: {dtoType.Name}");
                return formFieldHelper.GetAmisFormFieldsFromParameters(parameters, dtoType);
            }

            // 否则使用原有方法（不启用AI填充）
            Console.WriteLine("[ButtonHelper调试] 使用原有方法（不启用AI填充）");
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

            // 如果是只读表单（查看表单），不启用AI填充功能
            if (isReadOnly)
            {
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

            // 否则使用原有方法（不启用AI填充）
            Console.WriteLine("[ButtonHelper调试] 使用原有方法（属性，不启用AI填充）");
            return formFieldHelper.GetAmisFormFieldsFromProperties(properties);
        }
    }
}

