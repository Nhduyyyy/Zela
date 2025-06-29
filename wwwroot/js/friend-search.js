document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const resultsBody = document.getElementById('resultsBody');
    let searchTimeout;

    // Enhanced search functionality
    const searchCount = document.getElementById('searchCount');
    const loadingState = document.getElementById('loadingState');
    const noResultsState = document.getElementById('noResultsState');
    const searchResults = document.getElementById('searchResults');
    
    if (searchInput && resultsBody) {
    searchInput.addEventListener('input', function() {
        clearTimeout(searchTimeout);
        const query = this.value.toLowerCase().trim();

            searchTimeout = setTimeout(() => {
                filterUsers(query);
            }, 300);
        });
    }
    
    function filterUsers(query) {
        const rows = resultsBody.querySelectorAll('tr');
        let visibleCount = 0;

                if (query === '') {
            // Show all users
            rows.forEach(row => {
                row.style.display = '';
                visibleCount++;
            });
        } else {
            // Filter users
            rows.forEach(row => {
                const username = row.dataset.username;
                const fakeId = row.dataset.fakeId;
                
                if (username && fakeId && (username.includes(query) || fakeId.includes(query))) {
                    row.style.display = '';
                    visibleCount++;
                    } else {
                        row.style.display = 'none';
                    }
            });
        }
        
        // Update search count
        if (searchCount) {
            searchCount.textContent = visibleCount;
        }
        
        // Show/hide no results state
        if (visibleCount === 0 && query !== '') {
            if (searchResults) searchResults.style.display = 'none';
            if (noResultsState) noResultsState.style.display = 'block';
        } else {
            if (searchResults) searchResults.style.display = 'block';
            if (noResultsState) noResultsState.style.display = 'none';
        }
    }

    // Thêm confirmation cho các nút nguy hiểm
    const dangerButtons = document.querySelectorAll('.btn-danger');
    dangerButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const form = button.closest('form');
            if (form && form.action.includes('RemoveFriend')) {
                const confirmed = confirm('Bạn có chắc muốn hủy kết bạn?');
                if (!confirmed) {
                    e.preventDefault();
                }
            }
        });
    });

    // Xử lý form submissions với AJAX để cập nhật UI real-time
    const friendshipForms = document.querySelectorAll('form');
    friendshipForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            const action = form.action || '';
            const button = form.querySelector('button[type="submit"]');
            
            // Chỉ xử lý AJAX cho SendRequest (nút "Kết bạn")
            if (action.includes('SendRequest') || (button && button.textContent.includes('Kết bạn'))) {
            e.preventDefault();
                handleSendRequest(form);
            } else {
                // Cho các action khác, chỉ thêm loading state
                if (button) {
                    button.disabled = true;
                    const originalText = button.innerHTML;
                    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
                }
            }
        });
    });

    function handleSendRequest(form) {
        const button = form.querySelector('button[type="submit"]');
        const originalText = button.innerHTML;
        const row = form.closest('tr');
        const statusCell = row.querySelector('td:nth-child(3)'); // Status column
        const actionCell = row.querySelector('td:nth-child(4)'); // Action column

            button.disabled = true;
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gửi...';

        const formData = new FormData(form);

        fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                'X-Requested-With': 'XMLHttpRequest'
                }
            })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Cập nhật status
                statusCell.innerHTML = '<span class="text-info"><i class="fas fa-clock"></i> Đã gửi lời mời</span>';
                
                // Cập nhật action
                actionCell.innerHTML = '<span class="text-secondary status-waiting"><i class="fas fa-hourglass-half"></i> Đang chờ phản hồi</span>';
                
                // Hiển thị thông báo thành công
                showNotification('Đã gửi lời mời kết bạn thành công!', 'success');
            } else {
                // Reset button nếu thất bại
                button.disabled = false;
                button.innerHTML = originalText;
                showNotification(data.message || 'Có lỗi xảy ra khi gửi lời mời', 'error');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    button.disabled = false;
            button.innerHTML = originalText;
            showNotification('Có lỗi xảy ra khi gửi lời mời', 'error');
        });
    }

    function showNotification(message, type) {
        // Tạo notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'success' ? 'success' : 'danger'} notification-popup`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            animation: slideInRight 0.3s ease-out;
        `;
        notification.innerHTML = `
            <strong>${type === 'success' ? '✅' : '❌'}</strong> ${message}
            <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Tự động ẩn sau 3 giây
        setTimeout(() => {
            if (notification.parentElement) {
                notification.style.animation = 'slideOutRight 0.3s ease-in';
                setTimeout(() => notification.remove(), 300);
            }
        }, 3000);
    }
});

function getStatusHtml(role) {
    switch (role) {
        case 0: return '<span class="text-muted">Chưa là bạn</span>';
        case 1: return '<span class="text-info">Đã gửi lời mời</span>';
        case 2: return '<span class="text-warning">Có lời mời đến</span>';
        case 3: return '<span class="text-success">Đã là bạn</span>';
        default: return '';
    }
}

function getActionHtml(user) {
    switch (user.role) {
        case 0:
            return `
                <form action="/Friendship/SendRequest" method="post" class="mb-0">
                    <input type="hidden" name="userId2" value="${user.userId}" />
                    <button type="submit" class="btn btn-sm btn-primary">Kết bạn</button>
                </form>`;
        case 1:
            return '<span class="text-secondary">Đang chờ</span>';
        case 2:
            return '<span class="text-secondary">Chờ phản hồi</span>';
        case 3:
            return `
                <form action="/Friendship/RemoveFriend" method="post" class="mb-0">
                    <input type="hidden" name="friendId" value="${user.userId}" />
                    <button type="submit" class="btn btn-sm btn-danger">Hủy bạn</button>
                </form>`;
        default:
            return '';
    }
}
