/**
 * 考试结果主模块
 * 处理考试结果页面的数据加载和显示
 */

(function(window) {
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

    // 初始化AMIS
    let amis = amisRequire('amis/embed');
    let amisInstance = null;

    /**
     * 考试结果管理器类
     */
    class ResultManager {
        constructor(options = {}) {
            this.options = {
                tenantId: options.tenantId || window.tenantId,
                recordId: options.recordId || window.recordId,
                debug: false,
                idleTimeout: 300000, // 5分钟无活动超时
                ...options
            };

            // 结果数据
            this.resultData = null;
            this.examData = null;
            this.questionResults = [];

            // 状态管理
            this.isLoading = false;

            // 空闲检测相关
            this.idleTimer = null;
            this.isIdleWarningShown = false;

            this.init();
        }

        /**
         * 初始化结果管理器
         */
        async init() {
            try {
                this.showLoading(true);
                
                // 验证必要参数
                if (!this.options.tenantId || !this.options.recordId) {
                    throw new Error('缺少必要的参数：tenantId 或 recordId');
                }

                // 加载考试结果数据
                await this.loadResultData();
                
                // 初始化AMIS
                this.initAmis();

                // 初始化空闲检测
                this.initIdleDetection();

                this.showLoading(false);
                this.logDebug('考试结果管理器初始化完成');

            } catch (error) {
                console.error('考试结果初始化失败:', error);
                this.showError('考试结果加载失败，请刷新页面重试');
            }
        }

        /**
         * 加载考试结果数据
         */
        async loadResultData() {
            try {
                const token = TokenManager.getToken();
                const response = await fetch(window.ExamApiManager.transformUrl(`/exam/api/exam/client/result/${this.options.recordId}`), {
                    headers: {
                        'Authorization': token ? 'Bearer ' + token : '',
                        'TenantId': this.options.tenantId,
                        'X-Forwarded-With': 'CodeSpirit',
                        'Content-Type': 'application/json'
                    }
                });

                if (response.status === 401) {
                    window.location.href = `/${this.options.tenantId}/exam/login`;
                    throw new Error('认证失败，请重新登录');
                }

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const result = await response.json();
                
                // 检查业务状态码
                if (result.status !== undefined && result.status !== 0) {
                    throw new Error(result.msg || '获取考试结果失败');
                }

                // 兼容不同的响应格式
                this.resultData = result.data || result;
                this.examData = this.resultData.exam || {};
                
                // 根据旧版本API，题目数据在answers字段中
                this.questionResults = this.resultData.answers || 
                                     this.resultData.questionResults || 
                                     this.resultData.questions || 
                                     this.resultData.answerResults ||
                                     this.resultData.details ||
                                     [];

                // 增强调试信息
                console.log('🔍 [ResultManager] API原始响应:', result);
                console.log('📊 [ResultManager] 解析后的resultData:', this.resultData);
                console.log('📋 [ResultManager] examData:', this.examData);
                console.log('❓ [ResultManager] questionResults:', this.questionResults);
                console.log('🔢 [ResultManager] questionResults长度:', this.questionResults ? this.questionResults.length : 0);
                
                // 检查题目数据的结构
                if (this.questionResults && this.questionResults.length > 0) {
                    console.log('📝 [ResultManager] 第一题示例数据:', this.questionResults[0]);
                }

                this.logDebug('考试结果数据加载完成', this.resultData);

            } catch (error) {
                console.error('加载考试结果数据失败:', error);
                throw error;
            }
        }

        /**
         * 初始化AMIS
         */
        initAmis() {
            const schema = this.buildAmisSchema();
            
            // 渲染AMIS页面
            amisInstance = amis.embed('#root', schema, {
                location: history.location,
                data: {
                    tenant: { name: '考试平台' },
                    result: this.resultData,
                    exam: this.examData,
                    questions: this.questionResults
                },
                context: {
                    WEB_HOST: window.webHost,
                    TENANT_ID: window.tenantId,
                    RECORD_ID: this.options.recordId
                },
                requestAdaptor: (api) => {
                    const token = TokenManager.getToken();
                    return {
                        ...api,
                        headers: {
                            ...api.headers,
                            'Authorization': token ? 'Bearer ' + token : '',
                            'TenantId': window.tenantId,
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    };
                }
            }, {
                theme: 'antd',
                locale: 'zh-CN'
            });



            this.amisScoped = amisInstance;
        }

        /**
         * 构建AMIS页面结构
         */
        buildAmisSchema() {
            const resultOverview = this.buildResultOverview();
            const body = [
                // 结果头部
                this.buildResultHeader(),
                // 成绩概览（可能是数组或单个对象）
                ...(Array.isArray(resultOverview) ? resultOverview : [resultOverview]),
                // 详细统计
                this.buildResultDetails(),
                // 题目分析
                this.buildQuestionAnalysis(),
                // 操作按钮
                this.buildResultActions()
            ];

            return {
                type: 'page',
                className: 'result-content',
                body: body
            };
        }

        /**
         * 构建结果头部
         */
        buildResultHeader() {
            const isPassed = this.resultData.isPassed;
            const statusIcon = isPassed ? '✓' : '✗';
            const statusClass = isPassed ? 'passed' : 'failed';
            const statusText = isPassed ? '考试通过' : '考试未通过';
            const gradeText = this.getGradeText(this.resultData.score, this.resultData.totalScore);

            return {
                type: 'panel',
                className: 'result-header',
                body: {
                    type: 'html',
                    html: `
                        <div class="result-status-icon ${statusClass}">
                            ${statusIcon}
                        </div>
                        <h1 class="result-title">${statusText}</h1>
                        <p class="result-subtitle">${this.examData.title || '在线考试'} - ${gradeText}</p>
                    `
                }
            };
        }

        /**
         * 构建成绩概览（支持成绩换算显示）
         */
        buildResultOverview() {
            // 检查是否启用了成绩换算
            const isScoreConverted = this.resultData.isScoreConverted || false;
            const originalScore = this.resultData.originalScore || this.resultData.score;
            const finalScore = this.resultData.score; // Score字段存储最终显示成绩
            const originalTotalScore = this.examData.originalTotalScore || this.examData.totalScore || this.resultData.totalScore;
            const convertedTotalScore = this.examData.conversionTargetFullScore;
            
            // 确定显示的主要成绩
            const displayScore = finalScore; // 最终显示成绩
            const displayTotalScore = isScoreConverted ? convertedTotalScore : originalTotalScore;
            
            const scorePercentage = displayTotalScore > 0 
                ? ((displayScore / displayTotalScore) * 100).toFixed(1)
                : 0;

            // 构建成绩卡片
            const scoreCard = this.buildScoreCard(isScoreConverted, originalScore, finalScore, 
                                                 originalTotalScore, displayTotalScore);

            const overviewItems = [
                scoreCard,
                {
                    body: {
                        type: 'panel',
                        className: 'result-score-card',
                        body: {
                            type: 'html',
                            html: `
                                <div class="result-score-label">得分率</div>
                                <div class="result-score-value">${scorePercentage}%</div>
                                <div class="result-score-unit">百分比</div>
                            `
                        }
                    }
                },
                {
                    body: {
                        type: 'panel',
                        className: 'result-score-card',
                        body: {
                            type: 'html',
                            html: `
                                <div class="result-score-label">用时</div>
                                <div class="result-score-value">${this.formatDuration(this.resultData.duration || 0)}</div>
                                <div class="result-score-unit">时间</div>
                            `
                        }
                    }
                },
                {
                    body: {
                        type: 'panel',
                        className: 'result-score-card',
                        body: {
                            type: 'html',
                            html: `
                                <div class="result-score-label">状态</div>
                                <div class="result-score-value ${this.resultData.isPassed ? 'passed' : 'failed'}">
                                    ${this.resultData.isPassed ? '通过' : '未通过'}
                                </div>
                                <div class="result-score-unit">结果</div>
                            `
                        }
                    }
                }
            ];

            // 如果启用了成绩换算，在顶部添加醒目提醒
            const components = [];
            if (isScoreConverted) {
                components.push({
                    type: 'alert',
                    level: 'success',
                    className: 'score-conversion-alert',
                    body: {
                        type: 'html',
                        html: `
                            <div class="conversion-reminder">
                                <div style="display: flex; align-items: center; margin-bottom: 8px;">
                                    <span style="font-size: 18px; margin-right: 8px;">🎯</span>
                                    <strong style="font-size: 16px;">成绩换算提醒</strong>
                                </div>
                                <div style="font-size: 14px; line-height: 1.5;">
                                    本考试已应用成绩换算：<strong>${this.examData.conversionDescription || `将${originalTotalScore}分制转换为${convertedTotalScore}分制`}</strong><br/>
                                    <span style="color: #666;">原始成绩：${originalScore}/${originalTotalScore}分 → 最终成绩：${finalScore}/${convertedTotalScore}分</span>
                                </div>
                            </div>
                        `
                    }
                });
            }

            components.push({
                type: 'grid',
                className: 'result-overview',
                columns: overviewItems
            });

            return components.length === 1 ? components[0] : components;
        }

        /**
         * 构建成绩卡片（支持换算显示）
         */
        buildScoreCard(isScoreConverted, originalScore, finalScore, originalTotalScore, finalTotalScore) {
            // 显示原始成绩
            return {
                body: {
                    type: 'panel',
                    className: 'result-score-card',
                    body: {
                        type: 'html',
                        html: `
                            <div class="result-score-label">总分</div>
                            <div class="result-score-value score">${finalScore}</div>
                            <div class="result-score-unit">/ ${finalTotalScore}分</div>
                        `
                    }
                }
            };
        }

        /**
         * 构建详细统计
         */
        buildResultDetails() {
            const correctCount = this.questionResults.filter(q => q.isCorrect).length;
            const incorrectCount = this.questionResults.filter(q => {
                return !this.isQuestionUnanswered(q) && !q.isCorrect;
            }).length;
            const unansweredCount = this.questionResults.filter(q => {
                return this.isQuestionUnanswered(q);
            }).length;
            
            // 现在后端返回所有题目（包括未作答），可以直接使用questionResults长度
            const totalQuestions = this.questionResults.length;
            
            console.log('📊 [ResultManager] 题目统计数据:');
            console.log('📊 [ResultManager] examData.questions?.length:', this.examData.questions?.length);
            console.log('📊 [ResultManager] examData.questionCount:', this.examData.questionCount);
            console.log('📊 [ResultManager] examData.totalQuestions:', this.examData.totalQuestions);
            console.log('📊 [ResultManager] questionResults.length:', this.questionResults.length);
            console.log('📊 [ResultManager] 最终采用总题目数:', totalQuestions);

            return {
                type: 'panel',
                className: 'result-details',
                body: [
                    {
                        type: 'html',
                        html: '<h3 class="result-details-title">答题统计</h3>'
                    },
                    {
                        type: 'grid',
                        className: 'result-stats-grid',
                        columns: [
                            {
                                body: {
                                    type: 'html',
                                    html: `
                                        <div class="result-stat-item">
                                            <div class="result-stat-value">${totalQuestions}</div>
                                            <div class="result-stat-label">总题数</div>
                                        </div>
                                    `
                                }
                            },
                            {
                                body: {
                                    type: 'html',
                                    html: `
                                        <div class="result-stat-item">
                                            <div class="result-stat-value" style="color: var(--result-success-color);">${correctCount}</div>
                                            <div class="result-stat-label">正确</div>
                                        </div>
                                    `
                                }
                            },
                            {
                                body: {
                                    type: 'html',
                                    html: `
                                        <div class="result-stat-item">
                                            <div class="result-stat-value" style="color: var(--result-danger-color);">${incorrectCount}</div>
                                            <div class="result-stat-label">错误</div>
                                        </div>
                                    `
                                }
                            },
                            {
                                body: {
                                    type: 'html',
                                    html: `
                                        <div class="result-stat-item">
                                            <div class="result-stat-value" style="color: var(--result-warning-color);">${unansweredCount}</div>
                                            <div class="result-stat-label">未作答</div>
                                        </div>
                                    `
                                }
                            },
                            {
                                body: {
                                    type: 'html',
                                    html: `
                                        <div class="result-stat-item">
                                            <div class="result-stat-value">${((correctCount / totalQuestions) * 100).toFixed(1)}%</div>
                                            <div class="result-stat-label">正确率</div>
                                        </div>
                                    `
                                }
                            }
                        ]
                    }
                ]
            };
        }

        /**
         * 构建题目分析
         */
        buildQuestionAnalysis() {
            console.log('🔧 [ResultManager] buildQuestionAnalysis调用');
            console.log('🔧 [ResultManager] this.questionResults:', this.questionResults);
            console.log('🔧 [ResultManager] 是否为空判断:', !this.questionResults || this.questionResults.length === 0);
            
            // 检查是否启用了题目分析功能
            const enableQuestionAnalysis = this.resultData.enableQuestionAnalysis !== undefined ? 
                this.resultData.enableQuestionAnalysis : true; // 默认启用
            
            console.log('🔧 [ResultManager] 题目分析启用状态:', enableQuestionAnalysis);
            
            if (!enableQuestionAnalysis) {
                console.log('⚠️ [ResultManager] 题目分析功能已禁用');
                return {
                    type: 'panel',
                    className: 'result-analysis',
                    body: {
                        type: 'html',
                        html: `
                            <div class="result-empty">
                                <div class="result-empty-text">
                                    <i class="fa fa-eye-slash" style="font-size: 24px; color: #ccc; margin-bottom: 10px;"></i>
                                    <div>题目分析已关闭</div>
                                </div>
                            </div>
                        `
                    }
                };
            }
            
            if (!this.questionResults || this.questionResults.length === 0) {
                console.log('⚠️ [ResultManager] 显示空状态：暂无题目分析数据');
                return {
                    type: 'panel',
                    className: 'result-analysis',
                    body: {
                        type: 'html',
                        html: `
                            <div class="result-empty">
                                <div class="result-empty-text">暂无题目分析数据</div>
                            </div>
                        `
                    }
                };
            }
            
            console.log('✅ [ResultManager] 开始构建题目分析，题目数量:', this.questionResults.length);

            const questionItems = this.questionResults.map((question, index) => {
                const statusClass = this.getQuestionStatusClass(question);
                const statusText = this.getQuestionStatusText(question);
                
                // 兼容新旧API字段名
                const questionTitle = question.content || question.title || `第${index + 1}题`;
                const obtainedScore = question.obtainedScore !== undefined ? question.obtainedScore : question.score;
                const totalScore = question.score || question.totalScore || 0;
                const questionType = question.type || question.questionType;
                
                // 处理判断题答案显示为勾叉符号
                let userAnswer = question.userAnswer || question.answer || '未作答';
                let correctAnswer = question.correctAnswer || '暂无';
                
                if (questionType === 'TrueFalse' || questionType === 3) {
                    if (userAnswer && userAnswer.toLowerCase() === 'true') userAnswer = '✓';
                    else if (userAnswer && userAnswer.toLowerCase() === 'false') userAnswer = '✗';
                    
                    if (correctAnswer && correctAnswer.toLowerCase() === 'true') correctAnswer = '✓';
                    else if (correctAnswer && correctAnswer.toLowerCase() === 'false') correctAnswer = '✗';
                }
                
                // 状态图标
                const statusIcon = statusClass === 'correct' ? '✓' : 
                                 statusClass === 'unanswered' ? '?' : '✗';
                const statusIconClass = statusClass === 'correct' ? 'status-icon-correct' : 
                                      statusClass === 'unanswered' ? 'status-icon-unanswered' : 'status-icon-incorrect';
                
                return {
                    type: 'html',
                    html: `
                        <div class="question-result-item">
                            <div class="question-result-number ${statusClass}">
                                ${index + 1}
                            </div>
                            <div class="question-result-content">
                                <div class="question-result-title">
                                    ${questionTitle}
                                </div>
                                <div class="question-result-score">
                                    得分：${obtainedScore || 0}/${totalScore}分
                                </div>
                                <div class="question-result-answers">
                                    <div class="answer-section">
                                        <div class="answer-label">你的答案：</div>
                                        <div class="user-answer">${userAnswer}</div>
                                    </div>
                                    <div class="answer-section">
                                        <div class="answer-label">正确答案：</div>
                                        <div class="correct-answer">${correctAnswer}</div>
                                    </div>
                                </div>
                            </div>
                            <div class="question-result-status ${statusClass}">
                                <div class="status-icon ${statusIconClass}">${statusIcon}</div>
                                <div class="status-text">${statusText}</div>
                            </div>
                        </div>
                    `
                };
            });

            return {
                type: 'panel',
                className: 'result-analysis',
                body: [
                    {
                        type: 'html',
                        html: '<h3 class="result-analysis-title">题目分析</h3>'
                    },
                    ...questionItems
                ]
            };
        }

        /**
         * 构建操作按钮
         */
        buildResultActions() {
            return {
                type: 'html',
                className: 'result-actions',
                html: `
                    <div class="result-actions-container">
                        <button type="button" class="result-btn result-btn-secondary" onclick="window.resultManager.goHome()">
                            <i class="fa fa-home"></i> 返回首页
                        </button>
                        <button type="button" class="result-btn result-btn-success" onclick="window.resultManager.printResult()">
                            <i class="fa fa-print"></i> 打印结果
                        </button>
                    </div>
                `
            };
        }



        /**
         * 返回首页
         */
        goHome() {
            window.location.href = `/${this.options.tenantId}/exam/app`;
        }



        /**
         * 打印结果
         */
        printResult() {
            window.print();
        }



        /**
         * 判断题目是否未作答
         * @param {Object} question - 题目对象
         * @returns {boolean} - 是否未作答
         */
        isQuestionUnanswered(question) {
            // 检查isAnswered标志
            if (question.isAnswered === false) {
                return true;
            }
            
            // 检查答案是否为空（null、undefined、空字符串、空数组）
            const answer = question.userAnswer || question.answer;
            
            if (answer === null || answer === undefined || answer === '') {
                return true;
            }
            
            // 如果是数组类型的答案（多选题），检查是否为空数组
            if (Array.isArray(answer) && answer.length === 0) {
                return true;
            }
            
            // 如果是字符串类型，去除空格后检查是否为空
            if (typeof answer === 'string' && answer.trim() === '') {
                return true;
            }
            
            return false;
        }

        /**
         * 获取题目状态样式类
         */
        getQuestionStatusClass(question) {
            const isCorrect = question.isCorrect;
            
            if (isCorrect) {
                return 'correct';
            } else if (this.isQuestionUnanswered(question)) {
                return 'unanswered';
            } else {
                return 'incorrect';
            }
        }

        /**
         * 获取题目状态文本
         */
        getQuestionStatusText(question) {
            const isCorrect = question.isCorrect;
            
            if (isCorrect) {
                return '正确';
            } else if (this.isQuestionUnanswered(question)) {
                return '未作答';
            } else {
                return '错误';
            }
        }

        /**
         * 获取等级文本
         */
        getGradeText(score, totalScore) {
            if (totalScore === 0) return '无评分';
            
            const percentage = (score / totalScore) * 100;
            
            if (percentage >= 90) return '优秀';
            if (percentage >= 80) return '良好';
            if (percentage >= 70) return '中等';
            if (percentage >= 60) return '及格';
            return '不及格';
        }

        /**
         * 格式化持续时间
         * @param {number} minutes - 时长（分钟）
         */
        formatDuration(minutes) {
            if (!minutes || minutes <= 0) {
                return '0分钟';
            }
            
            // API返回的是分钟数
            const totalMinutes = Math.floor(minutes);
            
            if (totalMinutes < 60) {
                return `${totalMinutes}分钟`;
            } else {
                const hours = Math.floor(totalMinutes / 60);
                const remainingMinutes = totalMinutes % 60;
                return remainingMinutes > 0 ? `${hours}小时${remainingMinutes}分钟` : `${hours}小时`;
            }
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
            
            if (amisInstance && amisInstance.env && amisInstance.env.notify) {
                amisInstance.env.notify('error', message);
            } else if (window.amis && window.amis.toast) {
                window.amis.toast.error(message);
            } else {
                alert(message);
            }
        }

        /**
         * Debug日志
         */
        logDebug(message, data = null) {
            if (this.options.debug || (window.CS_CONFIG && window.CS_CONFIG.isDevelopment)) {
                if (data) {
                    console.log(`[ResultManager] ${message}`, data);
                } else {
                    console.log(`[ResultManager] ${message}`);
                }
            }
        }

        /**
         * 初始化空闲检测
         */
        initIdleDetection() {
            // 监听的活动事件类型
            const activityEvents = [
                'mousedown', 'mousemove', 'keypress', 'keydown',
                'scroll', 'touchstart', 'click', 'contextmenu'
            ];

            // 重置空闲定时器的处理函数
            const resetIdleTimer = () => {
                this.resetIdleTimer();
            };

            // 绑定事件监听器
            activityEvents.forEach(event => {
                document.addEventListener(event, resetIdleTimer, true);
            });

            // 存储事件处理函数和事件类型，用于后续清理
            this.activityEvents = activityEvents;
            this.resetIdleTimerHandler = resetIdleTimer;

            // 启动空闲定时器
            this.resetIdleTimer();

            this.logDebug(`空闲检测已初始化，超时时间: ${this.options.idleTimeout / 1000}秒`);
        }

        /**
         * 重置空闲定时器
         */
        resetIdleTimer() {
            // 清除现有定时器
            if (this.idleTimer) {
                clearTimeout(this.idleTimer);
            }

            // 重置警告状态
            this.isIdleWarningShown = false;

            // 设置新的定时器
            this.idleTimer = setTimeout(() => {
                this.handleIdleTimeout();
            }, this.options.idleTimeout);

            this.logDebug('空闲定时器已重置');
        }

        /**
         * 处理空闲超时
         */
        handleIdleTimeout() {
            this.logDebug('检测到空闲超时，准备自动退出');

            // 显示退出提示
            if (amisInstance && amisInstance.env && amisInstance.env.notify) {
                amisInstance.env.notify('warning', '由于长时间无操作，即将自动返回首页');
            } else if (window.amis && window.amis.toast) {
                window.amis.toast.warning('由于长时间无操作，即将自动返回首页');
            }

            // 1秒后执行退出
            setTimeout(() => {
                this.autoExit();
            }, 1000);
        }

        /**
         * 自动退出到首页
         */
        autoExit() {
            this.logDebug('执行自动退出');
            
            // 清理资源
            this.cleanupIdleDetection();
            
            // 跳转到登录页面
            window.location.href = `/${this.options.tenantId}/exam/login`;
        }

        /**
         * 清理空闲检测
         */
        cleanupIdleDetection() {
            // 清除定时器
            if (this.idleTimer) {
                clearTimeout(this.idleTimer);
                this.idleTimer = null;
            }

            // 移除事件监听器
            if (this.activityEvents && this.resetIdleTimerHandler) {
                this.activityEvents.forEach(event => {
                    document.removeEventListener(event, this.resetIdleTimerHandler, true);
                });
            }

            this.logDebug('空闲检测已清理');
        }

        /**
         * 销毁管理器
         */
        destroy() {
            // 清理空闲检测
            this.cleanupIdleDetection();
            
            this.logDebug('考试结果管理器已销毁');
        }
    }

    // 导出到全局
    window.ResultManager = ResultManager;

    // 自动初始化
    window.addEventListener('DOMContentLoaded', () => {
        if (window.CS_CONFIG && window.CS_CONFIG.tenantId && window.CS_CONFIG.recordId) {
            window.resultManager = new ResultManager({
                tenantId: window.CS_CONFIG.tenantId,
                recordId: window.CS_CONFIG.recordId,
                debug: window.CS_CONFIG.isDevelopment
            });
            
            console.log('📊 考试结果管理器已自动初始化');
        }
    });

    // 页面卸载时清理资源
    window.addEventListener('beforeunload', () => {
        if (window.resultManager) {
            window.resultManager.destroy();
        }
    });

})(window); 