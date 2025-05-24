(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 简化后的日志函数
    window.log = function(message, level = 'log') {
        const now = new Date();
        const timeStr = now.toLocaleTimeString() + '.' + now.getMilliseconds().toString().padStart(3, '0');
        const logMessage = `[${timeStr}] ${message}`;
        console[level](logMessage);
        return logMessage;
    };
    
    // 全局数据存储
    window.globalData = {
        user: { id: null, name: '', avatar: '', roles: [] },
        profile: { name: '', studentNumber: '', idNo: '', gender: '', admissionTicket: '' },
        practice: { statistics: {}, settings: [] }
    };

    // 简化的全局数据访问方法
    window.GlobalData = {
        get: function (path, defaultValue) {
            const keys = path.split('.');
            let current = window.globalData;
            for (let i = 0; i < keys.length; i++) {
                if (current === undefined || current === null) return defaultValue;
                current = current[keys[i]];
            }
            return current !== undefined ? current : defaultValue;
        },
        set: function (path, value) {
            const keys = path.split('.');
            let current = window.globalData;
            for (let i = 0; i < keys.length - 1; i++) {
                if (current[keys[i]] === undefined) current[keys[i]] = {};
                current = current[keys[i]];
            }
            current[keys[keys.length - 1]] = value;
            return value;
        },
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
                        if (current[keys[i]] === undefined) current[keys[i]] = {};
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

    // 简化的初始化函数
    window.onAmisInitialized = function() {
        window.amisInitialized = true;
        
        // 检测触摸设备并适配
        if ('ontouchstart' in document.documentElement) {
            document.body.classList.add('touch-device');
            
            // 为触摸设备添加活跃状态样式
            document.querySelectorAll('.info-item, .practice-info-item').forEach(item => {
                item.addEventListener('touchstart', function() {
                    this.classList.add('touch-active');
                }, {passive: true});
                
                item.addEventListener('touchend', function() {
                    this.classList.remove('touch-active');
                }, {passive: true});
            });
        }
        
        // 监听窗口大小变化
        const checkWindowSize = function() {
            const isMobile = window.innerWidth <= 768;
            const isSmallMobile = window.innerWidth <= 576;
            document.body.classList.toggle('mobile-view', isMobile);
            document.body.classList.toggle('small-mobile-view', isSmallMobile);
        };
        
        window.addEventListener('resize', checkWindowSize);
        checkWindowSize(); // 初始检查
    };

    // 应用配置
    var app = {
        type: 'page',
        title: window.siteSettings ? window.siteSettings.clientAppName : '练习系统',
        body: [
            // 欢迎部分
            {
                type: 'service',
                api: '/identity/api/identity/profile',
                className: 'client-welcome-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h2><i class="fa fa-graduation-cap welcome-icon"></i> 欢迎您，${user.name}</h2><p><i class="fa fa-calendar welcome-icon"></i> 今天是 ${now|date:YYYY-MM-DD}，祝您练习顺利！</p>',
                        className: 'welcome-message'
                    },
                    {
                        type: 'button',
                        label: '返回首页',
                        icon: 'fa fa-home',
                        level: 'default',
                        actionType: 'link',
                        link: '/client/index'
                    }
                ],
                data: { now: new Date() }
            },
            { type: 'divider' },
            
            // 考生个人信息部分
            {
                type: 'service',
                api: '/exam/api/exam/client/profile',
                className: 'student-profile-section',
                data: {
                    name: '',
                    studentNumber: '',
                    idNo: '',
                    gender: '',
                    admissionTicket: '',
                    phoneNumber: '',
                    studentGroups: []
                },
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-user-circle section-icon"></i> 考生信息</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'card',
                        header: {
                            title: '个人信息',
                            subTitle: '考生基本资料',
                            avatarText: '信息'
                        },
                        body: [
                            {
                                type: 'flex',
                                justify: 'flex-start',
                                alignItems: 'stretch',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-user info-icon"></i><span class="info-label">姓名</span><span class="info-value">${name}</span></div>',
                                        className: 'flex-info-item',
                                        columnClassName: 'w-sm-12 w-md-4'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-id-card-o info-icon"></i><span class="info-label">学号</span><span class="info-value">${studentNumber}</span></div>',
                                        className: 'flex-info-item',
                                        columnClassName: 'w-sm-12 w-md-4'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-venus-mars info-icon"></i><span class="info-label">性别</span><span class="info-value">${gender}</span></div>',
                                        className: 'flex-info-item',
                                        columnClassName: 'w-sm-12 w-md-4'
                                    }
                                ]
                            }
                        ],
                        className: 'profile-card'
                    }
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        if (event.data) {
                                            // 保存到全局数据
                                            window.globalData.profile = event.data;
                                            console.log("考生信息数据:", event.data);
                                        }
                                    } catch (error) {
                                        console.error('处理考生信息数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            },
            { type: 'divider' },
            
            // 练习统计数据部分
            {
                type: 'service',
                api: '/exam/api/client/practice/analysis',
                className: 'practice-stats-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-bar-chart section-icon"></i> 练习统计</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'grid',
                        columns: [
                            {
                                md: 3,
                                //sm: 6,
                                body: {
                                    type: 'wrapper',
                                    className: 'stat-card',
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-icon"><i class="fa fa-clock-o"></i></div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-value">${totalPracticeTime || 0}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-label">累计练习时间(分钟)</div>'
                                        }
                                    ]
                                }
                            },
                            {
                                md: 3,
                                //sm: 6,
                                body: {
                                    type: 'wrapper',
                                    className: 'stat-card',
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-icon"><i class="fa fa-file-text-o"></i></div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-value">${totalPracticeCount || 0}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-label">完成练习次数</div>'
                                        }
                                    ]
                                }
                            },
                            {
                                md: 3,
                                //sm: 6,
                                body: {
                                    type: 'wrapper',
                                    className: 'stat-card',
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-icon"><i class="fa fa-question-circle"></i></div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-value">${totalQuestionCount || 0}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-label">已答题目总数</div>'
                                        }
                                    ]
                                }
                            },
                            {
                                md: 3,
                                //sm: 6,
                                body: {
                                    type: 'wrapper',
                                    className: 'stat-card',
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-icon"><i class="fa fa-check-circle"></i></div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-value">${correctRate || "0%"}</div>'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="stat-label">正确率</div>'
                                        }
                                    ]
                                }
                            }
                        ]
                    }
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        if (event.data) {
                                            // 保存到全局数据
                                            window.globalData.practice.statistics = event.data;
                                            console.log("练习统计数据:", event.data);
                                            
                                            // 计算正确率
                                            if (event.data.totalAnsweredCount > 0) {
                                                const correctRate = Math.round((event.data.totalCorrectCount / event.data.totalAnsweredCount) * 100) + '%';
                                                event.data.correctRate = correctRate;
                                            } else {
                                                event.data.correctRate = "0%";
                                            }
                                        }
                                    } catch (error) {
                                        console.error('处理练习统计数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            },
            { type: 'divider' },
            
            // 练习列表部分
            {
                type: 'service',
                api: '/exam/api/client/practice/settings',
                className: 'practice-list-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-list-alt section-icon"></i> 可用练习</h3>',
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
                                        subTitle: '${questionCount}题',
                                        avatarText: '练习'
                                    },
                                    body: [
                                        {
                                            type: 'flex',
                                            justify: 'flex-start',
                                            alignItems: 'stretch',
                                            items: [
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="practice-info-item"><i class="fa fa-tag practice-icon"></i><span class="practice-info-label">类型</span><span class="practice-info-value">${categoryName || "通用练习"}</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="practice-info-item"><i class="fa fa-clock-o practice-icon"></i><span class="practice-info-label">时间</span><span class="practice-info-value">${timeLimit || "不限"} 分钟</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                }
                                            ]
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="practice-info-item"><i class="fa fa-file-text-o practice-icon"></i><span class="practice-info-label">说明</span><span class="practice-info-value">${description || "暂无说明"}</span></div>',
                                            className: 'flex-info-item w-100'
                                        }
                                    ],
                                    actions: [
                                        {
                                            type: 'button',
                                            label: '开始练习',
                                            level: 'primary',
                                            actionType: 'ajax',
                                            api: {
                                                url: '/exam/api/client/practice/${id}/start',
                                                method: 'post',
                                                messages: { success: '开始练习...' }
                                            },
                                            redirect: '/client/practice/${id}'
                                        }
                                    ],
                                    className: 'practice-card'
                                },
                                placeholder: {
                                    type: 'tpl',
                                    tpl: '<div class="text-center text-muted">当前没有可用的练习</div>'
                                }
                            }
                        ]
                    }
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        if (event.data && Array.isArray(event.data.items)) {
                                            window.globalData.practice.settings = event.data.items;
                                            console.log("练习设置数据:", event.data.items);
                                        }
                                    } catch (error) {
                                        console.error('处理练习设置数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    },
                    mounted: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        // 触发应用初始化
                                        setTimeout(() => {
                                            if (typeof window.onAmisInitialized === 'function') {
                                                window.onAmisInitialized();
                                            }
                                        }, 10);
                                    } catch (error) {
                                        console.error('挂载后初始化时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            },
            { type: 'divider' },
            
            // 练习记录部分
            {
                type: 'service',
                api: '/exam/api/client/practice/records',
                className: 'practice-history-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-history section-icon"></i> 练习记录</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'table',
                        source: '${items}',
                        columns: [
                            { name: 'practiceName', label: '练习名称' },
                            { name: 'startTime', label: '开始时间', type: 'datetime' },
                            { name: 'duration', label: '用时(分钟)' },
                            { 
                                name: 'score', 
                                label: '得分', 
                                type: 'tpl',
                                tpl: '${score}/${totalScore}'
                            },
                            { 
                                name: 'correctRate', 
                                label: '正确率',
                                type: 'progress',
                                progressClassName: 'progress-xs',
                                map: {
                                    '0': 'danger',
                                    '30': 'warning',
                                    '60': 'info',
                                    '80': 'success'
                                }
                            },
                            {
                                type: 'operation',
                                label: '操作',
                                buttons: [
                                    {
                                        label: '查看详情',
                                        type: 'button',
                                        actionType: 'link',
                                        link: '/client/practice/result/${id}'
                                    }
                                ]
                            }
                        ],
                        placeholder: '没有练习记录'
                    }
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        if (event.data && Array.isArray(event.data.items)) {
                                            // 处理每条记录，计算正确率
                                            event.data.items.forEach(item => {
                                                if (item.totalQuestions > 0) {
                                                    item.correctRate = Math.round((item.correctCount / item.totalQuestions) * 100);
                                                } else {
                                                    item.correctRate = 0;
                                                }
                                            });
                                            console.log("练习记录数据:", event.data.items);
                                        }
                                    } catch (error) {
                                        console.error('处理练习记录数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            }
        ]
    };

    // 简化AMIS实例初始化
    let amisInstance = amis.embed(
        '#root',
        app,
        {
            location: history.location,
            data: { date: new Date() },
            context: {
                WEB_HOST: webHost
            },
            locale: 'zh-CN'
        },
        {
            requestAdaptor: (api) => {
                const token = localStorage.getItem('token');
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

                // 处理用户信息
                if (api.url.includes('/identity/api/identity/profile')) {
                    if (payload.status === 0 && payload.data) {
                        window.GlobalData.set('user.id', payload.data.id || null);
                        window.GlobalData.set('user.name', payload.data.name || payload.data.userName || '');
                        window.GlobalData.set('user.avatar', payload.data.avatar || '');
                        window.GlobalData.set('user.roles', payload.data.roles || []);
                        window.GlobalData.syncToAmis(amisInstance);
                    }
                }

                return payload;
            },
            theme: 'antd'
        }
    );

    // 路由监听
    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})(); 