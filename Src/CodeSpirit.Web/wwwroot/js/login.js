(function () {
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;
    
    TokenManager.clearToken();
    // 通过替换下面这个配置来生成不同页面
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
                                "<div class='logo'><img src='/logo.png' /></div>" +
                                "<div class='login-label'>Welcome</div>" +
                                "<div class='transverse'></div>" +
                                "<div class='login-label' style='margin-bottom: 10px;'>欢迎进入</div>" +
                                "<div class='login-label'>CodeSpirit</div>" +
                                "<div class='transverse'></div>" +
                                "<div class='login-label-x'>Welcome to CodeSpirit</div>" +
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
                            "style": {
                                "backgroundColor": "#eeeff2",
                                "paddingTop": "100px",
                                "height": "100vh",
                            },
                            "items": [
                                {
                                    "style": {
                                        "width": "505px",
                                        "height": "450px",
                                        "borderRadius": "20px",
                                        "border": "none"
                                    },
                                    "className": "form-wrap",
                                    "type": "panel",
                                    // "api": "/api/auth/login",
                                    "title": "",
                                    "body": [
                                        {
                                            "type": "tabs",
                                            "linksClassName": "tabs-title-box",
                                            "tabs": [
                                                {
                                                    "title": "密码登录",
                                                    "body": {
                                                        "type": "form",
                                                        "title": "",
                                                        "api": "/identity/api/identity/auth/login",
                                                        "submitText": "登录",
                                                        "trimValues": true,
                                                        "wrapWithPanel": false,
                                                        "redirect": "/",
                                                        "body": [
                                                            {
                                                                "type": "input-text",
                                                                "label": "账号",
                                                                "name": "userName",
                                                                "placeholder": "手机号码/账号/邮箱",
                                                                "required": true,
                                                                "className": "input-field"
                                                            },
                                                            {
                                                                "type": "input-password",
                                                                "label": "密码",
                                                                "name": "password",
                                                                "placeholder": "请输入密码",
                                                                "required": true,
                                                                "className": "input-field"
                                                            }
                                                            ,
                                                            {
                                                                "type": "button",
                                                                "label": "登录",
                                                                "level": "primary",
                                                                "actionType": "submit",
                                                                "className": "submit-btn"
                                                            }
                                                        ],
                                                        "onEvent": {
                                                            "submitSucc": {
                                                                "actions": [
                                                                    {
                                                                        "actionType": "custom",
                                                                        "script": "TokenManager.setToken(event.data.result.data.token);"
                                                                    }
                                                                ]
                                                            }
                                                        }
                                                    }
                                                },
                                                {
                                                    "title": "短信登录",
                                                    "body": {
                                                        "type": "form",
                                                        "api": "/identity/api/identity/auth/login",
                                                        "wrapWithPanel": false,
                                                        "body": [
                                                            {
                                                                "type": "input-text",
                                                                "label": "用户",
                                                                "name": "loginName",
                                                                "placeholder": "手机号码",
                                                                "required": true,
                                                                "className": "input-field"
                                                            },
                                                            {
                                                                "type": "input-text",
                                                                "label": "验证码",
                                                                "name": "code",
                                                                "placeholder": "请输入验证码",
                                                                "required": true,
                                                                "className": "input-field"
                                                            }
                                                        ]
                                                    }
                                                },
                                                {
                                                    "title": "扫码登录",
                                                    "body": [
                                                        {
                                                            "type": "qrcode",
                                                            "value": "二维码信息",
                                                            "label": "请使用App扫码登录",
                                                            "className": "qr-code"
                                                        }
                                                    ]
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
    }
        ;
    let amisScoped = amis.embed('#root', amisJSON, {
        location: history.location,
        data: {},
        context: {
            API_HOST: apiHost,
            WEB_HOST: webHost
        }
    }, { theme: 'antd' });
})();