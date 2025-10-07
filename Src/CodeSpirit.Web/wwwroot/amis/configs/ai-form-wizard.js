// AI表单向导配置JS文件
// 支持AI智能表单生成的三步向导流程
// 通过api.context获取配置参数

// 获取API上下文和查询参数
console.log('AI Form Wizard API:', api);

// 从api.context中获取配置参数
const config = api.context?.aiFormConfig || {};
console.log('AI Form Config:', config);
console.log('API Query:', api.query);

// 解构配置参数，提供默认值
const {
    title = "AI智能生成",
    dialogSize = "xl",
    apiPath = "",
    httpMethod = "post",
    statusApi = "",
    pollingInterval = 2000,
    formFields = [],
    data = {},
    actions = []
} = config;

/**
 * 创建AI表单步骤内容
 * @returns {Object} 表单步骤配置
 */
function createAiFormStep() {
    
    // 优化AI表单字段配置，处理AI填充相关的API数据传递
    formFields.forEach(field => {
        // 检查字段是否配置了AI填充功能
        if (field.addOn?.api?.data?.aifill) {
            // 设置数据传递配置，使用AMIS的数据引用语法
            // "&":"$$" 表示将当前作用域的所有数据传递给API
            field.addOn.api.data = { "&": "$$" };
            field.addOn.api.responseData = { "&": "$$" };
        }
    });

    console.log('createAiFormStep', formFields);

    return {
        type: "container",
        id: "aiFormStep",
        className: "p-3",
        body: [
            ...formFields,
            {
                type: "divider",
                className: "my-3"
            },
            createAiFormSubmitButton()
        ]
    };
}

/**
 * 创建AI表单提交按钮
 * @returns {Object} 提交按钮配置
 */
function createAiFormSubmitButton() {
    return {
        type: "button",
        label: "开始生成",
        level: "primary",
        actionType: "ajax",
        icon: "fa fa-rocket",
        className: "antd-btn antd-btn-primary",
        api: {
            url: apiPath,
            method: httpMethod,
            data: {"&":"$$"},
            responseData: {"&":"$$"}
        },
        reload: "aiLogsService",
        onEvent: {
            click: {
                actions: [
                    // 更新步骤指示器到第二步（AI处理中）
                    {
                        actionType: "next",
                        componentId: "aiSteps"
                    }
                ]
            }
        }
    };
}

/**
 * 创建AI进度步骤内容
 * @returns {Object} 进度步骤配置
 */
function createAiProgressStep() {
    return {
        type: "container",
        id: "aiProgressStep",
        className: "p-3",
        body: [
            // TaskId显示控件
            {
                type: "tpl",
                tpl: "<div class='alert alert-info mb-3 antd-alert antd-alert-info'><i class='fa fa-info-circle'></i> 任务ID: ${taskId}</div>"
            },
            // 进度状态显示
            {
                type: "container",
                className: "mb-3",
                body: [
                    // 状态文本
                    {
                        type: "tpl",
                        tpl: "<div class='mb-2'><strong>当前状态：</strong>${statusText || '准备中...'}</div>"
                    },
                    // 进度条
                    {
                        type: "progress",
                        value: "${progress || 0}",
                        showLabel: true,
                        strokeWidth: 10,
                        animate: true,
                        className: "antd-progress"
                    }
                ]
            },
            // 分隔线
            {
                type: "divider",
                className: "my-3"
            },
            // 日志标题
            {
                type: "tpl",
                tpl: "<h5><i class='fa fa-list-alt'></i> 处理日志</h5>"
            },
            // AI日志服务
            createAiLogService()
        ]
    };
}

/**
 * 创建AI日志服务组件
 * @returns {Object} 日志服务配置
 */
