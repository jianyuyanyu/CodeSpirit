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
        window.location.href = `/${window.tenantId}/exam/login`;
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
                ...options
            };

            // 结果数据
            this.resultData = null;
            this.examData = null;
            this.questionResults = [];

            // 状态管理
            this.isLoading = false;

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
                const response = await fetch(`/exam/api/exam/client/result/${this.options.recordId}`, {
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
            return {
                type: 'page',
                className: 'result-content',
                body: [
                    // 结果头部
                    this.buildResultHeader(),
                    // 成绩概览
                    this.buildResultOverview(),
                    // 详细统计
                    this.buildResultDetails(),
                    // 题目分析
                    this.buildQuestionAnalysis(),
                    // 操作按钮
                    this.buildResultActions()
                ]
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
         * 构建成绩概览
         */
        buildResultOverview() {
            const scorePercentage = this.resultData.totalScore > 0 
                ? ((this.resultData.score / this.resultData.totalScore) * 100).toFixed(1)
                : 0;

            return {
                type: 'grid',
                className: 'result-overview',
                columns: [
                    {
                        body: {
                            type: 'panel',
                            className: 'result-score-card',
                            body: {
                                type: 'html',
                                html: `
                                    <div class="result-score-label">总分</div>
                                    <div class="result-score-value score">${this.resultData.score}</div>
                                    <div class="result-score-unit">/ ${this.resultData.totalScore}分</div>
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
                ]
            };
        }

        /**
         * 构建详细统计
         */
        buildResultDetails() {
            const correctCount = this.questionResults.filter(q => q.isCorrect).length;
            const incorrectCount = this.questionResults.filter(q => {
                const obtainedScore = q.obtainedScore !== undefined ? q.obtainedScore : q.score;
                return !q.isCorrect && obtainedScore === 0;
            }).length;
            const partialCount = this.questionResults.filter(q => {
                const obtainedScore = q.obtainedScore !== undefined ? q.obtainedScore : q.score;
                const totalScore = q.score || q.totalScore || 0;
                return !q.isCorrect && obtainedScore > 0 && obtainedScore < totalScore;
            }).length;
            const totalQuestions = this.questionResults.length;

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
                                            <div class="result-stat-value" style="color: var(--result-warning-color);">${partialCount}</div>
                                            <div class="result-stat-label">部分正确</div>
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
                const userAnswer = question.userAnswer || question.answer || '未作答';
                const correctAnswer = question.correctAnswer || '暂无';
                
                // 状态图标
                const statusIcon = statusClass === 'correct' ? '✓' : 
                                 statusClass === 'partial' ? '△' : '✗';
                const statusIconClass = statusClass === 'correct' ? 'status-icon-correct' : 
                                      statusClass === 'partial' ? 'status-icon-partial' : 'status-icon-incorrect';
                
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
            window.location.href = `/${this.options.tenantId}/exam/application`;
        }



        /**
         * 打印结果
         */
        printResult() {
            window.print();
        }



        /**
         * 获取题目状态样式类
         */
        getQuestionStatusClass(question) {
            // 兼容新旧API字段
            const isCorrect = question.isCorrect;
            const obtainedScore = question.obtainedScore !== undefined ? question.obtainedScore : question.score;
            const totalScore = question.score || question.totalScore || 0;
            
            if (isCorrect) {
                return 'correct';
            } else if (obtainedScore > 0 && obtainedScore < totalScore) {
                return 'partial';
            } else {
                return 'incorrect';
            }
        }

        /**
         * 获取题目状态文本
         */
        getQuestionStatusText(question) {
            // 兼容新旧API字段
            const isCorrect = question.isCorrect;
            const obtainedScore = question.obtainedScore !== undefined ? question.obtainedScore : question.score;
            const totalScore = question.score || question.totalScore || 0;
            
            if (isCorrect) {
                return '正确';
            } else if (obtainedScore > 0 && obtainedScore < totalScore) {
                return '部分正确';
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
         * 销毁管理器
         */
        destroy() {
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

})(window); 