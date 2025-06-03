(function () {
    // 初始化为系统模式
    TokenManager.initSystemMode();
    
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
        notifications: { count: 0, hasUnread: false, items: [] },
        countdowns: { items: {}, hasCountdown: false },
        settings: {},
        permissions: [],
        profile: { name: '', studentNumber: '', idNo: '', gender: '', admissionTicket: '' }
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

    // 整合的时间和状态函数
    window.examUtils = {
        formatCountdown: function(startTimeStr) {
            try {
                const startTime = new Date(startTimeStr).getTime();
                const now = new Date().getTime();
                const diffSeconds = Math.floor((startTime - now) / 1000);
                
                if (diffSeconds <= 0) return '考试即将开始';
                
                const hours = Math.floor(diffSeconds / 3600);
                const minutes = Math.floor((diffSeconds % 3600) / 60);
                const seconds = diffSeconds % 60;
                
                return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            } catch (e) {
                return '--:--:--';
            }
        },
        isComingSoon: function(startTimeStr) {
            try {
                const startTime = new Date(startTimeStr).getTime();
                const now = new Date().getTime();
                const diffMs = startTime - now;
                return diffMs > 0 && diffMs <= 30 * 60 * 1000; // 30分钟内
            } catch (e) {
                return false;
            }
        },
        isUrgent: function(startTimeStr) {
            try {
                const startTime = new Date(startTimeStr).getTime();
                const now = new Date().getTime();
                const diffMs = startTime - now;
                return diffMs > 0 && diffMs <= 5 * 60 * 1000; // 5分钟内
            } catch (e) {
                return false;
            }
        },
        processExamData: function(exam) {
            if (!exam) return {};
            
            try {
                const startTime = new Date(exam.startTime).getTime();
                const now = new Date().getTime();
                const diffMs = startTime - now;
                
                // 设置基本数据
                const result = {
                    ...exam,
                    isComingSoon: false,
                    isUrgent: false,
                    countdownText: '--',
                    comingSoonLabel: '',
                    countdownContainerClass: '',
                    countdownTextClass: ''
                };
                
                // 处理未开始考试的倒计时
                if (exam.status === '未开始' && startTime > now) {
                    const diffSeconds = Math.floor(diffMs / 1000);
                    const hours = Math.floor(diffSeconds / 3600);
                    const minutes = Math.floor((diffSeconds % 3600) / 60);
                    const seconds = diffSeconds % 60;
                    
                    result.countdownText = diffSeconds <= 0 ? 
                        '考试即将开始' : 
                        `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
                    
                    // 计算状态
                    result.isComingSoon = diffMs > 0 && diffMs <= 30 * 60 * 1000;
                    result.isUrgent = diffMs > 0 && diffMs <= 5 * 60 * 1000;
                    
                    // 设置样式和标签
                    result.countdownContainerClass = result.isComingSoon ? 'countdown-pre-start' : '';
                    result.countdownTextClass = result.isUrgent ? 'countdown-urgent' : '';
                    result.comingSoonLabel = result.isComingSoon ? '<span class="label label-coming-soon">即将开始</span>' : '';
                }
                
                return result;
            } catch (e) {
                return exam || {};
            }
        }
    };

    // 简化的DOM相关函数
    window.countdownUI = {
        // 更新倒计时显示
        updateDisplay: function(examId, remainingSeconds) {
            try {
                const countdownElements = document.querySelectorAll(`.countdown-container[data-exam-id="${examId}"] .countdown-text`);
                if (!countdownElements || countdownElements.length === 0) return;
                
                // 配置值
                let config = {
                    text: '',
                    containerClass: '',
                    textClass: '',
                    borderColor: '',
                    bgColor: ''
                };
                
                if (remainingSeconds <= 0) {
                    config = {
                        text: '考试即将开始',
                        containerClass: 'countdown-pre-start',
                        textClass: 'countdown-ended',
                        borderColor: '#4caf50',
                        bgColor: 'rgba(76, 175, 80, 0.05)'
                    };
                } else {
                    // 格式化时间
                    const hours = Math.floor(remainingSeconds / 3600);
                    const minutes = Math.floor((remainingSeconds % 3600) / 60);
                    const seconds = remainingSeconds % 60;
                    config.text = `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
                    
                    if (remainingSeconds <= 60) {
                        // 一分钟内
                        config.containerClass = 'countdown-pre-start urgent';
                        config.textClass = 'countdown-urgent';
                        config.borderColor = '#f44336';
                        config.bgColor = 'rgba(244, 67, 54, 0.05)';
                    } else if (remainingSeconds <= 300) {
                        // 五分钟内
                        config.containerClass = 'countdown-pre-start';
                        config.textClass = 'countdown-urgent';
                        config.borderColor = '#ff9800';
                        config.bgColor = 'rgba(255, 152, 0, 0.05)';
                    } else if (remainingSeconds <= 1800) {
                        // 30分钟内
                        config.containerClass = 'countdown-pre-start';
                        config.textClass = '';
                        config.borderColor = '#03a9f4';
                        config.bgColor = 'rgba(3, 169, 244, 0.05)';
                    } else {
                        // 超过30分钟
                        config.containerClass = '';
                        config.textClass = '';
                        config.borderColor = '#03a9f4';
                        config.bgColor = '';
                    }
                }
                
                // 更新DOM显示
                countdownElements.forEach(el => {
                    el.textContent = config.text;
                    el.className = `countdown-text ${config.textClass}`.trim();
                    
                    el.classList.add('countdown-updated');
                    setTimeout(() => el.classList.remove('countdown-updated'), 500);
                    
                    const container = el.closest('.countdown-container');
                    if (container) {
                        container.className = `countdown-container ${config.containerClass}`.trim();
                        container.style.borderLeftColor = config.borderColor;
                        container.style.backgroundColor = config.bgColor;
                    }
                });
            } catch (error) {
                console.error('更新倒计时显示时出错:', error);
            }
        },
        
        // 刷新考试列表
        refreshExamList: function() {
            try {
                const examListService = document.querySelector('.exam-list-section');
                if (examListService) {
                    examListService.dispatchEvent(new CustomEvent('reload'));
                }
            } catch (error) {
                console.error('刷新考试列表时出错:', error);
            }
        }
    };
    
    // 倒计时管理
    window.countdownManager = {
        // 启动倒计时计时器
        startTimer: function() {
            // 清除旧计时器
            if (window.countdownInterval) {
                clearInterval(window.countdownInterval);
                window.countdownInterval = null;
            }
            
            // 立即尝试更新
            setTimeout(() => this.initializeDisplays(), 0);
            
            // 创建新计时器
            window.countdownInterval = setInterval(() => {
                try {
                    const countdownItems = window.globalData.countdowns.items;
                    let hasActiveCountdown = false;
                    const now = new Date().getTime();
                    
                    // 更新每个倒计时
                    Object.keys(countdownItems).forEach(examId => {
                        const item = countdownItems[examId];
                        const remaining = Math.floor((item.targetTime - now) / 1000);
                        
                        if (remaining > 0) {
                            item.remaining = remaining;
                            hasActiveCountdown = true;
                            window.countdownUI.updateDisplay(examId, remaining);
                        } else {
                            item.remaining = 0;
                            window.countdownUI.updateDisplay(examId, 0);
                            // 倒计时结束后刷新列表
                            setTimeout(() => window.countdownUI.refreshExamList(), 3000);
                        }
                    });
                    
                    // 如果没有活跃的倒计时，清除计时器
                    if (!hasActiveCountdown) {
                        clearInterval(window.countdownInterval);
                        window.countdownInterval = null;
                        window.globalData.countdowns.hasCountdown = false;
                    }
                } catch (error) {
                    console.error('更新倒计时时出错:', error);
                }
            }, 1000);
        },
        
        // 初始化倒计时显示
        initializeDisplays: function() {
            try {
                if (!window.globalData.countdowns.hasCountdown) return;
                
                const now = new Date().getTime();
                const countdownItems = window.globalData.countdowns.items;
                let updateAttempts = 0;
                
                // 递归尝试更新
                const attemptUpdate = function() {
                    updateAttempts++;
                    let foundElements = false;
                    
                    Object.keys(countdownItems).forEach(examId => {
                        const item = countdownItems[examId];
                        const remaining = Math.floor((item.targetTime - now) / 1000);
                        
                        const countdownElements = document.querySelectorAll(`.countdown-container[data-exam-id="${examId}"] .countdown-text`);
                        
                        if (countdownElements && countdownElements.length > 0) {
                            foundElements = true;
                            if (remaining > 0) {
                                window.countdownUI.updateDisplay(examId, remaining);
                            }
                        }
                    });
                    
                    // 最多尝试5次
                    if (!foundElements && updateAttempts < 5) {
                        setTimeout(attemptUpdate, 100);
                    }
                };
                
                attemptUpdate();
            } catch (error) {
                console.error('初始化倒计时显示时出错:', error);
            }
        },
        
        // 处理倒计时数据
        processCountdownData: function(items) {
            try {
                if (!Array.isArray(items)) return;
                
                const processedItems = items.map(exam => window.examUtils.processExamData(exam));
                const countdownItems = {};
                let hasCountdown = false;
                
                processedItems.forEach(exam => {
                    // 只为未开始的考试设置倒计时
                    if (exam.status === '未开始' && exam.startTime) {
                        const startTime = new Date(exam.startTime).getTime();
                        const now = new Date().getTime();
                        
                        if (startTime > now) {
                            countdownItems[exam.id] = {
                                targetTime: startTime,
                                remaining: Math.floor((startTime - now) / 1000),
                                examName: exam.name,
                                isPreStart: (startTime - now) <= 30 * 60 * 1000
                            };
                            hasCountdown = true;
                        }
                    }
                });
                
                // 更新全局倒计时数据
                window.globalData.countdowns.items = countdownItems;
                window.globalData.countdowns.hasCountdown = hasCountdown;
                
                // 如果有倒计时，启动计时器
                if (hasCountdown && !window.countdownInterval) {
                    this.startTimer();
                }
            } catch (error) {
                console.error('处理倒计时数据时出错:', error);
            }
        }
    };

    // 已简化的初始化函数
    window.onAmisInitialized = function() {
        window.amisInitialized = true;
        
        if (window.globalData.countdowns.hasCountdown && !window.countdownInterval) {
            window.countdownManager.startTimer();
        }
        
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

    // 应用配置
    var app = {
        type: 'page',
        title: window.siteSettings ? window.siteSettings.clientAppName : '考试系统',
        body: [
            // 欢迎部分
            {
                type: 'service',
                api: '/identity/api/identity/profile',
                className: 'client-welcome-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h2><i class="fa fa-graduation-cap welcome-icon"></i> 欢迎您，${user.name}</h2><p><i class="fa fa-calendar welcome-icon"></i> 今天是 ${now|date:YYYY-MM-DD}，祝您考试顺利！</p>',
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
                data: { now: new Date() }
            },
            { type: 'divider' },
            
            // 考生个人信息部分 - 移至此处（在考试列表之前）
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
                            },
                            {
                                type: 'flex',
                                justify: 'flex-start',
                                alignItems: 'stretch',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-id-card info-icon"></i><span class="info-label">身份证号</span><span class="info-value">${idNo || "<span class=\'text-muted\'>未设置</span>"}</span></div>',
                                        className: 'flex-info-item',
                                        columnClassName: 'w-sm-12 w-md-4'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-ticket info-icon"></i><span class="info-label">准考证号</span><span class="info-value">${admissionTicket || "<span class=\'text-muted\'>未设置</span>"}</span></div>',
                                        className: 'flex-info-item',
                                        columnClassName: 'w-sm-12 w-md-4'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="info-item"><i class="fa fa-phone info-icon"></i><span class="info-label">手机号码</span><span class="info-value">${phoneNumber || "<span class=\'text-muted\'>未设置</span>"}</span></div>',
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
            
            // 考试列表部分
            {
                type: 'service',
                api: '/exam/api/exam/client/available',
                className: 'exam-list-section',
                interval: 5000, // 每5秒自动刷新
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-list-alt section-icon"></i> 可参加的考试</h3>',
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
                                            type: 'flex',
                                            justify: 'flex-start',
                                            alignItems: 'stretch',
                                            items: [
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="exam-info-item"><i class="fa fa-calendar-check-o exam-icon"></i><span class="exam-info-label">开始时间</span><span class="exam-info-value">${startTime}</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="exam-info-item"><i class="fa fa-calendar-times-o exam-icon"></i><span class="exam-info-label">结束时间</span><span class="exam-info-value">${endTime}</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                }
                                            ]
                                        },
                                        {
                                            type: 'flex',
                                            justify: 'flex-start',
                                            alignItems: 'stretch',
                                            items: [
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="exam-info-item"><i class="fa fa-graduation-cap exam-icon"></i><span class="exam-info-label">总分</span><span class="exam-info-value">${totalScore}分</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                },
                                                {
                                                    type: 'tpl',
                                                    tpl: '<div class="exam-info-item"><i class="fa fa-info-circle exam-icon"></i><span class="exam-info-label">状态</span><span class="exam-info-value"><span class="label label-${status === \'进行中\' ? \'success\' : (status === \'未开始\' ? \'info\' : \'danger\')}">${status}</span>${comingSoonLabel|raw}</span></div>',
                                                    className: 'flex-info-item',
                                                    columnClassName: 'w-sm-12 w-md-6'
                                                }
                                            ]
                                        },
                                        { 
                                            type: 'tpl', 
                                            tpl: '<div class="countdown-container ${countdownContainerClass}" data-exam-id="${id}"><i class="fa fa-clock-o exam-icon"></i><span class="exam-info-label">倒计时</span><span class="countdown-text ${countdownTextClass}">${countdownText}</span></div>',
                                            visibleOn: "status === '未开始'"
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
                                                messages: { success: '开始考试...' }
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
                ],
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        if (event.data && Array.isArray(event.data.items)) {
                                            window.countdownManager.processCountdownData(event.data.items);
                                        }
                                    } catch (error) {
                                        console.error('处理倒计时数据时出错:', error);
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
                                            
                                            // 初始化倒计时显示
                                            setTimeout(() => {
                                                if (typeof window.countdownManager.initializeDisplays === 'function') {
                                                    window.countdownManager.initializeDisplays();
                                                }
                                            }, 50);
                                        }, 10);
                                    } catch (error) {
                                        console.error('挂载后初始化倒计时显示时出错:', error);
                                    }
                                `
                            }
                        ]
                    }
                }
            },
            { type: 'divider' },
            
            // 历史考试记录部分
            {
                type: 'service',
                api: '/exam/api/exam/client/history',
                className: 'exam-history-section',
                body: [
                    {
                        type: 'tpl',
                        tpl: '<h3><i class="fa fa-history section-icon"></i> 历史考试记录</h3>',
                        className: 'section-title'
                    },
                    {
                        type: 'table',
                        source: '${items}',
                        columns: [
                            { name: 'name', label: '考试名称' },
                            { name: 'startTime', label: '考试时间', type: 'datetime' },
                            { name: 'status', label: '状态', type: 'status' },
                            // {
                            //     type: 'operation',
                            //     label: '操作',
                            //     buttons: [
                            //         {
                            //             label: '查看详情',
                            //             type: 'button',
                            //             actionType: 'link',
                            //             link: '/client/exam/result/${id}'
                            //         }
                            //     ]
                            // }
                        ],
                        placeholder: '没有历史考试记录'
                    }
                ]
            }
        ],
        // 简化CSS
        css: {
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
                'padding': '7px 12px' // 增大触摸区域
            },
            '.touch-active': {
                'background-color': 'rgba(63, 81, 181, 0.15) !important',
                'transform': 'scale(0.98) !important'
            },
            // 区域样式
            '.client-welcome-section': {
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
            '.cxd-Divider': {
                'margin': '10px 0'
            },
            '.exam-list-section, .exam-history-section, .student-profile-section': {
                'padding': '12px',
                'margin': '0 25px 15px',
                'background-color': '#fff',
                'border-radius': 'var(--radius)',
                'box-shadow': 'var(--shadow)'
            },
            // 考生信息美化
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
            '.groups-item': {
                'margin-top': '15px',
                'background-color': 'rgba(3, 169, 244, 0.05)',
                'border-left': '3px solid #03a9f4'
            },
            '.groups-value': {
                'display': 'flex',
                'flex-wrap': 'wrap',
                'gap': '5px'
            },
            '.group-tag': {
                'background-color': '#e3f2fd',
                'color': '#1565c0',
                'padding': '3px 8px',
                'border-radius': '12px',
                'font-size': '12px',
                'display': 'inline-block',
                'border': '1px solid #bbdefb'
            },
            '.no-group': {
                'color': '#999',
                'font-style': 'italic'
            },
            // 考试卡片样式
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
            // 按钮样式
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
            // 标签样式
            '.label': {
                'padding': '3px 8px',
                'border-radius': '12px',
                'font-size': '12px',
                'display': 'inline-block'
            },
            '.label-success': { 'background-color': '#e8f5e9', 'color': '#2e7d32' },
            '.label-info': { 'background-color': '#e3f2fd', 'color': '#1565c0' },
            '.label-danger': { 'background-color': '#ffebee', 'color': '#c62828' },
            '.label-coming-soon': {
                'background-color': '#e1f5fe',
                'color': '#0288d1',
                'margin-left': '5px',
                'animation': 'pulse 1.5s infinite',
                'border': '1px solid #b3e5fc'
            },
            // 倒计时样式
            '.countdown-container': {
                'margin-top': '10px',
                'padding': '8px 12px',
                'background-color': '#f8f9fa',
                'border-radius': '6px',
                'border-left': '3px solid #03a9f4',
                'display': 'flex',
                'align-items': 'center',
                'transition': 'all 0.3s ease',
                'height': '100%'
            },
            '.countdown-text': {
                'font-family': 'Consolas, monospace',
                'font-weight': 'bold',
                'color': '#03a9f4',
                'margin-left': '5px',
                'transition': 'all 0.3s ease',
                'letter-spacing': '0.5px'
            },
            '.countdown-urgent': {
                'color': '#f44336',
                'animation': 'pulse 1s infinite',
                'text-shadow': '0 0 5px rgba(244, 67, 54, 0.3)'
            },
            '.countdown-ended': {
                'color': '#4caf50',
                'font-weight': 'bold',
                'animation': 'none'
            },
            '.countdown-updated': {
                'animation': 'highlight 0.5s ease-out'
            },
            '.countdown-pre-start': {
                'background-color': 'rgba(3, 169, 244, 0.1)',
                'box-shadow': '0 2px 5px rgba(0,0,0,0.05)',
                'transform': 'scale(1.02)'
            },
            '.countdown-container:hover': {
                'background-color': '#e9f7fe',
                'transform': 'translateY(-2px)',
                'box-shadow': '0 3px 6px rgba(0,0,0,0.08)'
            },
            '.countdown-container::before': {
                'content': '""',
                'display': 'inline-block',
                'width': '8px',
                'height': '8px',
                'background-color': '#03a9f4',
                'border-radius': '50%',
                'margin-right': '6px',
                'animation': 'pulse 1.5s infinite',
                'opacity': '0.7'
            },
            '.countdown-container.urgent::before': {
                'background-color': '#f44336'
            },
            // 动画
            '@keyframes pulse': {
                '0%': { 'opacity': '0.7' },
                '50%': { 'opacity': '1' },
                '100%': { 'opacity': '0.7' }
            },
            '@keyframes highlight': {
                '0%': {
                    'transform': 'scale(1.2)',
                    'text-shadow': '0 0 8px rgba(3, 169, 244, 0.8)'
                },
                '100%': {
                    'transform': 'scale(1)',
                    'text-shadow': 'none'
                }
            },
            // 基本样式
            '.info-icon, .exam-icon': {
                'margin-right': '10px',
                'width': '20px',
                'text-align': 'center',
                'color': 'var(--primary)'
            },
            '.info-item, .exam-info-item': {
                'height': '100%',
                'display': 'flex',
                'align-items': 'center',
                'margin': '0',
                'padding': '10px 15px',
                'border-radius': 'var(--radius)',
                'background-color': 'rgba(63, 81, 181, 0.05)',
                'transition': 'all 0.3s ease'
            },
            '.info-item:hover, .exam-info-item:hover': {
                'background-color': 'rgba(63, 81, 181, 0.1)',
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
            '.groups-item': {
                'margin-top': '15px',
                'background-color': 'rgba(3, 169, 244, 0.05)',
                'border-left': '3px solid #03a9f4'
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
            '.flex-info-item': {
                'padding': '0 5px',
                'margin-bottom': '10px',
                'flex': '1',
                'min-width': '0'
            },
            // 响应式样式
            '@media (max-width: 768px)': {
                '.exam-list-section, .exam-history-section, .client-welcome-section, .student-profile-section': {
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
                '.exam-list-section, .exam-history-section, .client-welcome-section, .student-profile-section': {
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
                },
                '.countdown-text': {
                    'font-size': '13px'
                },
                '.countdown-container': {
                    'padding': '6px 8px'
                }
            },
            '@media (max-width: 370px)': {
                '.info-label, .exam-info-label': {
                    'min-width': '40px',
                    'font-size': '12px'
                },
                '.info-value, .exam-info-value': {
                    'font-size': '12px'
                },
                '.info-icon, .exam-icon': {
                    'margin-right': '4px',
                    'font-size': '12px'
                },
                '.countdown-text': {
                    'font-size': '12px'
                },
                '.section-title': {
                    'font-size': '15px'
                },
                '.section-icon': {
                    'font-size': '13px'
                },
                '.cxd-Button': {
                    'font-size': '12px',
                    'padding': '4px 6px'
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
        }
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
                const token = TokenManager.getToken();
                return {
                    ...api,
                    headers: {
                        ...api.headers,
                        'Authorization': token ? 'Bearer ' + token : '',
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

    // 设置通知数据初始值
    amisInstance.updateProps({
        data: {
            notifications: {
                count: 0,
                hasUnread: false
            }
        }
    });

    // 路由监听
    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})();