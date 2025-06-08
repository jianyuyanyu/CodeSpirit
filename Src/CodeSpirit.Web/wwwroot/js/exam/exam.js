/**
 * 考试主模块
 * 处理考试页面的核心功能：题目加载、答题、提交等
 */

(function(window) {
    'use strict';

    /**
     * 考试管理器类
     */
    class ExamManager {
        constructor(options = {}) {
            this.options = {
                tenantId: options.tenantId || window.tenantId,
                examId: options.examId || window.examId,
                autoSave: true,
                autoSaveInterval: 30000, // 30秒
                timeWarningThreshold: 300, // 5分钟
                debug: false,
                ...options
            };

            // 考试数据
            this.examData = null;
            this.questions = [];
            this.answers = {};
            this.currentQuestionIndex = 0;
            this.startTime = null;
            this.endTime = null;
            this.timeRemaining = 0;

            // 计时器
            this.timerInterval = null;
            this.autoSaveInterval = null;

            // 状态管理
            this.isSubmitting = false;
            this.isLoading = false;
            this.hasUnsavedChanges = false;

            this.init();
        }

        /**
         * 初始化考试管理器
         */
        async init() {
            try {
                this.showLoading(true);
                
                // 验证必要参数
                if (!this.options.tenantId || !this.options.examId) {
                    throw new Error('缺少必要的参数：tenantId 或 examId');
                }

                // 加载考试数据
                await this.loadExamData();
                
                // 初始化AMIS
                this.initAmis();
                
                // 开始计时
                this.startTimer();
                
                // 启动自动保存
                if (this.options.autoSave) {
                    this.startAutoSave();
                }

                // 绑定事件
                this.bindEvents();

                this.showLoading(false);
                this.logDebug('考试管理器初始化完成');

            } catch (error) {
                console.error('考试初始化失败:', error);
                this.showError('考试加载失败，请刷新页面重试');
            }
        }

        /**
         * 加载考试数据
         */
        async loadExamData() {
            try {
                const token = window.TokenManager ? window.TokenManager.getToken() : null;
                const response = await fetch(`/exam/api/exam/client/${this.options.examId}`, {
                    headers: {
                        'Authorization': token ? `Bearer ${token}` : '',
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                this.examData = await response.json();
                this.questions = this.examData.questions || [];
                this.timeRemaining = this.examData.duration * 60; // 转换为秒
                this.startTime = new Date();

                this.logDebug('考试数据加载完成', this.examData);

            } catch (error) {
                console.error('加载考试数据失败:', error);
                throw error;
            }
        }

        /**
         * 初始化AMIS
         */
        initAmis() {
            const schema = this.buildAmisSchema();
            
            // 渲染AMIS页面
            const amisScoped = amis.embed('#root', schema, {
                locale: 'zh-CN',
                theme: 'antd'
            });

            this.amisScoped = amisScoped;
            
            // 监听AMIS事件
            amisScoped.on('change', (event) => {
                this.handleAnswerChange(event);
            });

            amisScoped.on('action', (event) => {
                this.handleAction(event);
            });
        }

        /**
         * 构建AMIS页面结构
         */
        buildAmisSchema() {
            return {
                type: 'page',
                className: 'exam-content',
                body: [
                    // 考试头部
                    this.buildExamHeader(),
                    // 考试状态栏
                    this.buildStatusBar(),
                    // 主要内容区域
                    {
                        type: 'grid',
                        className: 'exam-main',
                        columns: [
                            // 题目区域
                            {
                                md: 8,
                                body: this.buildQuestionsPanel()
                            },
                            // 侧边栏
                            {
                                md: 4,
                                body: this.buildSidebar()
                            }
                        ]
                    }
                ]
            };
        }

        /**
         * 构建考试头部
         */
        buildExamHeader() {
            return {
                type: 'panel',
                className: 'exam-header',
                body: [
                    {
                        type: 'html',
                        html: `
                            <h1 class="exam-title">${this.examData.title || '在线考试'}</h1>
                            <div class="exam-info">
                                <div class="exam-info-item">
                                    <svg class="icon" viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"/></svg>
                                    总题数：${this.questions.length}题
                                </div>
                                <div class="exam-info-item">
                                    <svg class="icon" viewBox="0 0 24 24"><path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z"/><path d="M12.5 7H11v6l5.25 3.15.75-1.23-4.5-2.67z"/></svg>
                                    考试时长：${this.examData.duration}分钟
                                </div>
                                <div class="exam-info-item">
                                    <svg class="icon" viewBox="0 0 24 24"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
                                    总分：${this.examData.totalScore || 100}分
                                </div>
                            </div>
                        `
                    }
                ]
            };
        }

        /**
         * 构建状态栏
         */
        buildStatusBar() {
            return {
                type: 'panel',
                className: 'exam-status-bar',
                body: [
                    {
                        type: 'grid',
                        columns: [
                            {
                                body: {
                                    type: 'html',
                                    html: '<div class="exam-time-remaining">剩余时间：<span id="exam-timer" class="exam-timer">--:--:--</span></div>'
                                }
                            },
                            {
                                body: {
                                    type: 'html',
                                    html: '<div class="exam-progress">已完成：<span id="exam-progress-text">0/0</span></div>'
                                }
                            },
                            {
                                body: [
                                    {
                                        type: 'button',
                                        label: '暂存答案',
                                        actionType: 'save',
                                        className: 'exam-btn exam-btn-secondary',
                                        icon: 'fa fa-save'
                                    },
                                    {
                                        type: 'button',
                                        label: '提交考试',
                                        actionType: 'submit',
                                        className: 'exam-btn exam-btn-danger',
                                        icon: 'fa fa-check',
                                        confirmText: '确定要提交考试吗？提交后不能再修改答案。'
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };
        }

        /**
         * 构建题目面板
         */
        buildQuestionsPanel() {
            return {
                type: 'panel',
                className: 'exam-questions',
                body: [
                    {
                        type: 'wizard',
                        mode: 'horizontal',
                        className: 'exam-wizard',
                        steps: this.questions.map((question, index) => ({
                            title: `第${index + 1}题`,
                            body: this.buildQuestionForm(question, index)
                        }))
                    }
                ]
            };
        }

        /**
         * 构建单个题目表单
         */
        buildQuestionForm(question, index) {
            const fields = [];

            // 题目标题和描述
            fields.push({
                type: 'html',
                html: `
                    <div class="question-header">
                        <h3 class="question-title">第${index + 1}题 (${question.score || 10}分)</h3>
                        <div class="question-content">${question.content || question.title}</div>
                    </div>
                `
            });

            // 根据题目类型添加不同的输入控件
            switch (question.type) {
                case 'single':
                    fields.push({
                        type: 'radios',
                        name: `answer_${question.id}`,
                        options: question.options || [],
                        required: true
                    });
                    break;

                case 'multiple':
                    fields.push({
                        type: 'checkboxes',
                        name: `answer_${question.id}`,
                        options: question.options || [],
                        required: true
                    });
                    break;

                case 'essay':
                    fields.push({
                        type: 'textarea',
                        name: `answer_${question.id}`,
                        placeholder: '请输入您的答案...',
                        required: true,
                        minRows: 5,
                        maxRows: 15
                    });
                    break;

                case 'blank':
                    fields.push({
                        type: 'input-text',
                        name: `answer_${question.id}`,
                        placeholder: '请填写答案',
                        required: true
                    });
                    break;

                default:
                    fields.push({
                        type: 'html',
                        html: '<p class="text-danger">不支持的题目类型</p>'
                    });
            }

            return {
                type: 'form',
                body: fields,
                actions: []
            };
        }

        /**
         * 构建侧边栏
         */
        buildSidebar() {
            return [
                // 题目导航
                {
                    type: 'panel',
                    className: 'question-nav',
                    title: '题目导航',
                    body: {
                        type: 'html',
                        html: this.buildQuestionNavHtml()
                    }
                },
                // 考试统计
                {
                    type: 'panel',
                    className: 'exam-stats',
                    title: '答题统计',
                    body: {
                        type: 'html',
                        html: `
                            <div class="exam-stats-item">
                                <span class="exam-stats-label">总题数</span>
                                <span class="exam-stats-value">${this.questions.length}</span>
                            </div>
                            <div class="exam-stats-item">
                                <span class="exam-stats-label">已答题数</span>
                                <span class="exam-stats-value answered" id="answered-count">0</span>
                            </div>
                            <div class="exam-stats-item">
                                <span class="exam-stats-label">未答题数</span>
                                <span class="exam-stats-value unanswered" id="unanswered-count">${this.questions.length}</span>
                            </div>
                            <div class="exam-stats-item">
                                <span class="exam-stats-label">标记题数</span>
                                <span class="exam-stats-value marked" id="marked-count">0</span>
                            </div>
                        `
                    }
                }
            ];
        }

        /**
         * 构建题目导航HTML
         */
        buildQuestionNavHtml() {
            let html = '<div class="question-nav-grid">';
            for (let i = 0; i < this.questions.length; i++) {
                const isAnswered = this.answers[this.questions[i].id] !== undefined;
                const classes = ['question-nav-item'];
                
                if (i === this.currentQuestionIndex) classes.push('current');
                if (isAnswered) classes.push('answered');
                
                html += `<div class="${classes.join(' ')}" data-question-index="${i}" onclick="examManager.goToQuestion(${i})">${i + 1}</div>`;
            }
            html += '</div>';
            return html;
        }

        /**
         * 处理答案变化
         */
        handleAnswerChange(event) {
            const { name, value } = event.data;
            
            if (name && name.startsWith('answer_')) {
                const questionId = name.replace('answer_', '');
                this.answers[questionId] = value;
                this.hasUnsavedChanges = true;
                
                this.updateQuestionNavigation();
                this.updateStatistics();
                
                this.logDebug('答案已更新', { questionId, value });
            }
        }

        /**
         * 处理操作事件
         */
        handleAction(event) {
            const { actionType } = event;
            
            switch (actionType) {
                case 'save':
                    this.saveAnswers();
                    break;
                case 'submit':
                    this.submitExam();
                    break;
                default:
                    this.logDebug('未处理的操作', actionType);
            }
        }

        /**
         * 跳转到指定题目
         */
        goToQuestion(index) {
            if (index >= 0 && index < this.questions.length) {
                this.currentQuestionIndex = index;
                this.updateQuestionNavigation();
                
                // 如果使用向导模式，可以触发步骤切换
                if (this.amisScoped) {
                    // 这里可以添加AMIS向导步骤切换逻辑
                }
            }
        }

        /**
         * 开始计时
         */
        startTimer() {
            this.timerInterval = setInterval(() => {
                this.timeRemaining--;
                this.updateTimerDisplay();
                
                // 时间警告
                if (this.timeRemaining === this.options.timeWarningThreshold) {
                    this.showTimeWarning();
                }
                
                // 时间到自动提交
                if (this.timeRemaining <= 0) {
                    this.handleTimeUp();
                }
            }, 1000);
        }

        /**
         * 更新计时器显示
         */
        updateTimerDisplay() {
            const hours = Math.floor(this.timeRemaining / 3600);
            const minutes = Math.floor((this.timeRemaining % 3600) / 60);
            const seconds = this.timeRemaining % 60;
            
            const timeString = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            const timerElement = document.getElementById('exam-timer');
            if (timerElement) {
                timerElement.textContent = timeString;
                
                // 时间不足时变红
                if (this.timeRemaining <= this.options.timeWarningThreshold) {
                    timerElement.style.color = '#f44336';
                }
            }
        }

        /**
         * 显示时间警告
         */
        showTimeWarning() {
            if (window.amis && window.amis.toast) {
                window.amis.toast.warning(`考试时间仅剩 ${Math.floor(this.options.timeWarningThreshold / 60)} 分钟，请抓紧时间！`);
            } else {
                alert(`考试时间仅剩 ${Math.floor(this.options.timeWarningThreshold / 60)} 分钟，请抓紧时间！`);
            }
        }

        /**
         * 时间到处理
         */
        handleTimeUp() {
            clearInterval(this.timerInterval);
            
            if (window.amis && window.amis.toast) {
                window.amis.toast.error('考试时间已到，系统将自动提交您的答卷');
            } else {
                alert('考试时间已到，系统将自动提交您的答卷');
            }
            
            this.submitExam(true);
        }

        /**
         * 启动自动保存
         */
        startAutoSave() {
            this.autoSaveInterval = setInterval(() => {
                if (this.hasUnsavedChanges) {
                    this.saveAnswers(true);
                }
            }, this.options.autoSaveInterval);
        }

        /**
         * 保存答案
         */
        async saveAnswers(isAuto = false) {
            try {
                const token = window.TokenManager ? window.TokenManager.getToken() : null;
                const response = await fetch(`/exam/api/exam/client/${this.options.examId}/save`, {
                    method: 'POST',
                    headers: {
                        'Authorization': token ? `Bearer ${token}` : '',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        answers: this.answers,
                        timestamp: new Date().toISOString()
                    })
                });

                if (response.ok) {
                    this.hasUnsavedChanges = false;
                    
                    if (!isAuto) {
                        if (window.amis && window.amis.toast) {
                            window.amis.toast.success('答案已保存');
                        }
                    }
                    
                    this.logDebug('答案保存成功', { isAuto });
                } else {
                    throw new Error('保存失败');
                }

            } catch (error) {
                console.error('保存答案失败:', error);
                
                if (!isAuto) {
                    if (window.amis && window.amis.toast) {
                        window.amis.toast.error('答案保存失败，请稍后重试');
                    }
                }
            }
        }

        /**
         * 提交考试
         */
        async submitExam(isAutoSubmit = false) {
            if (this.isSubmitting) return;
            
            this.isSubmitting = true;
            
            try {
                // 先保存答案
                await this.saveAnswers(true);
                
                const token = window.TokenManager ? window.TokenManager.getToken() : null;
                const response = await fetch(`/exam/api/exam/client/${this.options.examId}/submit`, {
                    method: 'POST',
                    headers: {
                        'Authorization': token ? `Bearer ${token}` : '',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        answers: this.answers,
                        submitTime: new Date().toISOString(),
                        isAutoSubmit: isAutoSubmit
                    })
                });

                if (response.ok) {
                    const result = await response.json();
                    
                    // 停止所有计时器
                    if (this.timerInterval) clearInterval(this.timerInterval);
                    if (this.autoSaveInterval) clearInterval(this.autoSaveInterval);
                    
                    // 跳转到结果页面
                    window.location.href = `/${this.options.tenantId}/exam/result/${result.recordId}`;
                    
                } else {
                    throw new Error('提交失败');
                }

            } catch (error) {
                console.error('提交考试失败:', error);
                this.isSubmitting = false;
                
                if (window.amis && window.amis.toast) {
                    window.amis.toast.error('考试提交失败，请稍后重试');
                } else {
                    alert('考试提交失败，请稍后重试');
                }
            }
        }

        /**
         * 更新题目导航
         */
        updateQuestionNavigation() {
            const navItems = document.querySelectorAll('.question-nav-item');
            navItems.forEach((item, index) => {
                const questionId = this.questions[index].id;
                const isAnswered = this.answers[questionId] !== undefined;
                
                item.classList.toggle('answered', isAnswered);
                item.classList.toggle('current', index === this.currentQuestionIndex);
            });
        }

        /**
         * 更新统计信息
         */
        updateStatistics() {
            const answeredCount = Object.keys(this.answers).length;
            const unansweredCount = this.questions.length - answeredCount;
            
            const answeredEl = document.getElementById('answered-count');
            const unansweredEl = document.getElementById('unanswered-count');
            const progressEl = document.getElementById('exam-progress-text');
            
            if (answeredEl) answeredEl.textContent = answeredCount;
            if (unansweredEl) unansweredEl.textContent = unansweredCount;
            if (progressEl) progressEl.textContent = `${answeredCount}/${this.questions.length}`;
        }

        /**
         * 显示/隐藏加载状态
         */
        showLoading(show) {
            const loadingEl = document.getElementById('loading');
            if (loadingEl) {
                loadingEl.style.display = show ? 'flex' : 'none';
            }
        }

        /**
         * 显示错误信息
         */
        showError(message) {
            this.showLoading(false);
            
            if (window.amis && window.amis.toast) {
                window.amis.toast.error(message);
            } else {
                alert(message);
            }
        }

        /**
         * 绑定事件
         */
        bindEvents() {
            // 页面卸载前提醒
            window.addEventListener('beforeunload', (e) => {
                if (this.hasUnsavedChanges && !this.isSubmitting) {
                    e.preventDefault();
                    e.returnValue = '您有未保存的答案，确定要离开吗？';
                    return '您有未保存的答案，确定要离开吗？';
                }
            });
        }

        /**
         * Debug日志
         */
        logDebug(message, data = null) {
            if (this.options.debug || (window.CS_CONFIG && window.CS_CONFIG.isDevelopment)) {
                if (data) {
                    console.log(`[ExamManager] ${message}`, data);
                } else {
                    console.log(`[ExamManager] ${message}`);
                }
            }
        }

        /**
         * 销毁管理器
         */
        destroy() {
            if (this.timerInterval) clearInterval(this.timerInterval);
            if (this.autoSaveInterval) clearInterval(this.autoSaveInterval);
            
            this.logDebug('考试管理器已销毁');
        }
    }

    // 导出到全局
    window.ExamManager = ExamManager;

    // 自动初始化
    window.addEventListener('DOMContentLoaded', () => {
        if (window.CS_CONFIG && window.CS_CONFIG.tenantId && window.CS_CONFIG.examId) {
            window.examManager = new ExamManager({
                tenantId: window.CS_CONFIG.tenantId,
                examId: window.CS_CONFIG.examId,
                debug: window.CS_CONFIG.isDevelopment
            });
            
            console.log('🎯 考试管理器已自动初始化');
        }
    });

    // 全局提交函数（供屏幕切换检测器调用）
    window.submitExam = function(reason = 'manual', message = '') {
        if (window.examManager) {
            console.warn(`🚨 考试被${reason === 'auto' ? '自动' : '手动'}提交: ${message}`);
            window.examManager.submitExam(reason === 'auto');
        }
    };

})(window); 