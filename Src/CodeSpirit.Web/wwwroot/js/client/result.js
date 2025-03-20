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
        title: '',
        body: [
            {
                type: 'service',
                api: '/identity/api/identity/profile',
                className: 'client-header',
                body: [
                    {
                        type: 'flex',
                        justify: 'space-between',
                        className: 'w-full',
                        items: [
                            {
                                type: 'tpl',
                                tpl: '<div class="logo"><img src="/logo.png" /><span>考试系统</span></div>',
                                className: 'client-logo'
                            },
                            {
                                type: 'flex',
                                justify: 'flex-end',
                                alignItems: 'center',
                                className: 'user-info-container',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="user-info">欢迎您，${name}</div>'
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                type: 'service',
                api: `/exam/api/exam/client/result/${recordId}`,
                className: 'result-container',
                body: [
                    {
                        type: 'panel',
                        header: {
                            type: 'tpl',
                            tpl: '<div style="font-size: 24px; font-weight: bold; color: var(--primary-color); text-align: center;">${name} - 考试结果</div>',
                            className: 'exam-title'
                        },
                        headerClassName: 'exam-panel-header',
                        bodyClassName: 'result-panel-body',
                        className: 'exam-panel',
                        body: [
                            {
                                type: 'flex',
                                justify: 'space-between',
                                className: 'exam-info',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '开始时间：${startTime}',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '提交时间：${submitTime}',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '考试时长：${duration}分钟',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '总分：${totalScore}分',
                                        className: 'exam-info-item'
                                    }
                                ]
                            },
                            {
                                type: 'flex',
                                justify: 'center',
                                className: 'mt-4 result-score-container',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="score-circle ${score >= totalScore * 0.6 ? \'pass\' : \'fail\'}">${score !== null ? score : \'-\'}<span class="score-total">/${totalScore}</span></div>',
                                        className: 'result-score'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="result-status ${isPassed ? \'pass\' : \'fail\'}">${isPassed ? \'通过\' : \'未通过\'}</div>',
                                        className: 'ml-3 result-status-text',
                                        visibleOn: 'score !== null'
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
                                headerClassName: 'answer-details-header',
                                body: [
                                    {
                                        type: 'each',
                                        name: 'answers',
                                        items: {
                                            type: 'panel',
                                            className: 'question-item question-result-item',
                                            headerClassName: 'question-result-header ${isCorrect ? \'correct\' : \'incorrect\'}',
                                            bodyClassName: 'question-result-body',
                                            title: {
                                                type: 'tpl',
                                                tpl: '<div class="question-result-title"><span class="question-index">${$index + 1}.</span> <span class="question-content">${content}</span><span class="question-score">${obtainedScore || 0}/${score}分</span></div>'
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
                                className: 'exam-actions',
                                items: [
                                    {
                                        type: 'button',
                                        label: '返回首页',
                                        level: 'primary',
                                        size: 'lg',
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
            // 全局样式
            ':root': {
                '--primary-color': '#3f51b5',
                '--success-color': '#4caf50',
                '--warning-color': '#ff9800',
                '--danger-color': '#f44336',
                '--border-radius': '8px',
                '--box-shadow': '0 4px 12px rgba(0,0,0,0.1)'
            },
            'body': {
                'background-color': '#f5f7fa',
                'font-family': '"PingFang SC", "Microsoft YaHei", sans-serif',
                'color': '#333'
            },
            // 头部导航样式
            '.client-header': {
                'background-color': '#fff',
                'box-shadow': 'var(--box-shadow)',
                'padding': '12px 24px',
                'position': 'sticky',
                'top': '0',
                'z-index': '100',
                'border-bottom': '1px solid #eaeaea'
            },
            '.client-logo': {
                'display': 'flex',
                'align-items': 'center'
            },
            '.client-logo img': {
                'height': '36px',
                'margin-right': '12px',
                'transition': 'transform 0.3s ease'
            },
            '.client-logo img:hover': {
                'transform': 'scale(1.05)'
            },
            '.client-logo span': {
                'font-size': '20px',
                'font-weight': 'bold',
                'color': 'var(--primary-color)',
                'letter-spacing': '0.5px'
            },
            // 用户信息样式
            '.user-info-container': {
                'margin-right': '20px'
            },
            '.user-info': {
                'font-size': '16px',
                'font-weight': '500',
                'color': '#333',
                'background-color': '#f9f9f9',
                'padding': '8px 16px',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)',
                'transition': 'all 0.3s ease'
            },
            '.user-info:hover': {
                'background-color': '#f0f2f5'
            },
            // 结果容器样式
            '.result-container': {
                'max-width': '1100px',
                'margin': '20px auto',
                'padding': '0 20px'
            },
            // 面板样式
            '.exam-panel': {
                'border-radius': 'var(--border-radius)',
                'overflow': 'hidden',
                'box-shadow': 'var(--box-shadow)',
                'border': 'none',
                'margin-bottom': '30px'
            },
            '.exam-panel-header': {
                'background-color': '#fff',
                'border-bottom': '1px solid #eaeaea',
                'padding': '15px 20px',
                'text-align': 'center',
                'background-image': 'linear-gradient(to right, rgba(63, 81, 181, 0.1), rgba(63, 81, 181, 0.05), rgba(63, 81, 181, 0))'
            },
            '.exam-panel-header .am-Panel-title': {
                'font-size': '24px !important',
                'font-weight': 'bold !important',
                'color': 'var(--primary-color) !important',
                'text-shadow': '0 1px 2px rgba(0,0,0,0.1) !important',
                'letter-spacing': '1px !important'
            },
            // 考试信息样式
            '.result-panel-body': {
                'background-color': '#fff',
                'padding': '20px'
            },
            '.exam-info': {
                'margin-bottom': '20px',
                'flex-wrap': 'wrap',
                'background-color': '#f9f9f9',
                'padding': '15px',
                'border-radius': 'var(--border-radius)'
            },
            '.exam-info-item': {
                'margin-right': '20px',
                'margin-bottom': '10px',
                'padding': '8px 15px',
                'background-color': '#fff',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 2px 5px rgba(0,0,0,0.05)',
                'border-left': '3px solid var(--primary-color)',
                'font-weight': '500'
            },
            // 分数显示
            '.result-score-container': {
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'margin': '30px 0'
            },
            '.score-circle': {
                'width': '150px',
                'height': '150px',
                'border-radius': '50%',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'font-size': '42px',
                'font-weight': 'bold',
                'color': '#fff',
                'position': 'relative',
                'box-shadow': '0 10px 20px rgba(0,0,0,0.15)',
                'transition': 'all 0.3s ease',
                'animation': 'scorePulse 1.5s ease-in-out'
            },
            '@keyframes scorePulse': {
                '0%': {
                    'transform': 'scale(0.8)',
                    'opacity': '0'
                },
                '100%': {
                    'transform': 'scale(1)',
                    'opacity': '1'
                }
            },
            '.score-circle.pass': {
                'background-color': 'var(--success-color)',
                'border': '5px solid rgba(76, 175, 80, 0.3)'
            },
            '.score-circle.fail': {
                'background-color': 'var(--danger-color)',
                'border': '5px solid rgba(244, 67, 54, 0.3)'
            },
            '.score-circle:hover': {
                'transform': 'translateY(-5px)',
                'box-shadow': '0 15px 30px rgba(0,0,0,0.2)'
            },
            '.score-total': {
                'font-size': '22px',
                'position': 'absolute',
                'right': '30px',
                'bottom': '35px'
            },
            '.result-status': {
                'font-size': '32px',
                'font-weight': 'bold',
                'margin-left': '30px',
                'text-shadow': '0 2px 5px rgba(0,0,0,0.1)'
            },
            '.result-status.pass': {
                'color': 'var(--success-color)'
            },
            '.result-status.fail': {
                'color': 'var(--danger-color)'
            },
            '.result-status-text': {
                'animation': 'fadeInRight 1s ease-in-out'
            },
            '@keyframes fadeInRight': {
                '0%': {
                    'transform': 'translateX(20px)',
                    'opacity': '0'
                },
                '100%': {
                    'transform': 'translateX(0)',
                    'opacity': '1'
                }
            },
            // 评语样式
            '.comments-container': {
                'background-color': '#f9f9f9',
                'padding': '20px',
                'border-radius': 'var(--border-radius)',
                'margin': '20px 0',
                'border-left': '4px solid var(--primary-color)',
                'box-shadow': '0 2px 8px rgba(0,0,0,0.05)'
            },
            '.comments-title': {
                'font-weight': 'bold',
                'margin-bottom': '10px',
                'color': 'var(--primary-color)',
                'font-size': '16px'
            },
            '.comments-content': {
                'line-height': '1.6',
                'white-space': 'pre-wrap'
            },
            // 题目样式
            '.answer-details-panel': {
                'border': 'none',
                'margin-top': '20px'
            },
            '.answer-details-header': {
                'background-color': '#f5f7fa',
                'border-bottom': '1px solid #eaeaea',
                'padding': '15px',
                'border-radius': 'var(--border-radius) var(--border-radius) 0 0'
            },
            '.answer-details-header .am-Panel-title': {
                'font-size': '18px',
                'font-weight': 'bold',
                'color': 'var(--primary-color)'
            },
            '.question-item': {
                'margin-bottom': '30px',
                'border': '1px solid #e8e8e8',
                'border-radius': 'var(--border-radius)',
                'overflow': 'hidden',
                'transition': 'all 0.3s ease',
                'position': 'relative'
            },
            '.question-item:hover': {
                'box-shadow': '0 5px 15px rgba(0,0,0,0.08)',
                'transform': 'translateY(-2px)'
            },
            '.question-result-header': {
                'padding': '15px 20px'
            },
            '.question-result-header.correct': {
                'background-color': 'rgba(76, 175, 80, 0.1)',
                'border-bottom': '2px solid var(--success-color)'
            },
            '.question-result-header.incorrect': {
                'background-color': 'rgba(244, 67, 54, 0.1)',
                'border-bottom': '2px solid var(--danger-color)'
            },
            '.question-result-title': {
                'display': 'flex',
                'align-items': 'center',
                'font-size': '16px'
            },
            '.question-index': {
                'font-weight': 'bold',
                'margin-right': '10px',
                'color': 'var(--primary-color)'
            },
            '.question-content': {
                'flex': '1'
            },
            '.question-score': {
                'font-weight': 'bold',
                'font-size': '14px',
                'padding': '3px 10px',
                'border-radius': '12px',
                'margin-left': '10px'
            },
            '.correct .question-score': {
                'background-color': 'rgba(76, 175, 80, 0.1)',
                'color': 'var(--success-color)'
            },
            '.incorrect .question-score': {
                'background-color': 'rgba(244, 67, 54, 0.1)',
                'color': 'var(--danger-color)'
            },
            '.question-result-body': {
                'padding': '20px'
            },
            '.answer-label': {
                'font-weight': 'bold',
                'color': '#666',
                'margin-bottom': '10px',
                'font-size': '15px'
            },
            '.user-answer': {
                'background-color': '#f8f9fa',
                'padding': '15px',
                'border-radius': 'var(--border-radius)',
                'white-space': 'pre-wrap',
                'border-left': '3px solid #aaa',
                'margin-bottom': '20px',
                'font-family': 'Consolas, monospace',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)'
            },
            '.correct-answer': {
                'background-color': 'rgba(76, 175, 80, 0.05)',
                'padding': '15px',
                'border-radius': 'var(--border-radius)',
                'border-left': '3px solid var(--success-color)',
                'white-space': 'pre-wrap',
                'font-family': 'Consolas, monospace',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)'
            },
            // 操作按钮样式
            '.exam-actions': {
                'margin-top': '30px',
                'margin-bottom': '20px'
            },
            '.am-Button--primary': {
                'background-color': 'var(--primary-color)',
                'border-color': 'var(--primary-color)',
                'padding': '10px 24px',
                'font-size': '16px',
                'border-radius': 'var(--border-radius)',
                'transition': 'all 0.3s ease'
            },
            '.am-Button--primary:hover': {
                'background-color': '#303f9f',
                'transform': 'translateY(-2px)',
                'box-shadow': '0 5px 15px rgba(63, 81, 181, 0.3)'
            },
            // 分割线样式
            '.am-Divider': {
                'margin': '20px 0',
                'background-color': '#eaeaea'
            },
            // 响应式样式
            '@media (max-width: 768px)': {
                '.result-container': {
                    'padding': '10px'
                },
                '.exam-info': {
                    'flex-direction': 'column'
                },
                '.exam-info-item': {
                    'width': '100%',
                    'margin-right': '0'
                },
                '.client-header': {
                    'padding': '10px 15px'
                },
                '.client-logo span': {
                    'font-size': '18px'
                },
                '.result-score-container': {
                    'flex-direction': 'column'
                },
                '.score-circle': {
                    'width': '120px',
                    'height': '120px',
                    'font-size': '36px'
                },
                '.result-status': {
                    'margin-left': '0',
                    'margin-top': '15px'
                },
                '.question-result-title': {
                    'flex-direction': 'column',
                    'align-items': 'flex-start'
                },
                '.question-score': {
                    'margin-left': '0',
                    'margin-top': '5px',
                    'align-self': 'flex-start'
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
            locale: 'zh-CN',
            data: {
                timer: {
                    displayText: '',
                    hours: 0,
                    minutes: 0,
                    seconds: 0,
                    remainingSeconds: 0
                }
            }
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