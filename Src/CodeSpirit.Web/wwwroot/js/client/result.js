(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    // 获取考试记录ID
    const recordId = window.location.pathname.split('/').pop();

    // 构建结果页面
    const resultPage = {
        type: 'page',
        title: '考试结果',
        body: [
            {
                type: 'service',
                api: `/exam/api/exam/client/result/${recordId}`,
                className: 'result-container',
                body: [
                    {
                        type: 'panel',
                        title: '${name} - 考试结果',
                        headerClassName: 'bg-light',
                        bodyClassName: 'result-panel-body',
                        body: [
                            {
                                type: 'flex',
                                justify: 'space-between',
                                className: 'result-header',
                                items: [
                                    {
                                        type: 'flex',
                                        direction: 'column',
                                        className: 'result-info-col',
                                        items: [
                                            {
                                                type: 'tpl',
                                                tpl: '考试时间：${startTime|date:YYYY-MM-DD HH:mm:ss}',
                                                className: 'result-info-item'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '提交时间：${submitTime|date:YYYY-MM-DD HH:mm:ss}',
                                                className: 'result-info-item'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '考试时长：${duration}分钟',
                                                className: 'result-info-item'
                                            }
                                        ]
                                    },
                                    {
                                        type: 'flex',
                                        direction: 'column',
                                        align: 'center',
                                        className: 'result-score-col',
                                        items: [
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="score-circle ${score >= totalScore * 0.6 ? \'pass\' : \'fail\'}">${score !== null ? score : \'-\'}<span class="score-total">/${totalScore}</span></div>',
                                                className: 'result-score'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="result-status ${isPassed ? \'pass\' : \'fail\'}">${isPassed ? \'通过\' : \'未通过\'}</div>',
                                                className: 'mt-2',
                                                visibleOn: 'score !== null'
                                            }
                                        ]
                                    }
                                ]
                            },
                            {
                                type: 'tpl',
                                tpl: '<div class="comments-container"><div class="comments-title">评语：</div><div class="comments-content">${comments || "暂无评语"}</div></div>',
                                className: 'mt-3',
                                visibleOn: 'status === "Graded"'
                            },
                            {
                                type: 'divider'
                            },
                            {
                                type: 'panel',
                                title: '答题详情',
                                className: 'answer-details-panel',
                                body: [
                                    {
                                        type: 'each',
                                        name: 'answers',
                                        items: {
                                            type: 'panel',
                                            className: 'question-result-item',
                                            headerClassName: 'question-result-header ${isCorrect ? \'bg-success-light\' : \'bg-danger-light\'}',
                                            bodyClassName: 'question-result-body',
                                            title: {
                                                type: 'tpl',
                                                tpl: '<div class="question-result-title"><span>${$index + 1}. ${content}</span><span class="question-score">${obtainedScore || 0}/${score}分</span></div>'
                                            },
                                            body: [
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="answer-label">你的答案：</div>',
                                                    className: 'mb-2'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="user-answer">${userAnswer || "未作答"}</div>',
                                                    className: 'mb-3'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="answer-label">正确答案：</div>',
                                                    className: 'mb-2',
                                                    visibleOn: 'status === "Graded"'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="correct-answer">${correctAnswer}</div>',
                                                    className: 'mb-2',
                                                    visibleOn: 'status === "Graded"'
                                                }
                                            ]
                                        }
                                    }
                                ]
                            },
                            {
                                type: 'flex',
                                justify: 'center',
                                className: 'mt-4',
                                items: [
                                    {
                                        type: 'button',
                                        label: '返回首页',
                                        level: 'primary',
                                        actionType: 'link',
                                        link: '/client/index'
                                    }
                                ]
                            }
                        ]
                    }
                ],
                placeholder: {
                    type: 'div',
                    className: 'p-5 text-center',
                    body: '加载结果中...'
                },
                onEvent: {
                    fetchFailed: {
                        actions: [
                            {
                                actionType: 'toast',
                                args: {
                                    msgType: 'error',
                                    msg: '获取考试结果失败'
                                }
                            },
                            {
                                actionType: 'redirect',
                                args: {
                                    url: '/client/index'
                                }
                            }
                        ]
                    }
                }
            }
        ],
        css: {
            '.result-container': {
                'max-width': '1000px',
                'margin': '0 auto'
            },
            '.result-header': {
                'padding': '15px 0',
                'flex-wrap': 'wrap'
            },
            '.result-info-col': {
                'flex': '1',
                'min-width': '250px'
            },
            '.result-score-col': {
                'flex': '0 0 auto',
                'min-width': '200px'
            },
            '.result-info-item': {
                'margin-bottom': '10px',
                'font-size': '15px'
            },
            '.score-circle': {
                'width': '120px',
                'height': '120px',
                'border-radius': '50%',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'font-size': '32px',
                'font-weight': 'bold',
                'color': '#fff',
                'position': 'relative'
            },
            '.score-circle.pass': {
                'background-color': 'var(--success)'
            },
            '.score-circle.fail': {
                'background-color': 'var(--danger)'
            },
            '.score-total': {
                'font-size': '18px',
                'position': 'absolute',
                'right': '25px',
                'bottom': '30px'
            },
            '.result-status': {
                'font-size': '20px',
                'font-weight': 'bold'
            },
            '.result-status.pass': {
                'color': 'var(--success)'
            },
            '.result-status.fail': {
                'color': 'var(--danger)'
            },
            '.comments-container': {
                'background-color': '#f9f9f9',
                'padding': '15px',
                'border-radius': '5px',
                'margin-top': '20px'
            },
            '.comments-title': {
                'font-weight': 'bold',
                'margin-bottom': '5px'
            },
            '.question-result-item': {
                'margin-bottom': '20px',
                'border': '1px solid #eee',
                'border-radius': '5px',
                'overflow': 'hidden'
            },
            '.question-result-header': {
                'padding': '10px 15px'
            },
            '.bg-success-light': {
                'background-color': 'rgba(40, 167, 69, 0.1)'
            },
            '.bg-danger-light': {
                'background-color': 'rgba(220, 53, 69, 0.1)'
            },
            '.question-result-title': {
                'display': 'flex',
                'justify-content': 'space-between',
                'align-items': 'center'
            },
            '.question-score': {
                'font-weight': 'normal',
                'font-size': '14px'
            },
            '.answer-label': {
                'font-weight': 'bold',
                'color': '#666'
            },
            '.user-answer': {
                'background-color': '#f5f5f5',
                'padding': '10px',
                'border-radius': '4px',
                'white-space': 'pre-wrap'
            },
            '.correct-answer': {
                'background-color': 'rgba(40, 167, 69, 0.05)',
                'padding': '10px',
                'border-radius': '4px',
                'border-left': '3px solid var(--success)',
                'white-space': 'pre-wrap'
            },
            '@media (max-width: 768px)': {
                '.result-header': {
                    'flex-direction': 'column'
                },
                '.result-score-col': {
                    'margin-top': '20px',
                    'align-items': 'center'
                }
            }
        }
    };

    // 初始化amis
    let amisInstance = amis.embed(
        '#root',
        resultPage,
        {
            location: history.location,
            locale: 'zh-CN'
        },
        {
            requestAdaptor: (api) => {
                var token = localStorage.getItem('token');
                return {
                    ...api,
                    headers: {
                        ...api.headers,
                        'Authorization': 'Bearer ' + token,
                        'X-Forwarded-With': 'CodeSpirit'
                    }
                };
            },
            responseAdaptor: function (api, payload, query, request, response) {
                // 处理错误响应
                if (response.status === 403) {
                    alert('您没有权限查看此考试结果！');
                    window.location.href = "/client/index";
                    return { msg: '您没有权限访问此页面，请联系管理员！' }
                }
                else if (response.status === 401) {
                    window.location.href = `/client/login`;
                    return { msg: '登录过期！' };
                }

                return payload;
            },
            theme: 'antd'
        }
    );

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})(); 