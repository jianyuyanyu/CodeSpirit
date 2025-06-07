(function () {
    'use strict';
    
    // 确保TokenManager已初始化为客户端模式（考试系统）
    TokenManager.initClientMode(window.tenantId, 'exam');
    
    // 检查用户认证状态
    if (!TokenManager.isAuthenticated()) {
        window.location.href = `/${window.tenantId}/exam/login`;
        return;
    }
    
    let amis = amisRequire('amis/embed');
    
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
        notifications: { count: 0, hasUnread: false, items: [] },
        countdowns: { items: {}, hasCountdown: false },
        settings: {},
        permissions: [],
        profile: { name: '', email: '', avatar: '', displayName: '' },
        examStats: { availableCount: 0, completedCount: 0, passRate: 0, totalScore: 0, averageScore: 0 }
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
    
    // 用户信息和系统数据
    let userInfo = null;
    let systemStats = null;

    /**
     * 通用API请求函数
     * @param {string} url 请求URL
     * @param {Object} options 请求选项
     * @returns {Promise<Object>} 响应数据
     */
    async function apiRequest(url, options = {}) {
        try {
            const token = TokenManager.getToken();
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Authorization': token ? 'Bearer ' + token : '',
                    'TenantId': window.tenantId,
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });
            
            // 处理HTTP错误状态
            if (response.status === 401) {
                // 认证失败，跳转到登录页
                window.location.href = `/${window.tenantId}/exam/login`;
                throw new Error('认证失败，请重新登录');
            } else if (response.status === 403) {
                throw new Error('您没有权限访问此资源');
            } else if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            
            // 处理业务错误
            if (result.status !== 0) {
                throw new Error(result.msg || '请求失败');
            }
            
            return result.data;
        } catch (error) {
            console.error(`API请求失败 [${url}]:`, error);
            throw error;
        }
    }





    /**
     * 构建页面AMIS配置
     * @param {Object} userInfo 用户信息
     * @param {Object} stats 统计数据
     * @returns {Object} AMIS配置
     */
    function buildPageConfig(userInfo, stats) {
        return {
            "type": "page",
            "title": "",
            "css": {
                ':root': {
                    '--primary': '#3f51b5',
                    '--secondary': '#ff4081',
                    '--text': '#333',
                    '--light-bg': '#f5f7fa',
                    '--radius': '8px',
                    '--shadow': '0 4px 12px rgba(0,0,0,0.08)'
                },
                'body': {
                    'font-family': '"Segoe UI", "Microsoft YaHei", sans-serif',
                    'color': 'var(--text)',
                    'background-color': '#f9fafc'
                },
                'body.mobile-view': {
                    'font-size': '14px'
                },
                'body.small-mobile-view': {
                    'font-size': '13px'
                },
                'body.touch-device .cxd-Button': {
                    'padding': '7px 12px'
                },
                '.touch-active': {
                    'background-color': 'rgba(63, 81, 181, 0.15) !important',
                    'transform': 'scale(0.98) !important'
                },
                '.exam-welcome-section': {
                    'padding': '20px',
                    'background': 'linear-gradient(135deg, #fff, var(--light-bg))',
                    'border-radius': 'var(--radius)',
                    'margin': '20px 25px 15px',
                    'box-shadow': 'var(--shadow)',
                    'border-left': '4px solid var(--primary)'
                },
                '.welcome-message h2': {
                    'margin-bottom': '10px',
                    'color': 'var(--primary)',
                    'font-size': '24px'
                },
                '.welcome-message p': {
                    'color': '#666',
                    'font-size': '16px'
                },
                '.welcome-icon': {
                    'color': 'var(--primary)',
                    'margin-right': '8px',
                    'width': '22px',
                    'text-align': 'center'
                },
                '.section-title': {
                    'padding-left': '25px',
                    'font-size': '18px',
                    'font-weight': '600',
                    'color': 'var(--primary)',
                    'position': 'relative',
                    'line-height': '1.5',
                    'display': 'flex',
                    'align-items': 'center'
                },
                '.section-title:before': {
                    'content': '""',
                    'position': 'absolute',
                    'left': '0',
                    'height': '16px',
                    'width': '4px',
                    'background-color': 'var(--primary)',
                    'border-radius': '2px'
                },
                '.section-icon': {
                    'color': 'var(--primary)',
                    'margin-right': '8px',
                    'width': '24px',
                    'text-align': 'center'
                },
                '.cxd-Divider': {
                    'margin': '10px 0'
                },
                '.exam-list-section, .exam-history-section, .user-profile-section, .exam-stats-section': {
                    'padding': '12px',
                    'margin': '0 25px 15px',
                    'background-color': '#fff',
                    'border-radius': 'var(--radius)',
                    'box-shadow': 'var(--shadow)'
                },
                '.profile-card': {
                    'border': 'none',
                    'box-shadow': 'none',
                    'margin-bottom': '0'
                },
                '.profile-card .cxd-Card-header': {
                    'background-color': 'transparent',
                    'border-bottom': 'none',
                    'padding-left': '0'
                },
                '.profile-card .cxd-Card-title': {
                    'font-weight': '600',
                    'color': 'var(--primary)',
                    'font-size': '18px'
                },
                '.profile-card .cxd-Card-body': {
                    'padding': '5px 15px 15px'
                },
                '.info-item, .exam-info-item': {
                    'margin': '5px 0',
                    'padding': '8px 12px',
                    'border-radius': 'var(--radius)',
                    'background-color': 'rgba(63, 81, 181, 0.03)',
                    'transition': 'all 0.3s ease',
                    'display': 'flex',
                    'align-items': 'center',
                    'height': '100%'
                },
                '.info-item:hover, .exam-info-item:hover': {
                    'background-color': 'rgba(63, 81, 181, 0.07)',
                    'transform': 'translateY(-2px)',
                    'box-shadow': '0 2px 6px rgba(0,0,0,0.05)'
                },
                '.info-label, .exam-info-label': {
                    'color': '#666',
                    'font-weight': '500',
                    'margin-right': '8px',
                    'min-width': '60px',
                    'display': 'inline-block'
                },
                '.info-value, .exam-info-value': {
                    'font-weight': '600',
                    'color': '#333'
                },
                '.info-icon, .exam-icon': {
                    'margin-right': '10px',
                    'width': '20px',
                    'text-align': 'center',
                    'color': 'var(--primary)'
                },
                '.flex-info-item': {
                    'padding': '0 5px',
                    'margin-bottom': '10px',
                    'flex': '1',
                    'min-width': '0'
                },
                '.exam-card': {
                    'height': '100%',
                    'transition': 'all 0.3s ease',
                    'margin-bottom': '20px',
                    'border': '1px solid #eaeaea',
                    'border-radius': 'var(--radius)',
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
                    'color': 'var(--primary)'
                },
                '.exam-description': {
                    'padding': '8px 12px',
                    'border-radius': 'var(--radius)',
                    'background-color': 'rgba(3, 169, 244, 0.03)',
                    'border-left': '3px solid #03a9f4'
                },
                '.stat-card': {
                    'background': 'linear-gradient(135deg, #fff, var(--light-bg))',
                    'border': '1px solid #eaeaea',
                    'transition': 'all 0.3s ease'
                },
                '.stat-card:hover': {
                    'transform': 'translateY(-3px)',
                    'box-shadow': '0 6px 15px rgba(0,0,0,0.1)'
                },
                '.stat-number': {
                    'font-size': '32px',
                    'font-weight': 'bold',
                    'color': 'var(--primary)',
                    'margin-bottom': '5px'
                },
                '.stat-label': {
                    'color': '#666',
                    'font-size': '14px'
                },
                '.cxd-Button--primary': {
                    'background-color': 'var(--primary)',
                    'border-color': 'var(--primary)',
                    'box-shadow': '0 2px 6px rgba(63, 81, 181, 0.25)',
                    'transition': 'all 0.3s ease'
                },
                '.cxd-Button--primary:hover': {
                    'background-color': '#303f9f',
                    'transform': 'translateY(-2px)',
                    'box-shadow': '0 4px 12px rgba(63, 81, 181, 0.4)'
                },
                '.cxd-Button--info': {
                    'background-color': '#03a9f4',
                    'border-color': '#03a9f4'
                },
                '.label': {
                    'padding': '3px 8px',
                    'border-radius': '12px',
                    'font-size': '12px',
                    'display': 'inline-block'
                },
                '.label-success': { 'background-color': '#e8f5e9', 'color': '#2e7d32' },
                '.label-info': { 'background-color': '#e3f2fd', 'color': '#1565c0' },
                '.label-danger': { 'background-color': '#ffebee', 'color': '#c62828' },
                '.label-primary': { 'background-color': '#e8eaf6', 'color': '#3f51b5' },
                '.label-warning': { 'background-color': '#fff3e0', 'color': '#f57c00' },
                '.exam-empty-placeholder': {
                    'padding': '40px 20px',
                    'text-align': 'center',
                    'color': '#999'
                },
                '@media (max-width: 768px)': {
                    '.exam-list-section, .exam-history-section, .exam-welcome-section, .user-profile-section, .exam-stats-section': {
                        'margin': '15px',
                        'padding': '12px'
                    },
                    '.welcome-message h2': { 'font-size': '20px' },
                    '.welcome-message p': { 'font-size': '14px' },
                    '.section-title': {
                        'font-size': '16px',
                        'padding-left': '20px',
                        'margin-bottom': '10px'
                    },
                    '.info-label, .exam-info-label': {
                        'min-width': '55px'
                    },
                    '.w-sm-12': {
                        'width': '100% !important'
                    },
                    '.flex-info-item': {
                        'padding': '0 0 8px 0'
                    },
                    '.info-item, .exam-info-item': {
                        'padding': '8px 10px'
                    },
                    '.profile-card .cxd-Card-body': {
                        'padding': '5px 10px 10px'
                    },
                    '.exam-card': {
                        'margin-bottom': '12px'
                    },
                    '.cxd-Divider': {
                        'margin': '8px 0'
                    }
                },
                '@media (max-width: 576px)': {
                    '.exam-list-section, .exam-history-section, .exam-welcome-section, .user-profile-section, .exam-stats-section': {
                        'margin': '10px',
                        'padding': '10px'
                    },
                    '.info-icon, .exam-icon': {
                        'margin-right': '5px',
                        'font-size': '14px'
                    },
                    '.info-label, .exam-info-label': {
                        'min-width': '45px',
                        'font-size': '13px'
                    },
                    '.info-value, .exam-info-value': {
                        'font-size': '13px'
                    },
                    '.welcome-message h2': { 'font-size': '18px' },
                    '.welcome-message p': { 'font-size': '13px' },
                    '.section-icon': {
                        'margin-right': '5px',
                        'font-size': '14px'
                    }
                },
                '@media (hover: none) and (pointer: coarse)': {
                    '.info-item:active, .exam-info-item:active': {
                        'background-color': 'rgba(63, 81, 181, 0.15)'
                    },
                    '.cxd-Button--primary:active': {
                        'transform': 'translateY(1px)'
                    }
                }
            },
            "body": [
                // 欢迎部分
                {
                    "type": "service",
                    "api": "/identity/api/identity/profile",
                    "className": "exam-welcome-section", 
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": `<h2><i class="fa fa-graduation-cap welcome-icon"></i> 欢迎进入考试系统</h2><p><i class="fa fa-calendar welcome-icon"></i> 欢迎您，\${displayName || userName || '用户'}！开始您的学习之旅吧</p>`,
                            "className": "welcome-message"
                        },
                        {
                            "type": "flex",
                            "justify": "flex-end",
                            "items": [
                                {
                                    "type": "button",
                                    "label": "个人资料",
                                    "icon": "fa fa-user",
                                    "level": "default",
                                    "actionType": "link",
                                    "className": "m-r-xs",
                                    "link": `/${window.tenantId}/profile`
                                },
                                {
                                    "type": "button",
                                    "label": "管理平台",
                                    "icon": "fa fa-arrow-left",
                                    "level": "info",
                                    "actionType": "link",
                                    "className": "m-r-xs",
                                    "link": `/${window.tenantId}/admin`
                                },
                                {
                                    "type": "button",
                                    "label": "退出登录",
                                    "icon": "fa fa-sign-out",
                                    "level": "danger",
                                    "actionType": "ajax",
                                    "confirmText": "确认要退出登录？",
                                    "api": "/identity/api/identity/auth/logout",
                                    "reload": "none",
                                    "redirect": `/${window.tenantId}/exam/login`
                                }
                            ]
                        }
                    ],
                    "data": { now: new Date() }
                },
                { "type": "divider" },
                
                // 用户信息区域
                {
                    "type": "service",
                    "api": "/identity/api/identity/profile",
                    "className": "user-profile-section",
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<h3><i class=\"fa fa-user-circle section-icon\"></i> 用户信息</h3>",
                            "className": "section-title"
                        },
                        {
                            "type": "card",
                            "header": {
                                "title": "个人资料",
                                "subTitle": "用户基本信息",
                                "avatarText": "用户"
                            },
                            "body": [
                                {
                                    "type": "flex",
                                    "justify": "flex-start",
                                    "alignItems": "stretch",
                                    "items": [
                                        {
                                            "type": "tpl",
                                            "tpl": "<div class=\"info-item\"><i class=\"fa fa-user info-icon\"></i><span class=\"info-label\">姓名</span><span class=\"info-value\">\${displayName || userName || '未知用户'}</span></div>",
                                            "className": "flex-info-item",
                                            "columnClassName": "w-sm-12 w-md-6"
                                        },
                                        {
                                            "type": "tpl",
                                            "tpl": "<div class=\"info-item\"><i class=\"fa fa-envelope info-icon\"></i><span class=\"info-label\">邮箱</span><span class=\"info-value\">\${email || '<span class=\"text-muted\">未设置邮箱</span>'}</span></div>",
                                            "className": "flex-info-item",
                                            "columnClassName": "w-sm-12 w-md-6"
                                        }
                                    ]
                                }
                            ],
                            "className": "profile-card"
                        }
                    ]
                },
                { "type": "divider" },
                
                // 统计数据
                {
                    "type": "service",
                    "api": "/exam/api/exam/client/stats",
                    "className": "exam-stats-section",
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<h3><i class=\"fa fa-bar-chart section-icon\"></i> 考试统计</h3>",
                            "className": "section-title"
                        },
                        {
                            "type": "grid",
                            "className": "exam-stats",
                            "columns": [
                                {
                                    "md": 3,
                                    "body": {
                                        "type": "card",
                                        "className": "stat-card text-center",
                                        "body": [
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-number'>\${availableCount || 0}</div>"
                                            },
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-label'>可参加考试</div>"
                                            }
                                        ]
                                    }
                                },
                                {
                                    "md": 3,
                                    "body": {
                                        "type": "card",
                                        "className": "stat-card text-center",
                                        "body": [
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-number'>\${completedCount || 0}</div>"
                                            },
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-label'>已完成考试</div>"
                                            }
                                        ]
                                    }
                                },
                                {
                                    "md": 3,
                                    "body": {
                                        "type": "card",
                                        "className": "stat-card text-center",
                                        "body": [
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-number'>\${passRate || 0}%</div>"
                                            },
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-label'>通过率</div>"
                                            }
                                        ]
                                    }
                                },
                                {
                                    "md": 3,
                                    "body": {
                                        "type": "card",
                                        "className": "stat-card text-center",
                                        "body": [
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-number'>\${averageScore || 0}</div>"
                                            },
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class='stat-label'>平均分</div>"
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                },
                { "type": "divider" },
                
                // 可参加的考试列表
                {
                    "type": "service",
                    "api": "/exam/api/exam/client/available",
                    "className": "exam-list-section",
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<h3><i class=\"fa fa-list-alt section-icon\"></i> 可参加的考试</h3>",
                            "className": "section-title"
                        },
                        {
                            "type": "grid",
                            "columns": [
                                {
                                    "type": "each",
                                    "name": "items",
                                    "items": {
                                        "type": "card",
                                        "header": {
                                            "title": "\${examTitle}",
                                            "subTitle": "\${duration}分钟",
                                            "avatarText": "考试"
                                        }, 
                                        "body": [
                                            {
                                                "type": "flex",
                                                "justify": "flex-start",
                                                "alignItems": "stretch",
                                                "items": [
                                                    {
                                                        "type": "tpl",
                                                        "tpl": "<div class=\"exam-info-item\"><i class=\"fa fa-calendar-check-o exam-icon\"></i><span class=\"exam-info-label\">开始时间</span><span class=\"exam-info-value\">\${startTime}</span></div>",
                                                        "className": "flex-info-item",
                                                        "columnClassName": "w-sm-12 w-md-6"
                                                    },
                                                    {
                                                        "type": "tpl",
                                                        "tpl": "<div class=\"exam-info-item\"><i class=\"fa fa-calendar-times-o exam-icon\"></i><span class=\"exam-info-label\">结束时间</span><span class=\"exam-info-value\">\${endTime}</span></div>",
                                                        "className": "flex-info-item",
                                                        "columnClassName": "w-sm-12 w-md-6"
                                                    }
                                                ]
                                            },
                                            {
                                                "type": "flex",
                                                "justify": "flex-start",
                                                "alignItems": "stretch",
                                                "items": [
                                                    {
                                                        "type": "tpl",
                                                        "tpl": "<div class=\"exam-info-item\"><i class=\"fa fa-graduation-cap exam-icon\"></i><span class=\"exam-info-label\">最大尝试</span><span class=\"exam-info-value\">\${maxAttempts}次</span></div>",
                                                        "className": "flex-info-item",
                                                        "columnClassName": "w-sm-12 w-md-6"
                                                    },
                                                    {
                                                        "type": "tpl",
                                                        "tpl": "<div class=\"exam-info-item\"><i class=\"fa fa-info-circle exam-icon\"></i><span class=\"exam-info-label\">状态</span><span class=\"exam-info-value\"><span class=\"label label-info\">可参加</span></span></div>",
                                                        "className": "flex-info-item",
                                                        "columnClassName": "w-sm-12 w-md-6"
                                                    }
                                                ]
                                            },
                                            {
                                                "type": "tpl",
                                                "tpl": "<div class=\"exam-description\"><i class=\"fa fa-file-text-o exam-icon\"></i><span class=\"exam-info-label\">说明</span><span class=\"exam-info-value\">\${examDescription || '暂无说明'}</span></div>",
                                                "className": "mt-2"
                                            }
                                        ],
                                        "actions": [
                                            {
                                                "type": "button",
                                                "label": "开始考试",
                                                "level": "primary",
                                                "actionType": "custom",
                                                "className": "m-r-xs",
                                                "script": "window.startExam(event.data.id)",
                                                "visibleOn": "this.canStart === true"
                                            },
                                            {
                                                "type": "button", 
                                                "label": "查看详情",
                                                "level": "default",
                                                "actionType": "custom",
                                                "script": "window.viewExamDetail(event.data.id)"
                                            }
                                        ],
                                        "className": "exam-card"
                                    },
                                    "placeholder": {
                                        "type": "tpl",
                                        "tpl": "<div class=\"text-center text-muted p-4\"><i class=\"fa fa-inbox fa-3x mb-3\"></i><br/>当前没有可参加的考试</div>",
                                        "className": "exam-empty-placeholder"
                                    }
                                }
                            ]
                        }
                    ]
                },
                { "type": "divider" },
                
                // 考试历史记录
                {
                    "type": "service", 
                    "api": "/exam/api/exam/client/history",
                    "className": "exam-history-section",
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<h3><i class=\"fa fa-history section-icon\"></i> 历史考试记录</h3>",
                            "className": "section-title"
                        },
                        {
                            "type": "table",
                            "source": "\${items}",
                            "columns": [
                                { "name": "examTitle", "label": "考试名称" },
                                { "name": "submitTime", "label": "提交时间", "type": "datetime", "format": "YYYY-MM-DD HH:mm" },
                                { "name": "score", "label": "得分", "tpl": "\${score || '未评分'}" },
                                {
                                    "name": "status",
                                    "label": "状态",
                                    "type": "mapping",
                                    "map": {
                                        "InProgress": "<span class='label label-info'>进行中</span>",
                                        "Completed": "<span class='label label-success'>已完成</span>",
                                        "Submitted": "<span class='label label-primary'>已提交</span>",
                                        "Timeout": "<span class='label label-warning'>超时</span>",
                                        "Terminated": "<span class='label label-danger'>已终止</span>"
                                    }
                                },
                                {
                                    "name": "isPassed",
                                    "label": "是否通过",
                                    "type": "mapping",
                                    "map": {
                                        "true": "<span class='label label-success'>通过</span>",
                                        "false": "<span class='label label-danger'>未通过</span>"
                                    }
                                },
                                {
                                    "type": "operation",
                                    "label": "操作",
                                    "buttons": [
                                        {
                                            "type": "button",
                                            "label": "查看结果",
                                            "level": "link",
                                            "actionType": "custom",
                                            "script": "window.viewExamResult(event.data.id)",
                                            "visibleOn": "this.canViewResult === true"
                                        }
                                    ]
                                }
                            ],
                            "placeholder": "暂无考试记录"
                        }
                    ]
                }
            ]
        };
    }

    /**
     * 触摸设备适配初始化
     */
    window.onAmisInitialized = function() {
        window.amisInitialized = true;
        
        // 检测触摸设备并适配
        if ('ontouchstart' in document.documentElement) {
            document.body.classList.add('touch-device');
            
            // 为触摸设备添加活跃状态样式
            document.querySelectorAll('.info-item, .exam-info-item').forEach(item => {
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

    /**
     * 初始化页面
     */
    async function initPage() {
        try {
            // 显示加载状态
            showLoading(true);
            
            // 构建页面配置
            const amisJSON = buildPageConfig(null, null);
            
            // 隐藏加载状态
            showLoading(false);
            
            // 初始化amis
            let amisInstance = amis.embed(
                '#root',
                amisJSON,
                {
                    locale: 'zh-CN',
                    data: {
                        tenantId: window.tenantId
                    }
                },
                {
                    requestAdaptor: (api) => {
                        const token = TokenManager.getToken();
                        return {
                            ...api,
                            headers: {
                                ...api.headers,
                                'Authorization': token ? 'Bearer ' + token : '',
                                'TenantId': window.tenantId,
                                'X-Forwarded-With': 'CodeSpirit'
                            }
                        };
                    },
                    responseAdaptor: function (api, payload, query, request, response) {
                        // 处理HTTP错误响应
                        if (response.status === 401) {
                            // 认证失败，跳转到登录页
                            window.location.href = `/${window.tenantId}/exam/login`;
                            return { 
                                status: -1,
                                msg: '登录过期，请重新登录' 
                            };
                        } else if (response.status === 403) {
                            return { 
                                status: -1, 
                                msg: '您没有权限访问此资源' 
                            };
                        } else if (response.status >= 500) {
                            return {
                                status: -1,
                                msg: '服务器内部错误，请稍后重试'
                            };
                        } else if (response.status >= 400 && response.status < 500) {
                            return {
                                status: -1,
                                msg: payload.msg || '请求失败'
                            };
                        }
                        
                        // 处理用户信息
                        if (api.url.includes('/identity/api/identity/profile')) {
                            if (payload.status === 0 && payload.data) {
                                window.GlobalData.set('user.id', payload.data.id || null);
                                window.GlobalData.set('user.name', payload.data.name || payload.data.userName || '');
                                window.GlobalData.set('user.avatar', payload.data.avatar || '');  
                                window.GlobalData.set('user.roles', payload.data.roles || []);
                                window.GlobalData.set('profile.name', payload.data.displayName || payload.data.userName || '');
                                window.GlobalData.set('profile.email', payload.data.email || '');
                                window.GlobalData.set('profile.avatar', payload.data.avatar || '');
                                window.GlobalData.set('profile.displayName', payload.data.displayName || '');
                                window.GlobalData.syncToAmis(amisInstance);
                            }
                        }
                        
                        // 处理考试统计
                        if (api.url.includes('/exam/api/exam/client/stats')) {
                            if (payload.status === 0 && payload.data) {
                                window.GlobalData.set('examStats', payload.data);
                                window.GlobalData.syncToAmis(amisInstance);
                            }
                        }
                        
                        // 处理成功响应
                        if (payload && typeof payload === 'object') {
                            // 统一响应格式
                            if (payload.status === undefined && payload.data !== undefined) {
                                return {
                                    status: 0,
                                    data: payload.data,
                                    msg: payload.msg || '成功'
                                };
                            }
                        }
                        
                        return payload;
                    },
                    theme: 'antd'
                }
            );
            
            // 全局暴露实例
            window.amisInstance = amisInstance;
            
            // 触发应用初始化
            setTimeout(() => {
                if (typeof window.onAmisInitialized === 'function') {
                    window.onAmisInitialized();
                }
            }, 100);
            
        } catch (error) {
            console.error('初始化页面失败:', error);
            showError('页面加载失败，请刷新重试');
        }
    }

    /**
     * 显示/隐藏加载状态
     * @param {boolean} show 是否显示
     */
    function showLoading(show) {
        const loadingEl = document.getElementById('loading');
        if (loadingEl) {
            loadingEl.style.display = show ? 'block' : 'none';
        }
    }

    /**
     * 显示错误信息
     * @param {string} message 错误消息
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="exam-empty-state">
                <i class="fa fa-exclamation-triangle"></i>
                <h3>加载失败</h3>
                <p>${message}</p>
                <button onclick="location.reload()" class="exam-btn-primary">
                    <i class="fa fa-refresh"></i> 重新加载
                </button>
            </div>
        `;
        
        document.getElementById('root').innerHTML = errorHTML;
    }

    // ===== 全局函数 (供AMIS调用) =====

    /**
     * 开始考试
     * @param {string} examId 考试ID
     */
    window.startExam = function(examId) {
        if (!examId) {
            alert('考试ID无效');
            return;
        }
        
        // 确认开始考试
        if (confirm('确定要开始这场考试吗？考试开始后将无法暂停。')) {
            // 跳转到考试页面
            window.location.href = `/${window.tenantId}/exam/${examId}`;
        }
    };

    /**
     * 查看考试详情
     * @param {string} examId 考试ID
     */
    window.viewExamDetail = function(examId) {
        if (!examId) {
            alert('考试ID无效');
            return;
        }
        
        // 使用统一的API请求函数
        apiRequest(`/exam/api/exam/client/${examId}/basic`)
            .then(examInfo => {
                alert(`考试名称：${examInfo.examTitle}\n考试说明：${examInfo.examDescription}\n考试时长：${examInfo.duration} 分钟\n题目数量：${examInfo.totalQuestions} 题`);
            })
            .catch(error => {
                console.error('获取考试详情出错:', error);
                alert('获取考试详情失败：' + error.message);
            });
    };

    /**
     * 查看考试结果
     * @param {string} recordId 考试记录ID
     */
    window.viewExamResult = function(recordId) {
        if (!recordId) {
            alert('考试记录ID无效');
            return;
        }
        
        // 跳转到结果页面
        window.location.href = `/${window.tenantId}/exam/result/${recordId}`;
    };

    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPage);
    } else {
        initPage();
    }

})(); 