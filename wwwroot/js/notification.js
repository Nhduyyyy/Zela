document.addEventListener('DOMContentLoaded', function () {
    // ========================
    // Tham chiếu các phần tử DOM
    // ========================
    const bell = document.getElementById('notification-bell');
    const dropdown = document.getElementById('notification-dropdown');
    const list = document.getElementById('notification-list');
    const countBadge = document.getElementById('notification-count');
    const markAllReadBtn = document.getElementById('mark-all-as-read');

    // ========================
    // State lưu trữ thông báo và trạng thái dropdown
    // ========================
    let notifications = [];
    let isDropdownOpen = false;

    // ========================
    // Các hàm gọi API lấy/thay đổi trạng thái thông báo
    // ========================
    const api = {
        async getNotifications() {
            try {
                const response = await fetch('/api/notifications');
                if (!response.ok) throw new Error('Failed to fetch');
                return await response.json();
            } catch (error) {
                console.error('Error fetching notifications:', error);
                return [];
            }
        },
        async markAsRead(id) {
            await fetch(`/api/notifications/${id}/read`, { method: 'POST' });
        },
        async markAllAsRead() {
            await fetch('/api/notifications/read-all', { method: 'POST' });
        }
    };

    // ========================
    // Hàm render danh sách thông báo ra dropdown
    // ========================
    function renderNotifications() {
        if (notifications.length === 0) {
            list.innerHTML = '<div style="text-align:center; padding: 20px;">Không có thông báo mới.</div>';
            countBadge.style.display = 'none';
            return;
        }

        list.innerHTML = notifications.map(n => `
            <div class="notification-item ${n.isRead ? '' : 'unread'}" data-id="${n.notificationId}" data-url="${n.redirectUrl}">
                <div class="notification-item-content">
                    <div class="notification-item-sender">${n.senderName}</div>
                    <div class="notification-item-preview">${n.content}</div>
                    <div class="notification-item-time">${formatTimeAgo(n.timestamp)}</div>
                </div>
            </div>
        `).join('');

        const unreadCount = notifications.filter(n => !n.isRead).length;
        if (unreadCount > 0) {
            countBadge.textContent = unreadCount;
            countBadge.style.display = 'block';
        } else {
            countBadge.style.display = 'none';
        }
    }

    // ========================
    // Event: Click vào chuông để mở/đóng dropdown và load thông báo
    // ========================
    bell.addEventListener('click', async (e) => {
        e.stopPropagation();
        isDropdownOpen = !isDropdownOpen;
        if (isDropdownOpen) {
            dropdown.style.display = 'block';
            notifications = await api.getNotifications();
            renderNotifications();
        } else {
            dropdown.style.display = 'none';
        }
    });

    // ========================
    // Event: Click ra ngoài để đóng dropdown
    // ========================
    document.addEventListener('click', (e) => {
        if (!dropdown.contains(e.target)) {
            isDropdownOpen = false;
            dropdown.style.display = 'none';
        }
    });

    // ========================
    // Event: Click vào 1 thông báo để đánh dấu đã đọc và chuyển trang (nếu có url)
    // ========================
    list.addEventListener('click', async (e) => {
        const item = e.target.closest('.notification-item');
        if (item) {
            const id = item.dataset.id;
            const url = item.dataset.url;
            await api.markAsRead(id);
            if (url) {
                window.location.href = url;
            }
        }
    });

    // ========================
    // Event: Click "Đánh dấu tất cả đã đọc"
    // ========================
    markAllReadBtn.addEventListener('click', async () => {
        await api.markAllAsRead();
        notifications.forEach(n => n.isRead = true);
        renderNotifications();
    });

    // ========================
    // Utility: Định dạng thời gian kiểu "x phút trước"
    // ========================
    function formatTimeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now - date) / 1000);
        let interval = seconds / 31536000;
        if (interval > 1) return Math.floor(interval) + " năm trước";
        interval = seconds / 2592000;
        if (interval > 1) return Math.floor(interval) + " tháng trước";
        interval = seconds / 86400;
        if (interval > 1) return Math.floor(interval) + " ngày trước";
        interval = seconds / 3600;
        if (interval > 1) return Math.floor(interval) + " giờ trước";
        interval = seconds / 60;
        if (interval > 1) return Math.floor(interval) + " phút trước";
        return "Vài giây trước";
    }

    // ========================
    // (Optional) Kết nối SignalR để nhận thông báo realtime
    // ========================
    // const connection = new signalR.HubConnectionBuilder().withUrl("/notificationHub").build();
    // connection.on("ReceiveNotification", (notification) => {
    //     notifications.unshift(notification);
    //     renderNotifications();
    // });
    // connection.start().catch(err => console.error(err.toString()));
}); 