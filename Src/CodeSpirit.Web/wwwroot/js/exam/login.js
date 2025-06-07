(function () {
    // 初始化为客户端模式（考试系统）
    TokenManager.initClientMode();
    
    // 设置考试登录标志
    window.isExamLogin = true;
    
    let amis = amisRequire('amis/embed');
    const match = amisRequire('path-to-regexp').match;

    TokenManager.clearToken();
    
    /**
     * 获取URL参数
     * @param {string} name 参数名
     * @returns {string|null} 参数值
     */
    function getUrlParameter(name) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }
    
    // 考试系统登录页面配置
    let amisJSON = {
        "type": "page",
        "title": "",
        "body": {
            "type": "flex",
            "justify": "center",
            "alignItems": "center",
            "className": "exam-login-page-container",
            "items": [
                {
                    "type": "tpl",
                    "tpl": "<div class='exam-decoration'><div class='circle-1'></div><div class='circle-2'></div><div class='square-1'></div><div class='square-2'></div></div>",
                    "className": "exam-background-decoration"
                },
                {
                    "type": "container",
                    "className": "exam-login-container",
                    "style": {
                        "maxWidth": "450px",
                        "width": "100%",
                        "margin": "20px auto",
                        "position": "relative",
                        "zIndex": "10"
                    },
                    "body": [
                        // 租户信息显示
                        {
                            "type": "tpl",
                            "tpl": "<div class='tenant-info'><i class='fa fa-building'></i><span>${tenantName}</span></div>",
                            "data": {
                                "tenantId": window.tenantId,
                                "tenantName": window.tenantName
                            }
                        },
                        // Logo和标题
                        {
                            "type": "tpl",
                            "tpl": buildExamBrandingTpl(),
                            "className": "mb-4"
                        },
                        // 登录表单面板
                        {
                            "type": "panel",
                            "className": "exam-login-panel",
                            "body": [
                                {
                                    "type": "form",
                                    "title": "",
                                    "api": "/identity/api/identity/auth/client/login",
                                    "trimValues": true,
                                    "wrapWithPanel": false,
                                    "className": "exam-login-form p-4",
                                    "body": [
                                        // 隐藏字段：租户ID和客户端类型
                                        {
                                            "type": "hidden",
                                            "name": "tenantId",
                                            "value": window.tenantId
                                        },
                                        {
                                            "type": "hidden",
                                            "name": "clientType",
                                            "value": "exam"
                                        },
                                        // 用户名输入框
                                        {
                                            "type": "input-text",
                                            "name": "userName",
                                            "label": "用户名",
                                            "placeholder": "请输入用户名/学号/身份证号码",
                                            "required": true,
                                            "inputClassName": "exam-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-user",
                                            "labelClassName": "exam-label",
                                            "minLength": 2,
                                            "maxLength": 50,
                                            "value": getUrlParameter('username') || "",
                                            "validationErrors": {
                                                "minLength": "用户名至少需要2个字符",
                                                "maxLength": "用户名最多50个字符",
                                                "isRequired": "请输入用户名"
                                            },
                                            "validateOnChange": false,
                                            "validateOnBlur": true
                                        },
                                        // 密码输入框
                                        {
                                            "type": "input-password",
                                            "name": "password",
                                            "label": "密码",
                                            "placeholder": "请输入密码",
                                            "required": true,
                                            "inputClassName": "exam-input",
                                            "clearable": true,
                                            "prefixIcon": "fa fa-lock",
                                            "labelClassName": "exam-label",
                                            "revealPassword": true,
                                            "minLength": 6,
                                            "maxLength": 128,
                                            "validationErrors": {
                                                "minLength": "密码至少需要6个字符",
                                                "maxLength": "密码最多128个字符",
                                                "isRequired": "请输入密码"
                                            },
                                            "validateOnChange": false,
                                            "validateOnBlur": true
                                        },
                                        // 记住我和忘记密码
                                        {
                                            "type": "flex",
                                            "justify": "space-between",
                                            "className": "mt-2 mb-3",
                                            "items": [
                                                {
                                                    "type": "checkbox",
                                                    "name": "rememberMe",
                                                    "option": "记住我",
                                                    "className": "exam-remember-me-checkbox"
                                                },
                                                {
                                                    "type": "tpl",
                                                    "tpl": "<a href='#' class='exam-forgot-password' onclick='alert(\"请联系管理员重置密码\")'>忘记密码？</a>",
                                                    "className": "exam-forgot-link"
                                                }
                                            ]
                                        },
                                        // 安全提示
                                        {
                                            "type": "tpl",
                                            "tpl": "<div class='exam-security-notice'><i class='fa fa-shield-alt'></i><div class='notice-content'><div class='notice-title'>安全提示</div><div class='notice-text'>考试过程中系统将监控您的操作行为，请确保在安全的环境中进行考试。禁止切换窗口、使用外部工具等作弊行为。</div></div></div>"
                                        },
                                        // 登录按钮
                                        {
                                            "type": "button",
                                            "label": "安全登录",
                                            "level": "primary",
                                            "block": true,
                                            "actionType": "submit",
                                            "className": "exam-login-btn mt-4",
                                            "size": "lg",
                                            "icon": "fa fa-shield-alt",
                                            "loadingOn": "${formSubmitting}",
                                            "disabledOn": "${formSubmitting}",
                                            "tooltip": "点击进入安全考试环境"
                                        }
                                    ],
                                    "onEvent": {
                                        "submitSucc": {
                                            "actions": [
                                                {
                                                    "actionType": "custom",
                                                    "script": "handleLoginSuccess(event.data.result.data);"
                                                }
                                            ]
                                        },
                                        "submitFail": {
                                            "actions": [
                                                {
                                                    "actionType": "custom",
                                                    "script": "handleLoginError(event.data);"
                                                }
                                            ]
                                        }
                                    }
                                }
                            ]
                        },
                        // 页脚
                        {
                            "type": "tpl",
                            "tpl": buildExamFooterTpl(),
                            "className": "text-center mt-3 exam-footer"
                        }
                    ]
                }
            ]
        },
        "css": {
            // 全局变量
            ":root": {
                "--exam-primary-color": "#1e88e5",
                "--exam-primary-light": "#42a5f5",
                "--exam-primary-dark": "#1565c0",
                "--exam-success-color": "#4caf50",
                "--exam-warning-color": "#ff9800",
                "--exam-danger-color": "#f44336",
                "--exam-border-radius": "8px",
                "--exam-box-shadow": "0 4px 12px rgba(0,0,0,0.1)"
            },
            "body": {
                "background-color": "#e3f2fd",
                "background-image": "linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)",
                "min-height": "100vh",
                "font-family": "'PingFang SC', 'Microsoft YaHei', sans-serif",
                "color": "#333",
                "overflow-x": "hidden",
                "margin": "0",
                "padding": "0"
            },
            "a": {
                "color": "var(--exam-primary-color)",
                "text-decoration": "none",
                "transition": "color 0.3s ease"
            },
            "a:hover": {
                "color": "var(--exam-primary-dark)"
            },
            ".exam-login-page-container": {
                "min-height": "100vh",
                "background": "var(--exam-bg-gradient)",
                "background-size": "400% 400%",
                "animation": "gradientShift 15s ease infinite",
                "position": "relative",
                "overflow": "hidden"
            },
            // 背景装饰
            ".exam-decoration": {
                "position": "absolute",
                "top": "0",
                "left": "0",
                "width": "100%",
                "height": "100%",
                "pointer-events": "none",
                "z-index": "1"
            },
            ".circle-1": {
                "position": "absolute",
                "width": "200px",
                "height": "200px",
                "background": "linear-gradient(135deg, rgba(30, 136, 229, 0.05) 0%, rgba(30, 136, 229, 0.1) 100%)",
                "border-radius": "50%",
                "top": "10%",
                "left": "5%",
                "animation": "float 20s ease-in-out infinite"
            },
            ".circle-2": {
                "position": "absolute",
                "width": "150px",
                "height": "150px",
                "background": "linear-gradient(135deg, rgba(66, 165, 245, 0.05) 0%, rgba(66, 165, 245, 0.1) 100%)",
                "border-radius": "50%",
                "bottom": "15%",
                "right": "8%",
                "animation": "float 15s ease-in-out infinite reverse"
            },
            ".square-1": {
                "position": "absolute",
                "width": "120px",
                "height": "120px",
                "background": "linear-gradient(135deg, rgba(21, 101, 192, 0.05) 0%, rgba(21, 101, 192, 0.1) 100%)",
                "border-radius": "15%",
                "top": "60%",
                "left": "8%",
                "transform": "rotate(45deg)",
                "z-index": "-1",
                "animation": "float 25s ease-in-out infinite"
            },
            ".square-2": {
                "position": "absolute",
                "width": "100px",
                "height": "100px",
                "background": "linear-gradient(135deg, rgba(255, 152, 0, 0.05) 0%, rgba(255, 152, 0, 0.1) 100%)",
                "border-radius": "10%",
                "bottom": "20%",
                "right": "15%",
                "transform": "rotate(30deg)",
                "z-index": "-1",
                "animation": "float 18s ease-in-out infinite alternate"
            },
            "@keyframes float": {
                "0%, 100%": { "transform": "translateY(0px)" },
                "50%": { "transform": "translateY(-20px)" }
            },
            // 表单样式
            ".exam-input": {
                "background-color": "rgba(255, 255, 255, 0.95)",
                "border": "2px solid rgba(30, 136, 229, 0.1)",
                "border-radius": "var(--exam-border-radius)",
                "padding": "14px 16px",
                "font-size": "15px",
                "transition": "all 0.3s ease",
                "box-shadow": "inset 0 1px 3px rgba(0,0,0,0.05)"
            },
            ".exam-input:focus": {
                "border-color": "var(--exam-primary-color)",
                "box-shadow": "0 0 0 3px rgba(30, 136, 229, 0.1)",
                "transform": "translateY(-2px)"
            },
            ".exam-login-btn": {
                "background": "var(--exam-primary-gradient)",
                "border-color": "var(--exam-primary-color)",
                "padding": "16px 24px",
                "font-size": "16px",
                "border-radius": "var(--exam-border-radius)",
                "transition": "all 0.3s ease",
                "margin-top": "20px",
                "font-weight": "600",
                "letter-spacing": "0.5px",
                "box-shadow": "0 4px 6px rgba(30, 136, 229, 0.2)",
                "text-transform": "uppercase",
                "position": "relative",
                "overflow": "hidden"
            },
            ".exam-login-btn:hover": {
                "background": "var(--exam-primary-dark)",
                "transform": "translateY(-2px)",
                "box-shadow": "0 6px 15px rgba(30, 136, 229, 0.3)"
            },
            ".exam-login-btn:active": {
                "transform": "translateY(1px)",
                "box-shadow": "0 2px 8px rgba(30, 136, 229, 0.3)"
            },
            ".am-CheckboxControl": {
                "font-size": "14px"
            },
            ".am-CheckboxControl-input:checked + .am-CheckboxControl-icon": {
                "background-color": "var(--exam-primary-color)",
                "border-color": "var(--exam-primary-color)"
            },
            ".exam-remember-me-checkbox": {
                "margin-bottom": "0"
            },
            ".exam-forgot-link": {
                "font-size": "14px",
                "padding-top": "6px"
            },
            ".exam-forgot-password": {
                "color": "#666",
                "text-decoration": "none",
                "transition": "all 0.2s ease",
                "font-size": "14px"
            },
            ".exam-forgot-password:hover": {
                "color": "var(--exam-primary-color)",
                "text-decoration": "underline"
            },
            ".exam-footer": {
                "color": "rgba(255, 255, 255, 0.8)",
                "font-size": "13px",
                "margin-top": "30px",
                "text-shadow": "0 1px 1px rgba(0,0,0,0.1)"
            },
            ".exam-footer-content p": {
                "margin": "0 0 10px 0",
                "font-size": "13px"
            },
            ".exam-footer-links": {
                "display": "flex",
                "justify-content": "center",
                "align-items": "center",
                "gap": "10px",
                "flex-wrap": "wrap"
            },
            ".exam-footer-links a": {
                "color": "rgba(255, 255, 255, 0.7)",
                "text-decoration": "none",
                "font-size": "12px",
                "transition": "color 0.3s ease"
            },
            ".exam-footer-links a:hover": {
                "color": "rgba(255, 255, 255, 0.9)"
            },
            ".exam-footer-links .separator": {
                "color": "rgba(255, 255, 255, 0.4)",
                "font-size": "12px"
            },
            // 响应式样式
            "@media (max-width: 768px)": {
                ".exam-login-container": {
                    "margin": "10px",
                    "padding": "20px"
                },
                ".exam-login-form": {
                    "padding": "20px !important"
                },
                ".exam-input": {
                    "padding": "12px 14px",
                    "font-size": "14px"
                },
                ".exam-login-btn": {
                    "padding": "14px 20px",
                    "font-size": "14px"
                }
            },
            "@media (max-width: 480px)": {
                ".exam-login-container": {
                    "margin": "5px",
                    "padding": "15px"
                },
                ".exam-login-form": {
                    "padding": "15px !important"
                },
                ".exam-toast": {
                    "top": "10px !important",
                    "right": "10px !important",
                    "left": "10px !important",
                    "min-width": "auto !important",
                    "max-width": "none !important",
                    "transform": "translateY(-100%) !important"
                },
                ".exam-toast.show": {
                    "transform": "translateY(0) !important"
                }
            },
            // 错误页面样式
            ".exam-error": {
                "min-height": "100vh",
                "display": "flex",
                "align-items": "center",
                "justify-content": "center",
                "background": "var(--exam-bg-gradient)",
                "padding": "20px"
            },
            ".exam-error .error-content": {
                "background": "rgba(255, 255, 255, 0.95)",
                "border-radius": "var(--exam-border-radius)",
                "padding": "40px",
                "text-align": "center",
                "max-width": "500px",
                "width": "100%",
                "box-shadow": "var(--exam-box-shadow)"
            },
            ".exam-error .error-icon": {
                "font-size": "60px",
                "color": "var(--exam-danger-color)",
                "margin-bottom": "20px"
            },
            ".exam-error h3": {
                "color": "#333",
                "margin-bottom": "15px",
                "font-size": "24px"
            },
            ".exam-error p": {
                "color": "#666",
                "margin-bottom": "25px",
                "font-size": "16px"
            },
            ".exam-error .error-actions": {
                "display": "flex",
                "gap": "15px",
                "justify-content": "center",
                "margin-bottom": "30px",
                "flex-wrap": "wrap"
            },
            ".exam-error .btn": {
                "padding": "12px 24px",
                "border-radius": "var(--exam-border-radius)",
                "text-decoration": "none",
                "font-size": "14px",
                "border": "none",
                "cursor": "pointer",
                "transition": "all 0.3s ease"
            },
            ".exam-error .btn-primary": {
                "background": "var(--exam-primary-color)",
                "color": "white"
            },
            ".exam-error .btn-primary:hover": {
                "background": "var(--exam-primary-dark)"
            },
            ".exam-error .btn-secondary": {
                "background": "#6c757d",
                "color": "white"
            },
            ".exam-error .btn-secondary:hover": {
                "background": "#5a6268"
            },
            ".exam-error .error-tips": {
                "text-align": "left",
                "background": "#f8f9fa",
                "padding": "20px",
                "border-radius": "var(--exam-border-radius)",
                "border-left": "4px solid var(--exam-primary-color)"
            },
            ".exam-error .error-tips p": {
                "margin": "0 0 10px 0",
                "font-weight": "500",
                "color": "#333"
            },
            ".exam-error .error-tips ul": {
                "margin": "0",
                "padding-left": "20px",
                "color": "#666"
            },
            ".exam-error .error-tips li": {
                "margin-bottom": "5px",
                "font-size": "14px"
            },
            // Toast 提示框样式
            ".exam-toast": {
                "position": "fixed",
                "top": "20px",
                "right": "20px",
                "z-index": "10000",
                "background": "white",
                "border-radius": "var(--exam-border-radius)",
                "box-shadow": "0 6px 16px rgba(0, 0, 0, 0.12)",
                "padding": "16px 20px",
                "min-width": "300px",
                "max-width": "500px",
                "transform": "translateX(100%)",
                "transition": "all 0.3s cubic-bezier(0.4, 0, 0.2, 1)",
                "opacity": "0",
                "pointer-events": "none"
            },
            ".exam-toast.show": {
                "transform": "translateX(0)",
                "opacity": "1",
                "pointer-events": "auto"
            },
            ".exam-toast-success": {
                "border-left": "4px solid #52c41a"
            },
            ".exam-toast-error": {
                "border-left": "4px solid #ff4d4f"
            },
            ".exam-toast .toast-content": {
                "display": "flex",
                "align-items": "center",
                "font-size": "14px",
                "color": "#333",
                "font-weight": "400",
                "line-height": "1.5"
            },
            ".exam-toast .toast-content i": {
                "font-size": "16px",
                "margin-right": "8px !important"
            },
            // 优化登录按钮样式
            ".exam-login-btn .cxd-Button": {
                "background": "linear-gradient(135deg, var(--exam-primary-color) 0%, var(--exam-primary-dark) 100%) !important",
                "border": "none !important",
                "box-shadow": "0 4px 12px rgba(30, 136, 229, 0.3) !important",
                "transition": "all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important",
                "font-weight": "600 !important",
                "letter-spacing": "0.5px !important",
                "text-transform": "none !important",
                "position": "relative !important",
                "overflow": "hidden !important"
            },
            ".exam-login-btn .cxd-Button:hover": {
                "transform": "translateY(-2px) !important",
                "box-shadow": "0 6px 20px rgba(30, 136, 229, 0.4) !important"
            },
            ".exam-login-btn .cxd-Button:active": {
                "transform": "translateY(0) !important",
                "box-shadow": "0 2px 8px rgba(30, 136, 229, 0.3) !important"
            },
            ".exam-login-btn .cxd-Button:disabled": {
                "opacity": "0.7 !important",
                "transform": "none !important",
                "cursor": "not-allowed !important"
            },
            // 优化表单验证错误样式
            ".cxd-Form-feedback": {
                "font-size": "12px !important",
                "margin-top": "4px !important",
                "color": "#ff4d4f !important",
                "display": "flex !important",
                "align-items": "center !important"
            },
            ".cxd-Form-feedback:before": {
                "content": "'⚠️' !important",
                "margin-right": "4px !important",
                "font-size": "12px !important"
            },
            ".has-error .exam-input": {
                "border-color": "#ff4d4f !important",
                "box-shadow": "0 0 0 2px rgba(255, 77, 79, 0.2) !important"
            },
            ".has-error .exam-input:focus": {
                "border-color": "#ff4d4f !important",
                "box-shadow": "0 0 0 3px rgba(255, 77, 79, 0.2) !important"
            }
        }
    };

    // 初始化考试系统登录页面
    initializeExamLogin();

    /**
     * 初始化考试系统登录页面
     * 1. 获取租户配置信息
     * 2. 应用租户主题
     * 3. 渲染登录表单
     */
    async function initializeExamLogin() {
        try {
            showLoading(true);
            
            // 获取租户配置信息
            const tenantConfig = await fetchTenantConfig(window.tenantId);
            
            if (!tenantConfig) {
                showError('租户不存在或已停用');
                return;
            }
            
            // 保存租户配置到全局
            window.tenantConfig = tenantConfig;
            
            // 应用租户主题（针对考试系统）
            applyExamTheme(tenantConfig.themeConfig);
            
            // 渲染登录页面
            renderExamLoginPage(tenantConfig);
            
            showLoading(false);
            
        } catch (error) {
            console.error('初始化考试系统登录页面失败:', error);
            showError('加载失败，请刷新页面重试');
        }
    }

    /**
     * 获取租户配置信息
     * @param {string} tenantId 租户ID
     * @returns {Promise<Object>} 租户配置
     */
    async function fetchTenantConfig(tenantId) {
        if (!tenantId) {
            throw new Error('租户ID不能为空');
        }
        
        try {
            const response = await fetch(`/identity/api/identity/tenants/${tenantId}/login-config`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Forwarded-With': 'CodeSpirit'
                }
            });
            
            // 处理HTTP错误状态
            if (response.status === 404) {
                throw new Error('租户不存在或已停用');
            } else if (response.status === 403) {
                throw new Error('您没有权限访问此租户');
            } else if (response.status >= 500) {
                throw new Error('服务器内部错误，请稍后重试');
            } else if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            
            // 处理业务错误
            if (result.status !== 0) {
                throw new Error(result.msg || '获取租户配置失败');
            }
            
            return result.data;
        } catch (error) {
            console.error('获取租户配置失败:', error);
            throw error;
        }
    }

    /**
     * 应用考试系统主题
     * @param {string|Object} themeConfig 主题配置
     */
    function applyExamTheme(themeConfig) {
        if (!themeConfig) return;
        
        try {
            const theme = typeof themeConfig === 'string' ? JSON.parse(themeConfig) : themeConfig;
            const root = document.documentElement;
            
            // 应用考试系统特定的主题颜色
            if (theme.primaryColor) {
                root.style.setProperty('--exam-primary-color', theme.primaryColor);
                root.style.setProperty('--exam-primary-gradient', 
                    `linear-gradient(135deg, ${theme.primaryColor} 0%, ${theme.secondaryColor || theme.primaryColor} 100%)`);
            }
            
            if (theme.secondaryColor) {
                root.style.setProperty('--exam-secondary-color', theme.secondaryColor);
            }
            
            if (theme.accentColor) {
                root.style.setProperty('--exam-accent-color', theme.accentColor);
            }
            
            if (theme.backgroundColor) {
                root.style.setProperty('--exam-bg-gradient', 
                    `linear-gradient(135deg, ${theme.backgroundColor} 0%, ${theme.primaryColor || '#1e88e5'} 100%)`);
            }
            
            // 应用自定义CSS
            if (theme.customCss) {
                let style = document.getElementById('exam-custom-style');
                if (style) {
                    style.remove();
                }
                style = document.createElement('style');
                style.id = 'exam-custom-style';
                style.textContent = theme.customCss;
                document.head.appendChild(style);
            }
            
            // 更新页面标题和图标
            if (theme.title) {
                document.title = `${theme.title} - 考试系统登录`;
            }
            
            if (theme.favicon) {
                updateFavicon(theme.favicon);
            }
            
            console.log('考试系统主题应用成功');
            
        } catch (error) {
            console.warn('应用考试系统主题失败:', error);
        }
    }

    /**
     * 更新网站图标
     * @param {string} faviconUrl 图标URL
     */
    function updateFavicon(faviconUrl) {
        const link = document.querySelector("link[rel*='icon']") || document.createElement('link');
        link.type = 'image/x-icon';
        link.rel = 'shortcut icon';
        link.href = faviconUrl;
        document.getElementsByTagName('head')[0].appendChild(link);
    }

    /**
     * 构建考试系统品牌信息模板
     * @returns {string} HTML模板
     */
    function buildExamBrandingTpl() {
        const config = window.tenantConfig || {};
        const logoUrl = config.logoUrl || '/logo.png';
        const displayName = config.displayName || config.name || '考试系统';
        
        return `
            <div class='exam-logo text-center'>
                <img src='${logoUrl}' alt='${displayName}' 
                     onerror="this.src='/logo.png'" 
                     loading="lazy" />
                <h2>${displayName}<span class='exam-badge'>考试</span></h2>
                <p class='exam-subtitle'>
                    <i class='fa fa-graduation-cap'></i>
                    安全考试环境 - 请使用您的账户凭据登录
                </p>
            </div>
        `;
    }

    /**
     * 构建考试系统页脚模板
     * @returns {string} HTML模板
     */
    function buildExamFooterTpl() {
        const config = window.tenantConfig || {};
        const currentYear = new Date().getFullYear();
        const companyName = config.displayName || config.name || '考试系统';
        
        return `
            <div class='exam-footer-content'>
                <p>&copy; ${currentYear} ${companyName} 考试平台 版权所有</p>
                <div class='exam-footer-links'>
                    <a href='/${window.tenantId}/exam/help' title='查看考试帮助'>
                        <i class='fa fa-question-circle'></i> 考试帮助
                    </a>
                    <span class='separator'>|</span>
                    <a href='/${window.tenantId}/exam/contact' title='技术支持'>
                        <i class='fa fa-headset'></i> 技术支持
                    </a>
                    <span class='separator'>|</span>
                    <a href='/${window.tenantId}/admin' title='返回管理平台'>
                        <i class='fa fa-arrow-left'></i> 返回管理平台
                    </a>
                </div>
            </div>
        `;
    }

    /**
     * 渲染考试登录页面
     * @param {Object} tenantConfig 租户配置
     */
    function renderExamLoginPage(tenantConfig) {
        // 隐藏加载状态，显示登录表单
        document.getElementById('loading').style.display = 'none';
        
        // 初始化amis
        let amisInstance = amis.embed(
            '#root',
            amisJSON,
            {
                locale: 'zh-CN',
                data: {
                    tenantId: window.tenantId,
                    tenantName: window.tenantName,
                    tenantConfig: tenantConfig
                }
            },
            {
                requestAdaptor: (api) => {
                    return {
                        ...api,
                        headers: {
                            ...api.headers,
                            'Content-Type': 'application/json',
                            'TenantId': window.tenantId,
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    };
                },
                responseAdaptor: function (api, payload, query, request, response) {
                    // 处理HTTP错误响应
                    if (response.status === 401) {
                        return {
                            status: 1,
                            msg: '用户名或密码错误',
                            data: null
                        };
                    } else if (response.status === 403) {
                        return {
                            status: 1,
                            msg: '您没有权限访问考试系统',
                            data: null
                        };
                    } else if (response.status === 404) {
                        return {
                            status: 1,
                            msg: '租户不存在或已停用',
                            data: null
                        };
                    } else if (response.status >= 500) {
                        return {
                            status: 1,
                            msg: '服务器内部错误，请稍后重试',
                            data: null
                        };
                    } else if (response.status >= 400 && response.status < 500) {
                        return {
                            status: 1,
                            msg: payload.msg || '请求失败',
                            data: null
                        };
                    }
                    
                    // 处理成功响应
                    if (payload && typeof payload === 'object') {
                        // 统一响应格式
                        if (payload.status === undefined && payload.data !== undefined) {
                            return {
                                status: 0,
                                data: payload.data,
                                msg: payload.msg || '成功'
                            };
                        }
                        
                        // 处理登录成功的特殊情况
                        if (api.url && api.url.includes('/auth/client/login') && payload.status === 0) {
                            return {
                                status: 0,
                                data: payload.data,
                                msg: payload.msg || '登录成功'
                            };
                        }
                    }
                    
                    return payload;
                },
                theme: 'antd'
            }
        );
        
        // 添加考试系统特定的安全检查
        initExamSecurityEnhancements();
    }

    /**
     * 显示/隐藏加载状态
     * @param {boolean} show 是否显示
     */
    function showLoading(show) {
        const loadingEl = document.getElementById('loading');
        if (loadingEl) {
            loadingEl.style.display = show ? 'flex' : 'none';
        }
    }

    /**
     * 显示错误信息
     * @param {string} message 错误消息
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="exam-error">
                <div class="error-content">
                    <div class="error-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3>🚫 无法访问考试系统</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重新加载
                        </button>
                        <a href="/${window.tenantId}/admin" class="btn btn-secondary">
                            <i class="fa fa-arrow-left"></i> 返回管理平台
                        </a>
                    </div>
                    <div class="error-tips">
                        <p>💡 遇到问题？</p>
                        <ul>
                            <li>检查网络连接是否正常</li>
                            <li>确认您有考试系统的访问权限</li>
                            <li>联系管理员获取帮助</li>
                        </ul>
                    </div>
                </div>
            </div>
        `;
        
        document.getElementById('root').innerHTML = errorHTML;
    }

    /**
     * 初始化考试系统安全增强
     */
    function initExamSecurityEnhancements() {
        // 考试环境特有的安全检查
        detectDeveloperTools();
        initFullscreenMonitoring();
        initFocusMonitoring();
        initCopyPasteProtection();
    }

    /**
     * 检测开发者工具
     */
    function detectDeveloperTools() {
        let devtools = {open: false, orientation: null};
        let threshold = 160;
        
        setInterval(function() {
            if (window.outerHeight - window.innerHeight > threshold || 
                window.outerWidth - window.innerWidth > threshold) {
                if (!devtools.open) {
                    devtools.open = true;
                    console.warn('⚠️ 检测到开发者工具已打开！考试过程中请关闭开发者工具以确保考试公平性。');
                    // 可以在这里添加更严格的处理，比如记录违规行为
                }
            } else {
                devtools.open = false;
            }
        }, 500);
    }

    /**
     * 初始化全屏监控
     */
    function initFullscreenMonitoring() {
        let fullscreenWarningShown = false;
        
        document.addEventListener('fullscreenchange', function() {
            if (!document.fullscreenElement && !fullscreenWarningShown) {
                fullscreenWarningShown = true;
                setTimeout(() => {
                    console.warn('建议在全屏模式下进行考试以获得最佳体验。');
                    fullscreenWarningShown = false;
                }, 1000);
            }
        });
    }

    /**
     * 初始化焦点监控
     */
    function initFocusMonitoring() {
        let focusWarningShown = false;
        
        window.addEventListener('blur', function() {
            if (window.isExamLogin && !focusWarningShown) {
                focusWarningShown = true;
                setTimeout(() => {
                    console.warn('⚠️ 请保持专注于考试系统，避免切换到其他窗口或应用。');
                    focusWarningShown = false;
                }, 1000);
            }
        });
    }

    /**
     * 初始化复制粘贴保护
     */
    function initCopyPasteProtection() {
        // 禁用选择文本
        document.addEventListener('selectstart', function(e) {
            if (window.isExamLogin) {
                e.preventDefault();
                return false;
            }
        });
        
        // 禁用复制粘贴
        document.addEventListener('copy', function(e) {
            if (window.isExamLogin) {
                e.preventDefault();
                return false;
            }
        });
        
        document.addEventListener('paste', function(e) {
            if (window.isExamLogin) {
                e.preventDefault();
                return false;
            }
        });
    }

    // ===== 全局函数定义 (供AMIS事件调用) =====
    
    /**
     * 处理登录成功
     * @param {Object} data 登录返回数据
     */
    window.handleLoginSuccess = function(data) {
        try {
            // 显示成功提示
            showSuccessToast('登录成功！正在进入考试系统...');
            
            // 保存认证信息
            if (data.token) {
                TokenManager.setToken(data.token, 24);
                TokenManager.setClientType('exam');
            }
            
            // 保存用户信息
            if (data.user) {
                TokenManager.setUserInfo(data.user);
            }
            
            // 跳转到目标页面
            setTimeout(() => {
                const urlParams = new URLSearchParams(window.location.search);
                const redirectUrl = urlParams.get('redirect');
                
                if (redirectUrl) {
                    window.location.href = decodeURIComponent(redirectUrl);
                } else {
                    window.location.href = `/${window.tenantId}/exam/`;
                }
            }, 1500);
            
        } catch (error) {
            console.error('处理登录成功回调失败:', error);
            showErrorToast('登录处理失败，请重试');
        }
    };

    /**
     * 处理登录错误
     * @param {Object} errorData 错误数据
     */
    window.handleLoginError = function(errorData) {
        try {
            let errorMessage = '登录失败，请检查用户名和密码';
            
            if (errorData && errorData.msg) {
                errorMessage = errorData.msg;
            } else if (errorData && errorData.message) {
                errorMessage = errorData.message;
            }
            
            // 显示友好的错误信息
            if (errorMessage.includes('用户名') || errorMessage.includes('密码')) {
                errorMessage = '用户名或密码错误，请重新输入';
            } else if (errorMessage.includes('锁定') || errorMessage.includes('禁用')) {
                errorMessage = '账户已被锁定或禁用，请联系管理员';
            } else if (errorMessage.includes('权限')) {
                errorMessage = '您没有访问考试系统的权限，请联系管理员';
            }
            
            showErrorToast(errorMessage);
            
            // 记录错误日志
            console.error('考试系统登录失败:', errorData);
            
        } catch (error) {
            console.error('处理登录错误回调失败:', error);
            showErrorToast('登录失败，请重试');
        }
    };

    /**
     * 清除表单错误
     */
    window.clearFormErrors = function() {
        try {
            const errorElements = document.querySelectorAll('.has-error, .text-danger, .cxd-Form-feedback');
            errorElements.forEach(el => {
                el.classList.remove('has-error', 'text-danger');
                if (el.classList.contains('cxd-Form-feedback')) {
                    el.style.display = 'none';
                }
            });
        } catch (error) {
            console.warn('清除表单错误失败:', error);
        }
    };

    /**
     * 处理验证错误
     * @param {Object} errorData 验证错误数据
     */
    window.handleValidationErrors = function(errorData) {
        try {
            console.warn('表单验证失败:', errorData);
            
            // 如果有具体的字段错误，不显示全局提示
            if (errorData && errorData.errors && Object.keys(errorData.errors).length > 0) {
                return;
            }
            
            showErrorToast('请检查表单输入是否正确');
        } catch (error) {
            console.warn('处理验证错误失败:', error);
        }
    };

    /**
     * 显示成功提示
     * @param {string} message 提示信息
     */
    function showSuccessToast(message) {
        const toast = createToast(message, 'success');
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);
        
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    /**
     * 显示错误提示
     * @param {string} message 错误信息
     */
    function showErrorToast(message) {
        const toast = createToast(message, 'error');
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);
        
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 5000);
    }

    /**
     * 创建提示框元素
     * @param {string} message 提示信息
     * @param {string} type 提示类型 success|error
     * @returns {HTMLElement} 提示框元素
     */
    function createToast(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `exam-toast exam-toast-${type}`;
        
        const icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';
        const iconColor = type === 'success' ? '#52c41a' : '#ff4d4f';
        
        toast.innerHTML = `
            <div class="toast-content">
                <i class="fa ${icon}" style="color: ${iconColor}; margin-right: 8px;"></i>
                <span>${message}</span>
            </div>
        `;
        
        return toast;
    }

    // 页面安全性检查
    function initSecurityChecks() {
        // 禁用右键菜单
        document.addEventListener('contextmenu', function(e) {
            if (window.isExamLogin) {
                e.preventDefault();
                return false;
            }
        });
        
        // 禁用常见快捷键
        document.addEventListener('keydown', function(e) {
            if (window.isExamLogin) {
                // 禁用F12, Ctrl+Shift+I, Ctrl+U等
                if (e.key === 'F12' || 
                    (e.ctrlKey && e.shiftKey && e.key === 'I') ||
                    (e.ctrlKey && e.key === 'u')) {
                    e.preventDefault();
                    return false;
                }
            }
        });
        
        // 检测窗口焦点变化
        let focusWarningShown = false;
        window.addEventListener('blur', function() {
            if (window.isExamLogin && !focusWarningShown) {
                focusWarningShown = true;
                setTimeout(() => {
                    alert('请保持专注于考试系统，避免切换到其他窗口或应用。');
                    focusWarningShown = false;
                }, 1000);
            }
        });
    }
    
    // 初始化安全检查
    initSecurityChecks();
    
})(); 