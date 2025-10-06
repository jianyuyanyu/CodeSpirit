// 题目导入向导配置JS文件
// 使用Wizard组件优化题目导入流程布局
// 支持AntD主题的题目导入向导组件
// 通过api.query获取配置参数

// 添加DiffEditor样式优化
const diffEditorStyles = `
<style>
.question-diff-dialog .cxd-Modal-content {
    height: 90vh !important;
    max-height: 90vh !important;
}

.question-diff-dialog .cxd-Modal-body {
    height: calc(90vh - 120px) !important;
    overflow: hidden !important;
}

.diff-editor-container {
    height: 400px !important;
    min-height: 400px !important;
    border: 1px solid #d9d9d9;
    border-radius: 6px;
    overflow: hidden;
}

.diff-editor-container .monaco-editor {
    border-radius: 6px;
}

.antd-tabs .cxd-Tabs-content {
    height: calc(100% - 44px);
    overflow: hidden;
}

.antd-tabs .cxd-Tabs-pane {
    height: 100%;
    padding: 16px;
    overflow: auto;
}

.diff-editor-container .monaco-diff-editor .original-in-monaco-diff-editor,
.diff-editor-container .monaco-diff-editor .modified-in-monaco-diff-editor {
    border-radius: 0 !important;
}

.diff-editor-container .monaco-diff-editor .monaco-editor .margin,
.diff-editor-container .monaco-diff-editor .monaco-editor .monaco-editor-background {
    background-color: #fafafa !important;
}

.question-diff-dialog .antd-alert {
    margin-bottom: 12px;
}

.question-diff-dialog .antd-tabs {
    height: calc(100% - 60px);
}

/* 主要DiffEditor样式 */
.main-diff {
    height: 500px !important;
    min-height: 500px !important;
}

.diff-main-container {
    margin-bottom: 20px;
}

.correction-notes-panel {
    margin-top: 16px;
}

/* DiffEditor标题栏样式 */
.diff-editor-container::before {
    content: "";
    display: block;
    height: 2px;
    background: linear-gradient(90deg, #1890ff 0%, #52c41a 100%);
    margin-bottom: 0;
}

/* Monaco Editor差异高亮优化 */
.monaco-diff-editor .monaco-editor .margin-view-overlays .line-insert,
.monaco-diff-editor .monaco-editor .margin-view-overlays .line-delete {
    border-left: 3px solid;
}

.monaco-diff-editor .monaco-editor .margin-view-overlays .line-insert {
    border-left-color: #52c41a;
    background-color: rgba(82, 196, 26, 0.1);
}

.monaco-diff-editor .monaco-editor .margin-view-overlays .line-delete {
    border-left-color: #ff4d4f;
    background-color: rgba(255, 77, 79, 0.1);
}

/* Combo组件样式 */
.question-combo-list {
    border: 1px solid #d9d9d9;
    border-radius: 6px;
    background: #fafafa;
}

.question-combo-item {
    background: #fff;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
    margin-bottom: 16px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.05);
    transition: all 0.3s ease;
}

.question-combo-item:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    border-color: #1890ff;
}

.question-item-container {
    padding: 16px;
}

.question-header {
    border-radius: 6px;
    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    border-left: 4px solid #1890ff;
}

.question-title {
    font-weight: 600;
    color: #1890ff;
}

.question-content-editor textarea {
    border: 1px solid #d9d9d9;
    border-radius: 6px;
    font-family: 'Microsoft YaHei', sans-serif;
}

.question-options-editor {
    background: #f9f9f9;
    border-radius: 6px;
    padding: 8px;
}

.question-actions {
    border-top: 1px solid #e8e8e8;
    margin-top: 16px;
    border-radius: 0 0 8px 8px;
    background: #fafafa;
}

.question-audit-info .cxd-Alert {
    border-radius: 6px;
    border-left: 4px solid #faad14;
}

/* 响应式设计 */
@media (max-width: 768px) {
    .question-diff-dialog .cxd-Modal-content {
        height: 95vh !important;
        max-height: 95vh !important;
    }
    
    .main-diff {
        height: 350px !important;
        min-height: 350px !important;
    }
    
    .diff-header {
        flex-direction: column !important;
        align-items: flex-start !important;
        gap: 8px;
    }
    
    .diff-header .cxd-Flex:last-child {
        margin-top: 8px;
    }
    
    .question-combo-item {
        margin-bottom: 12px;
    }
    
    .question-item-container {
        padding: 12px;
    }
    
    .question-header {
        flex-direction: column !important;
        align-items: flex-start !important;
        gap: 8px;
    }
    
    .question-header .cxd-Flex:last-child {
        margin-top: 8px;
        flex-wrap: wrap;
    }
}
</style>
`;

