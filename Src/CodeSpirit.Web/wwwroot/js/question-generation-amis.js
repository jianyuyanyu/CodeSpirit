/**
 * 题目生成页面脚本 - 基于AMIS框架
 * 实现AI题目生成功能，包括表单提交、实时进度展示和状态通知
 * 
 * 外部CSS样式文件: /css/question-generation-amis.css
 */
(function () {
    'use strict';

    // 初始化变量
    let connection = null;       // SignalR连接对象 
    let currentSessionId = null; // 当前生成会话ID
    let amisScoped = null;       // AMIS作用域对象
    let logs = [];               // 日志数组

    // 全局数据对象，用于存储生成状态信息
    window.globalData = {
        generation: {
            status: 'waiting',           // 生成状态：waiting, generating, completed, error
            sessionId: null,             // 会话ID
            progressStage: '准备中...',   // 当前阶段
            progressPercentage: 0,       // 进度百分比
            progressMessage: '正在初始化...', // 进度消息
            questionCount: 0,            // 生成的题目数量
            duration: 0,                 // 生成耗时(秒)
            errorMessage: '',            // 错误消息
            errorDetails: '',            // 错误详情
            completionMessage: '生成完成'  // 完成消息
        },
        logs: []                        // 日志数组
    };

    /**
     * 全局数据管理工具
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
         * @param {object} scope - amis作用域
         * @param {string[]} [selectedPaths] - 要同步的路径列表，不指定则同步全部数据
         */
        syncToAmis: function (scope, selectedPaths) {
            if (!scope || !scope.setValueByName) return;

            try {
                if (selectedPaths && Array.isArray(selectedPaths)) {
                    // 只同步指定路径的数据
                    for (const path of selectedPaths) {
                        const value = this.get(path);
                        scope.setValueByName(path, value);
                    }
                } else {
                    // 同步所有数据
                    for (const key in window.globalData.generation) {
                        scope.setValueByName(key, window.globalData.generation[key]);
                    }
                    // 同步日志
                    scope.setValueByName('generationLogs', window.globalData.logs);
                }
            } catch (error) {
                console.error('同步数据到AMIS失败:', error);
            }
        },

        /**
         * 更新单个字段并同步到AMIS
         * @param {string} path - 数据路径
         * @param {*} value - 要设置的值
         * @param {object} [scope] - amis作用域，不提供则使用全局保存的作用域
         */
        update: function (path, value, scope) {
            this.set(path, value);

            // 确保作用域对象正确，并且有setValueByName方法
            const effectiveScope = scope || window.amisScoped;
            if (effectiveScope && typeof effectiveScope.setValueByName === 'function') {
                if (path.startsWith('generation.')) {
                    // 转换路径从generation.xxx到xxx
                    const amisPath = path.substring('generation.'.length);
                    effectiveScope.setValueByName(amisPath, value);
                } else {
                    effectiveScope.setValueByName(path, value);
                }
            } else {
                console.log("未能更新AMIS作用域，setValueByName方法不可用");
            }
        }
    };

    // 添加认证请求适配器
    const requestAdaptor = function (api) {
        console.log("请求适配器被调用:", api);
        const token = localStorage.getItem('token');
        return {
            ...api,
            headers: {
                ...api.headers,
                'Authorization': 'Bearer ' + token,
                'X-Forwarded-With': 'CodeSpirit'
            }
        };
    };

    /**
     * 错误处理工具
     */
    window.ErrorHandler = {
        /**
         * 处理API错误
         * @param {string} title - 错误标题
         * @param {string} message - 错误消息
         */
        handleApiError: function (title, message) {
            console.error(`${title}: ${message}`);

            // 添加到日志
            addLog(`错误: ${message}`);

            // 更新全局状态
            GlobalData.set('generation.status', 'error');
            GlobalData.set('generation.errorMessage', title);
            GlobalData.set('generation.errorDetails', message);

            // 同步到AMIS作用域
            const updateMethod = window.amisUpdateMethod;
            if (updateMethod && typeof updateMethod === 'function') {
                updateMethod('generationStatus', 'error');
                updateMethod('errorMessage', title);
                updateMethod('errorDetails', message);
            } else {
                console.error("无法更新AMIS作用域，更新方法不可用");
                alert(`错误: ${title} - ${message}`);
            }

            // 停止SignalR连接
            if (connection) {
                try {
                    connection.stop();
                } catch (err) {
                    console.error("停止SignalR连接时出错:", err);
                }
            }
        },

        /**
         * 显示用户友好的错误提示
         * @param {string} message - 错误消息
         */
        showError: function (message) {
            alert(`错误: ${message}`);
        }
    };

    /**
     * 初始化SignalR连接
     * @param {string} sessionId 生成会话ID
     */
    function initializeSignalRConnection(sessionId) {
        if (connection) {
            connection.stop();
        }

        addLog("正在建立实时连接...");
        console.log("正在建立实时连接...", sessionId);

        // 创建连接
        connection = new signalR.HubConnectionBuilder()
            .withUrl("https://localhost:61882/api/exam/questionGenerationHub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // 存储连接实例以便后续管理
        window.signalRConnection = connection;

        // 注册事件处理程序
        registerSignalREvents();

        // 启动连接
        connection.start()
            .then(function () {
                addLog("实时连接已建立");
                console.log("实时连接已建立");
                // 加入生成组
                return connection.invoke("JoinGenerationGroup", sessionId);
            })
            .then(function () {
                addLog(`已加入生成组: ${sessionId}`);
                console.log(`已加入生成组: ${sessionId}`);
                executeGeneration(sessionId);
            })
            .catch(function (err) {
                console.error("连接错误:", err);
                ErrorHandler.handleApiError("连接错误", err.toString());
            });
    }

    /**
     * 更新生成状态
     * @param {string} status - 生成状态：waiting, generating, completed, error
     */
    function updateGenerationStatus(status) {
        console.log(`更新生成状态: ${status}`);

        // 更新全局状态
        GlobalData.set('generation.status', status);

        // 使用AMIS实例直接更新
        if (window.amisInstance && typeof window.amisInstance.updateProps === 'function') {
            try {
                window.amisInstance.updateProps({
                    data: {
                        generationStatus: status
                    }
                });
                console.log(`通过amisInstance.updateProps更新状态成功: ${status}`);
                return true;
            } catch (err) {
                console.error("通过AMIS实例更新状态时出错:", err);
            }
        }

        // 备用方法：使用全局更新方法
        const updateMethod = window.amisUpdateMethod;
        if (updateMethod && typeof updateMethod === 'function') {
            updateMethod('generationStatus', status);
            console.log(`通过updateMethod更新状态成功: ${status}`);
            return true;
        }

        console.warn("无法更新生成状态，更新方法不可用");
        return false;
    }

    /**
     * 注册SignalR事件处理程序
     */
    function registerSignalREvents() {
        if (!connection) {
            console.error("无法注册SignalR事件，连接尚未建立");
            return;
        }

        connection.on("GenerationStarted", (data) => {
            console.log("收到生成开始事件:", data);
            updateGenerationStatus("进行中");
            addLog("生成已开始");
        });

        connection.on("GenerationProgress", (data) => {
            console.log(`收到生成进度更新:`, data);
            // 从对象中提取信息
            const stage = data.stage || '';
            const message = data.message || '';
            const percentage = data.percentage || 0;
            updateProgress(stage, message, percentage);
        });

        connection.on("GenerationCompleted", (data) => {
            console.log(`收到生成完成事件:`, data);
            // 从对象中提取信息

            const generatedCount = data.generatedCount || data.questionCount || 0;
            const message = data.message || generatedCount;
            // 更新状态
            updateGenerationStatus("completed");
            // 更新进度
            updateProgress("已完成", message, 100);
            addLog(`生成完成: ${message}`);

            // 更新生成数量信息
            if (window.amisUpdateMethod) {
                window.amisUpdateMethod('questionCount', generatedCount);
                window.amisUpdateMethod('generationStatus', 'completed'); // 确保状态更新
            }

            // 自动获取生成的题目
            const sessionId = GlobalData.get('generation.sessionId');
            if (sessionId) {
                // 添加短暂延迟确保后端数据已就绪
                setTimeout(() => {
                    fetchGeneratedQuestions(sessionId);
                    // 显示结果区域
                    if (window.amisUpdateMethod) {
                        window.amisUpdateMethod('showResults', true);
                        // 强制更新UI
                        forceUpdateAmisComponent();
                        // 滚动到结果区域
                        setTimeout(() => {
                            const resultsElement = document.querySelector('.generated-questions-container');
                            if (resultsElement) {
                                resultsElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
                            }
                        }, 500);
                    }
                }, 800);
            } else {
                console.error("无法获取生成的题目，会话ID不存在");
                addLog("无法获取生成的题目，会话ID不存在");
            }
        });

        connection.on("GenerationError", (data) => {
            console.error(`收到生成错误事件:`, data);
            // 错误可能在data.error或message中，或者本身就是字符串
            let errorMessage = '未知错误';
            if (typeof data === 'string') {
                errorMessage = data;
            } else if (data && typeof data === 'object') {
                errorMessage = data.error || data.message || data.errorMessage || '未知错误';
            }

            updateGenerationStatus("失败");
            updateProgress("失败", errorMessage, 0);
            addLog(`生成失败: ${errorMessage}`);
            ErrorHandler.handleApiError("生成失败", errorMessage);
        });

        console.log("SignalR事件已注册");
    }

    /**
     * 获取生成的题目列表
     * @param {string} sessionId - 生成会话ID
     */
    function fetchGeneratedQuestions(sessionId) {
        // 确保有会话ID
        if (!sessionId) {
            console.warn('未提供会话ID，无法获取生成的题目');
            return;
        }

        // 更新加载状态
        if (window.amisUpdateMethod) {
            window.amisUpdateMethod({
                isFetchingQuestions: true,
                questionFetchMessage: '正在获取生成的题目...'
            });
        }

        // 获取认证令牌
        const token = getToken();

        // 构建请求头
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        };

        // 如果有令牌则添加到头部
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        // 发送请求获取题目 - 注意URL
        console.log(`正在获取生成的题目，会话ID: ${sessionId}`);

        // 使用与后端匹配的API路径
        fetch(`/exam/api/exam/Questions/generated/${sessionId}`, {
            method: 'GET',
            headers: headers
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`获取题目失败: ${response.status} ${response.statusText}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('获取到生成的题目:', data);

                // 确保数据格式正确 - 这里需要检查ApiResponse的结构
                if (!data || !Array.isArray(data.data)) {
                    console.warn('获取到的题目数据格式不正确:', data);
                    if (window.amisUpdateMethod) {
                        window.amisUpdateMethod({
                            isFetchingQuestions: false,
                            questionFetchMessage: '题目数据格式不正确',
                            generatedQuestions: []
                        });
                    }
                    return;
                }

                // 格式化题目数据 - 使用data作为题目数组
                const formattedQuestions = data.data.map((question, index) => {
                    // 为选项添加标签 (A, B, C, D...)
                    const options = Array.isArray(question.options) ?
                        question.options.map((option, optIndex) => ({
                            ...option,
                            label: String.fromCharCode(65 + optIndex) // A, B, C, D...
                        })) : [];

                    return {
                        ...question,
                        index: index + 1,
                        typeName: question.typeName || '未知类型',
                        difficultyName: question.difficultyName || '未知难度',
                        content: question.content || '无内容',
                        options: options
                    };
                });

                // 更新AMIS组件
                if (window.amisUpdateMethod) {
                    window.amisUpdateMethod({
                        isFetchingQuestions: false,
                        questionFetchMessage: '题目获取成功',
                        generatedQuestions: formattedQuestions,
                        showResults: true,               // 确保显示结果面板
                        generationStatus: 'completed'    // 确保状态正确
                    });
                    
                    // 强制更新
                    forceUpdateAmisComponent();
                    
                    // 确保状态类正确
                    checkGenerationStatusClass('completed');
                }

                // 滚动到题目容器
                setTimeout(() => {
                    const questionsContainer = document.querySelector('.generated-questions-container');
                    if (questionsContainer) {
                        questionsContainer.scrollIntoView({ behavior: 'smooth' });
                    }
                }, 500);
            })
            .catch(error => {
                console.error('获取题目时发生错误:', error);
                if (window.amisUpdateMethod) {
                    window.amisUpdateMethod({
                        isFetchingQuestions: false,
                        questionFetchMessage: `获取题目失败: ${error.message}`,
                        generatedQuestions: []
                    });
                }
            });
    }

    /**
     * 获取认证令牌
     * @returns {string} 认证令牌
     */
    function getToken() {
        // 从cookie或localStorage中获取令牌
        // 这里使用示例实现，需要根据实际认证方式调整
        return document.cookie.replace(/(?:(?:^|.*;\s*)token\s*\=\s*([^;]*).*$)|^.*$/, "$1") ||
            localStorage.getItem('token');
    }

    /**
     * 强制AMIS组件重新渲染
     * 参考exam.js中的方法，确保所有数据正确同步到AMIS
     */
    function forceUpdateAmisComponent() {
        setTimeout(() => {
            if (window.amisInstance && window.amisInstance.forceUpdate) {
                window.amisInstance.forceUpdate();
            }
        }, 200);
    }

    /**
     * 执行生成
     * @param {string} sessionId - 生成会话ID
     */
    function executeGeneration(sessionId) {
        addLog("触发后端生成过程...");

        // 获取AMIS表单数据
        const formData = window.globalData && window.globalData.formData ? window.globalData.formData : {};

        console.log(`正在执行生成，会话ID: ${sessionId}`);

        // 发送生成请求 - 确保URL正确匹配后端API
        fetch(`/exam/api/exam/Questions/ai/execute-generation/${sessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + localStorage.getItem('token'),
                'X-Forwarded-With': 'CodeSpirit'
            },
            body: JSON.stringify(formData)
        }).catch(err => {
            // 忽略错误，因为这是后台长时间运行的任务
            // 实际状态将通过SignalR通知
            console.log("后台生成任务已启动，状态将通过SignalR通知", err);
        });
    }

    /**
     * 执行生成过程 - 全局函数供AMIS调用
     * @param {Object} formData - 表单数据 
     * @param {Object} context - AMIS上下文对象
     */
    window.executeGenerationProcess = function (formData, context) {
        console.log("执行生成过程:", formData);
        console.log("AMIS上下文对象:", context);

        // 保存表单数据到全局对象，供后续请求使用
        GlobalData.set('formData', formData);
        window.globalData.formData = formData;

        // 检查AMIS作用域关键属性（避免循环引用问题）
        if (context && context.scoped) {
            console.log("AMIS scoped对象可用");
            console.log("scoped属性:", Object.keys(context.scoped));

            if (context.scoped.getComponents) {
                try {
                    const components = context.scoped.getComponents();
                    console.log("组件数量:", components ? components.length : 0);
                } catch (err) {
                    console.error("获取组件失败:", err);
                }
            }
        }

        // 获取更新方法
        let updateMethod = null;

        // 方法1: 直接使用AMIS的setState方法
        if (context && context.scoped && typeof context.scoped.setState === 'function') {
            console.log("找到scoped.setState方法");
            updateMethod = function (key, value) {
                const updateObj = {};
                updateObj[key] = value;
                context.scoped.setState(updateObj);
            };
        }
        // 方法2: 使用AMIS的setValueByName方法
        else if (context && context.scoped && typeof context.scoped.setValueByName === 'function') {
            console.log("找到scoped.setValueByName方法");
            updateMethod = context.scoped.setValueByName.bind(context.scoped);
        }
        // 方法3: 使用表单的setValues方法
        else if (context && context.scoped && context.scoped.getComponentByName && context.scoped.getComponentByName('form')) {
            console.log("找到表单组件");
            const form = context.scoped.getComponentByName('form');
            if (form && typeof form.setValues === 'function') {
                console.log("找到form.setValues方法");
                updateMethod = function (key, value) {
                    const values = {};
                    values[key] = value;
                    form.setValues(values);
                };
            }
        }
        // 方法4: 查找页面组件并使用其setState方法
        else if (context && context.scoped && context.scoped.getComponents) {
            try {
                const components = context.scoped.getComponents();
                console.log("查找组件中的setState方法，组件数:", components ? components.length : 0);
                if (components && components.length > 0) {
                    for (let i = 0; i < components.length; i++) {
                        const comp = components[i];
                        if (comp && typeof comp.setState === 'function') {
                            console.log("找到组件setState方法");
                            updateMethod = function (key, value) {
                                const updateObj = {};
                                updateObj[key] = value;
                                comp.setState(updateObj);
                            };
                            break;
                        }
                    }
                }
            } catch (err) {
                console.error("查找组件时出错:", err);
            }
        }

        // 方法5: 通过amisInstance直接更新
        if (!updateMethod && window.amisInstance && window.amisInstance.updateProps) {
            console.log("找到amisInstance.updateProps方法");
            updateMethod = function (key, value) {
                try {
                    const updateData = { data: {} };
                    updateData.data[key] = value;
                    window.amisInstance.updateProps(updateData);
                    console.log(`通过amisInstance更新: ${key} = ${JSON.stringify(value)}`);
                } catch (err) {
                    console.error("通过amisInstance更新失败:", err);
                }
            };
        }

        // 如果没有找到其他方法，使用一个模拟的方法并记录错误
        if (!updateMethod) {
            console.warn("无法找到有效的AMIS更新方法，将使用模拟方法");
            updateMethod = function (key, value) {
                console.log(`模拟更新: ${key} = ${JSON.stringify(value)}`);
            };
        }

        // 保存更新方法到全局变量
        window.amisUpdateMethod = updateMethod;

        // 更新状态为生成中
        GlobalData.set('generation.status', 'generating');
        updateMethod('generationStatus', 'generating');

        // 初始化进度
        updateMethod('progressPercentage', 0);
        updateMethod('progressStage', '准备中...');
        updateMethod('progressMessage', '正在初始化...');

        // 清空日志
        GlobalData.set('logs', []);
        updateMethod('generationLogs', []);

        // 直接调用fetch而不依赖AMIS的ajax
        fetch("/exam/api/exam/Questions/ai/generate-and-save", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": "Bearer " + localStorage.getItem("token"),
                "X-Forwarded-With": "CodeSpirit"
            },
            body: JSON.stringify({
                topic: formData.topic,
                count: formData.count,
                type: formData.type,
                difficulty: formData.difficulty,
                categoryId: formData.categoryId,
                requirements: formData.requirements
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log("生成请求响应:", data);

                // 处理嵌套的响应结构
                let responseData = data;
                if (data.result && data.result.value) {
                    responseData = data.result.value;
                    console.log("提取内部响应数据:", responseData);
                }

                if (responseData.status === 0 && responseData.data) {
                    const sessionId = responseData.data.sessionId;
                    currentSessionId = sessionId;

                    // 更新全局变量
                    GlobalData.set('generation.sessionId', sessionId);
                    console.log("获取到会话ID:", sessionId);

                    // 初始化SignalR连接
                    setTimeout(() => {
                        console.log("即将初始化SignalR连接...");
                        initializeSignalRConnection(sessionId);
                    }, 100);
                } else {
                    // 错误处理
                    console.error("请求返回失败状态:", responseData);
                    GlobalData.set('generation.status', 'error');
                    GlobalData.set('generation.errorMessage', "生成失败");
                    GlobalData.set('generation.errorDetails', responseData.msg || "未知错误");

                    updateMethod('generationStatus', 'error');
                    updateMethod('errorMessage', "生成失败");
                    updateMethod('errorDetails', responseData.msg || "未知错误");

                    // 记录日志
                    addLog(`错误: ${responseData.msg || "未知错误"}`);

                    // 停止连接
                    if (connection) {
                        connection.stop();
                    }
                }
            })
            .catch(error => {
                // 错误处理
                console.error("请求失败:", error);

                GlobalData.set('generation.status', 'error');
                GlobalData.set('generation.errorMessage', "请求失败");
                GlobalData.set('generation.errorDetails', error.toString());

                updateMethod('generationStatus', 'error');
                updateMethod('errorMessage', "请求失败");
                updateMethod('errorDetails', error.toString());

                // 记录日志
                addLog(`错误: ${error.toString()}`);

                // 停止连接
                if (connection) {
                    connection.stop();
                }
            });
    };

    /**
     * 更新进度显示
     * @param {string} stage - 当前阶段名称
     * @param {string} message - 进度消息
     * @param {number} percentage - 完成百分比
     */
    function updateProgress(stage, message, percentage) {
        console.log(`更新进度: ${stage}, ${message}, ${percentage}%`);

        // 确保百分比是有效数值
        if (isNaN(percentage)) {
            percentage = 0;
            console.warn("进度百分比无效，已设为0");
        }

        // 更新全局状态
        GlobalData.set('generation.progressStage', getStageName(stage));
        GlobalData.set('generation.progressMessage', message);
        GlobalData.set('generation.progressPercentage', percentage);

        // 确保生成状态为"生成中"
        if (GlobalData.get('generation.status') !== 'generating') {
            GlobalData.set('generation.status', 'generating');
        }

        // 使用AMIS实例直接更新（最可靠的方法）
        if (window.amisInstance && typeof window.amisInstance.updateProps === 'function') {
            try {
                // 直接更新所有进度相关属性
                window.amisInstance.updateProps({
                    data: {
                        generationStatus: 'generating',
                        progressStage: getStageName(stage),
                        progressMessage: message,
                        progressPercentage: percentage,
                        generationLogs: window.globalData.logs || []
                    }
                });

                // 尝试调用强制更新
                if (typeof window.amisInstance.forceUpdate === 'function') {
                    window.amisInstance.forceUpdate();
                }

                return true;
            } catch (err) {
                console.error("通过AMIS实例更新进度时出错:", err);
            }
        }

        // 备用方法：使用全局更新方法
        try {
            const updateMethod = window.amisUpdateMethod;
            if (updateMethod && typeof updateMethod === 'function') {
                // 确保更新UI状态为"生成中"
                updateMethod('generationStatus', 'generating');
                updateMethod('progressStage', getStageName(stage));
                updateMethod('progressMessage', message);
                updateMethod('progressPercentage', percentage);
                console.log("通过updateMethod更新进度成功");
                return true;
            } else {
                console.error("无法更新AMIS进度，更新方法不可用");

                // 尝试通过amisInstance更新（最后尝试）
                if (window.amisInstance && window.amisInstance.updateProps) {
                    const updateData = {
                        data: {
                            generationStatus: 'generating',
                            progressStage: getStageName(stage),
                            progressMessage: message,
                            progressPercentage: percentage
                        }
                    };
                    window.amisInstance.updateProps(updateData);
                    console.log("通过amisInstance.updateProps更新进度成功");
                }
            }
        } catch (err) {
            console.error("更新进度时出错:", err);

            // 直接尝试通过DOM更新，作为最后的备选方案
            updateProgressDOM(stage, message, percentage);
        }
    }

    /**
     * 使用DOM直接更新进度显示（备选方案）
     * @param {string} stage - 当前阶段名称
     * @param {string} message - 进度消息
     * @param {number} percentage - 完成百分比
     */
    function updateProgressDOM(stage, message, percentage) {
        try {
            console.log("尝试通过DOM直接更新进度...");

            // 更新进度条
            const progressBars = document.querySelectorAll('.progress .progress-bar');
            if (progressBars && progressBars.length > 0) {
                progressBars.forEach(bar => {
                    bar.style.width = `${percentage}%`;
                    bar.setAttribute('aria-valuenow', percentage);
                });
                console.log("更新了DOM进度条");
            }

            // 更新阶段文本
            const stageElements = document.querySelectorAll('[data-role="progress-stage"]');
            if (stageElements && stageElements.length > 0) {
                const stageName = getStageName(stage);
                stageElements.forEach(el => {
                    el.textContent = stageName;
                });
                console.log("更新了DOM阶段文本");
            }

            // 更新进度消息
            const messageElements = document.querySelectorAll('[data-role="progress-message"]');
            if (messageElements && messageElements.length > 0) {
                messageElements.forEach(el => {
                    el.textContent = message;
                });
                console.log("更新了DOM进度消息");
            }

            // 更新百分比显示
            const percentageElements = document.querySelectorAll('[data-role="progress-percentage"]');
            if (percentageElements && percentageElements.length > 0) {
                percentageElements.forEach(el => {
                    el.textContent = `${percentage}%`;
                });
                console.log("更新了DOM百分比显示");
            }

            // 确保生成中的容器显示
            const generatingContainer = document.querySelector('[data-status-container="generating"]');
            if (generatingContainer) {
                generatingContainer.style.display = 'block';
                console.log("显示了生成中容器");
            }

            // 隐藏其他状态容器
            const otherContainers = document.querySelectorAll('[data-status-container]:not([data-status-container="generating"])');
            if (otherContainers && otherContainers.length > 0) {
                otherContainers.forEach(container => {
                    container.style.display = 'none';
                });
                console.log("隐藏了其他状态容器");
            }

            return true;
        } catch (error) {
            console.error("通过DOM更新进度时出错:", error);
            return false;
        }
    }

    /**
     * 添加日志条目
     * @param {string} message - 日志消息
     */
    function addLog(message) {
        try {
            const timestamp = new Date().toLocaleTimeString();
            const logItem = `[${timestamp}] ${message}`;
            console.log("生成日志:", logItem);

            // 添加到开头
            if (!window.globalData.logs) {
                window.globalData.logs = [];
            }
            window.globalData.logs.unshift(logItem);

            // 限制日志条数，防止过多
            if (window.globalData.logs.length > 100) {
                window.globalData.logs = window.globalData.logs.slice(0, 100);
            }

            // 更新AMIS中的日志数组
            const updateMethod = window.amisUpdateMethod;
            if (updateMethod && typeof updateMethod === 'function') {
                updateMethod('generationLogs', window.globalData.logs);
            } else {
                console.warn("无法更新AMIS日志，更新方法不可用");

                // 尝试通过amisInstance更新
                if (window.amisInstance && window.amisInstance.updateProps) {
                    const updateData = {
                        data: {
                            generationLogs: window.globalData.logs
                        }
                    };
                    window.amisInstance.updateProps(updateData);
                }
            }

            // 强制刷新AMIS页面
            if (window.amisInstance && window.amisInstance.forceUpdate) {
                try {
                    window.amisInstance.forceUpdate();
                } catch (err) {
                    console.warn("强制刷新AMIS页面失败:", err);
                }
            }
        } catch (err) {
            console.error("添加日志时出错:", err);
        }
    }

    /**
     * 重置表单和状态
     */
    function resetForm() {
        // 更新全局状态
        GlobalData.set('generation.status', 'waiting');
        window.globalData.logs = [];

        // 使用全局更新方法
        const updateMethod = window.amisUpdateMethod;
        if (updateMethod && typeof updateMethod === 'function') {
            updateMethod('generationStatus', 'waiting');
            updateMethod('generationLogs', []);
            updateMethod('progressPercentage', 0);
            updateMethod('progressStage', '准备中...');
            updateMethod('progressMessage', '正在初始化...');
            updateMethod('errorMessage', '');
            updateMethod('errorDetails', '');
            updateMethod('generatedQuestions', []); // 清空题目列表
            updateMethod('showResults', false);    // 隐藏结果面板
        } else {
            console.warn("无法重置AMIS表单，更新方法不可用");

            // 尝试更新amisInstance
            if (window.amisInstance && window.amisInstance.updateProps) {
                const updateData = {
                    data: {
                        generationStatus: 'waiting',
                        generationLogs: [],
                        progressPercentage: 0,
                        progressStage: '准备中...',
                        progressMessage: '正在初始化...',
                        errorMessage: '',
                        errorDetails: '',
                        generatedQuestions: [],
                        showResults: false
                    }
                };
                window.amisInstance.updateProps(updateData);
            }
        }

        if (connection) {
            connection.stop();
        }

        currentSessionId = null;
        
        // 强制刷新界面
        forceUpdateAmisComponent();
        
        // 更新状态类
        checkGenerationStatusClass('waiting');
    }

    /**
     * 获取阶段显示名称
     * @param {string} stage - 阶段标识
     * @returns {string} 阶段显示名称
     */
    function getStageName(stage) {
        const stageNames = {
            'preparing': '准备阶段',
            'generating': '生成阶段',
            'saving': '保存阶段'
        };
        return stageNames[stage] || stage;
    }

    /**
     * 获取题目类型显示名称
     * @param {number} type - 题目类型ID
     * @returns {string} 题目类型名称
     */
    function getQuestionTypeName(type) {
        const typeMap = {
            0: '单选题',
            1: '多选题',
            2: '判断题',
            3: '填空题',
            4: '简答题',
            5: '编程题'
        };
        return typeMap[type] || '未知类型';
    }

    /**
     * 获取题目难度名称
     * @param {number} difficulty - 难度ID
     * @returns {string} 难度名称
     */
    function getQuestionDifficultyName(difficulty) {
        const difficultyMap = {
            0: '简单',
            1: '中等',
            2: '困难'
        };
        return difficultyMap[difficulty] || '未知难度';
    }

    // AMIS页面配置
    const amisJSON = {
        type: "page",
        title: "AI题目生成",
        className: "question-generation-page p-3",
        body: [
            {
                type: "grid",
                className: "mb-4",
                columns: [
                    {
                        md: 6,
                        body: {
                            type: "form",
                            title: "",
                            mode: "horizontal",
                            horizontal: {
                                left: 3,
                                right: 9
                            },
                            className: "form-card shadow-sm border-0 rounded",
                            wrapWithPanel: true,
                            panelClassName: "border-0 shadow-sm rounded",
                            actionsClassName: "border-top mt-3 pt-3",
                            actions: [
                                {
                                    type: "button",
                                    label: "开始生成",
                                    level: "primary",
                                    size: "lg",
                                    className: "question-generation-button shadow-sm",
                                    iconClassName: "fas fa-magic me-1",
                                    onEvent: {
                                        click: {
                                            actions: [
                                                {
                                                    actionType: "custom",
                                                    script: "window.executeGenerationProcess(event.data, event.context);"
                                                }
                                            ]
                                        }
                                    },
                                    disabledOn: "generation.generationStatus === 'generating'"
                                }
                            ],
                            body: [
                                {
                                    type: "input-text",
                                    name: "topic",
                                    label: "主题",
                                    required: true,
                                    maxLength: 100,
                                    value: "C#编程基础",
                                    placeholder: "请输入题目主题或知识领域",
                                    description: "请输入题目主题或知识领域",
                                    disabledOn: "data.generationStatus === 'generating'",
                                    clearable: true,
                                    prefixIcon: "fa fa-book"
                                },
                                {
                                    type: "input-number",
                                    name: "count",
                                    label: "题目数量",
                                    value: 10,
                                    min: 1,
                                    max: 10,
                                    step: 1,
                                    required: true,
                                    description: "范围为1-10题",
                                    disabledOn: "data.generationStatus === 'generating'",
                                    displayMode: "enhance"
                                },
                                {
                                    type: "select",
                                    name: "type",
                                    label: "题目类型",
                                    value: 1,
                                    options: [
                                        { label: "单选题", value: 1 },
                                        { label: "多选题", value: 2 },
                                        { label: "判断题", value: 3 }
                                    ],
                                    required: true,
                                    disabledOn: "data.generationStatus === 'generating'",
                                    searchable: true,
                                    clearable: false
                                },
                                {
                                    type: "select",
                                    name: "difficulty",
                                    label: "难度",
                                    value: 2,
                                    options: [
                                        { label: "简单", value: 1, badge: "success" },
                                        { label: "中等", value: 2, badge: "warning" },
                                        { label: "困难", value: 3, badge: "danger" }
                                    ],
                                    required: true,
                                    disabledOn: "data.generationStatus === 'generating'",
                                    searchable: false,
                                    clearable: false
                                },
                                {
                                    type: "tree-select",
                                    name: "categoryId",
                                    label: "分类",
                                    source: "/exam/api/exam/QuestionCategories/tree",
                                    multiple: false,
                                    required: true,
                                    cascade: true,
                                    showOutline: true,
                                    labelField: "name",
                                    valueField: "id",
                                    disabledOn: "data.generationStatus === 'generating'",
                                    searchable: true
                                },
                                {
                                    type: "textarea",
                                    name: "requirements",
                                    label: "生成要求",
                                    maxLength: 500,
                                    showCounter: true,
                                    placeholder: "请输入对生成题目的特定要求，例如：围绕某个特定概念、包含具体知识点等",
                                    disabledOn: "data.generationStatus === 'generating'",
                                    minRows: 3,
                                    maxRows: 6
                                },
                                {
                                    type: "hidden",
                                    name: "generationStatus",
                                    value: "waiting"
                                },
                                {
                                    type: "hidden",
                                    name: "progressPercentage",
                                    value: 0
                                },
                                {
                                    type: "hidden",
                                    name: "progressStage",
                                    value: "准备中..."
                                },
                                {
                                    type: "hidden",
                                    name: "progressMessage",
                                    value: "正在初始化..."
                                },
                                {
                                    type: "hidden",
                                    name: "questionCount",
                                    value: 0
                                },
                                {
                                    type: "hidden",
                                    name: "duration",
                                    value: 0
                                },
                                {
                                    type: "hidden",
                                    name: "errorMessage",
                                    value: ""
                                },
                                {
                                    type: "hidden",
                                    name: "errorDetails",
                                    value: ""
                                },
                                {
                                    type: "hidden",
                                    name: "completionMessage",
                                    value: "生成完成"
                                },
                                {
                                    type: "hidden",
                                    name: "generationLogs",
                                    value: []
                                },
                                {
                                    type: "hidden",
                                    name: "generatedQuestions",
                                    value: []
                                },
                                {
                                    type: "hidden",
                                    name: "showResults",
                                    value: false
                                }
                            ]
                        }
                    },
                    {
                        md: 6,
                        body: {
                            type: "service",
                            className: "h-100",
                            initFetch: false,
                            schemaApi: "",
                            body: [
                                {
                                    type: "card",
                                    className: "h-100 shadow-sm border-0",
                                    header: {
                                        title: "生成进度",
                                        className: "border-bottom bg-light",
                                        subTitle: ""
                                    },
                                    headerClassName: "bg-light border-bottom",
                                    bodyClassName: "p-3",
                                    body: [
                                        // 待生成状态
                                        {
                                            type: "tpl",
                                            tpl: "<div class='waiting-container text-center py-5 fade-in'><i class='fas fa-robot waiting-icon'></i><p class='mt-3 text-secondary fs-5'>请填写表单开始生成题目</p><p class='text-muted'>AI将根据您的需求自动生成高质量的考试题目</p></div>",
                                            visibleOn: "data.generationStatus === 'waiting' || !data.generationStatus",
                                            data: {
                                                status: "waiting"
                                            }
                                        },

                                        // 生成中状态
                                        {
                                            type: "container",
                                            className: "fade-in",
                                            visibleOn: "data.generationStatus === 'generating'",
                                            data: {
                                                status: "generating"
                                            },
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='text-center text-primary my-3'><i class='fa fa-cog fa-spin me-2'></i><span class='fs-5 fw-medium'>正在生成题目...</span></div>",
                                                    className: "mb-3"
                                                },
                                                {
                                                    type: "grid",
                                                    className: "mb-2",
                                                    columns: [
                                                        {
                                                            md: 6,
                                                            body: {
                                                                type: "tpl",
                                                                tpl: "<div class='progress-stage fw-medium'>${progressStage || '准备中...'}</div>",
                                                                className: "font-weight-bold",
                                                                data: {
                                                                    role: "progress-stage"
                                                                }
                                                            }
                                                        },
                                                        {
                                                            md: 6,
                                                            body: {
                                                                type: "tpl",
                                                                className: "text-right text-end",
                                                                tpl: "<div class='progress-percentage fw-bold'>${progressPercentage || 0}%</div>",
                                                                data: {
                                                                    role: "progress-percentage"
                                                                }
                                                            }
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: "progress",
                                                    mode: "line",
                                                    value: "${progressPercentage || 0}",
                                                    strokeWidth: 8,
                                                    showLabel: false,
                                                    animate: true,
                                                    className: "generation-progress mb-3"
                                                },
                                                {
                                                    type: "alert",
                                                    level: "info",
                                                    body: "${progressMessage || '正在初始化...'}",
                                                    showIcon: true,
                                                    className: "mt-2 shadow-sm",
                                                    data: {
                                                        role: "progress-message"
                                                    }
                                                },
                                                {
                                                    type: "panel",
                                                    title: "生成日志",
                                                    titleClassName: "fs-6",
                                                    headerClassName: "bg-dark text-light py-2 px-3 rounded-top",
                                                    bodyClassName: "bg-dark text-light p-2 rounded-bottom",
                                                    className: "mt-4 shadow",
                                                    body: {
                                                        type: "each",
                                                        name: "generationLogs",
                                                        items: {
                                                            type: "tpl",
                                                            tpl: "<div>${item || ''}</div>"
                                                        },
                                                        placeholder: "<div class='text-muted p-2'>暂无日志记录</div>"
                                                    },
                                                    affixFooter: false,
                                                    style: {
                                                        height: "200px",
                                                        overflow: "auto",
                                                        fontFamily: "monospace",
                                                        fontSize: "13px"
                                                    }
                                                }
                                            ]
                                        },

                                        // 生成完成状态
                                        {
                                            type: "container",
                                            className: "fade-in",
                                            visibleOn: "data.generationStatus === 'completed' || data.showResults === true",
                                            data: {
                                                status: "completed"
                                            },
                                            body: [
                                                {
                                                    type: "alert",
                                                    level: "success",
                                                    showIcon: true,
                                                    body: "${completionMessage}",
                                                    className: "shadow-sm"
                                                },
                                                {
                                                    type: "card",
                                                    className: "mt-3 border-0 shadow-sm",
                                                    headerClassName: "bg-light",
                                                    header: {
                                                        title: "生成结果统计",
                                                        className: "fs-6"
                                                    },
                                                    body: [
                                                        {
                                                            type: "grid",
                                                            columns: [
                                                                {
                                                                    md: 6,
                                                                    body: {
                                                                        type: "card",
                                                                        className: "border-0 bg-light text-center py-2",
                                                                        body: [
                                                                            {
                                                                                type: "tpl",
                                                                                tpl: "<div class='fs-1 text-primary fw-bold'>${questionCount}</div><div class='text-muted'>题目数量</div>"
                                                                            }
                                                                        ]
                                                                    }
                                                                },
                                                                {
                                                                    md: 6,
                                                                    body: {
                                                                        type: "card",
                                                                        className: "border-0 bg-light text-center py-2",
                                                                        body: [
                                                                            {
                                                                                type: "tpl",
                                                                                tpl: "<div class='fs-1 text-primary fw-bold'>${duration}</div><div class='text-muted'>耗时(秒)</div>"
                                                                            }
                                                                        ]
                                                                    }
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: "panel",
                                                    title: "生成的题目",
                                                    titleClassName: "fs-6",
                                                    className: "generated-questions-container mt-4 shadow-sm",
                                                    headerClassName: "bg-primary text-white py-2",
                                                    body: [
                                                        {
                                                            type: 'table',
                                                            source: '${generatedQuestions}',
                                                            className: 'table-striped table-hover generated-questions-table',
                                                            columnsTogglable: false,
                                                            columns: [
                                                                {
                                                                    name: 'index',
                                                                    label: '序号',
                                                                    type: 'tpl',
                                                                    tpl: '<span class="badge bg-light text-dark">${index}</span>',
                                                                    width: 60
                                                                },
                                                                {
                                                                    name: 'content',
                                                                    label: '题目内容',
                                                                    type: 'tpl',
                                                                    tpl: '<div class="text-truncate" style="max-width: 300px;" title="${content}">${content}</div>',
                                                                    width: 300
                                                                },
                                                                {
                                                                    name: 'type',
                                                                    label: '题型',
                                                                    type: 'mapping',
                                                                    map: {
                                                                        '1': '<span class="status-badge status-badge-success">单选题</span>',
                                                                        '2': '<span class="status-badge status-badge-processing">多选题</span>',
                                                                        '3': '<span class="status-badge">判断题</span>',
                                                                        '*': '<span class="status-badge">其他</span>'
                                                                    },
                                                                    width: 80
                                                                },
                                                                {
                                                                    name: 'difficulty',
                                                                    label: '难度',
                                                                    type: 'mapping',
                                                                    map: {
                                                                        '1': '<span class="text-success"><i class="fa fa-circle me-1"></i>简单</span>',
                                                                        '2': '<span class="text-warning"><i class="fa fa-circle me-1"></i>中等</span>',
                                                                        '3': '<span class="text-danger"><i class="fa fa-circle me-1"></i>困难</span>',
                                                                        '*': '<span>未知</span>'
                                                                    },
                                                                    width: 80
                                                                },
                                                                {
                                                                    type: 'operation',
                                                                    label: '操作',
                                                                    buttons: [
                                                                        {
                                                                            type: 'button',
                                                                            label: '查看',
                                                                            actionType: 'dialog',
                                                                            level: 'link',
                                                                            icon: 'fa fa-eye',
                                                                            dialog: {
                                                                                title: '题目详情',
                                                                                size: 'lg',
                                                                                body: [
                                                                                    {
                                                                                        type: 'card',
                                                                                        className: 'question-card mb-3 border-0 shadow-sm',
                                                                                        header: {
                                                                                            title: '基本信息',
                                                                                            className: 'fs-6'
                                                                                        },
                                                                                        headerClassName: 'bg-light',
                                                                                        body: [
                                                                                            {
                                                                                                type: 'grid',
                                                                                                columns: [
                                                                                                    {
                                                                                                        md: 4,
                                                                                                        body: {
                                                                                                            type: 'tpl',
                                                                                                            tpl: '<div class="mb-2"><span class="text-muted me-2">题目ID:</span><span class="badge bg-light text-dark">${id}</span></div>'
                                                                                                        }
                                                                                                    },
                                                                                                    {
                                                                                                        md: 4,
                                                                                                        body: {
                                                                                                            type: 'tpl',
                                                                                                            tpl: '<div class="mb-2"><span class="text-muted me-2">题型:</span>${type == 1 ? "<span class=\'status-badge status-badge-success\'>单选题</span>" : type == 2 ? "<span class=\'status-badge status-badge-processing\'>多选题</span>" : "<span class=\'status-badge\'>判断题</span>"}</div>'
                                                                                                        }
                                                                                                    },
                                                                                                    {
                                                                                                        md: 4,
                                                                                                        body: {
                                                                                                            type: 'tpl',
                                                                                                            tpl: '<div class="mb-2"><span class="text-muted me-2">难度:</span>${difficulty == 1 ? "<span class=\'text-success\'><i class=\'fa fa-circle me-1\'></i>简单</span>" : difficulty == 2 ? "<span class=\'text-warning\'><i class=\'fa fa-circle me-1\'></i>中等</span>" : "<span class=\'text-danger\'><i class=\'fa fa-circle me-1\'></i>困难</span>"}</div>'
                                                                                                        }
                                                                                                    }
                                                                                                ]
                                                                                            }
                                                                                        ]
                                                                                    },
                                                                                    {
                                                                                        type: 'card',
                                                                                        className: 'question-card mb-3 border-0 shadow-sm',
                                                                                        header: {
                                                                                            title: '题目内容',
                                                                                            className: 'fs-6'
                                                                                        },
                                                                                        headerClassName: 'bg-light',
                                                                                        body: {
                                                                                            type: 'markdown',
                                                                                            value: '${content}'
                                                                                        }
                                                                                    },
                                                                                    {
                                                                                        type: 'card',
                                                                                        className: 'question-card mb-3 border-0 shadow-sm',
                                                                                        header: {
                                                                                            title: '选项',
                                                                                            className: 'fs-6'
                                                                                        },
                                                                                        headerClassName: 'bg-light',
                                                                                        visibleOn: 'this.type == 1 || this.type == 2',
                                                                                        body: [
                                                                                            {
                                                                                                type: 'each',
                                                                                                name: 'options',
                                                                                                items: {
                                                                                                    type: 'card',
                                                                                                    className: 'mb-2 border-0 bg-light',
                                                                                                    body: {
                                                                                                        type: 'tpl',
                                                                                                        tpl: '<div class="p-2 ${isCorrect ? "bg-success bg-opacity-10 border-start border-3 border-success" : ""}"><span class="badge ${isCorrect ? "bg-success" : "bg-secondary"} me-2">${label}</span> ${content}</div>'
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        ]
                                                                                    },
                                                                                    {
                                                                                        type: 'card',
                                                                                        className: 'question-card border-0 shadow-sm',
                                                                                        header: {
                                                                                            title: '答案与解析',
                                                                                            className: 'fs-6'
                                                                                        },
                                                                                        headerClassName: 'bg-light',
                                                                                        body: [
                                                                                            {
                                                                                                type: 'alert',
                                                                                                className: 'mb-3',
                                                                                                level: 'success',
                                                                                                icon: 'fa fa-check-circle',
                                                                                                body: '${correctAnswer}'
                                                                                            },
                                                                                            {
                                                                                                type: 'markdown',
                                                                                                value: '#### 解析\n\n${analysis}'
                                                                                            }
                                                                                        ]
                                                                                    }
                                                                                ]
                                                                            }
                                                                        }
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: "button-group",
                                                    className: "mt-4",
                                                    buttons: [
                                                        {
                                                            type: "button",
                                                            level: "primary",
                                                            label: "查看题库",
                                                            actionType: "link",
                                                            link: "/exam/questions",
                                                            className: "me-2 shadow-sm"
                                                        },
                                                        {
                                                            type: "button",
                                                            level: "default",
                                                            label: "再次生成",
                                                            className: "shadow-sm",
                                                            onEvent: {
                                                                click: {
                                                                    actions: [
                                                                        {
                                                                            actionType: "custom",
                                                                            script: "resetForm();"
                                                                        }
                                                                    ]
                                                                }
                                                            }
                                                        }
                                                    ]
                                                }
                                            ]
                                        },

                                        // 错误状态
                                        {
                                            type: "container",
                                            className: "fade-in",
                                            visibleOn: "data.generationStatus === 'error'",
                                            data: {
                                                status: "error"
                                            },
                                            body: [
                                                {
                                                    type: "alert",
                                                    level: "danger",
                                                    showIcon: true,
                                                    body: "${errorMessage}",
                                                    className: "shadow-sm"
                                                },
                                                {
                                                    type: "card",
                                                    className: "mt-3 border-0 shadow-sm",
                                                    header: {
                                                        title: "错误详情",
                                                        className: "fs-6"
                                                    },
                                                    headerClassName: "bg-danger text-white",
                                                    body: "${errorDetails}",
                                                    bodyClassName: "text-danger",
                                                    style: {
                                                        maxHeight: "150px",
                                                        overflow: "auto"
                                                    }
                                                },
                                                {
                                                    type: "button",
                                                    level: "primary",
                                                    label: "重试",
                                                    className: "mt-4 shadow-sm",
                                                    onEvent: {
                                                        click: {
                                                            actions: [
                                                                {
                                                                    actionType: "custom",
                                                                    script: "resetForm();"
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
                    }
                ]
            }
        ]
    };

    // 初始化AMIS应用
    window.addEventListener('load', function () {
        console.log("页面加载完成，初始化AMIS应用");
        
        const amisApp = amisRequire('amis/embed');

        // 保存全局作用域
        window.amisScoped = null;
        window.amisInstance = null;

        // 初始默认数据
        const initialData = {
            generationStatus: 'waiting',
            progressPercentage: 0,
            progressStage: '准备中...',
            progressMessage: '正在初始化...',
            questionCount: 0,
            duration: 0,
            errorMessage: '',
            errorDetails: '',
            completionMessage: '生成完成',
            generationLogs: [],
            generatedQuestions: [], // 添加生成的题目数组
            showResults: false     // 添加结果显示标志
        };

        // 设置到全局数据
        for (const key in initialData) {
            GlobalData.set(`generation.${key}`, initialData[key]);
        }
        window.globalData.logs = [];

        // 配置AMIS应用
        let app = amisApp.embed('#question-generation-app', amisJSON, {
            locale: 'zh-CN',
            theme: 'antd',
            data: initialData
        }, {
            requestAdaptor: requestAdaptor,
            // 添加AMIS错误处理
            responseAdaptor: function (api, payload, query, request, response) {
                // 处理错误响应
                if (response.status >= 400) {
                    ErrorHandler.showError(`请求失败: ${response.status} ${response.statusText}`);
                    return payload;
                }

                return payload;
            },
            onAction: function () {
                console.log("AMIS动作触发:", arguments);
            },
            // 添加值变更事件
            watchData: function (data) {
                console.log("AMIS数据变更:", data);
                if (data && data.generationStatus) {
                    // 更新全局数据保持同步
                    GlobalData.set('generation.status', data.generationStatus);
                    console.log("页面状态变更:", data.generationStatus);

                    // 检查CSS类
                    //checkGenerationStatusClass(data.generationStatus);
                }
            },
            scopeRef: function (scoped) {
                console.log("AMIS作用域更新");
                window.amisScoped = scoped;

                // 保存全局更新方法
                if (scoped && typeof scoped.setValueByName === 'function') {
                    window.amisUpdateMethod = scoped.setValueByName.bind(scoped);
                } else if (scoped && typeof scoped.setState === 'function') {
                    window.amisUpdateMethod = function (key, value) {
                        const updateObj = {};
                        updateObj[key] = value;
                        scoped.setState(updateObj);
                    };
                }

                // 初始同步全局数据到AMIS
                GlobalData.syncToAmis(scoped);
            }
        });

        // 保存应用实例到全局变量
        window.amisInstance = app;

        // 暴露重置函数到全局
        window.resetForm = resetForm;

        // 添加DOM辅助函数，确保视图状态正确
        window.addDomEventHandlers();

        // 立即设置一次状态类，确保CSS正确
        checkGenerationStatusClass('waiting');

        // 暴露更多调试方法
        window.debugAmis = {
            updateProgress: updateProgress,
            addLog: addLog,
            simulateProgress: function (stage, message, percentage) {
                updateProgress(stage, message, percentage);
            },
            simulateCompleted: function (questionCount, duration) {
                const durationInSeconds = duration || 30;
                const count = questionCount || 10;

                // 更新全局状态
                GlobalData.set('generation.status', 'completed');
                GlobalData.set('generation.questionCount', count);
                GlobalData.set('generation.duration', durationInSeconds);
                GlobalData.set('generation.completionMessage', `成功生成 ${count} 道题目！`);

                // 使用全局更新方法
                const updateMethod = window.amisUpdateMethod;
                if (updateMethod && typeof updateMethod === 'function') {
                    updateMethod('generationStatus', 'completed');
                    updateMethod('questionCount', count);
                    updateMethod('duration', durationInSeconds);
                    updateMethod('completionMessage', `成功生成 ${count} 道题目！`);
                }

                addLog(`生成完成！共生成 ${count} 道题目，耗时 ${durationInSeconds} 秒`);
            },
            simulateError: function (title, message) {
                ErrorHandler.handleApiError(title || "生成错误", message || "模拟的错误消息");
            },
            forceUpdate: function () {
                if (window.amisInstance && window.amisInstance.forceUpdate) {
                    window.amisInstance.forceUpdate();
                }
            },
            setStatus: function (status) {
                const validStatus = ['waiting', 'generating', 'completed', 'error'];
                if (validStatus.includes(status)) {
                    // 更新全局状态
                    GlobalData.set('generation.status', status);

                    // 使用全局更新方法
                    const updateMethod = window.amisUpdateMethod;
                    if (updateMethod && typeof updateMethod === 'function') {
                        updateMethod('generationStatus', status);
                    }

                    // 如果是生成中状态，初始化进度数据
                    if (status === 'generating') {
                        updateProgress('preparing', '正在准备生成...', 10);
                    }

                    console.log(`状态已更新为: ${status}`);
                    return true;
                } else {
                    console.error(`无效的状态: ${status}，有效值为: ${validStatus.join(', ')}`);
                    return false;
                }
            },
            refreshUI: function () {
                // 强制刷新AMIS实例
                if (window.amisInstance) {
                    if (window.amisInstance.forceUpdate) {
                        window.amisInstance.forceUpdate();
                    }

                    // 尝试通过更新props触发重新渲染
                    if (window.amisInstance.updateProps) {
                        const currentData = {};
                        for (const key in initialData) {
                            currentData[key] = GlobalData.get(`generation.${key}`, initialData[key]);
                        }
                        window.amisInstance.updateProps({ data: currentData });
                    }

                    return true;
                }
                return false;
            },
            dumpState: function () {
                return {
                    globalData: window.globalData,
                    amisScoped: !!window.amisScoped,
                    amisInstance: !!window.amisInstance,
                    amisUpdateMethod: !!window.amisUpdateMethod,
                    connection: !!connection,
                    currentSessionId: currentSessionId
                };
            }
        };

        console.log("AMIS应用初始化完成", app);
        console.log("调试方法已可用: window.debugAmis");
    });

    /**
     * 添加DOM事件处理程序
     */
    window.addDomEventHandlers = function () {
         try {
             // 添加轮询检查器，确保状态和UI一致
             setInterval(function() {
                 const status = GlobalData.get('generation.status', 'waiting');
                 checkGenerationStatusClass(status);
             }, 1000);

             console.log("DOM事件处理器已添加");
         } catch (error) {
             console.error("添加DOM事件处理器时出错:", error);
         }
    };

    /**
     * 检查并设置生成状态CSS类
     * @param {string} status - 当前状态
     */
    function checkGenerationStatusClass(status) {
        try {
            const root = document.getElementById('question-generation-app');
            if (root) {
                // 将"已完成"转换为"completed"以保持统一
                if (status === '已完成') {
                    status = 'completed';
                }
                
                // 移除所有状态类
                root.classList.remove('status-waiting', 'status-generating', 'status-completed', 'status-error');

                // 添加当前状态类
                root.classList.add(`status-${status}`);
                console.log(`设置状态类: status-${status}`);

                // 手动检查容器可见性
                const containers = document.querySelectorAll('[data-status]');
                if (containers && containers.length > 0) {
                    containers.forEach(container => {
                        const containerStatus = container.getAttribute('data-status');
                        if (containerStatus === status) {
                            container.style.display = 'block';
                        } else {
                            container.style.display = 'none';
                        }
                    });
                    console.log("已手动更新状态容器可见性");
                }

                return true;
            }
            return false;
        } catch (error) {
            console.error("检查状态类时出错:", error);
            return false;
        }
    }

    // 停止SignalR连接
    function stopSignalRConnection() {
        if (window.signalRConnection || connection) {
            console.log('正在关闭SignalR连接...');
            const conn = window.signalRConnection || connection;
            conn.stop()
                .then(() => {
                    console.log('SignalR连接已关闭');
                    window.signalRConnection = null;
                    if (window.amisUpdateMethod) {
                        window.amisUpdateMethod({
                            signalRConnected: false
                        });
                    }
                })
                .catch(err => {
                    console.error('关闭SignalR连接时发生错误:', err);
                });
        }
    }
})(); 