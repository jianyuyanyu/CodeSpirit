// 工作流预览页面脚本
class WorkflowPreview {
    constructor(workflowId) {
        this.workflowId = workflowId;
        this.workflowData = null;
        this.init();
    }

    init() {
        console.log('初始化工作流预览组件，工作流ID:', this.workflowId);

        // 确保DOM元素存在
        if (!this.checkRequiredElements()) {
            console.error('必要的DOM元素不存在，无法初始化');
            return;
        }

        // 初始化 Mermaid
        this.initializeMermaid();

        // 加载工作流数据
        this.loadWorkflowData();
        
        // 初始化交互功能
        this.initializeInteractions();
    }

    /**
     * 检查必要的DOM元素是否存在
     * @returns {boolean} 是否所有必要元素都存在
     */
    checkRequiredElements() {
        const requiredElements = [
            'workflow-diagram',
            'nodes-table-body'
        ];

        for (const elementId of requiredElements) {
            if (!document.getElementById(elementId)) {
                console.error(`必要元素不存在: ${elementId}`);
                return false;
            }
        }

        return true;
    }

    /**
     * 初始化Mermaid
     */
    initializeMermaid() {
        try {
            if (typeof mermaid === 'undefined') {
                console.error('Mermaid库未加载');
                return;
            }

            mermaid.initialize({
                startOnLoad: false,
                theme: 'base',
                themeVariables: {
                    // 主色调 - Ant Design 蓝色
                    primaryColor: '#1890ff',
                    primaryTextColor: '#262626',
                    primaryBorderColor: '#1890ff',
                    
                    // 次要色调
                    secondaryColor: '#f0f5ff',
                    tertiaryColor: '#fafafa',
                    
                    // 背景色
                    background: '#ffffff',
                    mainBkg: '#ffffff',
                    secondBkg: '#f0f5ff',
                    tertiaryBkg: '#fafafa',
                    
                    // 线条颜色
                    lineColor: '#1890ff',
                    edgeLabelBackground: '#ffffff',
                    
                    // 文字颜色
                    textColor: '#262626',
                    labelTextColor: '#262626',
                    
                    // 节点样式
                    nodeBorder: '#1890ff',
                    clusterBkg: '#f0f5ff',
                    clusterBorder: '#1890ff',
                    
                    // 特殊节点颜色
                    fillType0: '#f6ffed', // 开始节点 - 绿色
                    fillType1: '#f0f5ff', // 审批节点 - 蓝色
                    fillType2: '#fff2e8', // 条件节点 - 橙色
                    fillType3: '#fff1f0', // 结束节点 - 红色
                    
                    // 激活状态
                    activeTaskBkgColor: '#1890ff',
                    activeTaskBorderColor: '#1890ff',
                    
                    // 网格
                    gridColor: '#f0f0f0',
                    
                    // 箭头
                    arrowheadColor: '#1890ff'
                },
                flowchart: {
                    useMaxWidth: true,
                    htmlLabels: true,
                    curve: 'linear',
                    padding: 20,
                    nodeSpacing: 50,
                    rankSpacing: 80,
                    diagramPadding: 20
                },
                securityLevel: 'loose',
                fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", sans-serif'
            });

            console.log('Mermaid初始化成功');
        } catch (error) {
            console.error('Mermaid初始化失败:', error);
        }
    }

    async loadWorkflowData() {
        try {
            // 并行加载工作流基本信息和预览数据
            const [workflowResponse, previewResponse] = await Promise.all([
                this.fetchWithAuth(`/approval/api/approval/workflowdefinitions/${this.workflowId}`),
                this.fetchWithAuth(`/approval/api/approval/workflowdefinitions/${this.workflowId}/preview-data`)
            ]);

            if (!workflowResponse.ok) {
                throw new Error(`获取工作流基本信息失败: ${workflowResponse.status}`);
            }

            if (!previewResponse.ok) {
                throw new Error(`获取工作流预览数据失败: ${previewResponse.status}`);
            }

            const workflowData = await workflowResponse.json();
            const previewData = await previewResponse.json();

            // 保存工作流基本信息
            this.workflowData = workflowData.data;

            // 更新页面标题和基本信息
            this.updateWorkflowInfo(this.workflowData);
            
            // 渲染流程图
            this.renderFlowChart(previewData.data);
            
            // 渲染节点表格
            this.renderNodesTable(previewData.data.nodes);
            
        } catch (error) {
            console.error('加载工作流数据失败:', error);
            this.showError('加载工作流数据失败，请刷新页面重试');
        }
    }