// 将样式注入到页面
if (typeof document !== 'undefined') {
    const styleElement = document.createElement('div');
    styleElement.innerHTML = diffEditorStyles;
    document.head.appendChild(styleElement.firstElementChild);
}

// 可以通过api变量获取当前请求的参数
console.log('Question Import Wizard API:', api);
console.log('API Query:', api.query);

// 从URL参数获取配置
const configType = api.query?.type || 'wizard';
const baseApi = api.query?.baseApi || '/exam/api/exam';
const rootApi = api.query?.rootApi || '${ROOT_API}';

console.log('Config Type:', configType);
console.log('Base API:', baseApi);
console.log('Root API:', rootApi);

// 根据配置类型返回不同的配置
if (configType === 'preview') {
    // 题目预览配置已移至独立文件
    // 重定向到专门的预览配置文件
    return {
        type: "service",
        schemaApi: `js:/amis/configs/question-preview.js?baseApi=${baseApi}`
    };
}

// 默认返回题目导入向导配置
return {
    type: "wizard",
    id: "questionImportWizard",
    title: "题目导入向导",
    className: "question-import-wizard antd-steps",
    mode: "horizontal",
    actionFinishLabel: "完成导入",
    actionNextLabel: "下一步",
    actionPrevLabel: "上一步",
    bulkSubmit: false,
    readOnly: false,
    stepValidation: true,
    // 在移动端使用垂直模式
    modeOn: "window.innerWidth < 768 ? 'vertical' : 'horizontal'",
    steps: [
        // 步骤1：解析配置
        {
            title: "解析配置",
            subTitle: "设置题目分类和导入文本",
            api: `post:${baseApi}/questions/import-wizard/step1-parse`,
            body: [
                {
                    type: "alert",
                    level: "info",
                    body: "请选择题目分类并输入要解析的题目文本，系统将自动解析题目内容。",
                    className: "antd-alert antd-alert-info m-b-md"
                },
                {
                    type: "tree-select",
                    name: "categoryId",
                    label: "题目分类",
                    required: true,
                    source: `${rootApi}/api/exam/QuestionCategories/tree`,
                    multiple: false,
                    cascade: true,
                    showOutline: true,
                    labelField: "name",
                    valueField: "id",
                    className: "antd-tree-select"
                },
                {
                    type: "editor",
                    name: "text",
                    label: "Word格式文本",
                    required: true,
                    language: "markdown",
                    size: "xxl",
                    className: "antd-editor",
                    placeholder: `一、单项选择题（每题1分，80小题，共计80分）
1、SSL协议最早是由(B)提出的。
A、Microsoft
B、Netscape
C、ISO
D、IBM
【难度】困难
【解析】SSL协议是由Netscape公司提出的，用于在互联网上提供安全通信。
【标签】SSL、安全通信`
                },
                {
                    type: "grid",
                    columns: [
                        {
                            body: [
                                {
                                    type: "switch",
                                    name: "enableAiAudit",
                                    label: "启用AI审核",
                                    value: true,
                                    className: "antd-switch"
                                }
                            ]
                        },
                        {
                            body: [
                                {
                                    type: "switch",
                                    name: "autoCorrectErrors",
                                    label: "自动修正错误",
                                    value: true,
                                    visibleOn: "enableAiAudit",
                                    className: "antd-switch"
                                }
                            ]
                        }
                    ]
                }
            ]
        },
        // 步骤2：预览编辑和确认导入
        {
            title: "预览编辑",
            subTitle: "查看解析结果、编辑题目并选择要导入的题目",
            api: `post:${baseApi}/questions/import-wizard/step4-import`,
            body: [
                {
                    type: "service",
                    api: `get:${baseApi}/questions/import-wizard/step2-preview/\${sessionId}`,
                    body: [
                        {
                            type: "alert",
                            level: "info",
                            body: "AI审核完成：共解析 ${aiAuditSummary.totalCount} 个题目，通过 ${aiAuditSummary.passedCount} 个，修正 ${aiAuditSummary.correctedCount} 个，发现错误 ${aiAuditSummary.errorCount} 个。",
                            className: "antd-alert antd-alert-info m-b-md"
                        },
                        {
                            type: "form",
                            actions: [],
                            className: "antd-form",
                            body: [
                                {
                                    type: "hidden",
                                    name: "sessionId",
                                    value: "${sessionId}"
                                },
                                // 题目统计信息
                                {
                                    type: "alert",
                                    level: "success",
                                    body: "共解析到 ${questions.length} 个题目，您可以编辑题目内容并选择要导入的题目。",
                                    className: "antd-alert antd-alert-success m-b-md"
                                },
                                // 使用 combo 组件进行题目预览、编辑和选择
                                {
                                    type: "combo",
                                    name: "questions",
                                    label: "题目列表（可编辑）",
                                    value: "${questions}",
                                    multiple: true,
                                    addable: false,
                                    removable: true,
                                    draggable: true,
                                    multiLine: true,
                                    className: "question-combo-list",
                                    itemClassName: "question-combo-item",
                                    placeholder: "暂无题目数据",
                                    items: [
                                        {
                                            type: "container",
                                            className: "question-item-container",
                                            body: [
                                                // 题目头部信息
                                                {
                                                    type: "flex",
                                                    justify: "space-between",
                                                    alignItems: "center",
                                                    className: "question-header p-sm bg-light m-b-sm",
                                                    items: [
                                                        {
                                                            type: "tpl",
                                                            tpl: "<strong><i class='fa fa-file-text-o text-primary'></i> 题目 ${index + 1}</strong>",
                                                            className: "question-title"
                                                        },
                                                        {
                                                            type: "flex",
                                                            items: [
                                                                {
                                                                    type: "tpl",
                                                                    tpl: "${type == 1 ? '<span class=\"label label-info\">单选</span>' : type == 2 ? '<span class=\"label label-primary\">多选</span>' : type == 3 ? '<span class=\"label label-warning\">判断</span>' : type == 4 ? '<span class=\"label label-success\">简答</span>' : type}",
                                                                    className: "m-r-sm"
                                                                },
                                                                {
                                                                    type: "tpl",
                                                                    tpl: "${difficulty == 1 ? '<span class=\"label label-success\">简单</span>' : difficulty == 2 ? '<span class=\"label label-warning\">中等</span>' : difficulty == 3 ? '<span class=\"label label-danger\">困难</span>' : difficulty}",
                                                                    className: "m-r-sm"
                                                                },
                                                                {
                                                                    type: "tpl",
                                                                    tpl: "${auditStatus == 1 ? '<span class=\"label label-success\"><i class=\"fa fa-check\"></i> 通过</span>' : auditStatus == 2 ? '<span class=\"label label-danger\"><i class=\"fa fa-times\"></i> 有错误</span>' : auditStatus == 3 ? '<span class=\"label label-danger\"><i class=\"fa fa-times\"></i> 失败</span>' : '<span class=\"label label-default\"><i class=\"fa fa-clock-o\"></i> 未审核</span>'}",
                                                                    className: "m-r-sm"
                                                                },
                                                                {
                                                                    type: "tpl",
                                                                    tpl: "${isCorrected ? '<span class=\"label label-warning\"><i class=\"fa fa-exchange\"></i> 已修正</span>' : ''}",
                                                                    visibleOn: "${isCorrected}"
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                },
                                                // 题目内容编辑区域
                                                {
                                                    type: "grid",
                                                    columns: [
                                                        {
                                                            md: 8,
                                                            body: [
                                                                {
                                                                    type: "textarea",
                                                                    name: "content",
                                                                    label: "题目内容",
                                                                    required: true,
                                                                    minRows: 3,
                                                                    maxRows: 8,
                                                                    className: "question-content-editor"
                                                                },
                                                                {
                                                                    type: "input-array",
                                                                    name: "options",
                                                                    label: "选项",
                                                                    visibleOn: "${type == 1 || type == 2}",
                                                                    items: {
                                                                        type: "input-text",
                                                                        placeholder: "请输入选项内容"
                                                                    },
                                                                    addable: true,
                                                                    removable: true,
                                                                    minLength: 2,
                                                                    maxLength: 8,
                                                                    className: "question-options-editor"
                                                                },
                                                                {
                                                                    type: "input-text",
                                                                    name: "correctAnswer",
                                                                    label: "正确答案",
                                                                    required: true,
                                                                    placeholder: "请输入正确答案",
                                                                    className: "question-answer-editor"
                                                                },
                                                                {
                                                                    type: "textarea",
                                                                    name: "analysis",
                                                                    label: "解析",
                                                                    minRows: 2,
                                                                    maxRows: 6,
                                                                    placeholder: "请输入题目解析（可选）",
                                                                    className: "question-analysis-editor"
                                                                }
                                                            ]
                                                        },
                                                        {
                                                            md: 4,
                                                            body: [
                                                                {
                                                                    type: "select",
                                                                    name: "type",
                                                                    label: "题目类型",
                                                                    options: [
                                                                        { label: "单选题", value: 1 },
                                                                        { label: "多选题", value: 2 },
                                                                        { label: "判断题", value: 3 },
                                                                        { label: "简答题", value: 4 }
                                                                    ],
                                                                    className: "question-type-selector"
                                                                },
                                                                {
                                                                    type: "select",
                                                                    name: "difficulty",
                                                                    label: "难度等级",
                                                                    options: [
                                                                        { label: "简单", value: 1 },
                                                                        { label: "中等", value: 2 },
                                                                        { label: "困难", value: 3 }
                                                                    ],
                                                                    className: "question-difficulty-selector"
                                                                },
                                                                {
                                                                    type: "input-text",
                                                                    name: "knowledgePoints",
                                                                    label: "知识点标签",
                                                                    placeholder: "多个标签用逗号分隔",
                                                                    className: "question-tags-editor"
                                                                },
                                                                {
                                                                    type: "input-number",
                                                                    name: "defaultScore",
                                                                    label: "默认分值",
                                                                    min: 0.5,
                                                                    max: 100,
                                                                    step: 0.5,
                                                                    value: 1,
                                                                    className: "question-score-editor"
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                },
                                                // 审核信息和操作按钮
                                                {
                                                    type: "container",
                                                    visibleOn: "${auditMessage || isCorrected}",
                                                    className: "question-audit-info m-t-sm",
                                                    body: [
                                                        {
                                                            type: "alert",
                                                            level: "${auditStatus == 2 || auditStatus == 3 ? 'danger' : 'info'}",
                                                            body: "${auditMessage}",
                                                            className: "m-b-sm",
                                                            visibleOn: "${auditMessage}"
                                                        }
                                                    ]
                                                },
                                                // 操作按钮区域
                                                {
                                                    type: "flex",
                                                    justify: "flex-end",
                                                    className: "question-actions p-sm bg-light",
                                                    items: [
                                                        {
                                                            type: "button",
                                                            label: "预览",
                                                            level: "link",
                                                            size: "sm",
                                                            icon: "fa fa-eye",
                                                            className: "m-r-sm",
                                                            actionType: "dialog",
                                                            dialog: {
                                                                title: "题目预览",
                                                                size: "lg",
                                                                body: {
                                                                    type: "service",
                                                                    schemaApi: `js:/amis/configs/question-preview.js?baseApi=${baseApi}`,
                                                                    data: {
                                                                        question: {
                                                                            content: "$content",
                                                                            type: "$type",
                                                                            options: "$options",
                                                                            correctAnswer: "$correctAnswer",
                                                                            analysis: "$analysis",
                                                                            difficulty: "$difficulty",
                                                                            tags: "$knowledgePoints"
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        },
                                                        {
                                                            type: "button",
                                                            label: "修正对比",
                                                            level: "link",
                                                            size: "sm",
                                                            icon: "fa fa-exchange",
                                                            visibleOn: "${isCorrected}",
                                                            actionType: "dialog",
                                                            dialog: {
                                                                title: "AI修正对比 - 题目 ${index + 1}",
                                                                size: "xl",
                                                                className: "question-diff-dialog",
                                                                body: {
                                                                    type: "service",
                                                                    api: `get:${baseApi}/questions/import-wizard/correction-diff/\${sessionId}/\${index}`,
                                                                    body: [
                                                                        {
                                                                            type: "alert",
                                                                            level: "info",
                                                                            body: "以下是AI自动修正的内容对比，按照导入模板的完整格式显示修正前后的变化。",
                                                                            className: "antd-alert antd-alert-info m-b-md"
                                                                        },
                                                                        {
                                                                            type: "container",
                                                                            className: "diff-main-container",
                                                                            body: [
                                                                                {
                                                                                    type: "flex",
                                                                                    justify: "space-between",
                                                                                    alignItems: "center",
                                                                                    className: "diff-header p-sm bg-light m-b-sm",
                                                                                    items: [
                                                                                        {
                                                                                            type: "tpl",
                                                                                            tpl: "<strong><i class='fa fa-exchange text-primary'></i> 题目完整格式对比</strong>",
                                                                                            className: "diff-title"
                                                                                        },
                                                                                        {
                                                                                            type: "flex",
                                                                                            items: [
                                                                                                {
                                                                                                    type: "tpl",
                                                                                                    tpl: "<span class='text-muted'><i class='fa fa-arrow-left text-danger'></i> 修正前</span>",
                                                                                                    className: "m-r-md"
                                                                                                },
                                                                                                {
                                                                                                    type: "tpl",
                                                                                                    tpl: "<span class='text-muted'><i class='fa fa-arrow-right text-success'></i> 修正后</span>"
                                                                                                }
                                                                                            ]
                                                                                        }
                                                                                    ]
                                                                                },
                                                                                {
                                                                                    type: "diff-editor",
                                                                                    name: "questionDiff",
                                                                                    language: "text",
                                                                                    diffValue: "${originalText}",
                                                                                    value: "${correctedText}",
                                                                                    options: {
                                                                                        readOnly: true,
                                                                                        renderSideBySide: true,
                                                                                        originalEditable: false,
                                                                                        modifiedEditable: false,
                                                                                        minimap: { enabled: false },
                                                                                        wordWrap: "on",
                                                                                        lineNumbers: "on",
                                                                                        folding: false,
                                                                                        scrollBeyondLastLine: false,
                                                                                        automaticLayout: true,
                                                                                        theme: "vs",
                                                                                        fontSize: 14,
                                                                                        lineHeight: 22,
                                                                                        renderWhitespace: "boundary",
                                                                                        ignoreTrimWhitespace: false,
                                                                                        renderIndicators: true,
                                                                                        enableSplitViewResizing: true,
                                                                                        originalAriaLabel: "修正前完整题目",
                                                                                        modifiedAriaLabel: "修正后完整题目"
                                                                                    },
                                                                                    size: "xxl",
                                                                                    className: "diff-editor-container main-diff"
                                                                                }
                                                                            ]
                                                                        },
                                                                        {
                                                                            type: "divider",
                                                                            className: "m-t-lg m-b-md"
                                                                        },
                                                                        {
                                                                            type: "panel",
                                                                            title: "AI修正说明",
                                                                            className: "antd-panel correction-notes-panel",
                                                                            headerClassName: "bg-warning text-white",
                                                                            visibleOn: "${correctionNotes && correctionNotes.length > 0}",
                                                                            body: [
                                                                                {
                                                                                    type: "each",
                                                                                    name: "correctionNotes",
                                                                                    items: {
                                                                                        type: "alert",
                                                                                        level: "success",
                                                                                        body: "<i class='fa fa-check-circle'></i> ${item}",
                                                                                        className: "antd-alert antd-alert-success m-b-sm",
                                                                                        showIcon: false
                                                                                    }
                                                                                }
                                                                            ]
                                                                        },
                                                                        {
                                                                            type: "alert",
                                                                            level: "info",
                                                                            body: "<i class='fa fa-info-circle'></i> 此题目未发生修正，内容保持原样。",
                                                                            className: "antd-alert antd-alert-info",
                                                                            showIcon: false,
                                                                            visibleOn: "${!hasChanges}"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        },
        // 步骤3：导入结果
        {
            title: "导入结果",
            subTitle: "查看导入结果详情",
            actions: [
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
                                    componentId: "questionImportWizard"
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
                    close: true,
                    onEvent: {
                        click: {
                            actions: [
                                {
                                    actionType: "reload",
                                    componentName: "questionsCrud"
                                }
                            ]
                        }
                    }
                }
            ],
            body: [
                // 统计卡片
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
                                            tpl: "<div style='font-size: 14px; color: #666;'>总题目数</div>"
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
                // 错误分类统计
                {
                    type: "container",
                    visibleOn: "${failedRecords && failedRecords.length > 0}",
                    body: [
                        {
                            type: "panel",
                            title: "错误分类统计",
                            className: "antd-panel m-b-md",
                            body: [
                                {
                                    type: "grid",
                                    columns: [
                                        {
                                            body: [
                                                {
                                                    type: "panel",
                                                    className: "antd-panel text-center",
                                                    bodyClassName: "p-md",
                                                    body: [
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 24px; color: #ff7875; margin-bottom: 5px;'><i class='fa fa-copy'></i></div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 18px; font-weight: bold; color: #ff7875;'>${failedRecords.filter(item => item.errorType === 'DUPLICATE' || item.errorMessage.includes('重复') || item.errorMessage.includes('已存在')).length}</div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 12px; color: #666;'>题目重复</div>"
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
                                                    bodyClassName: "p-md",
                                                    body: [
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 24px; color: #faad14; margin-bottom: 5px;'><i class='fa fa-exclamation-triangle'></i></div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 18px; font-weight: bold; color: #faad14;'>${failedRecords.filter(item => item.errorType === 'VALIDATION' || item.errorMessage.includes('格式') || item.errorMessage.includes('验证')).length}</div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 12px; color: #666;'>格式错误</div>"
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
                                                    bodyClassName: "p-md",
                                                    body: [
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 24px; color: #722ed1; margin-bottom: 5px;'><i class='fa fa-ban'></i></div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 18px; font-weight: bold; color: #722ed1;'>${failedRecords.filter(item => item.errorType === 'PERMISSION' || item.errorMessage.includes('权限') || item.errorMessage.includes('访问')).length}</div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 12px; color: #666;'>权限不足</div>"
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
                                                    bodyClassName: "p-md",
                                                    body: [
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 24px; color: #f5222d; margin-bottom: 5px;'><i class='fa fa-bug'></i></div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 18px; font-weight: bold; color: #f5222d;'>${failedRecords.filter(item => !['DUPLICATE', 'VALIDATION', 'PERMISSION'].includes(item.errorType) && !item.errorMessage.includes('重复') && !item.errorMessage.includes('已存在') && !item.errorMessage.includes('格式') && !item.errorMessage.includes('验证') && !item.errorMessage.includes('权限') && !item.errorMessage.includes('访问')).length}</div>"
                                                        },
                                                        {
                                                            type: "tpl",
                                                            tpl: "<div style='font-size: 12px; color: #666;'>其他错误</div>"
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
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
                            perPage: 20,
                            className: "antd-table",
                            columns: [
                                {
                                    name: "rowIndex",
                                    label: "序号",
                                    type: "text",
                                    width: 80,
                                    className: "antd-table-cell text-center"
                                },
                                {
                                    name: "content",
                                    label: "题目内容",
                                    type: "tpl",
                                    width: "40%",
                                    className: "antd-table-cell",
                                    tpl: "<div style='max-width: 300px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;' title='${content}'>${content}</div>"
                                },
                                {
                                    name: "errorType",
                                    label: "错误类型",
                                    type: "tpl",
                                    width: 120,
                                    className: "antd-table-cell text-center",
                                    tpl: "${errorType === 'DUPLICATE' || errorMessage.includes('重复') || errorMessage.includes('已存在') ? '<span class=\"label label-warning\"><i class=\"fa fa-copy\"></i> 题目重复</span>' : errorType === 'VALIDATION' || errorMessage.includes('格式') || errorMessage.includes('验证') ? '<span class=\"label label-danger\"><i class=\"fa fa-exclamation-triangle\"></i> 格式错误</span>' : errorType === 'PERMISSION' || errorMessage.includes('权限') || errorMessage.includes('访问') ? '<span class=\"label label-info\"><i class=\"fa fa-ban\"></i> 权限不足</span>' : '<span class=\"label label-default\"><i class=\"fa fa-bug\"></i> 其他错误</span>'}"
                                },
                                {
                                    name: "errorMessage",
                                    label: "错误详情",
                                    type: "tpl",
                                    className: "antd-table-cell",
                                    tpl: "<div style='color: #f5222d; font-size: 12px; line-height: 1.4;'><i class='fa fa-exclamation-circle'></i> ${errorMessage}</div>"
                                },
                                {
                                    type: "operation",
                                    label: "操作",
                                    width: 100,
                                    buttons: [
                                        {
                                            type: "button",
                                            label: "详情",
                                            level: "link",
                                            size: "sm",
                                            icon: "fa fa-info-circle",
                                            actionType: "dialog",
                                            dialog: {
                                                title: "错误详情",
                                                size: "md",
                                                body: [
                                                    {
                                                        type: "form",
                                                        mode: "horizontal",
                                                        horizontal: {
                                                            left: 3,
                                                            right: 9
                                                        },
                                                        body: [
                                                            {
                                                                type: "static",
                                                                label: "题目序号",
                                                                value: "${rowIndex}"
                                                            },
                                                            {
                                                                type: "static",
                                                                label: "题目内容",
                                                                value: "${content}"
                                                            },
                                                            {
                                                                type: "static",
                                                                label: "错误类型",
                                                                value: "${errorType || '未分类'}"
                                                            },
                                                            {
                                                                type: "static",
                                                                label: "错误详情",
                                                                value: "${errorMessage}"
                                                            },
                                                            {
                                                                type: "static",
                                                                label: "建议解决方案",
                                                                value: "${errorType === 'DUPLICATE' || errorMessage.includes('重复') || errorMessage.includes('已存在') ? '请检查题目内容是否与现有题目重复，或修改题目内容后重新导入' : errorType === 'VALIDATION' || errorMessage.includes('格式') || errorMessage.includes('验证') ? '请检查题目格式是否正确，包括选项格式、答案格式等' : errorType === 'PERMISSION' || errorMessage.includes('权限') || errorMessage.includes('访问') ? '请联系管理员检查您的权限设置' : '请联系技术支持或查看系统日志获取更多信息'}"
                                                            }
                                                        ]
                                                    }
                                                ]
                                            }
                                        }
                                    ]
                                }
                            ],
                            headerToolbar: [
                                {
                                    type: "export-excel",
                                    label: "导出失败记录",
                                    className: "antd-btn antd-btn-default",
                                    icon: "fa fa-download"
                                },
                                {
                                    type: "button",
                                    label: "常见问题",
                                    className: "antd-btn antd-btn-info",
                                    icon: "fa fa-question-circle",
                                    actionType: "dialog",
                                    dialog: {
                                        title: "导入失败常见问题",
                                        size: "lg",
                                        body: [
                                            {
                                                type: "collapse",
                                                accordion: false,
                                                body: [
                                                    {
                                                        header: "题目重复问题",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='p-md'><h5><i class='fa fa-copy text-warning'></i> 题目重复</h5><p><strong>原因：</strong>导入的题目内容与数据库中已存在的题目相同。</p><p><strong>解决方案：</strong></p><ul><li>修改题目内容，确保与现有题目不重复</li><li>检查是否误导入了相同的文件</li><li>联系管理员确认是否需要更新现有题目</li></ul></div>"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        header: "格式错误问题",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='p-md'><h5><i class='fa fa-exclamation-triangle text-danger'></i> 格式错误</h5><p><strong>常见原因：</strong></p><ul><li>选项格式不正确（如缺少选项标识）</li><li>正确答案格式不匹配</li><li>题目内容包含特殊字符</li><li>必填字段为空</li></ul><p><strong>解决方案：</strong></p><ul><li>检查题目格式是否符合要求</li><li>确保选项使用正确的标识符</li><li>验证正确答案格式</li><li>移除或转义特殊字符</li></ul></div>"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        header: "权限问题",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='p-md'><h5><i class='fa fa-ban text-info'></i> 权限不足</h5><p><strong>原因：</strong>当前用户没有足够的权限执行导入操作。</p><p><strong>解决方案：</strong></p><ul><li>联系系统管理员申请相应权限</li><li>确认您的账户状态是否正常</li><li>检查是否在正确的题目分类下导入</li></ul></div>"
                                                            }
                                                        ]
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    ]
};
