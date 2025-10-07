// 增强导入组件配置JS文件
// 使用Wizard组件优化批量导入流程布局
// 支持AntD主题的增强批量导入组件
// 通过api.context获取配置参数

// 可以通过api变量获取当前请求的参数
console.log('Enhanced Import API:', api);

// 从api.context中获取配置参数
const config = api.context?.enhancedImportConfig || {};
console.log('Enhanced Import Config:', config);
console.log('API Query:', api.query);

// 解构配置参数，提供默认值
const {
    fieldName = "importData",
    itemType = "ImportDto",
    maxLength = 1000,
    showTemplateDownload = true,
    showImportResult = true,
    templateDownloadText = "下载导入模板",
    importButtonText = "开始导入",
    templateDownloadApi = "",
    submitApi = "",
    label = "批量导入数据",
    required = false,
    createInputTable = true,
    columns = []
} = config;

// 构建Wizard步骤配置
const wizardSteps = [];

console.debug("showTemplateDownload:", showTemplateDownload);
// 第一步：模板下载（如果启用）
if (showTemplateDownload) {
    wizardSteps.push({
        title: "下载模板",
        subTitle: "获取导入模板文件",
        body: [
            {
                type: "alert",
                level: "info",
                body: `请先下载导入模板，按照模板格式填写${label}后再进行上传。`,
                className: "antd-alert antd-alert-info m-b-md"
            },
            {
                type: "flex",
                justify: "center",
                items: [
                    {
                        type: "button",
                        label: templateDownloadText,
                        level: "primary",
                        size: "lg",
                        icon: "fa fa-download",
                        actionType: "download",
                        api: templateDownloadApi,
                        className: "antd-btn antd-btn-primary antd-btn-lg"
                    }
                ]
            },
            {
                type: "divider",
                className: "m-t-lg m-b-lg"
            },
            {
                type: "alert",
                level: "warning",
                body: `注意事项：<br/>• 请严格按照模板格式填写数据<br/>• 最多支持${maxLength}条记录<br/>• 确保数据格式正确，避免导入失败`,
                className: "antd-alert antd-alert-warning"
            }
        ]
    });
}

// 第二步：文件上传
wizardSteps.push({
    title: "上传文件",
    subTitle: "选择要导入的Excel文件",
    body: [
        {
            type: "alert",
            level: "info",
            body: "请选择填写好数据的Excel文件进行上传，系统将自动解析文件内容。",
            className: "antd-alert antd-alert-info m-b-md"
        },
        {
            type: "input-excel",
            name: fieldName,
            label: "选择Excel文件",
            placeholder: "请拖拽Excel文件到当前区域或点击选择文件",
            required: required,
            className: "antd-upload",
            autoFill: {
                // [fieldName]: "${items}"
                [fieldName]: "${importData}"
            },
            // 添加调试信息
            onEvent: {
                change: {
                    actions: [
                        {
                            actionType: "toast",
                            args: {
                                msg: `文件上传成功，解析到 \${event.data.importData ? event.data.importData.length : 0} 条记录`,
                                level: "success"
                            },
                            expression: "${event.data.importData && event.data.importData.length > 0}"
                        }
                    ]
                }
            }
        },
        {
            type: "alert",
            level: "success",
            body: `<i class="fa fa-check-circle"></i> 文件上传成功，解析到 \${${fieldName}.length} 条记录`,
            visibleOn: `${fieldName}.length > 0`,
            className: "antd-alert antd-alert-success m-t-md"
        }
    ]
});

console.debug("createInputTable:", createInputTable);
// 第三步：数据预览（如果启用输入表格）
if (createInputTable) {
    wizardSteps.push({
        title: "数据预览",
        subTitle: "预览和编辑导入数据",
        api: {
            url: submitApi,
            method: "post",
            data: {
                "&": "$$",
                [fieldName]: `\${${fieldName}}`
            },
            requestAdaptor: function (api) {
                // 从api.data中获取导入数据
                const importData = api.data[fieldName];
                console.log('提交导入数据:', importData);
                api.data = {};
                api.data[fieldName] = importData;
                return api;
            },
            adaptor: function (payload) {
                console.log('导入API响应:', payload);
                // 保存导入结果到数据域
                if (payload.data) {
                    payload.importResult = payload.data;
                }
                return payload;
            }
        },
        actions: [
            {
                "actionType": "prev",
                "type": "action",
                label: "上一步",
                size: "lg",
                className: "antd-btn antd-btn-primary antd-btn-lg",
            },
            {
                "actionType": "next",
                "type": "action",
                label: importButtonText,
                level: "primary",
                size: "lg",
                icon: "fa fa-upload",
                className: "antd-btn antd-btn-primary antd-btn-lg",
            }
        ],
        body: [
            {
                type: "alert",
                level: "info",
                body: "请检查导入的数据是否正确，您可以在此处进行编辑、添加或删除记录。",
                className: "antd-alert antd-alert-info m-b-md"
            },
            {
                type: "input-table",
                name: fieldName,
                label: "数据列表",
                showIndex: true,
                addable: true,
                editable: true,
                copyable: true,
                removable: true,
                perPage: 10,
                maxLength: maxLength,
                className: "antd-table",
                columns: columns,
                footerAddBtn: {
                    label: "添加一行",
                    icon: "fa fa-plus",
                    className: "antd-btn antd-btn-default"
                }
            },
            {
                type: "flex",
                justify: "space-between",
                align: "center",
                items: [
                    {
                        type: "tpl",
                        tpl: `<div class="text-muted antd-text-muted"><i class="fa fa-list"></i> 当前共 \${${fieldName}.length} 条记录</div>`
                    },
                    {
                        type: "tpl",
                        tpl: `<div class="text-muted antd-text-muted"><i class="fa fa-info-circle"></i> 最多支持 ${maxLength} 条记录</div>`
                    }
                ]
            },
            {
                type: "alert",
                level: "warning",
                body: "数据量接近上限，请注意控制导入记录数量！",
                visibleOn: `${fieldName}.length > ${maxLength} * 0.8`,
                className: "antd-alert antd-alert-warning m-t-md"
            }
        ]
    });
}

