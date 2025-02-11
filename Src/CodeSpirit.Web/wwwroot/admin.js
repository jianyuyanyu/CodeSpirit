(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    // 使用 HashHistory
    const history = History.createHashHistory();

    const app = {
        type: 'app',
        brandName: 'CodeSpirit',
        logo: webHost+'/favicon.ico',
        header: {
            type: 'tpl',
            inline: false,
            className: 'w-full',
            // tpl: '<div class="flex justify-between"><div>顶部区域左侧</div><div>顶部区域右侧</div></div>'
        },
        api: apiHost+ '/api/identity/amis/site'
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
                WEB_HOST: webHost
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
                console.debug(api);
                var token = localStorage.getItem('token');
                return {
                    ...api,
                    headers: {
                        ...api.headers,  // 保留已有的请求头
                        'Authorization': 'Bearer ' + token  // 添加新的请求头
                    }
                };
            },
            responseAdaptor: function (api, payload, query, request, response) {
                console.debug('payload', payload);
                console.debug('response', response);
                if (response.status === 403) {
                    // 提示没有权限
                    //amisInstance.doAction({ actionType: 'toast', args: { msgType :'error',msg:'您没有权限访问此页面，请联系管理员！' } });
                    return { msg:'您没有权限访问此页面，请联系管理员！'}
                }
                else if (response.status === 401) {
                    // 跳转到登录页
                    window.location.href = '/login';  // 替换为实际的登录页路径
                    return { msg:'登录过期！' };  // 返回一个空对象，避免 Amis 继续处理
                }

                return payload;  // 正常返回数据
            },
            //fetcher: ({ url, method, data, config }) => {
            //    return axios({ url, method, data, ...config })
            //        .then(response => {
            //            console.debug(response);
            //            if (response.status === 401) {
            //                window.location.href = '/login';
            //                return Promise.reject(response);
            //            }
            //            return response;
            //        })
            //        .catch(error => {
            //            console.error('请求错误', error);
            //            return Promise.reject(error);
            //        });
            //},
            theme: 'antd'
        }
    );

    history.listen(state => {
        amisInstance.updateProps({
            location: state.location || state
        });
    });
})();