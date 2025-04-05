/**
 * 在线考试系统客户端主模块
 * 负责考试页面渲染、答题、计时和提交功能
 * @module ExamClient
 */
(function () {
    'use strict';

    // 引入依赖模块
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 获取考试ID
    const examId = window.location.pathname.split('/').pop();
    
    // 调试模式配置
    window.enableAMISDebug = false; // 生产环境应设为false
    
    // 常量定义
    const CONSTANTS = {
        TIMER_UPDATE_INTERVAL: 1000,       // 计时器更新频率(毫秒)
        SUBMIT_REDIRECT_DELAY: 1500,       // 提交后跳转延迟(毫秒)
        MAX_RETRY_COUNT: 3,                // 最大重试次数
        RETRY_DELAY: 2000,                 // 重试延迟基础时间(毫秒)
        AUTO_SUBMIT_DELAY: 3000,           // 自动提交延迟(毫秒)
        COUNTDOWN_THRESHOLDS: {            // 倒计时阈值(秒)
            WARNING: 1800,                 // 警告阈值(30分钟)
            URGENT: 300,                   // 紧急阈值(5分钟)
            EXTREMELY_URGENT: 60           // 极度紧急阈值(1分钟)
        },
        TIMER_TRIGGER_POINTS: [300, 240, 180, 120, 60, 30, 10] // 特殊倒计时提醒点(秒)
    };
    
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
            recordId: null,
            screenSwitchCount: 0,         // 切屏次数属性
            allowedScreenSwitchCount: 0    // 允许切屏次数属性
        },
        timer: {
            displayText: '加载中...',
            hours: 0,
            minutes: 0,
            seconds: 0,
            remainingSeconds: 0
        }
    };

    // 私有状态变量
    let examTimerInterval = null;    // 计时器间隔ID
    let remainingTime = 0;           // 剩余时间(秒)
    let examAnswers = [];            // 答案集合
    let recordId = null;             // 考试记录ID
    let isSubmitting = false;        // 是否正在提交
    
    // AMIS实例引用
    window.amisInstance = null;

    /**
     * 全局数据管理工具
     * @namespace GlobalData
     */
    window.GlobalData = {
        /**
         * 获取指定路径的数据
         * @param {string} path - 数据路径，格式为"a.b.c"
         * @param {*} defaultValue - 数据不存在时的默认值
         * @returns {*} 获取的数据或默认值
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
         * @param {string} path - 数据路径，格式为"a.b.c"
         * @param {*} value - 要设置的值
         * @returns {*} 设置的值
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
         * @param {object} amisInstance - amis实例
         * @param {string[]} [selectedPaths] - 要同步的路径列表，不指定则同步全部数据
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
                
                // 可选：触发重新渲染
                if (typeof amisInstance.forceUpdate === 'function') {
                    amisInstance.forceUpdate();
                }
            } catch (error) {
                console.error('[GlobalData] 同步数据到AMIS失败:', error);
            }
        },
        
        /**
         * 更新单个字段并同步到AMIS
         * @param {string} path - 数据路径
         * @param {*} value - 要设置的值
         * @param {boolean} [syncToAmis=true] - 是否同步到AMIS
         */
        update: function(path, value, syncToAmis = true) {
            this.set(path, value);
            
            if (syncToAmis && window.amisInstance) {
                this.syncToAmis(window.amisInstance, [path]);
            }
        }
    };
    
    /**
     * 计时器模块，管理考试计时逻辑
     * @namespace ExamTimer
     */
    const ExamTimer = {
        /**
         * 启动考试计时器
         * @param {number} duration - 考试时长(分钟)
         * @param {string|Date} startTime - 考试开始时间
         */
        start: function(duration, startTime) {
            console.log("[计时器] 开始启动计时器", { duration, startTime });
            
            if (!duration || !startTime) {
                console.error("[计时器] 参数无效", { duration, startTime });
                return;
            }
            
            try {
                // 解析开始时间
                let examStartTime = new Date(startTime);
                
                // 验证开始时间是否有效
                if (isNaN(examStartTime.getTime())) {
                    console.error("[计时器] 无效的开始时间格式", startTime);
                    // 使用当前时间作为备用
                    examStartTime = new Date();
                    console.log("[计时器] 使用当前时间作为备用", examStartTime);
                }
                
                // 计算考试结束时间
                const examEndTime = new Date(examStartTime.getTime() + duration * 60 * 1000);
                console.log("[计时器] 计算出的结束时间", examEndTime);
                
                // 计算剩余时间(秒)
                const currentTime = new Date();
                let secondsRemaining = Math.floor((examEndTime.getTime() - currentTime.getTime()) / 1000);
                
                // 考试已结束的情况
                if (secondsRemaining <= 0) {
                    console.log("[计时器] 考试时间已结束或即将结束");
                    remainingTime = 0;
                    
                    // 更新显示
                    this.updateDisplay();
                    
                    // 延迟后自动提交考试
                    setTimeout(() => {
                        console.log("[计时器] 考试时间已结束，准备自动提交");
                        
                        // 显示警告提示
                        if (typeof window.showScreenSwitchWarning === 'function') {
                            window.showScreenSwitchWarning("考试时间已结束，系统将自动提交您的答卷!");
                        }
                        
                        // 延迟后自动提交
                        setTimeout(() => {
                            if (typeof window.submitExam === 'function') {
                                window.submitExam(true); // 自动提交
                            }
                        }, CONSTANTS.AUTO_SUBMIT_DELAY);
                    }, 500);
                    
                    return;
                }
                
                // 设置剩余时间
                remainingTime = secondsRemaining;
                console.log("[计时器] 设置剩余时间(秒)", remainingTime);
                
                // 清除之前的计时器
                if (examTimerInterval) {
                    clearInterval(examTimerInterval);
                    console.log("[计时器] 清除之前的计时器");
                }
                
                // 更新计时器显示
                this.updateDisplay();
                
                // 启动计时器
                console.log("[计时器] 正在启动计时器间隔");
                examTimerInterval = setInterval(() => {
                    remainingTime--;
                    
                    if (remainingTime <= 0) {
                        console.log("[计时器] 考试时间结束，准备自动提交");
                        clearInterval(examTimerInterval);
                        
                        if (typeof window.submitExam === 'function') {
                            window.submitExam(true); // 自动提交
                        }
                        return;
                    }
                    
                    this.updateDisplay();
                }, CONSTANTS.TIMER_UPDATE_INTERVAL);
                
                console.log("[计时器] 计时器已成功启动", examTimerInterval);
            } catch (error) {
                console.error("[计时器] 启动过程中出错", error);
            }
        },
        
        /**
         * 更新计时器显示
         */
        updateDisplay: function() {
            try {
                // 防御性编程：确保remainingTime是有效值
                if (isNaN(remainingTime) || remainingTime === undefined) {
                    console.warn("[计时器] remainingTime无效:", remainingTime);
                    remainingTime = 0;
                }
                
                // 确保remainingTime不为负数
                remainingTime = Math.max(0, remainingTime);
                
                // 计算时分秒
                const hours = Math.floor(remainingTime / 3600);
                const minutes = Math.floor((remainingTime % 3600) / 60);
                const seconds = remainingTime % 60;
                
                // 格式化显示文本
                const displayText = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
                
                // 更新全局数据
                window.GlobalData.set('timer.displayText', displayText);
                window.GlobalData.set('timer.hours', hours);
                window.GlobalData.set('timer.minutes', minutes);
                window.GlobalData.set('timer.seconds', seconds);
                window.GlobalData.set('timer.remainingSeconds', remainingTime);
        
                // 检测是否在特殊时间段内，设置相应样式类
                let timerClassName = "exam-timer";
                
                // 根据剩余时间设置不同的样式
                if (remainingTime <= CONSTANTS.COUNTDOWN_THRESHOLDS.URGENT) {
                    timerClassName += " countdown-urgent countdown-final";
                    this.handleFinalCountdown(remainingTime);
                } else if (remainingTime <= CONSTANTS.COUNTDOWN_THRESHOLDS.WARNING) {
                    timerClassName += " countdown-warn";
                }
                
                // 更新DOM显示
                this.updateDOMDisplay(displayText, timerClassName);
                
                // 同步到amis上下文
                if (window.amisInstance && window.amisInstance.updateProps) {
                    try {
                        window.amisInstance.updateProps({
                            data: {
                                timer: {
                                    displayText: displayText,
                                    hours: hours,
                                    minutes: minutes,
                                    seconds: seconds,
                                    remainingSeconds: remainingTime
                                }
                            }
                        });
                    } catch (e) {
                        console.error("[计时器] 更新AMIS显示时出错", e);
                    }
                }
            } catch (error) {
                console.error("[计时器] 更新显示时发生错误:", error);
                
                // 尝试最基本的DOM更新作为备用
                try {
                    this.updateDOMDisplay("00:00:00", "exam-timer");
                } catch (e) {
                    console.error("[计时器] 更新DOM时发生致命错误:", e);
                }
            }
        },
        
        /**
         * 更新DOM显示
         * @param {string} displayText - 显示文本
         * @param {string} className - CSS类名
         * @private
         */
        updateDOMDisplay: function(displayText, className) {
            const timerElements = document.querySelectorAll('.exam-timer');
            if (timerElements && timerElements.length > 0) {
                timerElements.forEach(el => {
                    el.innerHTML = `剩余时间：${displayText}`;
                    el.className = className || "exam-timer";
                });
            }
        },
        
        /**
         * 处理最后倒计时特效
         * @param {number} remainingSeconds - 剩余秒数
         * @private
         */
        handleFinalCountdown: function(remainingSeconds) {
            try {
                // 标记整个页面进入最后倒计时状态
                document.body.classList.add('final-countdown-active');
                
                // 更新计时器容器的样式
                const timerContainer = document.querySelector('.exam-timer-container');
                if (timerContainer) {
                    timerContainer.classList.add('final-countdown');
                    
                    // 移除之前的紧急程度类
                    timerContainer.classList.remove('extremely-urgent', 'very-urgent', 'urgent');
                    
                    // 根据剩余时间设置不同程度的紧急样式
                    if (remainingSeconds <= CONSTANTS.COUNTDOWN_THRESHOLDS.EXTREMELY_URGENT) {
                        timerContainer.classList.add('extremely-urgent');
                    } else if (remainingSeconds <= 180) { // 3分钟
                        timerContainer.classList.add('very-urgent');
                    } else {
                        timerContainer.classList.add('urgent');
                    }
                }
                
                // 检查是否需要触发特殊提示
                if (CONSTANTS.TIMER_TRIGGER_POINTS.includes(remainingSeconds)) {
                    this.showCountdownAlert(remainingSeconds);
                }
            } catch (error) {
                console.error("[计时器] 处理倒计时特效时出错:", error);
            }
        },
        
        /**
         * 显示倒计时警告
         * @param {number} remainingSeconds - 剩余秒数
         * @private
         */
        showCountdownAlert: function(remainingSeconds) {
            let message = '';
            let duration = 3000; // 默认显示3秒
            
            // 根据不同的时间节点设置不同的消息
            switch(remainingSeconds) {
                case 300: // 5分钟
                    message = '注意：考试仅剩最后5分钟！';
                    duration = 5000;
                    break;
                case 240: // 4分钟
                    message = '考试即将结束，请检查您的答案！';
                    break;
                case 180: // 3分钟
                    message = '仅剩3分钟，请加快完成！';
                    break;
                case 120: // 2分钟
                    message = '仅剩2分钟，请准备提交！';
                    duration = 4000;
                    break;
                case 60: // 1分钟
                    message = '最后1分钟！请确保保存所有答案！';
                    duration = 5000;
                    break;
                case 30: // 30秒
                    message = '30秒！即将自动提交！';
                    break;
                case 10: // 10秒
                    message = '10秒！系统即将自动提交您的答卷！';
                    duration = 5000;
                    break;
                default:
                    return; // 不是特殊时间点，不显示提醒
            }
            
            // 创建并显示提示元素
            const alertElement = document.createElement('div');
            alertElement.className = 'final-countdown-alert';
            alertElement.innerHTML = `
                <div class="alert-content">
                    <div class="alert-icon"><i class="fa fa-clock-o"></i></div>
                    <div class="alert-message">${message}</div>
                    <div class="alert-timer">${Math.floor(remainingSeconds / 60)}:${(remainingSeconds % 60).toString().padStart(2, '0')}</div>
                </div>
            `;
            
            // 添加到页面
            document.body.appendChild(alertElement);
            
            // 添加动画类
            setTimeout(() => {
                alertElement.classList.add('show');
            }, 10);
            
            // 设置自动关闭
            setTimeout(() => {
                alertElement.classList.remove('show');
                alertElement.classList.add('hide');
                
                // 动画结束后移除元素
                setTimeout(() => {
                    if (document.body.contains(alertElement)) {
                        document.body.removeChild(alertElement);
                    }
                }, 500);
            }, duration);
        },
        
        /**
         * 清除计时器
         */
        clear: function() {
            if (examTimerInterval) {
                clearInterval(examTimerInterval);
                examTimerInterval = null;
                console.log("[计时器] 已清除");
            }
        }
    };

    // 答案状态
    // 无需重复声明，保持原有的private变量

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
            
            // 计算考试结束时间
            const examEndTimeByDuration = new Date(examStartTime.getTime() + duration * 60 * 1000);
            console.log("计算出的结束时间", examEndTimeByDuration);
            
            // 获取当前时间
            const currentTime = new Date();
            console.log("当前时间", currentTime);
            
            // 计算剩余时间（秒）
            let secondsRemaining = Math.floor((examEndTimeByDuration.getTime() - currentTime.getTime()) / 1000);
            
            // 如果剩余时间小于0，可能是考试已经结束
            if (secondsRemaining <= 0) {
                console.log("考试时间已结束或即将结束");
                secondsRemaining = 0;
                
                // 更新剩余时间为0
                remainingTime = 0;
                
                // 更新显示
                updateTimerDisplay();
                
                // 延迟一点时间后自动提交考试
                setTimeout(() => {
                    console.log("考试时间已结束，准备自动提交");
                    
                    // 显示警告提示
                    window.showScreenSwitchWarning("考试时间已结束，系统将自动提交您的答卷!");
                    
                    // 延迟3秒后自动提交，给用户一点时间看到提示
                    setTimeout(() => {
                        submitExam(true); // 自动提交
                    }, 3000);
                }, 500);
                
                return;
            }
            
            // 设置剩余时间
            remainingTime = secondsRemaining;
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
                //console.log("剩余时间", remainingTime);
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
        try {
            // 防御性编程：确保remainingTime是有效值
            if (isNaN(remainingTime) || remainingTime === undefined) {
                console.warn("更新计时器时发现remainingTime无效:", remainingTime);
                remainingTime = 0;
            }
            
            // 确保remainingTime不为负数
            remainingTime = Math.max(0, remainingTime);
            
            const hours = Math.floor(remainingTime / 3600);
            const minutes = Math.floor((remainingTime % 3600) / 60);
            const seconds = remainingTime % 60;
            
            const displayText = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            //console.log("[计时器] 更新显示:", displayText, "剩余秒数:", remainingTime);
            
            // 更新全局数据
            window.GlobalData.set('timer.displayText', displayText);
            window.GlobalData.set('timer.hours', hours);
            window.GlobalData.set('timer.minutes', minutes);
            window.GlobalData.set('timer.seconds', seconds);
            window.GlobalData.set('timer.remainingSeconds', remainingTime);
    
            // 检测是否在特殊时间段内，设置相应样式类
            let timerClassName = "exam-timer";
            
            if (remainingTime <= 300) { // 5分钟内
                timerClassName += " countdown-urgent countdown-final";
                
                // 处理最后5分钟的特效
                handleFinalCountdown(remainingTime);
            } else if (remainingTime <= 1800) { // 30分钟内
                timerClassName += " countdown-warn";
            }
            
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
                    updateTimerDOMDisplay(displayText, timerClassName);
                } catch (e) {
                    console.error("更新计时器显示时出错", e);
                    // 出错时直接更新DOM作为后备方案
                    updateTimerDOMDisplay(displayText, timerClassName);
                }
            } else {
                console.warn("amisInstance未初始化，使用DOM方式更新计时器显示");
                // 尝试直接更新DOM
                updateTimerDOMDisplay(displayText, timerClassName);
            }
        } catch (error) {
            console.error("更新计时器显示时发生错误:", error);
            // 尝试最基本的DOM更新
            try {
                const displayText = "00:00:00";
                updateTimerDOMDisplay(displayText, "exam-timer");
            } catch (e) {
                console.error("更新计时器DOM时发生致命错误:", e);
            }
        }
    }
    
    // 处理最后5分钟倒计时的特效
    function handleFinalCountdown(remainingSeconds) {
        try {
            // 标记整个页面进入最后倒计时状态
            document.body.classList.add('final-countdown-active');
            
            // 更新计时器容器的样式
            const timerContainer = document.querySelector('.exam-timer-container');
            if (timerContainer) {
                timerContainer.classList.add('final-countdown');
                
                // 根据剩余时间设置不同程度的紧急样式
                if (remainingSeconds <= 60) { // 最后1分钟
                    timerContainer.classList.add('extremely-urgent');
                } else if (remainingSeconds <= 180) { // 最后3分钟
                    timerContainer.classList.add('very-urgent');
                } else {
                    timerContainer.classList.add('urgent');
                }
            }
            
            // 特殊事件触发时机
            const triggerPoints = [300, 240, 180, 120, 60, 30, 10];
            
            // 检查是否需要触发提示
            if (triggerPoints.includes(remainingSeconds)) {
                showFinalCountdownAlert(remainingSeconds);
            }
            
        } catch (error) {
            console.error("[最后倒计时] 处理特效时出错:", error);
        }
    }
    
    // 辅助函数：更新计时器DOM显示
    function updateTimerDOMDisplay(displayText, className) {
        const timerElements = document.querySelectorAll('.exam-timer');
        if (timerElements && timerElements.length > 0) {
            timerElements.forEach(el => {
                el.innerHTML = `剩余时间：${displayText}`;
                el.className = className || "exam-timer"; // 应用适当的类名
            });
        }
    }
    
    // 修改保存答案函数的部分代码
    function saveAnswer(questionId, answer) {
        try {
            console.log(`[保存答案] 开始保存题目 ${questionId} 的答案:`, answer);
            
            // 获取考试记录ID
            const recordId = window.globalData.exam.recordId;
            
            if (!recordId) {
                console.error('[保存答案] 错误：未找到考试记录ID');
                return;
            }
            
            // 参数验证
            if (!questionId) {
                console.error('[保存答案] 错误：questionId 不能为空');
                return;
            }
            
            // 确保examAnswers是数组
            if (!Array.isArray(examAnswers)) {
                console.warn('[保存答案] examAnswers不是数组，正在初始化');
                examAnswers = [];
            }
            
            // 确保答案格式正确
            let processedAnswer = answer;
            
            // 处理 null 或 undefined 答案
            if (answer === null || answer === undefined) {
                console.warn(`[保存答案] 题目 ${questionId} 的答案为空，将设置为空字符串`);
                processedAnswer = '';
            }
            
            // 如果不是字符串或数组，则转换为字符串
            if (typeof answer !== 'string' && !Array.isArray(answer)) {
                console.log(`[保存答案] 题目 ${questionId} 的答案类型为 ${typeof answer}，转换为字符串`);
                processedAnswer = String(answer);
            }
            
            // 如果是数组但包含非字符串元素，规范化数组
            if (Array.isArray(processedAnswer)) {
                console.log(`[保存答案] 处理题目 ${questionId} 的数组答案`);
                processedAnswer = processedAnswer.map(item => {
                    if (item === null || item === undefined) {
                        return '';
                    }
                    if (typeof item !== 'string') {
                        return String(item);
                    }
                    return item;
                });
            }
            
            // 查找已有答案
            const existingIndex = examAnswers.findIndex(a => a.questionId === questionId);
            console.log(`[保存答案] 查找结果：${existingIndex >= 0 ? '找到已有答案' : '未找到已有答案'}`);
            
            // 准备要保存的答案对象
            const answerObject = {
                questionId: questionId,
                answer: processedAnswer,
                timestamp: new Date().toISOString()
            };
            
            // 在保存前验证答案对象
            if (!answerObject.questionId || answerObject.answer === undefined) {
                console.error('[保存答案] 错误：答案对象无效', answerObject);
                return;
            }
            
            if (existingIndex >= 0) {
                // 更新已有答案
                console.log(`[保存答案] 更新题目 ${questionId} 的已有答案`);
                examAnswers[existingIndex] = answerObject;
            } else {
                // 添加新答案
                console.log(`[保存答案] 添加题目 ${questionId} 的新答案`);
                examAnswers.push(answerObject);
            }
            
            // 验证更新后的答案列表
            console.log(`[保存答案] 更新后的答案列表长度: ${examAnswers.length}`);
            
            // 向服务器提交当前答案
            sendAnswerToServer(recordId, questionId, processedAnswer);
            
            console.log(`[保存答案] 题目 ${questionId} 的答案保存完成`);
            
        } catch (error) {
            console.error('[保存答案] 保存过程中发生错误：', error);
        }
    }

    // 添加向服务器提交单个答案的函数
    function sendAnswerToServer(recordId, questionId, answer, retryCount = 0) {
        const maxRetries = 3;
        const retryDelay = 2000; // 2秒后重试
        
        // 跟踪未提交的答案（用于提交考试时的最终提交）
        if (!window.unsyncedAnswers) {
            window.unsyncedAnswers = new Set();
        }
        
        // 标记此答案为未同步
        window.unsyncedAnswers.add(questionId);
        
        // 更新同步状态显示
        updateSyncStatus();
        
        console.log(`[提交答案] 向服务器提交题目 ${questionId} 的答案，尝试次数: ${retryCount + 1}`);
        
        // 确保questionId作为字符串提交，避免精度丢失
        const answerDto = {
            questionId: String(questionId), // 使用String()确保是字符串
            answer: answer
        };
        
        fetch(`/exam/api/exam/client/${recordId}/save-answer`, {
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
                console.log(`[提交答案] 题目 ${questionId} 的答案已成功提交到服务器`);
                // 从未同步列表中移除
                window.unsyncedAnswers.delete(questionId);
                
                // 添加到已同步列表，用于跟踪
                if (!window.syncedAnswers) {
                    window.syncedAnswers = new Set();
                }
                window.syncedAnswers.add(questionId);
                
                // 更新同步状态显示
                updateSyncStatus();
            } else {
                console.error(`[提交答案] 服务器返回错误: ${data.msg || '未知错误'}`);
                // 如果服务器返回了明确的错误，可以考虑重试
                if (retryCount < maxRetries) {
                    setTimeout(() => {
                        sendAnswerToServer(recordId, questionId, answer, retryCount + 1);
                    }, retryDelay * (retryCount + 1));
                }
            }
        })
        .catch(error => {
            console.error(`[提交答案] 向服务器提交题目 ${questionId} 的答案失败:`, error);
            
            // 如果还有重试次数，则延迟后重试
            if (retryCount < maxRetries) {
                console.log(`[提交答案] 将在 ${retryDelay * (retryCount + 1) / 1000} 秒后重试...`);
                setTimeout(() => {
                    sendAnswerToServer(recordId, questionId, answer, retryCount + 1);
                }, retryDelay * (retryCount + 1));
            } else {
                console.error(`[提交答案] 已达到最大重试次数 (${maxRetries})，题目 ${questionId} 的答案将在提交试卷时一并提交`);
                
                // 标记为需要在最终提交时同步
                if (!window.failedSyncAnswers) {
                    window.failedSyncAnswers = new Map();
                }
                window.failedSyncAnswers.set(questionId, answer);
                
                // 更新同步状态显示
                updateSyncStatus();
            }
        });
    }

    // 添加更新同步状态显示的函数
    function updateSyncStatus() {
        try {
            // 计算未同步的答案数量
            const unsyncedCount = window.unsyncedAnswers ? window.unsyncedAnswers.size : 0;
            const failedCount = window.failedSyncAnswers ? window.failedSyncAnswers.size : 0;
            const totalUnsyncedCount = unsyncedCount + failedCount;
            
            // 获取状态显示元素
            const statusElements = document.querySelectorAll('.sync-status-value');
            const iconElements = document.querySelectorAll('.sync-status-icon');
            
            if (statusElements && statusElements.length > 0) {
                // 设置适当的状态文本和样式
                if (totalUnsyncedCount === 0) {
                    // 全部已同步
                    statusElements.forEach(el => {
                        el.textContent = "已同步";
                        el.className = "sync-status-value synced";
                    });
                    
                    iconElements.forEach(el => {
                        el.className = "sync-status-icon synced";
                        el.setAttribute('title', '所有答案已同步到服务器');
                    });
                } else {
                    // 有未同步的答案
                    const text = `同步中 (${totalUnsyncedCount})`;
                    statusElements.forEach(el => {
                        el.textContent = text;
                        el.className = "sync-status-value syncing";
                    });
                    
                    iconElements.forEach(el => {
                        el.className = "sync-status-icon syncing";
                        el.setAttribute('title', `有 ${totalUnsyncedCount} 个答案正在同步到服务器`);
                    });
                    
                    // 如果有同步失败的答案，特殊处理
                    if (failedCount > 0) {
                        statusElements.forEach(el => {
                            el.className = "sync-status-value sync-failed";
                        });
                        
                        iconElements.forEach(el => {
                            el.className = "sync-status-icon sync-failed";
                            el.setAttribute('title', `有 ${failedCount} 个答案同步失败，将在提交时重试`);
                        });
                    }
                }
            }
        } catch (error) {
            console.error('[同步状态] 更新同步状态显示时出错：', error);
        }
    }

    // 提交考试
    function submitExam(isAutoSubmit = false) {
        if (isAutoSubmit) {
            window.showScreenSwitchWarning("考试时间已结束，系统将自动提交您的答卷！");
        }
        
        // 在提交前重新计算一次高度，确保页面布局正确
        if (typeof updateFixedHeaderHeight === 'function') {
            updateFixedHeaderHeight();
        }
        
        // 从全局数据获取recordId
        const recordId = window.globalData.exam.recordId;
        
        // 检查recordId是否有效
        if (!recordId) {
            console.error("提交失败: recordId为空");
            if (!isAutoSubmit) {
                alert("提交失败：无法获取考试记录ID，请刷新页面重试");
            }
            return;
        }
        
        // 显示提交中的加载提示
        showSubmittingNotification();
        
        // 首先检查是否有未同步到服务器的答案
        let unsyncedAnswersExist = window.unsyncedAnswers && window.unsyncedAnswers.size > 0;
        let failedSyncAnswersExist = window.failedSyncAnswers && window.failedSyncAnswers.size > 0;
        
        if (unsyncedAnswersExist || failedSyncAnswersExist) {
            console.log('[提交考试] 检测到未同步的答案，先进行同步...');
            
            // 收集所有需要同步的答案
            const answersToSync = [];
            
            // 添加未同步的答案
            if (unsyncedAnswersExist) {
                console.log(`[提交考试] 发现 ${window.unsyncedAnswers.size} 个未同步的答案`);
                
                // 从examAnswers中找出对应的答案
                window.unsyncedAnswers.forEach(questionId => {
                    const answer = examAnswers.find(a => a.questionId === questionId);
                    if (answer) {
                        answersToSync.push({
                            questionId: String(answer.questionId), // 使用String()确保是字符串
                            answer: answer.answer
                        });
                    }
                });
            }
            
            // 添加同步失败的答案
            if (failedSyncAnswersExist) {
                console.log(`[提交考试] 发现 ${window.failedSyncAnswers.size} 个同步失败的答案`);
                
                window.failedSyncAnswers.forEach((answer, questionId) => {
                    answersToSync.push({
                        questionId: String(questionId), // 使用String()确保是字符串
                        answer: answer
                    });
                });
            }
            
            console.log('[提交考试] 正在同步答案...', answersToSync);
            
            // 使用批量保存接口一次性提交所有未同步的答案
            fetch(`/exam/api/exam/client/${recordId}/save-answers`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + localStorage.getItem('token'),
                    'X-Forwarded-With': 'CodeSpirit'
                },
                body: JSON.stringify(answersToSync)
            })
            .then(response => response.json())
            .then(data => {
                if (data.status === 0) {
                    console.log('[提交考试] 所有未同步答案已成功提交');
                    // 清空未同步记录
                    window.unsyncedAnswers = new Set();
                    window.failedSyncAnswers = new Map();
                    // 更新同步状态显示
                    updateSyncStatus();
                    // 继续提交考试
                    proceedWithExamSubmission(recordId, isAutoSubmit);
                } else {
                    console.error('[提交考试] 批量同步答案失败:', data.msg);
                    // 尽管同步失败，仍然继续提交考试，确保不影响用户
                    proceedWithExamSubmission(recordId, isAutoSubmit);
                }
            })
            .catch(error => {
                console.error('[提交考试] 批量同步答案发生错误:', error);
                // 即使发生错误也继续提交考试
                proceedWithExamSubmission(recordId, isAutoSubmit);
            });
        } else {
            // 没有未同步的答案，直接提交考试
            console.log('[提交考试] 所有答案已同步，直接提交考试');
            proceedWithExamSubmission(recordId, isAutoSubmit);
        }
    }
    
    // 显示提交中的通知
    function showSubmittingNotification() {
        // 使用自定义通知或浏览器原生API
        if (typeof createCustomNotification === 'function') {
            createCustomNotification('提交中', '正在提交您的答案，请勿关闭页面...', 'info', 0);
        } else {
            // 如果自定义通知不可用，只在控制台记录
            console.log('[提交考试] 正在提交答案，请勿关闭页面...');
        }
    }
    
    // 实际执行提交考试的逻辑
    function proceedWithExamSubmission(recordId, isAutoSubmit) {
        console.log('[提交考试] 开始最终提交...');
        
        // 转换为后端需要的格式，确保questionId是字符串类型
        const answers = examAnswers.map(a => {
            return {
                questionId: String(a.questionId), // 使用String()确保是字符串
                answer: a.answer
            };
        });
        
        console.debug('[提交考试] 最终提交的答案数据:', answers);

        // 提示用户提交正在进行中
        if (typeof createCustomNotification === 'function') {
            createCustomNotification('提交中', '正在提交考试，请不要关闭页面...', 'info', 30000);
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
                
                // 显示成功提示
                if (typeof createCustomNotification === 'function') {
                    createCustomNotification('提交成功', '您的考试已成功提交！', 'success', 3000);
                }
                
                // 根据后端返回的enableViewResult决定是否跳转到结果页面
                if (data.data && data.data.enableViewResult) {
                    // 允许查看结果，跳转到结果页面
                    setTimeout(() => {
                        window.location.href = `/client/exam/result/${recordId}`;
                    }, 1500);
                } else {
                    // 不允许查看结果，显示提交成功页面
                    setTimeout(() => {
                        window.location.href = "/client/index";
                    }, 1500);
                }
            } else {
                // 显示错误提示
                if (typeof createCustomNotification === 'function') {
                    createCustomNotification('提交失败', data.msg || "提交失败，请重试", 'error', 5000);
                } else {
                    alert(data.msg || "提交失败，请重试");
                }
            }
        })
        .catch(error => {
            console.error("[提交考试] 提交考试失败", error);
            
            // 显示错误提示
            if (typeof createCustomNotification === 'function') {
                createCustomNotification('提交失败', "网络错误，请检查网络连接后重试", 'error', 5000);
            } else {
                alert("提交失败，请检查网络连接后重试");
            }
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
        // title: window.siteSettings ? window.siteSettings.clientAppName : '考试系统',
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
                                        tpl: '<div class="logo"><img src="' + (window.siteSettings ? window.siteSettings.logoUrl : '/logo.png') + '" /><span>' + (window.siteSettings ? window.siteSettings.clientAppName : '考试系统') +'</span></div>',
                                        className: 'client-logo'
                                    },
                                    {
                                        type: 'flex',
                                        justify: 'flex-end',
                                        alignItems: 'center',
                                        className: 'user-info-container',
                                        items: [
                                            //{
                                            //    type: 'tpl',
                                            //    tpl: '<div class="user-info">欢迎您，${name}</div>'
                                            //},
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="sync-status-icon" title="答案同步状态"><i class="fa fa-cloud-upload"></i> <span class="sync-status-value">已同步</span></div>'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="screen-switch-icon" title="当前切屏次数/允许次数"><i class="fa fa-desktop"></i> <span class="screen-switch-value">0</span>/<span class="allowed-switch-value">${allowedScreenSwitchCount || "?"}</span></div>'
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
                    // 添加考生信息组件
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
                                type: 'card',
                                className: 'student-info-card',
                                bodyClassName: 'student-info-body',
                                body: [
                                    {
                                        type: 'flex',
                                        justify: 'space-between',
                                        alignItems: 'center',
                                        items: [
                                            {
                                                type: 'tpl',
                                                tpl: '<div><i class="fa fa-user"></i> <span class="text-muted">考生：</span><strong>${name}</strong></div>',
                                                className: 'student-info-item student-name'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '<div><i class="fa fa-id-card"></i> <span class="text-muted">身份证：</span>${idNo}</div>',
                                                className: 'student-info-item'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '<div><i class="fa fa-venus-mars"></i> <span class="text-muted">性别：</span>${gender}</div>',
                                                className: 'student-info-item'
                                            },
                                            {
                                                type: 'tpl',
                                                tpl: '<div><i class="fa fa-ticket"></i> <span class="text-muted">准考证号：</span>${admissionTicket || "未设置"}</div>',
                                                className: 'student-info-item'
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
                    {
                        type: 'flex',
                        justify: 'space-between',
                        className: 'w-full header-status-container',
                        items: [
                            {
                                type: 'flex',
                                justify: 'center',
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
                api: `/exam/api/exam/client/${examId}/basic`,
                className: 'exam-container',
                onEvent: {
                    fetchInited: {
                        actions: [
                            {
                                actionType: "custom",
                                script: `
                                    try {
                                        console.log("考试数据加载成功", event.data); 
                                        
                                        // 防御性检查：确保event和event.data存在
                                        if (!event || !event.data) {
                                            console.error("事件数据无效", event);
                                            return;
                                        }
                                        
                                        // 从API响应中获取记录ID
                                        recordId = event.data.recordId;
                                        
                                        // 更新全局考试数据
                                        window.globalData.exam.name = event.data.name || '';
                                        window.globalData.exam.duration = event.data.duration || 0;
                                        window.globalData.exam.startTime = event.data.startTime || null;
                                        window.globalData.exam.endTime = event.data.endTime || null;
                                        window.globalData.exam.totalScore = event.data.totalScore || 0;
                                        window.globalData.exam.recordId = event.data.recordId || null;
                                        
                                        // 更新切屏相关数据
                                        window.globalData.exam.screenSwitchCount = event.data.screenSwitchCount || 0;
                                        window.globalData.exam.allowedScreenSwitchCount = event.data.allowedScreenSwitchCount || 0;
                                        
                                        // 从API同步切屏次数到全局变量
                                        window.screenSwitchCount = event.data.screenSwitchCount || 0;
                                        
                                        // 更新UI显示
                                        const switchCountElements = document.querySelectorAll('.screen-switch-value');
                                        if (switchCountElements && switchCountElements.length > 0) {
                                            switchCountElements.forEach(el => {
                                                el.textContent = window.screenSwitchCount.toString();
                                            });
                                        }
                                        
                                        // 更新允许切屏次数显示
                                        const allowedSwitchElements = document.querySelectorAll('.allowed-switch-value');
                                        if (allowedSwitchElements && allowedSwitchElements.length > 0) {
                                            const allowedCount = event.data.allowedScreenSwitchCount || 0;
                                            allowedSwitchElements.forEach(el => {
                                                el.textContent = allowedCount.toString();
                                            });
                                        }
                                        
                                        // 初始化考试计时器
                                        initializeExamTimer(event.data);
                                        
                                        // 保存题目数据以便其他功能使用
                                        window.globalData.exam.questions = event.data.questions || [];
                                        
                                        console.log("成功初始化考试数据");
                                        // 打印题目数据，检查是否正确
                                        console.log("考试题目数据：", event.data.questions);
                                        
                                    } catch (error) {
                                        console.error("初始化考试数据时出错:", error);
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
                                        tpl: '总分：${totalScore} 分',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '题目数：${questions.length} 题',
                                        className: 'exam-info-item'
                                    },
                                    {
                                        type: 'tpl',
                                        tpl: '考试时长：${duration}分钟',
                                        className: 'exam-info-item'
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
                                    id: "examForm",
                                    actions: [],  // 隐藏表单自带的提交按钮
                                    body: {
                                        type: "each",
                                        name: "questions",
                                        items: {
                                            type: "container",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class=\"question-label\"><pre>${index + 1}. ${item.content} </pre><span style=\"color:#999\">（${item.score}分）</span></div>",
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
                                                                            script: "saveAnswer(event.data.__super.questionId, event.data.value);"
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
                                                                            script: "saveAnswer(event.data.__super.questionId, event.data.value);"
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
                                                                            script: "saveAnswer(event.data.__super.questionId, event.data.value);"
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
                                                                            script: "saveAnswer(event.data.__super.questionId, event.data.value);"
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
                                },
                                //onEvent: {
                                //    fetchInited: {
                                //        actions: [
                                //            {
                                //                actionType: "custom",
                                //                script: "console.log('题目数据：', event.data.questions);"
                                //            }
                                //        ]
                                //    }
                                //}
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
                                                        script: 'submitExam(false);',
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
            // 样式已经移动到外部 CSS 文件
        }
    };

    // 注册用于提交考试的全局方法
    window.submitExam = submitExam;
    window.cancelExam = cancelExam;
    window.startExamTimer = startExamTimer;
    window.updateTimerDisplay = updateTimerDisplay;
    window.saveAnswer = saveAnswer;
    window.initializeExamTimer = initializeExamTimer;

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
                },
                name: '',
                questions: window.globalData?.exam?.questions || []
            },
            locale: 'zh-CN',
            context: {
                API_HOST: apiHost || '',
                WEB_HOST: webHost || '',
                aspire_dashboard: aspire_dashboard || ''
            }
        },
        {
            // 添加AMIS组件挂载事件处理
            mountRenderer: (renderer) => {
                console.log("AMIS渲染器已挂载");
                // 标记AMIS实例已完全初始化
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
    
    // 确保AMIS实例初始化完成
    console.log("AMIS实例初始化完成，测试属性访问:", amisInstance.props ? "props已存在" : "props不存在");
    
    // 如果amisInstance.props不存在，进行初始化
    if (!amisInstance.props) {
        amisInstance.props = {
            data: {
                timer: {
                    displayText: '加载中...',
                    hours: 0, 
                    minutes: 0,
                    seconds: 0,
                    remainingSeconds: 0
                }
            }
        };
        console.log("已初始化AMIS实例的props属性");
    }

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });

    // 在考试页面加载完成后初始化答案
    document.addEventListener('DOMContentLoaded', function() {
        // console.log("[页面加载] 文档已加载完成");
        
        // 初始化同步状态显示
        updateSyncStatus();
        
        // 尝试初始化计时器
        if (window.globalData && window.globalData.exam) {
            const { startTime, duration } = window.globalData.exam;
            if (startTime && duration) {
                console.log("[页面加载] 从全局数据初始化计时器", { startTime, duration });
                // 这里不直接调用startExamTimer，因为API加载完成后会自动调用
            }
        } else {
            console.log("[页面加载] 全局考试数据尚未准备好，将等待API加载");
        }
        
        // 计算并设置固定头部高度
        setTimeout(updateFixedHeaderHeight, 500);
        // 窗口大小改变时重新计算
        window.addEventListener('resize', updateFixedHeaderHeight);
        
        // 设置滚动事件处理
        setupScrollHandler();
    });
    
    // 更新固定头部高度的函数
    function updateFixedHeaderHeight() {
        try {
            const fixedHeaderContainer = document.querySelector('.fixed-header-container');
            if (fixedHeaderContainer) {
                const height = fixedHeaderContainer.offsetHeight;
                document.documentElement.style.setProperty('--fixed-header-height', height + 'px');
                //console.log("[固定头部] 高度已更新:", height + 'px');
                
                // 更新spacer高度
                const spacer = document.querySelector('.fixed-header-spacer');
                if (spacer) {
                    spacer.style.height = (height + 10) + 'px';
                    //console.log("[固定头部] spacer高度已更新:", (height + 10) + 'px');
                }
            }
        } catch (error) {
            console.error("[固定头部] 更新高度时出错:", error);
        }
    }
    
    // 监听滚动事件，压缩头部
    function setupScrollHandler() {
        let lastScrollTop = 0;
        const scrollThreshold = 50; // 滚动超过50px时压缩头部
        
        window.addEventListener('scroll', function() {
            try {
                const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                const fixedHeader = document.querySelector('.fixed-header-container');
                
                if (fixedHeader) {
                    if (scrollTop > scrollThreshold) {
                        // 向下滚动超过阈值，添加压缩样式
                        fixedHeader.classList.add('compact-header');
                    } else {
                        // 滚动回顶部附近，恢复正常样式
                        fixedHeader.classList.remove('compact-header');
                    }
                    
                    // 滚动方向变化时，更新布局
                    if (Math.abs(scrollTop - lastScrollTop) > 10) {
                        setTimeout(updateFixedHeaderHeight, 150); // 在过渡完成后更新高度
                    }
                }
                
                lastScrollTop = scrollTop;
            } catch (error) {
                console.error("[滚动处理] 错误:", error);
            }
        }, { passive: true });
    }

    // 添加页面卸载时的保存机制
    window.addEventListener('beforeunload', function() {
        try {
            const userId = window.globalData.user.id;
            const examId = window.globalData.exam.id;
            if (userId && examId && Array.isArray(examAnswers) && examAnswers.length > 0) {
                const storageKey = `exam_${userId}_${examId}_answers`;
                localStorage.setItem(storageKey, JSON.stringify(examAnswers));
                console.log('[页面卸载] 已保存答案到本地存储');
            }
        } catch (error) {
            console.error('[页面卸载] 保存答案失败：', error);
        }
    });

    // 添加滚动监听逻辑，实现顶部栏的紧凑化
    function setupHeaderScroll() {
        // 获取头部容器
        const fixedHeaderContainer = document.querySelector('.fixed-header-container');
        if (!fixedHeaderContainer) {
            console.warn("[UI优化] 未找到固定头部容器，无法设置紧凑化效果");
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
        
        console.log("[UI优化] 设置顶部栏滚动紧凑化效果");
    }

    // 显示最后倒计时提醒
    function showFinalCountdownAlert(remainingSeconds) {
        let message = '';
        let duration = 3000; // 默认显示3秒
        
        // 根据不同的时间节点设置不同的消息
        if (remainingSeconds === 300) {
            message = '注意：考试仅剩最后5分钟！';
            duration = 5000;
        } else if (remainingSeconds === 240) {
            message = '考试即将结束，请检查您的答案！';
        } else if (remainingSeconds === 180) {
            message = '仅剩3分钟，请加快完成！';
        } else if (remainingSeconds === 120) {
            message = '仅剩2分钟，请准备提交！';
            duration = 4000;
        } else if (remainingSeconds === 60) {
            message = '最后1分钟！请确保保存所有答案！';
            duration = 5000;
        } else if (remainingSeconds === 30) {
            message = '30秒！即将自动提交！';
        } else if (remainingSeconds === 10) {
            message = '10秒！系统即将自动提交您的答卷！';
            duration = 5000;
        }
        
        // 创建并显示提示元素
        const alertElement = document.createElement('div');
        alertElement.className = 'final-countdown-alert';
        alertElement.innerHTML = `
            <div class="alert-content">
                <div class="alert-icon"><i class="fa fa-clock-o"></i></div>
                <div class="alert-message">${message}</div>
                <div class="alert-timer">${Math.floor(remainingSeconds / 60)}:${(remainingSeconds % 60).toString().padStart(2, '0')}</div>
            </div>
        `;
        
        // 添加到页面
        document.body.appendChild(alertElement);
        
        // 添加动画类
        setTimeout(() => {
            alertElement.classList.add('show');
        }, 10);
        
        // 设置自动关闭
        setTimeout(() => {
            alertElement.classList.remove('show');
            alertElement.classList.add('hide');
            
            // 动画结束后移除元素
            setTimeout(() => {
                if (document.body.contains(alertElement)) {
                    document.body.removeChild(alertElement);
                }
            }, 500);
        }, duration);
    }

    // 启动紧凑化顶部栏功能
    window.addEventListener('DOMContentLoaded', function() {
        setupHeaderScroll();
    });

    // 修改onLoad回调函数，确保初始化相关功能
    window.onLoad = function() {
        console.log('[考试页] 初始化事件');
        
        // 初始化考试全局变量
        window.examAnswers = [];
        
        // 设置切屏检测
        if (typeof window.ScreenSwitchDetector !== 'undefined' && typeof window.ScreenSwitchDetector.setup === 'function') {
            window.ScreenSwitchDetector.setup();
        } else if (typeof window.setupScreenSwitchDetection === 'function') {
            // 向后兼容旧的全局函数
            window.setupScreenSwitchDetection();
        }
    };

    // 初始化考试计时器
    function initializeExamTimer(examData) {
        try {
            // 获取服务器返回的开始时间和考试时长
            const serverStartTime = examData.startTime;
            const examDuration = examData.duration;
            
            if (!serverStartTime) {
                console.error("服务器未返回有效的开始时间，无法启动计时器");
                return;
            }
            
            console.log("从服务器获取的考试信息:", {
                startTime: serverStartTime,
                duration: examDuration + "分钟"
            });
            
            // 调用计时器函数，传递考试时长和开始时间
            startExamTimer(examDuration, serverStartTime);
            
            // 在考试数据加载后初始化切屏检测（延迟执行确保数据已加载）
            setTimeout(function() {
                console.log("延迟执行切屏检测初始化");
                if (typeof window.ScreenSwitchDetector !== 'undefined' && typeof window.ScreenSwitchDetector.setup === 'function') {
                    window.ScreenSwitchDetector.setup();
                } else if (typeof window.setupScreenSwitchDetection === 'function') {
                    // 向后兼容旧的全局函数
                    window.setupScreenSwitchDetection();
                } else {
                    console.error("切屏检测函数未定义");
                }
            }, 2000);
            
            // 显示调试信息
            console.log("考试全局数据已更新:", {
                exam: window.globalData.exam,
                recordId: window.globalData.exam.recordId,
                screenSwitch: {
                    count: window.globalData.exam.screenSwitchCount,
                    allowed: window.globalData.exam.allowedScreenSwitchCount
                }
            });
        } catch (error) {
            console.error("初始化考试计时器失败", error);
        }
    }

    // 使函数全局可用，供AMIS调用
    window.initializeExamTimer = initializeExamTimer;
    window.saveAnswer = saveAnswer;
})(); 