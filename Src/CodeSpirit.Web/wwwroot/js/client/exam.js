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
            totalScore: 0,
            recordId: null
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
            
            // 确保amisInstance已初始化
            if (!window.amisInstance) {
                console.warn("计时器启动时amisInstance未初始化，将在5秒后重试");
                setTimeout(() => {
                    if (window.amisInstance) {
                        console.log("重试成功，amisInstance已初始化");
                        updateTimerDisplay();
                    } else {
                        console.error("重试失败，amisInstance仍未初始化");
                    }
                }, 5000);
            }
            
            // 更新计时器显示
            updateTimerDisplay();
            
            // 启动计时器
            console.log("正在启动计时器间隔");
            examTimerInterval = setInterval(() => {
                remainingTime--;
                console.log("剩余时间", remainingTime);
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
            // 直接更新数据而不是通过syncToAmis函数
            try {
                // 创建计时器数据对象
                const timerData = {
                    displayText: displayText,
                    hours: hours,
                    minutes: minutes,
                    seconds: seconds,
                    remainingSeconds: remainingTime
                };
                
                // 防御性编程：检查props和data是否存在
                window.amisInstance.updateProps({
                    data: {
                        timer: timerData
                    }
                });
                
                // 触发重新渲染
                if (typeof window.amisInstance.forceUpdate === 'function') {
                    window.amisInstance.forceUpdate();
                }
                
                // 更新DOM，强制显示最新时间
                const timerElements = document.querySelectorAll('.exam-timer');
                if (timerElements && timerElements.length > 0) {
                    timerElements.forEach(el => {
                        el.innerHTML = `剩余时间：${displayText}`;
                    });
                }
            } catch (e) {
                console.error("更新计时器显示时出错", e);
                
                // 出错时直接更新DOM作为后备方案
                const timerElements = document.querySelectorAll('.exam-timer');
                if (timerElements && timerElements.length > 0) {
                    timerElements.forEach(el => {
                        el.innerHTML = `剩余时间：${displayText}`;
                    });
                }
            }
        } else {
            console.warn("amisInstance未初始化，无法更新计时器显示");
            // 尝试直接更新DOM
            const timerElements = document.querySelectorAll('.exam-timer');
            if (timerElements && timerElements.length > 0) {
                timerElements.forEach(el => {
                    el.innerHTML = `剩余时间：${displayText}`;
                });
            }
        }
    }
    
    // 保存答案
    function saveAnswer(questionId, answer) {
        // 确保答案格式正确
        let processedAnswer = answer;
        
        // 如果不是字符串或数组，则转换为字符串
        if (answer !== null && answer !== undefined && typeof answer !== 'string' && !Array.isArray(answer)) {
            processedAnswer = String(answer);
        }
        
        // 如果是数组但包含非字符串元素，规范化数组
        if (Array.isArray(processedAnswer)) {
            processedAnswer = processedAnswer.map(item => {
                if (item !== null && item !== undefined && typeof item !== 'string') {
                    return String(item);
                }
                return item;
            });
        }
        
        // 查找已有答案
        const existingIndex = examAnswers.findIndex(a => a.questionId === questionId);
        
        if (existingIndex >= 0) {
            // 更新已有答案
            examAnswers[existingIndex].answer = processedAnswer;
        } else {
            // 添加新答案
            examAnswers.push({
                questionId: questionId,
                answer: processedAnswer
            });
        }
        
        console.log(`保存题目 ${questionId} 的答案:`, processedAnswer);
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

        console.debug(answers);
        
        // 从全局数据获取recordId
        const recordId = window.globalData.exam.recordId;
        
        // 检查recordId是否有效
        if (!recordId) {
            console.error("提交失败: recordId为空");
            alert("提交失败：无法获取考试记录ID，请刷新页面重试");
            return;
        }
        
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
                api: `/exam/api/exam/client/${examId}/basic`,
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
                                    window.GlobalData.set('exam.recordId', event.data.recordId || null);
                                    
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
                        header: {
                            type: 'tpl',
                            tpl: '<div style="font-size: 24px; font-weight: bold; color: var(--primary-color); text-align: center;">${name}</div>',
                            className: 'exam-title'
                        },
                        headerClassName: 'exam-panel-header',
                        bodyClassName: 'exam-info-panel',
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
                                        tpl: '结束时间：${endTime}',
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
                                schemaApi: `get:/exam/api/exam/client/${examId}/amis`,
                                className: 'question-container',
                                onEvent: {
                                    fetchSchemaInited: {
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
                                        confirmTitle: '确认提交',
                                        confirmText: '确定要提交试卷吗？提交后将无法修改答案。',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'custom',
                                                        script: 'submitExam();',
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
            // 计时器样式
            '.exam-timer-container': {
                'padding': '10px 18px',
                'background-color': '#fff',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 2px 8px rgba(0,0,0,0.08)',
                'border': '1px solid #f0f0f0',
                'transition': 'all 0.3s ease'
            },
            '.exam-timer': {
                'color': 'var(--danger-color)',
                'font-size': '20px',
                'font-weight': 'bold',
                'font-family': 'Consolas, monospace',
                'display': 'flex',
                'align-items': 'center'
            },
            '.exam-timer::before': {
                'content': '""',
                'display': 'inline-block',
                'width': '12px',
                'height': '12px',
                'background-color': 'var(--danger-color)',
                'border-radius': '50%',
                'margin-right': '10px',
                'animation': 'pulse 1s infinite'
            },
            '@keyframes pulse': {
                '0%': {
                    'opacity': '0.6',
                    'transform': 'scale(0.9)'
                },
                '50%': {
                    'opacity': '1',
                    'transform': 'scale(1.1)'
                },
                '100%': {
                    'opacity': '0.6',
                    'transform': 'scale(0.9)'
                }
            },
            // 考试容器样式
            '.exam-container': {
                'margin': '20px auto',
                'max-width': '1100px',
                'padding': '0 20px'
            },
            // 面板样式
            '.am-Panel': {
                'border-radius': 'var(--border-radius)',
                'overflow': 'hidden',
                'box-shadow': 'var(--box-shadow)',
                'border': 'none'
            },
            '.am-Panel-heading': {
                'background-color': '#fff',
                'border-bottom': '1px solid #eaeaea',
                'padding': '15px 20px',
                'text-align': 'center'
            },
            '.am-Panel-body': {
                'padding': '20px'
            },
            // panel标题样式
            '.exam-panel-header': {
                'background-color': '#fff',
                'border-bottom': '1px solid #eaeaea',
                'padding': '15px 20px',
                'text-align': 'center',
                'background-image': 'linear-gradient(to right, rgba(63, 81, 181, 0.1), rgba(63, 81, 181, 0.05), rgba(63, 81, 181, 0))'
            },
            '.exam-panel-header .am-Panel-title, .exam-panel-header .exam-title': {
                'font-size': '24px !important',
                'font-weight': 'bold !important',
                'color': 'var(--primary-color) !important',
                'text-shadow': '0 1px 2px rgba(0,0,0,0.1) !important',
                'letter-spacing': '1px !important'
            },
            // 考试信息样式
            '.exam-info-panel': {
                'background-color': '#fff',
                'padding': '15px'
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
            // 题目样式
            '.question-container': {
                'margin-top': '20px'
            },
            '.question-item': {
                'margin-bottom': '30px',
                'padding': '20px',
                'border': '1px solid #e8e8e8',
                'border-radius': 'var(--border-radius)',
                'background-color': '#fff',
                'box-shadow': '0 2px 8px rgba(0,0,0,0.04)',
                'transition': 'all 0.3s ease',
                'position': 'relative'
            },
            '.question-item:hover': {
                'box-shadow': '0 5px 15px rgba(0,0,0,0.08)',
                'transform': 'translateY(-2px)'
            },
            '.question-item::before': {
                'content': 'attr(data-question-index)',
                'position': 'absolute',
                'top': '-12px',
                'left': '20px',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'width': '30px',
                'height': '30px',
                'background-color': 'var(--primary-color)',
                'color': 'white',
                'border-radius': '50%',
                'font-weight': 'bold',
                'box-shadow': '0 2px 5px rgba(0,0,0,0.2)',
                'z-index': '1'
            },
            '.question-label': {
                'font-size': '16px',
                'font-weight': '500',
                'margin-bottom': '20px',
                'display': 'block',
                'padding-left': '15px',
                'border-left': '4px solid var(--primary-color)',
                'line-height': '1.5'
            },
            '.question-type-tag': {
                'display': 'inline-block',
                'padding': '2px 8px',
                'border-radius': '12px',
                'background-color': 'rgba(63, 81, 181, 0.1)',
                'color': 'var(--primary-color)',
                'font-size': '12px',
                'margin-right': '10px',
                'font-weight': 'bold'
            },
            '.question-score': {
                'display': 'inline-block',
                'padding': '2px 8px',
                'border-radius': '12px',
                'background-color': 'rgba(76, 175, 80, 0.1)',
                'color': 'var(--success-color)',
                'font-size': '12px',
                'float': 'right',
                'font-weight': 'bold'
            },
            '.question-content': {
                'margin-bottom': '15px',
                'padding-bottom': '15px',
                'border-bottom': '1px dashed #eaeaea'
            },
            // 选项样式
            '.am-RadioControl-group, .am-CheckboxControl-group': {
                'padding': '10px 0',
                'display': 'flex',
                'flex-direction': 'column',
                'gap': '12px'
            },
            '.am-RadioControl, .am-CheckboxControl': {
                'margin-bottom': '0',
                'padding': '12px 16px',
                'border-radius': 'var(--border-radius)',
                'transition': 'all 0.3s ease',
                'border': '1px solid #e8e8e8',
                'background-color': '#fff',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.02)',
                'position': 'relative',
                'overflow': 'hidden',
                'cursor': 'pointer'
            },
            '.am-RadioControl:hover, .am-CheckboxControl:hover': {
                'background-color': '#f9f9ff',
                'border-color': '#d0d5ff',
                'box-shadow': '0 3px 6px rgba(0,0,0,0.05)',
                'transform': 'translateY(-1px)'
            },
            '.enhanced-options .am-RadioControl, .enhanced-options .am-CheckboxControl': {
                'margin-bottom': '8px',
                'border-left': '3px solid transparent'
            },
            '.question-container .am-RadioControl, .question-container .am-CheckboxControl': {
                'border': '1px solid #e0e0e0',
                'border-radius': '8px',
                'margin-bottom': '10px',
                'transition': 'all 0.2s ease-in-out'
            },
            '.am-RadioControl-input:checked + .am-RadioControl-icon, .am-CheckboxControl-input:checked + .am-CheckboxControl-icon': {
                'background-color': 'var(--primary-color)',
                'border-color': 'var(--primary-color)'
            },
            '.am-RadioControl.is-checked, .am-CheckboxControl.is-checked': {
                'background-color': 'rgba(63, 81, 181, 0.05)',
                'border-left': '3px solid var(--primary-color)',
                'padding-left': '14px',
                'font-weight': '500'
            },
            '.am-RadioControl-label, .am-CheckboxControl-label': {
                'font-size': '15px',
                'line-height': '1.5',
                'padding-left': '5px'
            },
            // 选项字母标记
            '.option-label': {
                'display': 'inline-block',
                'width': '26px',
                'height': '26px',
                'line-height': '26px',
                'text-align': 'center',
                'background-color': '#f0f0f0',
                'color': '#333',
                'border-radius': '50%',
                'margin-right': '10px',
                'font-weight': 'bold',
                'font-size': '14px',
                'box-shadow': '0 2px 4px rgba(0,0,0,0.1)'
            },
            '.is-checked .option-label': {
                'background-color': 'var(--primary-color)',
                'color': 'white',
                'box-shadow': '0 2px 4px rgba(63, 81, 181, 0.3)'
            },
            '.enhanced-options .am-RadioControl-label, .enhanced-options .am-CheckboxControl-label': {
                'font-size': '15px',
                'line-height': '1.6',
                'font-family': '"PingFang SC", "Microsoft YaHei", sans-serif',
                'display': 'flex',
                'align-items': 'center'
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
            '.am-Button--link': {
                'color': '#666',
                'font-size': '16px',
                'transition': 'all 0.3s ease'
            },
            '.am-Button--link:hover': {
                'color': 'var(--danger-color)',
                'text-decoration': 'none'
            },
            // 分割线样式
            '.am-Divider': {
                'margin': '20px 0',
                'background-color': '#eaeaea'
            },
            // 响应式样式
            '@media (max-width: 768px)': {
                '.exam-container': {
                    'padding': '10px'
                },
                '.question-label': {
                    'font-size': '15px'
                },
                '.client-header': {
                    'padding': '10px 15px',
                    'flex-direction': 'column'
                },
                '.client-logo span': {
                    'font-size': '18px'
                },
                '.exam-timer': {
                    'font-size': '16px'
                },
                '.exam-info-item': {
                    'width': '100%',
                    'margin-right': '0'
                },
                '.user-info-container': {
                    'margin-right': '0',
                    'margin-bottom': '10px'
                },
                '.exam-title': {
                    'font-size': '20px'
                }
            },
            // 新增样式
            '.enhanced-options': {
                'border': '1px solid #eaeaea',
                'border-radius': 'var(--border-radius)',
                'padding': '10px 15px',
                'margin-top': '10px',
                'background-color': '#fafafa',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.03)'
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

    // 立即设置全局amis实例以确保计时器可以使用
    window.amisInstance = amisInstance;

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})(); 