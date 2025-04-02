(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    // 获取考试ID
    const examId = window.location.pathname.split('/').pop();
    window.enableAMISDebug = true;
    
    // 创建全局切屏计数变量
    window.screenSwitchCount = 0;
    
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
            screenSwitchCount: 0,         // 添加切屏次数属性
            allowedScreenSwitchCount: 0   // 添加允许切屏次数属性
        },
        timer: {
            displayText: '加载中...',
            hours: 0,
            minutes: 0,
            seconds: 0,
            remainingSeconds: 0
        }
    };

    // 改进显示警告的函数，使用正确的AMIS API
    window.showScreenSwitchWarning = function(message) {
        try {            
            // 默认消息
            const warningMessage = message || "警告：系统已记录您的切屏行为！频繁切屏可能会被判定为作弊行为。";
        
            
            // 尝试方法4：添加一个自定义通知
            const createCustomNotification = function() {
                // 创建通知DOM元素
                const notification = document.createElement('div');
                notification.className = 'custom-notification warning';
                notification.innerHTML = `
                    <div class="notification-title">切屏警告</div>
                    <div class="notification-body">${warningMessage}</div>
                `;
                
                // 添加样式
                notification.style.position = 'fixed';
                notification.style.top = '20px';
                notification.style.right = '20px';
                notification.style.backgroundColor = '#ffebee';
                notification.style.color = '#f44336';
                notification.style.padding = '15px';
                notification.style.borderRadius = '4px';
                notification.style.boxShadow = '0 2px 10px rgba(0,0,0,0.2)';
                notification.style.zIndex = '9999';
                notification.style.maxWidth = '300px';
                notification.style.animation = 'fadeIn 0.3s ease';
                
                // 添加到页面
                document.body.appendChild(notification);
                
                // 5秒后自动移除
                setTimeout(() => {
                    notification.style.animation = 'fadeOut 0.3s ease';
                    setTimeout(() => {
                        if (document.body.contains(notification)) {
                            document.body.removeChild(notification);
                        }
                    }, 300);
                }, 5000);
                
                return true;
            };
            
            // 添加必要的CSS动画
            const addAnimationStyles = function() {
                if (!document.getElementById('custom-notification-styles')) {
                    const style = document.createElement('style');
                    style.id = 'custom-notification-styles';
                    style.innerHTML = `
                        @keyframes fadeIn {
                            from { opacity: 0; transform: translateY(-20px); }
                            to { opacity: 1; transform: translateY(0); }
                        }
                        @keyframes fadeOut {
                            from { opacity: 1; transform: translateY(0); }
                            to { opacity: 0; transform: translateY(-20px); }
                        }
                        .custom-notification {
                            font-family: "PingFang SC", "Microsoft YaHei", sans-serif;
                        }
                        .custom-notification .notification-title {
                            font-weight: bold;
                            margin-bottom: 5px;
                        }
                    `;
                    document.head.appendChild(style);
                }
            };
            
            // 尝试自定义通知
            addAnimationStyles();
            createCustomNotification();
            console.log("[切屏警告] 使用自定义通知方式显示警告");
            return true;
            
        } catch (error) {
            console.error("[切屏警告] 显示AMIS警告时出错:", error);
            
            // 最后的后备方案：使用原生alert (不推荐，会触发新的切屏事件)
            // alert(message || "警告：系统已记录您的切屏行为！频繁切屏可能会被判定为作弊行为。");
            return false;
        }
    };
    
    // 修改recordScreenSwitch函数，使用AMIS弹框代替alert
    window.recordScreenSwitch = function() {
        try {
            console.log("[切屏检测] 检测到切屏行为");
            
            // 获取记录ID - 从全局变量获取
            const recordId = window.globalData.exam.recordId;
            if (!recordId) {
                console.error("[切屏检测] 无法获取考试记录ID");
                
                // 使用AMIS弹框显示警告
                window.showScreenSwitchWarning("警告：系统检测到切屏行为！请勿频繁切换窗口。");
                return;
            }
            
            // 获取允许的切屏次数
            const allowedCount = window.globalData.exam.allowedScreenSwitchCount || 0;
            
            // 更新计数(使用全局变量)
            window.screenSwitchCount++;
            // 同步到globalData
            window.globalData.exam.screenSwitchCount = window.screenSwitchCount;
            console.log("[切屏检测] 当前切屏次数:", window.screenSwitchCount);
            
            // 显示警告信息，根据切屏次数和允许次数调整内容
            if (allowedCount > 0) {
                if (window.screenSwitchCount > allowedCount) {
                    // 超过允许次数
                    window.showScreenSwitchWarning(`严重警告：您已超出允许的切屏次数(${allowedCount}次)！此行为已被记录为作弊嫌疑。`);
                } else {
                    // 未超过允许次数
                    window.showScreenSwitchWarning(`警告：系统已记录您的切屏行为！您已切屏 ${window.screenSwitchCount} 次，允许切屏 ${allowedCount} 次。`);
                }
            } else {
                // 没有明确的允许次数
                window.showScreenSwitchWarning();
            }
            
            // 直接更新DOM显示
            const switchCountElements = document.querySelectorAll('.screen-switch-value');
            if (switchCountElements && switchCountElements.length > 0) {
                switchCountElements.forEach(el => {
                    el.textContent = window.screenSwitchCount.toString();
                });
            }
            
            // 发送切屏记录到服务器
            fetch(`/exam/api/exam/client/${recordId}/screen-switch`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + localStorage.getItem('token'),
                    'X-Forwarded-With': 'CodeSpirit'
                }
            })
            .then(response => response.json())
            .then(data => {
                if (data.status === 0) {
                    console.log("[切屏检测] 切屏记录已成功发送到服务器");
                    
                    // 如果服务器返回了更新后的切屏次数，更新本地计数
                    if (data.data && typeof data.data.screenSwitchCount === 'number') {
                        window.screenSwitchCount = data.data.screenSwitchCount;
                        window.globalData.exam.screenSwitchCount = data.data.screenSwitchCount;
                        
                        // 更新DOM显示
                        const elements = document.querySelectorAll('.screen-switch-value');
                        if (elements && elements.length > 0) {
                            elements.forEach(el => {
                                el.textContent = window.screenSwitchCount.toString();
                            });
                        }
                    }
                } else {
                    console.error("[切屏检测] 记录切屏失败:", data.msg);
                }
            })
            .catch(error => {
                console.error("[切屏检测] 发送切屏记录时出错:", error);
            });
        } catch (error) {
            console.error("[切屏检测] 记录切屏过程中发生错误:", error);
        }
    };
    
    // 更新切屏检测初始化函数，但保留现有功能
    window.setupScreenSwitchDetection = function() {
        console.log("[切屏检测] 正在设置切屏检测...");
        
        try {
            // 检查是否已有记录ID
            if (!window.globalData.exam.recordId) {
                console.warn("[切屏检测] 记录ID未设置，将在3秒后重试");
                setTimeout(window.setupScreenSwitchDetection, 3000);
                return;
            }
            
            // 移除可能存在的旧事件监听器
            document.removeEventListener('visibilitychange', window.handleVisibilityChange);
            window.removeEventListener('blur', window.handleWindowBlur);
            
            // 添加事件监听
            document.addEventListener('visibilitychange', window.handleVisibilityChange);
            window.addEventListener('blur', window.handleWindowBlur);
            
            // 初始化切屏次数显示
            window.screenSwitchCount = window.globalData.exam.screenSwitchCount || 0;
            
            // 更新DOM显示
            const switchCountElements = document.querySelectorAll('.screen-switch-value');
            if (switchCountElements && switchCountElements.length > 0) {
                switchCountElements.forEach(el => {
                    el.textContent = window.screenSwitchCount.toString();
                });
            }
            
            // 更新允许切屏次数显示
            const allowedSwitchElements = document.querySelectorAll('.allowed-switch-value');
            if (allowedSwitchElements && allowedSwitchElements.length > 0) {
                const allowedCount = window.globalData.exam.allowedScreenSwitchCount || 0;
                allowedSwitchElements.forEach(el => {
                    el.textContent = allowedCount.toString();
                });
            }
            
            // 如果有允许的切屏次数，显示初始提示
            const allowedCount = window.globalData.exam.allowedScreenSwitchCount || 0;
            if (allowedCount > 0) {
                // 5秒后显示切屏限制提示
                setTimeout(() => {
                    window.showScreenSwitchWarning(`本次考试允许切屏 ${allowedCount} 次，超过将被记录为作弊嫌疑。当前已切屏 ${window.screenSwitchCount} 次。`);
                }, 5000);
            }
            
            console.log("[切屏检测] 切屏检测已成功启用");
        } catch (error) {
            console.error("[切屏检测] 设置切屏检测时发生错误:", error);
        }
    };
    
    // 事件处理函数
    window.handleVisibilityChange = function() {
        if (document.visibilityState === 'hidden') {
            window.recordScreenSwitch();
        }
    };
    
    window.handleWindowBlur = function() {
        window.recordScreenSwitch();
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
            
            console.log("[计时器] 更新显示:", displayText, "剩余秒数:", remainingTime);
            
            // 更新全局数据
            window.GlobalData.set('timer.displayText', displayText);
            window.GlobalData.set('timer.hours', hours);
            window.GlobalData.set('timer.minutes', minutes);
            window.GlobalData.set('timer.seconds', seconds);
            window.GlobalData.set('timer.remainingSeconds', remainingTime);
    
            // 检测是否在特殊时间段内，设置相应样式类
            let timerClassName = "exam-timer";
            let timerStyles = {};
            
            if (remainingTime <= 300) { // 5分钟内
                timerClassName += " countdown-urgent countdown-final";
                timerStyles = {
                    color: '#f44336',
                    fontWeight: 'bold',
                    animation: 'pulse-scale 1s infinite',
                    textShadow: '0 0 5px rgba(244, 67, 54, 0.5)'
                };
                
                // 处理最后5分钟的特效
                handleFinalCountdown(remainingTime);
            } else if (remainingTime <= 1800) { // 30分钟内
                timerClassName += " countdown-warn";
                timerStyles = {
                    color: '#ff9800',
                    fontWeight: 'bold',
                    animation: 'pulse 1.5s infinite',
                    textShadow: '0 0 3px rgba(255, 152, 0, 0.3)'
                };
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
                    updateTimerDOMDisplay(displayText, timerClassName, timerStyles);
                } catch (e) {
                    console.error("更新计时器显示时出错", e);
                    // 出错时直接更新DOM作为后备方案
                    updateTimerDOMDisplay(displayText, timerClassName, timerStyles);
                }
            } else {
                console.warn("amisInstance未初始化，使用DOM方式更新计时器显示");
                // 尝试直接更新DOM
                updateTimerDOMDisplay(displayText, timerClassName, timerStyles);
            }
        } catch (error) {
            console.error("更新计时器显示时发生错误:", error);
            // 尝试最基本的DOM更新
            try {
                const displayText = "00:00:00";
                updateTimerDOMDisplay(displayText, "exam-timer", {});
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
    
    // 辅助函数：更新计时器DOM显示
    function updateTimerDOMDisplay(displayText, className, styles) {
        const timerElements = document.querySelectorAll('.exam-timer');
        if (timerElements && timerElements.length > 0) {
            timerElements.forEach(el => {
                el.innerHTML = `剩余时间：${displayText}`;
                el.className = className || "exam-timer"; // 应用适当的类名
                
                // 应用内联样式
                if (styles && typeof styles === 'object') {
                    Object.keys(styles).forEach(key => {
                        el.style[key] = styles[key];
                    });
                }
            });
        }
    }
    
    // 修改初始化函数，确保在页面加载时正确加载答案
    function initializeAnswersFromStorage() {
        try {
            // 检查全局数据对象是否存在
            if (!window.globalData) {
                console.warn('[初始化答案] 全局数据对象未初始化，将在3秒后重试');
                setTimeout(initializeAnswersFromStorage, 3000);
                return;
            }

            // 防御性获取用户ID和考试ID
            const userId = window.globalData?.user?.id;
            const examId = window.globalData?.exam?.id;
            
            console.log('[初始化答案] 当前用户ID:', userId);
            console.log('[初始化答案] 当前考试ID:', examId);
            
            if (!userId || !examId) {
                console.warn('[初始化答案] 用户ID或考试ID未就绪，将在3秒后重试');
                setTimeout(initializeAnswersFromStorage, 3000);
                return;
            }
            
            const storageKey = `exam_${userId}_${examId}_answers`;
            console.log(`[初始化答案] 尝试从存储密钥加载: ${storageKey}`);
            
            try {
                const savedAnswers = localStorage.getItem(storageKey);
                
                if (!savedAnswers) {
                    console.log('[初始化答案] 本地存储中没有找到已保存的答案，初始化空数组');
                    examAnswers = [];
                    return;
                }
                
                const parsedAnswers = JSON.parse(savedAnswers);
                
                if (!Array.isArray(parsedAnswers)) {
                    console.error('[初始化答案] 存储的答案不是数组格式');
                    examAnswers = [];
                    return;
                }
                
                console.log('[初始化答案] 从本地存储加载已保存的答案');
                examAnswers = parsedAnswers;
                console.log('[初始化答案] 成功加载已保存答案：', examAnswers);
                
                // 同步到amis实例
                if (window.amisInstance) {
                    try {
                        const answersMap = {};
                        examAnswers.forEach(answer => {
                            if (answer && answer.questionId) {
                                answersMap[`question_${answer.questionId}`] = answer.answer;
                            }
                        });
                        
                        window.amisInstance.updateProps({
                            data: {
                                ...(window.amisInstance.props?.data || {}),
                                ...answersMap
                            }
                        });
                        console.log('[初始化答案] 已同步答案到amis实例');
                    } catch (amisError) {
                        console.error('[初始化答案] 同步到amis实例失败：', amisError);
                    }
                }
                
                // 检查备份存储
                try {
                    const backupKey = `exam_${userId}_${examId}_answers_backup`;
                    const backupAnswers = sessionStorage.getItem(backupKey);
                    if (backupAnswers) {
                        const parsedBackupAnswers = JSON.parse(backupAnswers);
                        if (Array.isArray(parsedBackupAnswers) && parsedBackupAnswers.length > examAnswers.length) {
                            console.log('[初始化答案] 从备份存储发现更多答案，使用备份数据');
                            examAnswers = parsedBackupAnswers;
                            // 再次同步到amis实例
                            if (window.amisInstance) {
                                const backupAnswersMap = {};
                                examAnswers.forEach(answer => {
                                    if (answer && answer.questionId) {
                                        backupAnswersMap[`question_${answer.questionId}`] = answer.answer;
                                    }
                                });
                                window.amisInstance.updateProps({
                                    data: {
                                        ...(window.amisInstance.props?.data || {}),
                                        ...backupAnswersMap
                                    }
                                });
                            }
                        }
                    }
                } catch (backupError) {
                    console.error('[初始化答案] 检查备份存储失败：', backupError);
                }
                
            } catch (parseError) {
                console.error('[初始化答案] 解析存储的答案失败：', parseError);
                examAnswers = [];
            }
        } catch (error) {
            console.error('[初始化答案] 初始化答案时发生错误：', error);
            // 设置默认值
            examAnswers = [];
        }
    }

    // 修改保存答案函数的部分代码
    function saveAnswer(questionId, answer) {
        try {
            console.log(`[保存答案] 开始保存题目 ${questionId} 的答案:`, answer);
            
            // 获取用户ID和考试ID
            const userId = window.globalData.user.id;
            const examId = window.globalData.exam.id;
            
            if (!userId) {
                console.error('[保存答案] 错误：未找到用户ID');
                return;
            }
            
            if (!examId) {
                console.error('[保存答案] 错误：未找到考试ID');
                return;
            }
            
            // 生成存储密钥
            const storageKey = `exam_${userId}_${examId}_answers`;
            console.log(`[保存答案] 使用存储密钥: ${storageKey}`);
            
            // 参数验证
            if (!questionId) {
                console.error('[保存答案] 错误：questionId 不能为空');
                return;
            }
            
            // 确保examAnswers是数组
            if (!Array.isArray(examAnswers)) {
                console.warn('[保存答案] examAnswers不是数组，正在初始化');
                examAnswers = [];
                // 尝试从存储中恢复
                initializeAnswersFromStorage();
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
            console.log('[保存答案] 更新后的完整答案列表:', JSON.parse(JSON.stringify(examAnswers)));
            
            // 保存到本地存储
            try {
                const serializedAnswers = JSON.stringify(examAnswers);
                
                // 在保存前验证序列化的数据
                if (serializedAnswers === '[]' || serializedAnswers === '{}') {
                    console.error('[保存答案] 错误：序列化后的答案为空');
                    return;
                }
                
                localStorage.setItem(storageKey, serializedAnswers);
                
                // 验证保存是否成功
                const savedData = localStorage.getItem(storageKey);
                const parsedSavedData = JSON.parse(savedData);
                console.log(`[保存答案] 本地存储验证 - 保存的答案数量: ${parsedSavedData.length}`);
                
                if (parsedSavedData.length !== examAnswers.length) {
                    console.error('[保存答案] 警告：保存的答案数量与当前答案数量不匹配');
                }
                
                console.log(`[保存答案] 成功保存到本地存储，键名：${storageKey}`);
            } catch (storageError) {
                console.error('[保存答案] 保存到本地存储失败：', storageError);
                // 尝试使用备用存储方案
                try {
                    const backupKey = `exam_${userId}_${examId}_answers_backup`;
                    sessionStorage.setItem(backupKey, JSON.stringify(examAnswers));
                    console.log('[保存答案] 已保存到会话存储作为备份');
                } catch (backupError) {
                    console.error('[保存答案] 备份存储也失败了：', backupError);
                }
            }
            
            // 修改同步到amis实例的部分
            if (window.amisInstance) {
                try {
                    // 创建更新数据对象
                    const updateData = {
                        [`question_${questionId}`]: processedAnswer
                    };
                    
                    // 获取当前的props数据
                    const currentData = window.amisInstance.props?.data || {};
                    
                    // 合并数据
                    const newData = {
                        ...currentData,
                        ...updateData
                    };
                    
                    // 更新props
                    window.amisInstance.updateProps({
                        data: newData
                    });
                    
                    console.log('[保存答案] 已同步到amis实例:', updateData);
                } catch (amisError) {
                    console.error('[保存答案] 同步到amis实例失败:', amisError);
                    // 同步失败不影响保存过程继续
                }
            }
            
            console.log(`[保存答案] 题目 ${questionId} 的答案保存完成`);
            
        } catch (error) {
            console.error('[保存答案] 保存过程中发生错误：', error);
            // 尝试进行错误恢复
            try {
                const userId = window.globalData?.user?.id;
                const examId = window.globalData?.exam?.id;
                
                if (!userId || !examId) {
                    console.error('[保存答案] 恢复失败：未找到用户ID或考试ID');
                    return;
                }
                
                const storageKey = `exam_${userId}_${examId}_answers`;
                const savedAnswers = localStorage.getItem(storageKey);
                if (savedAnswers) {
                    console.log('[保存答案] 从本地存储恢复答案');
                    const parsedAnswers = JSON.parse(savedAnswers);
                    if (Array.isArray(parsedAnswers) && parsedAnswers.length > 0) {
                        examAnswers = parsedAnswers;
                        console.log('[保存答案] 成功恢复答案');
                        
                        // 尝试重新同步到amis实例
                        if (window.amisInstance) {
                            try {
                                const answersMap = {};
                                examAnswers.forEach(answer => {
                                    if (answer && answer.questionId) {
                                        answersMap[`question_${answer.questionId}`] = answer.answer;
                                    }
                                });
                                
                                window.amisInstance.updateProps({
                                    data: {
                                        ...(window.amisInstance.props?.data || {}),
                                        ...answersMap
                                    }
                                });
                                console.log('[保存答案] 已重新同步所有答案到amis实例');
                            } catch (syncError) {
                                console.error('[保存答案] 重新同步到amis实例失败:', syncError);
                            }
                        }
                    }
                }
            } catch (recoveryError) {
                console.error('[保存答案] 恢复答案失败：', recoveryError);
            }
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
                
                // 根据后端返回的enableViewResult决定是否跳转到结果页面
                if (data.data && data.data.enableViewResult) {
                    // 允许查看结果，跳转到结果页面
                    window.location.href = `/client/exam/result/${recordId}`;
                } else {
                    // 不允许查看结果，显示提交成功页面
                    alert("考试提交成功！");
                    window.location.href = "/client/index";
                }
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
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="user-info">欢迎您，${name}</div>'
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
                                            allowedSwitchElements.forEach(el => {
                                                el.textContent = window.globalData.exam.allowedScreenSwitchCount.toString();
                                            });
                                        }
                                        
                                        // 显示调试信息
                                        console.log("全局数据已更新:", {
                                            exam: window.globalData.exam,
                                            recordId: window.globalData.exam.recordId,
                                            screenSwitch: {
                                                count: window.globalData.exam.screenSwitchCount,
                                                allowed: window.globalData.exam.allowedScreenSwitchCount
                                            }
                                        });
                                        
                                        // 启动计时器
                                        try {
                                            // 获取服务器返回的开始时间和考试时长
                                            const serverStartTime = event.data.startTime;
                                            const examDuration = event.data.duration;
                                            
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
                                        } catch (error) {
                                            console.error("调用计时器函数失败", error);
                                        }
                                        
                                        // 在考试数据加载后初始化切屏检测（延迟执行确保数据已加载）
                                        setTimeout(function() {
                                            console.log("延迟执行切屏检测初始化");
                                            if (typeof window.setupScreenSwitchDetection === 'function') {
                                                window.setupScreenSwitchDetection();
                                            } else {
                                                console.error("切屏检测函数未定义");
                                            }
                                        }, 2000);
                                    } catch (error) {
                                        console.error("处理考试数据时出错:", error);
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
                '--box-shadow': '0 4px 12px rgba(0,0,0,0.1)',
                '--fixed-header-height': 'auto', // 动态计算高度
                '--header-transition': 'all 0.3s ease', // 添加过渡效果
                '--urgent-color': '#f44336',
                '--very-urgent-color': '#d50000',
                '--extremely-urgent-color': '#b71c1c'
            },
            'body': {
                'background-color': '#f5f7fa',
                'font-family': '"PingFang SC", "Microsoft YaHei", sans-serif',
                'color': '#333',
                'padding-top': '0' // 将由spacer控制
            },
            // 固定头部容器
            '.fixed-header-container': {
                'position': 'fixed',
                'top': '0',
                'left': '0',
                'right': '0',
                'z-index': '1000',
                'background-color': '#fff',
                'box-shadow': '0 2px 8px rgba(0,0,0,0.15)',
                'width': '100%',
                'transition': 'var(--header-transition)'
            },
            // 压缩模式下的头部样式
            '.compact-header .client-header': {
                'padding': '4px 16px'
            },
            '.compact-header .student-profile-section': {
                'transform': 'scale(0.95)',
                'transform-origin': 'center top',
                'margin': '0'
            },
            '.compact-header .student-info-card': {
                'margin-bottom': '5px'
            },
            '.compact-header .student-info-body': {
                'padding': '4px 15px'
            },
            '.compact-header .header-status-container': {
                'padding': '2px 0',
                'margin-top': '0'
            },
            '.compact-header .exam-timer-container, .compact-header .screen-switch-counter': {
                'transform': 'scale(0.95)',
                'transform-origin': 'center center'
            },
            '.fixed-header-spacer': {
                'height': 'calc(var(--fixed-header-height) + 10px)', // 动态高度 + 额外空间
                'content': '""',
                'display': 'block',
                'transition': 'var(--header-transition)'
            },
            // 头部导航样式
            '.client-header': {
                'background-color': '#fff',
                'padding': '8px 16px',
                'border-bottom': '1px solid #eaeaea'
            },
            '.header-container': {
                'margin-bottom': '10px'
            },
            '.header-status-container': {
                'padding': '5px 0',
                'background-color': 'rgba(245, 247, 250, 0.8)',
                'border-radius': 'var(--border-radius)',
                'flex-wrap': 'wrap'
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
                'transition': 'all 0.3s ease',
                'margin-right': '10px'
            },
            '.user-info:hover': {
                'background-color': '#f0f2f5'
            },
            '.screen-switch-icon': {
                'font-size': '14px',
                'font-weight': '500',
                'color': '#f44336',
                'background-color': 'rgba(244, 67, 54, 0.1)',
                'padding': '8px 12px',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)',
                'transition': 'all 0.3s ease',
                'display': 'flex',
                'align-items': 'center',
                'cursor': 'help'
            },
            '.screen-switch-icon i': {
                'margin-right': '5px',
                'font-size': '14px'
            },
            '.screen-switch-icon:hover': {
                'background-color': 'rgba(244, 67, 54, 0.15)',
                'transform': 'translateY(-2px)',
                'box-shadow': '0 3px 6px rgba(0,0,0,0.1)'
            },
            '.screen-switch-value': {
                'font-weight': 'bold'
            },
            '.allowed-switch-value': {
                'opacity': '0.8'
            },
            // 计时器样式
            '.exam-timer-container': {
                'padding': '10px 18px',
                'background-color': '#fff',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 2px 8px rgba(0,0,0,0.08)',
                'border': '1px solid #f0f0f0',
                'transition': 'all 0.3s ease',
                'min-width': '180px',
                'text-align': 'center',
                'margin': '0 auto' // 居中显示
            },
            '.exam-timer': {
                'color': 'var(--danger-color)',
                'font-size': '20px',
                'font-weight': 'bold',
                'font-family': 'Consolas, monospace',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center'
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
            '.countdown-urgent': {
                'color': '#f44336 !important',
                'text-shadow': '0 0 5px rgba(244, 67, 54, 0.3)'
            },
            '.countdown-urgent::before': {
                'background-color': '#f44336',
                'animation': 'pulse 0.8s infinite'
            },
            '.countdown-final': {
                'font-size': '110% !important',
                'letter-spacing': '1px'
            },
            '.countdown-warn': {
                'color': '#ff9800 !important',
                'text-shadow': '0 0 3px rgba(255, 152, 0, 0.3)'
            },
            '.countdown-warn::before': {
                'background-color': '#ff9800',
                'animation': 'pulse 1.2s infinite'
            },
            // 最后5分钟倒计时样式
            '.final-countdown': {
                'background-color': 'rgba(244, 67, 54, 0.08)',
                'border': '2px solid var(--urgent-color)',
                'box-shadow': '0 0 15px rgba(244, 67, 54, 0.2)',
                'transform': 'scale(1.02)',
                'animation': 'pulse-shadow 2s infinite'
            },
            '.final-countdown.urgent': {
                'background-color': 'rgba(244, 67, 54, 0.1)'
            },
            '.final-countdown.very-urgent': {
                'background-color': 'rgba(213, 0, 0, 0.15)',
                'border-color': 'var(--very-urgent-color)',
                'box-shadow': '0 0 20px rgba(213, 0, 0, 0.25)',
                'animation': 'pulse-shadow 1.5s infinite'
            },
            '.final-countdown.extremely-urgent': {
                'background-color': 'rgba(183, 28, 28, 0.2)',
                'border-color': 'var(--extremely-urgent-color)',
                'box-shadow': '0 0 25px rgba(183, 28, 28, 0.3)',
                'animation': 'pulse-shadow 1s infinite'
            },
            // 最后倒计时全局样式
            '.final-countdown-active .fixed-header-container': {
                'border-bottom': '2px solid var(--urgent-color)'
            },
            // 倒计时提示框
            '.final-countdown-alert': {
                'position': 'fixed',
                'top': '50%',
                'left': '50%',
                'transform': 'translate(-50%, -50%) scale(0.9)',
                'background-color': 'rgba(0, 0, 0, 0.85)',
                'color': 'white',
                'padding': '20px 30px',
                'border-radius': '12px',
                'box-shadow': '0 5px 30px rgba(0, 0, 0, 0.5)',
                'z-index': '10000',
                'text-align': 'center',
                'min-width': '300px',
                'max-width': '80%',
                'opacity': '0',
                'transition': 'all 0.4s ease',
                'pointer-events': 'none'
            },
            '.final-countdown-alert.show': {
                'opacity': '1',
                'transform': 'translate(-50%, -50%) scale(1)'
            },
            '.final-countdown-alert.hide': {
                'opacity': '0',
                'transform': 'translate(-50%, -60%) scale(0.9)'
            },
            '.alert-content': {
                'display': 'flex',
                'flex-direction': 'column',
                'align-items': 'center',
                'gap': '10px'
            },
            '.alert-icon': {
                'font-size': '40px',
                'color': 'var(--urgent-color)',
                'margin-bottom': '10px',
                'animation': 'pulse 1s infinite'
            },
            '.alert-message': {
                'font-size': '18px',
                'font-weight': 'bold',
                'margin-bottom': '5px'
            },
            '.alert-timer': {
                'font-size': '26px',
                'font-family': 'Consolas, monospace',
                'font-weight': 'bold',
                'color': 'var(--urgent-color)',
                'background-color': 'rgba(255, 255, 255, 0.15)',
                'padding': '5px 15px',
                'border-radius': '20px',
                'margin-top': '5px'
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
            '@keyframes pulse-scale': {
                '0%': {
                    'transform': 'scale(1)'
                },
                '50%': {
                    'transform': 'scale(1.05)'
                },
                '100%': {
                    'transform': 'scale(1)'
                }
            },
            '@keyframes pulse-shadow': {
                '0%': {
                    'box-shadow': '0 0 10px rgba(244, 67, 54, 0.2)'
                },
                '50%': {
                    'box-shadow': '0 0 20px rgba(244, 67, 54, 0.5)'
                },
                '100%': {
                    'box-shadow': '0 0 10px rgba(244, 67, 54, 0.2)'
                }
            },
            '@keyframes highlight': {
                '0%': {
                    'transform': 'scale(1.1)',
                    'text-shadow': '0 0 8px rgba(244, 67, 54, 0.8)'
                },
                '100%': {
                    'transform': 'scale(1)',
                    'text-shadow': 'none'
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
                'border-radius': 'var(--border-radius)',
                'gap': '10px',
                'justify-content': 'center'
            },
            '.exam-info-item': {
                'margin-right': '20px',
                'margin-bottom': '10px',
                'padding': '8px 15px',
                'background-color': '#fff',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 2px 5px rgba(0,0,0,0.05)',
                'border-left': '3px solid var(--primary-color)',
                'font-weight': '500',
                'min-width': '160px',
                'text-align': 'center'
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
                    'padding': '10px',
                    'margin': '10px auto'
                },
                '.fixed-header-container': {
                    'position': 'fixed' // 确保在移动设备上也是固定的
                },
                '.fixed-header-spacer': {
                    'height': 'calc(var(--fixed-header-height) + 15px)' // 移动设备上增加一点空间
                },
                '.exam-panel': {
                    'margin-top': '0'
                },
                '.exam-info-panel': {
                    'padding': '10px'
                },
                '.exam-info': {
                    'padding': '10px',
                    'flex-direction': 'column'
                },
                '.question-label': {
                    'font-size': '15px',
                    'padding-left': '10px',
                    'line-height': '1.4',
                    'margin-bottom': '15px'
                },
                '.question-content': {
                    'padding-bottom': '10px',
                    'margin-bottom': '10px'
                },
                '.question-item': {
                    'padding': '15px',
                    'margin-bottom': '20px',
                    'box-shadow': '0 1px 5px rgba(0,0,0,0.03)'
                },
                '.question-type-tag, .question-score': {
                    'font-size': '11px',
                    'padding': '1px 6px'
                },
                '.am-RadioControl, .am-CheckboxControl': {
                    'padding': '8px 12px',
                    'margin-bottom': '8px'
                },
                '.am-RadioControl-label, .am-CheckboxControl-label': {
                    'font-size': '14px',
                    'line-height': '1.4'
                },
                '.option-label': {
                    'width': '22px',
                    'height': '22px',
                    'line-height': '22px',
                    'font-size': '12px',
                    'margin-right': '8px'
                },
                '.am-Button--primary, .am-Button--link': {
                    'font-size': '14px',
                    'padding': '8px 16px'
                },
                '.exam-actions': {
                    'margin-top': '20px',
                    'flex-wrap': 'wrap',
                    'gap': '10px'
                },
                '.client-header': {
                    'padding': '10px 15px'
                },
                '.user-info': {
                    'padding': '6px 10px',
                    'font-size': '14px',
                    'margin-right': '5px'
                },
                '.screen-switch-icon': {
                    'padding': '6px 8px',
                    'font-size': '12px'
                },
                '.screen-switch-icon i': {
                    'font-size': '12px'
                },
                '.header-container': {
                    'align-items': 'center'
                },
                '.header-status-container': {
                    'flex-direction': 'row',
                    'justify-content': 'center',
                    'gap': '10px'
                },
                '.client-logo span': {
                    'font-size': '18px'
                },
                '.exam-timer-container': {
                    'width': '100%',
                    'justify-content': 'center',
                    'margin': '5px auto'
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
                    'margin-bottom': '0',
                    'display': 'flex',
                    'align-items': 'center'
                },
                '.exam-title': {
                    'font-size': '20px'
                },
                '.student-info-card': {
                    'margin-bottom': '5px'
                },
                '.student-info-body > .am-Flex': {
                    'flex-wrap': 'wrap',
                    'gap': '5px',
                    'justify-content': 'flex-start'
                },
                '.student-info-item': {
                    'margin-right': '10px',
                    'margin-bottom': '5px',
                    'font-size': '12px'
                },
                '.final-countdown-alert': {
                    'min-width': '280px',
                    'padding': '15px 20px'
                },
                '.alert-message': {
                    'font-size': '16px'
                },
                '.alert-timer': {
                    'font-size': '22px'
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
            },
            '.screen-switch-counter': {
                'margin-left': '0',
                'min-width': '180px'
            },
            '.screen-switch-count': {
                'font-size': '14px',
                'font-weight': '500',
                'color': '#f44336',
                'background-color': 'rgba(244, 67, 54, 0.1)',
                'padding': '6px 12px',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)',
                'transition': 'all 0.3s ease',
                'display': 'flex',
                'align-items': 'center',
                'justify-content': 'center',
                'text-align': 'center'
            },
            '.screen-switch-count::before': {
                'content': '""',
                'display': 'inline-block',
                'width': '8px',
                'height': '8px',
                'background-color': '#f44336',
                'border-radius': '50%',
                'margin-right': '6px'
            },
            '.screen-switch-value': {
                'font-weight': 'bold',
                'margin-left': '4px'
            },
            '.allowed-switch-value': {
                'font-weight': 'normal',
                'opacity': '0.8'
            },
            // 考生信息样式
            '.student-profile-section': {
                'margin': '0 0 10px 0',
                'padding': '0'
            },
            '.student-info-card': {
                'background': 'linear-gradient(to right, rgba(63, 81, 181, 0.05), rgba(255, 255, 255, 0.8))',
                'border': 'none',
                'border-radius': 'var(--border-radius)',
                'box-shadow': '0 1px 3px rgba(0,0,0,0.05)',
                'margin-bottom': '10px'
            },
            '.student-info-body': {
                'padding': '8px 15px'
            },
            '.student-info-item': {
                'font-size': '14px',
                'margin-right': '15px',
                'white-space': 'nowrap'
            },
            '.student-info-item i': {
                'color': 'var(--primary-color)',
                'margin-right': '4px',
                'font-size': '14px'
            },
            '.student-name': {
                'font-weight': '500'
            },
            '.student-groups': {
                'display': 'inline-block',
                'background-color': 'rgba(63, 81, 181, 0.1)',
                'color': 'var(--primary-color)',
                'border-radius': '12px',
                'padding': '0 8px',
                'font-size': '12px'
            }
        }
    };

    // 注册用于提交考试的全局方法
    window.submitExam = submitExam;
    window.cancelExam = cancelExam;
    window.startExamTimer = startExamTimer;
    window.updateTimerDisplay = updateTimerDisplay;
    window.saveAnswer = saveAnswer;
    window.initializeAnswersFromStorage = initializeAnswersFromStorage;

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
                name: ''
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

    // 在考试页面加载完成后初始化答案
    document.addEventListener('DOMContentLoaded', function() {
        console.log("[页面加载] 文档已加载完成");
        
        // 初始化答案
        initializeAnswersFromStorage();
        
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
})(); 