(function () {
    // 初始化为系统模式
    TokenManager.initSystemMode();
    
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
                                                                        TokenManager.setToken(payload.data.token, 24);
                                                                    }
                                                                    
                                                                    // 延迟跳转，让用户看到成功提示
                                                                    setTimeout(() => {
                                                                        const urlParams = new URLSearchParams(window.location.search);
                                                                        const redirectUrl = urlParams.get('redirect');
                                                                        
                                                                        // 调试信息
                                                                        console.log('🔍 ========== 系统登录跳转调试信息 ==========');
                                                                        console.log('📍 当前位置:', window.location.href);
                                                                        console.log('🔗 原始redirectUrl:', redirectUrl);
                                                                        console.log('📝 解码后redirectUrl:', redirectUrl ? decodeURIComponent(redirectUrl) : null);
                                                                        console.log('⚙️ URL搜索参数:', window.location.search);
                                                                        console.log('=======================================');
                                                                        
                                                                        /**
                                                                         * 检查重定向URL是否为登录相关页面
                                                                         * @param {string} url - 要检查的URL
                                                                         * @returns {boolean} 如果是登录页面返回true
                                                                         */
                                                                        const isLoginPage = (url) => {
                                                                            if (!url) return false;
                                                                            
                                                                            const urlLower = url.toLowerCase();
                                                                            
                                                                            // 精确匹配登录页面模式
                                                                            const exactPatterns = [
                                                                                '#/',           // 仅匹配根hash路由
                                                                                '#/login',      // hash登录页
                                                                                '#/auth',       // hash认证页
                                                                            ];
                                                                            
                                                                            // 包含匹配的登录页面模式
                                                                            const containsPatterns = [
                                                                                '/login',
                                                                                '/system/login', 
                                                                                '/tenant/login',
                                                                                'login.html'
                                                                            ];
                                                                            
                                                                            // 检查精确匹配
                                                                            const isExactMatch = exactPatterns.some(pattern => urlLower === pattern);
                                                                            
                                                                            // 检查包含匹配
                                                                            const isContainsMatch = containsPatterns.some(pattern => 
                                                                                urlLower.includes(pattern.toLowerCase())
                                                                            );
                                                                            
                                                                            const result = isExactMatch || isContainsMatch;
                                                                            
                                                                            console.log('🔍 系统登录页面检查:', {
                                                                                url: url,
                                                                                urlLower: urlLower,
                                                                                isExactMatch: isExactMatch,
                                                                                isContainsMatch: isContainsMatch,
                                                                                result: result
                                                                            });
                                                                            
                                                                            return result;
                                                                        };
                                                                        
                                                                        /**
                                                                         * 构建系统后台的跳转URL
                                                                         * @param {string} redirectUrl - 重定向URL
                                                                         * @returns {string} 完整的跳转URL
                                                                         */
                                                                        const buildSystemRedirectUrl = (redirectUrl) => {
                                                                            try {
                                                                                const decodedUrl = decodeURIComponent(redirectUrl);
                                                                                const baseUrl = '/'; // 系统后台基础URL
                                                                                
                                                                                console.log('🔨 ========== 构建系统跳转URL ==========');
                                                                                console.log('📝 输入参数:', redirectUrl);
                                                                                console.log('🔄 解码结果:', decodedUrl);
                                                                                console.log('🏠 基础URL:', baseUrl);
                                                                                
                                                                                // 如果是完整的URL（包含协议或域名），直接使用
                                                                                if (decodedUrl.startsWith('http://') || decodedUrl.startsWith('https://')) {
                                                                                    console.log('✅ 检测到完整URL，直接使用:', decodedUrl);
                                                                                    return decodedUrl;
                                                                                }
                                                                                
                                                                                // 如果是hash路由 (以#开头)
                                                                                if (decodedUrl.startsWith('#')) {
                                                                                    const result = baseUrl + decodedUrl;
                                                                                    console.log('✅ 检测到hash路由，拼接结果:', result);
                                                                                    return result;
                                                                                }
                                                                                
                                                                                // 如果是其他绝对路径，转换为hash路由
                                                                                if (decodedUrl.startsWith('/')) {
                                                                                    const result = `${baseUrl}#${decodedUrl}`;
                                                                                    console.log('✅ 检测到绝对路径，转为hash路由:', result);
                                                                                    return result;
                                                                                }
                                                                                
                                                                                // 如果是相对路径，转换为hash路由
                                                                                const result = `${baseUrl}#/${decodedUrl}`;
                                                                                console.log('✅ 检测到相对路径，转为hash路由:', result);
                                                                                return result;
                                                                                
                                                                            } catch (error) {
                                                                                console.error('构建系统跳转URL失败:', error);
                                                                                // 发生错误时回退到默认页面
                                                                                return '/admin';
                                                                            }
                                                                        };
                                                                        
                                                                        // 如果有重定向URL且不是登录页面，则跳转到重定向URL
                                                                        if (redirectUrl && !isLoginPage(redirectUrl)) {
                                                                            const targetUrl = buildSystemRedirectUrl(redirectUrl);
                                                                            console.log('🎯 准备跳转到:', targetUrl);
                                                                            
                                                                            // 验证目标URL是否有效
                                                                            try {
                                                                                new URL(targetUrl, window.location.origin);
                                                                                console.log('✅ URL验证通过，执行跳转:', targetUrl);
                                                                                window.location.href = targetUrl;
                                                                            } catch (urlError) {
                                                                                console.error('❌ 目标URL无效:', urlError);
                                                                                console.log('🔄 回退到系统后台首页');
                                                                                window.location.href = '/';
                                                                            }
                                                                        } else {
                                                                            // 否则跳转到系统后台首页
                                                                            console.log('🎯 准备跳转到系统后台首页: /');
                                                                            console.log('📝 跳转原因:', redirectUrl ? '重定向URL被识别为登录页面' : '没有重定向URL');
                                                                            window.location.href = '/';
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