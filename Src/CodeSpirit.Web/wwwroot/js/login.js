(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    
    TokenManager.clearToken();
    
    // 系统平台登录页面配置
    let amisJSON =
    {
        "type": "page",
        "title": "",
        "body": {
            "type": "container",
            "body": [
                {
                    "type": "grid",
                    "gap": "none",
                    "columns": [
                        {
                            "type": "tpl",
                            "xs": "3",
                            "sm": "3",
                            "md": "3",
                            "lg": "3",
                            "tpl": "<div class='login-left'>" +
                                "<div class='logo'><img src='"+ (window.siteSettings ? window.siteSettings.logoUrl : '/logo.png') +"' /></div>" +
                                "<div class='login-label'>Welcome</div>" +
                                "<div class='transverse'></div>" +
                                "<div class='login-label' style='margin-bottom: 10px;'>欢迎进入</div>" +
                                "<div class='login-label'>" + (window.siteSettings ? window.siteSettings.topSiteName : 'CodeSpirit 系统管理平台') +"</div>" +
                                "<div class='transverse'></div>" +
                                "<div class='login-label-x'>Welcome to "+ (window.siteSettings ? window.siteSettings.siteName : 'CodeSpirit System Management') +"</div>" +
                                "<div class='carousel'>" +
                                "<div class='carousel-img'><img src='/public/lb.png' alt='' /></div>" +
                                "</div></div>",
                            "width": 6,
                        },
                        {
                            "xs": "9",
                            "sm": "9",
                            "md": "9",
                            "lg": "9",
                            "type": "flex",
                            "justify": "center",
                            "alignItems": "center",
                            "style": {
                                "backgroundColor": "#eeeff2",
                                "minHeight": "100vh",
                                "padding": "20px"
                            },
                            "items": [
                                {
                                    "style": {
                                        "width": "505px",
                                        "minHeight": "auto",
                                        "maxHeight": "90vh",
                                        "borderRadius": "20px",
                                        "border": "none",
                                        "padding": "20px",
                                        "overflow": "auto"
                                    },
                                    "className": "form-wrap",
                                    "type": "panel",
                                    "title": "",
                                    "body": [
                                        {
                                            "type": "tabs",
                                            "linksClassName": "tabs-title-box",
                                            "tabs": [
                                                {
                                                    "title": "系统平台登录",
                                                    "body": {
                                                        "type": "form",
                                                        "title": "",
                                                        "api": {
                                                            "method": "post",
                                                            "url": "/identity/api/identity/auth/system/login",
                                                            "requestAdaptor": function(api) {
                                                                // 添加请求头
                                                                api.headers = api.headers || {};
                                                                api.headers['X-Forwarded-With'] = 'CodeSpirit';
                                                                api.headers['Content-Type'] = 'application/json';
                                                                return api;
                                                            },
                                                            "adaptor": function(payload, response, api) {
                                                                if (payload.status === 0) {
                                                                    // 登录成功，保存token
                                                                    if (payload.data && payload.data.token) {
                                                                        TokenManager.setToken(payload.data.token);
                                                                    }
                                                                    
                                                                    // 延迟跳转，让用户看到成功提示
                                                                    setTimeout(() => {
                                                                        const urlParams = new URLSearchParams(window.location.search);
                                                                        const redirectUrl = urlParams.get('redirect');
                                                                        
                                                                        /**
                                                                         * 检查重定向URL是否为登录相关页面
                                                                         * @param {string} url - 要检查的URL
                                                                         * @returns {boolean} 如果是登录页面返回true
                                                                         */
                                                                        const isLoginPage = (url) => {
                                                                            if (!url) return false;
                                                                            const loginPatterns = ['/login', '/system/login', '/tenant/login', 'login.html','#/'];
                                                                            return loginPatterns.some(pattern => 
                                                                                url.toLowerCase().includes(pattern.toLowerCase())
                                                                            );
                                                                        };
                                                                        
                                                                        // 如果有重定向URL且不是登录页面，则跳转到重定向URL
                                                                        if (redirectUrl && !isLoginPage(redirectUrl)) {
                                                                            window.location.href = decodeURIComponent(redirectUrl);
                                                                        } else {
                                                                            // 否则跳转到系统后台首页
                                                                            window.location.href = '/'; // 系统后台
                                                                        }
                                                                    }, 1000);
                                                                } else {
                                                                    // 显示具体的错误信息
                                                                    console.error('系统平台登录失败:', payload.msg);
                                                                }
                                                                return payload;
                                                            }
                                                        },
                                                        "submitText": "登录系统平台",
                                                        "trimValues": true,
                                                        "wrapWithPanel": false,
                                                        //"redirect": "/",
                                                        "style": {
                                                            "padding": "0"
                                                        },
                                                        "body": [
                                                            {
                                                                "type": "input-text",
                                                                "label": "系统平台账号",
                                                                "name": "userName",
                                                                "placeholder": "请输入系统平台账号",
                                                                "required": true,
                                                                "className": "input-field mb-3"
                                                            },
                                                            {
                                                                "type": "input-password",
                                                                "label": "密码",
                                                                "name": "password",
                                                                "placeholder": "请输入密码",
                                                                "required": true,
                                                                "className": "input-field mb-3"
                                                            },
                                                            {
                                                                "type": "checkbox",
                                                                "name": "rememberMe",
                                                                "option": "记住我",
                                                                "className": "mb-3"
                                                            },
                                                            {
                                                                "type": "button",
                                                                "label": "登录系统管理平台",
                                                                "level": "primary",
                                                                "actionType": "submit",
                                                                "className": "submit-btn",
                                                                "block": true,
                                                                "style": {
                                                                    "marginTop": "10px"
                                                                }
                                                            }
                                                        ]
                                                    }
                                                },
                                                {
                                                    "title": "租户登录入口",
                                                    "body": {
                                                        "type": "container",
                                                        "style": {
                                                            "padding": "0"
                                                        },
                                                        "body": [
                                                            {
                                                                "type": "alert",
                                                                "level": "warning",
                                                                "body": "如果您是租户用户，请前往租户专属登录页面",
                                                                "className": "mb-2",
                                                                "showIcon": true
                                                            },
                                                            {
                                                                "type": "form",
                                                                "title": "",
                                                                "wrapWithPanel": false,
                                                                "style": {
                                                                    "padding": "0"
                                                                },
                                                                "body": [
                                                                    {
                                                                        "type": "input-text",
                                                                        "label": "租户ID",
                                                                        "name": "tenantId",
                                                                        "placeholder": "请输入您的租户ID",
                                                                        "required": true,
                                                                        "className": "input-field mb-3",
                                                                        "description": "请联系您的租户管理员获取租户ID"
                                                                    },
                                                                    {
                                                                        "type": "button",
                                                                        "label": "前往租户登录页",
                                                                        "level": "info",
                                                                        "actionType": "submit",
                                                                        "className": "submit-btn",
                                                                        "block": true,
                                                                        "style": {
                                                                            "marginTop": "10px"
                                                                        },
                                                                    }
                                                                ],
                                                                "onEvent": {
                                                                    "submitSucc": {
                                                                        "actions": [
                                                                            {
                                                                                "actionType": "custom",
                                                                                "script": "const tenantId = event.data.tenantId; if (!tenantId) { amisRequire('amis').toast.error('请输入租户ID'); return; } window.location.href = `/${tenantId}/login`;"
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
                                    ],
                                    "width": 6
                                }
                            ]
                        }
                    ]
                }
            ]
        }
    };

    let amisScoped = amis.embed('#root', amisJSON, {
        location: history.location,
        data: {},
        context: {
            WEB_HOST: webHost
        }
    }, { theme: 'antd' });
})();