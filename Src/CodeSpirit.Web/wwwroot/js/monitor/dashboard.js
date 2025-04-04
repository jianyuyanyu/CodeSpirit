(function () {
    'use strict';
    
    // 初始化AMIS和路由
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();
    
    // 全局变量声明
    window.refreshTimer = null;
    
    // 获取考试ID
    let examId = null;
    
    // 尝试从URL路径中获取examId
    const urlMatch = match('/monitor/dashboard/:examId')(window.location.pathname);
    if (urlMatch && urlMatch.params.examId) {
        examId = urlMatch.params.examId;
    }
    
    // 如果路径中没有，则尝试从URL参数中获取
    if (!examId) {
        const urlParams = new URLSearchParams(window.location.search);
        examId = urlParams.get('examId');
    }
    
    if (!examId) {
        console.error('未找到考试ID');
        alert('未找到考试ID，请检查URL是否正确');
        window.location.href = '/';
    }
    
    // 全局数据
    window.globalData = {
        examId: examId,
        exam: {
            name: '',
            totalStudents: 0,
            onlineCount: 0,
            submittedCount: 0,
            totalQuestions: 0,
            suspiciousCount: 0
        },
        students: []
    };
    
    // 将所有函数定义在window上，确保AMIS脚本能够访问
    window.updateDashboardStats = function(data) {
        if (!data) return;
        
        // 更新全局数据
        window.globalData.exam.name = data.name || '';
        window.globalData.exam.totalStudents = data.totalParticipants || 0;
        window.globalData.exam.onlineCount = data.onlineCount || 0;
        window.globalData.exam.submittedCount = data.submittedCount || 0;
        window.globalData.exam.totalQuestions = data.totalQuestions || 0;
        window.globalData.exam.suspiciousCount = data.suspiciousCount || 0;
        
        // 更新学生数据
        window.globalData.students = data.students || [];
        
        // 更新DOM元素
        window.updateCounters();
    };
    
    window.updateCounters = function() {
        const data = window.globalData.exam;
        
        // 更新计数器显示
        window.updateCounter('.total-students-value', data.totalStudents);
        window.updateCounter('.online-count-value', data.onlineCount);
        window.updateCounter('.submitted-count-value', data.submittedCount);
        window.updateCounter('.suspicious-count-value', data.suspiciousCount);
        
        // 更新在线率和提交率
        const onlineRate = data.totalStudents > 0 ? 
            Math.round(data.onlineCount / data.totalStudents * 100) : 0;
        const submittedRate = data.totalStudents > 0 ? 
            Math.round(data.submittedCount / data.totalStudents * 100) : 0;
            
        window.updateCounter('.online-rate-value', `${onlineRate}%`);
        window.updateCounter('.submitted-rate-value', `${submittedRate}%`);
    };
    
    window.updateCounter = function(selector, value) {
        const elements = document.querySelectorAll(selector);
        if (elements && elements.length > 0) {
            elements.forEach(el => {
                el.textContent = value;
                
                // 添加动画效果
                el.classList.add('counter-updated');
                setTimeout(() => el.classList.remove('counter-updated'), 1000);
            });
        }
    };
    
    window.refreshDashboard = function() {
        const examListService = document.querySelector('.exam-monitor-service');
        if (examListService) {
            examListService.dispatchEvent(new CustomEvent('reload'));
            console.log('已触发监考数据刷新');
        }
    };
    
    window.startAutoRefresh = function() {
        window.stopAutoRefresh(); // 先停止现有的刷新计时器
        window.refreshTimer = setInterval(() => {
            window.refreshDashboard();
        }, 10000); // 固定10秒刷新一次
        console.log('已启动自动刷新，间隔10秒');
    };
    
    window.stopAutoRefresh = function() {
        if (window.refreshTimer) {
            clearInterval(window.refreshTimer);
            window.refreshTimer = null;
            console.log('已停止自动刷新');
        }
    };
    
    // 监考大屏页面配置
    const dashboardPage = {
        type: 'page',
        title: '监考大屏',
        data: {
            examId: examId
        },
        toolbar: [
            {
                type: 'button',
                label: '刷新',
                icon: 'fa fa-refresh',
                level: 'default',
                actionType: 'reload',
                target: 'exam-monitor-service'
            }
        ],
        body: [
            {
                type: 'service',
                name: 'exam-monitor-service',
                className: 'exam-monitor-service',
                api: `/exam/api/exam/Monitor/exam/${examId}`,
                interval: 10000,
                body: [
                    {
                        type: 'grid',
                        columns: [
                            {
                                md: 12,
                                body: {
                                    type: 'panel',
                                    title: {
                                        type: 'tpl',
                                        tpl: '<div class="dashboard-title"><i class="fa fa-desktop"></i> ${name} - 监考大屏</div>'
                                    },
                                    body: [
                                        {
                                            type: 'grid',
                                            columns: [
                                                {
                                                    md: 3,
                                                    body: {
                                                        type: 'panel',
                                                        className: 'counter-card total-students',
                                                        body: {
                                                            type: 'tpl',
                                                            tpl: `<div class="counter-item">
                                                                <div class="counter-icon"><i class="fa fa-users"></i></div>
                                                                <div class="counter-info">
                                                                    <div class="counter-value total-students-value">${'$'}{totalParticipants || 0}</div>
                                                                    <div class="counter-label">参考人数</div>
                                                                </div>
                                                            </div>`
                                                        }
                                                    }
                                                },
                                                {
                                                    md: 3,
                                                    body: {
                                                        type: 'panel',
                                                        className: 'counter-card online-count',
                                                        body: {
                                                            type: 'tpl',
                                                            tpl: `<div class="counter-item">
                                                                <div class="counter-icon"><i class="fa fa-signal"></i></div>
                                                                <div class="counter-info">
                                                                    <div class="counter-value online-count-value">${'$'}{onlineCount || 0}</div>
                                                                    <div class="counter-label">在线人数 <span class="counter-rate online-rate-value">${'$'}{totalParticipants > 0 ? ~~(onlineCount/totalParticipants*100) : 0}%</span></div>
                                                                </div>
                                                            </div>`
                                                        }
                                                    }
                                                },
                                                {
                                                    md: 3,
                                                    body: {
                                                        type: 'panel',
                                                        className: 'counter-card submitted-count',
                                                        body: {
                                                            type: 'tpl',
                                                            tpl: `<div class="counter-item">
                                                                <div class="counter-icon"><i class="fa fa-check-circle"></i></div>
                                                                <div class="counter-info">
                                                                    <div class="counter-value submitted-count-value">${'$'}{submittedCount || 0}</div>
                                                                    <div class="counter-label">已交卷 <span class="counter-rate submitted-rate-value">${'$'}{totalParticipants > 0 ? ~~(submittedCount/totalParticipants*100) : 0}%</span></div>
                                                                </div>
                                                            </div>`
                                                        }
                                                    }
                                                },
                                                {
                                                    md: 3,
                                                    body: {
                                                        type: 'panel',
                                                        className: 'counter-card suspicious-count',
                                                        body: {
                                                            type: 'tpl',
                                                            tpl: `<div class="counter-item">
                                                                <div class="counter-icon"><i class="fa fa-exclamation-triangle"></i></div>
                                                                <div class="counter-info">
                                                                    <div class="counter-value suspicious-count-value">${'$'}{suspiciousCount || 0}</div>
                                                                    <div class="counter-label">作弊嫌疑</div>
                                                                </div>
                                                            </div>`
                                                        }
                                                    }
                                                }
                                            ]
                                        },
                                        {
                                            type: 'panel',
                                            title: '考生监控',
                                            body: [
                                                {
                                                    type: 'input-text',
                                                    name: 'keywords',
                                                    label: '搜索',
                                                    placeholder: '输入姓名、学号、IP搜索',
                                                    addOn: {
                                                        type: 'button',
                                                        icon: 'fa fa-search',
                                                        actionType: 'clear'
                                                    }
                                                },
                                                {
                                                    type: 'crud',
                                                    syncLocation: false,
                                                    api: `/exam/api/exam/Monitor/exam/${examId}`,
                                                    source: '${students}',
                                                    defaultParams: {
                                                        examId: examId
                                                    },
                                                    filter: {
                                                        body: [
                                                            {
                                                                type: 'select',
                                                                name: 'statusFilter',
                                                                label: '状态',
                                                                clearable: true,
                                                                options: [
                                                                    { label: '全部', value: '' },
                                                                    { label: '考试中', value: 'InProgress' },
                                                                    { label: '已提交', value: 'Submitted' },
                                                                    { label: '已评分', value: 'Graded' }
                                                                ]
                                                            }
                                                        ]
                                                    },
                                                    columns: [
                                                        {
                                                            name: 'recordId',
                                                            label: '记录ID',
                                                            width: 80,
                                                            toggled: false
                                                        },
                                                        {
                                                            name: 'name',
                                                            label: '姓名',
                                                            width: 80,
                                                            sortable: true
                                                        },
                                                        {
                                                            name: 'studentNumber',
                                                            label: '学号',
                                                            width: 120,
                                                            sortable: true
                                                        },
                                                        {
                                                            name: 'idCardNumber',
                                                            label: '身份证号',
                                                            width: 160,
                                                            sortable: true
                                                        },
                                                        {
                                                            name: 'gender',
                                                            label: '性别',
                                                            width: 60,
                                                            sortable: true
                                                        },
                                                        {
                                                            name: 'ipAddress',
                                                            label: 'IP地址',
                                                            width: 130
                                                        },
                                                        {
                                                            name: 'statusText',
                                                            label: '状态',
                                                            width: 80,
                                                            type: 'status',
                                                            searchable: true
                                                        },
                                                        {
                                                            name: 'progressDisplay',
                                                            label: '进度',
                                                            width: 150,
                                                            type: 'tpl',
                                                            tpl: '<div class="progress"><div class="progress-bar" style="width: ${progressPercentage}%"></div><span class="progress-text">${progressDisplay}</span></div>'
                                                        },
                                                        {
                                                            name: 'screenSwitchCount',
                                                            label: '切屏次数',
                                                            width: 80,
                                                            sortable: true,
                                                            type: 'tpl',
                                                            tpl: '<span class="${screenSwitchCount > 3 ? "text-danger" : ""}">${screenSwitchCount}</span>'
                                                        },
                                                        {
                                                            name: 'cheatingSuspicionLevel',
                                                            label: '作弊嫌疑',
                                                            width: 100,
                                                            sortable: true,
                                                            type: 'progress',
                                                            progressBarExpr: '${cheatingSuspicionLevel}',
                                                            valueTpl: '${cheatingSuspicionLevel}'
                                                        },
                                                        {
                                                            name: 'remainingTimeDisplay',
                                                            label: '剩余时间',
                                                            width: 100,
                                                            type: 'tpl',
                                                            tpl: '<span class="${remainingSeconds <= 300 ? "text-danger" : ""}">${remainingTimeDisplay}</span>'
                                                        },
                                                        {
                                                            type: 'operation',
                                                            label: '操作',
                                                            width: 150,
                                                            buttons: [
                                                                {
                                                                    type: 'button',
                                                                    icon: 'fa fa-eye',
                                                                    tooltip: '查看详情',
                                                                    actionType: 'link',
                                                                    link: '/monitor/student/${recordId}'
                                                                },
                                                                {
                                                                    type: 'button',
                                                                    icon: 'fa fa-flag',
                                                                    tooltip: '标记作弊',
                                                                    level: 'danger',
                                                                    visibleOn: '${status === "InProgress" || status === "Submitted"}',
                                                                    actionType: 'ajax',
                                                                    confirmText: '确定标记该考生作弊吗？',
                                                                    api: {
                                                                        method: 'post',
                                                                        url: '/api/exam/Monitor/student/${recordId}/flag-cheating',
                                                                        data: {
                                                                            reason: '监考人员标记'
                                                                        }
                                                                    }
                                                                },
                                                                {
                                                                    type: 'button',
                                                                    icon: 'fa fa-stop',
                                                                    tooltip: '强制交卷',
                                                                    level: 'warning',
                                                                    visibleOn: '${status === "InProgress"}',
                                                                    actionType: 'ajax',
                                                                    confirmText: '确定要强制结束该考生的考试吗？',
                                                                    api: {
                                                                        method: 'post',
                                                                        url: '/api/exam/Monitor/student/${recordId}/terminate'
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
                            }
                        ]
                    }
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: 'custom',
                                script: `
                                    try {
                                        if (event.data) {
                                            // 调用更新函数
                                            window.updateDashboardStats(event.data);
                                            
                                            // 启动自动刷新
                                            if (!window.refreshTimer) {
                                                window.startAutoRefresh();
                                            }
                                        }
                                    } catch (error) {
                                        console.error('处理监考数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            }
        ],
        css: {
            // 仪表盘标题
            '.dashboard-title': {
                'font-size': '20px',
                'font-weight': 'bold',
                'color': '#333',
                'margin-bottom': '15px',
                'display': 'flex',
                'align-items': 'center'
            },
            '.dashboard-title i': {
                'margin-right': '10px',
                'color': '#3f51b5'
            },
            
            // 计数器卡片
            '.counter-card': {
                'border-radius': '8px',
                'box-shadow': '0 2px 10px rgba(0,0,0,0.05)',
                'transition': 'all 0.3s ease',
                'border': 'none',
                'background': '#fff',
                'overflow': 'hidden',
                'margin-bottom': '20px'
            },
            '.counter-card:hover': {
                'transform': 'translateY(-5px)',
                'box-shadow': '0 5px 15px rgba(0,0,0,0.1)'
            },
            '.counter-item': {
                'padding': '15px',
                'display': 'flex',
                'align-items': 'center'
            },
            '.counter-icon': {
                'font-size': '32px',
                'margin-right': '15px',
                'color': '#3f51b5',
                'width': '40px',
                'height': '40px',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center'
            },
            '.counter-info': {
                'flex': '1'
            },
            '.counter-value': {
                'font-size': '28px',
                'font-weight': 'bold',
                'margin-bottom': '5px',
                'transition': 'all 0.3s ease',
                'color': '#333'
            },
            '.counter-label': {
                'color': '#666',
                'font-size': '14px'
            },
            '.counter-rate': {
                'background-color': 'rgba(63,81,181,0.1)',
                'padding': '2px 6px',
                'border-radius': '10px',
                'font-size': '12px',
                'color': '#3f51b5',
                'margin-left': '5px'
            },
            '.counter-updated': {
                'animation': 'pulse 0.5s'
            },
            
            // 特定计数器样式
            '.total-students .counter-icon': {
                'color': '#3f51b5'
            },
            '.online-count .counter-icon': {
                'color': '#4caf50'
            },
            '.submitted-count .counter-icon': {
                'color': '#ff9800'
            },
            '.suspicious-count .counter-icon': {
                'color': '#f44336'
            },
            
            // 表格样式
            '.cxd-Table-content': {
                'font-size': '14px'
            },
            '.text-danger': {
                'color': '#f44336',
                'font-weight': 'bold'
            },
            
            // 动画
            '@keyframes pulse': {
                '0%': {
                    'transform': 'scale(1)'
                },
                '50%': {
                    'transform': 'scale(1.05)',
                    'color': '#3f51b5'
                },
                '100%': {
                    'transform': 'scale(1)'
                }
            },
            
            // 进度条样式
            '.progress': {
                'position': 'relative',
                'width': '100%',
                'height': '18px',
                'background-color': '#f5f5f5',
                'border-radius': '4px',
                'overflow': 'hidden'
            },
            '.progress-bar': {
                'position': 'absolute',
                'height': '100%',
                'background-color': '#4caf50',
                'transition': 'width 0.3s ease'
            },
            '.progress-text': {
                'position': 'absolute',
                'width': '100%',
                'text-align': 'center',
                'font-size': '12px',
                'line-height': '18px',
                'color': '#333',
                'z-index': '1'
            }
        }
    };

    // 创建AMIS实例
    let amisInstance = amis.embed(
        '#root',
        dashboardPage,
        {
            location: history.location,
            data: { date: new Date() },
            locale: 'zh-CN'
        },
        {
            theme: 'antd',
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
                    //window.location.href = `/login`;
                    return { msg: '登录过期！' };
                }
                return payload;
            }
        }
    );

    // 路由监听
    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
    
    // 初始执行
    window.startAutoRefresh();
})(); 