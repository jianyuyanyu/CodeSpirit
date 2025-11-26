/**
 * 在线考试系统客户端主模块
 * 负责考试页面渲染、答题、计时和提交功能
 * @module ExamClient
 */
(function () {
    'use strict';

    // 确保TokenManager已初始化
    TokenManager.initClientMode(window.tenantId, 'exam');

    // 检查用户认证状态
    if (!TokenManager.isAuthenticated()) {
        window.location.href = `/${window.tenantId}/exam/login`;
        return;
    }

    // 引入依赖模块
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 获取考试ID
    const examId = window.examId;

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
        TIMER_TRIGGER_POINTS: [300, 240, 180, 120, 60, 30, 10], // 特殊倒计时提醒点(秒)
        PERFORMANCE_THRESHOLDS: {          // 性能警告阈值(毫秒)
            SAVE_ANSWER: 200,              // 保存答案函数
            TIMER_UPDATE: 50,              // 计时器更新
            INITIALIZATION: 1000,          // 初始化函数
            ANSWER_CARD_UPDATE: 100        // 答题卡更新
        }
    };

    /**
     * 性能监控管理器
     * 检测开发人员工具并提供性能统计功能
     */
    const PerformanceMonitor = {
        isDevToolsOpen: false,
        isEnabled: false,
        statistics: new Map(),
        
        /**
         * 检测开发人员工具是否打开
         */
        detectDevTools: function() {
            const threshold = 160;
            let devtools = false;
            
            // 方法1：通过控制台大小检测
            if (window.outerHeight - window.innerHeight > threshold || 
                window.outerWidth - window.innerWidth > threshold) {
                devtools = true;
                return devtools;
            }
            
            // 方法2：通过console对象检测
            const element = new Image();
            element.__defineGetter__('id', function() {
                devtools = true;
            });
            console.log(element);
            
            return devtools;
        },
        
        /**
         * 初始化性能监控
         */
        init: function() {
            // 检测开发人员工具
            this.isDevToolsOpen = this.detectDevTools();
            this.isEnabled = this.isDevToolsOpen;
            
            if (this.isEnabled) {
                console.log('%c[性能监控] 检测到开发人员工具，已启用性能监控', 'color: #4CAF50; font-weight: bold;');
                this.setupPerformanceTracking();
                
                // 定期重新检测开发者工具状态
                setInterval(() => {
                    const currentState = this.detectDevTools();
                    if (currentState !== this.isDevToolsOpen) {
                        this.isDevToolsOpen = currentState;
                        this.isEnabled = currentState;
                        
                        if (currentState) {
                            console.log('%c[性能监控] 开发人员工具已打开，启用性能监控', 'color: #4CAF50; font-weight: bold;');
                            this.setupPerformanceTracking();
                        } else {
                            console.log('%c[性能监控] 开发人员工具已关闭，禁用性能监控', 'color: #FF9800; font-weight: bold;');
                        }
                    }
                }, 2000);
            }
        },
        
        /**
         * 设置性能追踪
         */
        setupPerformanceTracking: function() {
            if (!this.isEnabled) return;
            
            // 记录页面加载性能
            if (performance.navigation) {
                const loadTime = performance.timing.loadEventEnd - performance.timing.navigationStart;
                console.log(`%c[性能监控] 页面加载时间: ${loadTime}ms`, 'color: #2196F3;');
            }
        },
        
        /**
         * 包装函数以进行性能监控
         * @param {Function} fn - 要监控的函数
         * @param {string} name - 函数名称
         * @param {number} threshold - 警告阈值(毫秒)
         * @returns {Function} 包装后的函数
         */
        wrapFunction: function(fn, name, threshold = 100) {
            if (!this.isEnabled) return fn;
            
            const self = this;
            return function(...args) {
                const startTime = performance.now();
                const result = fn.apply(this, args);
                const endTime = performance.now();
                const duration = endTime - startTime;
                
                // 记录统计信息
                self.recordStatistic(name, duration);
                
                // 检查是否超过阈值
                if (duration > threshold) {
                    console.warn(`%c[性能警告] ${name} 执行时间过长: ${duration.toFixed(2)}ms (阈值: ${threshold}ms)`, 
                        'color: #FF5722; font-weight: bold;');
                }
                
                return result;
            };
        },
        
        /**
         * 包装异步函数以进行性能监控
         * @param {Function} fn - 要监控的异步函数
         * @param {string} name - 函数名称
         * @param {number} threshold - 警告阈值(毫秒)
         * @returns {Function} 包装后的函数
         */
        wrapAsyncFunction: function(fn, name, threshold = 100) {
            if (!this.isEnabled) return fn;
            
            const self = this;
            return async function(...args) {
                const startTime = performance.now();
                try {
                    const result = await fn.apply(this, args);
                    const endTime = performance.now();
                    const duration = endTime - startTime;
                    
                    // 记录统计信息
                    self.recordStatistic(name, duration);
                    
                    // 检查是否超过阈值
                    if (duration > threshold) {
                        console.warn(`%c[性能警告] ${name} 异步执行时间过长: ${duration.toFixed(2)}ms (阈值: ${threshold}ms)`, 
                            'color: #FF5722; font-weight: bold;');
                    }
                    
                    return result;
                } catch (error) {
                    const endTime = performance.now();
                    const duration = endTime - startTime;
                    console.error(`%c[性能监控] ${name} 执行出错 (耗时: ${duration.toFixed(2)}ms):`, 'color: #F44336;', error);
                    throw error;
                }
            };
        },
        
        /**
         * 记录统计信息
         * @param {string} name - 函数名称
         * @param {number} duration - 执行时间
         */
        recordStatistic: function(name, duration) {
            if (!this.statistics.has(name)) {
                this.statistics.set(name, {
                    count: 0,
                    totalTime: 0,
                    minTime: Infinity,
                    maxTime: 0,
                    avgTime: 0
                });
            }
            
            const stat = this.statistics.get(name);
            stat.count++;
            stat.totalTime += duration;
            stat.minTime = Math.min(stat.minTime, duration);
            stat.maxTime = Math.max(stat.maxTime, duration);
            stat.avgTime = stat.totalTime / stat.count;
        },
        
        /**
         * 获取性能统计报告
         */
        getReport: function() {
            if (!this.isEnabled) {
                console.log('%c[性能监控] 性能监控未启用', 'color: #FF9800;');
                return;
            }
            
            console.group('%c[性能统计报告]', 'color: #4CAF50; font-weight: bold; font-size: 14px;');
            
            if (this.statistics.size === 0) {
                console.log('暂无性能数据');
            } else {
                const sortedStats = Array.from(this.statistics.entries())
                    .sort((a, b) => b[1].avgTime - a[1].avgTime);
                
                console.table(
                    sortedStats.reduce((acc, [name, stat]) => {
                        acc[name] = {
                            '调用次数': stat.count,
                            '总时间(ms)': stat.totalTime.toFixed(2),
                            '平均时间(ms)': stat.avgTime.toFixed(2),
                            '最小时间(ms)': stat.minTime.toFixed(2),
                            '最大时间(ms)': stat.maxTime.toFixed(2)
                        };
                        return acc;
                    }, {})
                );
            }
            
            console.groupEnd();
        },
        
        /**
         * 清空统计数据
         */
        clearStatistics: function() {
            this.statistics.clear();
            if (this.isEnabled) {
                console.log('%c[性能监控] 统计数据已清空', 'color: #2196F3;');
            }
        }
    };
    
    // 初始化性能监控
    PerformanceMonitor.init();
    
    // 将性能监控器暴露到全局，便于调试
    window.PerformanceMonitor = PerformanceMonitor;

    /**
     * 加载租户信息（使用缓存）
     * 参考application.js中的实现方式
     */
    async function loadTenantInfo() {
        try {
            // 使用缓存的登录配置接口
            const tenantConfig = await window.ExamApiManager.getLoginConfig(window.tenantId);
            
            // 转换为考试页面需要的格式
            const tenantInfo = {
                id: window.tenantId,
                name: tenantConfig.displayName || tenantConfig.name || '考试平台',
                logo: tenantConfig.logoUrl || '/logo.png',
                description: tenantConfig.description || ''
            };
            
            // 更新全局数据
            window.globalData.tenant = tenantInfo;
            
            // 同步到AMIS实例
            if (window.amisInstance) {
                window.GlobalData.syncToAmis(window.amisInstance, ['tenant']);
            }
            
            console.log('[租户信息] 加载成功:', tenantInfo);
            return tenantInfo;
        } catch (error) {
            console.warn('[租户信息] 加载失败:', error);
            const fallbackInfo = { 
                id: window.tenantId,
                name: '考试平台', 
                logo: '/logo.png',
                description: '' 
            };
            
            // 更新全局数据为默认值
            window.globalData.tenant = fallbackInfo;
            
            // 同步到AMIS实例
            if (window.amisInstance) {
                window.GlobalData.syncToAmis(window.amisInstance, ['tenant']);
            }
            
            return fallbackInfo;
        }
    }

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
            allowedScreenSwitchCount: 0,    // 允许切屏次数属性
            minExamTime:0 //最小考试时间
        },
        timer: {
            displayText: '加载中...',
            hours: 0,
            minutes: 0,
            seconds: 0,
            remainingSeconds: 0
        },
        tenant: {
            id: window.tenantId,
            name: '考试平台',
            logo: '/logo.png',
            description: ''
        }
    };

    // 私有状态变量
    let examTimerInterval = null;    // 计时器间隔ID
    let remainingTime = 0;           // 剩余时间(秒) - 仅用于显示和逻辑判断
    let examEndTimeMs = 0;           // 考试结束的精确时间戳(毫秒)
    window.examAnswers = [];         // 答案集合 - 改为全局变量
    let examAnswers = window.examAnswers; // 本地引用
    let recordId = null;             // 考试记录ID
    let isSubmitting = false;        // 是否正在提交
    
    // 性能优化相关变量
    let questionIdIndexMap = new Map();  // 题目ID索引映射，提升查找性能
    let answerSaveTimeouts = new Map();  // 答案保存防抖计时器
    let lastTimerUpdate = 0;             // 上次计时器更新时间，避免频繁DOM更新

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
        update: function (path, value, syncToAmis = true) {
            this.set(path, value);

            if (syncToAmis && window.amisInstance) {
                this.syncToAmis(window.amisInstance, [path]);
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

            // 计算基于考试时长的结束时间
            const examEndTimeByDuration = new Date(examStartTime.getTime() + duration * 60 * 1000);
            console.log("基于时长的结束时间", examEndTimeByDuration);

            // 获取系统设置的结束时间（如果存在）
            const systemEndTime = window.globalData?.exam?.endTime;

            // 取两者中较早的时间作为实际结束时间
            let actualEndTime = examEndTimeByDuration;
            if (systemEndTime) {
                const systemEndTimeMs = new Date(systemEndTime).getTime();
                const durationEndTimeMs = examEndTimeByDuration.getTime();
                
                // 使用较早的结束时间
                actualEndTime = new Date(Math.min(systemEndTimeMs, durationEndTimeMs));
                
                console.log("考试结束时间对比", {
                    基于时长的结束时间: new Date(durationEndTimeMs).toLocaleString(),
                    系统设置的结束时间: new Date(systemEndTimeMs).toLocaleString(),
                    实际使用的结束时间: actualEndTime.toLocaleString()
                });
            }

            // 存储精确的结束时间戳
            examEndTimeMs = actualEndTime.getTime();
            console.log("考试结束时间戳(ms)", examEndTimeMs);

            // 获取当前时间
            const currentTime = new Date();
            console.log("当前时间", currentTime);

            // 计算初始剩余时间（秒）
            let initialRemainingSeconds = Math.max(0, Math.floor((examEndTimeMs - currentTime.getTime()) / 1000));

            // 如果剩余时间小于0，可能是考试已经结束
            if (initialRemainingSeconds <= 0) {
                console.log("考试时间已结束或即将结束");
                remainingTime = 0; // 设置为0
                updateTimerDisplay();

                // 延迟一点时间后自动提交考试
                setTimeout(() => {
                    console.log("考试时间已结束，准备自动提交");
                    window.showScreenSwitchWarning("考试时间已结束，系统将自动提交您的答卷!");
                    setTimeout(() => {
                        submitExam(true); // 自动提交
                    }, 3000);
                }, 500);

                return;
            }

            // 设置初始剩余时间，用于首次显示
            remainingTime = initialRemainingSeconds;
            console.log("设置初始剩余时间(秒)", remainingTime);

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
                const callbackStartTime = performance.now(); // 记录回调开始时间

                // 基于结束时间戳计算当前剩余时间
                const currentTimeMs = new Date().getTime();
                const newRemainingSeconds = Math.max(0, Math.floor((examEndTimeMs - currentTimeMs) / 1000));

                remainingTime = newRemainingSeconds; // 更新剩余时间变量

                //const calculationTime = performance.now(); // 记录计算后时间
                //console.log(`[TimerTick] 开始执行，当前时间: ${new Date().toISOString()}, 计算后剩余时间: ${remainingTime}, 计算耗时: ${(calculationTime - callbackStartTime).toFixed(2)}ms`);

                if (remainingTime <= 1) {
                    console.log("[TimerTick] 考试时间结束，准备自动提交");
                    clearInterval(examTimerInterval);
                    // 确保显示为00:00:00
                    updateTimerDisplay();
                    submitExam(true); // 自动提交
                    return;
                }

                updateTimerDisplay();
                const updateDisplayTime = performance.now(); // 记录更新显示后时间
                const callbackDuration = updateDisplayTime - callbackStartTime;
                //console.log(`[TimerTick] 更新显示完成, 耗时: ${(updateDisplayTime - calculationTime).toFixed(2)}ms, 总耗时: ${callbackDuration.toFixed(2)}ms`);

                // 如果回调执行时间过长（例如超过50ms），可以发出警告
                if (callbackDuration > 50) {
                    console.warn(`[TimerTick] 警告：计时器回调执行时间过长 (${callbackDuration.toFixed(2)}ms)，可能导致计时不准`);
                }
            }, 1000); // 保持1秒间隔触发检查
            console.log("计时器已成功启动", examTimerInterval);
        } catch (error) {
            console.error("计时器启动过程中出错", error);
        }
    }

    // 优化版本的计时器显示更新
    const updateTimerDisplayOriginal = function() {
        try {
            const currentTime = Date.now();
            // 限制DOM更新频率，避免过度渲染
            if (currentTime - lastTimerUpdate < 900) { // 900ms内不重复更新DOM
                return;
            }
            lastTimerUpdate = currentTime;
            
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

            // 只在值真正变化时更新全局数据
            const currentRemainingSeconds = window.GlobalData.get('timer.remainingSeconds', -1);
            if (currentRemainingSeconds !== remainingTime) {
                window.GlobalData.set('timer.remainingSeconds', remainingTime);
            }

            // 检测是否在特殊时间段内，设置相应样式类
            let timerClassName = "exam-timer";

            if (remainingTime <= 300) { // 5分钟内
                timerClassName += " countdown-urgent countdown-final";

                // 处理最后5分钟的特效
                handleFinalCountdown(remainingTime);
            } else if (remainingTime <= 1800) { // 30分钟内
                timerClassName += " countdown-warn";
            }

            // 使用requestAnimationFrame优化DOM更新
            requestAnimationFrame(() => {
                updateTimerDOMDisplay(displayText, timerClassName);
            });

        } catch (error) {
            console.error("更新计时器显示时发生错误:", error);
        }
    };
    
    // 用性能监控包装计时器更新函数
    const updateTimerDisplay = PerformanceMonitor.wrapFunction(
        updateTimerDisplayOriginal,
        '计时器更新',
        CONSTANTS.PERFORMANCE_THRESHOLDS.TIMER_UPDATE
    );

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

    // 添加保存答案的函数 - 性能优化版本
    const saveAnswerOriginal = function(frontendQuestionId, answer) {
        try {
            // 确保frontendQuestionId是字符串类型
            frontendQuestionId = String(frontendQuestionId);
            
            // 查找对应的题目数据，获取服务器端需要的questionId
            const questions = window.globalData?.exam?.questions || [];
            const questionData = questions.find(q => String(q.id) === frontendQuestionId);
            
            if (!questionData) {
                console.error(`[保存答案] 未找到前端题目ID ${frontendQuestionId} 对应的题目数据`);
                return false;
            }
            
            // 获取服务器端需要的questionId
            const serverQuestionId = questionData.questionId || questionData.id;
            
            // 确保examAnswers是数组
            if (!Array.isArray(window.examAnswers)) {
                window.examAnswers = [];
            }
            
            // 高效查找和更新答案（使用前端ID作为本地存储键）
            const existingIndex = window.examAnswers.findIndex(a => String(a.questionId) === frontendQuestionId);
            if (existingIndex >= 0) {
                // 更新现有答案
                window.examAnswers[existingIndex].answer = answer;
                window.examAnswers[existingIndex].serverQuestionId = serverQuestionId; // 保存服务器ID
            } else {
                // 添加新答案
                window.examAnswers.push({ 
                    questionId: frontendQuestionId,  // 前端ID用于本地查找
                    serverQuestionId: serverQuestionId, // 服务器ID用于提交
                    answer 
                });
            }
            
            // 立即更新答题卡状态（使用前端ID）
            updateAnswerCardStatusOptimized(frontendQuestionId);
            
            // 获取考试记录ID
            const recordId = window.globalData.exam.recordId;
            
            if (!recordId) {
                console.error(`[答题卡] 保存答案失败：缺少考试ID(${examId})或记录ID(${recordId})`);
                return false;
            }
            
            // 使用防抖机制发送到服务器，发送服务器需要的ID
            debouncedSendAnswerToServer(recordId, serverQuestionId, answer);
            
            return true;
        } catch (error) {
            console.error(`[答题卡] 保存答案时出错:`, error);
            return false;
        }
    };
    
    // 用性能监控包装保存答案函数
    const saveAnswer = PerformanceMonitor.wrapFunction(
        saveAnswerOriginal, 
        '保存答案', 
        CONSTANTS.PERFORMANCE_THRESHOLDS.SAVE_ANSWER
    );

    // 防抖版本的答案发送函数
    function debouncedSendAnswerToServer(recordId, serverQuestionId, answer) {
        // 清除该题目的现有防抖计时器（使用服务器ID作为键）
        if (answerSaveTimeouts.has(serverQuestionId)) {
            clearTimeout(answerSaveTimeouts.get(serverQuestionId));
        }
        
        // 设置新的防抖计时器（800ms延迟）
        const timeoutId = setTimeout(() => {
            sendAnswerToServer(recordId, serverQuestionId, answer);
            answerSaveTimeouts.delete(serverQuestionId);
        }, 800);
        
        answerSaveTimeouts.set(serverQuestionId, timeoutId);
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

        fetch(window.ExamApiManager.transformUrl(`/exam/api/exam/client/${recordId}/save-answer`), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + TokenManager.getToken(),
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

        // 检查最小考试时间限制（仅对手动提交进行检查）
        // 但如果考试只剩最后5分钟，则允许提交
        if (!isAutoSubmit) {
            const examStartTime = window.globalData?.exam?.startTime;
            const minExamTime = window.globalData?.exam?.minExamTime; // 最小考试时间（分钟）
            
            // 获取当前剩余时间（秒）
            const currentRemainingTime = remainingTime;
            
            if (examStartTime && minExamTime && minExamTime > 0) {
                const currentTime = new Date();
                const startTime = new Date(examStartTime);
                const elapsedMinutes = Math.floor((currentTime - startTime) / (1000 * 60)); // 已用时间（分钟）
                
                // 如果剩余时间大于5分钟（300秒），才检查最小考试时间
                if (currentRemainingTime > 300 && elapsedMinutes < minExamTime) {
                    const remainingMinutes = minExamTime - elapsedMinutes;
                    const message = `请不要提前交卷！您需要再考试 ${remainingMinutes} 分钟后才能提交试卷。\n\n当前已考试时间：${elapsedMinutes} 分钟\n最低要求时间：${minExamTime} 分钟`;
                    
                    console.log(`[提交限制] 考试时间不足，已用时间：${elapsedMinutes}分钟，最低要求：${minExamTime}分钟`);
                    createCustomNotification('提交失败', message, 'error', 5000);
                    return;
                } else if (currentRemainingTime <= 300 && elapsedMinutes < minExamTime) {
                    // 剩余时间不足5分钟，允许提交，但给予提示
                    console.log(`[提交限制] 考试剩余时间不足5分钟，允许提交（已用时间：${elapsedMinutes}分钟，最低要求：${minExamTime}分钟）`);
                }
            }
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
                // 高效收集未同步的答案
                // 注意：unsyncedAnswers中存储的是服务器ID，需要反向查找前端ID对应的答案
                window.unsyncedAnswers.forEach(serverQuestionId => {
                    const serverQuestionIdStr = String(serverQuestionId);
                    
                    // 在本地答案中查找对应的答案（通过serverQuestionId匹配）
                    const answerData = window.examAnswers.find(a => a.serverQuestionId === serverQuestionIdStr);
                    if (answerData) {
                        answersToSync.push({
                            questionId: serverQuestionIdStr,  // 使用服务器ID
                            answer: answerData.answer
                        });
                    } else {
                        console.warn(`[提交考试] 未找到服务器ID ${serverQuestionIdStr} 对应的本地答案`);
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
            fetch(window.ExamApiManager.transformUrl(`/exam/api/exam/client/${recordId}/save-answers`), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + TokenManager.getToken(),
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
        const answers = window.examAnswers.map(a => {
            // 使用服务器ID进行最终提交
            const serverQuestionId = a.serverQuestionId || a.questionId;
            
            return {
                questionId: String(serverQuestionId), // 使用服务器ID
                answer: a.answer
            };
        });
        
        // 清理防抖计时器，避免内存泄漏
        answerSaveTimeouts.forEach(timeoutId => clearTimeout(timeoutId));
        answerSaveTimeouts.clear();

        console.debug('[提交考试] 最终提交的答案数据:', answers);

        // 提示用户提交正在进行中
        if (typeof createCustomNotification === 'function') {
            createCustomNotification('提交中', '正在提交考试，请不要关闭页面...', 'info', 30000);
        }

        // 提交考试
        fetch(window.ExamApiManager.transformUrl(`/exam/api/exam/client/${recordId}/submit`), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + TokenManager.getToken(),
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
                            window.location.href = `/${window.tenantId}/exam/result/${recordId}`;
                        }, 1500);
                    } else {
                        // 不允许查看结果，显示提交成功页面
                        setTimeout(() => {
                            window.location.href = `/${window.tenantId}/exam/app`;
                        }, 1500);
                    }
                }
                else if (data.status === 409) {
                    createCustomNotification('提交失败', data.msg || "提交失败，请重试", 'error', 5000);
                    // 不允许查看结果，显示提交成功页面
                    setTimeout(() => {
                        window.location.href = `/${window.tenantId}/index`;
                    }, 1500);
                }
                else {
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
            window.location.href = `/${window.tenantId}/index`;
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
                                        tpl: '<div class="logo"><img src="${tenant.logo}" /><span>${tenant.name}</span></div>',
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
                                                    
                                                    // 尝试创建水印（考生信息已加载）
                                                    if (typeof window.initializeWatermark === 'function') {
                                                        window.initializeWatermark();
                                                    }
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
                                className: 'exam-answer-card-container',
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
                                ],
                                onEvent: {
                                    "inited": {
                                        actions: [
                                            {
                                                actionType: "custom",
                                                script: "console.log('[答题卡] 初始化完成，items:', event.data.questions ? event.data.questions.length : 0);"
                                            }
                                        ]
                                    }
                                }
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

                                        // 更新最低考试时间
                                        window.globalData.exam.minExamTime = event.data.minExamTime || 0;
                                        
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
                                        
                                        // 为题目数据添加answered状态 - 性能优化版本
                                        if (Array.isArray(event.data.questions)) {
                                            // 确保examAnswers是数组
                                            if (!Array.isArray(window.examAnswers)) {
                                                console.warn("window.examAnswers未定义，初始化为空数组");
                                                window.examAnswers = [];
                                                examAnswers = window.examAnswers;
                                            }
                                            
                                            // 创建答案查找映射，提升性能（使用前端ID作为键）
                                            const answerMap = new Map();
                                            window.examAnswers.forEach(answer => {
                                                if (answer && answer.questionId) {
                                                    answerMap.set(String(answer.questionId), answer.answer);
                                                }
                                            });
                                            
                                            // 初始化题目的answered状态
                                            event.data.questions.forEach(question => {
                                                // 优先使用question.id，与DOM中的data-question-id保持一致
                                                const questionId = String(question.id);
                                                const hasAnswer = answerMap.has(questionId);
                                                const existingAnswer = question.answer;
                                                
                                                // 优先使用已保存的答案，其次是从服务器获取的答案
                                                question.answered = hasAnswer || (existingAnswer && existingAnswer.length > 0);
                                                
                                                // 如果本地有答案但题目对象没有，同步过去
                                                if (hasAnswer && !existingAnswer) {
                                                    question.answer = answerMap.get(questionId);
                                                }
                                            });
                                            
                                            // 初始化题目ID索引，提升后续查找性能
                                            setTimeout(initializeQuestionIdIndex, 100);
                                        }
                                        
                                        window.globalData.exam.questions = event.data.questions || [];
                                        
                                        // 对题目进行分组处理
                                        const questionGroups = groupQuestionsByType(event.data.questions || []);
                                        console.log("题目分组结果：", questionGroups);
                                        
                                        console.log("成功初始化考试数据");
                                        // 打印题目数据，检查是否正确
                                        console.log("考试题目数据：", event.data.questions);
                                        
                                        // 直接更新AMIS中的题目数据，确保答题卡显示
                                        if (window.amisInstance) {
                                            window.amisInstance.updateProps({
                                                data: {
                                                    questions: event.data.questions || [],
                                                    questionGroups: questionGroups
                                                }
                                            });
                                            console.log("已同步题目数据到AMIS:", event.data.questions?.length || 0);
                                            console.log("已同步分组数据到AMIS:", questionGroups.length);
                                        }
                                        
                                        // 尝试创建水印（考试数据已加载）
                                        if (typeof window.initializeWatermark === 'function') {
                                            window.initializeWatermark();
                                        }
                                        
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
                                        name: "questionGroups",
                                        trackExpression: "${item.type}",
                                        items: {
                                            type: "container",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class=\"question-type-header\"><h3 class=\"question-type-title\"><i class=\"fa fa-list\"></i> ${item.typeName}（共${item.count}题）</h3></div>",
                                                    inline: false,
                                                    className: "question-type-header-wrapper"
                                                },
                                                {
                                                    type: "each",
                                                    name: "questions",
                                                    trackExpression: "${item.id}",
                                                    items: {
                                                        type: "container",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class=\"question-label\" id=\"question_${item.id}_label\"><pre>${(item.globalIndex + 1) + '. ' + item.content | raw} </pre></div>",
                                                                inline: false
                                                            },
                                                            {
                                                                type: "container",
                                                                body: [
                                                                    {
                                                                        type: "radios",
                                                                        name: "question_${item.id}",
                                                                        source: "${options}",
                                                                        mode: "vertical",
                                                                        value: "${answer}",
                                                                        visibleOn: "item.type === 'SingleChoice'",
                                                                        className: "question-options",
                                                                        onEvent: {
                                                                            change: {
                                                                                actions: [
                                                                                    {
                                                                                        actionType: "custom",
                                                                                        script: "saveAnswer(event.data.__super.id, event.data.value);"
                                                                                    }
                                                                                ]
                                                                            }
                                                                        }
                                                                    },
                                                                    {
                                                                        type: "checkboxes",
                                                                        name: "question_${item.id}",
                                                                        source: "${options}",
                                                                        mode: "vertical",
                                                                        value: "${answer}",
                                                                        required: "${item.isRequired}",
                                                                        visibleOn: "item.type === 'MultipleChoice'",
                                                                        className: "question-options",
                                                                        onEvent: {
                                                                            change: {
                                                                                actions: [
                                                                                    {
                                                                                        actionType: "custom",
                                                                                        script: "saveAnswer(event.data.__super.id, event.data.value);"
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
                                                                                label: "✓",
                                                                                value: "True"
                                                                            },
                                                                            {
                                                                                label: "✗",
                                                                                value: "False"
                                                                            }
                                                                        ],
                                                                        mode: "vertical",
                                                                        value: "${answer}",
                                                                        visibleOn: "${item.type === 'TrueFalse'}",
                                                                        className: "question-options",
                                                                        onEvent: {
                                                                            change: {
                                                                                actions: [
                                                                                    {
                                                                                        actionType: "custom",
                                                                                        script: "saveAnswer(event.data.__super.id, event.data.value);"
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
                                                                                        script: "saveAnswer(event.data.__super.id, event.data.value);"
                                                                                    }
                                                                                ]
                                                                            }
                                                                        }
                                                                    }
                                                                ]
                                                            },
                                                            {
                                                                type: "divider",
                                                                hidden: "${__super.index === __super.questions.length - 1}"
                                                            }
                                                        ]
                                                    }
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

    // 注册用于提交考试的全局方法（部分函数在后面声明）
    window.submitExam = submitExam;
    window.cancelExam = cancelExam;
    window.startExamTimer = startExamTimer;
    window.updateTimerDisplay = updateTimerDisplay;
    window.saveAnswer = saveAnswer;
    // initializeExamTimer 将在后面赋值

    // 添加跳转到题目的全局方法
    window.scrollToQuestion = function (questionId, questionIndex) {
        try {
            console.log(`[答题卡] 尝试滚动到题目 ${questionIndex}, ID: ${questionId}`);
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

                // 使用window.scrollTo平滑滚动到目标位置
                window.scrollTo({
                    top: targetScrollPosition,
                    behavior: 'smooth'
                });

                // 添加高亮效果
                questionElement.parentElement.classList.add('highlight-question');
                // 3秒后移除高亮
                setTimeout(function () {
                    questionElement.parentElement.classList.remove('highlight-question');
                }, 3000);

                console.log(`[答题卡] 成功滚动到题目 ${questionIndex}，考虑了顶部高度: ${fixedHeaderHeight}px`);
                return true;
            } else {
                console.warn(`[答题卡] 未找到题目元素: ${questionId}`);
                return false;
            }
        } catch (error) {
            console.error(`[答题卡] 滚动到题目时出错:`, error);
            return false;
        }
    };

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
                questions: [], // 初始化为空数组
                tenant: window.globalData.tenant // 添加租户信息
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
                // 标记AMIS实例已完全初始化
                window.amisReady = true;
                
                // 在AMIS完全加载后尝试创建水印
                if (typeof window.initializeWatermark === 'function') {
                    window.initializeWatermark();
                }
            },
            updateLocation: (location) => {
                history.push(location);
            },
            jumpTo: (to) => {
                history.push(to);
            },
            requestAdaptor: (api) => {
                var token = TokenManager.getToken();
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
                    window.location.href = `/${window.tenantId}/exam/app`;
                    return { msg: '您没有权限访问此页面，请联系管理员！' }
                }
                else if (response.status === 401) {
                    window.location.href = `/${window.tenantId}/exam/login`;
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

                        //console.debug('Global user data updated:', window.globalData.user);
                    }
                }

                return payload;
            },
            theme: 'antd'
        }
    );

    // 立即设置全局amis实例以确保计时器可以使用
    window.amisInstance = amisInstance;

    // 立即加载租户信息以更新页面显示
    loadTenantInfo().then(tenantInfo => {
        console.log('[考试页] 租户信息初始化完成:', tenantInfo);
        // 强制更新AMIS实例的数据
        if (window.amisInstance) {
            window.amisInstance.updateProps({
                data: {
                    ...window.amisInstance.props.data,
                    tenant: tenantInfo
                }
            });
        }
    }).catch(error => {
        console.error('[考试页] 租户信息初始化失败:', error);
    });

    // 更新固定头部高度的函数
    function updateFixedHeaderHeight() {
        try {
            const fixedHeaderContainer = document.querySelector('.fixed-header-container');
            if (fixedHeaderContainer) {
                const height = fixedHeaderContainer.offsetHeight;
                document.documentElement.style.setProperty('--fixed-header-height', height + 'px');
                console.log("[固定头部] 高度已更新:", height + 'px');

                // 更新spacer高度
                const spacer = document.querySelector('.fixed-header-spacer');
                if (spacer) {
                    spacer.style.height = (height + 10) + 'px';
                    console.log("[固定头部] spacer高度已更新:", (height + 10) + 'px');
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

        window.addEventListener('scroll', function () {
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

        //console.log("[UI优化] 设置顶部栏滚动紧凑化效果");
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
    window.addEventListener('DOMContentLoaded', function () {
        setupHeaderScroll();

        // 计算并设置固定头部高度
        setTimeout(updateFixedHeaderHeight, 500);
        // 窗口大小改变时重新计算
        window.addEventListener('resize', updateFixedHeaderHeight);
    });

    // 修改onLoad回调函数，确保初始化相关功能
    window.onLoad = function () {
        console.log('[考试页] 初始化事件');

        // 初始化考试全局变量
        window.examAnswers = [];

        // 加载租户信息
        loadTenantInfo().then(tenantInfo => {
            console.log('[考试页] 租户信息加载完成:', tenantInfo);
        }).catch(error => {
            console.error('[考试页] 租户信息加载失败:', error);
        });

        // 设置切屏检测
        if (typeof window.ScreenSwitchDetector !== 'undefined' && typeof window.ScreenSwitchDetector.setup === 'function') {
            window.ScreenSwitchDetector.setup();
        } else if (typeof window.setupScreenSwitchDetection === 'function') {
            // 向后兼容旧的全局函数
            window.setupScreenSwitchDetection();
        }
        
        // 初始化水印
        if (typeof window.initializeWatermark === 'function') {
            window.initializeWatermark();
        }
    };
    
    // 页面卸载时清理所有资源
    window.addEventListener('beforeunload', function() {
        // 清理水印资源
        if (typeof window.WatermarkManager === 'object' && typeof window.WatermarkManager.destroy === 'function') {
            window.WatermarkManager.destroy();
        }
        
        // 清理考试相关资源
        if (typeof window.cleanupExamResources === 'function') {
            window.cleanupExamResources();
        }
    });

    // 初始化考试计时器
    const initializeExamTimerOriginal = function(examData) {
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
            setTimeout(function () {
                if (typeof window.ScreenSwitchDetector !== 'undefined' && typeof window.ScreenSwitchDetector.setup === 'function') {
                    window.ScreenSwitchDetector.setup();
                } else if (typeof window.setupScreenSwitchDetection === 'function') {
                    // 向后兼容旧的全局函数
                    window.setupScreenSwitchDetection();
                } else {
                    console.error("切屏检测函数未定义");
                }
                
                // 初始化性能优化组件
            }, 2000);
        } catch (error) {
            console.error("初始化考试计时器失败", error);
        }
    };
    
    // 用性能监控包装初始化函数
    const initializeExamTimer = PerformanceMonitor.wrapFunction(
        initializeExamTimerOriginal,
        '考试计时器初始化',
        CONSTANTS.PERFORMANCE_THRESHOLDS.INITIALIZATION
    );

    /**
     * 题型分组函数
     * 按题型对题目进行分组，题目已由后端按题型顺序排序
     * @param {Array} questions - 题目数组（后端已排序）
     * @returns {Array} 分组后的题目数组
     */
    function groupQuestionsByType(questions) {
        const groups = [];
        const typeOrder = ['SingleChoice', 'MultipleChoice', 'TrueFalse', 'Essay']; // 题型顺序
        const typeNames = {
            'SingleChoice': '单选题',
            'MultipleChoice': '多选题',
            'TrueFalse': '判断题',
            'Essay': '主观题'
        };
        
        // 按题型分组，题目保持后端返回的顺序
        const groupedByType = {};
        let questionIndex = 0; // 全局题目编号
        
        questions.forEach(question => {
            let type = question.type;
            // 如果不是已知类型，归类为主观题
            if (!typeOrder.includes(type)) {
                type = 'Essay';
            }
            
            if (!groupedByType[type]) {
                groupedByType[type] = [];
            }
            
            // 添加全局题目编号（题目已由后端按题型排序）
            question.globalIndex = questionIndex++;
            groupedByType[type].push(question);
        });
        
        // 按预定义顺序生成分组
        typeOrder.forEach(type => {
            if (groupedByType[type] && groupedByType[type].length > 0) {
                groups.push({
                    type: type,
                    typeName: typeNames[type],
                    questions: groupedByType[type],
                    count: groupedByType[type].length
                });
            }
        });
        
        return groups;
    }

    // 初始化题目ID索引映射，提升查找性能
    const initializeQuestionIdIndexOriginal = function() {
        questionIdIndexMap.clear();
        const questions = window.globalData?.exam?.questions || [];
        
        questions.forEach((question, index) => {
            if (question.id) {
                // 优先使用question.id，这个是与DOM中data-question-id匹配的ID
                const id = String(question.id);
                questionIdIndexMap.set(id, { index, question });
            } else if (question.questionId) {
                // 备用：如果没有question.id，使用questionId
                const id = String(question.questionId);
                questionIdIndexMap.set(id, { index, question });
            }
        });
    };
    
    // 用性能监控包装题目索引初始化函数
    const initializeQuestionIdIndex = PerformanceMonitor.wrapFunction(
        initializeQuestionIdIndexOriginal,
        '题目索引初始化',
        CONSTANTS.PERFORMANCE_THRESHOLDS.INITIALIZATION
    );
    
    // 优化版本的答题卡状态更新函数
    const updateAnswerCardStatusOptimizedOriginal = function(questionId) {
        try {
            questionId = String(questionId);
            
            let dataUpdated = false;
            
            // 使用索引映射快速查找
            const indexData = questionIdIndexMap.get(questionId);
            if (indexData) {
                indexData.question.answered = true;
                dataUpdated = true;
            } else {
                // 降级到传统方法（以防索引失效）
                const questions = window.globalData?.exam?.questions || [];
                // 优先使用question.id进行匹配，与DOM中的data-question-id保持一致
                const questionIndex = questions.findIndex(q => String(q.id) === questionId);
                if (questionIndex >= 0) {
                    questions[questionIndex].answered = true;
                    dataUpdated = true;
                } else {
                    console.warn(`[答题卡] 在数据中未找到题目ID: ${questionId}，但仍会尝试更新DOM`);
                }
            }
            
            // 无论是否找到题目数据，都尝试更新DOM（因为DOM可能独立存在）
            requestAnimationFrame(() => {
                updateAnswerCardDOM(questionId, true);
            });
            
            return dataUpdated;
        } catch (error) {
            console.error('[答题卡] 更新答题卡状态时出错:', error);
            
            // 即使出错也尝试更新DOM
            try {
                requestAnimationFrame(() => {
                    updateAnswerCardDOM(questionId, true);
                });
            } catch (domError) {
                console.error('[答题卡] DOM更新也失败:', domError);
            }
            
            return false;
        }
    };
    
    // 用性能监控包装答题卡更新函数
    const updateAnswerCardStatusOptimized = PerformanceMonitor.wrapFunction(
        updateAnswerCardStatusOptimizedOriginal,
        '答题卡更新',
        CONSTANTS.PERFORMANCE_THRESHOLDS.ANSWER_CARD_UPDATE
    );
    
    // 优化的DOM更新函数 - 适配实际DOM结构
    function updateAnswerCardDOM(questionId, answered) {
        try {
            // 根据实际DOM结构：外层span有answer-card-item类，内层a元素有data-question-id属性
            const anchorElements = document.querySelectorAll(`a[data-question-id="${questionId}"]`);
            
            anchorElements.forEach(anchor => {
                // 找到包含answer-card-item类的最外层容器
                let answerCardContainer = anchor.closest('.answer-card-item');
                
                // 如果没找到，向上遍历查找（适配不同的DOM结构）
                if (!answerCardContainer) {
                    let parent = anchor.parentElement;
                    while (parent && parent !== document.body) {
                        if (parent.classList.contains('answer-card-item')) {
                            answerCardContainer = parent;
                            break;
                        }
                        parent = parent.parentElement;
                    }
                }
                
                // 更新状态类名
                if (answerCardContainer) {
                    if (answered) {
                        answerCardContainer.classList.remove('unanswered');
                        answerCardContainer.classList.add('answered');
                    } else {
                        answerCardContainer.classList.remove('answered');
                        answerCardContainer.classList.add('unanswered');
                    }
                } else {
                    console.warn(`[答题卡] 未找到题目${questionId}对应的答题卡容器`);
                }
            });
            
            // 如果没有找到任何元素，尝试备用方案
            if (anchorElements.length === 0) {
                console.warn(`[答题卡] 未找到题目ID为${questionId}的元素，尝试备用查找方案`);
                
                // 备用方案：查找包含题目ID的任何元素
                const fallbackElements = document.querySelectorAll(`[data-question-id="${questionId}"], [data-question-index*="${questionId}"]`);
                
                fallbackElements.forEach(element => {
                    const container = element.closest('.answer-card-item') || element;
                    if (container) {
                        if (answered) {
                            container.classList.remove('unanswered');
                            container.classList.add('answered');
                        } else {
                            container.classList.remove('answered');
                            container.classList.add('unanswered');
                        }
                    }
                });
            }
            
        } catch (error) {
            console.error('[答题卡] 更新DOM时出错:', error);
        }
    }

    // 使函数全局可用，供AMIS调用
    window.initializeExamTimer = initializeExamTimer;
    window.saveAnswer = saveAnswer;
    window.initializeQuestionIdIndex = initializeQuestionIdIndex;
    window.updateAnswerCardStatusOptimized = updateAnswerCardStatusOptimized;
    window.groupQuestionsByType = groupQuestionsByType;

    // 兼容性函数，保持向后兼容
    function updateAnswerCardStatus(questionId) {
        return updateAnswerCardStatusOptimized(questionId);
    }
    
    // 清理资源的函数
    window.cleanupExamResources = function() {
        // 清理防抖计时器
        answerSaveTimeouts.forEach(timeoutId => clearTimeout(timeoutId));
        answerSaveTimeouts.clear();
        
        // 清理题目ID索引
        questionIdIndexMap.clear();
        
        // 清理计时器
        if (examTimerInterval) {
            clearInterval(examTimerInterval);
            examTimerInterval = null;
        }
    };

    // 调试工具函数
    window.debugExamQuestions = function() {
        console.group('[调试工具] 考试题目信息');
        
        // 显示题目数据和ID映射关系
        const questions = window.globalData?.exam?.questions || [];
        console.log('题目数据总数:', questions.length);
        console.log('索引映射大小:', questionIdIndexMap.size);
        console.log('索引映射内容:', Array.from(questionIdIndexMap.keys()));
        
        // 显示前端ID和服务器ID的映射关系
        console.group('题目ID映射关系:');
        questions.slice(0, 10).forEach((q, index) => {
            console.log(`题目${index + 1}:`, {
                前端ID: q.id,
                服务器ID: q.questionId,
                题目类型: q.type,
                题目内容: q.content?.substring(0, 30) + '...'
            });
        });
        if (questions.length > 10) {
            console.log(`... 还有 ${questions.length - 10} 个题目`);
        }
        console.groupEnd();
        
        // 显示已保存的答案
        console.group('已保存答案:');
        console.log('本地答案数量:', window.examAnswers?.length || 0);
        window.examAnswers?.slice(0, 5).forEach((answer, index) => {
            console.log(`答案${index + 1}:`, {
                前端ID: answer.questionId,
                服务器ID: answer.serverQuestionId,
                答案: answer.answer
            });
        });
        if ((window.examAnswers?.length || 0) > 5) {
            console.log(`... 还有 ${window.examAnswers.length - 5} 个答案`);
        }
        console.groupEnd();
        
        // 检查DOM中的答题卡元素
        const answerCardElements = document.querySelectorAll('.answer-card-item a[data-question-id]');
        console.log('DOM中的答题卡元素数量:', answerCardElements.length);
        console.log('未同步答案数量:', window.unsyncedAnswers?.size || 0);
        console.log('同步失败答案数量:', window.failedSyncAnswers?.size || 0);
        
        console.groupEnd();
    };
    
    window.testAnswerCardUpdate = function(questionId) {
        console.log(`[调试工具] 测试更新题目 ${questionId} 的答题卡状态`);
        updateAnswerCardStatusOptimized(questionId);
    };
    
    window.reinitializeQuestionIndex = function() {
        console.log('[调试工具] 重新初始化题目索引');
        initializeQuestionIdIndex();
    };

    // 性能监控调试工具
    window.getPerformanceReport = function() {
        PerformanceMonitor.getReport();
    };
    
    window.clearPerformanceStats = function() {
        PerformanceMonitor.clearStatistics();
    };
    
    window.enablePerformanceMonitoring = function() {
        PerformanceMonitor.isEnabled = true;
        console.log('%c[性能监控] 手动启用性能监控', 'color: #4CAF50; font-weight: bold;');
    };
    
    window.disablePerformanceMonitoring = function() {
        PerformanceMonitor.isEnabled = false;
        console.log('%c[性能监控] 手动禁用性能监控', 'color: #FF9800; font-weight: bold;');
    };
    
    // 在开发环境中添加性能监控提示
    if (PerformanceMonitor.isEnabled) {
        console.group('%c[性能监控] 调试命令', 'color: #2196F3; font-weight: bold;');
        console.log('%cgetPerformanceReport()', 'color: #4CAF50;', '- 查看性能统计报告');
        console.log('%cclearPerformanceStats()', 'color: #FF9800;', '- 清空统计数据');
        console.log('%cenablePerformanceMonitoring()', 'color: #2196F3;', '- 手动启用监控');
        console.log('%cdisablePerformanceMonitoring()', 'color: #F44336;', '- 手动禁用监控');
        console.groupEnd();
    }
})(); 