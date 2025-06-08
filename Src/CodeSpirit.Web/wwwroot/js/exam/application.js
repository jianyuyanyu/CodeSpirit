/**
 * 考试应用页面 - 基于AMIS框架
 * 包含租户信息、考生信息、公告通知、导航菜单等功能
 * 支持移动端适配，前端获取用户信息
 */
(function () {
    'use strict';
    
    // 确保TokenManager已初始化
    TokenManager.initClientMode(window.tenantId, 'exam');
    
    // 检查用户认证状态
    if (!TokenManager.isAuthenticated()) {
        window.location.href = `/${window.tenantId}/exam/login`;
        return;
    }
    
    let amis = amisRequire('amis/embed');
    let amisInstance = null;
    
    // 全局数据存储
    const appData = {
        tenant: { id: '', name: '', logo: '', description: '' },
        student: { 
            name: '', 
            idCard: '', 
            gender: '', 
            examNumber: '', 
            studentId: '', 
            phone: '', 
            avatar: '',
            displayName: ''
        },
        announcements: []
    };
    
    /**
     * 通用API请求函数
     */
    async function apiRequest(url, options = {}) {
        try {
            const token = TokenManager.getToken();
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Authorization': token ? 'Bearer ' + token : '',
                    'TenantId': window.tenantId,
                    'X-Forwarded-With': 'CodeSpirit',
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });
            
            if (response.status === 401) {
                window.location.href = `/${window.tenantId}/exam/login`;
                throw new Error('认证失败，请重新登录');
            }
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const result = await response.json();
            if (result.status !== 0) {
                throw new Error(result.msg || '请求失败');
            }
            
            return result.data;
        } catch (error) {
            console.error(`API请求失败 [${url}]:`, error);
            throw error;
        }
    }
    
    /**
     * 构建AMIS页面配置
     */
    function buildPageConfig() {
        return {
            type: "page",
            title: "",
            className: "exam-app-page",
            data: {
                // 初始数据，将通过API更新
                tenant: appData.tenant,
                student: appData.student,
                announcements: appData.announcements,
                now: new Date()
            },
            body: [
                // 租户信息条
                {
                    type: "container",
                    className: "tenant-info-bar",
                    body: [
                        {
                            type: "tpl",
                            tpl: "<div class='tenant-name'><i class='fa fa-building'></i> ${tenant.name || '考试平台'}</div>"
                        },
                        {
                            type: "tpl", 
                            tpl: "<div class='current-time'><i class='fa fa-clock-o'></i> ${now | date:'HH:mm'}</div>",
                            className: "current-time"
                        }
                    ]
                },
                
                // 考生信息卡片
                {
                    type: "container",
                    className: "student-info-card",
                    body: [
                        {
                            type: "flex",
                            justify: "flex-start",
                            alignItems: "center",
                            items: [
                                // 头像区域
                                {
                                    type: "container",
                                    className: "flex-shrink-0",
                                    body: [
                                        // 头像图片（如果有的话）
                                        {
                                            type: "avatar",
                                            src: "${student.avatar}",
                                            text: "${student.displayName || student.name || '用户' | substring:0:1}",
                                            className: "student-avatar",
                                            size: 60
                                        }
                                    ]
                                },
                                // 信息区域
                                {
                                    type: "container",
                                    className: "flex-grow-1 ml-3",
                                    body: [
                                        {
                                            type: "grid",
                                            columns: [
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>姓名:</span><span class='info-value'>\${student.displayName || student.name || '未知'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>性别:</span><span class='info-value'>\${student.gender || '未知'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>准考证:</span><span class='info-value'>\${student.examNumber || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>学号:</span><span class='info-value'>\${student.studentId || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>手机:</span><span class='info-value'>\${student.phone || '未设置'}</span></div>"
                                                    }
                                                },
                                                {
                                                    md: 6,
                                                    body: {
                                                        type: "tpl",
                                                        tpl: "<div class='student-info-item'><span class='info-label'>身份证:</span><span class='info-value'>\${student.idCard | truncate:10:'***' || '未设置'}</span></div>"
                                                    }
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                
                // 公告通知区域
                {
                    type: "container",
                    className: "announcement-section",
                    body: [
                        {
                            type: "container",
                            className: "announcement-header",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<i class='fa fa-bullhorn'></i> 公告通知"
                                }
                            ]
                        },
                        {
                            type: "container",
                            className: "announcement-content",
                            body: [
                                {
                                    type: "each",
                                    name: "announcements",
                                    items: {
                                        type: "container",
                                        className: "announcement-item",
                                        body: [
                                            {
                                                type: "flex",
                                                justify: "space-between",
                                                alignItems: "flex-start",
                                                items: [
                                                    {
                                                        type: "container",
                                                        className: "flex-grow-1",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='announcement-title'>\${title}</div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='announcement-content-text'>\${content}</div>"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        type: "tpl",
                                                        tpl: "<span class='announcement-time'>\${publishTime | date:'MM-DD HH:mm'}</span>"
                                                    }
                                                ]
                                            }
                                        ]
                                    },
                                    placeholder: {
                                        type: "container",
                                        className: "announcement-empty",
                                        body: [
                                            {
                                                type: "tpl",
                                                tpl: "<i class='fa fa-info-circle'></i><div>暂无公告通知</div>"
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                },
                
                // 导航菜单
                {
                    type: "container",
                    className: "nav-menu-section",
                    body: [
                        {
                            type: "html",
                            html: `
                                <div class="nav-menu-grid">
                                    <div class="nav-menu-item nav-practice" onclick="window.navigateTo('practice')">
                                        <i class="fa fa-pencil nav-menu-icon"></i>
                                        <div class="nav-menu-text">开始练习</div>
                                    </div>
                                    <div class="nav-menu-item nav-exam" onclick="window.navigateTo('exam')">
                                        <i class="fa fa-graduation-cap nav-menu-icon"></i>
                                        <div class="nav-menu-text">开始考试</div>
                                    </div>
                                    <div class="nav-menu-item nav-my-exams" onclick="window.navigateTo('my-exams')">
                                        <i class="fa fa-file-text nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的考试</div>
                                    </div>
                                    <div class="nav-menu-item nav-my-practice" onclick="window.navigateTo('my-practice')">
                                        <i class="fa fa-history nav-menu-icon"></i>
                                        <div class="nav-menu-text">我的练习</div>
                                    </div>
                                </div>
                            `
                        }
                    ]
                }
            ]
        };
    }
    
    /**
     * 导航到指定页面
     */
    window.navigateTo = function(page) {
        const routes = {
            'practice': `/${window.tenantId}/exam/practice`,
            'exam': `/${window.tenantId}/exam`,
            'my-exams': `/${window.tenantId}/exam/history`,
            'my-practice': `/${window.tenantId}/exam/practice-history`
        };
        
        if (routes[page]) {
            window.location.href = routes[page];
        }
    };
    
    /**
     * 加载初始数据
     */
    async function loadInitialData() {
        try {
            // 首先加载租户信息，如果失败则不继续
            const tenantInfo = await loadTenantInfo();
            Object.assign(appData.tenant, tenantInfo);
            
            // 然后并行加载其他数据
            const [studentInfo, announcements] = await Promise.all([
                loadStudentInfo().catch(error => {
                    console.warn('加载学生信息失败:', error);
                    return { name: '未知用户', displayName: '未知用户' };
                }),
                loadAnnouncements().catch(error => {
                    console.warn('加载公告失败:', error);
                    return [];
                })
            ]);
            
            // 更新应用数据
            Object.assign(appData.student, studentInfo);
            appData.announcements = announcements;
            
            // 更新AMIS数据
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        ...appData,
                        now: new Date()
                    }
                });
            }
            
        } catch (error) {
            console.error('加载初始数据失败:', error);
            
            // 如果是租户相关错误，显示更明确的错误信息
            if (error.message.includes('租户')) {
                showError(error.message);
            } else {
                showError('加载数据失败，请刷新页面重试');
            }
        }
    }
    
    /**
     * 加载租户信息
     * 参考login.js中的实现方式
     */
    async function loadTenantInfo() {
        try {
            const response = await fetch(`/identity/api/identity/tenants/${window.tenantId}/login-config`, {
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
            
            // 转换为应用页面需要的格式
            const tenantConfig = result.data;
            return {
                id: window.tenantId,
                name: tenantConfig.displayName || tenantConfig.name || '考试平台',
                logo: tenantConfig.logoUrl || '/logo.png',
                description: tenantConfig.description || ''
            };
        } catch (error) {
            console.warn('加载租户信息失败:', error);
            return { 
                id: window.tenantId,
                name: '考试平台', 
                logo: '/logo.png',
                description: '' 
            };
        }
    }
    
    /**
     * 加载考生信息 - 前端获取
     */
    async function loadStudentInfo() {
        try {
            const profile = await apiRequest('/identity/api/identity/profile');
            return {
                name: profile.userName || profile.name || '',
                displayName: profile.displayName || profile.userName || profile.name || '',
                idCard: profile.idCard || '',
                gender: profile.gender || '',
                examNumber: profile.examNumber || '',
                studentId: profile.studentId || profile.employeeId || '',
                phone: profile.phone || profile.phoneNumber || '',
                avatar: profile.avatar || ''
            };
        } catch (error) {
            console.warn('加载考生信息失败:', error);
            return { 
                name: '未知用户',
                displayName: '未知用户'
            };
        }
    }
    
    /**
     * 加载公告信息（模拟数据）
     */
    async function loadAnnouncements() {
        try {
            // 尝试从API加载公告
            // return await apiRequest('/exam/api/announcements');
            
            // 暂时使用模拟数据
            return [
                {
                    id: 1,
                    title: "重要通知：考试系统维护",
                    content: "系统将在本周日凌晨2:00-4:00进行维护，请合理安排考试时间。",
                    publishTime: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
                    priority: "high"
                },
                {
                    id: 2,
                    title: "考试规则提醒",
                    content: "请考生严格遵守考试纪律，诚信考试，违规行为将被记录。",
                    publishTime: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000),
                    priority: "normal"
                },
                {
                    id: 3,
                    title: "技术支持联系方式",
                    content: "如遇技术问题，请联系技术支持：400-123-4567",
                    publishTime: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
                    priority: "normal"
                }
            ];
        } catch (error) {
            console.warn('加载公告信息失败:', error);
            return [];
        }
    }
    

    
    /**
     * 显示错误信息
     * 参考login.js中的错误显示方式
     */
    function showError(message) {
        showLoading(false);
        
        const errorHTML = `
            <div class="exam-error">
                <div class="error-content">
                    <div class="error-icon">
                        <i class="fa fa-exclamation-triangle"></i>
                    </div>
                    <h3>🚫 无法加载考试应用</h3>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button onclick="location.reload()" class="btn btn-primary">
                            <i class="fa fa-refresh"></i> 重新加载
                        </button>
                        <a href="/${window.tenantId}/exam" class="btn btn-secondary">
                            <i class="fa fa-arrow-left"></i> 返回考试首页
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
     * 显示/隐藏加载状态
     */
    function showLoading(show) {
        const loading = document.getElementById('loading');
        if (loading) {
            loading.style.display = show ? 'flex' : 'none';
        }
    }
    
    /**
     * 初始化页面
     */
    async function initPage() {
        try {
            showLoading(true);
            
            // 构建AMIS配置
            const amisConfig = buildPageConfig();
            
            // 初始化AMIS
            amisInstance = amis.embed('#root', amisConfig, {
                location: history.location,
                data: {
                    ...appData,
                    now: new Date()
                },
                context: {
                    WEB_HOST: window.webHost,
                    TENANT_ID: window.tenantId
                },
                requestAdaptor: (api) => {
                    const token = TokenManager.getToken();
                    return {
                        ...api,
                        headers: {
                            ...api.headers,
                            'Authorization': token ? 'Bearer ' + token : '',
                            'TenantId': window.tenantId,
                            'X-Forwarded-With': 'CodeSpirit'
                        }
                    };
                }
            }, {
                theme: 'antd',
                locale: 'zh-CN'
            });
            
            // 加载数据
            await loadInitialData();
            
            // 设置定时更新时间显示
            setInterval(() => {
                if (amisInstance) {
                    amisInstance.updateProps({
                        data: {
                            now: new Date()
                        }
                    });
                }
            }, 30000); // 每30秒更新一次时间
            
        } catch (error) {
            console.error('页面初始化失败:', error);
            showError('页面加载失败，请刷新重试');
        } finally {
            showLoading(false);
        }
    }
    
    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initPage);
    } else {
        initPage();
    }
    
})(); 