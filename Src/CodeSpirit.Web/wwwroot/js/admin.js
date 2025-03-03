(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    const app = {
        type: 'app',
        brandName: 'CodeSpirit',
        logo: webHost + '/favicon.ico',
        header: {
            type: 'service',
            api: '/identity/api/identity/profile',
            silentPolling: false,
            className: 'flex w-full justify-end',
            body: [
                {
                    type: 'avatar',
                    src: '${avatar}',
                    text: '${name}',
                    icon: 'fa fa-user',
                    className: 'mr-2',
                    size: 30,
                    onError: function() {
                        return true;
                    }
                },
                {
                    type: 'dropdown-button',
                    label: '${name}',
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
                                    api: apiHost + '/api/identity/profile',
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
                            api: apiHost + '/api/identity/auth/logout',
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

                if (to.startsWith('/impersonate') || to.startsWith('/login')) {
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
                //console.debug('payload', payload);
                //console.debug('response', response);
                
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

                //// 如果是获取用户信息的接口,将数据注入到全局
                //if (api.url === apiHost + '/api/identity/profile') {
                //    amisInstance.updateProps({
                //        data: {
                //            ...payload.data // 将用户信息数据注入到全局
                //        }
                //    });
                //}

                return payload;
            },
            theme: 'antd'
        }
    );

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})();