// 第四步：导入结果（如果启用）
if (showImportResult) {
    wizardSteps.push({
        title: "导入结果",
        subTitle: "查看导入结果详情",
        actions:[
            {
                type: "button",
                label: "重新导入",
                level: "primary",
                icon: "fa fa-refresh",
                className: "antd-btn antd-btn-primary m-r-md",
                onEvent: {
                    click: {
                        actions: [
                            {
                                actionType: "prev",
                                componentId: "wizard"
                            }
                        ]
                    }
                }
            },
            {
                type: "button",
                label: "完成",
                level: "default",
                icon: "fa fa-check",
                className: "antd-btn antd-btn-default",
                close: true
            }
        ],
        body: [
            // 统计卡片 - 更醒目的展示
            {
                type: "grid",
                columns: [
                    {
                        body: [
                            {
                                type: "panel",
                                className: "antd-panel text-center",
                                bodyClassName: "p-lg",
                                body: [
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 48px; color: #1890ff; margin-bottom: 10px;'><i class='fa fa-database'></i></div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 36px; font-weight: bold; color: #333; margin-bottom: 5px;'>${(successCount || 0) + (failedCount || 0)}</div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 14px; color: #666;'>总记录数</div>"
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        body: [
                            {
                                type: "panel",
                                className: "antd-panel text-center",
                                bodyClassName: "p-lg",
                                body: [
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 48px; color: #52c41a; margin-bottom: 10px;'><i class='fa fa-check-circle'></i></div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 36px; font-weight: bold; color: #52c41a; margin-bottom: 5px;'>${successCount || 0}</div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 14px; color: #666;'>成功导入</div>"
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        body: [
                            {
                                type: "panel",
                                className: "antd-panel text-center",
                                bodyClassName: "p-lg",
                                body: [
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 48px; color: #ff4d4f; margin-bottom: 10px;'><i class='fa fa-times-circle'></i></div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 36px; font-weight: bold; color: #ff4d4f; margin-bottom: 5px;'>${failedCount || 0}</div>"
                                    },
                                    {
                                        type: "tpl",
                                        tpl: "<div style='font-size: 14px; color: #666;'>导入失败</div>"
                                    }
                                ]
                            }
                        ]
                    }
                ],
                className: "m-b-lg"
            },

            // 全部成功提示
            {
                type: "alert",
                level: "success",
                body: "<i class='fa fa-check-circle'></i> <strong>全部导入成功！</strong>",
                className: "antd-alert m-b-md",
                showIcon: true,
                visibleOn: "${failedCount == 0 || failedCount == null}"
            },
            
            // 部分成功提示
            {
                type: "alert",
                level: "warning",
                body: "<i class='fa fa-exclamation-circle'></i> <strong>部分导入成功</strong>，请查看下方失败记录详情",
                className: "antd-alert m-b-md",
                showIcon: true,
                visibleOn: "failedCount > 0 && successCount > 0"
            },
            
            // 全部失败提示
            {
                type: "alert",
                level: "danger",
                body: "<i class='fa fa-times-circle'></i> <strong>全部导入失败</strong>，请检查数据格式后重试",
                className: "antd-alert m-b-md",
                showIcon: true,
                visibleOn: "failedCount > 0 && (successCount == 0 || successCount == null)"
            },

            // 失败记录详情表格
            {
                type: "panel",
                title: "失败记录详情",
                className: "antd-panel",
                visibleOn: "${failedRecords && failedRecords.length > 0}",
                body: [
                    {
                        type: "crud",
                        source: "${failedRecords}",
                        syncLocation: false,
                        perPage: 1000,
                        className: "antd-table",
                        columns: [
                            {
                                name: "rowIndex",
                                label: "行号",
                                type: "text",
                                width: 80,
                                className: "antd-table-cell"
                            },
                            {
                                name: "errorMessage",
                                label: "错误信息",
                                type: "text",
                                className: "antd-table-cell"
                            }
                        ],
                        headerToolbar: [
                            {
                                type: "export-excel",
                                label: "导出失败记录",
                                className: "antd-btn antd-btn-default"
                            }
                        ]
                    }
                ]
            }
        ]
    });
}

// 直接返回Wizard组件配置
return {
    type: "wizard",
    id: "wizard",
    className: "enhanced-import-wizard antd-steps",
    steps: wizardSteps,
    actionNextLabel: "下一步",
    actionPrevLabel: "上一步",
    actionFinishLabel: "完成",
    bulkSubmit: false,
    readOnly: false,
    // 添加步骤验证逻辑
    stepValidation: true,
    // // 自定义步骤跳转逻辑
    // onEvent: {
    //     stepChange: {
    //         actions: [
    //             {
    //                 actionType: "toast",
    //                 args: {
    //                     msg: "正在进入${event.data.step === 0 ? '模板下载' : event.data.step === 1 ? '文件上传' : event.data.step === 2 ? '数据预览' : event.data.step === 3 ? '导入确认' : '导入结果'}步骤",
    //                     level: "info"
    //                 }
    //             }
    //         ]
    //     }
    // },
    // 添加响应式支持
    mode: "horizontal",
    // 在移动端使用垂直模式
    modeOn: "window.innerWidth < 768 ? 'vertical' : 'horizontal'"
};
