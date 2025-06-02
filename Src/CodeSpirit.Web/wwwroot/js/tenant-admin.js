/**
 * 租户后台管理系统前端入口
 * 基于AMIS框架构建的租户后台UI系统
 */
(function () {
    // 基础依赖
    const amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    const history = History.createHashHistory();

    // 获取租户ID
    const tenantId = window.tenantId;
    if (!tenantId) {
        console.error('租户ID不存在');
        window.location.href = '/login';
        return;
    }

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
                            label: '${userName || email || "用户"}',
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
                                    actionType: 'custom',
                                    script: 'window.logout();'
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    };

    /**
     * 路由工具函数
     */
    const routerUtils = {
        normalizeLink: function(to, location = history.location) {
            to = to || '';
            
            // 为租户路径添加租户ID前缀
            if (to && !to.startsWith('http') && !to.startsWith('/') && !to.startsWith('#')) {
                to = `/${tenantId}/${to}`;
            } else if (to && to.startsWith('/') && !to.startsWith(`/${tenantId}/`) && !to.startsWith('/api/')) {
                to = `/${tenantId}${to}`;
            }
            
            if (to && to[0] === '#') {
                to = location.pathname + location.search + to;
            } else if (to && to[0] === '?') {
                to = location.pathname + to;
            }

            const idx = to.indexOf('?');
            const idx2 = to.indexOf('#');
            let pathname = ~idx ? to.substring(0, idx) : ~idx2 ? to.substring(0, idx2) : to;
            let search = ~idx ? to.substring(idx, ~idx2 ? idx2 : undefined) : '';
            let hash = ~idx2 ? to.substring(idx2) : location.hash;

            if (!pathname) {
                pathname = location.pathname;
            } else if (pathname[0] != '/' && !/^https?\:\/\//.test(pathname)) {
                let relativeBase = location.pathname;
                const paths = relativeBase.split('/');
                paths.pop();
                let m;
                while ((m = /^\.\.?\//.exec(pathname))) {
                    if (m[0] === '../') {
                        paths.pop();
                    }
                    pathname = pathname.substring(m[0].length);
                }
                pathname = paths.concat(pathname).join('/');
            }

            return pathname + search + hash;
        },

        isCurrentUrl: function(to, ctx) {
            if (!to) {
                return false;
            }

            const pathname = history.location.pathname;
            const link = this.normalizeLink(to, {
                ...location,
                pathname,
                hash: ''
            });

            if (!~link.indexOf('http') && ~link.indexOf(':')) {
                let strict = ctx && ctx.strict;
                return match(link, {
                    decode: decodeURIComponent,
                    strict: typeof strict !== 'undefined' ? strict : true
                })(pathname);
            }

            return decodeURI(pathname) === link;
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
            location = routerUtils.normalizeLink(location);
            if (location === 'goBack') {
                return history.goBack();
            } else if (
                (!/^https?\:\/\//.test(location) &&
                    location ===
                    history.location.pathname + history.location.search) ||
                location === history.location.href
            ) {
                return;
            } else if (/^https?\:\/\//.test(location) || !history) {
                return (window.location.href = location);
            }

            history[replace ? 'replace' : 'push'](location);
        },
        
        jumpTo: (to, action) => {
            if (to === 'goBack') {
                return history.goBack();
            }

            to = routerUtils.normalizeLink(to);

            if (routerUtils.isCurrentUrl(to)) {
                return;
            }

            // 特殊路径处理
            if (to.startsWith('/login') || to.startsWith('/notifications') || to.startsWith('/chat')) {
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
            } else if (
                (!/^https?\:\/\//.test(to) &&
                    to === history.pathname + history.location.search) ||
                to === history.location.href
            ) {
                // do nothing
            } else {
                history.push(to);
            }
        },
        
        isCurrentUrl: (to, ctx) => routerUtils.isCurrentUrl(to, ctx),
        
        requestAdaptor: (api) => {
            const token = localStorage.getItem('token');
            return {
                ...api,
                headers: {
                    ...api.headers,
                    'Authorization': 'Bearer ' + token,
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
     * 获取租户信息
     */
    window.fetchTenantInfo = function () {
        return new Promise((resolve, reject) => {
            const token = localStorage.getItem('token');                
            fetch(`/identity/api/identity/tenants/${tenantId}/login-config`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'X-Forwarded-With': 'CodeSpirit',
                    'X-Tenant-Id': tenantId
                }
            })
                .then(response => {
                    if (!response.ok) {
                        console.error('获取租户信息失败:', response);
                        return null;
                    }
                    return response.json();
                })
                .then(data => {
                    if (data && data.status === 0 && data.data) {
                        const tenant = data.data;
                        
                        // 更新全局租户数据
                        window.GlobalData.set('tenant.id', tenant.tenantId);
                        window.GlobalData.set('tenant.name', tenant.name);
                        window.GlobalData.set('tenant.displayName', tenant.displayName);
                        window.GlobalData.set('tenant.description', tenant.description);
                        window.GlobalData.set('tenant.logoUrl', tenant.logoUrl || '/favicon.ico');
                        window.GlobalData.set('tenant.themeConfig', tenant.themeConfig);
                        
                        // 更新app配置
                        appConfig.brandName = tenant.displayName || tenant.name || '租户管理后台';
                        appConfig.logo = tenant.logoUrl || '/favicon.ico';
                        
                        // 更新amisOptions中的租户数据
                        amisOptions.data.tenant = {
                            id: tenant.tenantId,
                            name: tenant.name,
                            displayName: tenant.displayName,
                            description: tenant.description,
                            logoUrl: tenant.logoUrl || '/favicon.ico',
                            themeConfig: tenant.themeConfig
                        };
                        
                        // 更新页面标题
                        document.title = (tenant.displayName || tenant.name || '租户管理后台') + ' - CodeSpirit';
                        
                        // 应用租户主题配置
                        if (tenant.themeConfig) {
                            applyTenantTheme(tenant.themeConfig);
                        }
                        
                        console.log('租户信息已获取:', tenant);
                        resolve(tenant);
                    } else {
                        console.warn('租户信息获取失败或数据为空');
                        resolve(null);
                    }
                })
                .catch(error => {
                    console.error('获取租户信息异常:', error);
                    reject(error);
                });
        });
    };

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

    window.fetchUnreadNotificationCount = function () {
        const token = localStorage.getItem('token');                
        fetch(`/messaging/api/messaging/messages/my/unread/count`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'X-Forwarded-With': 'CodeSpirit',
                'X-Tenant-Id': tenantId
            }
        })
            .then(response => {
                if (!response.ok) {
                    console.error('获取未读消息数失败:', response);
                    return null;
                }
                return response.json();
            })
            .then(data => {
                if (data) {
                    const count = data.count || data.unreadCount || 0;
                    window.updateNotificationCount(count);
                }
            })
            .catch(error => {
                console.error('获取未读消息数失败:', error);
            });
    };

    /**
     * 退出登录
     */
    window.logout = function() {
        const token = localStorage.getItem('token');
        
        // 调用登出API
        fetch('/identity/api/identity/auth/logout', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json',
                'X-Forwarded-With': 'CodeSpirit',
                'X-Tenant-Id': tenantId
            }
        })
        .then(() => {
            // 清除本地存储
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            
            // 重定向到登录页
            window.location.href = '/login';
        })
        .catch(error => {
            console.error('退出登录失败:', error);
            // 即使API失败也清除本地存储并跳转
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = '/login';
        });
    };

    /**
     * 获取未读通知数量
     */
    window.fetchUnreadNotificationCount = function() {
        const token = localStorage.getItem('token');
        
        fetch('/messaging/api/messaging/messages/my/unread-count', {
            headers: {
                'Authorization': `Bearer ${token}`,
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
     * 初始化应用
     */
    async function initializeApp() {
        try {
            // 显示加载提示
            const rootElement = document.getElementById('root');
            if (rootElement) {
                rootElement.innerHTML = '<div style="display: flex; justify-content: center; align-items: center; height: 100vh; font-size: 16px;">正在加载租户信息...</div>';
            }
            
            // 先获取租户信息
            await window.fetchTenantInfo();
            
            // 清空加载提示
            if (rootElement) {
                rootElement.innerHTML = '';
            }
            
            // 使用更新后的配置初始化AMIS实例
            window.amisInstance = amis.embed('#root', appConfig, amisOptions, amisHandlers);
            
            // 绑定路由监听
            window.bindRouteListener();
            
            console.log('应用初始化完成');
            
        } catch (error) {
            console.error('应用初始化失败:', error);
            
            // 即使失败也要初始化基本界面
            const rootElement = document.getElementById('root');
            if (rootElement) {
                rootElement.innerHTML = '';
            }
            
            window.amisInstance = amis.embed('#root', appConfig, amisOptions, amisHandlers);
            window.bindRouteListener();
        }
    }

    // 路由监听器变量
    let routeUnlisten = null;

    // 绑定路由监听函数
    window.bindRouteListener = function() {
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
    document.addEventListener('DOMContentLoaded', function() {
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