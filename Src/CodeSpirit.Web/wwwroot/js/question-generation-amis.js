/**
 * 题目生成页面脚本 - 基于AMIS框架
 * 实现AI题目生成功能，包括表单提交、实时进度展示和状态通知
 */
(function () {
    'use strict';

    // 初始化变量
    let connection = null;       // SignalR连接对象 
    let currentSessionId = null; // 当前生成会话ID
    let amisInstance = null;     // AMIS实例对象

    // 数据管理器 - 统一管理数据更新和传递，应用AMIS数据域和数据链概念
    const DataManager = {
        // 基础数据结构
        initialData: {
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
            logs: [],                        // 日志数组
            generatedQuestions: [],          // 生成的题目
            showResults: false,              // 显示结果标志
            formData: {}                     // 表单数据
        },

        /**
         * 获取数据 - 支持深层路径访问
         * @param {string} path - 数据路径，格式为"a.b.c"
         * @param {*} defaultValue - 数据不存在时的默认值
         * @returns {*} 获取的数据或默认值
         */
        get(path, defaultValue) {
            if (!path) return defaultValue;

            const keys = path.split('.');
            let current = this.initialData;

            for (const key of keys) {
                if (current === undefined || current === null) {
                    return defaultValue;
                }
                current = current[key];
            }

            return current !== undefined ? current : defaultValue;
        },

        /**
         * 设置数据 - 支持深层路径设置
         * @param {string} path - 数据路径，格式为"a.b.c"
         * @param {*} value - 要设置的值
         * @returns {*} 设置的值
         */
        set(path, value) {
            if (!path) return value;

            const keys = path.split('.');
            let current = this.initialData;

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
         * 更新AMIS组件数据并触发更新
         * @param {object|string} keyOrData - 要更新的键或数据对象
         * @param {*} [value] - 如果keyOrData是字符串，则为要设置的值
         */
        updateAmis(keyOrData, value) {
            if (!amisInstance) {
                console.warn('AMIS实例不可用，无法更新数据');
                return;
            }

            try {
                const updateData = { data: {} };
                
                // 如果是对象，则更新多个键
                if (typeof keyOrData === 'object' && keyOrData !== null) {
                    Object.keys(keyOrData).forEach(key => {
                        // 设置到内部数据
                        if (key.includes('.')) {
                            this.set(key, keyOrData[key]);
                            // 为AMIS准备扁平化的数据 - 同时处理嵌套路径
                            const finalKey = key.split('.').pop();
                            updateData.data[finalKey] = keyOrData[key];
                        } else {
                            // 检查是否是从generation对象转换来的键
                            if (this.initialData.generation && this.initialData.generation.hasOwnProperty(key)) {
                                this.initialData.generation[key] = keyOrData[key];
                            } else {
                                this.initialData[key] = keyOrData[key];
                            }
                            updateData.data[key] = keyOrData[key];
                        }
                    });
                } 
                // 如果是字符串，则更新单个键
                else if (typeof keyOrData === 'string') {
                    // 设置到内部数据
                    if (keyOrData.includes('.')) {
                        this.set(keyOrData, value);
                        // 为AMIS准备扁平化的数据
                        const finalKey = keyOrData.split('.').pop();
                        updateData.data[finalKey] = value;
                    } else {
                        // 检查是否是从generation对象转换来的键
                        if (this.initialData.generation && this.initialData.generation.hasOwnProperty(keyOrData)) {
                            this.initialData.generation[keyOrData] = value;
                        } else {
                            this.initialData[keyOrData] = value;
                        }
                        updateData.data[keyOrData] = value;
                    }
                }

                // 确保完整同步generation对象的关键属性到顶层
                if (this.initialData.generation) {
                    // 同步generation的关键属性到顶层，确保数据链访问
                    const keysToSync = [
                        'progressStage', 'progressPercentage', 'progressMessage', 
                        'status', 'errorMessage', 'errorDetails', 'completionMessage'
                    ];
                    
                    keysToSync.forEach(key => {
                        if (this.initialData.generation[key] !== undefined) {
                            updateData.data[key] = this.initialData.generation[key];
                            // 同步到initialData顶层保持数据一致性
                            this.initialData[key] = this.initialData.generation[key];
                        }
                    });

                    // 特殊处理generationStatus字段，对应generation.status
                    if (this.initialData.generation.status !== undefined) {
                        updateData.data.generationStatus = this.initialData.generation.status;
                        this.initialData.generationStatus = this.initialData.generation.status;
                    }
                }

                // 使用AMIS实例更新UI
                amisInstance.updateProps(updateData);
                
                // 尝试强制更新UI
                setTimeout(() => {
                    if (amisInstance.forceUpdate) {
                        amisInstance.forceUpdate();
                    }
                }, 10);
                
                return true;
            } catch (error) {
                console.error('更新AMIS数据失败:', error);
                return false;
            }
        },

        /**
         * 添加日志条目
         * @param {string} message - 日志消息
         */
        addLog(message) {
            const timestamp = new Date().toLocaleTimeString();
            // 使用简单字符串格式的日志项，便于Log组件显示
            const logItem = `[${timestamp}] ${message}`;
            
            // 添加到开头
            if (!this.initialData.logs) {
                this.initialData.logs = [];
            }
            this.initialData.logs.unshift(logItem);
            
            // 限制日志条数
            if (this.initialData.logs.length > 100) {
                this.initialData.logs = this.initialData.logs.slice(0, 100);
            }
            
            // 同步到AMIS可用的generationLogs字段
            this.initialData.generationLogs = [...this.initialData.logs];
            
            // 更新AMIS - 直接使用updateProps更新可靠性更高
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        generationLogs: this.initialData.logs
                    }
                });
                
                // 强制刷新
                setTimeout(() => {
                    if (amisInstance && amisInstance.forceUpdate) {
                        amisInstance.forceUpdate();
                    }
                }, 50);
            }
            
            console.log('生成日志:', message);
        },

        /**
         * 更新生成状态
         * @param {string} status - 生成状态
         */
        updateGenerationStatus(status) {
            this.set('generation.status', status);
            this.updateAmis('generationStatus', status);
            
            // 确保根据状态更新页面CSS类
            setTimeout(() => {
                checkGenerationStatusClass(status);
            }, 50);
            
            console.log(`更新生成状态: ${status}`);
        },

        /**
         * 更新进度
         * @param {string} stage - 当前阶段
         * @param {string} message - 进度消息
         * @param {number} percentage - 进度百分比
         */
        updateProgress(stage, message, percentage) {
            // 确保百分比是有效数值
            if (isNaN(percentage)) {
                percentage = 0;
            }
            
            // 获取格式化的阶段名称
            const formattedStage = getStageName(stage);
            
            // 更新内部数据
            this.set('generation.progressStage', formattedStage);
            this.set('generation.progressMessage', message);
            this.set('generation.progressPercentage', percentage);
            
            // 同时更新到顶层，确保数据链访问
            this.initialData.progressStage = formattedStage;
            this.initialData.progressMessage = message;
            this.initialData.progressPercentage = percentage;
            
            // 直接使用amisInstance更新更可靠
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        progressStage: formattedStage,
                        progressMessage: message,
                        progressPercentage: percentage,
                        // 确保生成状态一致
                        generationStatus: this.get('generation.status', 'generating')
                    }
                });
                
                // 强制刷新
                setTimeout(() => {
                    if (amisInstance && amisInstance.forceUpdate) {
                        amisInstance.forceUpdate();
                    }
                }, 50);
            }
            
            console.log(`更新进度: ${stage}, ${message}, ${percentage}%`);
        },

        /**
         * 设置表单数据
         * @param {object} formData - 表单数据
         */
        setFormData(formData) {
            this.initialData.formData = formData;
        },

        /**
         * 重置所有数据到初始状态
         */
        reset() {
            // 重置基础数据
            this.initialData.generation = {
                status: 'waiting',
                sessionId: null,
                progressStage: '准备中...',
                progressPercentage: 0,
                progressMessage: '正在初始化...',
                questionCount: 0,
                duration: 0,
                errorMessage: '',
                errorDetails: '',
                completionMessage: '生成完成'
            };
            this.initialData.logs = [];
            this.initialData.generatedQuestions = [];
            this.initialData.showResults = false;
            
            // 更新AMIS
            this.updateAmis({
                generationStatus: 'waiting',
                progressStage: '准备中...',
                progressMessage: '正在初始化...',
                progressPercentage: 0,
                errorMessage: '',
                errorDetails: '',
                generationLogs: [],
                generatedQuestions: [],
                showResults: false
            });
            
            console.log('数据已重置');
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
    const ErrorHandler = {
        /**
         * 处理API错误
         * @param {string} title - 错误标题
         * @param {string} message - 错误消息
         */
        handleApiError: function (title, message) {
            console.error(`${title}: ${message}`);

            // 添加到日志
            DataManager.addLog(`错误: ${message}`);

            // 更新状态
            DataManager.set('generation.status', 'error');
            DataManager.set('generation.errorMessage', title);
            DataManager.set('generation.errorDetails', message);
            
            // 更新AMIS数据
            DataManager.updateAmis({
                generationStatus: 'error',
                errorMessage: title,
                errorDetails: message
            });

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

        DataManager.addLog("正在建立实时连接...");
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
                DataManager.addLog("实时连接已建立");
                console.log("实时连接已建立");
                // 加入生成组
                return connection.invoke("JoinGenerationGroup", sessionId);
            })
            .then(function () {
                DataManager.addLog(`已加入生成组: ${sessionId}`);
                console.log(`已加入生成组: ${sessionId}`);
                executeGeneration(sessionId);
            })
            .catch(function (err) {
                console.error("连接错误:", err);
                ErrorHandler.handleApiError("连接错误", err.toString());
            });
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
            DataManager.updateGenerationStatus("generating");
            DataManager.addLog("生成已开始");
        });

        connection.on("GenerationProgress", (data) => {
            console.log(`收到生成进度更新:`, data);
            // 从对象中提取信息
            const stage = data.stage || '';
            const message = data.message || '';
            const percentage = data.percentage || 0;
            DataManager.updateProgress(stage, message, percentage);
            
            // 添加进度日志
            const progressMessage = `${getStageName(stage)}: ${message} (${percentage}%)`;
            DataManager.addLog(progressMessage);
        });

        connection.on("GenerationCompleted", (data) => {
            console.log(`收到生成完成事件:`, data);
            // 从对象中提取信息
            const generatedCount = data.generatedCount || data.questionCount || 0;
            const message = data.message || generatedCount;
            
            // 更新状态
            DataManager.updateGenerationStatus("completed");
            // 更新进度
            DataManager.updateProgress("已完成", message, 100);
            DataManager.addLog(`生成完成: ${message}`);

            // 更新生成数量信息
            DataManager.updateAmis({
                questionCount: generatedCount,
                generationStatus: 'completed'
            });

            // 自动获取生成的题目
            const sessionId = DataManager.get('generation.sessionId');
            if (sessionId) {
                // 添加短暂延迟确保后端数据已就绪
                setTimeout(() => {
                    fetchGeneratedQuestions(sessionId);
                    // 显示结果区域
                    DataManager.updateAmis('showResults', true);
                    
                    // 滚动到结果区域
                    setTimeout(() => {
                        const resultsElement = document.querySelector('.generated-questions-container');
                        if (resultsElement) {
                            resultsElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
                        }
                    }, 500);
                }, 800);
            } else {
                console.error("无法获取生成的题目，会话ID不存在");
                DataManager.addLog("无法获取生成的题目，会话ID不存在");
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

            DataManager.updateGenerationStatus("error");
            DataManager.updateProgress("失败", errorMessage, 0);
            DataManager.addLog(`生成失败: ${errorMessage}`);
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
        DataManager.updateAmis({
            isFetchingQuestions: true,
            questionFetchMessage: '正在获取生成的题目...'
        });

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
                    DataManager.updateAmis({
                        isFetchingQuestions: false,
                        questionFetchMessage: '题目数据格式不正确',
                        generatedQuestions: []
                    });
                    return;
                }

                // 使用处理函数确保所有字段正确处理
                const formattedQuestions = processQuestionData(data.data);

                // 更新AMIS组件
                DataManager.updateAmis({
                    isFetchingQuestions: false,
                    questionFetchMessage: '题目获取成功',
                    generatedQuestions: formattedQuestions,
                    showResults: true,               // 确保显示结果面板
                    generationStatus: 'completed'    // 确保状态正确
                });
                
                // 确保状态类正确
                checkGenerationStatusClass('completed');

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
                DataManager.updateAmis({
                    isFetchingQuestions: false,
                    questionFetchMessage: `获取题目失败: ${error.message}`,
                    generatedQuestions: []
                });
            });
    }

    /**
     * 辅助函数 - 从题目类型ID获取名称
     * @param {number} typeId - 题目类型ID
     * @returns {string} 题目类型名称
     */
    function getQuestionTypeNameByValue(typeId) {
        const typeMap = {
            1: '单选题',
            2: '多选题',
            3: '判断题',
            4: '填空题',
            5: '简答题'
        };
        return typeMap[typeId] || '未知类型';
    }

    /**
     * 辅助函数 - 从难度ID获取名称
     * @param {number} difficultyId - 题目难度ID
     * @returns {string} 题目难度名称
     */
    function getQuestionDifficultyNameByValue(difficultyId) {
        const difficultyMap = {
            1: '简单',
            2: '中等',
            3: '困难'
        };
        return difficultyMap[difficultyId] || '未知难度';
    }

    /**
     * 辅助函数 - 格式化正确答案
     * @param {object} question - 题目对象
     * @returns {string} 格式化后的正确答案
     */
    function formatCorrectAnswer(question) {
        // 如果已有正确答案，直接返回
        if (question.correctAnswer) return question.correctAnswer;
        
        // 尝试从选项中提取正确答案
        if (Array.isArray(question.options)) {
            const correctOptions = question.options.filter(opt => opt.isCorrect);
            if (correctOptions.length > 0) {
                // 对于单选题和多选题，以字母和内容格式化
                const labels = correctOptions.map((opt, idx) => 
                    String.fromCharCode(65 + question.options.indexOf(opt)));
                    
                if (labels.length > 0) {
                    return `${labels.join('、')}：${correctOptions.map(opt => opt.content).join('；')}`;
                }
            }
        }
        
        // 判断题特殊处理
        if (question.type === 3) {
            return question.answer === true ? '正确' : '错误';
        }
        
        return '未提供答案';
    }

    /**
     * 获取认证令牌
     * @returns {string} 认证令牌
     */
    function getToken() {
        // 从cookie或localStorage中获取令牌
        return document.cookie.replace(/(?:(?:^|.*;\s*)token\s*\=\s*([^;]*).*$)|^.*$/, "$1") ||
            localStorage.getItem('token');
    }

    /**
     * 执行生成
     * @param {string} sessionId - 生成会话ID
     */
    function executeGeneration(sessionId) {
        DataManager.addLog("触发后端生成过程...");

        // 获取表单数据
        const formData = DataManager.initialData.formData || {};

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

        // 保存表单数据
        DataManager.setFormData(formData);

        // 更新状态为生成中
        DataManager.updateGenerationStatus('generating');
        
        // 初始化进度
        DataManager.updateProgress('准备中...', '正在初始化...', 0);
        
        // 清空日志
        DataManager.initialData.logs = [];
        DataManager.updateAmis('generationLogs', []);

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

                    // 更新会话ID
                    DataManager.set('generation.sessionId', sessionId);
                    console.log("获取到会话ID:", sessionId);

                    // 初始化SignalR连接
                    setTimeout(() => {
                        console.log("即将初始化SignalR连接...");
                        initializeSignalRConnection(sessionId);
                    }, 100);
                } else {
                    // 错误处理
                    console.error("请求返回失败状态:", responseData);
                    ErrorHandler.handleApiError("生成失败", responseData.msg || "未知错误");
                    
                    // 停止连接
                    if (connection) {
                        connection.stop();
                    }
                }
            })
            .catch(error => {
                // 错误处理
                console.error("请求失败:", error);
                ErrorHandler.handleApiError("请求失败", error.toString());
                
                // 停止连接
                if (connection) {
                    connection.stop();
                }
            });
    };

    /**
     * 重置表单和状态
     */
    function resetForm() {
        // 重置数据
        DataManager.reset();
        
        // 如果存在连接，停止连接
        if (connection) {
            connection.stop();
        }

        currentSessionId = null;
        
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
                    DataManager.updateAmis('signalRConnected', false);
                })
                .catch(err => {
                    console.error('关闭SignalR连接时发生错误:', err);
                });
        }
    }

    // AMIS页面配置
    const amisJSON = {
        type: "page",
        title: "AI题目生成",
        className: "question-generation-page p-3",
        data: {
            // 使用数据链，共享初始数据
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
            generatedQuestions: [],
            showResults: false
        },
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
                            // 添加数据域，方便表单内数据传递
                            data: {
                                topic: "C#编程基础",
                                count: 10,
                                type: 1,
                                difficulty: 2
                            },
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
                                    // 使用数据链表达式引用父级数据域
                                    disabledOn: "${generationStatus === 'generating'}"
                                }
                            ],
                            body: [
                                {
                                    type: "input-text",
                                    name: "topic",
                                    label: "主题",
                                    required: true,
                                    maxLength: 100,
                                    placeholder: "请输入题目主题或知识领域",
                                    description: "请输入题目主题或知识领域",
                                    // 使用数据链表达式引用父级数据域
                                    disabledOn: "${generationStatus === 'generating'}",
                                    clearable: true,
                                    prefixIcon: "fa fa-book"
                                },
                                {
                                    type: "input-number",
                                    name: "count",
                                    label: "题目数量",
                                    min: 1,
                                    max: 10,
                                    step: 1,
                                    required: true,
                                    description: "范围为1-10题",
                                    disabledOn: "${generationStatus === 'generating'}",
                                    displayMode: "enhance"
                                },
                                {
                                    type: "select",
                                    name: "type",
                                    label: "题目类型",
                                    options: [
                                        { label: "单选题", value: 1 },
                                        { label: "多选题", value: 2 },
                                        { label: "判断题", value: 3 }
                                    ],
                                    required: true,
                                    disabledOn: "${generationStatus === 'generating'}",
                                    searchable: true,
                                    clearable: false
                                },
                                {
                                    type: "select",
                                    name: "difficulty",
                                    label: "难度",
                                    options: [
                                        { label: "简单", value: 1, badge: "success" },
                                        { label: "中等", value: 2, badge: "warning" },
                                        { label: "困难", value: 3, badge: "danger" }
                                    ],
                                    required: true,
                                    disabledOn: "${generationStatus === 'generating'}",
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
                                    disabledOn: "${generationStatus === 'generating'}",
                                    searchable: true
                                },
                                {
                                    type: "textarea",
                                    name: "requirements",
                                    label: "生成要求",
                                    maxLength: 500,
                                    showCounter: true,
                                    placeholder: "请输入对生成题目的特定要求，例如：围绕某个特定概念、包含具体知识点等",
                                    disabledOn: "${generationStatus === 'generating'}",
                                    minRows: 3,
                                    maxRows: 6
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
                            // 使用数据链共享父级数据
                            data: "${parent.data}",
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
                                            // 使用数据链表达式判断状态
                                            visibleOn: "${generationStatus === 'waiting' || !generationStatus}"
                                        },

                                        // 生成中状态
                                        {
                                            type: "container",
                                            className: "fade-in",
                                            // 使用数据链表达式判断状态
                                            visibleOn: "${generationStatus === 'generating'}",
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
                                                                // 使用数据链表达式访问父级数据
                                                                tpl: "<div class='progress-stage fw-medium'>${progressStage || '准备中...'}</div>",
                                                                className: "font-weight-bold"
                                                            }
                                                        },
                                                        {
                                                            md: 6,
                                                            body: {
                                                                type: "tpl",
                                                                className: "text-right text-end",
                                                                // 使用数据链表达式访问父级数据
                                                                tpl: "<div class='progress-percentage fw-bold'>${progressPercentage || 0}%</div>"
                                                            }
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: "progress",
                                                    mode: "line",
                                                    // 使用数据链表达式访问父级数据
                                                    value: "${progressPercentage || 0}",
                                                    strokeWidth: 8,
                                                    showLabel: false,
                                                    animate: true,
                                                    className: "generation-progress mb-3"
                                                },
                                                {
                                                    type: "alert",
                                                    level: "info",
                                                    // 使用数据链表达式访问父级数据
                                                    body: "${progressMessage || '正在初始化...'}",
                                                    showIcon: true,
                                                    className: "mt-2 shadow-sm"
                                                },
                                                {
                                                    type: "card",
                                                    title: "生成日志",
                                                    className: "mt-4 shadow",
                                                    headerClassName: "bg-dark text-light py-2 px-3 rounded-top",
                                                    bodyClassName: "p-0",
                                                    header: {
                                                        title: "生成日志",
                                                        className: "fs-6"
                                                    },
                                                    body: {
                                                        type: "log",
                                                        source: "${generationLogs}",
                                                        height: 200,
                                                        className: "bg-dark text-light",
                                                        operation: ["stop", "clear", "export"],
                                                        placeholder: "<div class='text-muted p-2'>暂无日志记录</div>",
                                                        autoScroll: true
                                                    }
                                                }
                                            ]
                                        },

                                        // 生成完成状态
                                        {
                                            type: "container",
                                            className: "fade-in",
                                            // 使用数据链表达式判断状态
                                            visibleOn: "${generationStatus === 'completed' || showResults === true}",
                                            body: [
                                                {
                                                    type: "alert",
                                                    level: "success",
                                                    showIcon: true,
                                                    // 使用数据链表达式访问父级数据
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
                                                                                // 使用数据链表达式访问父级数据
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
                                                                                // 使用数据链表达式访问父级数据
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
                                                            // 使用数据链表达式访问父级数据
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
                                                                    type: 'html',
                                                                    html: '${typeDisplay|raw}',
                                                                    width: 80
                                                                },
                                                                {
                                                                    name: 'difficulty',
                                                                    label: '难度',
                                                                    type: 'html',
                                                                    html: '${difficultyDisplay|raw}',
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
                                                                                title: '题目详情 - ${typeName}',
                                                                                headerClassName: 'bg-primary text-white',
                                                                                closeOnEsc: true,
                                                                                closeOnOutside: true,
                                                                                showCloseButton: true,
                                                                                size: 'lg',
                                                                                // 使用严格模式传递数据
                                                                                data: {
                                                                                    "id": "${id}",
                                                                                    "type": "${type}",
                                                                                    "typeName": "${typeName}",
                                                                                    "difficulty": "${difficulty}",
                                                                                    "difficultyName": "${difficultyName}",
                                                                                    "content": "${content|raw}",
                                                                                    "options": "${options|json|raw}",
                                                                                    "correctAnswer": "${correctAnswer|raw}",
                                                                                    "analysis": "${analysis|raw}"
                                                                                },
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
                                                                                                            type: 'html',
                                                                                                            html: '<div class="mb-2"><span class="text-muted me-2">题型:</span>${typeDisplay|raw}</div>'
                                                                                                        }
                                                                                                    },
                                                                                                    {
                                                                                                        md: 4,
                                                                                                        body: {
                                                                                                            type: 'html',
                                                                                                            html: '<div class="mb-2"><span class="text-muted me-2">难度:</span>${difficultyDisplay|raw}</div>'
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
                                                                                            type: 'html',
                                                                                            html: '<div class="question-content">${content|raw}</div>'
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
                                                                                                type: 'service',
                                                                                                api: {
                                                                                                    method: 'get',
                                                                                                    url: '/api/dummy',
                                                                                                    mockResponse: "${options|json|raw}",
                                                                                                    responseType: 'json'
                                                                                                },
                                                                                                body: [
                                                                                                    {
                                                                                                        type: 'each',
                                                                                                        source: '${items}',
                                                                                                        items: {
                                                                                                            type: 'card',
                                                                                                            className: 'mb-2 border-0 bg-light option-card',
                                                                                                            bodyClassName: '${isCorrect ? "bg-success bg-opacity-10 border-start border-3 border-success" : ""}',
                                                                                                            body: {
                                                                                                                type: 'tpl',
                                                                                                                tpl: '<div class="p-2"><span class="badge ${isCorrect ? "bg-success" : "bg-secondary"} me-2">${label}</span> ${content}</div>'
                                                                                                            }
                                                                                                        }
                                                                                                    }
                                                                                                ]
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
                                                                                                body: '${correctAnswer|raw}'
                                                                                            },
                                                                                            {
                                                                                                type: 'divider',
                                                                                                className: 'my-2'
                                                                                            },
                                                                                            {
                                                                                                type: 'html',
                                                                                                html: '<h5 class="mb-2">解析</h5><div class="analysis-content">${analysis|raw}</div>'
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
                                            // 使用数据链表达式判断状态
                                            visibleOn: "${generationStatus === 'error'}",
                                            body: [
                                                {
                                                    type: "alert",
                                                    level: "danger",
                                                    showIcon: true,
                                                    // 使用数据链表达式访问父级数据
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
                                                    // 使用数据链表达式访问父级数据
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

        // 配置AMIS应用
        const app = amisApp.embed('#question-generation-app', amisJSON, {
            locale: 'zh-CN',
            theme: 'antd',
            // 使用DataManager的初始数据
            data: DataManager.initialData
        }, {
            requestAdaptor: requestAdaptor,
            // AMIS错误处理
            responseAdaptor: function (api, payload, query, request, response) {
                // 处理错误响应
                if (response.status >= 400) {
                    ErrorHandler.showError(`请求失败: ${response.status} ${response.statusText}`);
                    return payload;
                }

                return payload;
            },
            // 添加动作触发时的回调
            onAction: function (e) {
                console.log("AMIS动作触发:", e);
            },
            // 添加值变更事件监听
            watchData: function (data) {
                console.log("AMIS数据变更:", data);
                // 监听关键状态变更
                if (data) {
                    // 监听生成状态变更
                    if (data.generationStatus) {
                        // 更新状态
                        DataManager.set('generation.status', data.generationStatus);
                        console.log("页面状态变更:", data.generationStatus);
                        // 检查CSS类
                        checkGenerationStatusClass(data.generationStatus);
                    }
                    
                    // 监听进度相关字段变更
                    const progressKeys = ['progressPercentage', 'progressStage', 'progressMessage'];
                    let hasProgressChange = false;
                    progressKeys.forEach(key => {
                        if (data[key] !== undefined) {
                            DataManager.set(`generation.${key}`, data[key]);
                            hasProgressChange = true;
                        }
                    });
                    
                    // 如果有进度变更，强制更新UI
                    if (hasProgressChange && amisInstance && amisInstance.forceUpdate) {
                        setTimeout(() => amisInstance.forceUpdate(), 10);
                    }
                }
            }
        });

        // 保存应用实例到全局变量
        amisInstance = app;
        window.amisInstance = app;

        // 暴露重置函数到全局
        window.resetForm = resetForm;

        // 立即设置一次状态类，确保CSS正确
        checkGenerationStatusClass('waiting');

        console.log("AMIS应用初始化完成", app);
    });

    /**
     * 处理题目数据，确保所有显示字段正确处理
     * @param {Array} questions - 原始题目数据数组 
     * @returns {Array} 处理后的题目数据数组
     */
    function processQuestionData(questions) {
        if (!Array.isArray(questions)) return [];
        
        return questions.map((question, index) => {
            // 处理选项，添加标签 (A, B, C, D...)
            const options = Array.isArray(question.options) ?
                question.options.map((option, optIndex) => ({
                    ...option,
                    label: String.fromCharCode(65 + optIndex), // A, B, C, D...
                    // 确保选项内容是HTML安全的
                    content: option.content || ''
                })) : [];
            
            // 处理正确答案 - 如果未提供则尝试从选项中构建
            let correctAnswer = question.correctAnswer || formatCorrectAnswer(question);
            
            // 处理解析内容 - 确保存在
            let analysis = question.analysis || '暂无解析';
            
            // 处理题目类型名称
            let typeName = question.typeName || getQuestionTypeNameByValue(question.type);
            
            // 处理难度名称
            let difficultyName = question.difficultyName || getQuestionDifficultyNameByValue(question.difficulty);
            
            // 确保题目内容存在
            let content = question.content || '无内容';
            
            // 返回完整的题目对象
            return {
                ...question,
                index: index + 1,
                typeName,
                difficultyName,
                content,
                correctAnswer,
                analysis,
                options,
                // 添加额外字段以方便UI使用
                typeDisplay: getQuestionTypeDisplay(question.type),
                difficultyDisplay: getQuestionDifficultyDisplay(question.difficulty)
            };
        });
    }

    /**
     * 获取题目类型显示HTML
     * @param {number} typeId - 题目类型ID
     * @returns {string} 类型显示HTML
     */
    function getQuestionTypeDisplay(typeId) {
        const typeMap = {
            1: '<span class="status-badge status-badge-success">单选题</span>',
            2: '<span class="status-badge status-badge-processing">多选题</span>',
            3: '<span class="status-badge">判断题</span>',
            4: '<span class="status-badge status-badge-info">填空题</span>',
            5: '<span class="status-badge status-badge-warning">简答题</span>'
        };
        return typeMap[typeId] || '<span class="status-badge">未知类型</span>';
    }

    /**
     * 获取题目难度显示HTML
     * @param {number} difficultyId - 难度ID
     * @returns {string} 难度显示HTML
     */
    function getQuestionDifficultyDisplay(difficultyId) {
        const difficultyMap = {
            1: '<span class="text-success"><i class="fa fa-circle me-1"></i>简单</span>',
            2: '<span class="text-warning"><i class="fa fa-circle me-1"></i>中等</span>',
            3: '<span class="text-danger"><i class="fa fa-circle me-1"></i>困难</span>'
        };
        return difficultyMap[difficultyId] || '<span>未知难度</span>';
    }

})(); 