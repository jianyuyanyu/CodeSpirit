/**
 * 问卷参与页面JavaScript
 * 负责问卷加载、表单渲染、数据提交等功能
 */

(function() {
    'use strict';

    // 全局变量
    let accessCode = null;
    let sessionId = null;
    let surveyData = null;
    let amisInstance = null;
    let startTime = null;
    let currentTenantId = null;
    let isSubmitting = false; // 标记是否正在提交

    /**
     * 从URL路径解析租户ID
     */
    function getTenantIdFromPath() {
        try {
            const pathname = window.location.pathname;
            const pathParts = pathname.split('/').filter(part => part.length > 0);
            
            // 支持路径格式：/{tenantId}/survey/participate/{surveyId}
            // 其中 tenantId 是可选的
            
            const surveyIndex = pathParts.indexOf('survey');
            if (surveyIndex > 0) {
                // 检查 /{tenantId}/survey/participate/{surveyId} 格式
                // 租户ID在survey之前的位置
                return pathParts[surveyIndex - 1];
            }
            
            return null;
        } catch (error) {
            console.warn('从URL路径解析租户ID失败:', error);
            return null;
        }
    }

    /**
     * 获取租户信息
     */
    function getTenantInfo() {
        try {
            // 优先从URL路径获取租户ID（用于公开问卷）
            currentTenantId = getTenantIdFromPath();
            
            // 如果路径中没有，尝试从页面数据获取
            if (!currentTenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                currentTenantId = window.surveyPageData.tenantId;
            }
            
            // 如果还是没有，尝试从TokenManager获取（用于登录用户）
            if (!currentTenantId && window.TokenManager && typeof window.TokenManager.getCurrentTenantId === 'function') {
                currentTenantId = window.TokenManager.getCurrentTenantId();
            }
            
            // 最后尝试从platformInfo获取
            if (!currentTenantId && window.platformInfo && window.platformInfo.tenantId) {
                currentTenantId = window.platformInfo.tenantId;
            }
            
            console.log('当前租户ID:', currentTenantId, '来源: URL路径解析');
        } catch (error) {
            console.warn('获取租户信息失败:', error);
        }
    }

    /**
     * 生成会话ID
     */
    function generateSessionId() {
        const timestamp = Date.now();
        const random = Math.random().toString(36).substr(2, 9);
        return `session_${timestamp}_${random}`;
    }

    /**
     * 获取问卷访问码
     */
    function getSurveyAccessCode() {
        // 优先从页面数据获取
        if (window.surveyPageData && window.surveyPageData.accessCode) {
            return window.surveyPageData.accessCode;
        }
        
        // 备用方案：从URL获取
        const pathParts = window.location.pathname.split('/');
        const participateIndex = pathParts.indexOf('participate');
        if (participateIndex !== -1 && participateIndex + 1 < pathParts.length) {
            return pathParts[participateIndex + 1];
        }
        return null;
    }


    /**
     * 显示加载状态
     */
    function showLoading() {
        const container = document.getElementById('loading-container');
        const surveyContainer = document.getElementById('survey-container');
        const errorContainer = document.getElementById('error-container');
        
        if (container) container.style.display = 'block';
        if (surveyContainer) surveyContainer.style.display = 'none';
        if (errorContainer) errorContainer.style.display = 'none';
    }

    /**
     * 显示问卷内容
     */
    function showSurvey() {
        const container = document.getElementById('loading-container');
        const surveyContainer = document.getElementById('survey-container');
        const errorContainer = document.getElementById('error-container');
        
        if (container) container.style.display = 'none';
        if (surveyContainer) surveyContainer.style.display = 'block';
        if (errorContainer) errorContainer.style.display = 'none';
    }

    /**
     * 显示错误信息
     */
    function showError(message) {
        const container = document.getElementById('loading-container');
        const surveyContainer = document.getElementById('survey-container');
        const errorContainer = document.getElementById('error-container');
        const errorMessage = document.getElementById('error-message');
        
        if (container) container.style.display = 'none';
        if (surveyContainer) surveyContainer.style.display = 'none';
        if (errorContainer) errorContainer.style.display = 'block';
        if (errorMessage) errorMessage.textContent = message;
    }

    /**
     * 更新问卷信息显示
     */
    function updateSurveyInfo(survey) {
        try {
            // 显示问卷信息区域
            const surveyInfo = document.getElementById('survey-info');
            if (surveyInfo) {
                surveyInfo.style.display = 'block';
            }
            
            // 更新预计用时
            const durationElement = document.getElementById('survey-duration');
            if (durationElement && survey.estimatedMinutes) {
                durationElement.textContent = `预计用时: ${survey.estimatedMinutes}分钟`;
            }
            
            // 更新题目数量
            const questionCountElement = document.getElementById('survey-question-count');
            if (questionCountElement && survey.questionCount) {
                questionCountElement.textContent = `题目数量: ${survey.questionCount}题`;
            }
            
            // 更新进度条（初始化为10%，表示问卷已加载）
            updateProgress(10);
            
        } catch (error) {
            console.warn('更新问卷信息失败:', error);
        }
    }

    /**
     * 更新进度条
     */
    function updateProgress(percentage) {
        try {
            const progressFill = document.getElementById('survey-progress');
            if (progressFill) {
                // 只修改宽度，不修改高度
                progressFill.style.width = `${Math.max(0, Math.min(100, percentage))}%`;
                // 确保高度保持为4px
                progressFill.style.height = '4px';
                progressFill.style.maxHeight = '4px';
            }
            
            // 同时确保进度条容器的高度正确
            const progressBar = document.querySelector('.survey-progress-bar');
            if (progressBar) {
                progressBar.style.height = '4px';
                progressBar.style.maxHeight = '4px';
            }
        } catch (error) {
            console.warn('更新进度条失败:', error);
        }
    }

    /**
     * 保存问卷答案并更新进度
     * 参考exam.js中的saveAnswer逻辑
     */
    function saveQuestionAnswer(questionId, answer) {
        try {
            console.log(`[问卷答题] 保存题目 ${questionId} 的答案:`, answer);
            
            // 立即更新进度
            updateFormProgress();
            
            return true;
        } catch (error) {
            console.error(`[问卷答题] 保存答案时出错:`, error);
            return false;
        }
    }

    /**
     * 初始化表单进度跟踪
     * 参考exam.js，移除DOM事件监听，改为在AMIS组件中直接绑定事件
     */
    function initFormProgressTracking() {
        try {
            if (!surveyData || !surveyData.questions) {
                return;
            }

            const totalQuestions = surveyData.questions.length;
            console.log('初始化进度跟踪，总题目数:', totalQuestions);

            // 初始计算进度
            setTimeout(updateFormProgress, 500);
        } catch (error) {
            console.warn('初始化进度跟踪失败:', error);
        }
    }

    /**
     * 更新表单进度
     * 参考exam.js的逻辑，改进答案检测机制
     */
    function updateFormProgress() {
        try {
            if (!surveyData || !surveyData.questions) {
                return;
            }

            const totalQuestions = surveyData.questions.length;
            let answeredQuestions = 0;

            // 统计已回答的题目 - 改进检测逻辑
            surveyData.questions.forEach(question => {
                const fieldName = `question_${question.id}`;
                let hasAnswer = false;


                if (hasAnswer) {
                    answeredQuestions++;
                }
            });

            // 计算进度百分比 (10% 基础 + 90% 答题进度)
            const baseProgress = 10;
            const answerProgress = (answeredQuestions / totalQuestions) * 90;
            const totalProgress = baseProgress + answerProgress;

            updateProgress(totalProgress);
            console.log(`进度更新: ${answeredQuestions}/${totalQuestions} 题已回答，进度: ${totalProgress.toFixed(1)}%`);
        } catch (error) {
            console.warn('更新表单进度失败:', error);
        }
    }

    /**
     * 构建带租户信息的请求头
     */
    function buildRequestHeaders() {
        const headers = {
            'Content-Type': 'application/json',
            'X-Forwarded-With': 'CodeSpirit'
        };
        
        // 添加认证头
        let token = null;
        try {
            token = window.TokenManager?.getToken?.() || null;
        } catch (e) {
            console.warn('获取Token失败:', e);
        }
        
        if (token) {
            //headers['Authorization'] = 'Bearer ' + token;
        }
        
        // 添加租户ID头部（如果存在）
        if (currentTenantId) {
            headers['X-Tenant-Id'] = currentTenantId;
        }
        
        return headers;
    }

    /**
     * 加载问卷数据
     */
    async function loadSurvey() {
        try {
            showLoading();
            
            // 获取租户信息
            getTenantInfo();
            
            // 构建请求头
            const headers = buildRequestHeaders();
            
            // 动态获取租户ID
            let tenantId = currentTenantId;
            if (!tenantId) {
                tenantId = getTenantIdFromPath();
                if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                    tenantId = window.surveyPageData.tenantId;
                }
                if (!tenantId && window.TokenManager && typeof window.TokenManager.getCurrentTenantId === 'function') {
                    tenantId = window.TokenManager.getCurrentTenantId();
                }
                if (!tenantId && window.currentTenantId) {
                    tenantId = window.currentTenantId;
                }
            }
            
            // 构建查询参数
            const queryParams = new URLSearchParams();
            if (tenantId) {
                queryParams.append('tenantId', tenantId);
            }
            
            // 构建API URL - 仅支持访问码模式
            const accessCode = getSurveyAccessCode();
            if (!accessCode) {
                console.error('未找到访问码');
                showError('无效的访问码');
                return;
            }
            
            const checkUrl = `/survey/api/app/surveys/public/${accessCode}/check${queryParams.toString() ? '?' + queryParams.toString() : ''}`;
            const detailUrl = `/survey/api/app/surveys/public/${accessCode}${queryParams.toString() ? '?' + queryParams.toString() : ''}`;
            console.log('使用访问码模式，访问码:', accessCode);
            
            console.log('检查问卷可用性 URL:', checkUrl);
            console.log('请求头:', headers);
            
            const checkResponse = await fetch(checkUrl, {
                method: 'GET',
                headers: headers
            });
            
            console.log('问卷检查响应状态:', checkResponse.status);
            const checkResult = await checkResponse.json();
            console.log('问卷检查结果:', checkResult);
            
            if (checkResult.status !== 0 && !checkResult.success) {
                console.error('问卷检查失败:', checkResult);
                showError(checkResult.msg || checkResult.message || '问卷不可用');
                return;
            }

            console.log('问卷检查通过，继续检查参与权限');
            console.log('canParticipate:', checkResult.data?.canParticipate);

            if (!checkResult.data || !checkResult.data.canParticipate) {
                let message = '问卷不可参与';
                if (checkResult.data.isExpired) {
                    message = '问卷已过期，无法参与';
                } else if (!checkResult.data.isPublished) {
                    message = '问卷未发布';
                } else if (!checkResult.data.isPublic) {
                    message = '问卷不允许公开访问';
                }
                console.error('问卷不可参与:', message, checkResult.data);
                showError(message);
                return;
            }

            console.log('问卷可以参与，继续检查提交状态');
            
            // 检查是否已经提交过
            const submittedParams = new URLSearchParams({
                accessCode: accessCode,
                sessionId: sessionId
            });
            if (tenantId) {
                submittedParams.append('tenantId', tenantId);
            }
            
            const submittedUrl = `/survey/api/app/responses/check-submitted?${submittedParams.toString()}`;
            console.log('检查提交状态 URL:', submittedUrl);
            
            const submittedResponse = await fetch(submittedUrl, {
                method: 'GET',
                headers: headers
            });
            
            console.log('提交检查响应状态:', submittedResponse.status);
            const submittedResult = await submittedResponse.json();
            console.log('提交检查结果:', submittedResult);
            
            if ((submittedResult.status === 0 || submittedResult.success) && submittedResult.data && submittedResult.data.isSubmitted) {
                console.log('用户已提交过此问卷');
                showError('您已经提交过此问卷，感谢您的参与！');
                return;
            }

            console.log('用户未提交过此问卷，继续加载问卷详情');
            
            // 加载问卷详情 - 使用之前构建的detailUrl
            console.log('加载问卷详情 URL:', detailUrl);
            
            const response = await fetch(detailUrl, {
                method: 'GET',
                headers: headers
            });
            
            console.log('问卷详情响应状态:', response.status);
            const result = await response.json();
            console.log('问卷详情结果:', result);
            
            if (result.status !== 0 && !result.success) {
                console.error('加载问卷详情失败:', result);
                showError(result.msg || result.message || '加载问卷失败');
                return;
            }

            console.log('问卷详情加载成功，开始渲染');
            surveyData = result.data;
            startTime = new Date();
            console.log('问卷数据:', surveyData);
            renderSurvey(surveyData);
            
        } catch (error) {
            console.error('加载问卷失败:', error);
            console.error('错误详情:', {
                message: error.message,
                stack: error.stack,
                name: error.name
            });
            showError('网络错误，请检查网络连接后重试');
        }
    }

    /**
     * 渲染问卷表单
     */
    function renderSurvey(survey) {
        // 验证问卷数据
        if (!survey || !survey.questions || !Array.isArray(survey.questions)) {
            console.error('无效的问卷数据:', survey);
            showError('问卷数据格式错误');
            return;
        }

        console.log('开始渲染问卷:', survey.title, '题目数量:', survey.questions.length);

        const amisSchema = {
            type: 'page',
            title: survey.title || '问卷调查',
            subTitle: survey.description || '',
            className: 'survey-form-container',
            body: [
                {
                    type: 'alert',
                    level: 'info',
                    body: `本问卷共 ${survey.questionCount || survey.questions.length} 道题，预计需要 ${survey.estimatedMinutes || 5} 分钟完成。请认真填写，提交后不可修改。`,
                    showCloseButton: false,
                    className: 'survey-info-alert'
                },
                {
                    type: 'form',
                    name: 'surveyForm',
                    api: {
                        method: 'post',
                        url: '/survey/api/app/responses/submit',
                        requestAdaptor: function(api) {
                            // 标记开始提交，避免页面离开提醒
                            console.log('开始提交问卷，设置提交状态');
                            isSubmitting = true;
                            
                            // 转换表单数据为API需要的格式
                            const formData = api.data;
                            const answers = [];
                            
                            Object.keys(formData).forEach(key => {
                                if (key.startsWith('question_')) {
                                    const questionId = parseInt(key.replace('question_', ''));
                                    const value = formData[key];
                                    if (value !== null && value !== undefined && value !== '') {
                                        let answerText = '';
                                        let answerValue = '';
                                        
                                        if (Array.isArray(value)) {
                                            // 多选题
                                            answerValue = value.join(',');
                                            answerText = value.join(',');
                                        } else {
                                            // 单选题或文本题
                                            answerValue = String(value);
                                            answerText = String(value);
                                        }
                                        
                                        answers.push({
                                            questionId: questionId,
                                            answerText: answerText,
                                            answerValue: answerValue
                                        });
                                    }
                                }
                            });
                            
                            // 构建提交数据
                            const submitData = {
                                sessionId: sessionId,
                                answers: answers,
                                metadata: JSON.stringify({
                                    startTime: startTime,
                                    userAgent: navigator.userAgent,
                                    screenResolution: `${screen.width}x${screen.height}`,
                                    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone
                                })
                            };

                            // 添加访问码
                            submitData.accessCode = accessCode;

                            api.data = submitData;
                            
                            return api;
                        },
                        responseAdaptor: function(payload, response, api) {
                            console.log('API响应适配器 - payload:', payload);
                            console.log('API响应适配器 - response:', response);
                            
                            // 检查响应是否成功
                            if (payload && (payload.status === 0 || payload.success)) {
                                console.log('问卷提交成功');
                                
                                // 标记正在提交，避免页面离开提醒
                                isSubmitting = true;
                                
                                // 保存完成时间到localStorage
                                if (startTime) {
                                    localStorage.setItem('survey-start-time', startTime.getTime().toString());
                                }
                                
                                // 返回成功响应，让AMIS处理跳转
                                return {
                                    status: 0,
                                    msg: '问卷提交成功！'
                                };
                            } else {
                                // 提交失败，重置提交状态
                                isSubmitting = false;
                                
                                const message = payload?.msg || payload?.message || '提交失败，请重试';
                                console.error('问卷提交失败:', payload);
                                
                                // 返回错误消息给AMIS显示
                                return {
                                    status: 1,
                                    msg: message
                                };
                            }
                        }
                    },
                    submitText: '提交问卷',
                    resetAfterSubmit: false,
                    className: 'survey-form',
                    redirect: (function() {
                        // 动态计算跳转URL
                        let tenantId = currentTenantId;
                        if (!tenantId) {
                            tenantId = getTenantIdFromPath();
                            if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                                tenantId = window.surveyPageData.tenantId;
                            }
                            if (!tenantId && window.currentTenantId) {
                                tenantId = window.currentTenantId;
                            }
                        }
                        
                        if (tenantId) {
                            return `/${tenantId}/survey/success`;
                        } else {
                            return '/survey/success';
                        }
                    })(),
                    onFinished: function(values, response) {
                        console.log('表单提交完成 - onFinished回调:', response);
                        console.log('表单提交完成 - values:', values);
                        
                        // 标记正在提交，避免页面离开提醒
                        isSubmitting = true;
                        
                        // 备用跳转方案：如果redirect属性没有生效，手动跳转
                        setTimeout(() => {
                            // 动态获取租户ID
                            let tenantId = currentTenantId;
                            if (!tenantId) {
                                tenantId = getTenantIdFromPath();
                                if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                                    tenantId = window.surveyPageData.tenantId;
                                }
                                if (!tenantId && window.currentTenantId) {
                                    tenantId = window.currentTenantId;
                                }
                            }
                            
                            let successUrl;
                            if (tenantId) {
                                successUrl = `/${tenantId}/survey/success`;
                            } else {
                                successUrl = '/survey/success';
                            }
                            
                            console.log('准备跳转到成功页面:', successUrl);
                            window.location.href = successUrl;
                        }, 1000);
                    },
                    onValidate: function(values) {
                        const errors = {};
                        
                        // 验证必填题目
                        surveyData.questions.forEach(question => {
                            if (question.isRequired) {
                                const fieldName = `question_${question.id}`;
                                const value = values[fieldName];
                                
                                if (!value || (Array.isArray(value) && value.length === 0) || 
                                    (typeof value === 'string' && value.trim() === '')) {
                                    errors[fieldName] = '此题为必填项';
                                }
                            }
                        });
                        
                        return Object.keys(errors).length > 0 ? errors : null;
                    },
                    body: generateQuestionFields(survey.questions)
                }
            ]
        };

        // 更新问卷信息显示
        updateSurveyInfo(survey);
        
        // 渲染AMIS表单
        try {
            // 确保AMIS已加载
            if (typeof amisRequire === 'undefined') {
                console.error('AMIS未正确加载');
                showError('页面组件加载失败，请刷新页面重试');
                return;
            }
            
            const amis = amisRequire('amis/embed');
            
            // AMIS 配置选项
            const amisOptions = {
                locale: 'zh-CN',
                theme: 'antd'
            };

            // AMIS 事件处理器
            const amisHandlers = {
                requestAdaptor: function(api) {
                // 添加认证头和其他必要信息
                let token = null;
                try {
                    token = window.TokenManager?.getToken?.() || null;
                } catch (e) {
                    console.warn('获取Token失败:', e);
                }
                
                // 动态获取当前租户ID
                let tenantId = currentTenantId;
                if (!tenantId) {
                    // 重新尝试获取租户信息
                    tenantId = getTenantIdFromPath();
                    if (!tenantId && window.surveyPageData && window.surveyPageData.tenantId) {
                        tenantId = window.surveyPageData.tenantId;
                    }
                    if (!tenantId && window.TokenManager && typeof window.TokenManager.getCurrentTenantId === 'function') {
                        tenantId = window.TokenManager.getCurrentTenantId();
                    }
                    if (!tenantId && window.currentTenantId) {
                        tenantId = window.currentTenantId;
                    }
                }
                
                // 构建请求头
                const headers = {
                    ...api.headers,
                    //'Authorization': token ? 'Bearer ' + token : '',
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json'
                };
                
                // 添加租户ID头部（如果存在）
                if (tenantId) {
                    headers['X-Tenant-Id'] = tenantId;
                }
                
                // 确保所有请求都包含租户参数
                const data = { ...api.data };
                if (tenantId && !data.tenantId) {
                    data.tenantId = tenantId;
                }
                
                return {
                    ...api,
                    headers: headers,
                    data: data
                };
            },

            responseAdaptor: function(api, payload, query, request, response) {
                // 处理错误响应
                if (response.status === 401) {
                    window.location.href = '/login';
                    return { msg: '登录过期！' };
                } else if (response.status === 403) {
                    return { msg: '您没有权限访问此页面！' };
                }
                
                return payload;
            }
        };
        
        // 清理之前的实例
        if (amisInstance) {
            try {
                amisInstance.unmount();
            } catch (e) {
                console.warn('清理AMIS实例失败:', e);
            }
        }
        
        // 创建新的AMIS实例
        amisInstance = amis.embed('#survey-container', amisSchema, amisOptions, amisHandlers);
        
        console.log('问卷表单渲染完成');
        
        // 确保问卷容器显示
        setTimeout(() => {
            showSurvey();
            console.log('问卷容器显示状态已更新');
            
            // 初始化表单进度监听
            initFormProgressTracking();
            
            // 延迟更新初始进度，确保AMIS表单完全渲染
            setTimeout(() => {
                console.log('开始计算初始进度');
                updateFormProgress();
            }, 1000);
        }, 100);
        
        } catch (error) {
            console.error('渲染AMIS表单失败:', error);
            showError('表单渲染失败，请刷新页面重试');
        }
    }

    /**
     * 生成题目字段配置
     */
    function generateQuestionFields(questions) {
        if (!Array.isArray(questions)) {
            console.error('questions不是数组:', questions);
            return [];
        }

        return questions.map((question, index) => {
            // 验证题目数据
            if (!question || typeof question !== 'object') {
                console.warn(`题目 ${index} 数据无效:`, question);
                return null;
            }

            const baseField = {
                name: `question_${question.id || index}`,
                label: question.title || `题目 ${index + 1}`,
                description: question.description || '',
                required: question.isRequired || false,
                className: 'survey-question-field'
            };

            switch (question.type) {
                case 1: // SingleChoice - 单选题
                    return {
                        ...baseField,
                        type: 'radios',
                        options: question.options.map(opt => ({
                            label: opt.text,
                            value: opt.value
                        })),
                        className: baseField.className + ' survey-radio-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 2: // MultipleChoice - 多选题
                    return {
                        ...baseField,
                        type: 'checkboxes',
                        options: question.options.map(opt => ({
                            label: opt.text,
                            value: opt.value
                        })),
                        className: baseField.className + ' survey-checkbox-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 3: // Text - 填空题
                    return {
                        ...baseField,
                        type: 'input-text',
                        placeholder: '请输入您的答案',
                        className: baseField.className + ' survey-text-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 4: // Number - 数字题
                    return {
                        ...baseField,
                        type: 'input-number',
                        placeholder: '请输入数字',
                        className: baseField.className + ' survey-number-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 5: // Rating - 评分题
                    return {
                        ...baseField,
                        type: 'input-rating',
                        count: 5,
                        className: baseField.className + ' survey-rating-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 6: // Date - 日期题
                    return {
                        ...baseField,
                        type: 'input-date',
                        format: 'YYYY-MM-DD',
                        className: baseField.className + ' survey-date-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 7: // Time - 时间题
                    return {
                        ...baseField,
                        type: 'input-time',
                        format: 'HH:mm',
                        className: baseField.className + ' survey-time-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 8: // DateTime - 日期时间题
                    return {
                        ...baseField,
                        type: 'input-datetime',
                        format: 'YYYY-MM-DD HH:mm',
                        className: baseField.className + ' survey-datetime-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 9: // Textarea - 长文本题
                    return {
                        ...baseField,
                        type: 'textarea',
                        minRows: 3,
                        maxRows: 6,
                        placeholder: '请输入您的详细答案',
                        className: baseField.className + ' survey-textarea-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 10: // Matrix - 矩阵题
                    return {
                        ...baseField,
                        type: 'matrix-checkboxes',
                        rows: question.options.map(opt => ({
                            label: opt.text,
                            value: opt.value
                        })),
                        columns: [
                            { label: '非常不满意', value: '1' },
                            { label: '不满意', value: '2' },
                            { label: '一般', value: '3' },
                            { label: '满意', value: '4' },
                            { label: '非常满意', value: '5' }
                        ],
                        className: baseField.className + ' survey-matrix-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                case 11: // Ranking - 排序题
                    return {
                        ...baseField,
                        type: 'input-array',
                        inline: false,
                        draggable: true,
                        items: {
                            type: 'select',
                            options: question.options.map(opt => ({
                                label: opt.text,
                                value: opt.value
                            }))
                        },
                        className: baseField.className + ' survey-ranking-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
                    
                default:
                    console.warn(`未知的题目类型: ${question.type}, 题目ID: ${question.id}`);
                    return {
                        ...baseField,
                        type: 'input-text',
                        placeholder: '请输入您的答案',
                        className: baseField.className + ' survey-default-field',
                        onEvent: {
                            change: {
                                actions: [
                                    {
                                        actionType: "custom",
                                        script: `saveQuestionAnswer(${question.id || index}, event.data.value);`
                                    }
                                ]
                            }
                        }
                    };
            }
        }).filter(field => field !== null); // 过滤掉无效的字段
    }

    /**
     * 重新加载问卷
     */
    function reloadSurvey() {
        loadSurvey();
    }

    /**
     * 页面初始化
     */
    /**
     * 初始化进度条样式
     */
    function initProgressBar() {
        try {
            const progressBar = document.querySelector('.survey-progress-bar');
            const progressFill = document.getElementById('survey-progress');
            
            if (progressBar) {
                progressBar.style.height = '4px';
                progressBar.style.maxHeight = '4px';
                progressBar.style.overflow = 'hidden';
                console.log('进度条容器样式已初始化');
            }
            
            if (progressFill) {
                progressFill.style.height = '4px';
                progressFill.style.maxHeight = '4px';
                progressFill.style.width = '0%';
                console.log('进度条填充样式已初始化');
            }
        } catch (error) {
            console.warn('初始化进度条样式失败:', error);
        }
    }

    function init() {
        console.log('问卷参与页面初始化开始');
        
        // 首先初始化进度条样式
        initProgressBar();
        
        // 检查必要的全局对象
        if (typeof amisRequire === 'undefined') {
            console.error('AMIS未正确加载，请检查页面资源');
            showError('页面资源加载失败，请刷新页面重试');
            return;
        }
        
        // 获取访问码
        accessCode = getSurveyAccessCode();
        console.log('获取到访问码:', accessCode);
        
        if (!accessCode) {
            console.error('无法获取访问码');
            showError('无效的访问码');
            return;
        }

        // 生成会话ID
        sessionId = generateSessionId();
        console.log('生成会话ID:', sessionId);

        // 绑定重新加载按钮事件
        const reloadBtn = document.querySelector('.survey-retry-btn');
        if (reloadBtn) {
            reloadBtn.addEventListener('click', reloadSurvey);
            console.log('重新加载按钮事件已绑定');
        }

        // 加载问卷
        console.log('开始加载问卷数据');
        loadSurvey();

        // 页面离开前提醒
        window.addEventListener('beforeunload', function(e) {
            // 如果正在提交或已经提交成功，不显示提醒
            if (isSubmitting) {
                return;
            }
            
            // 如果问卷已加载但未提交，显示提醒
            if (surveyData && amisInstance) {
                const message = '您的问卷尚未提交，确定要离开吗？';
                e.returnValue = message;
                return message;
            }
        });
        
        console.log('问卷参与页面初始化完成');
    }

    // 暴露全局方法
    window.SurveyParticipate = {
        init: init,
        reload: reloadSurvey,
        getSurveyData: function() { return surveyData; },
        getSessionId: function() { return sessionId; },
        // 调试方法
        debug: {
            getCurrentState: function() {
                return {
                    accessCode: accessCode,
                    sessionId: sessionId,
                    currentTenantId: currentTenantId,
                    surveyData: surveyData,
                    amisInstance: amisInstance,
                    startTime: startTime
                };
            },
            testRender: function(mockData) {
                console.log('测试渲染，使用模拟数据:', mockData);
                if (mockData) {
                    renderSurvey(mockData);
                }
            },
            hideProgressBar: function() {
                const progressBar = document.querySelector('.survey-progress-bar');
                if (progressBar) {
                    progressBar.classList.add('hidden');
                    console.log('进度条已隐藏');
                }
            },
            showProgressBar: function() {
                const progressBar = document.querySelector('.survey-progress-bar');
                if (progressBar) {
                    progressBar.classList.remove('hidden');
                    console.log('进度条已显示');
                }
            },
            updateProgress: function() {
                updateFormProgress();
            },
            testProgressUpdate: function() {
                console.group('[测试] 进度更新功能');
                
                if (!surveyData || !surveyData.questions) {
                    console.error('问卷数据未加载');
                    console.groupEnd();
                    return;
                }
                
                console.log('问卷题目数量:', surveyData.questions.length);
                
                // 测试模拟答题
                const firstQuestion = surveyData.questions[0];
                if (firstQuestion) {
                    console.log('模拟回答第一题:', firstQuestion.id);
                    saveQuestionAnswer(firstQuestion.id, 'test-answer');
                }
                
                // 检查AMIS实例状态
                if (amisInstance) {
                    console.log('AMIS实例存在');
                    try {
                        const formComponent = amisInstance.getComponentByName('surveyForm');
                        if (formComponent) {
                            console.log('表单组件存在');
                            console.log('表单数据:', formComponent.data);
                        } else {
                            console.warn('未找到表单组件');
                        }
                    } catch (e) {
                        console.error('获取表单组件失败:', e);
                    }
                } else {
                    console.error('AMIS实例不存在');
                }
                
                console.groupEnd();
            }
        }
    };

    // 暴露答题函数供AMIS调用
    window.saveQuestionAnswer = saveQuestionAnswer;

    // 等待DOM加载完成后初始化
    console.log('=== survey-participate.js 加载完成 ===');
    console.log('DOM状态:', document.readyState);
    console.log('window.surveyPageData:', window.surveyPageData);
    
    if (document.readyState === 'loading') {
        console.log('DOM仍在加载，添加事件监听器');
        document.addEventListener('DOMContentLoaded', function() {
            console.log('DOMContentLoaded事件触发，开始初始化');
            init();
        });
    } else {
        console.log('DOM已加载完成，立即初始化');
        init();
    }

})();
