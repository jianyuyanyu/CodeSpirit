/**
 * 租户后台管理系统前端入口
 * 基于AMIS框架构建的租户后台UI系统
 * 增强功能：预加载器、错误处理
 */
(function () {
    // 获取租户ID
    const tenantId = window.tenantId;
    if (!tenantId) {
        console.error('租户ID不存在');
        window.location.href = '/login';
        return;
    }

    // 将租户ID暴露到全局，供脚本使用
    window.currentTenantId = tenantId;
    console.log('租户管理后台初始化 - 租户ID:', tenantId);

    // 初始化为租户模式
    TokenManager.initTenantMode(tenantId);

    // 基础依赖
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    /**
     * 全局数据存储（租户相关）
     */
    window.globalData = {
        tenant: {
            id: tenantId,
            name: '',
            displayName: '',
            logoUrl: '',
            themeConfig: {}
        },
        user: {
            id: null,
            name: '',
            avatar: '',
            roles: []
        },
        notifications: {
            count: 0,
            hasUnread: false,
            items: []
        },
        settings: {},
        permissions: []
    };

    /**
     * 租户应用配置
     */
    let appConfig = {
        type: 'app',
        brandName: '租户管理后台',
        logo: '/favicon.ico',
        api: `/api/navigation/tenant?tenantId=${tenantId}`, // 获取租户平台的导航
        header: {
            type: 'service',
            api: '/identity/api/identity/profile',
            silentPolling: false,
            className: 'flex w-full justify-between items-center px-4',
            body: [
                {
                    type: 'flex',
                    justify: 'space-between',
                    alignItems: 'center',
                    className: 'w-full',
                    items: [
                        {
                            type: 'container',
                            className: 'flex-1',
                            body: []
                        },
                        {
                            type: 'dropdown-button',
                            label: '${name || userName || email || "用户"}',
                            icon: 'fa fa-user',
                            trigger: 'click',
                            closeOnClick: true,
                            buttons: [
                                {
                                    type: 'button',
                                    label: '个人设置',
                                    icon: 'fa fa-cog',
                                    actionType: 'url',
                                    url: '/profile'
                                },
                                {
                                    type: 'divider'
                                },
                                {
                                    type: 'button',
                                    label: '退出登录',
                                    icon: 'fa fa-sign-out-alt',
                                    level: 'danger',
                                    actionType: 'custom',
                                    confirmText: '确认要退出登录？',
                                    onEvent: {
                                        click: {
                                            actions: [
                                                {
                                                    "actionType": "custom",
                                                    "script": "window.logout();"
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
    };

    /**
     * AMIS实例配置
     */
    let amisOptions = {
        location: history.location,
        data: {
            tenant: {
                id: tenantId,
                name: '加载中...',
                displayName: '加载中...',
                logoUrl: '/favicon.ico'
            },
            notifications: {
                count: 0,
                hasUnread: false
            }
        },
        context: {
            WEB_HOST: window.webHost || '',
            TENANT_ID: tenantId,
            PLATFORM_TYPE: 'tenant'
        }
    };

    /**
     * AMIS事件处理器
     */
    const amisHandlers = {
        updateLocation: (location, replace) => {
            if (location === 'goBack') {
                return history.goBack();
            } else if (/^https?\:\/\//.test(location) || !history) {
                return (window.location.href = location);
            }

            history[replace ? 'replace' : 'push'](location);
        },

        jumpTo: (to, action) => {
            if (to === 'goBack') {
                return history.goBack();
            }

            // 特殊路径处理
            if (to.startsWith('/impersonate') || to.startsWith('/login') || to.startsWith('/notifications') || to.startsWith('/chat') || to.endsWith("/login")) {
                window.location.href = to;
                return;
            }

            if (action && action.actionType === 'url') {
                action.blank === false
                    ? (window.location.href = to)
                    : window.open(to, '_blank');
                return;
            } else if (action && action.blank) {
                window.open(to, '_blank');
                return;
            }

            if (/^https?:\/\//.test(to)) {
                window.location.href = to;
            } else {
                history.push(to);
            }
        },

        isCurrentUrl: (to, ctx) => {
            if (!to) {
                return false;
            }

            const pathname = history.location.pathname;
            const link = to;

            if (!~link.indexOf('http') && ~link.indexOf(':')) {
                let strict = ctx && ctx.strict;
                return match(link, {
                    decode: decodeURIComponent,
                    strict: typeof strict !== 'undefined' ? strict : true
                })(pathname);
            }

            return decodeURI(pathname) === link;
        },

        requestAdaptor: (api) => {
            const token = TokenManager.getToken();
            return {
                ...api,
                headers: {
                    ...api.headers,
                    'Authorization': token ? 'Bearer ' + token : '',
                    'X-Forwarded-With': 'CodeSpirit',
                    'X-Tenant-Id': tenantId // 添加租户ID到请求头
                }
            };
        },

        responseAdaptor: function (api, payload, query, request, response) {
            // 处理错误响应
            if (response.status === 403) {
                return { msg: '您没有权限访问此页面，请联系管理员！' }
            }
            else if (response.status === 401) {
                // 获取当前路径作为重定向参数
                const currentPath = encodeURIComponent(window.location.hash || window.location.pathname);
                window.location.href = `/${tenantId}/login?redirect=${currentPath}`;
                return { msg: '登录过期！' };
            }

            // 处理 API 响应中的跳转信息
            if (payload && payload.redirect && payload.redirect.url) {
                handleApiRedirect(payload.redirect);
            }

            // 如果是获取用户信息的接口,将数据注入到全局
            if (api.url.includes('/identity/api/identity/profile')) {
                if (payload.status === 0 && payload.data) {
                    window.GlobalData.set('user.id', payload.data.id || null);
                    window.GlobalData.set('user.name', payload.data.name || payload.data.userName || '');
                    window.GlobalData.set('user.avatar', payload.data.avatar || '');
                    window.GlobalData.set('user.roles', payload.data.roles || []);

                    // 同时注入到amis全局上下文，使所有组件都能访问
                    window.GlobalData.syncToAmis(amisInstance);

                    console.debug('Global user data updated:', window.globalData.user);
                }
            }

            return payload;
        },

        theme: 'antd'
    };

    /**
     * 错误处理器 - 全局错误捕获和处理
     */
    class ErrorHandler {
        constructor() {
            this.errors = [];
            this.maxErrors = 50;
            this.init();
        }

        init() {
            // 捕获JS运行时错误
            window.addEventListener('error', (event) => {
                this.handleError({
                    type: 'javascript',
                    message: event.message || '未知JavaScript错误',
                    filename: event.filename || '未知文件',
                    lineno: event.lineno || 0,
                    colno: event.colno || 0,
                    stack: event.error?.stack || '无堆栈信息',
                    timestamp: Date.now(),
                    url: window.location.href
                });
            });

            // 捕获Promise未处理的错误
            window.addEventListener('unhandledrejection', (event) => {
                const reason = event.reason;
                this.handleError({
                    type: 'promise',
                    message: reason?.message || reason?.toString() || '未知Promise错误',
                    stack: reason?.stack || '无堆栈信息',
                    timestamp: Date.now(),
                    url: window.location.href,
                    promise: true
                });
            });

            // 捕获资源加载错误
            window.addEventListener('error', (event) => {
                if (event.target !== window) {
                    this.handleError({
                        type: 'resource',
                        message: `资源加载失败: ${event.target.src || event.target.href || '未知资源'}`,
                        element: event.target.tagName || '未知元素',
                        timestamp: Date.now(),
                        url: window.location.href
                    });
                }
            }, true);
        }

        handleError(error) {
            // 提供更详细的错误信息
            const errorDetails = {
                ...error,
                userAgent: navigator.userAgent,
                timestamp: error.timestamp || Date.now(),
                id: `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
            };

            console.error('错误捕获详情:', errorDetails);

            this.errors.unshift(errorDetails);
            if (this.errors.length > this.maxErrors) {
                this.errors = this.errors.slice(0, this.maxErrors);
            }

            // 显示友好的错误提示
            this.showErrorNotification(errorDetails);

            // 对于严重错误，显示错误页面
            if (this.isCriticalError(errorDetails)) {
                this.showErrorPage(errorDetails);
            }
        }

        isCriticalError(error) {
            // 判断是否为严重错误
            const criticalMessages = [
                'Script error',
                'Network Error',
                'ChunkLoadError',
                'Loading chunk',
                'Loading CSS chunk'
            ];

            return error.type === 'javascript' &&
                criticalMessages.some(msg =>
                    (error.message || '').includes(msg)
                );
        }

        showErrorNotification(error) {
            if (window.amisInstance) {
                const shortMessage = error.message.length > 100
                    ? error.message.substring(0, 100) + '...'
                    : error.message;

                window.amisInstance.notify('error', `发生错误: ${shortMessage}`, {
                    timeout: 5000
                });
            }
        }

        showErrorPage(error) {
            const errorPageHtml = `
                <div class="error-page">
                    <div class="error-container">
                        <div class="error-icon">
                            <i class="fa fa-exclamation-triangle"></i>
                        </div>
                        <h2>页面遇到问题</h2>
                        <p>我们正在努力修复这个问题，请稍后重试。</p>
                        <div class="error-actions">
                            <button onclick="window.location.reload()" class="btn btn-primary">
                                刷新页面
                            </button>
                            <button onclick="window.history.back()" class="btn btn-secondary">
                                返回上页
                            </button>
                        </div>
                        <details class="error-details">
                            <summary>错误详情 (ID: ${error.id})</summary>
                            <pre>${JSON.stringify(error, null, 2)}</pre>
                        </details>
                    </div>
                </div>
            `;

            const rootElement = document.getElementById('root');
            if (rootElement) {
                rootElement.innerHTML = errorPageHtml;
            }
        }

        getErrorSummary() {
            return {
                total: this.errors.length,
                byType: this.errors.reduce((acc, error) => {
                    acc[error.type] = (acc[error.type] || 0) + 1;
                    return acc;
                }, {}),
                recent: this.errors.slice(0, 5).map(error => ({
                    id: error.id,
                    type: error.type,
                    message: error.message,
                    timestamp: new Date(error.timestamp).toLocaleString()
                }))
            };
        }
    }

    /**
     * 预加载器 - 美观的加载动画和进度指示
     */
    class PreLoader {
        constructor() {
            this.element = null;
            this.progress = 0;
            this.stages = [
                { name: '初始化应用', weight: 10 },
                { name: '获取租户信息', weight: 30 },
                { name: '加载用户配置', weight: 20 },
                { name: '渲染界面', weight: 25 },
                { name: '完成加载', weight: 15 }
            ];
            this.currentStageIndex = 0;
            this.create();
        }

        create() {
            this.element = document.createElement('div');
            this.element.className = 'tenant-preloader';
            this.element.innerHTML = `
                <div class="preloader-container">
                    <div class="preloader-content">
                        <div class="tenant-logo-loading">
                            <div class="logo-placeholder">
                                <i class="fa fa-building"></i>
                            </div>
                            <div class="loading-spinner"></div>
                        </div>
                        <h2 class="loading-title">加载中</h2>
                        <div class="progress-container">
                            <div class="progress-bar">
                                <div class="progress-fill"></div>
                            </div>
                            <div class="progress-text">
                                <span class="progress-stage">初始化中...</span>
                                <span class="progress-percent">0%</span>
                            </div>
                        </div>
                        <div class="loading-tips">
                            <div class="tip active">正在为您准备最佳的管理体验</div>
                        </div>
                    </div>
                </div>
            `;

            document.body.appendChild(this.element);
            this.animateIn();
        }

        animateIn() {
            requestAnimationFrame(() => {
                this.element.classList.add('show');
            });
        }

        setProgress(progress, stageName) {
            this.progress = Math.min(100, Math.max(0, progress));

            if (this.element) {
                const progressFill = this.element.querySelector('.progress-fill');
                const progressPercent = this.element.querySelector('.progress-percent');
                const progressStage = this.element.querySelector('.progress-stage');

                if (progressFill) {
                    progressFill.style.width = `${this.progress}%`;
                }
                if (progressPercent) {
                    progressPercent.textContent = `${Math.round(this.progress)}%`;
                }
                if (progressStage && stageName) {
                    progressStage.textContent = stageName;
                }
            }
        }

        nextStage() {
            if (this.currentStageIndex < this.stages.length) {
                const stage = this.stages[this.currentStageIndex];
                const baseProgress = this.stages.slice(0, this.currentStageIndex)
                    .reduce((sum, s) => sum + s.weight, 0);

                this.setProgress(baseProgress + stage.weight, stage.name);
                this.currentStageIndex++;
            }
        }

        hide() {
            return new Promise((resolve) => {
                if (!this.element) {
                    resolve();
                    return;
                }

                this.setProgress(100, '加载完成');

                setTimeout(() => {
                    this.element.classList.add('fade-out');

                    setTimeout(() => {
                        if (this.element && this.element.parentNode) {
                            this.element.parentNode.removeChild(this.element);
                        }
                        this.element = null;
                        resolve();
                    }, 500);
                }, 300);
            });
        }
    }

    // 初始化增强功能
    const errorHandler = new ErrorHandler();
    const preLoader = new PreLoader();

    // 暴露到全局，方便调试和访问
    window.errorHandler = errorHandler;

    // 全局错误处理包装器
    function withErrorHandling(fn, context = 'Unknown') {
        return async function (...args) {
            try {
                return await fn.apply(this, args);
            } catch (error) {
                errorHandler.handleError({
                    type: 'function',
                    message: error.message,
                    context: context,
                    stack: error.stack,
                    timestamp: Date.now()
                });
                throw error;
            }
        };
    }

    /**
     * 获取租户信息
     */
    window.fetchTenantInfo = withErrorHandling(async function () {
        const token = TokenManager.getToken();
        const response = await fetch(`/identity/api/identity/tenants/${tenantId}/login-config`, {
            headers: {
                'Authorization': token ? `Bearer ${token}` : '',
                'X-Forwarded-With': 'CodeSpirit',
                'X-Tenant-Id': tenantId
            }
        });

        if (!response.ok) {
            throw new Error(`获取租户信息失败: ${response.status} ${response.statusText}`);
        }

        const result = await response.json();

        if (result.status !== 0) {
            throw new Error(result.message || '获取租户信息失败');
        }

        const tenantInfo = result.data;

        // 更新全局数据
        window.globalData.tenant = {
            id: tenantInfo.id,
            name: tenantInfo.name,
            displayName: tenantInfo.displayName || tenantInfo.name,
            logoUrl: tenantInfo.logoUrl || '/favicon.ico',
            themeConfig: tenantInfo.themeConfig || {}
        };

        // 更新应用配置
        appConfig.brandName = tenantInfo.displayName || tenantInfo.name;
        appConfig.logo = tenantInfo.logoUrl || '/favicon.ico';

        // 更新AMIS数据
        amisOptions.data.tenant = window.globalData.tenant;

        // 应用租户主题
        if (tenantInfo.themeConfig) {
            applyTenantTheme(tenantInfo.themeConfig);
        }

        console.log('租户信息加载完成:', tenantInfo);
    }, 'fetchTenantInfo');

    /**
     * 应用租户主题
     * @param {string|Object} themeConfig 主题配置
     */
    function applyTenantTheme(themeConfig) {
        if (!themeConfig) return;

        try {
            const theme = typeof themeConfig === 'string' ? JSON.parse(themeConfig) : themeConfig;
            const root = document.documentElement;

            // 应用主题颜色
            if (theme.primaryColor) {
                root.style.setProperty('--tenant-primary-color', theme.primaryColor);
            }

            if (theme.backgroundColor) {
                root.style.setProperty('--tenant-bg-color', theme.backgroundColor);
            }

            if (theme.sidebarBg) {
                root.style.setProperty('--tenant-sidebar-bg', theme.sidebarBg);
            }

            if (theme.headerBg) {
                root.style.setProperty('--tenant-header-bg', theme.headerBg);
            }

            // 应用自定义CSS
            if (theme.customCss) {
                const style = document.createElement('style');
                style.id = 'tenant-admin-custom-style';
                style.textContent = theme.customCss;
                document.head.appendChild(style);
            }

        } catch (error) {
            console.warn('应用租户主题失败:', error);
        }
    }

    /**
     * 通知相关功能（继承系统版本）
     */
    window.updateNotificationCount = function (count) {
        window.GlobalData.set('notifications.count', count);
        window.GlobalData.set('notifications.hasUnread', count > 0);

        amisInstance.updateProps({
            data: {
                notifications: {
                    count: count,
                    hasUnread: count > 0
                }
            }
        });
    };

    /**
     * 处理 API 响应中的跳转信息
     * @param {Object} redirectInfo 跳转信息对象
     */
    window.handleApiRedirect = function (redirectInfo) {
        if (!redirectInfo || !redirectInfo.url) {
            console.warn('跳转信息无效:', redirectInfo);
            return;
        }

        const {
            url,
            type = 0, // 默认当前窗口跳转
            delay = 0,
            showMessage = true,
            message = '正在跳转...'
        } = redirectInfo;

        // 显示跳转提示
        if (showMessage && message && window.amisInstance) {
            window.amisInstance.doAction([
                {
                    actionType: 'toast',
                    args: {
                        msg: message,
                        timeout: Math.max(delay, 2000)
                    }
                }
            ]);
        }

        // 执行跳转
        const doRedirect = () => {
            try {
                switch (type) {
                    case 0: // Self - 当前窗口跳转
                        window.location.href = url;
                        break;
                    case 1: // Blank - 新窗口打开
                        window.open(url, '_blank');
                        break;
                    case 2: // Replace - 替换当前页面
                        window.location.replace(url);
                        break;
                    default:
                        console.warn('未知的跳转类型:', type);
                        window.location.href = url;
                }
            } catch (error) {
                console.error('跳转失败:', error);
                // 回退到默认跳转方式
                window.location.href = url;
            }
        };

        // 延迟跳转
        if (delay > 0) {
            setTimeout(doRedirect, delay);
        } else {
            doRedirect();
        }
    };

    /**
     * 租户退出登录
     */
    window.logout = function () {
        console.log('开始执行租户退出登录');

        // 获取租户ID
        const tenantId = window.currentTenantId || window.tenantId;
        const loginUrl = tenantId ? `/${tenantId}/login` : '/login';

        // 获取Token（在清除之前先获取用于API调用）
        const token = TokenManager.getToken();
        console.log('获取到Token:', !!token);

        // 调用退出API（必须携带Token）
        fetch('/identity/api/identity/auth/logout', {
            method: 'POST',
            headers: {
                'Authorization': token ? `Bearer ${token}` : '',
                'Content-Type': 'application/json',
                'X-Forwarded-With': 'CodeSpirit',
                'X-Tenant-Id': tenantId
            }
        })
            .then(response => {
                console.log('退出API响应:', response.status);
                if (response.ok) {
                    console.log('服务器退出成功');
                }
            })
            .catch(error => {
                console.error('退出API失败:', error);
            })
            .finally(() => {
                // 无论API成功还是失败，都清除本地Token并跳转
                try {
                    TokenManager.clearToken();
                    console.log('Token已清除');
                } catch (e) {
                    console.error('清除Token失败:', e);
                }

                console.log('跳转到登录页:', loginUrl);
                window.location.href = loginUrl;
            });
    };

    /**
     * 获取未读通知数量
     */
    window.fetchUnreadNotificationCount = function () {
        const token = TokenManager.getToken();

        fetch('/messaging/api/messaging/messages/my/unread-count', {
            headers: {
                'Authorization': token ? `Bearer ${token}` : '',
                'X-Forwarded-With': 'CodeSpirit',
                'X-Tenant-Id': tenantId
            }
        })
            .then(response => response.json())
            .then(data => {
                if (data && data.status === 0) {
                    const count = data.data || 0;

                    // 更新通知数据
                    if (window.amisInstance) {
                        window.amisInstance.updateProps({
                            data: {
                                notifications: {
                                    count: count,
                                    hasUnread: count > 0
                                }
                            }
                        });
                    }
                }
            })
            .catch(error => {
                console.error('获取未读通知数量失败:', error);
            });
    };

    /**
     * 初始化应用（增强版）
     */
    async function initializeApp() {
        try {
            preLoader.nextStage(); // 初始化应用

            // 先获取租户信息
            preLoader.nextStage(); // 获取租户信息
            await window.fetchTenantInfo();

            // 加载用户配置
            preLoader.nextStage(); // 加载用户配置

            // 渲染界面
            preLoader.nextStage(); // 渲染界面

            // 使用更新后的配置初始化AMIS实例
            window.amisInstance = amis.embed('#root', appConfig, amisOptions, amisHandlers);

            // 绑定路由监听
            window.bindRouteListener();

            // 完成加载
            preLoader.nextStage(); // 完成加载

            // 隐藏预加载器
            await preLoader.hide();

            console.log('应用初始化完成');

        } catch (error) {
            console.error('应用初始化失败:', error);

            errorHandler.handleError({
                type: 'initialization',
                message: error.message,
                stack: error.stack,
                timestamp: Date.now()
            });

            // 隐藏预加载器并显示错误页面
            await preLoader.hide();

            // 即使失败也要初始化基本界面
            try {
                window.amisInstance = amis.embed('#root', appConfig, amisOptions, amisHandlers);
                window.bindRouteListener();
            } catch (fallbackError) {
                errorHandler.showErrorPage({
                    type: 'critical',
                    message: '应用初始化完全失败',
                    originalError: error,
                    fallbackError: fallbackError,
                    timestamp: Date.now()
                });
            }
        }
    }

    // 路由监听器变量
    let routeUnlisten = null;

    // 绑定路由监听函数
    window.bindRouteListener = function () {
        // 如果已经有监听器，先解绑
        if (routeUnlisten) {
            routeUnlisten();
        }

        // 绑定新的监听器
        routeUnlisten = history.listen(state => {
            if (window.amisInstance) {
                window.amisInstance.updateProps({
                    location: state.location || state
                });
            }
        });
    };

    // 页面加载完成后初始化应用
    document.addEventListener('DOMContentLoaded', function () {
        initializeApp();
    });

    // 如果DOMContentLoaded已经触发，立即执行
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        initializeApp();
    }

    /**
     * 全局数据操作工具（继承系统版本）
     */
    window.GlobalData = window.GlobalData || {
        get: function (path, defaultValue) {
            const keys = path.split('.');
            let current = window.globalData;

            for (let i = 0; i < keys.length; i++) {
                if (current === undefined || current === null) {
                    return defaultValue;
                }
                current = current[keys[i]];
            }

            return current !== undefined ? current : defaultValue;
        },

        set: function (path, value) {
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
            if (!amisInstance) return;

            const data = {};
            if (selectedPaths && Array.isArray(selectedPaths)) {
                selectedPaths.forEach(path => {
                    const keys = path.split('.');
                    let current = data;
                    let source = window.globalData;

                    for (let i = 0; i < keys.length - 1; i++) {
                        if (source[keys[i]] === undefined) break;

                        if (current[keys[i]] === undefined) {
                            current[keys[i]] = {};
                        }
                        current = current[keys[i]];
                        source = source[keys[i]];
                    }

                    current[keys[keys.length - 1]] = source[keys[keys.length - 1]];
                });
            } else {
                Object.assign(data, window.globalData);
            }

            amisInstance.updateProps({ data });
        }
    };
})(); 