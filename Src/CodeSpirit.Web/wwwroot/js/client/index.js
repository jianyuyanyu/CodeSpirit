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
        title: '考试系统',
        header: {
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
                            className: 'user-info',
                            items: [
                                {
                                    type: 'avatar',
                                    src: '${user.avatar}',
                                    text: '${user.name}',
                                    icon: 'fa fa-user',
                                    className: 'mr-2',
                                    size: 30
                                },
                                {
                                    type: 'dropdown-button',
                                    label: '${user.name}',
                                    align: 'right',
                                    className: 'ml-2',
                                    buttons: [
                                        {
                                            type: 'button',
                                            label: '个人信息',
                                            icon: 'fa fa-address-card',
                                            actionType: 'dialog',
                                            dialog: {
                                                title: '个人信息',
                                                size: 'md',
                                                body: {
                                                    type: 'form',
                                                    api: '/identity/api/identity/profile',
                                                    controls: [
                                                        {
                                                            type: 'image',
                                                            name: 'avatar',
                                                            label: '头像',
                                                            thumbMode: 'cover',
                                                            thumbRatio: '1:1',
                                                            width: 100,
                                                            height: 100,
                                                            className: 'rounded-full'
                                                        },
                                                        {
                                                            type: 'static',
                                                            name: 'userName',
                                                            label: '用户名'
                                                        },
                                                        {
                                                            type: 'static',
                                                            name: 'email',
                                                            label: '邮箱'
                                                        },
                                                        {
                                                            type: 'static',
                                                            name: 'phoneNumber',
                                                            label: '手机号'
                                                        }
                                                    ]
                                                }
                                            }
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
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        },
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
                                            tpl: '<div><span class="text-muted">开始时间：</span>${startTime|date:YYYY-MM-DD HH:mm}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div><span class="text-muted">结束时间：</span>${endTime|date:YYYY-MM-DD HH:mm}</div>'
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
                                            actionType: 'link',
                                            link: '/client/exam/${id}',
                                            visibleOn: "status === '进行中'"
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
            '.client-header': {
                'background-color': '#fff',
                'box-shadow': '0 2px 4px rgba(0,0,0,0.1)',
                'padding': '10px 20px'
            },
            '.client-logo': {
                'display': 'flex',
                'align-items': 'center'
            },
            '.client-logo img': {
                'height': '32px',
                'margin-right': '10px'
            },
            '.client-logo span': {
                'font-size': '18px',
                'font-weight': 'bold'
            },
            '.client-welcome-section': {
                'padding': '20px',
                'background-color': '#f8f9fa',
                'border-radius': '8px',
                'margin': '20px'
            },
            '.section-title': {
                'margin-bottom': '15px',
                'padding-left': '20px'
            },
            '.exam-list-section, .exam-history-section': {
                'padding': '10px',
                'margin': '0 20px'
            },
            '.exam-card': {
                'height': '100%',
                'transition': 'all 0.3s',
                'margin-bottom': '15px'
            },
            '.exam-card:hover': {
                'transform': 'translateY(-5px)',
                'box-shadow': '0 5px 15px rgba(0,0,0,0.1)'
            },
            '@media (max-width: 768px)': {
                '.client-header': {
                    'padding': '10px'
                },
                '.exam-list-section, .exam-history-section, .client-welcome-section': {
                    'margin': '10px',
                    'padding': '10px'
                },
                '.client-logo span': {
                    'font-size': '16px'
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