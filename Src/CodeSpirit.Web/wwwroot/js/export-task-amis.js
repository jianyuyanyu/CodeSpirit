/**
 * 导出任务进度页面脚本 - 基于AMIS框架
 * 实现导出任务进度展示功能，包括状态更新、进度展示和文件下载
 */
(function () {
    'use strict';

    // 初始化变量
    let taskId = null;            // 当前任务ID
    let amisInstance = null;      // AMIS实例对象
    let pollingTimer = null;      // 轮询定时器
    let pollingInterval = 1000;   // 轮询间隔（毫秒）

    /**
     * 初始化TokenManager为租户模式
     */
    function initializeTokenManager() {
        try {
            const tenantId = getTenantIdFromContext();
            TokenManager.initTenantMode(tenantId);
            console.log('TokenManager: 已初始化为租户模式');
        } catch (error) {
            console.error('初始化TokenManager失败:', error);
            // 仍然尝试初始化租户模式，但不传入租户ID
            TokenManager.initTenantMode(null);
        }
    }

    /**
     * 从上下文获取租户ID
     * @returns {string|null} 租户ID
     */
    function getTenantIdFromContext() {
        try {
            // 1. 从URL参数获取
            const urlParams = new URLSearchParams(window.location.search);
            const tenantIdFromUrl = urlParams.get('tenantId');
            if (tenantIdFromUrl) {
                return tenantIdFromUrl;
            }
            
            // 2. 从路径中提取（如 /tenant/{tenantId}/...）
            const pathSegments = window.location.pathname.split('/');
            const tenantIndex = pathSegments.findIndex(segment => segment === 'tenant');
            if (tenantIndex >= 0 && tenantIndex < pathSegments.length - 1) {
                return pathSegments[tenantIndex + 1];
            }
            
            // 3. 从TokenManager已存储的平台信息获取
            const platformInfo = TokenManager.getPlatformInfo();
            if (platformInfo && platformInfo.tenantId) {
                return platformInfo.tenantId;
            }
            
            // 4. 从页面元数据获取
            const tenantMeta = document.querySelector('meta[name="tenant-id"]');
            if (tenantMeta) {
                return tenantMeta.getAttribute('content');
            }
            
            // 5. 从全局变量获取（如果页面设置了）
            if (window.CURRENT_TENANT_ID) {
                return window.CURRENT_TENANT_ID;
            }
            
            return null;
        } catch (error) {
            console.error('获取租户ID失败:', error);
            return null;
        }
    }

    /**
     * 检查Token是否有效
     * @returns {boolean} Token是否有效
     */
    function isTokenValid() {
        return TokenManager.isAuthenticated();
    }

    /**
     * 处理认证错误，跳转到租户登录页面
     */
    function handleAuthError() {
        console.error('认证失败，准备跳转租户登录页面');
        
        // 延迟跳转，给用户看到错误信息的时间
        setTimeout(() => {
            const loginUrl = getLoginUrl();
            if (loginUrl) {
                window.location.href = loginUrl;
            }
        }, 2000);
    }

    /**
     * 获取租户登录页面URL
     * @returns {string} 登录URL
     */
    function getLoginUrl() {
        const currentUrl = encodeURIComponent(window.location.href);
        return `/tenant/login?returnUrl=${currentUrl}`;
    }

    // 数据管理器 - 统一管理数据更新和传递
    const DataManager = {
        // 基础数据结构
        initialData: {
            task: {
                taskId: '',
                fileName: '',
                status: '加载中',
                createTime: '',
                completionTime: '',
                progress: 0,
                processedRecords: 0,
                totalRecords: 0,
                fileUrl: '',
                errorMessage: ''
            },
            isLoading: true,
            hasError: false,
            errorMessage: '',
            token: ''
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
                            // 检查是否是从task对象转换来的键
                            if (this.initialData.task && this.initialData.task.hasOwnProperty(key)) {
                                this.initialData.task[key] = keyOrData[key];
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
                        // 检查是否是从task对象转换来的键
                        if (this.initialData.task && this.initialData.task.hasOwnProperty(keyOrData)) {
                            this.initialData.task[keyOrData] = value;
                        } else {
                            this.initialData[keyOrData] = value;
                        }
                        updateData.data[keyOrData] = value;
                    }
                }

                // 确保完整同步task对象的关键属性到顶层，方便数据访问
                if (this.initialData.task) {
                    // 同步task的关键属性到顶层
                    const keysToSync = [
                        'fileName', 'status', 'progress', 'createTime',
                        'completionTime', 'processedRecords', 'totalRecords',
                        'fileUrl', 'errorMessage'
                    ];

                    keysToSync.forEach(key => {
                        if (this.initialData.task[key] !== undefined) {
                            updateData.data[key] = this.initialData.task[key];
                            // 同步到initialData顶层保持数据一致性
                            this.initialData[key] = this.initialData.task[key];
                        }
                    });
                }

                // 使用AMIS实例更新UI
                amisInstance.updateProps(updateData);

                // 强制更新UI
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
         * 更新任务数据
         * @param {object} taskData - 任务数据
         */
        updateTaskData(taskData) {
            if (!taskData) return;

            // 更新内部数据
            this.initialData.task = { ...this.initialData.task, ...taskData };

            // 同步到顶层数据
            Object.keys(taskData).forEach(key => {
                this.initialData[key] = taskData[key];
            });

            // 添加token到数据模型 - 使用TokenManager
            this.initialData.token = TokenManager.getToken() || '';

            // 更新AMIS数据
            this.updateAmis({
                ...taskData,
                token: TokenManager.getToken() || ''
            });

            console.log('任务数据已更新:', taskData);
        },

        /**
         * 重置所有数据到初始状态
         */
        reset() {
            // 重置基础数据
            this.initialData.task = {
                taskId: '',
                fileName: '',
                status: '加载中',
                createTime: '',
                completionTime: '',
                progress: 0,
                processedRecords: 0,
                totalRecords: 0,
                fileUrl: '',
                errorMessage: ''
            };
            this.initialData.isLoading = true;
            this.initialData.hasError = false;
            this.initialData.errorMessage = '';

            // 更新AMIS
            this.updateAmis({
                isLoading: true,
                hasError: false,
                errorMessage: '',
                fileName: '',
                status: '加载中',
                progress: 0
            });

            console.log('数据已重置');
        }
    };

    /**
     * 添加认证请求适配器 - 使用TokenManager
     */
    const requestAdaptor = function (api) {
        console.log("请求适配器被调用:", api);
        
        // 使用TokenManager获取认证头
        const authHeaders = TokenManager.getAuthHeaders();
        
        return {
            ...api,
            headers: {
                ...api.headers,
                ...authHeaders,  // 包含Authorization和租户相关头信息
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

            // 确保message是字符串
            const errorMessage = typeof message === 'object' ?
                JSON.stringify(message, null, 2) : String(message);

            // 更新状态
            DataManager.updateAmis({
                isLoading: false,
                hasError: true,
                errorMessage: errorMessage
            });
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
     * 获取认证令牌 - 使用TokenManager
     * @returns {string} 认证令牌
     */
    function getToken() {
        return TokenManager.getToken();
    }

    /**
     * 从URL中获取任务ID
     * @returns {string} 任务ID
     */
    function getTaskIdFromUrl() {
        const pathSegments = window.location.pathname.split('/');
        return pathSegments[pathSegments.length - 1];
    }

    /**
     * 从fileUrl中提取fileId
     * @param {string} fileUrl - 文件URL
     * @returns {string|null} 提取的fileId，如果无法提取则返回null
     */
    function extractFileIdFromUrl(fileUrl) {
        if (!fileUrl) return null;

        // 尝试从URL中提取fileId
        // 假设fileUrl格式可能是以下几种之一：
        // 1. /api/tempfiles/download/{fileId}
        // 2. /api/tempfiles/{fileId}
        // 3. https://domain.com/api/tempfiles/download/{fileId}
        // 4. 其他包含fileId的格式

        try {
            // 创建URL对象（对于完整URL）或者假URL（对于相对路径）
            const url = fileUrl.startsWith('http') ? new URL(fileUrl) : new URL(fileUrl, window.location.origin);

            // 分割路径
            const pathParts = url.pathname.split('/').filter(Boolean);

            // 检查常见模式
            if (pathParts.includes('download')) {
                // 如果路径包含'download'，取其后面的部分作为fileId
                const downloadIndex = pathParts.indexOf('download');
                if (downloadIndex >= 0 && downloadIndex < pathParts.length - 1) {
                    return pathParts[downloadIndex + 1];
                }
            }

            // 如果路径包含'tempfiles'，取其后面的部分作为fileId
            if (pathParts.includes('tempfiles')) {
                const tempfilesIndex = pathParts.indexOf('tempfiles');
                if (tempfilesIndex >= 0 && tempfilesIndex < pathParts.length - 1) {
                    return pathParts[tempfilesIndex + 1];
                }
            }

            // 如果以上都不匹配，取路径的最后一部分作为fileId
            if (pathParts.length > 0) {
                return pathParts[pathParts.length - 1];
            }

            return null;
        } catch (error) {
            console.error('从URL提取fileId失败:', error);
            return null;
        }
    }

    /**
     * 加载任务信息
     * @param {string} taskId - 任务ID
     */
    async function loadTaskInfo(taskId) {
        if (!taskId) {
            ErrorHandler.handleApiError("参数错误", "未提供任务ID");
            return;
        }

        // 检查Token有效性
        if (!isTokenValid()) {
            ErrorHandler.handleApiError("认证失败", "Token无效或已过期，请重新登录");
            return;
        }

        DataManager.updateAmis({
            isLoading: true,
            hasError: false,
            errorMessage: ''
        });

        try {
            // 使用TokenManager获取认证头
            const authHeaders = TokenManager.getAuthHeaders();
            
            const response = await fetch(`/api/tempfiles/export-tasks/${taskId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...authHeaders,  // 使用TokenManager的认证头
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });

            if (!response.ok) {
                // 如果是401错误，尝试刷新Token
                if (response.status === 401) {
                    const refreshSuccess = await TokenManager.refreshToken();
                    if (refreshSuccess) {
                        // 重新尝试请求
                        return loadTaskInfo(taskId);
                    } else {
                        throw new Error('认证失败，请重新登录');
                    }
                }
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const data = await response.json();

            if (data.status === 0 && data.data) {
                // 格式化日期时间
                if (data.data.createTime) {
                    data.data.createTime = formatDateTime(data.data.createTime);
                }
                if (data.data.completionTime) {
                    data.data.completionTime = formatDateTime(data.data.completionTime);
                }
                if (data.data.updateTime) {
                    data.data.updateTime = formatDateTime(data.data.updateTime);
                }

                // 处理错误消息
                const errorMessages = data.data.errorMessages || [];

                // 从fileUrl中提取fileId
                const fileId = extractFileIdFromUrl(data.data.fileUrl);

                // 更新任务数据
                DataManager.updateTaskData({
                    ...data.data,
                    taskId: taskId,
                    fileId: fileId, // 添加fileId到数据模型
                    errorMessages: errorMessages,
                    isLoading: false
                });

                console.log("任务数据已加载:", data.data);

                // 处理任务状态和样式
                applyTaskStatusStyles(data.data.status);

                // 检查是否需要继续轮询
                if (isTaskCompleted(data.data.status)) {
                    stopPolling();
                } else {
                    startPolling(taskId);
                }
            } else {
                ErrorHandler.handleApiError("获取任务失败", data.msg || "未知错误");
            }
        } catch (error) {
            console.error("加载任务信息失败:", error);
            ErrorHandler.handleApiError("加载任务信息失败", error.toString());
        }
    }

    /**
     * 格式化日期时间
     * @param {string} dateTimeStr - 日期时间字符串
     * @returns {string} 格式化后的日期时间
     */
    function formatDateTime(dateTimeStr) {
        if (!dateTimeStr) return '';

        try {
            const date = new Date(dateTimeStr);
            return date.toLocaleString('zh-CN', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: false
            });
        } catch (e) {
            return dateTimeStr;
        }
    }

    /**
     * 检查任务是否已完成
     * @param {string} status - 任务状态
     * @returns {boolean} 是否已完成
     */
    function isTaskCompleted(status) {
        return status === "已完成" ||
            status === "已取消" ||
            status?.startsWith("失败") === true;
    }

    /**
     * 开始轮询任务状态
     * @param {string} taskId - 任务ID
     */
    function startPolling(taskId) {
        if (pollingTimer) {
            clearInterval(pollingTimer);
        }

        pollingTimer = setInterval(() => {
            refreshTaskInfo(taskId);
        }, pollingInterval);

        console.log(`开始轮询任务状态，任务ID: ${taskId}, 间隔: ${pollingInterval}ms`);
    }

    /**
     * 停止轮询
     */
    function stopPolling() {
        if (pollingTimer) {
            clearInterval(pollingTimer);
            pollingTimer = null;
            console.log("停止轮询任务状态");
        }
    }

    /**
     * 刷新任务信息
     * @param {string} taskId - 任务ID
     */
    async function refreshTaskInfo(taskId) {
        try {
            // 检查Token有效性
            if (!isTokenValid()) {
                console.error('Token无效，停止轮询');
                stopPolling();
                return;
            }

            // 使用TokenManager获取认证头
            const authHeaders = TokenManager.getAuthHeaders();
            
            const response = await fetch(`/api/tempfiles/export-tasks/${taskId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...authHeaders,  // 使用TokenManager的认证头
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });

            if (!response.ok) {
                // 如果是401错误，尝试刷新Token
                if (response.status === 401) {
                    const refreshSuccess = await TokenManager.refreshToken();
                    if (!refreshSuccess) {
                        console.error('Token刷新失败，停止轮询');
                        stopPolling();
                        return;
                    }
                    // Token刷新成功，下次轮询会使用新Token
                    return;
                }
                console.error(`刷新任务状态失败: HTTP ${response.status}`);
                return;
            }

            const data = await response.json();

            if (data.status === 0 && data.data) {
                // 格式化日期时间
                if (data.data.createTime) {
                    data.data.createTime = formatDateTime(data.data.createTime);
                }
                if (data.data.completionTime) {
                    data.data.completionTime = formatDateTime(data.data.completionTime);
                }
                if (data.data.updateTime) {
                    data.data.updateTime = formatDateTime(data.data.updateTime);
                }

                // 处理错误消息
                const errorMessages = data.data.errorMessages || [];

                // 记录前后状态差异
                const oldStatus = DataManager.get('task.status');
                const oldProgress = DataManager.get('task.progress', 0);

                // 从fileUrl中提取fileId
                const fileId = extractFileIdFromUrl(data.data.fileUrl);

                // 更新任务数据
                DataManager.updateTaskData({
                    ...data.data,
                    taskId: taskId,
                    fileId: fileId, // 添加fileId到数据模型
                    errorMessages: errorMessages,
                    isLoading: false
                });

                // 输出状态变化日志
                console.log(`任务状态更新: ${oldStatus}->${data.data.status}, 进度: ${oldProgress}->${data.data.progress}%`);

                // 处理任务状态和样式
                applyTaskStatusStyles(data.data.status);

                // 检查是否需要停止轮询
                if (isTaskCompleted(data.data.status)) {
                    stopPolling();
                }
            }
        } catch (error) {
            console.error("刷新任务信息失败:", error);
        }
    }

    /**
     * 应用任务状态相关的样式和交互效果
     * @param {string} status - 任务状态
     */
    function applyTaskStatusStyles(status) {
        setTimeout(() => {
            // 查找进度条元素并应用相应样式
            const progressBar = document.querySelector('.task-progress .progress-bar');
            if (progressBar) {
                // 移除所有状态类
                progressBar.classList.remove('bg-success', 'bg-warning', 'bg-danger', 'progress-bar-striped', 'progress-bar-animated');

                // 根据状态应用样式
                if (status === '处理中') {
                    progressBar.classList.add('progress-bar-striped', 'progress-bar-animated');
                } else if (status === '已完成') {
                    progressBar.classList.add('bg-success');
                } else if (status === '已取消') {
                    progressBar.classList.add('bg-warning');
                } else if (status.startsWith('失败')) {
                    progressBar.classList.add('bg-danger');
                }
            }

            // 查找状态徽章并应用相应样式
            const statusBadge = document.querySelector('.status-badge');
            if (statusBadge) {
                // 移除所有状态类
                statusBadge.classList.remove('processing', 'success', 'warning', 'error', 'default');

                // 根据状态应用样式
                if (status === '处理中') {
                    statusBadge.classList.add('processing');
                } else if (status === '已完成') {
                    statusBadge.classList.add('success');
                } else if (status === '已取消') {
                    statusBadge.classList.add('warning');
                } else if (status.startsWith('失败')) {
                    statusBadge.classList.add('error');
                } else {
                    statusBadge.classList.add('default');
                }
            }

            // 如果有错误消息，确保它们可见
            const errorMessages = DataManager.get('task.errorMessages', []);
            if (errorMessages && errorMessages.length > 0) {
                console.log('显示错误消息:', errorMessages);
            }
        }, 50);
    }

    // AMIS页面配置
    const amisJSON = {
        type: "page",
        title: "导出任务进度",
        className: "export-task-page p-3",
        data: {
            // 初始数据
            isLoading: true,
            hasError: false,
            errorMessage: '',
            fileName: '',
            status: '加载中',
            createTime: '',
            completionTime: '',
            progress: 0,
            processedRecords: 0,
            totalRecords: 0,
            fileUrl: '',
            updateTime: '',
            errorMessages: [],
            token: getToken()
        },
        body: [
            {
                type: "container",
                className: "export-task-container mb-4 p-0",
                style: {
                    maxWidth: "850px",
                    margin: "2rem auto"
                },
                body: [
                    {
                        type: "tpl",
                        tpl: "<h3 class='mb-4'><i class='fa fa-file-export me-2'></i>导出任务</h3>",
                        className: "mb-3 text-center"
                    },
                    {
                        type: "spinner",
                        overlay: false,
                        className: "text-center py-5",
                        size: "lg",
                        visibleOn: "isLoading"
                    },
                    {
                        type: "alert",
                        level: "danger",
                        className: "mb-4",
                        showIcon: true,
                        visibleOn: "${hasError}",
                        body: "找不到任务信息，任务可能已过期或不存在。${errorMessage}"
                    },
                    {
                        type: "alert",
                        level: "info",
                        className: "mb-4 export-description",
                        showIcon: true,
                        body: "<div class='export-info-text'><p><strong>导出说明：</strong>系统将排队生成导出文件，并打包为压缩包以供下载。大量数据导出可能需要较长时间，请耐心等待。</p><p><strong>生成完成后，请在半小时内完成下载，否则文件将会被清理。</p></div>",
                        visibleOn: "!isLoading && !hasError"
                    },
                    {
                        type: "card",
                        className: "shadow-sm task-card",
                        visibleOn: "!isLoading && !hasError",
                        header: {
                            className: "border-bottom task-card-header",
                            title: {
                                type: "tpl",
                                tpl: "<div class='d-flex justify-content-between align-items-center'><div><span class='h5 mb-0 task-file-name'><i class='fa fa-file-archive me-2 text-primary'></i>${fileName}</span></div><div><span class='badge status-badge ${status === \"处理中\" ? \"processing\" : (status === \"已完成\" ? \"success\" : (status === \"已取消\" ? \"warning\" : (status.startsWith(\"失败\") ? \"error\" : \"default\")))}'><i class='fa fa-${status === \"处理中\" ? \"sync fa-spin\" : (status === \"已完成\" ? \"check\" : (status === \"已取消\" ? \"ban\" : (status.startsWith(\"失败\") ? \"times\" : \"circle\")))} me-1'></i>${status}</span></div></div>"
                            }
                        },
                        body: [
                            {
                                type: "grid",
                                className: "mb-4 task-info-grid",
                                columns: [
                                    {
                                        md: 6,
                                        body: {
                                            type: "panel",
                                            title: "任务详情",
                                            className: "border-0 task-info-panel info-card",
                                            titleClassName: "fs-6 mb-3 text-primary fw-bold border-bottom pb-2",
                                            bodyClassName: "py-2 info-card-body",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item file-name-item'><i class='fa fa-file-alt text-primary me-2'></i><label>文件名称：</label><span class='file-name-text'>${fileName}</span></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item'><i class='fa fa-calendar-alt text-primary me-2'></i><label>创建时间：</label><span>${createTime || '未知'}</span></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item'><i class='fa fa-clock text-primary me-2'></i><label>更新时间：</label><span>${updateTime || '未知'}</span></div>",
                                                    visibleOn: "${updateTime}"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item'><i class='fa fa-check-circle text-primary me-2'></i><label>完成时间：</label><span>${completionTime || '未完成'}</span></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item'><i class='fa fa-tasks text-primary me-2'></i><label>任务ID：</label><span class='text-monospace'>${taskId}</span></div>"
                                                },
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='info-item records-info'><i class='fa fa-database text-primary me-2'></i><label>处理记录：</label><span class='counter-value'>${processedRecords}</span> / <span class='counter-total'>${totalRecords}</span></div>"
                                                }
                                            ]
                                        }
                                    },
                                    {
                                        md: 6,
                                        body: {
                                            type: "panel",
                                            title: "任务进度",
                                            className: "border-0 task-info-panel progress-panel",
                                            titleClassName: "fs-6 mb-3 text-primary fw-bold border-bottom pb-2",
                                            body: [
                                                {
                                                    type: "tpl",
                                                    tpl: "<div class='circular-progress-container'><div class='circular-progress' style='background: conic-gradient(${status === \"处理中\" ? \"#2196F3\" : (status === \"已完成\" ? \"#4CAF50\" : (status === \"已取消\" ? \"#FF9800\" : (status.startsWith(\"失败\") ? \"#F44336\" : \"#9E9E9E\")))} ${progress}%, #EEEEEE 0%);'><div class='circular-progress-inner'><span class='progress-value'>${progress}%</span></div></div></div>"
                                                }
                                            ]
                                        }
                                    }
                                ]
                            },
                            {
                                type: "container",
                                className: "task-error-messages",
                                visibleOn: "${errorMessages && errorMessages.length > 0}",
                                body: [
                                    {
                                        type: "panel",
                                        title: "错误信息",
                                        headerClassName: "bg-danger text-white",
                                        bodyClassName: "p-0",
                                        className: "mb-3 shadow-sm",
                                        body: {
                                            type: "each",
                                            name: "errorMessages",
                                            items: {
                                                type: "alert",
                                                level: "danger",
                                                showIcon: true,
                                                className: "m-2 border-0",
                                                body: "${item}"
                                            }
                                        }
                                    }
                                ]
                            },
                            {
                                type: "container",
                                className: "text-center mt-4 file-download-container",
                                visibleOn: "fileUrl && (status === '已完成' || status === '已取消')",
                                body: [
                                    {
                                        type: "tpl",
                                        tpl: "<div class='success-icon mb-3'><i class='fa fa-check-circle text-success'></i></div>"
                                    },
                                    {
                                        type: "button",
                                        label: "下载文件",
                                        level: "primary",
                                        className: "mb-3 download-button",
                                        iconClassName: "fa fa-download",
                                        actionType: "download",
                                        api: "${fileUrl}",
                                        visibleOn: "fileUrl && status === '已完成'"
                                    }
                                ]
                            }
                        ],
                        footer: {
                            className: "text-center border-top bg-light card-footer",
                            body: [
                                {
                                    type: "button",
                                    label: "返回",
                                    level: "secondary",
                                    className: "mt-2 back-button",
                                    iconClassName: "fa fa-arrow-left me-1",
                                    onEvent: {
                                        click: {
                                            actions: [
                                                {
                                                    actionType: "url",
                                                    url: "/exam/examRecords",
                                                    expression: ""
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
        ]
    };

    // 初始化应用
    window.addEventListener('load', function () {
        console.log("页面加载完成，初始化导出任务进度应用");

        try {
            // 初始化TokenManager为租户模式
            initializeTokenManager();
            
            // 检查Token有效性
            if (!isTokenValid()) {
                ErrorHandler.handleApiError("认证失败", "请先登录后再访问此页面");
                return;
            }

            // 获取任务ID
            taskId = getTaskIdFromUrl();
            console.log("获取到任务ID:", taskId);

            if (!taskId) {
                ErrorHandler.handleApiError("参数错误", "无法获取任务ID");
                return;
            }

            // 初始化AMIS应用
            const amisApp = amisRequire('amis/embed');

            // 配置AMIS实例
            const app = amisApp.embed('#export-task-app', amisJSON, {
                locale: 'zh-CN',
                theme: 'antd',
                data: DataManager.initialData
            }, {
                requestAdaptor: requestAdaptor,
                responseAdaptor: function (api, payload, query, request, response) {
                    if (response.status >= 400) {
                        // 401错误特殊处理
                        if (response.status === 401) {
                            ErrorHandler.showError("认证失败，请重新登录");
                            // 跳转到租户登录页面
                            setTimeout(() => {
                                const loginUrl = getLoginUrl();
                                if (loginUrl) {
                                    window.location.href = loginUrl;
                                }
                            }, 2000);
                        } else {
                            ErrorHandler.showError(`请求失败: ${response.status} ${response.statusText}`);
                        }
                        return payload;
                    }
                    return payload;
                }
            });

            amisInstance = app;
            console.log("AMIS应用初始化完成");

            // 加载任务信息
            loadTaskInfo(taskId);
        } catch (error) {
            console.error("初始化应用失败:", error);
            ErrorHandler.showError("初始化应用失败: " + error.message);
        }
    });

    // 页面卸载时清理
    window.addEventListener('unload', function () {
        stopPolling();
    });

    // 暴露给全局的函数
    window.ExportTaskApp = {
        refresh: function () {
            loadTaskInfo(taskId);
        },
        stopPolling: stopPolling,
        getTokenInfo: function() {
            return {
                hasToken: TokenManager.hasToken(),
                isAuthenticated: TokenManager.isAuthenticated(),
                platformType: TokenManager.platformType,
                tenantId: TokenManager.currentTenantId
            };
        }
    };
})(); 