    /**
     * 使用认证token发送fetch请求
     * @param {string} url 请求URL
     * @param {Object} options 请求选项
     * @returns {Promise<Response>} fetch响应
     */
    async fetchWithAuth(url, options = {}) {
        const authHeaders = TokenManager.getAuthHeaders();
        const defaultOptions = {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                ...authHeaders,
                ...options.headers
            }
        };

        return fetch(url, { ...defaultOptions, ...options });
    }

    /**
     * 更新工作流基本信息显示
     * @param {Object} workflow 工作流数据
     */
    updateWorkflowInfo(workflow) {
        // 更新页面标题
        const titleElement = document.querySelector('.workflow-header h1');
        if (titleElement) {
            titleElement.innerHTML = `
                <i class="fas fa-project-diagram"></i>
                工作流预览 - ${workflow.name}
            `;
        }

        // 更新基本信息
        const nameElement = document.querySelector('[data-field="name"]');
        const codeElement = document.querySelector('[data-field="code"]');
        const versionElement = document.querySelector('[data-field="version"]');
        const statusElement = document.querySelector('[data-field="status"]');
        const descriptionElement = document.querySelector('[data-field="description"]');

        if (nameElement) {
            nameElement.innerHTML = `<i class="fas fa-project-diagram"></i> ${workflow.name}`;
            this.animateElement(nameElement);
        }
        if (codeElement) {
            codeElement.innerHTML = `<i class="fas fa-code"></i> ${workflow.code}`;
            this.animateElement(codeElement);
        }
        if (versionElement) {
            versionElement.innerHTML = `<i class="fas fa-tag"></i> v${workflow.version}`;
            this.animateElement(versionElement);
        }
        if (statusElement) {
            const statusClass = workflow.isEnabled ? 'status-enabled' : 'status-disabled';
            const statusText = workflow.isEnabled ? '已启用' : '已禁用';
            const statusIcon = workflow.isEnabled ? 'fas fa-check-circle' : 'fas fa-pause-circle';
            
            statusElement.innerHTML = `<i class="${statusIcon}"></i> ${statusText}`;
            statusElement.className = `workflow-status-badge ${statusClass}`;
            this.animateElement(statusElement);
        }
        if (descriptionElement) {
            const description = workflow.description || '暂无描述';
            descriptionElement.innerHTML = `<i class="fas fa-file-alt"></i> ${description}`;
            this.animateElement(descriptionElement);
        }
    }

    /**
     * 为元素添加动画效果
     * @param {HTMLElement} element 要动画的元素
     */
    animateElement(element) {
        element.style.opacity = '0';
        element.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            element.style.transition = 'all 0.5s ease';
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        }, 100);
    }

    /**
     * 显示错误信息
     * @param {string} message 错误消息
     */
    showError(message) {
        console.error('显示错误信息:', message);
        
        // 显示流程图错误
        const diagramElement = document.getElementById('workflow-diagram');
        if (diagramElement) {
            diagramElement.innerHTML = `
                <div class="workflow-error">
                    <i class="fas fa-exclamation-triangle"></i>
                    <h4>加载失败</h4>
                    <p>${message}</p>
                    <div class="error-actions">
                        <button class="btn btn-primary" onclick="location.reload()">
                            <i class="fas fa-redo"></i> 重新加载
                        </button>
                        <button class="btn btn-outline-secondary" onclick="history.back()">
                            <i class="fas fa-arrow-left"></i> 返回
                        </button>
                    </div>
                </div>
            `;
        } else {
            console.warn('流程图容器元素不存在');
        }
        
        // 显示节点表格错误
        const tableBodyElement = document.getElementById('nodes-table-body');
        if (tableBodyElement) {
            tableBodyElement.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center">
                        <div class="workflow-error">
                            <i class="fas fa-exclamation-triangle"></i>
                            <h4>数据加载失败</h4>
                            <p>${message}</p>
                        </div>
                    </td>
                </tr>
            `;
        } else {
            console.warn('节点表格容器元素不存在');
        }
        
        // 清空基本信息显示
        this.clearWorkflowInfo();
    }

    /**
     * 清空工作流基本信息显示
     */
    clearWorkflowInfo() {
        const fields = ['name', 'code', 'version', 'status', 'description'];
        fields.forEach(field => {
            const element = document.querySelector(`[data-field="${field}"]`);
            if (element) {
                element.textContent = '加载失败';
                if (field === 'status') {
                    element.className = 'badge badge-danger';
                }
            }
        });
    }

    renderFlowChart(data) {
        const diagramContainer = document.getElementById('workflow-diagram');
        
        if (!diagramContainer) {
            console.error('流程图容器元素不存在');
            return;
        }

        const mermaidCode = this.generateMermaidCode(data);
        
        try {
            // 确保Mermaid已经初始化
            if (typeof mermaid === 'undefined') {
                throw new Error('Mermaid库未加载');
            }

            // 清空容器
            diagramContainer.innerHTML = '';
            
            // 创建一个临时div来渲染Mermaid
            const tempDiv = document.createElement('div');
            tempDiv.style.visibility = 'hidden';
            tempDiv.style.position = 'absolute';
            document.body.appendChild(tempDiv);
            
            const uniqueId = 'diagram-' + Date.now();
            
            // 使用Promise方式渲染Mermaid
            mermaid.render(uniqueId, mermaidCode).then((result) => {
                // 移除临时div
                document.body.removeChild(tempDiv);
                
                // 将结果插入到目标容器
                diagramContainer.innerHTML = result.svg;
                console.log('流程图渲染成功');
                
                // 添加节点交互事件
                setTimeout(() => {
                    this.addNodeClickEvents();
                }, 100);
                
            }).catch(function(error) {
                console.error('Mermaid渲染失败:', error);
                
                // 移除临时div
                if (document.body.contains(tempDiv)) {
                    document.body.removeChild(tempDiv);
                }
                
                diagramContainer.innerHTML = `
                    <div class="d-flex justify-content-center align-items-center h-100">
                        <div class="text-center">
                            <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                            <p class="text-muted">流程图渲染失败</p>
                            <small class="text-muted">错误: ${error.message}</small>
                        </div>
                    </div>
                `;
            });
            
        } catch (error) {
            console.error('渲染流程图失败:', error);
            diagramContainer.innerHTML = `
                <div class="d-flex justify-content-center align-items-center h-100">
                    <div class="text-center">
                        <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                        <p class="text-muted">流程图渲染失败</p>
                        <small class="text-muted">错误: ${error.message}</small>
                    </div>
                </div>
            `;
        }
    }

    generateMermaidCode(data) {
        let mermaidCode = 'flowchart TD\n';
        
        if (!data.nodes || data.nodes.length === 0) {
            return `flowchart TD
    EMPTY["📋 暂无流程图数据"]
    class EMPTY emptyNode
    classDef emptyNode fill:#fafafa,stroke:#d9d9d9,stroke-width:2px,color:#8c8c8c`;
        }

        const nodes = data.nodes;
        const nodeMap = {};
        
        // 生成节点定义
        nodes.forEach(node => {
            const nodeId = `N${node.id}`;
            nodeMap[node.name] = nodeId;
            const nodeDefinition = this.getMermaidNodeDefinition(nodeId, node.name, node.nodeType);
            mermaidCode += `    ${nodeDefinition}\n`;
        });

        // 生成连接线
        nodes.forEach(node => {
            const fromNodeId = `N${node.id}`;
            
            if (node.conditions && node.conditions.length > 0) {
                node.conditions.forEach(condition => {
                    const nextNodeName = condition.nextNodeName;
                    const expression = condition.expression || '';
                    
                    if (nextNodeName && nodeMap[nextNodeName]) {
                        const toNodeId = nodeMap[nextNodeName];
                        if (expression && expression !== 'true') {
                            mermaidCode += `    ${fromNodeId} -->|"${expression}"| ${toNodeId}\n`;
                        } else {
                            mermaidCode += `    ${fromNodeId} --> ${toNodeId}\n`;
                        }
                    }
                });
            } else {
                // 简单的顺序连接
                const currentIndex = nodes.findIndex(n => n.id === node.id);
                if (currentIndex < nodes.length - 1) {
                    const nextNode = nodes[currentIndex + 1];
                    const toNodeId = `N${nextNode.id}`;
                    mermaidCode += `    ${fromNodeId} --> ${toNodeId}\n`;
                }
            }
        });

        // 添加样式类
        nodes.forEach(node => {
            const nodeId = `N${node.id}`;
            const styleClass = this.getMermaidNodeStyleClass(node.nodeType);
            mermaidCode += `    class ${nodeId} ${styleClass}\n`;
        });

        // 添加Ant Design风格的样式定义
        mermaidCode += `
    classDef startNode fill:#f6ffed,stroke:#52c41a,stroke-width:2px,color:#262626
    classDef approvalNode fill:#f0f5ff,stroke:#1890ff,stroke-width:2px,color:#262626
    classDef conditionNode fill:#fff2e8,stroke:#fa8c16,stroke-width:2px,color:#262626
    classDef endNode fill:#fff1f0,stroke:#f5222d,stroke-width:2px,color:#262626
    classDef defaultNode fill:#fafafa,stroke:#d9d9d9,stroke-width:2px,color:#262626
        `;

        return mermaidCode;
    }

    getMermaidNodeDefinition(nodeId, nodeName, nodeType) {
        // 限制节点名称长度，避免显示过长
        const displayName = nodeName.length > 10 ? nodeName.substring(0, 10) + '...' : nodeName;
        
        switch (nodeType?.toLowerCase()) {
            case 'start':
                return `${nodeId}(["🚀 ${displayName}"])`;
            case 'end':
                return `${nodeId}(["✅ ${displayName}"])`;
            case 'condition':
                return `${nodeId}{"🔍 ${displayName}"}`;
            case 'approval':
                return `${nodeId}["👤 ${displayName}"]`;
            case 'parallelgateway':
                return `${nodeId}{{"⚡ ${displayName}"}}`;
            case 'exclusivegateway':
                return `${nodeId}{{"🔀 ${displayName}"}}`;
            case 'carboncopy':
                return `${nodeId}[["📨 ${displayName}"]]`;
            default:
                return `${nodeId}["⚙️ ${displayName}"]`;
        }
    }

    getMermaidNodeStyleClass(nodeType) {
        switch (nodeType?.toLowerCase()) {
            case 'start':
                return 'startNode';
            case 'end':
                return 'endNode';
            case 'condition':
                return 'conditionNode';
            case 'approval':
                return 'approvalNode';
            default:
                return 'approvalNode';
        }
    }

    renderNodesTable(nodes) {
        const tbody = document.getElementById('nodes-table-body');
        
        if (!tbody) {
            console.error('节点表格容器元素不存在');
            return;
        }
        
        if (!nodes || nodes.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-muted">暂无节点数据</td>
                </tr>
            `;
            return;
        }

        const rows = nodes.map((node, index) => {
            const nodeTypeName = this.getNodeTypeName(node.nodeType);
            const approvalModeName = this.getApprovalModeName(node.approvalMode);
            const approvers = node.approvers ? node.approvers.map(a => a.approverName || a.approverValue).join(', ') : '无';
            const conditions = node.conditions ? node.conditions.map(c => c.description || c.expression).join('; ') : '无';
            
            const nodeTypeClass = this.getNodeTypeClass(node.nodeType);
            const nodeTypeIcon = this.getNodeTypeIcon(node.nodeType);

            return `
                <tr style="animation: fadeInUp 0.5s ease-out ${index * 0.1}s both;">
                    <td>
                        <i class="${nodeTypeIcon}"></i>
                        <strong>${node.name}</strong>
                    </td>
                    <td>
                        <span class="node-type-badge ${nodeTypeClass}">
                            <i class="${nodeTypeIcon}"></i>
                            ${nodeTypeName}
                        </span>
                    </td>
                    <td>
                        <span class="badge badge-outline-primary">
                            <i class="fas fa-users"></i>
                            ${approvalModeName}
                        </span>
                    </td>
                    <td>
                        <i class="fas fa-user-check text-primary"></i>
                        ${approvers}
                    </td>
                    <td>
                        <i class="fas fa-filter text-info"></i>
                        ${conditions}
                    </td>
                </tr>
            `;
        }).join('');

        tbody.innerHTML = rows;
    }

    getNodeTypeName(nodeType) {
        const typeMap = {
            'Start': '开始节点',
            'Approval': '审批节点',
            'Condition': '条件节点',
            'End': '结束节点',
            'ParallelGateway': '并行网关',
            'ExclusiveGateway': '排他网关',
            'CarbonCopy': '抄送节点'
        };
        return typeMap[nodeType] || nodeType;
    }

    getApprovalModeName(approvalMode) {
        const modeMap = {
            'Sequential': '串行审批',
            'Parallel': '并行审批',
            'CounterSign': '会签',
            'OrSign': '或签'
        };
        return modeMap[approvalMode] || approvalMode;
    }

    /**
     * 获取节点类型对应的CSS类
     * @param {string} nodeType 节点类型
     * @returns {string} CSS类名
     */
    getNodeTypeClass(nodeType) {
        const classMap = {
            'Start': 'type-start',
            'Approval': 'type-approval',
            'Condition': 'type-condition',
            'End': 'type-end',
            'ParallelGateway': 'type-approval',
            'ExclusiveGateway': 'type-condition',
            'CarbonCopy': 'type-approval'
        };
        return classMap[nodeType] || 'type-approval';
    }

    /**
     * 获取节点类型对应的图标
     * @param {string} nodeType 节点类型
     * @returns {string} 图标类名
     */
    getNodeTypeIcon(nodeType) {
        const iconMap = {
            'Start': 'fas fa-play-circle',
            'Approval': 'fas fa-user-check',
            'Condition': 'fas fa-code-branch',
            'End': 'fas fa-stop-circle',
            'ParallelGateway': 'fas fa-code-branch',
            'ExclusiveGateway': 'fas fa-random',
            'CarbonCopy': 'fas fa-copy'
        };
        return iconMap[nodeType] || 'fas fa-cog';
    }

    /**
     * 初始化交互功能
     */
    initializeInteractions() {
        // 缩放控制
        const zoomInBtn = document.getElementById('zoom-in-btn');
        const zoomOutBtn = document.getElementById('zoom-out-btn');
        const fitScreenBtn = document.getElementById('fit-screen-btn');
        const downloadBtn = document.getElementById('download-btn');

        if (zoomInBtn) {
            zoomInBtn.addEventListener('click', () => this.zoomDiagram(1.2));
        }
        if (zoomOutBtn) {
            zoomOutBtn.addEventListener('click', () => this.zoomDiagram(0.8));
        }
        if (fitScreenBtn) {
            fitScreenBtn.addEventListener('click', () => this.fitDiagramToScreen());
        }
        if (downloadBtn) {
            downloadBtn.addEventListener('click', () => this.downloadDiagram());
        }

        // 键盘快捷键
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey || e.metaKey) {
                switch (e.key) {
                    case '=':
                    case '+':
                        e.preventDefault();
                        this.zoomDiagram(1.2);
                        break;
                    case '-':
                        e.preventDefault();
                        this.zoomDiagram(0.8);
                        break;
                    case '0':
                        e.preventDefault();
                        this.fitDiagramToScreen();
                        break;
                }
            }
        });
    }

    /**
     * 缩放流程图
     * @param {number} scale 缩放比例
     */
    zoomDiagram(scale) {
        const diagramElement = document.getElementById('workflow-diagram');
        const svgElement = diagramElement.querySelector('svg');
        
        if (svgElement) {
            const currentTransform = svgElement.style.transform || 'scale(1)';
            const currentScale = parseFloat(currentTransform.match(/scale\(([^)]+)\)/)?.[1] || '1');
            const newScale = Math.max(0.1, Math.min(3, currentScale * scale));
            
            svgElement.style.transform = `scale(${newScale})`;
            svgElement.style.transformOrigin = 'center center';
            
            // 添加缩放动画
            svgElement.style.transition = 'transform 0.3s ease';
            setTimeout(() => {
                svgElement.style.transition = '';
            }, 300);
        }
    }

    /**
     * 适应屏幕大小
     */
    fitDiagramToScreen() {
        const diagramElement = document.getElementById('workflow-diagram');
        const svgElement = diagramElement.querySelector('svg');
        
        if (svgElement) {
            svgElement.style.transform = 'scale(1)';
            svgElement.style.transformOrigin = 'center center';
            
            // 添加动画效果
            svgElement.style.transition = 'transform 0.3s ease';
            setTimeout(() => {
                svgElement.style.transition = '';
            }, 300);
        }
    }

    /**
     * 下载流程图
     */
    downloadDiagram() {
        const diagramElement = document.getElementById('workflow-diagram');
        const svgElement = diagramElement.querySelector('svg');
        
        if (svgElement) {
            try {
                // 创建canvas元素
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                
                // 获取SVG尺寸
                const svgRect = svgElement.getBoundingClientRect();
                canvas.width = svgRect.width * 2; // 高分辨率
                canvas.height = svgRect.height * 2;
                
                // 设置白色背景
                ctx.fillStyle = 'white';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                
                // 将SVG转换为图片
                const svgData = new XMLSerializer().serializeToString(svgElement);
                const img = new Image();
                
                img.onload = () => {
                    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                    
                    // 下载图片
                    const link = document.createElement('a');
                    link.download = `workflow-${this.workflowId}-${Date.now()}.png`;
                    link.href = canvas.toDataURL('image/png');
                    link.click();
                };
                
                img.src = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgData)));
                
            } catch (error) {
                console.error('下载流程图失败:', error);
                alert('下载失败，请稍后重试');
            }
        }
    }

    /**
     * 添加节点点击事件
     */
    addNodeClickEvents() {
        const diagramElement = document.getElementById('workflow-diagram');
        const nodeElements = diagramElement.querySelectorAll('.node');
        
        nodeElements.forEach(node => {
            node.style.cursor = 'pointer';
            node.addEventListener('click', (e) => {
                const nodeId = node.id;
                this.showNodeDetails(nodeId);
            });
        });
    }

    /**
     * 显示节点详情
     * @param {string} nodeId 节点ID
     */
    showNodeDetails(nodeId) {
        // 这里可以添加显示节点详情的逻辑
        console.log('点击节点:', nodeId);
        
        // 高亮选中的节点
        const diagramElement = document.getElementById('workflow-diagram');
        const allNodes = diagramElement.querySelectorAll('.node');
        const clickedNode = diagramElement.querySelector(`#${nodeId}`);
        
        // 移除其他节点的高亮
        allNodes.forEach(node => {
            node.style.filter = '';
        });
        
        // 高亮当前节点
        if (clickedNode) {
            clickedNode.style.filter = 'drop-shadow(0 0 8px rgba(24, 144, 255, 0.6))';
        }
    }
}

// 将WorkflowPreview类暴露到全局，供页面初始化时使用
window.WorkflowPreview = WorkflowPreview;
