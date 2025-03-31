(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    // 全局数据对象，用于存储用户信息和其他共享数据
    window.globalData = {
        user: {
            id: null,
            name: '',
            avatar: '',
            roles: []
        },
        notifications: {
            count: 0,
            hasUnread: false,
            items: []
        },
        // 可以添加其他全局数据
        settings: {},
        permissions: []
    };

    // 全局数据辅助函数
    window.GlobalData = {
        // 获取数据
        get: function (path, defaultValue) {
            const keys = path.split('.');
            let current = window.globalData;

            for (let i = 0; i < keys.length; i++) {
                if (current === undefined || current === null) {
                    return defaultValue;
                }
                current = current[keys[i]];
            }

            return current !== undefined ? current : defaultValue;
        },

        // 设置数据
        set: function (path, value) {
            const keys = path.split('.');
            let current = window.globalData;

            for (let i = 0; i < keys.length - 1; i++) {
                if (current[keys[i]] === undefined) {
                    current[keys[i]] = {};
                }
                current = current[keys[i]];
            }

            current[keys[keys.length - 1]] = value;
            return value;
        },

        // 将全局数据同步到amis上下文
        syncToAmis: function (amisInstance, selectedPaths) {
            if (!amisInstance) return;

            const data = {};
            if (selectedPaths && Array.isArray(selectedPaths)) {
                selectedPaths.forEach(path => {
                    const keys = path.split('.');
                    let current = data;
                    let source = window.globalData;

                    for (let i = 0; i < keys.length - 1; i++) {
                        if (source[keys[i]] === undefined) break;

                        if (current[keys[i]] === undefined) {
                            current[keys[i]] = {};
                        }
                        current = current[keys[i]];
                        source = source[keys[i]];
                    }

                    current[keys[keys.length - 1]] = source[keys[keys.length - 1]];
                });
            } else {
                Object.assign(data, window.globalData);
            }

            amisInstance.updateProps({ data });
        }
    };

    var app = {
        type: 'page',
        title: window.siteSettings ? window.siteSettings.clientAppName : '考试系统',
        body: [
            {
                type: 'service',
                api: '/identity/api/identity/profile',
                className: 'client-welcome-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h2>欢迎您，${user.name}</h2><p>今天是 ${now|date:YYYY-MM-DD}，祝您考试顺利！</p>',
                        className: 'welcome-message'
                    },
                    {
                        type: 'button',
                        label: '退出登录',
                        icon: 'fa fa-sign-out',
                        level: 'danger',
                        actionType: 'ajax',
                        confirmText: '确认要退出登录？',
                        api: '/identity/api/identity/auth/logout',
                        reload: 'none',
                        redirect: '/client/login'
                    }
                ],
                data: {
                    now: new Date()
                }
            },
            {
                type: 'divider'
            },
            {
                type: 'service',
                api: '/exam/api/exam/client/available',
                className: 'exam-list-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3>可参加的考试</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'grid',
                        columns: [
                            {
                                type: 'each',
                                name: 'items',
                                items: {
                                    type: 'card',
                                    header: {
                                        title: '${name}',
                                        subTitle: '${duration}分钟',
                                        avatarText: '考试'
                                    },
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div><span class="text-muted">开始时间：</span>${startTime}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div><span class="text-muted">结束时间：</span>${endTime}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div><span class="text-muted">总分：</span>${totalScore}分</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div><span class="text-muted">状态：</span><span class="label label-${status === \'进行中\' ? \'success\' : (status === \'未开始\' ? \'info\' : \'danger\')}">${status}</span></div>'
                                        }
                                    ],
                                    actions: [
                                        {
                                            type: 'button',
                                            label: '开始考试',
                                            level: 'primary',
                                            actionType: 'ajax',
                                            api: {
                                                url: '/exam/api/exam/client/${id}/start',
                                                method: 'post',
                                                messages: {
                                                    success: '开始考试...'
                                                }
                                            },
                                            visibleOn: "status === '进行中'",
                                            redirect: '/client/exam/${id}'
                                        },
                                        {
                                            type: 'button',
                                            label: '查看成绩',
                                            level: 'info',
                                            actionType: 'link',
                                            link: '/client/exam/result/${id}',
                                            visibleOn: "status === '已结束' && hasResult"
                                        }
                                    ],
                                    className: 'exam-card'
                                },
                                placeholder: {
                                    type: 'tpl',
                                    tpl: '<div class="text-center text-muted">当前没有可参加的考试</div>'
                                }
                            }
                        ]
                    }
                ]
            },
            {
                type: 'divider'
            },
            {
                type: 'service',
                api: '/exam/api/exam/client/history',
                className: 'exam-history-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3>历史考试记录</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'table',
                        source: '${items}',
                        columns: [
                            {
                                name: 'name',
                                label: '考试名称'
                            },
                            {
                                name: 'startTime',
                                label: '考试时间',
                                type: 'datetime'
                            },
                            {
                                name: 'score',
                                label: '得分'
                            },
                            {
                                name: 'totalScore',
                                label: '总分'
                            },
                            {
                                name: 'status',
                                label: '状态',
                                type: 'status'
                            },
                            {
                                type: 'operation',
                                label: '操作',
                                buttons: [
                                    {
                                        label: '查看详情',
                                        type: 'button',
                                        actionType: 'link',
                                        link: '/client/exam/result/${id}'
                                    }
                                ]
                            }
                        ],
                        placeholder: '没有历史考试记录'
                    }
                ]
            }
        ],
        css: {
            ':root': {
                '--primary-color': '#3f51b5',
                '--secondary-color': '#ff4081',
                '--text-color': '#333',
                '--light-bg': '#f5f7fa',
                '--border-radius': '8px',
                '--box-shadow': '0 4px 12px rgba(0,0,0,0.08)'
            },
            'body': {
                'font-family': '"Segoe UI", "Microsoft YaHei", sans-serif',
                'color': 'var(--text-color)',
                'background-color': '#f9fafc'
            },
            '.client-header': {
                'background-color': '#fff',
                'box-shadow': '0 2px 10px rgba(0,0,0,0.06)',
                'padding': '12px 24px',
                'position': 'sticky',
                'top': '0',
                'z-index': '100'
            },
            '.client-logo': {
                'display': 'flex',
                'align-items': 'center'
            },
            '.client-logo img': {
                'height': '38px',
                'margin-right': '12px',
                'transition': 'transform 0.3s ease'
            },
            '.client-logo:hover img': {
                'transform': 'scale(1.05)'
            },
            '.client-logo span': {
                'font-size': '20px',
                'font-weight': 'bold',
                'color': 'var(--primary-color)',
                'letter-spacing': '0.5px'
            },
            '.client-welcome-section': {
                'padding': '30px',
                'background': 'linear-gradient(135deg, #fff, var(--light-bg))',
                'border-radius': 'var(--border-radius)',
                'margin': '30px 25px 20px',
                'box-shadow': 'var(--box-shadow)',
                'border-left': '4px solid var(--primary-color)'
            },
            '.welcome-message h2': {
                'margin-bottom': '10px',
                'color': 'var(--primary-color)',
                'font-size': '24px'
            },
            '.welcome-message p': {
                'color': '#666',
                'font-size': '16px'
            },
            '.section-title': {
                'margin-bottom': '20px',
                'padding-left': '25px',
                'font-size': '20px',
                'font-weight': '600',
                'color': 'var(--primary-color)',
                'position': 'relative',
                'line-height': '1.5',
                'display': 'flex',
                'align-items': 'center'
            },
            '.section-title:before': {
                'content': '""',
                'position': 'absolute',
                'left': '0',
                'height': '18px',
                'width': '4px',
                'background-color': 'var(--primary-color)',
                'border-radius': '2px'
            },
            '.exam-list-section, .exam-history-section': {
                'padding': '15px',
                'margin': '0 25px 25px',
                'background-color': '#fff',
                'border-radius': 'var(--border-radius)',
                'box-shadow': 'var(--box-shadow)'
            },
            '.exam-card': {
                'height': '100%',
                'transition': 'all 0.3s ease',
                'margin-bottom': '20px',
                'border': '1px solid #eaeaea',
                'border-radius': 'var(--border-radius)',
                'overflow': 'hidden'
            },
            '.exam-card:hover': {
                'transform': 'translateY(-5px)',
                'box-shadow': '0 8px 20px rgba(0,0,0,0.12)'
            },
            '.exam-card .cxd-Card-header': {
                'background-color': 'var(--light-bg)',
                'border-bottom': '1px solid #eaeaea'
            },
            '.exam-card .cxd-Card-title': {
                'font-weight': '600',
                'color': 'var(--primary-color)'
            },
            '.exam-card .cxd-Card-body': {
                'padding': '16px'
            },
            '.exam-card .cxd-Card-actions': {
                'background-color': '#f8fafd',
                'padding': '10px 16px'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary, .cxd-Button--primary': {
                'background-color': 'var(--primary-color) !important',
                'border-color': 'var(--primary-color) !important',
                'padding': '6px 16px !important',
                'font-weight': '500 !important',
                'letter-spacing': '0.5px !important',
                'box-shadow': '0 2px 6px rgba(63, 81, 181, 0.25) !important',
                'transition': 'all 0.3s ease !important'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary:hover, .cxd-Button--primary:hover': {
                'background-color': '#303f9f !important',
                'border-color': '#303f9f !important',
                'box-shadow': '0 4px 12px rgba(63, 81, 181, 0.4) !important',
                'transform': 'translateY(-2px) !important'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary:active, .cxd-Button--primary:active': {
                'transform': 'translateY(0) !important',
                'box-shadow': '0 2px 4px rgba(63, 81, 181, 0.3) !important'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary, .exam-card .cxd-Button--primary': {
                'position': 'relative !important',
                'overflow': 'hidden !important'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary:before, .exam-card .cxd-Button--primary:before': {
                'content': '""',
                'position': 'absolute',
                'top': '0',
                'left': '-100%',
                'width': '100%',
                'height': '100%',
                'background': 'linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent)',
                'transition': 'all 0.6s ease',
                'z-index': '1'
            },
            '.exam-card .cxd-Card-actions .cxd-Button--primary:hover:before, .exam-card .cxd-Button--primary:hover:before': {
                'left': '100%'
            },
            '.cxd-Button--primary span': {
                'position': 'relative',
                'z-index': '2'
            },
            '.cxd-Button--info': {
                'background-color': '#03a9f4',
                'border-color': '#03a9f4'
            },
            '.cxd-Table': {
                'border-radius': 'var(--border-radius)',
                'overflow': 'hidden',
                'box-shadow': '0 0 0 1px rgba(0,0,0,0.05)'
            },
            '.cxd-Table-headCell': {
                'background-color': 'var(--light-bg)',
                'font-weight': '600'
            },
            '.label': {
                'padding': '3px 8px',
                'border-radius': '12px',
                'font-size': '12px',
                'display': 'inline-block'
            },
            '.label-success': {
                'background-color': '#e8f5e9',
                'color': '#2e7d32'
            },
            '.label-info': {
                'background-color': '#e3f2fd',
                'color': '#1565c0'
            },
            '.label-danger': {
                'background-color': '#ffebee',
                'color': '#c62828'
            },
            '.user-info': {
                'display': 'flex',
                'align-items': 'center'
            },
            '.user-info .cxd-Avatar': {
                'border': '2px solid rgba(63, 81, 181, 0.2)'
            },
            '.cxd-Divider': {
                'margin': '10px 25px',
                'background-color': '#eaeaea'
            },
            '@media (max-width: 768px)': {
                '.client-header': {
                    'padding': '10px 15px'
                },
                '.exam-list-section, .exam-history-section, .client-welcome-section': {
                    'margin': '15px',
                    'padding': '15px'
                },
                '.client-logo span': {
                    'font-size': '18px'
                },
                '.welcome-message h2': {
                    'font-size': '20px'
                },
                '.section-title': {
                    'font-size': '18px',
                    'padding-left': '20px'
                }
            }
        }
    };

    let amisInstance = amis.embed(
        '#root',
        app,
        {
            location: history.location,
            data: {
                date: new Date()
            },
            context: {
                API_HOST: apiHost,
                WEB_HOST: webHost,
                aspire_dashboard: aspire_dashboard
            },
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
                    return { msg: '您没有权限访问此页面，请联系管理员！' }
                }
                else if (response.status === 401) {
                    window.location.href = `/client/login`;
                    return { msg: '登录过期！' };
                }

                // 如果是获取用户信息的接口,将数据注入到全局
                if (api.url.includes('/identity/api/identity/profile')) {
                    // 更新全局数据对象
                    if (payload.status === 0 && payload.data) {
                        window.GlobalData.set('user.id', payload.data.id || null);
                        window.GlobalData.set('user.name', payload.data.name || payload.data.userName || '');
                        window.GlobalData.set('user.avatar', payload.data.avatar || '');
                        window.GlobalData.set('user.roles', payload.data.roles || []);

                        // 同时注入到amis全局上下文，使所有组件都能访问
                        window.GlobalData.syncToAmis(amisInstance);

                        console.debug('Global user data updated:', window.globalData.user);
                    }
                }

                return payload;
            },
            theme: 'antd'
        }
    );

    amisInstance.updateProps({
        data: {
            notifications: {
                count: 0,
                hasUnread: false
            }
        }
    });

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})();