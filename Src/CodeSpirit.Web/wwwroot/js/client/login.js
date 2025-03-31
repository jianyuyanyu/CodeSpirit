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
            "alignItems": "center",
            "className": "login-page-container",
            "items": [
                {
                    "type": "tpl",
                    "tpl": "<div class='login-decoration'><div class='circle-1'></div><div class='circle-2'></div><div class='square-1'></div><div class='square-2'></div></div>",
                    "className": "login-background-decoration"
                },
                {
                    "type": "container",
                    "className": "client-login-container",
                    "style": {
                        "maxWidth": "420px",
                        "width": "100%",
                        "margin": "20px auto",
                        "position": "relative",
                        "zIndex": "10"
                    },
                    "body": [
                        {
                            "type": "tpl",
                            "tpl": "<div class='client-logo text-center'><img src='"+ (window.siteSettings ? window.siteSettings.logoUrl : '/logo.png') +"' /><h2>"+ (window.siteSettings ? window.siteSettings.siteName : '考试系统') +"</h2><p class='login-subtitle'>用户登录</p></div>",
                            "className": "mb-4"
                        },
                        {
                            "type": "panel",
                            "className": "client-login-panel",
                            "body": [
                                {
                                    "type": "form",
                                    "title": "",
                                    "api": "/identity/api/identity/auth/login",
                                    "trimValues": true,
                                    "wrapWithPanel": false,
                                    "className": "login-form p-4",
                                    "body": [
                                        {
                                            "type": "input-text",
                                            "name": "userName",
                                            "label": "用户名",
                                            "placeholder": "请输入用户名/手机号/邮箱",
                                            "required": true,
                                            "inputClassName": "client-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-user",
                                            "labelClassName": "login-label"
                                        },
                                        {
                                            "type": "input-password",
                                            "name": "password",
                                            "label": "密码",
                                            "placeholder": "请输入密码",
                                            "required": true,
                                            "inputClassName": "client-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-lock",
                                            "labelClassName": "login-label"
                                        },
                                        {
                                            "type": "flex",
                                            "justify": "space-between",
                                            "className": "mt-2 mb-3",
                                            "items": [
                                                {
                                                    "type": "checkbox",
                                                    "name": "rememberMe",
                                                    "option": "记住我",
                                                    "className": "remember-me-checkbox"
                                                },
                                                {
                                                    "type": "tpl",
                                                    "tpl": "<a href='#' class='forgot-password'>忘记密码?</a>",
                                                    "className": "forgot-link"
                                                }
                                            ]
                                        },
                                        {
                                            "type": "button",
                                            "label": "登录",
                                            "level": "primary",
                                            "block": true,
                                            "actionType": "submit",
                                            "className": "login-btn mt-4"
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
                            "tpl": "<div class='text-center mt-3 login-footer'>© 2025 "+ (window.siteSettings ? window.siteSettings.siteName : '考试系统') +" 版权所有</div>"
                        }
                    ]
                }
            ]
        },
        "css": {
            // 全局变量
            ":root": {
                "--primary-color": "#3f51b5",
                "--primary-light": "#7986cb",
                "--primary-dark": "#303f9f",
                "--success-color": "#4caf50",
                "--warning-color": "#ff9800",
                "--danger-color": "#f44336",
                "--border-radius": "8px",
                "--box-shadow": "0 4px 12px rgba(0,0,0,0.1)"
            },
            "body": {
                "background-color": "#f5f7fa",
                "background-image": "linear-gradient(135deg, #f5f7fa 0%, #e9ecf2 100%)",
                "min-height": "100vh",
                "font-family": "'PingFang SC', 'Microsoft YaHei', sans-serif",
                "color": "#333",
                "overflow-x": "hidden",
                "margin": "0",
                "padding": "0"
            },
            "a": {
                "color": "var(--primary-color)",
                "text-decoration": "none",
                "transition": "color 0.3s ease"
            },
            "a:hover": {
                "color": "var(--primary-dark)"
            },
            ".login-page-container": {
                "min-height": "100vh",
                "width": "100vw",
                "position": "relative",
                "overflow": "hidden",
                "display": "flex",
                "align-items": "center",
                "justify-content": "center",
                "padding": "20px 0"
            },
            ".login-background-decoration": {
                "position": "absolute",
                "top": "0",
                "left": "0",
                "width": "100%",
                "height": "100%",
                "z-index": "1"
            },
            ".login-decoration": {
                "position": "absolute",
                "width": "100%",
                "height": "100%",
                "top": "0",
                "left": "0",
                "overflow": "hidden",
                "pointer-events": "none"
            },
            ".circle-1": {
                "position": "absolute",
                "width": "300px",
                "height": "300px",
                "border-radius": "50%",
                "background": "linear-gradient(135deg, rgba(63, 81, 181, 0.1) 0%, rgba(63, 81, 181, 0.2) 100%)",
                "top": "-50px",
                "left": "-50px",
                "z-index": "-1",
                "animation": "float 15s ease-in-out infinite"
            },
            ".circle-2": {
                "position": "absolute",
                "width": "400px",
                "height": "400px",
                "border-radius": "50%",
                "background": "linear-gradient(135deg, rgba(63, 81, 181, 0.05) 0%, rgba(63, 81, 181, 0.1) 100%)",
                "bottom": "-100px",
                "right": "-100px",
                "z-index": "-1",
                "animation": "float 20s ease-in-out infinite"
            },
            ".square-1": {
                "position": "absolute",
                "width": "200px",
                "height": "200px",
                "background": "linear-gradient(135deg, rgba(76, 175, 80, 0.05) 0%, rgba(76, 175, 80, 0.1) 100%)",
                "border-radius": "15%",
                "top": "30%",
                "left": "-100px",
                "transform": "rotate(45deg)",
                "z-index": "-1",
                "animation": "float 25s ease-in-out infinite"
            },
            ".square-2": {
                "position": "absolute",
                "width": "150px",
                "height": "150px",
                "background": "linear-gradient(135deg, rgba(255, 152, 0, 0.05) 0%, rgba(255, 152, 0, 0.1) 100%)",
                "border-radius": "10%",
                "bottom": "20%",
                "right": "10%",
                "transform": "rotate(30deg)",
                "z-index": "-1",
                "animation": "float 18s ease-in-out infinite alternate"
            },
            "@keyframes float": {
                "0%": {
                    "transform": "translate(0px, 0px) rotate(0deg)"
                },
                "25%": {
                    "transform": "translate(10px, 10px) rotate(2deg)"
                },
                "50%": {
                    "transform": "translate(5px, 15px) rotate(0deg)"
                },
                "75%": {
                    "transform": "translate(-5px, 8px) rotate(-2deg)"
                },
                "100%": {
                    "transform": "translate(0px, 0px) rotate(0deg)"
                }
            },
            ".client-login-container": {
                "background-color": "transparent",
                "animation": "fadeIn 0.8s ease-in-out",
                "backdrop-filter": "blur(5px)",
                "z-index": "10"
            },
            "@keyframes fadeIn": {
                "0%": {
                    "opacity": "0",
                    "transform": "translateY(20px)"
                },
                "100%": {
                    "opacity": "1",
                    "transform": "translateY(0)"
                }
            },
            ".client-logo": {
                "margin-bottom": "30px",
                "text-align": "center"
            },
            ".client-logo img": {
                "max-width": "90px",
                "margin-bottom": "15px",
                "animation": "logoFloat 3s ease-in-out infinite",
                "filter": "drop-shadow(0 4px 6px rgba(0,0,0,0.1))"
            },
            "@keyframes logoFloat": {
                "0%": {
                    "transform": "translateY(0px)"
                },
                "50%": {
                    "transform": "translateY(-8px)"
                },
                "100%": {
                    "transform": "translateY(0px)"
                }
            },
            ".client-logo h2": {
                "color": "var(--primary-color)",
                "font-weight": "bold",
                "margin-bottom": "5px",
                "letter-spacing": "1px",
                "font-size": "28px",
                "text-shadow": "0 2px 4px rgba(0,0,0,0.1)"
            },
            ".login-subtitle": {
                "color": "#666",
                "font-size": "16px",
                "margin-top": "5px"
            },
            ".client-login-panel": {
                "border-radius": "var(--border-radius)",
                "box-shadow": "var(--box-shadow)",
                "overflow": "hidden",
                "background-color": "rgba(255, 255, 255, 0.95)",
                "border": "none",
                "transition": "transform 0.3s ease, box-shadow 0.3s ease",
                "backdrop-filter": "blur(10px)"
            },
            ".client-login-panel:hover": {
                "transform": "translateY(-5px)",
                "box-shadow": "0 15px 30px rgba(0,0,0,0.1)"
            },
            ".login-form": {
                "padding": "25px"
            },
            ".login-label": {
                "font-weight": "500",
                "color": "#555",
                "margin-bottom": "8px"
            },
            ".client-input": {
                "border-radius": "var(--border-radius)",
                "padding": "10px 15px",
                "border": "1px solid #ddd",
                "box-shadow": "inset 0 1px 3px rgba(0,0,0,0.05)",
                "transition": "all 0.3s ease",
                "font-size": "15px"
            },
            ".client-input:focus": {
                "border-color": "var(--primary-color)",
                "box-shadow": "0 0 0 3px rgba(63, 81, 181, 0.1)"
            },
            ".login-btn": {
                "background-color": "var(--primary-color)",
                "border-color": "var(--primary-color)",
                "padding": "12px 24px",
                "font-size": "16px",
                "border-radius": "var(--border-radius)",
                "transition": "all 0.3s ease",
                "margin-top": "20px",
                "font-weight": "500",
                "letter-spacing": "0.5px",
                "box-shadow": "0 4px 6px rgba(63, 81, 181, 0.2)"
            },
            ".login-btn:hover": {
                "background-color": "var(--primary-dark)",
                "transform": "translateY(-2px)",
                "box-shadow": "0 6px 15px rgba(63, 81, 181, 0.3)"
            },
            ".login-btn:active": {
                "transform": "translateY(1px)",
                "box-shadow": "0 2px 8px rgba(63, 81, 181, 0.3)"
            },
            ".am-CheckboxControl": {
                "font-size": "14px"
            },
            ".am-CheckboxControl-input:checked + .am-CheckboxControl-icon": {
                "background-color": "var(--primary-color)",
                "border-color": "var(--primary-color)"
            },
            ".remember-me-checkbox": {
                "margin-bottom": "0"
            },
            ".forgot-link": {
                "font-size": "14px",
                "padding-top": "6px"
            },
            ".forgot-password": {
                "color": "#666",
                "text-decoration": "none",
                "transition": "all 0.2s ease",
                "font-size": "14px"
            },
            ".forgot-password:hover": {
                "color": "var(--primary-color)",
                "text-decoration": "underline"
            },
            ".login-footer": {
                "color": "#888",
                "font-size": "13px",
                "margin-top": "30px",
                "text-shadow": "0 1px 1px rgba(255,255,255,0.8)"
            },
            // 响应式样式
            "@media (max-width: 768px)": {
                ".client-login-container": {
                    "margin": "15px",
                    "width": "calc(100% - 30px)"
                },
                ".client-logo h2": {
                    "font-size": "24px"
                },
                ".client-logo img": {
                    "max-width": "70px"
                },
                ".login-form": {
                    "padding": "20px 15px"
                },
                ".circle-1, .circle-2, .square-1, .square-2": {
                    "opacity": "0.5"
                }
            },
            "@media (max-width: 480px)": {
                ".client-login-panel": {
                    "box-shadow": "0 2px 10px rgba(0,0,0,0.1)"
                },
                ".login-label": {
                    "font-size": "15px"
                },
                ".client-input": {
                    "padding": "8px 12px",
                    "font-size": "14px"
                },
                ".login-btn": {
                    "padding": "10px 20px",
                    "font-size": "15px"
                },
                ".client-logo h2": {
                    "font-size": "22px"
                },
                ".login-subtitle": {
                    "font-size": "14px"
                }
            },
            "@media (orientation: landscape) and (max-height: 600px)": {
                ".login-page-container": {
                    "height": "auto",
                    "min-height": "100vh",
                    "padding": "20px 0"
                },
                ".client-logo": {
                    "margin-bottom": "15px"
                },
                ".client-logo img": {
                    "max-width": "60px"
                },
                ".client-logo h2": {
                    "font-size": "20px"
                },
                ".login-subtitle": {
                    "font-size": "13px"
                }
            },
            "@media (min-width: 1200px)": {
                ".client-login-container": {
                    "max-width": "450px"
                },
                ".client-logo img": {
                    "max-width": "100px"
                },
                ".client-logo h2": {
                    "font-size": "32px"
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