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
            rules: []
        },
        countdown: {
            hours: 0,
            minutes: 0,
            seconds: 0,
            status: '',
            canStart: false
        }
    };
    
    let countdownTimer = null;
    
    /**
     * 通用API请求函数
     */
    async function apiRequest(url, options = {}) {
        try {
            const token = TokenManager.getToken();
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Authorization': token ? 'Bearer ' + token : '',
                    'TenantId': window.tenantId,
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });
            
            if (response.status === 401) {
                window.location.href = `/${window.tenantId}/exam/login`;
                throw new Error('认证失败，请重新登录');
            }
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            if (result.status !== 0) {
                throw new Error(result.msg || '请求失败');
            }
            
            return result.data;
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
     * 加载考试信息
     */
    async function loadExamInfo() {
        if (!window.examId) {
            throw new Error('缺少考试ID');
        }
        
        try {
            const data = await apiRequest(`/exam/api/exam/client/${window.examId}/info`);
            examData.exam = {
                id: data.id,
                title: data.title || '考试',
                description: data.description || '',
                duration: data.duration || 0,
                totalQuestions: data.totalQuestions || 0,
                totalScore: data.totalScore || 0,
                startTime: data.startTime ? new Date(data.startTime) : null,
                endTime: data.endTime ? new Date(data.endTime) : null,
                status: data.status || '',
                rules: data.rules || getDefaultRules()
            };
            
            // 计算倒计时
            updateCountdown();
        } catch (error) {
            console.error('加载考试信息失败:', error);
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
                rules: getDefaultRules()
            };
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
     * 更新倒计时
     */
    function updateCountdown() {
        if (!examData.exam.startTime) {
            examData.countdown.status = '暂未开始';
            examData.countdown.canStart = false;
            return;
        }
        
        const now = new Date();
        const startTime = examData.exam.startTime;
        const endTime = examData.exam.endTime;
        
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
                canStart: true
            };
        } else {
            // 考试已结束
            examData.countdown = {
                hours: 0,
                minutes: 0,
                seconds: 0,
                status: '考试已结束',
                canStart: false
            };
        }
        
        // 更新页面数据
        if (amisInstance) {
            amisInstance.updateData({
                countdown: examData.countdown,
                exam: examData.exam
            });
        }
    }
    
    /**
     * 开始倒计时
     */
    function startCountdownTimer() {
        if (countdownTimer) {
            clearInterval(countdownTimer);
        }
        
        countdownTimer = setInterval(() => {
            updateCountdown();
        }, 1000);
    }
    
    /**
     * 开始考试
     */
    function startExam() {
        if (!examData.countdown.canStart) {
            alert('考试尚未开始或已结束');
            return;
        }
        
        // 跳转到考试页面
        window.location.href = `/${window.tenantId}/exam/paper/${examData.exam.id}`;
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
                                    type: "container",
                                    className: "exam-info-grid",
                                    body: [
                                        {
                                            type: "tpl",
                                            tpl: "<div class='exam-info-item'><div class='label'>考试时长</div><div class='value'>${exam.duration}分钟</div></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='exam-info-item'><div class='label'>题目数量</div><div class='value'>${exam.totalQuestions}题</div></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='exam-info-item'><div class='label'>总分</div><div class='value'>${exam.totalScore}分</div></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='exam-info-item'><div class='label'>考生姓名</div><div class='value'>${student.displayName}</div></div>"
                                        }
                                    ]
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
    });
    
})(); 