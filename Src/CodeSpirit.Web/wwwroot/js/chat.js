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

// 将函数暴露到全局作用域 - 直接暴露到全局对象，不使用window前缀
window.scrollToBottom = scrollToBottom;
window.showNotification = showNotification;
window.showToast = showToast;

// 添加缺少的函数：获取未读通知数量
window.fetchUnreadNotificationCount = function(userId) {
    console.log("获取用户未读通知数量:", userId);
    try {
        // 获取存储的token
        var token = localStorage.getItem('token');
        
        // 创建一个HTTP请求获取未读消息数，添加token认证
        fetch(`messaging/api/messaging/messages/my/unread/count`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                }
                // 即使请求失败也不抛出错误，只是记录日志
                console.log('获取未读消息数失败');
                return { count: 0 };
            })
            .then(data => {
                // 更新UI上的未读消息数量
                updateNotificationCount(data.count || 0);
            })
            .catch(error => {
                console.error("获取未读消息数出错:", error);
            });
    } catch (error) {
        console.error("执行fetchUnreadNotificationCount时出错:", error);
    }
};

// 更新通知计数
window.updateNotificationCount = function(count) {
    console.log("更新通知计数:", count);
    try {
        // 查找通知徽章元素
        const badge = document.querySelector('.notification-badge .badge');
        if (badge) {
            // 如果存在徽章元素，更新其内容
            badge.textContent = count > 99 ? "99+" : count.toString();
            
            // 更新徽章显示状态
            const badgeContainer = document.querySelector('.notification-badge');
            if (badgeContainer) {
                if (count > 0) {
                    badgeContainer.classList.add('has-notifications');
                } else {
                    badgeContainer.classList.remove('has-notifications');
                }
            }
        }
    } catch (error) {
        console.error("执行updateNotificationCount时出错:", error);
    }
};

// 添加缺少的函数：显示聊天通知 (备用实现，若admin.js中已有定义则不会覆盖)
if (typeof window.showChatNotification !== 'function') {
    window.showChatNotification = function(message, senderName, conversationId) {
        console.log("显示聊天通知:", message, "发送者:", senderName);
        try {
            // 显示一个Toast通知
            showToast(message, `来自 ${senderName} 的消息`);
            
            // 尝试显示浏览器通知
            if ("Notification" in window && Notification.permission === "granted") {
                const notification = new Notification(`来自 ${senderName} 的新消息`, {
                    body: message,
                    icon: "/images/notification-icon.png" // 确保该图片存在
                });
                
                notification.onclick = function() {
                    window.focus();
                    if (conversationId) {
                        window.location.href = `/chat-app?conversation=${conversationId}`;
                    } else {
                        window.location.href = '/chat-app';
                    }
                    this.close();
                };
            }
        } catch (error) {
            console.error("执行showChatNotification时出错:", error);
            // 确保错误被捕获，不会破坏用户体验
            try {
                showToast(message, `来自 ${senderName} 的消息`);
            } catch (e) {
                console.error("备用通知也失败:", e);
            }
        }
    };
}

// 清除DOM中的无效属性
window.cleanupInvalidAttributes = function() {
    console.log("清理DOM中的无效属性");
    try {
        // 查找所有可能有 @onclick 属性的元素
        const allElements = document.querySelectorAll('*');
        let count = 0;
        
        allElements.forEach(element => {
            // 检查元素是否有 @onclick 属性
            if (element.hasAttribute('@onclick')) {
                element.removeAttribute('@onclick');
                count++;
            }
            
            // 检查其他可能的无效属性
            const invalidPrefixes = ['@', ':', 'bind:'];
            for (let i = 0; i < element.attributes.length; i++) {
                const attrName = element.attributes[i].name;
                
                for (const prefix of invalidPrefixes) {
                    if (attrName.startsWith(prefix)) {
                        element.removeAttribute(attrName);
                        count++;
                        i--; // 因为我们删除了一个属性，需要调整索引
                        break;
                    }
                }
            }
        });
        
        console.log(`清理完成，共移除 ${count} 个无效属性`);
        return count;
    } catch (error) {
        console.error("清理DOM属性时出错:", error);
        return -1;
    }
};