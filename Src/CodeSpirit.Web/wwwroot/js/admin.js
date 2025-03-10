(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    // 全局数据对象，用于存储用户信息和其他共享数据
    window.globalData = {
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
        // 可以添加其他全局数据
        settings: {},
        permissions: []
    };

    // 全局数据辅助函数
    window.GlobalData = {
        // 获取数据
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

        // 设置数据
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

        // 将全局数据同步到amis上下文
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

    const app = {
        type: 'app',
        brandName: 'CodeSpirit',
        logo: webHost + '/favicon.ico',
        header: {
            type: 'service',
            api: '/identity/api/identity/profile',
            silentPolling: false,
            className: 'flex w-full justify-end',
            onEvent: {
                fetchInited: {
                    actions: [
                        {
                            actionType: "custom",
                            script: `
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
                                        desc: '<span class=\"text-base\">${content}</span>',
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
                                    //itemAction: {
                                    //    actionType: 'ajax',
                                    //    api: 'POST:/messaging/api/messaging/messages/my/${id}/read'
                                    //},
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
                    },
                },
                {
                    type: 'button',
                    icon: 'fa fa-comments',
                    tooltip: '聊天',
                    level: 'link',
                    className: 'mr-3 chat-btn',
                    actionType: 'url',
                    url: '/chat'
                },
                {
                    type: 'avatar',
                    src: '${user.avatar}',
                    text: '${user.name}',
                    icon: 'fa fa-user',
                    className: 'mr-2',
                    size: 30,
                    onError: function () {
                        return true;
                    }
                },
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
        },
        api: '/api/navigation/site'
    };

    function normalizeLink(to, location = history.location) {
        to = to || '';
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
    }

    function isCurrentUrl(to, ctx) {
        if (!to) {
            return false;
        }

        const pathname = history.location.pathname;
        const link = normalizeLink(to, {
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

    let amisInstance = amis.embed(
        '#root',
        app,
        {
            location: history.location,
            data: {},
            context: {
                API_HOST: apiHost,
                WEB_HOST: webHost,
                aspire_dashboard: aspire_dashboard
            }
        },
        {
            updateLocation: (location, replace) => {
                location = normalizeLink(location);
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

                to = normalizeLink(to);

                if (isCurrentUrl(to)) {
                    return;
                }

                if (to.startsWith('/impersonate') || to.startsWith('/login') || to.startsWith('/notifications') || to.startsWith('/chat')) {
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
            isCurrentUrl: isCurrentUrl,
            requestAdaptor: (api) => {
                var token = localStorage.getItem('token');
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

                // 处理错误响应
                if (response.status === 403) {
                    return { msg: '您没有权限访问此页面，请联系管理员！' }
                }
                else if (response.status === 401) {
                    // 获取当前路径作为重定向参数
                    const currentPath = encodeURIComponent(window.location.hash || window.location.pathname);
                    window.location.href = `/login?redirect=${currentPath}`;
                    return { msg: '登录过期！' };
                }

                // 如果是获取用户信息的接口,将数据注入到全局
                if (api.url.includes('/identity/api/identity/profile')) {
                    // 更新全局数据对象
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
        }
    );

    amisInstance.updateProps({
        data: {
            notifications: {
                count: 0,
                hasUnread: false
            }
        }
    });

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });

    // 导出全局函数用于更新通知
    window.updateNotificationCount = function (count) {
        // 更新全局数据
        window.GlobalData.set('notifications.count', count);
        window.GlobalData.set('notifications.hasUnread', count > 0);

        // 这样更新后，所有绑定到这些变量的组件都会自动更新
        amisInstance.updateProps({
            data: {
                notifications: {
                    count: count,
                    hasUnread: count > 0
                }
            }
        });

    };

    // 自动获取未读通知数
    window.fetchUnreadNotificationCount = function () {
        var token = localStorage.getItem('token');                
        // 发起AJAX请求获取未读消息数
        fetch(`/messaging/api/messaging/messages/my/unread/count`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        })
            .then(response => {
                console.debug(response);
                if (!response.ok) {
                    console.error('获取未读消息数失败:', response);
                }
                return response.json();
            })
            .then(data => {
                console.debug(data);
                const count = data.count || data.unreadCount || 0;
                window.updateNotificationCount(count);
            })
            .catch(error => {
                console.error('获取未读消息数失败:', error);
            });
    };

    // 导出全局函数用于显示新聊天消息通知
    window.showChatNotification = function (message, sender, conversationId) {
        // 创建通知toast
        amis.toast.info(
            `<div class="chat-notification">
                <div class="sender">${sender}</div>
                <div class="message">${message}</div>
                <div class="action">点击查看</div>
            </div>`,
            {
                position: 'top-right',
                closeButton: true,
                showIcon: true,
                timeout: 8000,
                onClose: () => { /* 可以添加回调 */ }
            }
        ).then(toastObj => {
            // 点击通知时打开对应的聊天窗口
            const notificationEl = document.querySelector('.chat-notification');
            if (notificationEl) {
                notificationEl.addEventListener('click', () => {
                    window.location.href = `/chat-app?conversation=${conversationId}`;
                    toastObj.close();
                });
            }
        });
    };

})();