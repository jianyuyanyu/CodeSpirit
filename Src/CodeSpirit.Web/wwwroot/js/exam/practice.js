/**
 * 在线练习系统客户端主模块
 * 负责单题练习页面渲染、答题和导航功能
 * @module PracticeClient
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

    // 引入依赖模块
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 获取练习ID
    const practiceId = window.practiceId;

    // 调试模式配置
    window.enableAMISDebug = false;

    // 常量定义
    const CONSTANTS = {
        AUTO_SAVE_DELAY: 1000,           // 自动保存延迟(毫秒)
        MAX_RETRY_COUNT: 3,              // 最大重试次数
        RETRY_DELAY: 2000,               // 重试延迟基础时间(毫秒)
        QUESTION_CHANGE_DELAY: 300       // 题目切换延迟(毫秒)
    };

    /**
     * 通用API请求函数
     * 使用ExamApiManager进行统一的API请求处理
     * @param {string} url - API地址
     * @param {object} options - 请求选项
     * @returns {Promise<any>} API响应数据
     */
    async function apiRequest(url, options = {}) {
        return window.ExamApiManager.request(url, options);
    }

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
            description: '',
            recordId: null,
            currentQuestionIndex: 0,
            totalQuestions: 0,
            questions: [],
            answers: new Map()
        },
        tenant: {
            id: window.tenantId,
            name: '练习平台',
            logo: '/logo.png',
            description: ''
        }
    };

    // 私有状态变量
    let practiceData = null;
    let currentQuestion = null;
    let currentQuestionIndex = 0;
    let totalQuestions = 0;
    let practiceAnswers = new Map();
    let recordId = null;
    let isInitialized = false;

    // AMIS实例引用
    window.amisInstance = null;

    /**
     * 全局数据管理工具
     * @namespace GlobalData
     */
    window.GlobalData = {
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

        syncToAmis: function (amisInstance, selectedPaths) {
            if (!amisInstance || !amisInstance.updateProps) return;
            try {
                const data = {};
                if (selectedPaths && Array.isArray(selectedPaths)) {
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
                    Object.assign(data, window.globalData);
                }
                amisInstance.updateProps({ data });
            } catch (error) {
                console.error('[GlobalData] 同步数据到AMIS失败:', error);
            }
        },

        update: function (path, value, syncToAmis = true) {
            this.set(path, value);
            if (syncToAmis && window.amisInstance) {
                this.syncToAmis(window.amisInstance, [path]);
            }
        }
    };

    /**
     * 加载租户信息（使用缓存）
     * 参考application.js中的实现方式
     * @returns {Promise<Object>} 租户信息
     */
    async function loadTenantInfo() {
        try {
            // 使用缓存的登录配置接口
            const tenantConfig = await window.ExamApiManager.getLoginConfig(window.tenantId);
            
            // 转换为练习页面需要的格式
            const tenantInfo = {
                id: window.tenantId,
                name: tenantConfig.displayName || tenantConfig.name || '练习平台',
                logo: tenantConfig.logoUrl || '/logo.png',
                description: tenantConfig.description || ''
            };
            
            // 更新全局数据
            window.GlobalData.set('tenant', tenantInfo);
            
            // 同步到AMIS实例
            if (window.amisInstance) {
                window.GlobalData.syncToAmis(window.amisInstance, ['tenant']);
            }
            
            console.log('[练习] 租户信息加载成功:', tenantInfo);
            return tenantInfo;
        } catch (error) {
            console.warn('[练习] 租户信息加载失败:', error);
            const fallbackInfo = { 
                id: window.tenantId,
                name: '练习平台', 
                logo: '/logo.png',
                description: '' 
            };
            
            // 更新全局数据为默认值
            window.GlobalData.set('tenant', fallbackInfo);
            
            // 同步到AMIS实例
            if (window.amisInstance) {
                window.GlobalData.syncToAmis(window.amisInstance, ['tenant']);
            }
            
            return fallbackInfo;
        }
    }

    /**
     * 加载练习数据
     * @param {string} practiceId - 练习ID
     * @returns {Promise<boolean>} 加载是否成功
     */
    async function loadPractice(practiceId) {
        console.log('[练习] 开始加载练习数据:', practiceId);
        
        try {
            const data = await apiRequest(`/exam/api/exam/client/practice/${practiceId}/start`, {
                method: 'POST'
            });
            
            console.log('[练习] 服务器响应:', data);

            // 检查标准API响应格式
            if (data) {
                return await processPracticeData(data);
            } else {
                console.error('[练习] 服务器返回错误: 数据为空');
                return false;
            }
        } catch (error) {
            console.error('[练习] 加载练习数据失败:', error);
            return false;
        }
    }

    /**
     * 处理练习数据
     * @param {object} data - 练习数据
     * @returns {Promise<boolean>} 处理是否成功
     */
    async function processPracticeData(data) {
        try {
            practiceData = data;
            recordId = data.recordId;
            
            // 更新全局数据
            window.GlobalData.set('practice.name', data.practiceName || '练习');
            window.GlobalData.set('practice.description', data.description || '');
            window.GlobalData.set('practice.recordId', recordId);
            
            // 处理题目数据
            if (data.questions && data.questions.length > 0) {
                const processedQuestions = await processQuestions(data.questions);
                totalQuestions = processedQuestions.length;
                currentQuestionIndex = 0;
                
                window.GlobalData.set('practice.questions', processedQuestions);
                window.GlobalData.set('practice.totalQuestions', totalQuestions);
                window.GlobalData.set('practice.currentQuestionIndex', currentQuestionIndex);
                
                // 加载当前题目
                await loadCurrentQuestion();
                
                console.log('[练习] 练习数据处理完成，题目数量:', totalQuestions);
                return true;
            } else {
                console.error('[练习] 没有找到题目数据');
                return false;
            }
        } catch (error) {
            console.error('[练习] 处理练习数据失败:', error);
            return false;
        }
    }

    /**
     * 处理题目数据
     * @param {Array} questions - 题目数组
     * @returns {Promise<Array>} 处理后的题目数组
     */
    async function processQuestions(questions) {
        return questions.map(question => {
            // 确保题目ID是字符串类型
            const questionId = String(question.id || question.questionId);
            
            // 处理题目类型，将数字类型转换为字符串
            let questionType = 'SingleChoice';
            if (typeof question.type === 'number') {
                switch (question.type) {
                    case 1: questionType = 'SingleChoice'; break;
                    case 2: questionType = 'MultipleChoice'; break;
                    case 3: questionType = 'TrueFalse'; break;
                    case 4: questionType = 'FillInBlank'; break;
                    case 5: questionType = 'Essay'; break;
                    default: questionType = 'SingleChoice'; break;
                }
            } else {
                questionType = question.type || 'SingleChoice';
            }
            
            // 处理选项数据
            let options = [];
            if (question.options && Array.isArray(question.options)) {
                options = question.options.map((opt, index) => ({
                    label: opt.text || opt.label || opt,
                    value: String(index) // 使用索引作为值
                }));
            }
            
            return {
                id: questionId,
                questionId: questionId,
                content: question.content || '',
                type: questionType,
                score: question.defaultScore || question.score || 0,
                options: options,
                answer: question.answer || null,
                explanation: question.analysis || question.explanation || '',
                isRequired: question.isRequired || false
            };
        });
    }

    /**
     * 加载当前题目
     * @returns {Promise<boolean>} 加载是否成功
     */
    async function loadCurrentQuestion() {
        try {
            const questions = window.GlobalData.get('practice.questions', []);
            
            if (!questions || questions.length === 0) {
                console.error('[练习] 没有题目数据');
                return false;
            }

            if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.length) {
                console.error('[练习] 题目索引超出范围:', currentQuestionIndex);
                return false;
            }

            currentQuestion = questions[currentQuestionIndex];
            
            if (!currentQuestion || !currentQuestion.questionId) {
                console.error('[练习] 当前题目数据无效:', currentQuestion);
                return false;
            }

            console.log('[练习] 加载题目:', currentQuestion.questionId, '索引:', currentQuestionIndex + 1);
            
            // 更新题目数据到全局
            await updateCurrentQuestionData();
            
            // 更新UI显示
            updateQuestionUI();
            
            return true;
        } catch (error) {
            console.error('[练习] 加载当前题目失败:', error);
            return false;
        }
    }

    /**
     * 更新当前题目数据
     * @returns {Promise<void>}
     */
    async function updateCurrentQuestionData() {
        try {
            // 获取之前保存的答案
            const savedAnswer = practiceAnswers.get(currentQuestion.questionId);
            
            // 根据题目类型处理答案格式
            let displayAnswer = null;
            if (savedAnswer !== undefined && savedAnswer !== null) {
                // 如果有保存的答案，使用保存的答案
                displayAnswer = savedAnswer;
                console.log('[练习] 加载已保存的答案:', currentQuestion.questionId, displayAnswer);
            } else {
                // 如果没有保存的答案，清空答案显示
                displayAnswer = getEmptyAnswerByType(currentQuestion.type);
                console.log('[练习] 清空答案显示:', currentQuestion.questionId, currentQuestion.type);
            }
            
            // 更新当前题目数据
            const questionData = {
                ...currentQuestion,
                answer: displayAnswer,
                currentIndex: currentQuestionIndex,
                totalCount: totalQuestions
            };
            
            window.GlobalData.set('practice.currentQuestion', questionData);
            window.GlobalData.set('practice.currentQuestionIndex', currentQuestionIndex);
            window.GlobalData.set('practice.totalQuestions', totalQuestions);
            
            // 强制同步到AMIS并刷新数据
            if (window.amisInstance) {
                // 构建完整的数据结构，确保AMIS表达式能够正确访问
                const updateData = {
                    practice: {
                        ...window.globalData.practice,
                        currentQuestion: questionData,
                        currentQuestionIndex: currentQuestionIndex,
                        totalQuestions: totalQuestions
                    },
                    // 直接设置当前答案到根级别，确保表单控件能够正确绑定
                    currentAnswer: displayAnswer
                };
                
                // 使用 updateProps 方法强制更新数据
                window.amisInstance.updateProps({ data: updateData });
                
                console.log('[练习] 数据已同步到AMIS:', {
                    currentQuestionIndex,
                    totalQuestions,
                    isLastQuestion: currentQuestionIndex >= totalQuestions - 1,
                    currentAnswer: displayAnswer
                });
            }
        } catch (error) {
            console.error('[练习] 更新当前题目数据失败:', error);
        }
    }

    /**
     * 根据题目类型获取空答案
     * @param {string} questionType - 题目类型
     * @returns {*} 对应类型的空答案
     */
    function getEmptyAnswerByType(questionType) {
        switch (questionType) {
            case 'SingleChoice':
            case 'TrueFalse':
                return null; // 单选题和判断题清空选择
            case 'MultipleChoice':
                return []; // 多选题清空为空数组
            case 'FillInBlank':
            case 'Essay':
            default:
                return ''; // 主观题清空为空字符串
        }
    }

    /**
     * 更新题目UI显示
     */
    function updateQuestionUI() {
        try {
            // 更新题目编号显示
            const questionNumberElements = document.querySelectorAll('.question-number');
            questionNumberElements.forEach(el => {
                el.textContent = `第${currentQuestionIndex + 1}题`;
            });
            
            // 更新进度显示
            const progressElements = document.querySelectorAll('.question-progress');
            progressElements.forEach(el => {
                el.textContent = `${currentQuestionIndex + 1}/${totalQuestions}`;
            });
            
            console.log('[练习] UI更新完成');
        } catch (error) {
            console.error('[练习] 更新题目UI失败:', error);
        }
    }

    /**
     * 保存答案
     * @param {string} questionId - 题目ID
     * @param {*} answer - 答案
     * @returns {Promise<boolean>} 保存是否成功
     */
    async function saveAnswer(questionId, answer) {
        try {
            console.log('[练习] 保存答案:', questionId, answer);
            
            // 保存到本地缓存
            practiceAnswers.set(String(questionId), answer);
            
            // 更新全局数据
            const answers = window.GlobalData.get('practice.answers', new Map());
            answers.set(String(questionId), answer);
            window.GlobalData.set('practice.answers', answers);
            
            // 如果是当前题目，更新当前题目的答案显示
            if (currentQuestion && String(currentQuestion.questionId) === String(questionId)) {
                // 更新当前题目数据
                const updatedQuestionData = {
                    ...currentQuestion,
                    answer: answer
                };
                window.GlobalData.set('practice.currentQuestion', updatedQuestionData);
                
                // 同步到AMIS
                if (window.amisInstance) {
                    window.amisInstance.updateProps({ 
                        data: { 
                            currentAnswer: answer,
                            practice: {
                                ...window.globalData.practice,
                                currentQuestion: updatedQuestionData
                            }
                        } 
                    });
                }
            }
            
            // 发送到服务器
            if (recordId) {
                await sendAnswerToServer(recordId, questionId, answer);
            }
            
            // 检查是否为最后一题且已答题，如果是则自动完成练习
            await checkAutoComplete();
            
            return true;
        } catch (error) {
            console.error('[练习] 保存答案失败:', error);
            return false;
        }
    }

    /**
     * 发送答案到服务器
     * @param {string} recordId - 记录ID
     * @param {string} questionId - 题目ID
     * @param {*} answer - 答案
     * @param {number} retryCount - 重试次数
     * @returns {Promise<boolean>} 发送是否成功
     */
    async function sendAnswerToServer(recordId, questionId, answer, retryCount = 0) {
        try {
            const data = await apiRequest(`/exam/api/exam/client/practice/${recordId}/save-answer`, {
                method: 'POST',
                body: JSON.stringify({
                    questionId: String(questionId),
                    answer: answer
                })
            });
            
            console.log('[练习] 答案已保存到服务器:', questionId);
            return true;
        } catch (error) {
            console.error('[练习] 发送答案到服务器失败:', error);
            
            if (retryCount < CONSTANTS.MAX_RETRY_COUNT) {
                console.log('[练习] 重试保存答案...');
                await new Promise(resolve => setTimeout(resolve, CONSTANTS.RETRY_DELAY * (retryCount + 1)));
                return await sendAnswerToServer(recordId, questionId, answer, retryCount + 1);
            }
            
            return false;
        }
    }

    /**
     * 上一题
     * @returns {Promise<boolean>} 切换是否成功
     */
    async function previousQuestion() {
        if (currentQuestionIndex <= 0) {
            console.log('[练习] 已经是第一题了');
            return false;
        }
        
        currentQuestionIndex--;
        window.GlobalData.set('practice.currentQuestionIndex', currentQuestionIndex);
        
        return await loadCurrentQuestion();
    }

    /**
     * 下一题
     * @returns {Promise<boolean>} 切换是否成功
     */
    async function nextQuestion() {
        if (currentQuestionIndex >= totalQuestions - 1) {
            console.log('[练习] 已经是最后一题了');
            return false;
        }
        
        currentQuestionIndex++;
        window.GlobalData.set('practice.currentQuestionIndex', currentQuestionIndex);
        
        return await loadCurrentQuestion();
    }

    /**
     * 检查是否自动完成练习
     * @returns {Promise<void>}
     */
    async function checkAutoComplete() {
        try {
            // 检查是否为最后一题
            if (currentQuestionIndex >= totalQuestions - 1) {
                console.log('[练习] 当前是最后一题，检查是否已答题');
                
                // 检查当前题目是否已答题
                const currentQuestionId = currentQuestion?.questionId;
                if (currentQuestionId) {
                    const answer = practiceAnswers.get(String(currentQuestionId));
                    if (answer !== undefined && answer !== null && answer !== '') {
                        console.log('[练习] 最后一题已答题，自动完成练习');
                        
                        // 延迟1秒后自动完成，给用户一个反应时间
                        setTimeout(async () => {
                            await completePractice();
                        }, 1000);
                    }
                }
            }
        } catch (error) {
            console.error('[练习] 检查自动完成失败:', error);
        }
    }

    /**
     * 完成练习
     * @returns {Promise<void>}
     */
    async function completePractice() {
        try {
            console.log('[练习] 开始完成练习');
            showLoading('正在提交练习...');
            
            // 调用完成练习API
            const data = await apiRequest(`/exam/api/exam/client/practice/${recordId}/complete`, {
                method: 'POST'
            });
            
            console.log('[练习] 练习完成成功');
            
            // 显示完成提示
            if (window.amisInstance) {
                window.amisInstance.getComponentByName('app')?.dispatchEvent('toast', {
                    level: 'success',
                    msg: '练习已完成！'
                });
            }
            
            // 延迟2秒后跳转到我的练习界面
            setTimeout(() => {
                window.location.href = `/${window.tenantId}/exam/practice-history`;
            }, 2000);
            
        } catch (error) {
            console.error('[练习] 完成练习失败:', error);
            
            if (window.amisInstance) {
                window.amisInstance.getComponentByName('app')?.dispatchEvent('toast', {
                    level: 'error',
                    msg: '完成练习失败：' + error.message
                });
            }
        } finally {
            hideLoading();
        }
    }

    /**
     * 暴露全局方法
     */
    function exposeGlobalMethods() {
        window.saveAnswer = saveAnswer;
        window.previousQuestion = previousQuestion;
        window.nextQuestion = nextQuestion;
        window.loadPractice = loadPractice;
        window.loadCurrentQuestion = loadCurrentQuestion;
        window.completePractice = completePractice;
        
        // 添加调试方法
        window.testAnswerState = function() {
            console.log('[练习] 测试答案状态:');
            console.log('当前题目ID:', currentQuestion?.questionId);
            console.log('当前题目类型:', currentQuestion?.type);
            console.log('本地保存的答案:', practiceAnswers.get(currentQuestion?.questionId));
            console.log('题目数据中的答案:', currentQuestion?.answer);
            
            if (window.amisInstance && window.amisInstance.props && window.amisInstance.props.store) {
                const storeData = window.amisInstance.props.store.data;
                console.log('AMIS当前答案:', storeData.currentAnswer);
                console.log('AMIS题目答案:', storeData.practice?.currentQuestion?.answer);
            }
        };
        
        window.clearCurrentAnswer = function() {
            console.log('[练习] 清空当前答案');
            if (currentQuestion) {
                practiceAnswers.delete(currentQuestion.questionId);
                updateCurrentQuestionData();
            }
        };
        
        window.setTestAnswer = function(answer) {
            console.log('[练习] 设置测试答案:', answer);
            if (currentQuestion) {
                saveAnswer(currentQuestion.questionId, answer);
            }
        };
    }

    // 练习页面配置
    const practicePage = {
        type: 'page',
        className: 'practice-page',
        body: [
            // 头部固定区域
            {
                type: 'container',
                className: 'practice-header-container',
                body: [
                    {
                        type: 'service',
                        api: '/identity/api/identity/profile',
                        className: 'practice-client-header',
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
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="practice-progress"><span class="question-progress">${practice.currentQuestionIndex + 1}/${practice.totalQuestions}</span></div>'
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            // 题目内容区域
            {
                type: 'container',
                className: 'practice-content-container',
                body: [
                    {
                        type: 'panel',
                        className: 'practice-panel',
                        header: {
                            type: 'tpl',
                            tpl: '<div class="practice-title">${practice.name}</div>'
                        },
                        body: [
                            // 题目显示区域
                            {
                                type: 'container',
                                className: 'question-display-area',
                                body: [
                                    {
                                        type: 'tpl',
                                        tpl: '<div class="question-number-badge"><span class="question-number">第${practice.currentQuestionIndex + 1}题</span></div>',
                                        className: 'question-number-container'
                                    },
                                    {
                                        type: 'container',
                                        className: 'question-content-container',
                                        body: [
                                            {
                                                type: 'tpl',
                                                tpl: '<div class="question-content"><pre>${practice.currentQuestion.content | raw}</pre><span class="question-score">（${practice.currentQuestion.score}分）</span></div>',
                                                className: 'question-text'
                                            },
                                            // 答题区域
                                            {
                                                type: 'container',
                                                className: 'answer-area',
                                                body: [
                                                    // 单选题
                                                    {
                                                        type: 'radios',
                                                        name: 'currentAnswer',
                                                        source: '${practice.currentQuestion.options}',
                                                        mode: 'vertical',
                                                        value: '${practice.currentQuestion.answer}',
                                                        visibleOn: "${practice.currentQuestion.type === 'SingleChoice'}",
                                                        className: 'question-options single-choice-options',
                                                        onEvent: {
                                                            change: {
                                                                actions: [
                                                                    {
                                                                        actionType: 'custom',
                                                                        script: 'saveAnswer(event.data.practice.currentQuestion.questionId, event.data.value);'
                                                                    }
                                                                ]
                                                            }
                                                        }
                                                    },
                                                    // 多选题
                                                    {
                                                        type: 'checkboxes',
                                                        name: 'currentAnswer',
                                                        source: '${practice.currentQuestion.options}',
                                                        mode: 'vertical',
                                                        value: '${practice.currentQuestion.answer}',
                                                        visibleOn: "${practice.currentQuestion.type === 'MultipleChoice'}",
                                                        className: 'question-options multiple-choice-options',
                                                        onEvent: {
                                                            change: {
                                                                actions: [
                                                                    {
                                                                        actionType: 'custom',
                                                                        script: 'saveAnswer(event.data.practice.currentQuestion.questionId, event.data.value);'
                                                                    }
                                                                ]
                                                            }
                                                        }
                                                    },
                                                    // 判断题（仅勾叉符号）
                                                    {
                                                        type: 'radios',
                                                        name: 'currentAnswer',
                                                        options: [
                                                            { label: '✓', value: 'True' },
                                                            { label: '✗', value: 'False' }
                                                        ],
                                                        mode: 'vertical',
                                                        value: '${practice.currentQuestion.answer}',
                                                        visibleOn: "${practice.currentQuestion.type === 'TrueFalse'}",
                                                        className: 'question-options true-false-options',
                                                        labelClassName: 'true-false-label',
                                                        onEvent: {
                                                            change: {
                                                                actions: [
                                                                    {
                                                                        actionType: 'custom',
                                                                        script: 'saveAnswer(event.data.practice.currentQuestion.questionId, event.data.value);'
                                                                    }
                                                                ]
                                                            }
                                                        }
                                                    },
                                                    // 主观题
                                                    {
                                                        type: 'textarea',
                                                        name: 'currentAnswer',
                                                        placeholder: '请输入答案',
                                                        minRows: 4,
                                                        maxRows: 8,
                                                        value: '${practice.currentQuestion.answer}',
                                                        visibleOn: "${practice.currentQuestion.type !== 'SingleChoice' && practice.currentQuestion.type !== 'MultipleChoice' && practice.currentQuestion.type !== 'TrueFalse'}",
                                                        className: 'question-textarea subjective-answer',
                                                        onEvent: {
                                                            change: {
                                                                actions: [
                                                                    {
                                                                        actionType: 'custom',
                                                                        script: 'saveAnswer(event.data.practice.currentQuestion.questionId, event.data.value);'
                                                                    }
                                                                ]
                                                            }
                                                        }
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            // 导航按钮区域
            {
                type: 'container',
                className: 'practice-navigation-container',
                body: [
                    {
                        type: 'flex',
                        justify: 'space-between',
                        className: 'navigation-buttons',
                        items: [
                            {
                                type: 'button',
                                label: '上一题',
                                level: 'default',
                                size: 'lg',
                                className: 'prev-question-btn',
                                disabledOn: '${practice.currentQuestionIndex <= 0}',
                                onEvent: {
                                    click: {
                                        actions: [
                                            {
                                                actionType: 'custom',
                                                script: 'previousQuestion();'
                                            }
                                        ]
                                    }
                                }
                            },
                            {
                                type: 'flex',
                                className: 'center-buttons',
                                items: [
                                    {
                                        type: 'button',
                                        label: '完成练习',
                                        level: 'success',
                                        size: 'lg',
                                        className: 'complete-practice-btn',
                                        onEvent: {
                                            click: {
                                                actions: [
                                                    {
                                                        actionType: 'dialog',
                                                        dialog: {
                                                            title: '确认完成练习',
                                                            body: '确定要完成当前练习吗？完成后将无法继续答题。',
                                                            actions: [
                                                                {
                                                                    type: 'button',
                                                                    label: '取消',
                                                                    level: 'default',
                                                                    actionType: 'cancel'
                                                                },
                                                                {
                                                                    type: 'button',
                                                                    label: '确定完成',
                                                                    level: 'primary',
                                                                    onEvent: {
                                                                        click: {
                                                                            actions: [
                                                                                {
                                                                                    actionType: 'custom',
                                                                                    script: 'completePractice();'
                                                                                }
                                                                            ]
                                                                        }
                                                                    }
                                                                }
                                                            ]
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                ]
                            },
                            {
                                type: 'button',
                                label: '下一题',
                                level: 'primary',
                                size: 'lg',
                                className: 'next-question-btn',
                                disabledOn: '${practice.currentQuestionIndex >= practice.totalQuestions - 1}',
                                onEvent: {
                                    click: {
                                        actions: [
                                            {
                                                actionType: 'custom',
                                                script: 'nextQuestion();'
                                            }
                                        ]
                                    }
                                }
                            }
                        ]
                    }
                ]
            }
        ]
    };

    // 初始化AMIS
    let amisInstance = amis.embed(
        '#root',
        practicePage,
        {
            location: history.location,
            data: {
                practice: {
                    name: '加载中...',
                    currentQuestionIndex: 0,
                    totalQuestions: 0,
                    currentQuestion: {
                        id: '',
                        questionId: '',
                        content: '正在加载题目...',
                        type: 'SingleChoice',
                        score: 0,
                        options: [],
                        answer: null
                    },
                    questions: [],
                    answers: new Map()
                },
                // 当前答案字段，用于表单控件绑定
                currentAnswer: null
            },
            locale: 'zh-CN',
            context: {
                WEB_HOST: webHost || ''
            }
        },
        {
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
                if (response.status === 403) {
                    alert('您没有权限进行此练习！');
                    window.location.href = `/${window.tenantId}/exam/app`;
                    return { msg: '您没有权限访问此页面，请联系管理员！' };
                }
                else if (response.status === 401) {
                    window.location.href = `/${window.tenantId}/exam/login`;
                    return { msg: '登录过期！' };
                }
                return payload;
            },
            theme: 'antd'
        }
    );

    // 设置全局AMIS实例
    window.amisInstance = amisInstance;

    // 暴露全局方法
    exposeGlobalMethods();

    /**
     * 显示加载状态
     * @param {string} text - 加载提示文本
     */
    function showLoading(text = '正在加载练习...') {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = 'flex';
            const textElement = loadingElement.querySelector('.practice-loading-text');
            if (textElement) {
                textElement.textContent = text;
            }
        }
    }

    /**
     * 隐藏加载状态
     */
    function hideLoading() {
        const loadingElement = document.getElementById('loading');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
    }

    // 页面加载完成后初始化
    window.addEventListener('DOMContentLoaded', function() {
        console.log('[练习] 页面加载完成，开始初始化...');
        
        showLoading('正在加载练习数据...');
        
        setTimeout(async () => {
            try {
                // 先加载租户信息
                await loadTenantInfo();
                
                // 再加载练习数据
                const success = await loadPractice(practiceId);
                if (success) {
                    isInitialized = true;
                    hideLoading();
                    console.log('[练习] 初始化完成');
                } else {
                    hideLoading();
                    console.error('[练习] 初始化失败');
                    alert('练习加载失败，请刷新页面重试');
                }
            } catch (error) {
                hideLoading();
                console.error('[练习] 初始化过程中出错:', error);
                alert('练习加载失败，请刷新页面重试');
            }
        }, 500);
    });

    // 页面卸载时的清理
    window.addEventListener('beforeunload', function() {
        console.log('[练习] 页面即将卸载，执行清理...');
    });

})(); 