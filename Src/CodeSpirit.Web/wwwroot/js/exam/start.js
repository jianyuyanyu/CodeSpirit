/**
 * 考试开始页面 - 基于AMIS框架
 * 包含考试信息加载、倒计时、考试开始等功能
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
    
    let amis = amisRequire('amis/embed');
    let amisInstance = null;
    
    // 考试数据
    const examData = {
        tenant: { id: '', name: '', logo: '' },
        student: { name: '', displayName: '', examNumber: '', avatar: '' },
        exam: {
            id: window.examId || '',
            title: '',
            description: '',
            duration: 0,
            totalQuestions: 0,
            totalScore: 0,
            startTime: null,
            endTime: null,
            status: '',
            canStart: false,
            rules: []
        },
        countdown: {
            hours: 0,
            minutes: 0,
            seconds: 0,
            status: '',
            canStart: false
        },
        serverTimeDiff: 0 // 客户端与服务器的时间差（毫秒）
    };
    
    let countdownTimer = null;
    let previousCountdown = null; // 用于检测数字变化
    let timeSyncTimer = null; // 时间同步定时器
    
    /**
     * 通用API请求函数
     * 使用ExamApiManager进行统一的API请求处理
     */
    async function apiRequest(url, options = {}) {
        try {
            const response = await window.ExamApiManager.request(url, options);
            
            // 特殊处理：对于start API，如果返回的不是标准格式，直接返回
            if (url.includes('/start') && response && typeof response === 'object' && !response.hasOwnProperty('status')) {
                return response;
            }
            
            return response;
        } catch (error) {
            console.error(`API请求失败 [${url}]:`, error);
            throw error;
        }
    }
    
    /**
     * 加载租户信息
     */
    async function loadTenantInfo() {
        try {
            const data = await apiRequest(`/identity/api/identity/tenants/${window.tenantId}/login-config`);
            examData.tenant = {
                id: data.tenantId || window.tenantId,
                name: data.tenantName || '考试平台',
                logo: data.logoUrl || ''
            };
        } catch (error) {
            console.warn('加载租户信息失败:', error);
            examData.tenant = {
                id: window.tenantId,
                name: '考试平台',
                logo: ''
            };
        }
    }
    
    /**
     * 加载考生信息
     */
    async function loadStudentInfo() {
        try {
            const data = await apiRequest('/identity/api/identity/profile');
            examData.student = {
                name: data.name || data.displayName || '考生',
                displayName: data.displayName || data.name || '考生',
                examNumber: data.examNumber || data.candidateNumber || '',
                avatar: data.avatar || data.profilePicture || ''
            };
        } catch (error) {
            console.warn('加载考生信息失败:', error);
            examData.student = {
                name: '考生',
                displayName: '考生',
                examNumber: '',
                avatar: ''
            };
        }
    }
    
    /**
     * 加载考试信息（使用轻量级接口）
     */
    async function loadExamInfo() {
        if (!window.examId) {
            throw new Error('缺少考试ID');
        }
        
        try {
            console.log('🔍 正在加载考试轻量信息，考试ID:', window.examId);
            const data = await apiRequest(`/exam/api/exam/client/${window.examId}/light`);
            console.log('✅ 考试轻量信息获取成功:', data);
            
            // 计算客户端与服务器的时间差
            const serverTime = new Date(data.serverTime);
            const clientTime = new Date();
            examData.serverTimeDiff = serverTime.getTime() - clientTime.getTime();
            console.log('⏰ 服务器时间差:', examData.serverTimeDiff, 'ms');
            
            examData.exam = {
                id: data.id,
                title: data.name || '考试',
                description: data.description || '',
                duration: data.duration || 0,
                totalQuestions: data.questionCount || 0,
                totalScore: data.totalScore || 0,
                startTime: data.startTime ? new Date(data.startTime) : null,
                endTime: data.endTime ? new Date(data.endTime) : null,
                status: data.status || 'pending',
                canStart: data.canStart || false,
                rules: getDefaultRules()
            };
            
            console.log('📋 处理后的考试信息:', examData.exam);
            
            // 计算倒计时
            updateCountdown();
        } catch (error) {
            console.error('❌ 加载考试轻量信息失败:', error);
            // 使用模拟数据
            examData.exam = {
                id: window.examId || 'demo',
                title: '模拟考试',
                description: '这是一个模拟考试，用于测试系统功能',
                duration: 90,
                totalQuestions: 20,
                totalScore: 100,
                startTime: new Date(Date.now() + 5 * 60 * 1000), // 5分钟后开始
                endTime: new Date(Date.now() + 95 * 60 * 1000), // 95分钟后结束
                status: 'pending',
                canStart: false,
                rules: getDefaultRules()
            };
            examData.serverTimeDiff = 0;
            updateCountdown();
        }
    }
    
    /**
     * 获取默认考试规则
     */
    function getDefaultRules() {
        return [
            '考试开始前请仔细阅读考试规则和注意事项',
            '考试过程中请保持安静，不得交头接耳',
            '考试时间到后系统将自动提交试卷',
            '考试过程中如遇技术问题请及时联系监考老师',
            '严禁作弊，一经发现立即取消考试资格',
            '请确保网络连接稳定，避免因网络问题影响考试',
            '考试结束后请等待系统提示再离开考场'
        ];
    }
    

    
    /**
     * 更新倒计时（使用服务器时间校正）
     */
    function updateCountdown() {
        if (!examData.exam.startTime) {
            examData.countdown.status = '暂未开始';
            examData.countdown.canStart = false;
            return;
        }
        
        // 使用服务器时间校正后的当前时间
        const now = new Date(Date.now() + examData.serverTimeDiff);
        const startTime = examData.exam.startTime;
        const endTime = examData.exam.endTime;
        
        // 保存之前的倒计时数据
        const prevCountdown = previousCountdown ? { ...previousCountdown } : null;
        
        if (now < startTime) {
            // 考试未开始
            const diff = startTime - now;
            const hours = Math.floor(diff / (1000 * 60 * 60));
            const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((diff % (1000 * 60)) / 1000);
            
            examData.countdown = {
                hours: hours,
                minutes: minutes,
                seconds: seconds,
                status: '距离考试开始还有',
                canStart: false
            };
        } else if (now >= startTime && now < endTime) {
            // 考试进行中
            examData.countdown = {
                hours: 0,
                minutes: 0,
                seconds: 0,
                status: '考试进行中，可以开始答题',
                canStart: true // 考试时间内可以开始
            };
            // 更新考试状态
            examData.exam.canStart = true;
            examData.exam.status = 'inProgress';
        } else {
            // 考试已结束
            examData.countdown = {
                hours: 0,
                minutes: 0,
                seconds: 0,
                status: '考试已结束',
                canStart: false
            };
            // 更新考试状态
            examData.exam.canStart = false;
            examData.exam.status = 'ended';
        }
        
        // 检测数字变化并添加动画效果
        if (prevCountdown) {
            const changedUnits = [];
            if (prevCountdown.hours !== examData.countdown.hours) changedUnits.push('hours');
            if (prevCountdown.minutes !== examData.countdown.minutes) changedUnits.push('minutes');
            if (prevCountdown.seconds !== examData.countdown.seconds) changedUnits.push('seconds');
            
            // 为变化的数字添加动画效果
            if (changedUnits.length > 0) {
                setTimeout(() => {
                    addCountdownAnimation(changedUnits);
                }, 100);
            }
        }
        
        // 更新页面数据
        if (amisInstance) {
            try {
                // 使用updateProps更新数据
                amisInstance.updateProps({
                    data: {
                        countdown: examData.countdown,
                        exam: examData.exam,
                        tenant: examData.tenant,
                        student: examData.student
                    }
                });
            } catch (error) {
                console.warn('更新AMIS数据失败:', error);
            }
        }
        
        // 更新开始按钮状态
        setTimeout(() => {
            updateStartButtonState();
            updateCountdownTimeState();
        }, 100);
        
        // 保存当前倒计时数据用于下次比较
        previousCountdown = { ...examData.countdown };
    }
    
    /**
     * 为倒计时数字添加变化动画效果
     */
    function addCountdownAnimation(changedUnits) {
        changedUnits.forEach((unit, index) => {
            const unitIndex = ['hours', 'minutes', 'seconds'].indexOf(unit);
            if (unitIndex !== -1) {
                setTimeout(() => {
                    const countdownUnit = document.querySelector(`.countdown-timer .countdown-unit:nth-child(${unitIndex + 1})`);
                    if (countdownUnit) {
                        countdownUnit.classList.add('changing');
                        setTimeout(() => {
                            countdownUnit.classList.remove('changing');
                        }, 600);
                    }
                }, index * 100);
            }
        });
    }

    /**
     * 更新开始按钮状态
     */
    function updateStartButtonState() {
        const startBtn = document.getElementById('startExamBtn');
        if (!startBtn) return;
        
        const canStart = examData.countdown.canStart;
        const isTimeToStart = examData.countdown.hours === 0 && 
                             examData.countdown.minutes === 0 && 
                             examData.countdown.seconds === 0 && 
                             examData.exam.status !== 'ended';
        
        // 更新按钮状态
        if (canStart || isTimeToStart) {
            startBtn.disabled = false;
            startBtn.textContent = '开始考试';
            startBtn.style.opacity = '1';
            startBtn.style.cursor = 'pointer';
            console.log('✅ 开始按钮已启用');
        } else {
            startBtn.disabled = true;
            startBtn.textContent = '等待开始';
            startBtn.style.opacity = '0.6';
            startBtn.style.cursor = 'not-allowed';
        }
    }

    /**
     * 根据剩余时间更新倒计时颜色状态
     */
    function updateCountdownTimeState() {
        const countdownTimer = document.querySelector('.countdown-timer');
        if (!countdownTimer) return;

        let newTimeState = '';
        let stateDescription = '';

        // 如果考试已经开始
        if (examData.countdown.canStart) {
            newTimeState = 'time-started';
            stateDescription = '考试进行中';
        } else {
            // 计算总剩余秒数
            const totalSeconds = examData.countdown.hours * 3600 + 
                               examData.countdown.minutes * 60 + 
                               examData.countdown.seconds;

            // 根据剩余时间确定状态
            if (totalSeconds <= 30) {
                newTimeState = 'time-critical';
                stateDescription = '危险状态 (≤30秒)';
            } else if (totalSeconds <= 60) {
                newTimeState = 'time-urgent';
                stateDescription = '紧急状态 (≤1分钟)';
            } else if (totalSeconds <= 300) {
                newTimeState = 'time-warning';
                stateDescription = '警告状态 (≤5分钟)';
            } else {
                newTimeState = 'time-normal';
                stateDescription = '正常状态 (>5分钟)';
            }
        }

        // 检查状态是否需要更新
        const currentStateClass = Array.from(countdownTimer.classList)
            .find(cls => cls.startsWith('time-'));

        if (currentStateClass !== newTimeState) {
            // 移除所有时间状态类
            countdownTimer.classList.remove('time-normal', 'time-warning', 'time-urgent', 'time-critical', 'time-started');
            
            // 添加新状态类
            countdownTimer.classList.add(newTimeState);
            
            console.log(`🎨 倒计时状态切换: ${currentStateClass || '无'} → ${newTimeState} (${stateDescription})`);
        }
    }
    
    /**
     * 同步服务器时间
     */
    async function syncServerTime() {
        try {
            console.log('🔄 同步服务器时间...');
            const data = await apiRequest(`/exam/api/exam/client/${window.examId}/light`);
            
            const serverTime = new Date(data.serverTime);
            const clientTime = new Date();
            const newTimeDiff = serverTime.getTime() - clientTime.getTime();
            
            // 如果时间差变化超过5秒，则更新时间差
            if (Math.abs(newTimeDiff - examData.serverTimeDiff) > 5000) {
                console.log('⏰ 更新服务器时间差:', examData.serverTimeDiff, '->', newTimeDiff);
                examData.serverTimeDiff = newTimeDiff;
                
                // 更新考试状态
                examData.exam.status = data.status;
                examData.exam.canStart = data.canStart;
            }
        } catch (error) {
            console.warn('⚠️ 同步服务器时间失败:', error);
        }
    }

    /**
     * 开始倒计时和时间同步
     */
    function startCountdownTimer() {
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }
        if (timeSyncTimer) {
            clearInterval(timeSyncTimer);
        }
        
        // 每秒更新倒计时
        countdownTimer = setInterval(() => {
            updateCountdown();
        }, 1000);
        
        // 每30秒同步一次服务器时间
        timeSyncTimer = setInterval(() => {
            syncServerTime();
        }, 30000);
    }
    
    /**
     * 开始考试
     */
    function startExam() {
        if (!examData.countdown.canStart) {
            alert('考试尚未开始或已结束');
            return;
        }
        
        try {
            console.log('🚀 开始考试，考试ID:', examData.exam.id);
            
            // 首先调用开始考试API创建考试记录
            apiRequest(`/exam/api/exam/client/${examData.exam.id}/start`, {
                method: 'POST'
            }).then(response => {
                console.log('✅ 考试记录创建成功:', response);
                
                // 无论response格式如何，都跳转到考试页面
                const examId = response.id || response.examId || examData.exam.id;
                const targetUrl = `/${window.tenantId}/exam/${examId}`;
                
                console.log('🔄 跳转到考试页面:', targetUrl);
                window.location.href = targetUrl;
                
            }).catch(error => {
                console.error('❌ 开始考试失败:', error);
                // 优先显示服务器返回的具体错误信息
                const errorMessage = error.msg || error.message || '未知错误';
                alert('开始考试失败：' + errorMessage);
            });
        } catch (error) {
            console.error('💥 开始考试出错:', error);
            alert('开始考试失败，请重试');
        }
    }
    
    /**
     * 返回应用首页
     */
    function goBack() {
        window.location.href = `/${window.tenantId}/exam/app`;
    }
    
    /**
     * 构建AMIS页面配置
     */
    function buildPageConfig() {
        return {
            type: "page",
            title: "",
            className: "exam-start-page",
            data: {
                tenant: examData.tenant,
                student: examData.student,
                exam: examData.exam,
                countdown: examData.countdown
            },
            body: [
                {
                    type: "container",
                    className: "exam-prepare-card",
                    body: [
                        // 头部
                        {
                            type: "container",
                            className: "exam-prepare-header",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<h2>${exam.title}</h2><p>${exam.description}</p>"
                                }
                            ]
                        },
                        
                        // 考试信息
                        {
                            type: "container",
                            className: "exam-info-section",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<div class='exam-info-title'><i class='fa fa-info-circle'></i>考试信息</div>",
                                    className: "exam-info-title"
                                },
                                {
                                    type: "html",
                                    html: `
                                        <div class="exam-info-grid">
                                            <div class='exam-info-item'>
                                                <div class='label'>考试时长</div>
                                                <div class='value'>\${exam.duration}分钟</div>
                                            </div>
                                            <div class='exam-info-item'>
                                                <div class='label'>题目数量</div>
                                                <div class='value'>\${exam.totalQuestions}题</div>
                                            </div>
                                            <div class='exam-info-item'>
                                                <div class='label'>总分</div>
                                                <div class='value'>\${exam.totalScore}分</div>
                                            </div>
                                            <div class='exam-info-item'>
                                                <div class='label'>考生姓名</div>
                                                <div class='value'>\${student.displayName}</div>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        },
                        
                        // 倒计时
                        {
                            type: "container",
                            className: "countdown-section",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<div class='countdown-title'>${countdown.status}</div>"
                                },
                                {
                                    type: "container",
                                    className: "countdown-display",
                                    visibleOn: "${countdown.hours > 0 || countdown.minutes > 0 || countdown.seconds > 0}",
                                    body: [
                                        {
                                            type: "container",
                                            className: "countdown-timer",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='countdown-unit'><span class='number'>${countdown.hours}</span><div class='label'>时</div></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='countdown-unit'><span class='number'>${countdown.minutes}</span><div class='label'>分</div></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='countdown-unit'><span class='number'>${countdown.seconds}</span><div class='label'>秒</div></div>"
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        
                        // 考试规则
                        {
                            type: "container",
                            className: "exam-rules-section",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<div class='exam-rules-title'><i class='fa fa-exclamation-triangle'></i>考试规则</div>"
                                },
                                {
                                    type: "html",
                                    html: `
                                        <ul class="exam-rules-list">
                                            <li>考试开始前请仔细阅读考试规则和注意事项</li>
                                            <li>考试过程中请保持安静，不得交头接耳</li>
                                            <li>考试时间到后系统将自动提交试卷</li>
                                            <li>考试过程中如遇技术问题请及时联系监考老师</li>
                                            <li>严禁作弊，一经发现立即取消考试资格</li>
                                            <li>请确保网络连接稳定，避免因网络问题影响考试</li>
                                            <li>考试结束后请等待系统提示再离开考场</li>
                                        </ul>
                                    `
                                }
                            ]
                        },
                        
                        // 操作按钮
                        {
                            type: "container",
                            className: "exam-actions",
                            body: [
                                {
                                    type: "html",
                                    html: `
                                        <button class="back-btn" onclick="window.goBack()">
                                            <i class="fa fa-arrow-left"></i>返回
                                        </button>
                                        <button class="exam-start-btn" onclick="window.startExam()" 
                                                id="startExamBtn" 
                                                \${countdown.canStart ? '' : 'disabled'}>
                                            <i class="fa fa-play"></i>开始考试
                                        </button>
                                    `
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
    
    /**
     * 显示错误信息
     */
    function showError(message) {
        const errorConfig = {
            type: "page",
            body: [
                {
                    type: "container",
                    className: "exam-error",
                    body: [
                        {
                            type: "container",
                            className: "error-content",
                            body: [
                                {
                                    type: "html",
                                    html: `
                                        <div class="error-icon">
                                            <i class="fa fa-exclamation-triangle"></i>
                                        </div>
                                        <h3>加载失败</h3>
                                        <p>${message}</p>
                                        <button class="exam-start-btn" onclick="window.location.reload()">
                                            <i class="fa fa-refresh"></i>重新加载
                                        </button>
                                        <button class="back-btn" onclick="window.goBack()">
                                            <i class="fa fa-arrow-left"></i>返回首页
                                        </button>
                                    `
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        
        showLoading(false);
        amisInstance = amis.embed('#root', errorConfig);
    }
    
    /**
     * 显示/隐藏加载状态
     */
    function showLoading(show) {
        const loadingEl = document.getElementById('loading');
        if (loadingEl) {
            loadingEl.style.display = show ? 'flex' : 'none';
        }
    }
    
    /**
     * 初始化页面
     */
    async function initPage() {
        try {
            showLoading(true);
            
            // 检查是否有考试ID
            if (!window.examId) {
                throw new Error('缺少考试ID参数');
            }
            
            // 并行加载数据
            await Promise.all([
                loadTenantInfo(),
                loadStudentInfo(),
                loadExamInfo()
            ]);
            
            // 构建页面
            const pageConfig = buildPageConfig();
            showLoading(false);
            amisInstance = amis.embed('#root', pageConfig);
            
            // 开始倒计时
            startCountdownTimer();
            
            // 初始化按钮状态
            setTimeout(() => {
                updateStartButtonState();
                updateCountdownTimeState();
            }, 500);
            
            // 绑定全局函数
            window.startExam = startExam;
            window.goBack = goBack;
            
        } catch (error) {
            console.error('初始化页面失败:', error);
            showError(error.message || '页面加载失败');
        }
    }
    
    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPage);
    } else {
        initPage();
    }
    
    // 页面卸载时清理
    window.addEventListener('beforeunload', () => {
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }
        if (timeSyncTimer) {
            clearInterval(timeSyncTimer);
        }
    });
    
})(); 