function createAiLogService() {
    return {
        type: "service",
        id: "aiLogsService",
        name: "aiLogsService",
        interval: pollingInterval,
        silentPolling: true,
        stopAutoRefreshWhen: "progress == 100",
        initFetch: true,
        api: {
            url: `${statusApi}/\${taskId}`,
            method: "get",
            sendOn: "taskId != null && taskId != ''"
        },
        onEvent: {
            fetchInited: {
                actions: [
                    // 当进度达到100%时，自动跳转到下一步
                    {
                        expression: "progress == 100",
                        actionType: "next",
                        componentId: "aiSteps"
                    }
                ]
            }
        },
        body: [
            // 使用AMIS的log组件
            {
                type: "log",
                height: 300,
                source: "${logs}",
                autoScroll: true,
                placeholder: "暂无日志信息",
                className: "antd-log"
            }
        ]
    };
}

/**
 * 创建AI结果步骤内容
 * @returns {Object} 结果步骤配置
 */
function createAiResultStep() {
    return {
        type: "service",
        name: "aiResult",
        id: "aiResultStep",
        interval: pollingInterval,
        silentPolling: true,
        stopAutoRefreshWhen: "progress == 100",
        initFetch: false, // 禁用初始加载
        api: {
            url: `${statusApi}/\${taskId}`,
            method: "get",
            sendOn: "taskId != null && taskId != ''"
        },
        body: createAiResultPanelBody()
    };
}

/**
 * 创建AI结果面板内容
 * @returns {Array} 结果面板配置数组
 */
function createAiResultPanelBody() {
    return [
        // 状态展示
        {
            type: "alert",
            level: "${status == 2 ? 'success' : (status == 'failed' ? 'danger' : 'info')}",
            className: "antd-alert mb-3",
            showIcon: true,
            body: {
                type: "tpl",
                tpl: "<strong>状态：</strong>${statusText}<br/><strong>进度：</strong>${progress}%<br/><strong>耗时：</strong>${elapsedTime}"
            }
        },
        // 结果展示
        {
            type: "container",
            visibleOn: "${status == 2}",
            body: [
                {
                    type: "divider"
                },
                {
                    type: "tpl",
                    tpl: "<h4><i class='fa fa-check-circle text-success'></i> 生成结果</h4>"
                },
                {
                    type: "json",
                    name: "result",
                    source: "${result}",
                    levelExpand: 2,
                    className: "antd-json-view"
                },
                // 如果没有结果数据的提示
                {
                    type: "alert",
                    level: "warning",
                    body: "暂无结果数据",
                    visibleOn: "!result",
                    className: "antd-alert antd-alert-warning"
                }
            ]
        }
    ];
}

/**
 * 创建向导步骤配置
 * @returns {Array} 步骤配置数组
 */
function createWizardSteps() {
    return [
        // 第一步：填写表单
        {
            title: "填写表单",
            description: "填写AI生成所需的参数信息",
            body: [createAiFormStep()],
            jumpableOn: "${!taskId}",
            actions: []
        },
        // 第二步：AI处理中
        {
            title: "AI处理中",
            description: "AI正在分析您的需求并生成内容",
            body: [createAiProgressStep()],
            actions: []
        },
        // 第三步：查看结果
        {
            title: "查看结果",
            description: "查看AI生成的结果内容",
            body: [createAiResultStep()],
            actions: [
                {
                    type: "button",
                    level: "primary",
                    label: "完成",
                    actionType: "close",
                    className: "antd-btn antd-btn-primary",
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: "reload",
                                    componentName: config.crudComponentName
                                }
                            ]
                        }
                    }
                }
            ]
        }
    ];
}

// 直接返回Wizard组件配置（Service组件会将其放入对话框中）
return {
    type: "wizard",
    id: "aiSteps",
    name: "aiSteps",
    className: "mb-4 antd-steps",
    mode: "horizontal",
    actionNextLabel: "下一步",
    actionPrevLabel: "上一步",
    actionFinishLabel: "完成",
    initApi: false, // 禁用自动初始化
    steps: createWizardSteps(),
    // 添加步骤验证逻辑
    stepValidation: true,
    bulkSubmit: false,
    readOnly: false,
    // 添加响应式支持
    modeOn: "window.innerWidth < 768 ? 'vertical' : 'horizontal'"
};
