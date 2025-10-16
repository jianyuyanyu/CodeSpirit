(function () {
    'use strict';
    
    /**
     * 监考大屏初始化函数
     * 功能：初始化 AMIS 实例和页面基础配置
     */
    function initMonitorDashboard() {
        // 获取全局变量
        const tenantId = window.tenantId;
        const examId = window.examId;
        const tenantName = window.tenantName;
        
        if (!tenantId || !examId) {
            console.error('缺少必要参数：租户ID或考试ID');
            alert('参数错误，请检查URL');
            return;
        }
        
        // API URL转换函数
        function transformApiUrl(url) {
            return window.ExamApiManager ? window.ExamApiManager.transformUrl(url) : url;
        }
        
        // 初始化为租户模式（参考 tenant-admin.js）
        if (window.TokenManager && window.TokenManager.initTenantMode) {
            window.TokenManager.initTenantMode(tenantId);
            console.log(`监考大屏已初始化租户模式：${tenantId}`);
        }
        
        // 初始化 AMIS
        let amis = amisRequire('amis/embed');
        
        return { amis, tenantId, examId, tenantName };
    }
    
    const config = initMonitorDashboard();
    if (!config) return;
    
    const { amis, tenantId, examId, tenantName } = config;
    
    // 全局数据
    window.monitorData = {
        examId: examId,
        tenantId: tenantId,
        refreshTimer: null
    };
    
    
    // 监考大屏页面配置
    const monitorDashboardSchema = {
        type: 'page',
        title: '',
        className: 'monitor-dashboard-page',
        data: {
            examId: examId,
            tenantId: tenantId
        },
        initApi: {
            method: 'get',
            url: transformApiUrl(`/exam/api/exam/ExamSettings/${examId}`)
        },

        body: [
            // 页面标题
            {
                type: 'tpl',
                tpl: '<div class="monitor-title"><i class="fa fa-desktop"></i> ${name || "考试"} - 监考大屏</div>',
                className: 'text-center mb-4'
            },
            
            // 监控数据服务
            {
                type: 'service',
                name: 'exam-monitor-service',
                testid: 'exam-monitor-service',
                api: transformApiUrl(`/exam/api/exam/Monitor/exam/${examId}`),
                interval: 10000,
                body: [
                    // 统计卡片区域
                    {
                        type: 'grid',
                        className: 'stats-cards-container',
                        columns: [
                            {
                                md: 3,
                                sm: 6,
                                xs: 12,
                                body: {
                                    type: 'card',
                                    className: 'stats-card',
                                    header: {
                                        title: ''
                                    },
                                    body: {
                                        type: 'tpl',
                                        tpl: `<div class="stats-card-content">
                                                <div class="stats-icon info">
                                                    <i class="fa fa-users"></i>
                                                </div>
                                                <div class="stats-value">\${totalParticipants || 0}</div>
                                                <div class="stats-label">参考人数</div>
                                                <div class="stats-rate empty"></div>
                                            </div>`
                                    }
                                }
                            },
                            {
                                md: 3,
                                sm: 6,
                                xs: 12,
                                body: {
                                    type: 'card',
                                    className: 'stats-card',
                                    header: {
                                        title: ''
                                    },
                                    body: {
                                        type: 'tpl',
                                        tpl: `<div class="stats-card-content">
                                                <div class="stats-icon success">
                                                    <i class="fa fa-signal"></i>
                                                </div>
                                                <div class="stats-value">\${onlineCount || 0}</div>
                                                <div class="stats-label">在线人数</div>
                                                <div class="stats-rate">\${totalParticipants > 0 ? ~~(onlineCount/totalParticipants*100) : 0}%</div>
                                            </div>`
                                    }
                                }
                            },
                            {
                                md: 3,
                                sm: 6,
                                xs: 12,
                                body: {
                                    type: 'card',
                                    className: 'stats-card',
                                    header: {
                                        title: ''
                                    },
                                    body: {
                                        type: 'tpl',
                                        tpl: `<div class="stats-card-content">
                                                <div class="stats-icon warning">
                                                    <i class="fa fa-check-circle"></i>
                                                </div>
                                                <div class="stats-value">\${submittedCount || 0}</div>
                                                <div class="stats-label">已交卷</div>
                                                <div class="stats-rate">\${totalParticipants > 0 ? ~~(submittedCount/totalParticipants*100) : 0}%</div>
                                            </div>`
                                    }
                                }
                            },
                            {
                                md: 3,
                                sm: 6,
                                xs: 12,
                                body: {
                                    type: 'card',
                                    className: 'stats-card',
                                    header: {
                                        title: ''
                                    },
                                    body: {
                                        type: 'tpl',
                                        tpl: `<div class="stats-card-content">
                                                <div class="stats-icon danger">
                                                    <i class="fa fa-exclamation-triangle"></i>
                                                </div>
                                                <div class="stats-value">\${suspiciousCount || 0}</div>
                                                <div class="stats-label">作弊嫌疑</div>
                                                <div class="stats-rate empty"></div>
                                            </div>`
                                    }
                                }
                            }
                        ]
                    },
                    
                    // 考生监控列表
                    {
                        type: 'card',
                        className: 'students-list-container',
                        header: {
                            title: '考生监控',
                            subTitle: '实时监控考生状态'
                        },
                        body: [
                            // 考生列表
                            {
                                type: 'crud',
                                syncLocation: false,
                                source: '${students}',
                                api: transformApiUrl(`/exam/api/exam/Monitor/exam/${examId}`),
                                filter: {
                                    title: '',
                                    body: [
                                        {
                                            type: 'input-text',
                                            name: 'keywords',
                                            placeholder: '搜索考生...'
                                        }
                                    ]
                                },
                                headerToolbar: [
                                    'statistics',
                                    {
                                        type: 'columns-toggler',
                                        align: 'right'
                                    },
                                    {
                                        type: 'reload',
                                        align: 'right'
                                    }
                                ],
                                footerToolbar: [
                                    'pagination'
                                ],
                                columns: [
                                    {
                                        name: 'name',
                                        label: '姓名',
                                        width: 120,
                                        type: 'tpl',
                                        tpl: '<strong>${name || "-"}</strong>'
                                    },
                                    {
                                        name: 'studentNumber',
                                        label: '学号',
                                        width: 150,
                                        sortable: true
                                    },
                                    {
                                        name: 'ipAddress',
                                        label: 'IP地址',
                                        width: 130,
                                        type: 'tpl',
                                        tpl: '<code style="font-size: 12px; padding: 2px 4px; background: #f5f5f5; border-radius: 3px;">${ipAddress || "未知"}</code>'
                                    },
                                    {
                                        name: 'statusText',
                                        label: '状态',
                                        width: 100,
                                        type: 'mapping',
                                        map: {
                                            '考试中': '<span class="label label-info" style="background: #17a2b8; color: white; padding: 2px 8px; border-radius: 12px; font-size: 12px;">考试中</span>',
                                            '已提交': '<span class="label label-success" style="background: #28a745; color: white; padding: 2px 8px; border-radius: 12px; font-size: 12px;">已提交</span>',
                                            '已评分': '<span class="label label-warning" style="background: #ffc107; color: #212529; padding: 2px 8px; border-radius: 12px; font-size: 12px;">已评分</span>',
                                            '未开始': '<span class="label label-secondary" style="background: #6c757d; color: white; padding: 2px 8px; border-radius: 12px; font-size: 12px;">未开始</span>',
                                            '*': '<span class="label label-default" style="background: #e9ecef; color: #495057; padding: 2px 8px; border-radius: 12px; font-size: 12px;">${statusText || "未知"}</span>'
                                        }
                                    },
                                    {
                                        name: 'startTime',
                                        label: '开始时间',
                                        width: 160,
                                        type: 'datetime',
                                        format: 'MM-DD HH:mm'
                                    },
                                    {
                                        name: 'progressPercentage',
                                        label: '进度',
                                        width: 120,
                                        type: 'progress',
                                        showLabel: true,
                                        stripe: true,
                                        animate: true,
                                        map: {
                                            '*': '${progressPercentage || 0}'
                                        }
                                    },
                                    {
                                        name: 'screenSwitchCount',
                                        label: '切屏次数',
                                        width: 100,
                                        type: 'tpl',
                                        tpl: '<span style="color: ${screenSwitchCount > 5 ? "#dc3545" : screenSwitchCount > 2 ? "#ffc107" : "#28a745"}; font-weight: bold;">${screenSwitchCount || 0}</span>',
                                        sortable: true
                                    },
                                    {
                                        type: 'operation',
                                        label: '操作',
                                        width: 150,
                                        buttons: [
                                            {
                                                type: 'button',
                                                label: '详情',
                                                level: 'primary',
                                                size: 'sm',
                                                actionType: 'dialog',
                                                dialog: {
                                                    title: '考生详情',
                                                    size: 'lg',
                                                    body: {
                                                        type: 'service',
                                                        api: transformApiUrl(`/exam/api/exam/Monitor/student/\${recordId}`),
                                                        body: [
                                                            {
                                                                type: 'form',
                                                                wrapWithPanel: false,
                                                                body: [
                                                                    {
                                                                        type: 'static',
                                                                        name: 'name',
                                                                        label: '姓名'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'studentNumber',
                                                                        label: '学号'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'ipAddress',
                                                                        label: 'IP地址'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'deviceInfo',
                                                                        label: '设备信息'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'startTime',
                                                                        label: '开始时间',
                                                                        format: 'YYYY-MM-DD HH:mm:ss'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'submitTime',
                                                                        label: '交卷时间',
                                                                        format: 'YYYY-MM-DD HH:mm:ss'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'cheatingSuspicionLevel',
                                                                        label: '作弊嫌疑等级'
                                                                    },
                                                                    {
                                                                        type: 'static',
                                                                        name: 'screenSwitchCount',
                                                                        label: '切屏次数'
                                                                    }
                                                                ]
                                                            }
                                                        ]
                                                    }
                                                }
                                            },
                                            {
                                                type: 'button',
                                                label: '强制交卷',
                                                level: 'danger',
                                                size: 'sm',
                                                visibleOn: 'status === 1',
                                                actionType: 'ajax',
                                                confirmText: '确定要强制该考生交卷吗？',
                                                api: {
                                                    method: 'post',
                                                    url: transformApiUrl(`/exam/api/Monitor/student/\${recordId}/terminate`)
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
    };

    /**
     * 渲染监考大屏页面
     * 功能：使用 AMIS 渲染完整的监考界面
     */
    function renderDashboard() {
        const amisInstance = amis.embed('#root', monitorDashboardSchema, {
            locale: 'zh-CN',
            theme: 'antd'
        }, {
            /**
             * 请求适配器
             * 功能：为所有API请求添加认证Token和租户信息（参考 tenant-admin.js）
             */
            requestAdaptor: function(api) {
                // 获取认证Token（使用租户模式）
                const token = window.TokenManager ? window.TokenManager.getToken() : null;
                
                return {
                    ...api,
                    headers: {
                        ...api.headers,
                        'Authorization': token ? `Bearer ${token}` : '',
                        'X-Forwarded-With': 'CodeSpirit',
                        'X-Tenant-Id': tenantId // 添加租户ID到请求头
                    }
                };
            },
            
            /**
             * 响应适配器  
             * 功能：处理API响应，包括错误处理和数据格式化（参考 tenant-admin.js）
             */
            responseAdaptor: function(api, payload, query, request, response) {
                // 处理权限错误
                if (response.status === 403) {
                    return { msg: '您没有权限访问此页面，请联系管理员！' };
                }
                
                // 处理认证错误
                if (response.status === 401) {
                    // 获取当前路径作为重定向参数
                    const currentPath = encodeURIComponent(window.location.hash || window.location.pathname);
                    window.location.href = `/${tenantId}/login?redirect=${currentPath}`;
                    return { msg: '登录过期！' };
                }
                
                return payload;
            }
        });
        
        return amisInstance;
    }

    /**
     * 页面初始化处理
     * 功能：设置页面标题和启动监控功能
     */
    function initializePage() {
        console.log(`监考大屏已加载 - 租户：${tenantId}, 考试：${examId}`);
        
        // 检查TokenManager是否可用
        if (!window.TokenManager) {
            console.warn('TokenManager未加载，尝试使用基本认证模式');
        }
        
        // 验证登录状态
        const token = window.TokenManager ? window.TokenManager.getToken() : localStorage.getItem('token');
        if (!token) {
            console.warn('未找到有效Token，可能需要重新登录');
            window.location.href = `/${tenantId}/login?redirect=${encodeURIComponent(window.location.pathname)}`;
            return;
        }
        
        // 设置页面标题
        document.title = `${tenantName} - 监考大屏`;
        
        // 渲染页面
        const amisInstance = renderDashboard();
        
        return amisInstance;
    }

    // 页面加载完成后初始化
    document.addEventListener('DOMContentLoaded', initializePage);

})(); 