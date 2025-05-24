/**
 * 在线练习系统客户端主模块
 * 负责练习页面渲染、答题、计时和提交功能
 */
(function () {
    'use strict';

    // 引入依赖模块
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 获取练习ID
    const practiceId = window.location.pathname.split('/').pop();
    
    // 常量定义
    const CONSTANTS = {
        TIMER_UPDATE_INTERVAL: 1000,       // 计时器更新频率(毫秒)
        SUBMIT_REDIRECT_DELAY: 1500,       // 提交后跳转延迟(毫秒)
        MAX_RETRY_COUNT: 3,                // 最大重试次数
        RETRY_DELAY: 2000                  // 重试延迟基础时间(毫秒)
    };
    
    // 全局数据对象
    window.globalData = {
        user: {
            id: null,
            name: '',
            avatar: '',
            roles: []
        },
        practice: {
            id: practiceId,
            name: '',
            timeLimit: 0,
            startTime: null,
            recordId: null,
            questionCount: 0,
            totalScore: 0
        },
        timer: {
            displayText: '加载中...',
            hours: 0,
            minutes: 0,
            seconds: 0,
            elapsed: 0
        }
    };

    // 私有状态变量
    let practiceTimerInterval = null;    // 计时器间隔ID
    let elapsedTime = 0;                 // 已用时间(秒)
    window.practiceAnswers = [];         // 答案集合 - 全局变量
    let recordId = null;                 // 练习记录ID
    let isSubmitting = false;            // 是否正在提交
    
    // AMIS实例引用
    window.amisInstance = null;

    /**
     * 全局数据管理工具
     */
    window.GlobalData = {
        /**
         * 获取指定路径的数据
         */
        get: function (path, defaultValue) {
            if (!path) return defaultValue;
            
            const keys = path.split('.');
            let current = window.globalData;

            for (const key of keys) {
                if (current === undefined || current === null) {
                    return defaultValue;
                }
                current = current[key];
            }

            return current !== undefined ? current : defaultValue;
        },

        /**
         * 设置指定路径的数据
         */
        set: function (path, value) {
            if (!path) return value;
            
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

        /**
         * 将全局数据同步到amis上下文
         */
        syncToAmis: function (amisInstance, selectedPaths) {
            if (!amisInstance || !amisInstance.updateProps) return;

            try {
                const data = {};
                
                if (selectedPaths && Array.isArray(selectedPaths)) {
                    // 只同步指定路径的数据
                    for (const path of selectedPaths) {
                        const keys = path.split('.');
                        let current = data;
                        let source = window.globalData;
                        let isValid = true;

                        for (let i = 0; i < keys.length - 1; i++) {
                            if (source[keys[i]] === undefined) {
                                isValid = false;
                                break;
                            }

                            if (current[keys[i]] === undefined) {
                                current[keys[i]] = {};
                            }
                            current = current[keys[i]];
                            source = source[keys[i]];
                        }

                        if (isValid) {
                            const lastKey = keys[keys.length - 1];
                            current[lastKey] = source[lastKey];
                        }
                    }
                } else {
                    // 同步所有数据
                    Object.assign(data, window.globalData);
                }

                // 更新AMIS实例
                amisInstance.updateProps({ data });
            } catch (error) {
                console.error('[GlobalData] 同步数据到AMIS失败:', error);
            }
        },
        
        /**
         * 更新单个字段并同步到AMIS
         */
        update: function(path, value, syncToAmis = true) {
            this.set(path, value);
            
            if (syncToAmis && window.amisInstance) {
                this.syncToAmis(window.amisInstance, [path]);
            }
        }
    };
    
    // 开始计时器 - 跟踪已练习时间
    function startPracticeTimer() {
        console.log("开始启动计时器");
        
        // 清除之前的计时器
        if (practiceTimerInterval) {
            clearInterval(practiceTimerInterval);
        }
        
        // 初始化已用时间
        elapsedTime = 0;
        
        // 更新计时器显示
        updateTimerDisplay();
        
        // 启动计时器，每秒更新一次
        practiceTimerInterval = setInterval(() => {
            elapsedTime++;
            updateTimerDisplay();
        }, 1000);
        
        console.log("计时器已启动");
    }
    
    // 更新计时器显示
    function updateTimerDisplay() {
        try {
            // 计算小时、分钟和秒
            const hours = Math.floor(elapsedTime / 3600);
            const minutes = Math.floor((elapsedTime % 3600) / 60);
            const seconds = elapsedTime % 60;
            
            // 格式化显示文本
            const displayText = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            // 更新全局数据
            window.GlobalData.set('timer.elapsed', elapsedTime);
            window.GlobalData.set('timer.hours', hours);
            window.GlobalData.set('timer.minutes', minutes);
            window.GlobalData.set('timer.seconds', seconds);
            window.GlobalData.set('timer.displayText', displayText);
            
            // 直接更新DOM显示
            updateTimerDOMDisplay(displayText);
        } catch (error) {
            console.error("更新计时器显示时出错:", error);
        }
    }
    
    // 辅助函数：更新计时器DOM显示
    function updateTimerDOMDisplay(displayText) {
        const timerElements = document.querySelectorAll('.practice-timer');
        if (timerElements && timerElements.length > 0) {
            timerElements.forEach(el => {
                el.innerHTML = `已用时间：${displayText}`;
            });
        }
    }
    
    // 保存答案
    function saveAnswer(questionId, answer) {
        try {
            // 验证questionId有效性
            if (!questionId || questionId === 'undefined' || questionId === 'null') {
                console.error(`无法保存答案：题目ID无效(${questionId})`);
                return false;
            }
            
            // 确保practiceAnswers对象存在
            if (!window.practiceAnswers) {
                window.practiceAnswers = {};
            }
            
            // 保存答案到practiceAnswers对象
            window.practiceAnswers[questionId] = answer;
            
            // 更新答题卡状态
            updateAnswerCardStatus(questionId);
            
            // 获取练习记录ID
            const recordId = window.globalData.practice.recordId;
            
            if (!recordId) {
                console.error(`保存答案失败：缺少练习记录ID(${recordId})`);
                return false;
            }
            
            // 发送答案到服务器
            sendAnswerToServer(recordId, questionId, answer);
            
            return true;
        } catch (error) {
            console.error(`保存答案时出错:`, error);
            return false;
        }
    }

    // 向服务器提交单个答案
    function sendAnswerToServer(recordId, questionId, answer, retryCount = 0) {
        const maxRetries = CONSTANTS.MAX_RETRY_COUNT;
        const retryDelay = CONSTANTS.RETRY_DELAY;
        
        console.log(`向服务器提交题目 ${questionId} 的答案，尝试次数: ${retryCount + 1}`);
        
        // 验证questionId有效性
        if (!questionId || questionId === 'undefined' || questionId === 'null') {
            console.error(`题目ID无效: ${questionId}，不提交答案`);
            return false;
        }
        
        // 确保questionId作为字符串提交
        const answerDto = {
            questionId: String(questionId),
            answer: answer
        };
        
        fetch(`/exam/api/client/practice/${recordId}/save-answer`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + localStorage.getItem('token'),
                'X-Forwarded-With': 'CodeSpirit'
            },
            body: JSON.stringify(answerDto)
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            if (data.status === 0) {
                console.log(`题目 ${questionId} 的答案已成功提交到服务器`);
            } else {
                console.error(`服务器返回错误: ${data.msg || '未知错误'}`);
                // 如果服务器返回了明确的错误，可以考虑重试
                if (retryCount < maxRetries) {
                    setTimeout(() => {
                        sendAnswerToServer(recordId, questionId, answer, retryCount + 1);
                    }, retryDelay * (retryCount + 1));
                }
            }
        })
        .catch(error => {
            console.error(`向服务器提交题目 ${questionId} 的答案失败:`, error);
            
            // 如果还有重试次数，则延迟后重试
            if (retryCount < maxRetries) {
                console.log(`将在 ${retryDelay * (retryCount + 1) / 1000} 秒后重试...`);
                setTimeout(() => {
                    sendAnswerToServer(recordId, questionId, answer, retryCount + 1);
                }, retryDelay * (retryCount + 1));
            } else {
                console.error(`已达到最大重试次数 (${maxRetries})，题目 ${questionId} 的答案将在提交时一并提交`);
            }
        });
    }

    // 提交练习
    function submitPractice() {
        // 获取recordId
        const recordId = window.globalData.practice.recordId;
        
        // 检查recordId是否有效
        if (!recordId) {
            console.error("提交失败: recordId为空");
            alert("提交失败：无法获取练习记录ID，请刷新页面重试");
            return;
        }
        
        // 显示提交中的提示
        alert("正在提交您的练习答案，请稍候...");
        
        // 转换为后端需要的格式，确保questionId是字符串类型
        const answers = Object.entries(window.practiceAnswers).map(([questionId, answer]) => {
            return {
                questionId: String(questionId),
                answer: answer
            };
        });
        
        // 提交练习
        fetch(`/exam/api/client/practice/${recordId}/submit`, {
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
                if (practiceTimerInterval) {
                    clearInterval(practiceTimerInterval);
                }
                
                // 显示成功提示
                alert("练习已成功提交！");
                
                // 跳转到结果页面
                setTimeout(() => {
                    window.location.href = `/client/practice/result/${recordId}`;
                }, CONSTANTS.SUBMIT_REDIRECT_DELAY);
            } else {
                // 显示错误提示
                alert(data.msg || "提交失败，请重试");
            }
        })
        .catch(error => {
            console.error("提交练习失败", error);
            alert("提交失败，请检查网络连接后重试");
        });
    }

    // 取消练习
    function cancelPractice() {
        if (confirm("确定要放弃本次练习吗？已答题目将不会保存。")) {
            // 清除计时器
            if (practiceTimerInterval) {
                clearInterval(practiceTimerInterval);
            }
            
            // 返回练习首页
            window.location.href = "/client/practice";
        }
    }

    // 更新答题卡状态
    function updateAnswerCardStatus(questionId) {
        try {
            // 查找对应的答题卡元素
            const answerCardItem = document.querySelector(`.answer-card-item[data-question-id="${questionId}"]`);
            if (answerCardItem) {
                answerCardItem.classList.add('answered');
            }
            
            return true;
        } catch (error) {
            console.error('更新答题卡状态时出错:', error);
            return false;
        }
    }
    
    // 跳转到题目
    window.scrollToQuestion = function(questionId, questionIndex) {
        try {
            console.log(`尝试滚动到题目 ${questionIndex}, ID: ${questionId}`);
            // 查找对应的题目元素
            const questionElement = document.getElementById(`question_${questionId}_label`);
            if (questionElement) {
                // 获取固定头部的高度
                const fixedHeaderHeight = document.querySelector('.fixed-header-container')?.offsetHeight || 0;
                
                // 获取元素的位置
                const rect = questionElement.getBoundingClientRect();
                const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                
                // 计算目标滚动位置，减去固定头部高度和额外的20px空间
                const targetScrollPosition = rect.top + scrollTop - fixedHeaderHeight - 20;
                
                // 平滑滚动到目标位置
                window.scrollTo({
                    top: targetScrollPosition,
                    behavior: 'smooth'
                });
                
                // 添加高亮效果
                questionElement.parentElement.classList.add('highlight-question');
                // 3秒后移除高亮
                setTimeout(function() {
                    questionElement.parentElement.classList.remove('highlight-question');
                }, 3000);
                
                return true;
            } else {
                console.warn(`未找到题目元素: ${questionId}`);
                return false;
            }
        } catch (error) {
            console.error(`滚动到题目时出错:`, error);
            return false;
        }
    };

    // 初始化练习数据
    function initializePracticeData(practiceData) {
        try {
            // 获取服务器返回的数据
            recordId = practiceData.recordId;
            
            // 更新全局练习数据
            window.globalData.practice.name = practiceData.name || '';
            window.globalData.practice.timeLimit = practiceData.timeLimit || 0;
            window.globalData.practice.startTime = practiceData.startTime || null;
            window.globalData.practice.recordId = practiceData.recordId || null;
            window.globalData.practice.questionCount = practiceData.questions?.length || 0;
            window.globalData.practice.totalScore = practiceData.totalScore || 0;
            
            // 为题目数据添加answered状态
            if (Array.isArray(practiceData.questions)) {
                // 初始化题目的answered状态为false
                practiceData.questions.forEach(question => {
                    question.answered = question.answer && question.answer.length > 0;
                });
                
                window.globalData.practice.questions = practiceData.questions;
                
                // 如果已有答案，加载到practiceAnswers
                practiceData.questions.forEach(question => {
                    if (question.answer) {
                        window.practiceAnswers[question.id] = question.answer;
                    }
                });
            }
            
            // 启动计时器
            startPracticeTimer();
            
            console.log("成功初始化练习数据");
            
            // 更新AMIS中的题目数据
            if (window.amisInstance) {
                window.amisInstance.updateProps({
                    data: {
                        questions: practiceData.questions || []
                    }
                });
            }
            
        } catch (error) {
            console.error("初始化练习数据时出错:", error);
        }
    }

    // 更新固定头部高度
    function updateFixedHeaderHeight() {
        try {
            const fixedHeaderContainer = document.querySelector('.fixed-header-container');
            if (fixedHeaderContainer) {
                const height = fixedHeaderContainer.offsetHeight;
                document.documentElement.style.setProperty('--fixed-header-height', height + 'px');
                
                // 更新spacer高度
                const spacer = document.querySelector('.fixed-header-spacer');
                if (spacer) {
                    spacer.style.height = (height + 10) + 'px';
                }
            }
        } catch (error) {
            console.error("更新固定头部高度时出错:", error);
        }
    }
    
    // 添加滚动监听逻辑，实现顶部栏的紧凑化
    function setupHeaderScroll() {
        // 获取头部容器
        const fixedHeaderContainer = document.querySelector('.fixed-header-container');
        if (!fixedHeaderContainer) {
            console.warn("未找到固定头部容器，无法设置紧凑化效果");
            return;
        }
        
        const scrollThreshold = 50; // 滚动多少像素后紧凑化

        // 滚动事件处理函数
        function handleScroll() {
            if (window.scrollY > scrollThreshold) {
                // 滚动足够距离后添加紧凑样式
                fixedHeaderContainer.classList.add('compact-header');
            } else {
                // 回到顶部时移除紧凑样式
                fixedHeaderContainer.classList.remove('compact-header');
            }
        }

        // 添加滚动事件监听
        window.addEventListener('scroll', handleScroll);
        
        // 初始调用一次，确保页面刷新后也应用正确的样式
        handleScroll();
    }

    // 页面配置
    const practicePage = {
        type: 'page',
        body: [
            // 头部固定区域开始
            {
                type: 'container',
                className: 'fixed-header-container',
                body: [
                    {
                        type: 'service',
                        api: '/identity/api/identity/profile',
                        className: 'client-header',
                        body: [
                            {
                                type: 'flex',
                                justify: 'space-between',
                                className: 'w-full header-container',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="client-logo"><img src="' + (window.siteSettings ? window.siteSettings.logoUrl : '/logo.png') + '" /><span>' + (window.siteSettings ? window.siteSettings.clientAppName : '练习系统') +'</span></div>',
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
                                            console.log("用户数据加载成功", event.data);
                                            window.globalData.user.id = event.data.id || null;
                                            window.globalData.user.name = event.data.name || event.data.userName || '';
                                            window.globalData.user.avatar = event.data.avatar || '';
                                            window.globalData.user.roles = event.data.roles || [];
                                        `
                                    }
                                ]
                            }
                        }
                    },
                    {
                        type: 'flex',
                        justify: 'center',
                        className: 'w-full practice-timer-container',
                        items: [
                            {
                                type: 'tpl',
                                tpl: '<div class="practice-timer">已用时间：${timer.displayText}</div>'
                            }
                        ]
                    },
                    {
                        type: 'flex',
                        justify: 'center',
                        className: 'w-full header-status-container answer-card-row',
                        items: [
                            {
                                type: 'flex',
                                justify: 'start',
                                alignItems: 'center',
                                className: 'practice-answer-card-container',
                                id: "answerCardContainer",
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="answer-card-title">答题卡</div>'
                                    },
                                    {
                                        type: 'each',
                                        name: 'questions',
                                        visibleOn: "questions && questions.length > 0",
                                        trackExpression: "${item.id}",
                                        items: {
                                            type: 'tpl',
                                            tpl: '<a href="javascript:void(0)" data-question-id="${item.id}" data-question-index="${index + 1}" title="${index + 1}. ${item.type === \'SingleChoice\' ? \'单选题\' : item.type === \'MultipleChoice\' ? \'多选题\' : item.type === \'TrueFalse\' ? \'判断题\' : \'主观题\'}">${index + 1}</a>',
                                            className: 'answer-card-item ${item.answered ? "answered" : "unanswered"}',
                                            onEvent: {
                                                click: {
                                                    actions: [
                                                        {
                                                            actionType: "custom",
                                                            script: "scrollToQuestion(event.data.item.id, event.data.index + 1);"
                                                        }
                                                    ]
                                                }
                                            }
                                        }
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="answer-card-empty">加载中...</div>',
                                        visibleOn: "!questions || questions.length === 0"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            // 头部固定区域结束
            // 添加空白填充区域，防止内容被固定头部遮挡
            {
                type: 'tpl',
                tpl: '<div class="fixed-header-spacer"></div>',
                className: 'fixed-header-spacer'
            },
            {
                type: 'service',
                api: `/exam/api/client/practice/${practiceId}/basic`,
                className: 'practice-container',
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        console.log("练习数据加载成功", event.data);
                                        
                                        if (!event || !event.data) {
                                            console.error("事件数据无效", event);
                                            return;
                                        }
                                        
                                        initializePracticeData(event.data);
                                    } catch (error) {
                                        console.error("初始化练习数据时出错:", error);
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
                            className: 'practice-title'
                        },
                        headerClassName: 'practice-panel-header',
                        bodyClassName: 'practice-info-panel',
                        className: 'practice-panel',
                        body: [
                            {
                                type: 'flex',
                                justify: 'space-between',
                                className: 'practice-info',
                                items: [
                                    {
                                        type: 'tpl',
                                        tpl: '题目数：${questions.length} 题',
                                        className: 'practice-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '总分：${totalScore} 分',
                                        className: 'practice-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '时间限制：${timeLimit || "不限"} 分钟',
                                        className: 'practice-info-item'
                                    }
                                ]
                            },
                            {
                                type: 'divider'
                            },
                            {
                                type: 'container',
                                className: 'question-container',
                                body: {
                                    type: "form",
                                    title: "",
                                    id: "practiceForm",
                                    actions: [],  // 隐藏表单自带的提交按钮
                                    body: {
                                        type: "each",
                                        name: "questions",
                                        trackExpression: "${item.id}",
                                        items: {
                                            type: "container",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class=\"question-label\" id=\"question_${item.id}_label\"><pre>${index + 1}. ${item.content} </pre><span style=\"color:#999\">（${item.score}分）</span></div>",
                                                    inline: false
                                                },
                                                {
                                                    type: "container",
                                                    body: [
                                                        {
                                                            type: "radios",
                                                            name: "question_${item.id}",
                                                            source: "${options}",
                                                            mode: "horizontal",
                                                            value: "${answer}",
                                                            visibleOn: "item.type === 'SingleChoice'",
                                                            onEvent: {
                                                                change: {
                                                                    actions: [
                                                                        {
                                                                            actionType: "custom",
                                                                            script: "saveAnswer(event.data.__super.item.id, event.data.value);"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        },
                                                        {
                                                            type: "checkboxes",
                                                            name: "question_${item.id}",
                                                            options: "${options}",
                                                            mode: "horizontal",
                                                            value: "${answer}",
                                                            required: "${item.isRequired}",
                                                            visibleOn: "item.type === 'MultipleChoice'",
                                                            onEvent: {
                                                                change: {
                                                                    actions: [
                                                                        {
                                                                            actionType: "custom",
                                                                            script: "saveAnswer(event.data.__super.item.id, event.data.value);"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        },
                                                        {
                                                            type: "radios",
                                                            name: "question_${item.id}",
                                                            options: [
                                                                {
                                                                    label: "正确",
                                                                    value: "True"
                                                                },
                                                                {
                                                                    label: "错误",
                                                                    value: "False"
                                                                }
                                                            ],
                                                            mode: "horizontal",
                                                            value: "${answer}",
                                                            visibleOn: "${item.type === 'TrueFalse'}",
                                                            onEvent: {
                                                                change: {
                                                                    actions: [
                                                                        {
                                                                            actionType: "custom",
                                                                            script: "saveAnswer(event.data.__super.item.id, event.data.value);"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        },
                                                        {
                                                            type: "textarea",
                                                            name: "question_${item.id}",
                                                            placeholder: "请输入答案",
                                                            minRows: 3,
                                                            maxRows: 6,
                                                            required: "${item.isRequired}",
                                                            visibleOn: "${item.type !== 'SingleChoice' && item.type !== 'MultipleChoice' && item.type !== 'TrueFalse'}",
                                                            onEvent: {
                                                                change: {
                                                                    actions: [
                                                                        {
                                                                            actionType: "custom",
                                                                            script: "saveAnswer(event.data.__super.item.id, event.data.value);"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: "divider",
                                                    hidden: "${index === questions.length - 1}"
                                                }
                                            ]
                                        }
                                    }
                                }
                            },
                            {
                                type: 'divider'
                            },
                            {
                                type: 'flex',
                                justify: 'center',
                                className: 'practice-actions',
                                items: [
                                    {
                                        type: 'button',
                                        label: '取消练习',
                                        level: 'default',
                                        size: 'lg',
                                        confirmTitle: '确认取消',
                                        confirmText: '确定要放弃本次练习吗？',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'custom',
                                                        script: 'cancelPractice();',
                                                    }
                                                ]
                                            }
                                        }
                                    },
                                    {
                                        type: 'button',
                                        label: '提交练习',
                                        level: 'primary',
                                        size: 'lg',
                                        confirmTitle: '确认提交',
                                        confirmText: '确定要提交练习吗？提交后将无法修改答案。',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'custom',
                                                        script: 'submitPractice();',
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
        ]
    };

    // 注册全局方法
    window.submitPractice = submitPractice;
    window.cancelPractice = cancelPractice;
    window.saveAnswer = saveAnswer;
    window.initializePracticeData = initializePracticeData;
    
    // 初始化amis
    let amisInstance = amis.embed(
        '#root',
        practicePage,
        {
            location: history.location,
            data: {
                timer: {
                    displayText: '00:00:00',
                    hours: 0,
                    minutes: 0,
                    seconds: 0,
                    elapsed: 0
                },
                name: '',
                questions: [] // 初始化为空数组
            },
            locale: 'zh-CN',
            context: {
                WEB_HOST: webHost || ''
            }
        },
        {
            // 添加AMIS组件挂载事件处理
            mountRenderer: (renderer) => {
                console.log("AMIS渲染器已挂载");
                window.amisReady = true;
            },
            updateLocation: (location) => {
                history.push(location);
            },
            jumpTo: (to) => {
                history.push(to);
            },
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
                    alert('您没有权限进行此练习！');
                    window.location.href = "/client/practice";
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

                        // 同时注入到amis全局上下文
                        window.GlobalData.syncToAmis(amisInstance);
                    }
                }

                return payload;
            },
            theme: 'antd'
        }
    );

    // 设置全局amis实例
    window.amisInstance = amisInstance;
    
    // DOM加载完成后执行
    window.addEventListener('DOMContentLoaded', function() {
        // 设置顶部栏紧凑化效果
        setupHeaderScroll();
        
        // 计算并设置固定头部高度
        setTimeout(updateFixedHeaderHeight, 500);
        
        // 窗口大小改变时重新计算
        window.addEventListener('resize', updateFixedHeaderHeight);
    });
})(); 