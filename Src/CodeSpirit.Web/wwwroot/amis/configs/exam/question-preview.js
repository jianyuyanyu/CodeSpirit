// 题目预览配置JS文件
// 专门用于题目预览的AMIS配置

// 可以通过api变量获取当前请求的参数
console.log('Question Preview API:', api);
console.log('API Query:', api.query);

// 从URL参数获取配置
const baseApi = api.query?.baseApi || '/exam/api/exam';

// 题目预览配置
return {
    type: "page",
    title: "",
    className: "question-preview-page",
    css: {
        ".question-preview-page": {
            "padding": "0",
            "background": "#f8f9fa"
        },
        ".question-content-card": {
            "background": "#fff",
            "border-radius": "8px",
            "box-shadow": "0 2px 8px rgba(0,0,0,0.1)",
            "margin-bottom": "16px",
            "padding": "20px"
        },
        ".question-title": {
            "font-size": "16px",
            "font-weight": "600",
            "color": "#262626",
            "line-height": "1.5",
            "margin-bottom": "16px",
            "border-left": "4px solid #1890ff",
            "padding-left": "12px"
        },
        ".question-meta": {
            "display": "flex",
            "gap": "16px",
            "margin-bottom": "20px",
            "flex-wrap": "wrap"
        },
        ".meta-item": {
            "display": "flex",
            "align-items": "center",
            "gap": "6px",
            "padding": "4px 12px",
            "background": "#f0f2f5",
            "border-radius": "16px",
            "font-size": "12px"
        },
        ".question-options": {
            "margin": "16px 0"
        },
        ".option-item": {
            "display": "flex",
            "align-items": "flex-start",
            "gap": "8px",
            "margin-bottom": "8px",
            "padding": "8px 12px",
            "background": "#fafafa",
            "border-radius": "6px",
            "border": "1px solid #e8e8e8"
        },
        ".option-label": {
            "min-width": "24px",
            "height": "24px",
            "display": "flex",
            "align-items": "center",
            "justify-content": "center",
            "background": "#1890ff",
            "color": "#fff",
            "border-radius": "50%",
            "font-size": "12px",
            "font-weight": "600"
        },
        ".option-content": {
            "flex": "1",
            "line-height": "1.5",
            "color": "#595959"
        },
        ".answer-section": {
            "margin-top": "20px",
            "padding": "16px",
            "background": "#f6ffed",
            "border": "1px solid #b7eb8f",
            "border-radius": "6px"
        },
        ".analysis-section": {
            "margin-top": "16px",
            "padding": "16px",
            "background": "#fff7e6",
            "border": "1px solid #ffd591",
            "border-radius": "6px"
        },
        ".section-title": {
            "font-weight": "600",
            "color": "#262626",
            "margin-bottom": "8px",
            "display": "flex",
            "align-items": "center",
            "gap": "6px"
        }
    },
    body: [
        {
            type: "container",
            className: "question-content-card",
            body: [
                // 题目标题
                {
                    type: "tpl",
                    tpl: "<div class='question-title'>${question.content}</div>"
                },
                // 题目元信息
                {
                    type: "tpl",
                    tpl: "<div class='question-meta'><div class='meta-item'><i class='fa fa-tag'></i><span>${question.type == 1 ? '单选题' : question.type == 2 ? '多选题' : question.type == 3 ? '判断题' : question.type == 4 ? '简答题' : question.type}</span></div><div class='meta-item'><i class='fa fa-signal'></i><span>${question.difficulty == 1 ? '简单' : question.difficulty == 2 ? '中等' : question.difficulty == 3 ? '困难' : question.difficulty}</span></div></div>"
                },
                // 选项显示（仅选择题）
                {
                    type: "container",
                    className: "question-options",
                    visibleOn: "${question.type == 1 || question.type == 2}",
                    body: [
                        {
                            type: "each",
                            name: "question.options",
                            items: {
                                type: "tpl",
                                tpl: "<div class='option-item'><div class='option-label'>${['A','B','C','D','E','F','G','H'][index] || (index + 1)}</div><div class='option-content'>${item}</div></div>"
                            }
                        }
                    ]
                },
                // 正确答案
                {
                    type: "container",
                    className: "answer-section",
                    body: [
                        {
                            type: "tpl",
                            tpl: "<div class='section-title'><i class='fa fa-check-circle' style='color: #52c41a'></i><span>正确答案</span></div><div>${question.correctAnswer}</div>"
                        }
                    ]
                },
                // 解析
                {
                    type: "container",
                    className: "analysis-section",
                    visibleOn: "${question.analysis}",
                    body: [
                        {
                            type: "tpl",
                            tpl: "<div class='section-title'><i class='fa fa-lightbulb-o' style='color: #fa8c16'></i><span>题目解析</span></div><div>${question.analysis}</div>"
                        }
                    ]
                },
                // 标签（如果有）
                {
                    type: "tpl",
                    visibleOn: "${question.tags}",
                    tpl: "<div style='margin-top: 16px; padding-top: 16px; border-top: 1px solid #e8e8e8;'><div class='section-title'><i class='fa fa-tags' style='color: #722ed1'></i><span>知识点标签</span></div><div style='padding: 2px 8px; background: #f0f0f0; border-radius: 12px; font-size: 12px; color: #666; display: inline-block;'>${question.tags}</div></div>"
                }
            ]
        }
    ]
};
