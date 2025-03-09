// 滚动聊天消息到底部
function scrollToBottom(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

// 显示通知
function showNotification(title, message) {
    if (!("Notification" in window)) {
        console.log("浏览器不支持通知");
        return;
    }

    if (Notification.permission === "granted") {
        const notification = new Notification(title, {
            body: message,
            icon: "/images/notification-icon.png"
        });
        
        notification.onclick = function() {
            window.focus();
            this.close();
        };
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(function (permission) {
            if (permission === "granted") {
                showNotification(title, message);
            }
        });
    }
}

// 显示Toast提示
function showToast(message, title = "通知") {
    // 检查是否已经有toast容器，如果没有则创建一个
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.position = 'fixed';
        toastContainer.style.top = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    // 创建一个新的toast元素
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.style.backgroundColor = '#333';
    toast.style.color = 'white';
    toast.style.padding = '12px 20px';
    toast.style.marginBottom = '10px';
    toast.style.borderRadius = '4px';
    toast.style.boxShadow = '0 2px 5px rgba(0,0,0,0.2)';
    toast.style.minWidth = '250px';
    toast.style.opacity = '0';
    toast.style.transition = 'opacity 0.3s ease-in-out';
    
    // 添加标题
    const titleElement = document.createElement('div');
    titleElement.textContent = title;
    titleElement.style.fontWeight = 'bold';
    titleElement.style.marginBottom = '5px';
    
    // 添加消息
    const messageElement = document.createElement('div');
    messageElement.textContent = message;
    
    // 将内容添加到toast中
    toast.appendChild(titleElement);
    toast.appendChild(messageElement);
    
    // 添加到容器
    toastContainer.appendChild(toast);
    
    // 显示toast
    setTimeout(() => {
        toast.style.opacity = '1';
    }, 50);
    
    // 几秒后隐藏toast
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => {
            toastContainer.removeChild(toast);
        }, 300);
    }, 5000);
}

// 导出函数以供Blazor组件使用
// export { scrollToBottom, showNotification, showToast };

// 将函数暴露到全局作用域
window.scrollToBottom = scrollToBottom;
window.showNotification = showNotification;
window.showToast = showToast;