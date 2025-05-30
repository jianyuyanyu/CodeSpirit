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
    const appConfig = {
        type: 'app',
        brandName: '租户管理后台',
        logo: '/favicon.ico', // 初始默认logo，后续通过API更新
        api: `/api/navigation/tenant?tenantId=${tenantId}`, // 获取租户平台的导航
        header: {
            type: 'service',
            api: '/identity/api/identity/profile',
            silentPolling: false,
            className: 'flex w-full justify-between items-center px-4',
            onEvent: {
                fetchInited: {
                    actions: [
                        {
                            actionType: "custom",
                            script: `
                                // 获取租户信息并更新品牌
                                window.fetchTenantInfo();
                                // 获取未读通知数
                                window.fetchUnreadNotificationCount();
                                
                                // 设置定时任务，每分钟更新一次未读通知数
                                window.notificationTimer = setInterval(function() {
                                    window.fetchUnreadNotificationCount();
                                }, 60000);
                            `
                        }
                    ]
                }
            },
            body: [
                // 左侧：租户品牌信息（更显眼的展示）
                {
                    type: 'flex',
                    className: 'tenant-header-info mr-auto',
                    items: [
                        {
                            type: 'container',
                            className: 'tenant-brand-container flex items-center',
                            body: [
                                {
                                    type: 'image',
                                    name: 'tenant.logoUrl',
                                    className: 'tenant-logo rounded-full',
                                    width: 40,
                                    height: 40,
                                    defaultImage: '/favicon.ico'
                                },
                                {
                                    type: 'container',
                                    className: 'tenant-info ml-3',
                                    body: [
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="tenant-name">${tenant.displayName || tenant.name || "租户后台"}</div>',
                                            className: 'mb-1'
                                        },
                                        {
                                            type: 'tpl',
                                            tpl: '<div class="tenant-id">租户ID: ${tenant.id}</div>'
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                // 右侧：通知和用户菜单
                {
                    type: 'flex',
                    className: 'header-actions ml-auto flex items-center',
                    items: [
                        // 通知按钮
                        {
                            type: 'button',
                            icon: 'fa fa-bell',
                            className: 'mr-3 notification-btn',
                            tooltip: '通知',
                            level: 'link',
                            badge: {
                                mode: 'text',
                                text: '${notifications.count}',
                                position: 'top-left',
                                visibleOn: 'this.notifications.hasUnread',
                                level: 'danger'
                            },
                            actionType: 'dialog',
                            dialog: {
                                title: '我的通知',
                                size: 'md',
                                body: {
                                    type: 'service',
                                    api: {
                                        url: '/messaging/api/messaging/messages/my/list',
                                        method: 'GET'
                                    },
                                    body: [
                                        {
                                            type: 'list',
                                            source: '${items}',
                                            listItem: {
                                                title: '${title}',
                                                desc: '<span class="text-base">${content}</span>',
                                                actions: [
                                                    {
                                                        type: 'button',
                                                        icon: 'fa fa-times',
                                                        tooltip: '删除通知',
                                                        actionType: 'ajax',
                                                        api: 'DELETE:/messaging/api/messaging/messages/my/${id}',
                                                        confirmText: '确定要删除该通知吗？'
                                                    },
                                                    {
                                                        type: 'button',
                                                        icon: 'fa fa-check',
                                                        tooltip: '标记为已读',
                                                        actionType: 'ajax',
                                                        api: 'POST:/messaging/api/messaging/messages/my/${id}/read'
                                                    }
                                                ]
                                            },
                                            placeholder: '暂无通知',
                                            footer: [
                                                {
                                                    type: 'button',
                                                    label: '全部标记为已读',
                                                    level: 'primary',
                                                    size: 'sm',
                                                    actionType: 'ajax',
                                                    api: 'POST:/messaging/api/messaging/messages/my/read/all',
                                                    reload: 'window'
                                                }
                                            ]
                                        }
                                    ]
                                }
                            }
                        },
                        // 用户头像
                        {
                            type: 'avatar',
                            src: '${user.avatar}',
                            text: '${user.name}',
                            icon: 'fa fa-user',
                            className: 'mr-3',
                            size: 40,
                            onError: function () {
                                return true;
                            }
                        },
                        // 用户菜单
                        {
                            type: 'dropdown-button',
                            label: '${user.name}',
                            align: 'right',
                            className: 'ml-2',
                            buttons: [
                                {
                                    type: 'button',
                                    label: '个人信息',
                                    icon: 'fa fa-address-card',
                                    actionType: 'dialog',
                                    dialog: {
                                        title: '个人信息',
                                        size: 'md',
                                        body: {
                                            type: 'form',
                                            api: '/identity/api/identity/profile',
                                            controls: [
                                                {
                                                    type: 'image',
                                                    name: 'avatar',
                                                    label: '头像',
                                                    thumbMode: 'cover',
                                                    thumbRatio: '1:1',
                                                    width: 100,
                                                    height: 100,
                                                    className: 'rounded-full'
                                                },
                                                {
                                                    type: 'static',
                                                    name: 'userName',
                                                    label: '用户名'
                                                },
                                                {
                                                    type: 'static',
                                                    name: 'email',
                                                    label: '邮箱'
                                                }
                                            ]
                                        }
                                    }
                                },
                                {
                                    type: 'divider'
                                },
                                {
                                    type: 'button',
                                    label: '租户设置',
                                    icon: 'fa fa-cog',
                                    level: 'info',
                                    actionType: 'dialog',
                                    dialog: {
                                        title: '租户设置',
                                        size: 'lg',
                                        body: {
                                            type: 'service',
                                            api: `/identity/api/identity/tenants/${tenantId}/login-config`,
                                            body: [
                                                {
                                                    type: 'form',
                                                    title: '租户基本信息',
                                                    controls: [
                                                        {
                                                            type: 'static',
                                                            name: 'id',
                                                            label: '租户ID'
                                                        },
                                                        {
                                                            type: 'static',
                                                            name: 'name',
                                                            label: '租户名称'
                                                        },
                                                        {
                                                            type: 'static',
                                                            name: 'displayName',
                                                            label: '显示名称'
                                                        },
                                                        {
                                                            type: 'image',
                                                            name: 'logoUrl',
                                                            label: '租户Logo',
                                                            width: 100,
                                                            height: 100
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    }
                                },
                                {
                                    type: 'button',
                                    label: '切换到系统管理',
                                    icon: 'fa fa-exchange-alt',
                                    level: 'info',
                                    actionType: 'url',
                                    url: '/admin',
                                    visibleOn: 'this.user.roles && this.user.roles.indexOf("Admin") !== -1'
                                },
                                {
                                    type: 'button',
                                    label: '退出登录',
                                    icon: 'fa fa-sign-out',
                                    level: 'danger',
                                    actionType: 'ajax',
                                    confirmText: '确认要退出登录？',
                                    api: '/identity/api/identity/auth/logout',
                                    reload: 'none',
                                    redirect: '/login'
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
    const amisOptions = {
        location: history.location,
        data: {
            tenant: {
                id: tenantId,
                name: '加载中...',
                displayName: '加载中...',
                logoUrl: '/favicon.ico'
            }
        },
        context: {
            WEB_HOST: webHost,
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

    // 初始化AMIS实例
    let amisInstance = amis.embed('#root', appConfig, amisOptions, amisHandlers);

    // 初始化数据
    amisInstance.updateProps({
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
        }
    });

    // 监听路由变化
    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });

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

    /**
     * 获取租户信息
     */
    window.fetchTenantInfo = function () {
        const token = localStorage.getItem('token');                
        fetch(`/identity/api/tenants/${tenantId}/login-config`, {
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
                    
                    // 更新全局数据
                    window.GlobalData.set('tenant.name', tenant.name || '');
                    window.GlobalData.set('tenant.displayName', tenant.displayName || tenant.name || '');
                    window.GlobalData.set('tenant.logoUrl', tenant.logoUrl || '/favicon.ico');
                    window.GlobalData.set('tenant.themeConfig', tenant.themeConfig || {});

                    // 更新AMIS实例中的数据
                    amisInstance.updateProps({
                        data: {
                            tenant: {
                                id: tenantId,
                                name: tenant.name || '',
                                displayName: tenant.displayName || tenant.name || '',
                                logoUrl: tenant.logoUrl || '/favicon.ico'
                            }
                        }
                    });

                    // 更新应用品牌名称
                    if (tenant.displayName || tenant.name) {
                        document.title = `${tenant.displayName || tenant.name} - 管理后台`;
                    }

                    // 应用租户主题
                    if (tenant.themeConfig) {
                        applyTenantTheme(tenant.themeConfig);
                    }
                }
            })
            .catch(error => {
                console.error('获取租户信息失败:', error);
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
})(); 