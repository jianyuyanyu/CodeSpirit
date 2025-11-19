/**
 * 我的考试页面 - 基于AMIS框架
 * 专注于显示和管理可参加的考试
 * 支持移动端适配
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
            displayName: '',
            studentGroups: [],
            userId: 0,
            candidateId: 0
        },
        availableExams: []
    };
    
    /**
     * 通用API请求函数
     * 使用ExamApiManager进行统一的API请求处理
     */
    async function apiRequest(url, options = {}) {
        return window.ExamApiManager.request(url, options);
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
                availableExams: appData.availableExams,
                now: new Date()
            },
            body: [
                // 租户信息条
                {
                    type: "container",
                    className: "tenant-info-bar",
                    body: [
                        {
                            type: "flex",
                            justify: "flex-start",
                            alignItems: "center",
                            className: "tenant-info-content",
                            items: [
                                {
                                    type: "tpl",
                                    tpl: "<div class='tenant-name'><i class='fa fa-building'></i> ${tenant.name || '考试平台'}</div>"
                                },
                                {
                                    type: "tpl", 
                                    tpl: "<div class='current-time'><i class='fa fa-clock-o'></i> ${now | date:'HH:mm'}</div>",
                                    className: "current-time"
                                },
                                {
                                    type: "html",
                                    html: `
                                        <div class="logout-container">
                                            <div class="logout-btn" onclick="window.handleLogout()" title="安全退出">
                                                <i class="fa fa-sign-out logout-icon"></i>
                                                <span class="logout-text">退出</span>
                                            </div>
                                        </div>
                                    `
                                }
                            ]
                        }
                    ]
                },
                
                // 考生信息卡片 - 优化布局
                {
                    type: "container",
                    className: "student-info-card",
                    body: [
                        // 卡片标题
                        {
                            type: "tpl",
                            tpl: "<div class='card-header'><i class='fa fa-user-circle'></i> 考生信息</div>"
                        },
                        // 头像区域（居中）
                        {
                            type: "container",
                            className: "student-avatar-section",
                            body: [
                                {
                                    type: "avatar",
                                    src: "${student.avatar}",
                                    text: "${student.displayName || student.name || '用户' | substring:0:1}",
                                    className: "student-avatar",
                                    size: 80
                                }
                            ]
                        },
                        // 信息网格（对称布局）
                        {
                            type: "flex",
                            className: "student-info-grid",
                            justify: "flex-start",
                            alignItems: "flex-start",
                            items: [
                                {
                                    type: "container",
                                    className: "student-info-column student-info-left-column",
                                    body: [
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-user info-icon'></i><span class='info-label'>姓名</span><span class='info-value'>\${student.displayName || student.name || '未知'}</span></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-id-card info-icon'></i><span class='info-label'>准考证</span><span class='info-value exam-number-link'>\${student.examNumber || '未设置'}</span></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-phone info-icon'></i><span class='info-label'>手机</span><span class='info-value'>\${student.phone || '未设置'}</span></div>"
                                        }
                                    ]
                                },
                                {
                                    type: "container",
                                    className: "student-info-column student-info-right-column",
                                    body: [
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-venus-mars info-icon'></i><span class='info-label'>性别</span><span class='info-value'>\${student.gender || '未知'}</span></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-graduation-cap info-icon'></i><span class='info-label'>学号</span><span class='info-value'>\${student.studentId || '未设置'}</span></div>"
                                        },
                                        {
                                            type: "tpl",
                                            tpl: "<div class='student-info-item'><i class='fa fa-id-card-o info-icon'></i><span class='info-label'>身份证</span><span class='info-value'>\${student.idCard || '未设置'}</span></div>"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                
                // 当前考试面板
                {
                    type: "container",
                    className: "current-exams-section",
                    visibleOn: "${availableExams && availableExams.length > 0}",
                    body: [
                        {
                            type: "container",
                            className: "current-exams-header",
                            body: [
                                {
                                    type: "tpl",
                                    tpl: "<i class='fa fa-graduation-cap'></i> 当前考试"
                                }
                            ]
                        },
                        {
                            type: "container",
                            className: "current-exams-content",
                            body: [
                                {
                                    type: "each",
                                    name: "availableExams",
                                    items: {
                                        type: "container",
                                        className: "exam-item",
                                        body: [
                                            {
                                                type: "flex",
                                                justify: "space-between",
                                                alignItems: "center",
                                                items: [
                                                    {
                                                        type: "container",
                                                        className: "flex-grow-1",
                                                        body: [
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-title'>\${name}</div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-info'><span class='exam-time'><i class='fa fa-clock-o'></i> \${startTimeFormatted || startTime} - \${endTimeFormatted || endTime}</span><span class='exam-duration'><i class='fa fa-hourglass-half'></i> \${duration}分钟</span><span class='exam-score'><i class='fa fa-star'></i> \${totalScore}分</span></div>"
                                                            },
                                                            {
                                                                type: "tpl",
                                                                tpl: "<div class='exam-description'>\${description || '暂无描述'}</div>",
                                                                visibleOn: "${description}"
                                                            }
                                                        ]
                                                    },
                                                    {
                                                        type: "html",
                                                        html: `
                                                            <div class="exam-actions">
                                                                <div class="exam-status-badge \${status === '未开始' || status === '进行中' ? 'status-available' : (status === '已结束' ? 'status-ended' : 'status-unknown')}">
                                                                    <i class="fa fa-\${status === '进行中' ? 'hourglass-half' : (status === '未开始' ? 'clock-o' : 'check-circle')}"></i>
                                                                    \${status}
                                                                </div>
                                                                <button class="exam-start-btn" onclick="window.goToExamStart('\${id}')" 
                                                                        \${status === '未开始' || status === '进行中' ? '' : 'disabled'}>
                                                                    <i class="fa fa-\${status === '进行中' ? 'refresh' : 'play-circle'}"></i>
                                                                    <span>\${status === '未开始' ? '开始考试' : (status === '进行中' ? '继续考试' : '考试已结束')}</span>
                                                                </button>
                                                            </div>
                                                        `
                                                    }
                                                ]
                                            }
                                        ]
                                    },
                                    placeholder: {
                                        type: "container",
                                        className: "exam-empty",
                                        body: [
                                            {
                                                type: "tpl",
                                                tpl: `
                                                    <div class='empty-illustration'>
                                                        <i class='fa fa-calendar-o'></i>
                                                    </div>
                                                    <h3 class='empty-title'>暂无可参加的考试</h3>
                                                    <p class='empty-description'>当考试时间到了，您可参加的考试将会显示在这里</p>
                                                    <button class='empty-action' onclick='location.reload()'>
                                                        <i class='fa fa-refresh'></i> 刷新页面
                                                    </button>
                                                `
                                            }
                                        ]
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
    
    /**
     * 返回主页
     */
    window.goToHome = function() {
        window.location.href = `/${window.tenantId}/exam`;
    };
    
    /**
     * 处理退出登录 - 优化版本
     */
    window.handleLogout = function() {
        // 创建自定义确认对话框
        showLogoutConfirmDialog();
    };

    /**
     * 显示退出登录确认对话框
     */
    function showLogoutConfirmDialog() {
        // 创建遮罩层
        const overlay = document.createElement('div');
        overlay.className = 'logout-overlay';
        
        // 创建对话框
        const dialog = document.createElement('div');
        dialog.className = 'logout-dialog';
        dialog.innerHTML = `
            <div class="logout-dialog-header">
                <div class="logout-dialog-icon">
                    <i class="fa fa-sign-out"></i>
                </div>
                <h3>确认退出登录</h3>
            </div>
            <div class="logout-dialog-body">
                <p>🔒 您确定要退出当前账户吗？</p>
                <p class="logout-dialog-note">退出后需要重新输入账号密码才能登录</p>
            </div>
            <div class="logout-dialog-actions">
                <button class="logout-cancel-btn" onclick="window.closeLogoutDialog()">
                    <i class="fa fa-times"></i> 取消
                </button>
                <button class="logout-confirm-btn" onclick="window.confirmLogout()">
                    <i class="fa fa-sign-out"></i> 确认退出
                </button>
            </div>
        `;
        
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);
        
        // 添加动画效果
        setTimeout(() => {
            overlay.classList.add('show');
            dialog.classList.add('show');
        }, 10);
        
        // 点击遮罩关闭对话框
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                window.closeLogoutDialog();
            }
        });
        
        // ESC键关闭对话框
        const handleEsc = (e) => {
            if (e.key === 'Escape') {
                window.closeLogoutDialog();
                document.removeEventListener('keydown', handleEsc);
            }
        };
        document.addEventListener('keydown', handleEsc);
    }

    /**
     * 关闭退出登录对话框
     */
    window.closeLogoutDialog = function() {
        const overlay = document.querySelector('.logout-overlay');
        if (overlay) {
            overlay.classList.remove('show');
            setTimeout(() => {
                document.body.removeChild(overlay);
            }, 300);
        }
    };

    /**
     * 确认退出登录
     */
    window.confirmLogout = function() {
        try {
            // 关闭对话框
            window.closeLogoutDialog();
            
            // 显示退出动画
            const logoutBtn = document.querySelector('.logout-btn');
            if (logoutBtn) {
                logoutBtn.classList.add('logging-out');
                logoutBtn.innerHTML = '<i class="fa fa-spinner fa-spin logout-icon"></i><span class="logout-text">退出中...</span>';
            }
            
            // 页面渐出效果
            document.body.style.transition = 'all 0.5s ease';
            document.body.style.opacity = '0.7';
            document.body.style.transform = 'scale(0.98)';
            document.body.style.filter = 'blur(1px)';
            
            // 清除Token
            TokenManager.clearToken();
            
            // 延迟跳转到登录页
            setTimeout(() => {
                window.location.href = `/${window.tenantId}/exam/login`;
            }, 800);
            
        } catch (error) {
            console.error('退出登录失败:', error);
            // 即使出错也要跳转到登录页
            window.location.href = `/${window.tenantId}/exam/login`;
        }
    };
    
    /**
     * 跳转到考试开始页面
     * @param {string|number} examId 考试ID
     */
    function goToExamStart(examId) {
        if (!examId) {
            console.error('考试ID不能为空');
            return;
        }
        
        // 确保examId为字符串类型，去除可能的引号
        const cleanExamId = String(examId).replace(/['"]/g, '');
        const url = `/${window.tenantId}/exam/start/${cleanExamId}`;
        console.log('跳转到考试开始页面:', url, '考试ID:', cleanExamId);
        window.location.href = url;
    }

    // 将函数暴露到全局
    window.goToExamStart = goToExamStart;

    
    /**
     * 加载初始数据
     */
    async function loadInitialData() {
        try {
            // 首先加载租户信息，如果失败则不继续
            const tenantInfo = await loadTenantInfo();
            Object.assign(appData.tenant, tenantInfo);
            
            // 然后并行加载其他数据
            const [studentInfo, availableExams] = await Promise.all([
                loadStudentInfo().catch(error => {
                    console.warn('加载学生信息失败:', error);
                    return { 
                        name: '未知用户', 
                        displayName: '未知用户',
                        idCard: '',
                        gender: '',
                        examNumber: '',
                        studentId: '',
                        phone: '',
                        avatar: '',
                        studentGroups: [],
                        userId: 0,
                        candidateId: 0
                    };
                }),
                loadAvailableExams().catch(error => {
                    console.warn('加载可用考试失败:', error);
                    return [];
                })
            ]);
            
            // 更新应用数据
            Object.assign(appData.student, studentInfo);
            appData.availableExams = availableExams;
            
            // 更新AMIS数据
            if (amisInstance) {
                amisInstance.updateProps({
                    data: {
                        ...appData,
                        now: new Date()
                    }
                });
            }
            
            // 更新页面标题
            if (studentInfo.name) {
                document.title = `${studentInfo.name} - 考试管理平台`;
            }
            
            // 在控制台显示考生信息摘要
            console.log('👤 当前考生信息摘要:');
            console.log(`   姓名: ${studentInfo.name || '未设置'}`);
            console.log(`   学号: ${studentInfo.studentId || '未设置'}`);
            console.log(`   准考证: ${studentInfo.examNumber || '未设置'}`);
            console.log(`   考生组: ${studentInfo.studentGroups?.length ? studentInfo.studentGroups.join(', ') : '未分组'}`);
            console.log(`   用户ID: ${studentInfo.userId || '未知'}`);
            console.log(`   考生ID: ${studentInfo.candidateId || '未知'}`);
            
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
     * 加载租户信息（使用缓存）
     * 参考login.js中的实现方式
     */
    async function loadTenantInfo() {
        try {
            // 使用缓存的登录配置接口
            const tenantConfig = await window.ExamApiManager.getLoginConfig(window.tenantId);
            
            // 转换为应用页面需要的格式
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
     * 加载考生信息 - 使用考试API获取
     */
    async function loadStudentInfo() {
        try {
            console.log('🔍 正在从考试API获取考生信息...');
            const profile = await apiRequest('/exam/api/exam/client/profile');
            console.log('✅ 考生信息获取成功:', profile);
            
            const studentInfo = {
                name: profile.name || '',
                displayName: profile.name || '',
                idCard: profile.idNo || '',
                gender: profile.gender || '',
                examNumber: profile.admissionTicket || '',
                studentId: profile.studentNumber || '',
                phone: profile.phoneNumber || '',
                avatar: '', // API中没有头像字段
                studentGroups: profile.studentGroups || [],
                userId: profile.userId || 0,
                candidateId: profile.id || 0
            };
            
            console.log('📋 处理后的考生信息:', studentInfo);
            return studentInfo;
            
        } catch (error) {
            console.warn('⚠️ 考试API获取考生信息失败:', error);
            
            // 如果考试API失败，尝试使用身份API作为备用
            try {
                console.log('🔄 尝试使用身份API作为备用...');
                const identityProfile = await apiRequest('/identity/api/identity/profile');
                console.log('✅ 身份API获取成功:', identityProfile);
                
                const fallbackInfo = {
                    name: identityProfile.userName || identityProfile.name || '',
                    displayName: identityProfile.displayName || identityProfile.userName || identityProfile.name || '',
                    idCard: identityProfile.idCard || '',
                    gender: identityProfile.gender || '',
                    examNumber: identityProfile.examNumber || '',
                    studentId: identityProfile.studentId || identityProfile.employeeId || '',
                    phone: identityProfile.phone || identityProfile.phoneNumber || '',
                    avatar: identityProfile.avatar || '',
                    studentGroups: [],
                    userId: identityProfile.id || 0,
                    candidateId: 0
                };
                
                console.log('📋 备用API处理后的信息:', fallbackInfo);
                return fallbackInfo;
                
            } catch (fallbackError) {
                console.error('❌ 备用身份API也失败:', fallbackError);
                console.log('📋 使用默认用户信息');
                
                return { 
                    name: '未知用户',
                    displayName: '未知用户',
                    idCard: '',
                    gender: '',
                    examNumber: '',
                    studentId: '',
                    phone: '',
                    avatar: '',
                    studentGroups: [],
                    userId: 0,
                    candidateId: 0
                };
            }
        }
    }
    
    /**
     * 加载可用考试列表
     */
    async function loadAvailableExams() {
        try {
            const data = await apiRequest('/exam/api/exam/client/available');
            
            // 处理考试数据，优化时间显示格式
            const processedData = (data || []).map(exam => ({
                ...exam,
                // 格式化时间显示，将"2025-06-09 12:00:00"转换为"06-09 12:00"格式
                startTimeFormatted: formatExamTime(exam.startTime),
                endTimeFormatted: formatExamTime(exam.endTime),
                // 保留原始时间用于比较
                startTimeOriginal: exam.startTime,
                endTimeOriginal: exam.endTime
            }));
            
            console.log('📅 处理后的考试数据:', processedData);
            return processedData;
        } catch (error) {
            console.warn('加载可用考试失败:', error);
            return [];
        }
    }
    
    /**
     * 格式化考试时间显示
     * @param {string} timeString - 时间字符串，格式如"2025-06-09 12:00:00"
     * @returns {string} 格式化后的时间，如"06-09 12:00"
     */
    function formatExamTime(timeString) {
        if (!timeString) return '';
        
        try {
            // 解析时间字符串
            const date = new Date(timeString.replace(/-/g, '/'));  // 兼容性处理
            if (isNaN(date.getTime())) {
                return timeString; // 如果解析失败，返回原字符串
            }
            
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            const hour = String(date.getHours()).padStart(2, '0');
            const minute = String(date.getMinutes()).padStart(2, '0');
            
            return `${month}-${day} ${hour}:${minute}`;
        } catch (error) {
            console.warn('时间格式化失败:', timeString, error);
            return timeString;
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