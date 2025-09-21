/**
 * 工作流预览页面初始化脚本
 * 负责TokenManager初始化和工作流预览组件的启动
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log('工作流预览页面初始化开始');
    
    // 检查必要的数据是否存在
    if (!window.workflowPageData) {
        console.error('工作流页面数据不存在');
        showErrorMessage('页面数据加载失败，请刷新页面重试');
        return;
    }

    const { workflowId, tenantId, routeTenantId } = window.workflowPageData;
    
    // 验证工作流ID
    if (!workflowId) {
        console.error('工作流ID不存在');
        showErrorMessage('工作流ID无效，请检查URL参数');
        return;
    }

    // 获取有效的租户ID
    const effectiveTenantId = getEffectiveTenantId(tenantId, routeTenantId);
    
    if (!effectiveTenantId) {
        console.error('租户ID不存在，无法初始化TokenManager');
        showErrorMessage('租户信息获取失败，请重新登录');
        return;
    }

    // 将租户ID暴露到全局，供脚本使用
    window.currentTenantId = effectiveTenantId;
    console.log('工作流预览页面初始化 - 租户ID:', effectiveTenantId, '工作流ID:', workflowId);

    // 初始化TokenManager为租户模式
    TokenManager.initTenantMode(effectiveTenantId);
    
    // 检查用户是否已认证
    if (!TokenManager.isAuthenticated()) {
        console.warn('用户未认证，重定向到登录页面');
        const loginUrl = routeTenantId ? `/${routeTenantId}/login` : '/login';
        window.location.href = loginUrl;
        return;
    }

    const tenantInfo = TokenManager.getTenantInfo();
    console.log('用户已认证，租户信息:', tenantInfo);
    console.log('当前租户ID:', effectiveTenantId);
    
    // 初始化工作流预览组件
    if (window.WorkflowPreview) {
        new WorkflowPreview(workflowId);
    } else {
        console.error('WorkflowPreview类未加载');
        showErrorMessage('页面组件加载失败，请刷新页面重试');
    }
});

/**
 * 获取有效的租户ID
 * @param {string} tenantId 页面传递的租户ID
 * @param {string} routeTenantId 路由中的租户ID
 * @returns {string|null} 有效的租户ID
 */
function getEffectiveTenantId(tenantId, routeTenantId) {
    // 优先使用路由中的租户ID
    if (routeTenantId) {
        return routeTenantId;
    }
    
    // 其次使用页面传递的租户ID
    if (tenantId) {
        return tenantId;
    }
    
    // 从多个来源尝试获取租户ID
    return getTenantIdFromContext();
}

/**
 * 从上下文中获取租户ID
 * 返回租户ID字符串或null
 */
function getTenantIdFromContext() {
    // 方法1: 从URL路径中提取
    const pathMatch = window.location.pathname.match(/\/([^\/]+)\/approval\/workflow-preview/);
    if (pathMatch) {
        return pathMatch[1];
    }
    
    // 方法2: 从Cookie中获取
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        const [name, value] = cookie.trim().split('=');
        if (name === 'tenantId' || name === 'current-tenant') {
            return decodeURIComponent(value);
        }
    }
    
    // 方法3: 从localStorage中获取
    const storedTenantId = localStorage.getItem('currentTenantId');
    if (storedTenantId) {
        return storedTenantId;
    }
    
    // 方法4: 从全局变量中获取
    if (window.currentTenantId) {
        return window.currentTenantId;
    }
    
    return null;
}

/**
 * 显示错误消息
 * message: 错误消息字符串
 */
function showErrorMessage(message) {
    const container = document.querySelector('.container-fluid');
    if (container) {
        container.innerHTML = `
            <div class="alert alert-danger text-center" role="alert">
                <i class="fas fa-exclamation-triangle fa-2x mb-2"></i>
                <h4>初始化失败</h4>
                <p>${message}</p>
                <a href="/login" class="btn btn-primary">重新登录</a>
            </div>
        `;
    }
}
