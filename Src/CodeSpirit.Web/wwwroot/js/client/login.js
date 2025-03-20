(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;

    TokenManager.clearToken();
    // 通过替换下面这个配置来生成不同页面
    let amisJSON = {
        "type": "page",
        "title": "",
        "body": {
            "type": "flex",
            "justify": "center",
            "items": [
                {
                    "type": "container",
                    "className": "client-login-container",
                    "style": {
                        "maxWidth": "380px",
                        "width": "100%",
                        "margin": "50px auto"
                    },
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<div class='client-logo text-center'><img src='/logo.png' /><h3>用户登录</h3></div>",
                            "className": "mb-4"
                        },
                        {
                            "type": "panel",
                            "className": "client-login-panel",
                            "title": "用户登录",
                            "titleClassName": "text-center",
                            "body": [
                                {
                                    "type": "form",
                                    "title": "",
                                    "api": "/identity/api/identity/auth/login",
                                    "trimValues": true,
                                    "wrapWithPanel": false,
                                    "className": "p-3",
                                    "body": [
                                        {
                                            "type": "input-text",
                                            "name": "userName",
                                            "placeholder": "请输入用户名/手机号/邮箱",
                                            "required": true,
                                            "inputClassName": "client-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-user"
                                        },
                                        {
                                            "type": "input-password",
                                            "name": "password",
                                            "placeholder": "请输入密码",
                                            "required": true,
                                            "inputClassName": "client-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-lock"
                                        },
                                        {
                                            "type": "button",
                                            "label": "登录",
                                            "level": "primary",
                                            "block": true,
                                            "actionType": "submit",
                                            "className": "mt-4"
                                        }
                                    ],
                                    "onEvent": {
                                        "submitSucc": {
                                            "actions": [
                                                {
                                                    "actionType": "custom",
                                                    "script": "TokenManager.setToken(event.data.result.data.token);"
                                                },
                                                {
                                                    "actionType": "custom",
                                                    "script": "const urlParams = new URLSearchParams(window.location.search); const redirectUrl = urlParams.get('redirect'); if (redirectUrl) { window.location.href = decodeURIComponent(redirectUrl); } else { window.location.href = '/client/'; }"
                                                }
                                            ]
                                        }
                                    }
                                }
                            ]
                        },
                        {
                            "type": "tpl",
                            "tpl": "<div class='text-center mt-3'><a href='#'>忘记密码</a></div>"
                        }
                    ]
                }
            ]
        },
        "css": {
            ".client-login-container": {
                "background-color": "transparent"
            },
            ".client-logo": {
                "margin-bottom": "20px"
            },
            ".client-logo img": {
                "max-width": "80px",
                "margin-bottom": "10px"
            },
            ".client-login-panel": {
                "border-radius": "8px",
                "box-shadow": "0 4px 12px rgba(0,0,0,0.1)",
                "overflow": "hidden"
            },
            ".client-input": {
                "border-radius": "4px"
            },
            "@media (max-width: 768px)": {
                ".client-login-container": {
                    "margin": "20px",
                    "width": "calc(100% - 40px)"
                }
            }
        }
    };

    let amisScoped = amis.embed('#root', amisJSON, {
        location: history.location,
        data: {},
        context: {
            API_HOST: apiHost,
            WEB_HOST: webHost
        }
    }, { theme: 'antd' });
})();