(function () {
    'use strict';
    
    // 初始化AMIS和路由
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();
    
    // 全局变量声明
    window.refreshTimer = null;
    
    // 将所有函数挂载到window对象上，确保可访问
    window.startAutoRefresh = function() {
        window.stopAutoRefresh(); // 先停止现有的刷新计时器
        window.refreshTimer = setInterval(() => {
            window.refreshStudentData();
        }, window.globalData.refreshInterval);
        console.log(`已启动自动刷新，间隔${window.globalData.refreshInterval / 1000}秒`);
    };
    
    window.stopAutoRefresh = function() {
        if (window.refreshTimer) {
            clearInterval(window.refreshTimer);
            window.refreshTimer = null;
            console.log('已停止自动刷新');
        }
    };
    
    window.refreshStudentData = function() {
        const studentService = document.querySelector('.student-monitor-service');
        if (studentService) {
            studentService.dispatchEvent(new CustomEvent('reload'));
            console.log('已触发考生数据刷新');
        }
    };
    
    // 更新考生数据
    window.updateStudentData = function(data) {
        if (!data) return;
        
        // 更新全局数据
        window.globalData.student = data;
        
        // 解析作弊记录
        try {
            if (data.cheatingSuspicionRecord && data.cheatingSuspicionRecord !== '') {
                const recordsArray = JSON.parse(data.cheatingSuspicionRecord);
                
                // 更新DOM
                const recordsContainer = document.querySelector('.cheating-records-container');
                if (recordsContainer) {
                    // 只有当数据有变化时才更新
                    if (recordsContainer.dataset.recordCount !== String(recordsArray.length)) {
                        recordsContainer.dataset.recordCount = recordsArray.length;
                        
                        // 创建HTML
                        let recordsHtml = '';
                        recordsArray.forEach(record => {
                            recordsHtml += `
                                <div class="cheating-record-item">
                                    <div class="record-time">${record.time}</div>
                                    <div class="record-type">${window.getCheatTypeText(record.type)}</div>
                                    <div class="record-desc">${record.description || ''}</div>
                                </div>
                            `;
                        });
                        
                        // 更新DOM
                        recordsContainer.innerHTML = recordsHtml;
                    }
                }
            }
        } catch (e) {
            console.error('解析作弊记录失败:', e);
        }
    };
    
    // 获取作弊类型文本
    window.getCheatTypeText = function(type) {
        switch(type) {
            case 'screen_switch': return '切屏行为';
            case 'forced_submit': return '强制交卷';
            case 'manual_flag': return '人工标记';
            default: return type;
        }
    };
    
    // 获取考试记录ID
    let recordId = null;
    
    // 尝试从URL路径中获取recordId
    const urlMatch = match('/monitor/student/:recordId')(window.location.pathname);
    if (urlMatch && urlMatch.params.recordId) {
        recordId = urlMatch.params.recordId;
    }
    
    // 如果路径中没有，则尝试从URL参数中获取
    if (!recordId) {
        const urlParams = new URLSearchParams(window.location.search);
        recordId = urlParams.get('recordId');
    }
    
    if (!recordId) {
        console.error('未找到考试记录ID');
        alert('未找到考试记录ID，请检查URL是否正确');
        window.location.href = '/';
    }
    
    // 全局数据
    window.globalData = {
        recordId: recordId,
        refreshInterval: 5000, // 默认5秒刷新一次
        student: {}
    };
    
    // 考生监控页面配置
    const studentPage = {
        type: 'page',
        title: '考生监控',
        data: {
            recordId: recordId
        },
        toolbar: [
            {
                type: 'button',
                label: '返回监考大屏',
                icon: 'fa fa-arrow-left',
                level: 'link',
                actionType: 'ajax',
                api: {
                    url: '/exam/api/exam/Monitor/student/' + recordId,
                    method: 'get',
                    responseType: 'json',
                    adaptor: function(payload) {
                        if (payload && payload.data && payload.data.examId) {
                            window.location.href = '/monitor/dashboard/' + payload.data.examId;
                        } else {
                            window.location.href = '/';
                        }
                        return { status: 0 };
                    }
                }
            },
            {
                type: 'button',
                label: '刷新',
                icon: 'fa fa-refresh',
                level: 'default',
                actionType: 'reload',
                target: 'student-monitor-service'
            },
            {
                type: 'select',
                name: 'refreshInterval',
                label: '刷新间隔',
                value: window.globalData.refreshInterval,
                options: [
                    { label: '3秒', value: 3000 },
                    { label: '5秒', value: 5000 },
                    { label: '10秒', value: 10000 },
                    { label: '30秒', value: 30000 },
                    { label: '手动刷新', value: 0 }
                ],
                onChange: (value) => {
                    window.globalData.refreshInterval = parseInt(value);
                    if (window.globalData.refreshInterval > 0) {
                        window.startAutoRefresh();
                    } else {
                        window.stopAutoRefresh();
                    }
                }
            }
        ],
        body: [
            {
                type: 'service',
                name: 'student-monitor-service',
                className: 'student-monitor-service',
                api: '/exam/api/exam/Monitor/student/' + recordId,
                interval: window.globalData.refreshInterval,
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: 'custom',
                                script: `
                                    try {
                                        if (event.data) {
                                            // 调用更新函数
                                            window.updateStudentData(event.data);
                                            
                                            // 启动自动刷新
                                            if (window.globalData.refreshInterval > 0 && !window.refreshTimer) {
                                                window.startAutoRefresh();
                                            }
                                        }
                                    } catch (error) {
                                        console.error('处理学生数据时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                },
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
                                        tpl: '<div class="student-title"><i class="fa fa-user-circle"></i> 考生: ${name} <span class="student-number">${studentNumber}</span></div>'
                                    },
                                    body: [
                                        {
                                            type: 'grid',
                                            columns: [
                                                {
                                                    md: 8,
                                                    body: [
                                                        {
                                                            type: 'panel',
                                                            title: '考试状态',
                                                            className: 'student-info-panel',
                                                            body: [
                                                                {
                                                                    type: 'grid',
                                                                    columns: [
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-clock-o"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">状态</div>
                                                                                        <div class="info-value status-\${status}">\${statusText}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        },
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-hourglass-half"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">剩余时间</div>
                                                                                        <div class="info-value \${remainingSeconds <= 300 ? 'text-danger' : ''}">\${remainingSeconds ? (Math.floor(remainingSeconds/60) + "分" + (remainingSeconds%60) + "秒") : "--"}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        },
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-tasks"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">答题进度</div>
                                                                                        <div class="info-value">\${answeredCount}/\${totalQuestions} (\${progressPercentage}%)</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        }
                                                                    ]
                                                                },
                                                                {
                                                                    type: 'grid',
                                                                    columns: [
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-calendar"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">开始时间</div>
                                                                                        <div class="info-value">\${startTime ? (new Date(startTime)).toLocaleString() : '--'}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        },
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-desktop"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">切屏次数</div>
                                                                                        <div class="info-value \${screenSwitchCount > 3 ? 'text-danger' : ''}">\${screenSwitchCount}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        },
                                                                        {
                                                                            md: 4,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-exclamation-triangle"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">作弊嫌疑</div>
                                                                                        <div class="info-value">
                                                                                            <span class="progress-mini">
                                                                                                <span class="progress-bar \${cheatingSuspicionLevel > 50 ? 'danger' : (cheatingSuspicionLevel > 30 ? 'warning' : 'normal')}" style="width: \${cheatingSuspicionLevel}%"></span>
                                                                                            </span>
                                                                                            \${cheatingSuspicionLevel}%
                                                                                        </div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        }
                                                                    ]
                                                                },
                                                                {
                                                                    type: 'grid',
                                                                    columns: [
                                                                        {
                                                                            md: 6,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-map-marker"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">IP地址</div>
                                                                                        <div class="info-value">\${ipAddress}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        },
                                                                        {
                                                                            md: 6,
                                                                            body: {
                                                                                type: 'tpl',
                                                                                className: 'info-card',
                                                                                tpl: `<div class="info-item">
                                                                                    <div class="info-icon"><i class="fa fa-laptop"></i></div>
                                                                                    <div class="info-content">
                                                                                        <div class="info-label">设备信息</div>
                                                                                        <div class="info-value">\${deviceInfo || "未知"}</div>
                                                                                    </div>
                                                                                </div>`
                                                                            }
                                                                        }
                                                                    ]
                                                                },
                                                                {
                                                                    type: 'grid',
                                                                    columns: [
                                                                        {
                                                                            md: 12,
                                                                            body: {
                                                                                type: 'button-group',
                                                                                buttons: [
                                                                                    {
                                                                                        type: 'button',
                                                                                        label: '强制交卷',
                                                                                        level: 'danger',
                                                                                        icon: 'fa fa-stop',
                                                                                        visibleOn: "status === 'InProgress'",
                                                                                        actionType: 'ajax',
                                                                                        confirmText: '确定要强制结束该考生的考试吗？',
                                                                                        api: {
                                                                                            method: 'post',
                                                                                            url: '/api/exam/Monitor/student/' + recordId + '/terminate'
                                                                                        }
                                                                                    },
                                                                                    {
                                                                                        type: 'button',
                                                                                        label: '标记作弊',
                                                                                        level: 'warning',
                                                                                        icon: 'fa fa-flag',
                                                                                        visibleOn: "status === 'InProgress' || status === 'Submitted'",
                                                                                        actionType: 'dialog',
                                                                                        dialog: {
                                                                                            title: '标记作弊',
                                                                                            body: {
                                                                                                type: 'form',
                                                                                                api: {
                                                                                                    method: 'post',
                                                                                                    url: '/api/exam/Monitor/student/' + recordId + '/flag-cheating'
                                                                                                },
                                                                                                body: [
                                                                                                    {
                                                                                                        type: 'textarea',
                                                                                                        name: 'reason',
                                                                                                        label: '作弊原因',
                                                                                                        placeholder: '请输入作弊原因'
                                                                                                    }
                                                                                                ]
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                ]
                                                                            }
                                                                        }
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                },
                                                {
                                                    md: 4,
                                                    body: [
                                                        {
                                                            type: 'panel',
                                                            title: '作弊记录',
                                                            className: 'cheating-panel',
                                                            body: [
                                                                {
                                                                    type: 'tpl',
                                                                    tpl: '<div class="cheating-records-container" data-record-count="0">\
                                                                        <div class="no-records">加载中...</div>\
                                                                    </div>'
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
                ]
            }
        ],
        css: {
            // 标题样式
            '.student-title': {
                'font-size': '20px',
                'font-weight': 'bold',
                'color': '#333',
                'margin-bottom': '15px',
                'display': 'flex',
                'align-items': 'center'
            },
            '.student-title i': {
                'margin-right': '10px',
                'color': '#3f51b5'
            },
            '.student-number': {
                'font-size': '16px',
                'color': '#666',
                'margin-left': '10px',
                'font-weight': 'normal'
            },
            
            // 信息面板
            '.student-info-panel, .cheating-panel': {
                'border-radius': '8px',
                'box-shadow': '0 2px 10px rgba(0,0,0,0.05)',
                'background': '#fff',
                'overflow': 'hidden',
                'margin-bottom': '20px'
            },
            
            // 信息卡片
            '.info-card': {
                'padding': '5px',
                'margin-bottom': '10px'
            },
            '.info-item': {
                'padding': '10px',
                'border-radius': '8px',
                'background': '#f5f7fa',
                'display': 'flex',
                'align-items': 'center',
                'transition': 'all 0.3s ease'
            },
            '.info-item:hover': {
                'background': '#e3f2fd',
                'transform': 'translateY(-2px)',
                'box-shadow': '0 2px 6px rgba(0,0,0,0.05)'
            },
            '.info-icon': {
                'width': '40px',
                'height': '40px',
                'background': '#3f51b5',
                'color': '#fff',
                'border-radius': '50%',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'margin-right': '10px',
                'font-size': '18px'
            },
            '.info-content': {
                'flex': '1'
            },
            '.info-label': {
                'color': '#666',
                'font-size': '14px',
                'margin-bottom': '5px'
            },
            '.info-value': {
                'color': '#333',
                'font-size': '16px',
                'font-weight': '500'
            },
            
            // 进度条
            '.progress-mini': {
                'height': '6px',
                'width': '60px',
                'background': '#eee',
                'border-radius': '3px',
                'display': 'inline-block',
                'margin-right': '5px',
                'vertical-align': 'middle'
            },
            '.progress-bar': {
                'height': '100%',
                'border-radius': '3px',
                'display': 'block'
            },
            '.progress-bar.danger': {
                'background': '#f44336'
            },
            '.progress-bar.warning': {
                'background': '#ff9800'
            },
            '.progress-bar.normal': {
                'background': '#03a9f4'
            },
            
            // 状态文本样式
            '.status-InProgress': {
                'color': '#4caf50',
                'font-weight': 'bold'
            },
            '.status-Submitted': {
                'color': '#ff9800',
                'font-weight': 'bold'
            },
            '.status-Graded': {
                'color': '#03a9f4',
                'font-weight': 'bold'
            },
            
            // 作弊记录
            '.cheating-records-container': {
                'max-height': '500px',
                'overflow-y': 'auto',
                'padding': '5px'
            },
            '.no-records': {
                'text-align': 'center',
                'color': '#999',
                'padding': '30px',
                'font-style': 'italic'
            },
            '.cheating-record-item': {
                'padding': '12px',
                'border-left': '3px solid #f44336',
                'background': '#ffebee',
                'margin-bottom': '10px',
                'border-radius': '4px'
            },
            '.record-time': {
                'color': '#666',
                'font-size': '12px',
                'margin-bottom': '5px'
            },
            '.record-type': {
                'font-weight': 'bold',
                'color': '#d32f2f',
                'margin-bottom': '5px'
            },
            '.record-desc': {
                'color': '#333'
            },
            
            // 工具栏
            '.cxd-Page-toolbar': {
                'background': '#f5f7fa',
                'padding': '10px 20px',
                'border-radius': '8px',
                'margin-bottom': '20px',
                'display': 'flex',
                'align-items': 'center'
            },
            
            // 公共样式
            '.text-danger': {
                'color': '#f44336',
                'font-weight': 'bold'
            }
        }
    };

    // 创建AMIS实例
    let amisInstance = amis.embed(
        '#root',
        studentPage,
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
                    window.location.href = `/login`;
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