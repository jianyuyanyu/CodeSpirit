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
                        // Logo和标题
                        {
                            "type": "tpl",
                            "tpl": "<div class='exam-logo text-center'><img src='${tenantConfig.logoUrl}' alt='${tenantDisplayName}' onerror=\"this.src='/logo.png'\" loading='lazy' /><h2>${tenantDisplayName}<span class='exam-badge'>2.0</span></h2><p class='exam-subtitle'><i class='fa fa-graduation-cap'></i>安全考试环境 - 请使用您的账户凭据登录</p></div >",
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
     * 获取租户配置（使用缓存）
     * @param {string} tenantId - 租户ID
     * @returns {Promise<Object>} 租户配置数据
     */
    async function fetchTenantConfig(tenantId) {
        if (!tenantId) {
            throw new Error('租户ID不能为空');
        }
        
        try {
            // 使用缓存的登录配置接口
            return await window.ExamApiManager.getLoginConfig(tenantId);
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
        
        // 准备租户显示名称，确保正确编码
        let tenantDisplayName = tenantConfig.displayName || 
                               tenantConfig.name || 
                               window.tenantName || 
                               'default';
        
        // 初始化amis
        let amisInstance = amis.embed(
            '#root',
            amisJSON,
            {
                locale: 'zh-CN',
                data: {
                    tenantId: window.tenantId,
                    tenantName: window.tenantName,
                    tenantDisplayName: tenantDisplayName,
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
        
        // 添加考试系统特定的安全检查 (已禁用)
        // initExamSecurityEnhancements();
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
     * 初始化考试系统安全增强 (已禁用)
     */
    function initExamSecurityEnhancements() {
        // 考试环境特有的安全检查 (已禁用)
        // detectDeveloperTools();
        // initFullscreenMonitoring();
        // initFocusMonitoring();
        // initCopyPasteProtection();
    }

    /**
     * 检测开发者工具 (已禁用)
     */
    function detectDeveloperTools() {
        // 开发者工具检测已禁用
        /*
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
        */
    }

    /**
     * 初始化全屏监控 (已禁用)
     */
    function initFullscreenMonitoring() {
        // 全屏监控已禁用
        /*
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
        */
    }

    /**
     * 初始化焦点监控 (已禁用)
     */
    function initFocusMonitoring() {
        // 焦点监控已禁用
        /*
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
        */
    }

    /**
     * 初始化复制粘贴保护 (已禁用)
     */
    function initCopyPasteProtection() {
        // 复制粘贴保护已禁用
        /*
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
        */
    }

    // ===== 全局函数定义 (供AMIS事件调用) =====
    
    /**
     * 处理登录成功
     * @param {Object} data 登录返回数据
     */
    window.handleLoginSuccess = function(data) {
        try {
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
                    window.location.href = `/${window.tenantId}/exam/app`;
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

    // 页面安全性检查 (已禁用)
    function initSecurityChecks() {
        // 安全性检查已禁用
        /*
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
        */
    }
    
    // 初始化安全检查 (已禁用)
    // initSecurityChecks();
    
})(); 