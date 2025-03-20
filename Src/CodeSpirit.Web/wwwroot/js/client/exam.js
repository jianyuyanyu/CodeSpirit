(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    // 获取考试ID
    const examId = window.location.pathname.split('/').pop();
    window.enableAMISDebug = true;
    // 全局数据对象，用于存储用户信息和考试数据
    window.globalData = {
        user: {
            id: null,
            name: '',
            avatar: '',
            roles: []
        },
        exam: {
            id: examId,
            name: '',
            duration: 0,
            startTime: null,
            endTime: null,
            totalScore: 0
        },
        timer: {
            displayText: '加载中...',
            hours: 0,
            minutes: 0,
            seconds: 0,
            remainingSeconds: 0
        }
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

    // 创建计时器状态
    let examTimerInterval = null;
    let remainingTime = 0;
    let examEndTime = null;

    // 答案状态
    let examAnswers = [];
    let recordId = null;

    // 在文件开始位置添加
    window.amisInstance = null;

    // 开始计时器
    function startExamTimer(duration, startTime) {
        console.log("开始启动计时器函数", { duration, startTime });
        
        // 检查参数是否有效
        if (!duration || !startTime) {
            console.error("计时器参数无效", { duration, startTime });
            return;
        }
        
        try {
            // 计算结束时间 (优先使用系统设置的截止时间，超过则使用开始时间+考试时长)
            let examStartTime = new Date(startTime);
            console.log("考试开始时间", examStartTime);
            
            // 验证开始时间是否有效
            if (isNaN(examStartTime.getTime())) {
                console.error("无效的开始时间格式", startTime);
                // 使用当前时间作为备用
                examStartTime = new Date();
                console.log("使用当前时间作为备用", examStartTime);
            }
            
            const examEndTimeByDuration = new Date(examStartTime.getTime() + duration * 60 * 1000);
            console.log("计算出的结束时间", examEndTimeByDuration);
            
            // 设置剩余时间（分钟转为秒）
            remainingTime = duration * 60;
            console.log("设置剩余时间(秒)", remainingTime);
            
            // 清除之前的计时器
            if (examTimerInterval) {
                clearInterval(examTimerInterval);
                console.log("清除之前的计时器");
            }
            
            // 更新计时器显示
            updateTimerDisplay();
            
            // 启动计时器
            console.log("正在启动计时器间隔");
            examTimerInterval = setInterval(() => {
                remainingTime--;
                
                if (remainingTime <= 0) {
                    console.log("考试时间结束，准备自动提交");
                    clearInterval(examTimerInterval);
                    submitExam(true); // 自动提交
                    return;
                }
                
                updateTimerDisplay();
            }, 1000);
            console.log("计时器已成功启动", examTimerInterval);
        } catch (error) {
            console.error("计时器启动过程中出错", error);
        }
    }
    
    // 更新计时器显示
    function updateTimerDisplay() {
        const hours = Math.floor(remainingTime / 3600);
        const minutes = Math.floor((remainingTime % 3600) / 60);
        const seconds = remainingTime % 60;
        
        const displayText = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
        
        // 更新全局数据
        window.GlobalData.set('timer.displayText', displayText);
        window.GlobalData.set('timer.hours', hours);
        window.GlobalData.set('timer.minutes', minutes);
        window.GlobalData.set('timer.seconds', seconds);
        window.GlobalData.set('timer.remainingSeconds', remainingTime);

        // 同步到amis上下文
        if (window.amisInstance) {
            window.GlobalData.syncToAmis(window.amisInstance, ['timer']);
        }
    }
    
    // 保存答案
    function saveAnswer(questionId, answer) {
        // 查找已有答案
        const existingIndex = examAnswers.findIndex(a => a.questionId === questionId);
        
        if (existingIndex >= 0) {
            // 更新已有答案
            examAnswers[existingIndex].answer = answer;
        } else {
            // 添加新答案
            examAnswers.push({
                questionId: questionId,
                answer: answer
            });
        }
    }
    
    // 提交考试
    function submitExam(isAutoSubmit = false) {
        if (isAutoSubmit) {
            alert("考试时间已结束，系统将自动提交您的答卷！");
        }
        
        // 转换为后端需要的格式
        const answers = examAnswers.map(a => ({
            questionId: a.questionId,
            answer: a.answer
        }));
        
        // 提交考试
        fetch(`/exam/api/exam/client/${recordId}/submit`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + localStorage.getItem('token'),
                'X-Forwarded-With': 'CodeSpirit'
            },
            body: JSON.stringify(answers)
        })
        .then(response => response.json())
        .then(data => {
            if (data.status === 0) {
                // 清除计时器
                if (examTimerInterval) {
                    clearInterval(examTimerInterval);
                }
                
                // 跳转到结果页面
                window.location.href = `/client/exam/result/${recordId}`;
            } else {
                alert(data.msg || "提交失败，请重试");
            }
        })
        .catch(error => {
            console.error("提交考试失败", error);
            alert("提交失败，请检查网络连接后重试");
        });
    }

    // 取消考试
    function cancelExam() {
        if (confirm("确定要放弃本次考试吗？您的答案将不会被保存。")) {
            // 清除计时器
            if (examTimerInterval) {
                clearInterval(examTimerInterval);
            }
            
            // 返回首页
            window.location.href = "/client/index";
        }
    }

    // 用新的配置替换整个examPage对象
    const examPage = {
        type: 'page',
        title: '',
        body: [
            {
                type: 'service',
                initApi: '/identity/api/identity/profile',
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
                                className: 'exam-timer-container',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="exam-timer">剩余时间：${timer.displayText}</div>'
                                    }
                                ]
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
                                    alert("fetchSuccess事件已触发");
                                    console.log("fetchSuccess事件已触发", event.data);
                                    window.GlobalData.set('user.id', event.data.id || null);
                                    window.GlobalData.set('user.name', event.data.name || event.data.userName || '');
                                    window.GlobalData.set('user.avatar', event.data.avatar || '');
                                    window.GlobalData.set('user.roles', event.data.roles || []);
                                    window.GlobalData.syncToAmis(window.amisInstance);
                                `
                            }
                        ]
                    }
                }
            },
            {
                type: 'service',
                api: `/exam/api/exam/client/${examId}`,
                className: 'exam-container',
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    console.log("考试数据加载成功", event.data); 
                                    recordId = event.data.recordId; 
                                    
                                    // 更新全局考试数据
                                    window.GlobalData.set('exam.name', event.data.name || '');
                                    window.GlobalData.set('exam.duration', event.data.duration || 0);
                                    window.GlobalData.set('exam.startTime', event.data.startTime || null);
                                    window.GlobalData.set('exam.endTime', event.data.endTime || null);
                                    window.GlobalData.set('exam.totalScore', event.data.totalScore || 0);
                                    
                                    // 启动计时器
                                    try {
                                        startExamTimer(event.data.duration, event.data.startTime);
                                    } catch (error) {
                                        console.error("调用计时器函数失败", error);
                                    }
                                `
                            }
                        ]
                    }
                },
                body: [
                    {
                        type: 'panel',
                        title: '${name}',
                        bodyClassName: 'exam-info-panel',
                        body: [
                            {
                                type: 'flex',
                                justify: 'space-between',
                                className: 'exam-info',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '开始时间：${startTime|date:YYYY-MM-DD HH:mm:ss}',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '结束时间：${endTime|date:YYYY-MM-DD HH:mm:ss}',
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
                                type: 'divider'
                            },
                            {
                                type: 'service',
                                api: `/exam/api/exam/client/${examId}/amis`,
                                className: 'question-container',
                                onEvent: {
                                    fetchInited: {
                                        actions: [
                                            {
                                                actionType: "custom",
                                                script: `
                                                    console.log("题目Amis配置加载成功", event.data); 
                                                `
                                            }
                                        ]
                                    }
                                }
                            },
                            {
                                type: 'divider'
                            },
                            {
                                type: 'flex',
                                justify: 'center',
                                className: 'exam-actions',
                                items: [
                                    {
                                        type: 'button',
                                        label: '提交试卷',
                                        level: 'primary',
                                        size: 'lg',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'confirm',
                                                        componentId: 'submitConfirm',
                                                        dialog: {
                                                            title: '确认提交',
                                                            body: '确定要提交试卷吗？提交后将无法修改答案。',
                                                            onEvent: {
                                                                confirm: {
                                                                    actions: [
                                                                        {
                                                                            actionType: 'custom',
                                                                            script: 'submitExam();'
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    },
                                    {
                                        type: 'button',
                                        label: '取消考试',
                                        level: 'link',
                                        size: 'lg',
                                        className: 'ml-3',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'custom',
                                                        script: 'cancelExam();'
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                ]
                            }
                        ],
                        actions: []
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
            '.exam-timer-container': {
                'padding': '8px 16px',
                'background-color': '#f8f9fa',
                'border-radius': '4px'
            },
            '.exam-timer': {
                'color': 'var(--danger)',
                'font-size': '18px',
                'font-weight': 'bold'
            },
            '.exam-container': {
                'margin': '0 auto',
                'max-width': '1000px'
            },
            '.exam-info': {
                'margin-bottom': '20px',
                'flex-wrap': 'wrap'
            },
            '.exam-info-item': {
                'margin-right': '20px',
                'margin-bottom': '10px'
            },
            '.question-item': {
                'margin-bottom': '30px',
                'padding': '15px',
                'border': '1px solid #e8e8e8',
                'border-radius': '8px',
                'background-color': '#fafafa'
            },
            '.question-label': {
                'font-size': '16px',
                'font-weight': '500',
                'margin-bottom': '10px',
                'display': 'block'
            },
            '.exam-actions': {
                'margin-top': '30px',
                'margin-bottom': '20px'
            },
            '@media (max-width: 768px)': {
                '.exam-container': {
                    'padding': '10px'
                },
                '.question-label': {
                    'font-size': '14px'
                },
                '.client-header': {
                    'padding': '10px'
                },
                '.client-logo span': {
                    'font-size': '16px'
                }
            }
        }
    };

    // 注册用于提交考试的全局方法
    window.submitExam = submitExam;
    window.cancelExam = cancelExam;
    window.startExamTimer = startExamTimer;
    window.updateTimerDisplay = updateTimerDisplay;
    window.saveAnswer = saveAnswer;

    // 初始化amis
    let amisInstance = amis.embed(
        '#root',
        examPage,
        {
            location: history.location,
            data: {
                timer: {
                    displayText: '加载中...',
                    hours: 0,
                    minutes: 0,
                    seconds: 0,
                    remainingSeconds: 0
                }
            },
            locale: 'zh-CN',
            context: {
                API_HOST: apiHost || '',
                WEB_HOST: webHost || '',
                aspire_dashboard: aspire_dashboard || ''
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
                    alert('您没有权限参加此考试！');
                    window.location.href = "/client/index";
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

    window.amisInstance = amisInstance;

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})(); 