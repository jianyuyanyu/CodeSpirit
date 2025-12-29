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
        // 保存当前页面URL用于登录后跳转
        const currentUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = `/${window.tenantId}/exam/login?redirect=${currentUrl}`;
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
    let isStarting = false; // 防重复点击标志
    
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
            
            // 如果错误是Error对象且包含消息，尝试提取原始响应数据
            // ExamApiManager.request 在业务错误时会抛出 Error(result.msg)
            // 但我们需要保留原始响应以便错误处理函数能识别特定错误类型
            if (error instanceof Error && error.message) {
                // 创建一个包含msg属性的错误对象，方便convertToFriendlyMessage处理
                const errorObj = {
                    msg: error.message,
                    message: error.message
                };
                // 保留原始error对象以便调试
                errorObj.originalError = error;
                throw errorObj;
            }
            
            throw error;
        }
    }
    
    /**
     * 加载租户信息（使用缓存）
     */
    async function loadTenantInfo() {
        try {
            const data = await window.ExamApiManager.getLoginConfig(window.tenantId);
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
     * 设置开始按钮加载状态
     */
    function setStartButtonLoading(loading) {
        const startBtn = document.getElementById('startExamBtn');
        if (!startBtn) return;
        
        if (loading) {
            startBtn.disabled = true;
            startBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>正在进入考试...';
            startBtn.style.opacity = '0.8';
            startBtn.style.cursor = 'wait';
        } else {
            startBtn.disabled = !examData.countdown.canStart;
            startBtn.innerHTML = '<i class="fa fa-play"></i>开始考试';
            startBtn.style.opacity = examData.countdown.canStart ? '1' : '0.6';
            startBtn.style.cursor = examData.countdown.canStart ? 'pointer' : 'not-allowed';
        }
    }
    
    /**
     * 将错误转换为友好的消息
     * @returns {Object} { title: '错误标题', message: '错误详情' }
     */
    function convertToFriendlyMessage(error, defaultTitle = '操作失败') {
        let friendlyTitle = defaultTitle;
        let friendlyMessage = '';
        
        // 获取错误消息（优先从error.msg获取，因为API返回的错误格式是 {status: -1, msg: "..."}）
        // 如果error是Error对象，则从error.message获取
        // 如果error是字符串，则直接使用
        let errorMsg = '';
        let originalMsg = '';
        
        if (typeof error === 'object' && error !== null) {
            // 如果是业务错误响应对象 {status: -1, msg: "..."}
            if (error.msg) {
                errorMsg = error.msg.toLowerCase();
                originalMsg = error.msg;
            } 
            // 如果是Error对象
            else if (error.message) {
                errorMsg = error.message.toLowerCase();
                originalMsg = error.message;
            }
            // 如果是其他对象，尝试转换为字符串
            else {
                originalMsg = String(error);
                errorMsg = originalMsg.toLowerCase();
            }
        } else {
            // 如果error是字符串或其他类型
            originalMsg = String(error || '');
            errorMsg = originalMsg.toLowerCase();
        }
        
        // 调试日志：输出原始错误信息（开发环境）
        if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
            console.log('🔍 [错误处理] 原始错误:', error);
            console.log('🔍 [错误处理] 提取的错误消息:', originalMsg);
        }
        
        // 优先检测网络相关的原生错误（如 Failed to fetch）
        if (errorMsg.includes('failed to fetch') || 
            errorMsg.includes('network error') || 
            errorMsg.includes('networkerror') ||
            errorMsg.includes('net::err')) {
            friendlyTitle = '网络连接失败';
            friendlyMessage = '无法连接到服务器，请检查您的网络连接后重试';
        } 
        // 超时错误
        else if (errorMsg.includes('timeout') || errorMsg.includes('timed out')) {
            friendlyTitle = '请求超时';
            friendlyMessage = '服务器响应超时，请检查网络状况后重试';
        }
        // CORS 跨域错误
        else if (errorMsg.includes('cors') || errorMsg.includes('cross-origin')) {
            friendlyTitle = '访问受限';
            friendlyMessage = '无法访问考试服务，请联系技术支持';
        }
        // 服务器错误（5xx）
        else if (errorMsg.includes('500') || errorMsg.includes('502') || 
                 errorMsg.includes('503') || errorMsg.includes('504') ||
                 errorMsg.includes('internal server error')) {
            friendlyTitle = '服务器繁忙';
            friendlyMessage = '服务器暂时无法处理请求，请稍后重试';
        }
        // 未授权错误（401, 403）
        else if (errorMsg.includes('401') || errorMsg.includes('403') || 
                 errorMsg.includes('unauthorized') || errorMsg.includes('forbidden')) {
            friendlyTitle = '身份验证失败';
            friendlyMessage = '您的登录状态已过期，请重新登录';
        }
        // 资源未找到（404）
        else if (errorMsg.includes('404') || errorMsg.includes('not found')) {
            friendlyTitle = '考试不存在';
            friendlyMessage = '未找到指定的考试信息，请确认考试链接是否正确';
        }
        // 缺少参数
        else if (originalMsg.includes('缺少') || originalMsg.includes('参数')) {
            friendlyTitle = '参数错误';
            friendlyMessage = originalMsg || '访问参数有误，请确认链接是否正确';
        }
        // 业务错误：考试已开始
        else if (originalMsg.includes('已经开始') || originalMsg.includes('已存在')) {
            friendlyTitle = '您已经开始了这场考试';
            friendlyMessage = '系统检测到您已有进行中的考试记录，请直接进入考试页面';
        } 
        // 业务错误：时间未到
        else if (originalMsg.includes('未开始') || originalMsg.includes('时间')) {
            friendlyTitle = '考试时间未到';
            friendlyMessage = '请等待考试开始时间后再试';
        } 
        // 业务错误：考试已结束
        else if (originalMsg.includes('已结束')) {
            friendlyTitle = '考试已结束';
            friendlyMessage = '很抱歉，该场考试已经结束';
        } 
        // 业务错误：已完成考试次数达到限制（优先检查，放在其他业务错误之前）
        // 检查完整的错误消息，支持多种表达方式
        else if (originalMsg.includes('已完成该考试') || 
                 originalMsg.includes('已完成') && originalMsg.includes('次') ||
                 originalMsg.includes('已达到允许的最大次数') || 
                 originalMsg.includes('达到允许的最大次数') ||
                 originalMsg.includes('无法重新开始考试') ||
                 originalMsg.includes('无法重新开始') ||
                 originalMsg.includes('超过允许的考试次数') ||
                 originalMsg.includes('超过允许次数')) {
            friendlyTitle = '考试次数已用完';
            // 直接使用原始消息，确保用户看到完整的错误信息
            friendlyMessage = originalMsg || '您已完成该考试，无法重新开始';
            
            // 调试日志
            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                console.log('✅ [错误处理] 识别为考试次数限制错误:', friendlyMessage);
            }
        } 
        // 业务错误：网络连接
        else if (originalMsg.includes('网络') || originalMsg.includes('连接')) {
            friendlyTitle = '网络连接失败';
            friendlyMessage = '请检查您的网络连接后重试';
        } 
        // 业务错误：权限问题
        else if (originalMsg.includes('权限') || originalMsg.includes('授权')) {
            friendlyTitle = '没有考试权限';
            friendlyMessage = '您可能没有参加此考试的权限，请联系管理员';
        } 
        // 有具体错误消息（中文错误）
        else if (originalMsg && /[\u4e00-\u9fa5]/.test(originalMsg)) {
            friendlyTitle = defaultTitle;
            friendlyMessage = originalMsg;
        }
        // 英文原生错误或其他未知错误
        else if (originalMsg) {
            friendlyTitle = '系统异常';
            friendlyMessage = '操作失败，请稍后重试或联系技术支持';
        } 
        // 完全没有错误信息
        else {
            friendlyTitle = '系统异常';
            friendlyMessage = '请稍后重试或联系技术支持';
        }
        
        return {
            title: friendlyTitle,
            message: friendlyMessage
        };
    }
    
    /**
     * 显示友好的错误提示（用于操作失败时的alert弹窗）
     */
    function showFriendlyError(error) {
        const friendly = convertToFriendlyMessage(error, '抱歉，无法开始考试');
        
        // 显示友好的提示对话框
        const alertMessage = friendly.message 
            ? `${friendly.title}\n\n${friendly.message}` 
            : friendly.title;
        
        alert(alertMessage);
        console.error('❌ 开始考试失败:', error.msg || error.message || error);
    }
    
    /**
     * 开始考试
     */
    async function startExam() {
        // 防重复点击
        if (isStarting) {
            console.warn('⚠️ 正在处理开始考试请求，请勿重复点击');
            return;
        }
        
        // 验证是否可以开始考试
        if (!examData.countdown.canStart) {
            alert('考试尚未开始或已结束\n\n请等待考试开始时间');
            return;
        }
        
        // 验证考试ID
        if (!examData.exam.id) {
            alert('系统错误\n\n无法获取考试信息，请刷新页面重试');
            return;
        }
        
        try {
            // 设置防重复标志
            isStarting = true;
            
            // 显示按钮加载状态
            setStartButtonLoading(true);
            console.log('🚀 开始考试，考试ID:', examData.exam.id);
            
            // 调用开始考试API创建考试记录（在整个请求过程中保持加载状态）
            const response = await apiRequest(`/exam/api/exam/client/${examData.exam.id}/start`, {
                method: 'POST'
            });
            
            console.log('✅ 考试记录创建成功:', response);
            
            // 获取考试ID（优先使用API返回值，否则使用本地数据）
            const examId = response.id || response.examId || examData.exam.id;
            
            // 更新按钮状态为正在跳转
            const startBtn = document.getElementById('startExamBtn');
            if (startBtn) {
                startBtn.innerHTML = '<i class="fa fa-spinner fa-spin"></i>正在跳转到考试页面...';
                console.log('🔄 更新按钮状态：正在跳转');
            }
            
            // 构建目标URL（使用考试ID）
            const targetUrl = `/${window.tenantId}/exam/${examId}`;
            console.log('🔄 跳转到考试页面:', targetUrl);
            
            // 跳转到考试页面（加载状态保持到页面跳转完成）
            window.location.href = targetUrl;
            
            // 注意：成功跳转后不需要恢复状态，因为页面会卸载
            // 如果页面没有跳转（极端情况），3秒后恢复状态
            setTimeout(() => {
                if (isStarting) {
                    console.warn('⚠️ 页面跳转超时，恢复按钮状态');
                    isStarting = false;
                    setStartButtonLoading(false);
                }
            }, 3000);
            
        } catch (error) {
            // 只有在发生错误时才恢复按钮状态
            console.error('❌ 开始考试失败，恢复按钮状态', error);
            isStarting = false;
            setStartButtonLoading(false);
            
            // 确保错误消息被正确提取和传递
            let errorToShow = error;
            
            // 如果是Error对象，提取message并创建包含msg属性的对象
            if (error instanceof Error) {
                errorToShow = {
                    msg: error.message,
                    message: error.message,
                    originalError: error
                };
            }
            // 如果错误对象没有msg或message，尝试从其他位置获取
            else if (error && typeof error === 'object') {
                if (!error.msg && !error.message) {
                    // 尝试从响应的data中获取
                    if (error.data && error.data.msg) {
                        errorToShow = { msg: error.data.msg, message: error.data.msg };
                    } else if (error.response && error.response.msg) {
                        errorToShow = { msg: error.response.msg, message: error.response.msg };
                    } else {
                        // 如果都没有，尝试从对象的任何属性中查找
                        const msgKey = Object.keys(error).find(key => 
                            key.toLowerCase() === 'msg' || 
                            key.toLowerCase() === 'message' ||
                            (typeof error[key] === 'string' && error[key].length > 0)
                        );
                        if (msgKey && typeof error[msgKey] === 'string') {
                            errorToShow = { msg: error[msgKey], message: error[msgKey] };
                        }
                    }
                } else {
                    // 确保既有msg也有message属性
                    if (error.msg && !error.message) {
                        errorToShow = { ...error, message: error.msg };
                    } else if (error.message && !error.msg) {
                        errorToShow = { ...error, msg: error.message };
                    }
                }
            }
            
            // 调试日志：输出最终的错误对象（仅在开发环境）
            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                console.log('🔍 [错误处理] 最终传递给showFriendlyError的错误对象:', errorToShow);
            }
            
            // 显示友好的错误提示
            showFriendlyError(errorToShow);
        }
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
                                    tpl: "<h2>${exam.title}</h2>"
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
                                            <li>本次考试时长<strong>\${exam.duration}分钟</strong>，请合理安排时间。请在安静环境下考试，严禁在机场、商场等公共场所进行</li>
                                            <li>考试开始后<strong>禁止退出系统</strong>，超过2次将<span class="text-danger">强制终止考试</span></li>
                                            <li>离开考试界面超过<strong>3秒</strong>将被记录并可能认定为作弊，超过2次将<span class="text-danger">强制终止考试</span></li>
                                            <li>考试前需关闭可能影响考试的软件（微信、QQ、飞书、钉钉、腾讯会议、电脑管家、VPN等）以免自动弹窗</li>
                                            <li>考试时间到后系统将<strong>自动提交试卷</strong>，请确保网络连接稳定</li>
                                            <li>考试过程中如遇技术问题请及时联系监考老师，<span class="text-danger">严禁作弊</span>，一经发现立即取消考试资格</li>
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
     * 显示错误信息（用于页面加载失败时的全屏错误页面）
     */
    function showError(error) {
        // 转换为友好的错误消息
        const friendly = convertToFriendlyMessage(error, '页面加载失败');
        
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
                                        <h3>${friendly.title}</h3>
                                        <p>${friendly.message || '请尝试刷新页面'}</p>
                                        <button class="exam-start-btn" onclick="window.location.reload()">
                                            <i class="fa fa-refresh"></i>重新加载
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
            
        } catch (error) {
            console.error('初始化页面失败:', error);
            showError(error);
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