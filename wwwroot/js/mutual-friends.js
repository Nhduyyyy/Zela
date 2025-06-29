// Xử lý tương tác với tính năng bạn chung
document.addEventListener('DOMContentLoaded', function() {
    // Xử lý click vào badge bạn chung
    document.querySelectorAll('.mutual-friends-badge').forEach(badge => {
        badge.addEventListener('click', function() {
            const userId = this.dataset.userId;
            const userName = this.dataset.userName;
            
            showMutualFriendsModal(userId, userName);
        });
    });

    // Tự động load số lượng bạn chung cho các user card
    loadMutualFriendsCount();
});

/**
 * Hiển thị modal danh sách bạn chung
 */
async function showMutualFriendsModal(userId, userName) {
    // Tạo modal nếu chưa có
    let modal = document.getElementById('mutualFriendsModal');
    if (!modal) {
        modal = createMutualFriendsModal();
        document.body.appendChild(modal);
    }

    // Cập nhật title
    document.getElementById('modalUserName').textContent = userName;
    
    // Hiển thị modal
    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
    
    // Load danh sách bạn chung
    await loadMutualFriendsList(userId);
}

/**
 * Tạo modal HTML cho bạn chung
 */
function createMutualFriendsModal() {
    const modalHTML = `
        <div class="modal fade" id="mutualFriendsModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">
                            <i class="fas fa-users text-primary"></i>
                            Bạn chung với <span id="modalUserName"></span>
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div id="mutualFriendsContent">
                            <div class="text-center py-3">
                                <div class="spinner-border text-primary" role="status">
                                    <span class="visually-hidden">Đang tải...</span>
                                </div>
                                <p class="mt-2 text-muted">Đang tải danh sách bạn chung...</p>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Đóng</button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    const modalElement = document.createElement('div');
    modalElement.innerHTML = modalHTML;
    return modalElement.firstElementChild;
}

/**
 * Load danh sách bạn chung từ server
 */
async function loadMutualFriendsList(targetUserId) {
    const content = document.getElementById('mutualFriendsContent');
    
    try {
        const response = await fetch(`/Friendship/GetMutualFriendsList?targetUserId=${targetUserId}`);
        const data = await response.json();
        
        if (data.success) {
            if (data.friends && data.friends.length > 0) {
                content.innerHTML = `
                    <div class="row">
                        ${data.friends.map(friend => `
                            <div class="col-md-6 mb-3">
                                <div class="mutual-friend-item">
                                    <img src="${friend.avatarUrl}" 
                                         alt="${friend.name}" 
                                         class="mutual-friend-avatar">
                                    <div class="mutual-friend-info">
                                        <strong>${friend.name}</strong>
                                        <div class="mutual-friend-actions">
                                            <a href="/Chat/Index?friendId=${friend.userId}" 
                                               class="btn btn-sm btn-primary">
                                                <i class="fas fa-comment"></i> Chat
                                            </a>
                                            <button class="btn btn-sm btn-outline-info" 
                                                    onclick="showMutualFriendsModal(${friend.userId}, '${friend.name}')">
                                                <i class="fas fa-users"></i> Bạn chung
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                `;
            } else {
                content.innerHTML = `
                    <div class="text-center py-4">
                        <i class="fas fa-user-friends fa-3x text-muted mb-3"></i>
                        <h5>Không có bạn chung</h5>
                        <p class="text-muted">Bạn và người này chưa có bạn chung nào.</p>
                    </div>
                `;
            }
        } else {
            content.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-exclamation-triangle fa-3x text-warning mb-3"></i>
                    <h5>Lỗi</h5>
                    <p class="text-muted">${data.message || 'Không thể tải danh sách bạn chung'}</p>
                </div>
            `;
        }
    } catch (error) {
        console.error('Error loading mutual friends:', error);
        content.innerHTML = `
            <div class="text-center py-4">
                <i class="fas fa-wifi fa-3x text-danger mb-3"></i>
                <h5>Lỗi kết nối</h5>
                <p class="text-muted">Không thể tải danh sách bạn chung. Vui lòng thử lại.</p>
                <button class="btn btn-primary" onclick="loadMutualFriendsList(${targetUserId})">
                    <i class="fas fa-sync-alt"></i> Thử lại
                </button>
            </div>
        `;
    }
}

/**
 * Load số lượng bạn chung cho tất cả user cards
 */
async function loadMutualFriendsCount() {
    const badges = document.querySelectorAll('.mutual-friends-count');
    
    for (const badge of badges) {
        const userId = badge.dataset.userId;
        if (!userId) continue;
        
        try {
            const response = await fetch(`/Friendship/GetMutualFriendsCount?targetUserId=${userId}`);
            const data = await response.json();
            
            if (data.success) {
                const count = data.count || 0;
                if (count > 0) {
                    badge.innerHTML = `
                        <i class="fas fa-users text-primary"></i>
                        <strong class="text-primary">${count}</strong>
                        <small class="text-muted">bạn chung</small>
                    `;
                    badge.style.cursor = 'pointer';
                    badge.title = 'Nhấn để xem danh sách bạn chung';
                } else {
                    badge.innerHTML = `
                        <span class="text-muted">
                            <i class="fas fa-users"></i> Không có
                        </span>
                    `;
                }
            }
        } catch (error) {
            console.error('Error loading mutual friends count for user', userId, error);
        }
    }
}

/**
 * Cập nhật Find action để sử dụng method có bạn chung
 */
function loadFindWithMutualFriends() {
    window.location.href = '/Friendship/FindWithMutualFriends';
}

/**
 * Hiển thị tooltip cho badge bạn chung
 */
function initMutualFriendsTooltips() {
    const badges = document.querySelectorAll('.mutual-friends-badge');
    badges.forEach(badge => {
        badge.addEventListener('mouseenter', function() {
            if (this.dataset.count && parseInt(this.dataset.count) > 0) {
                // Có thể thêm tooltip library ở đây
                this.title = `Nhấn để xem ${this.dataset.count} bạn chung`;
            }
        });
    });
}

// CSS cho mutual friends modal
const mutualFriendsCSS = `
<style>
.mutual-friend-item {
    display: flex;
    align-items: center;
    padding: 1rem;
    background: #f8f9fa;
    border-radius: 10px;
    transition: all 0.3s ease;
}

.mutual-friend-item:hover {
    background: #e9ecef;
    transform: translateY(-2px);
}

.mutual-friend-avatar {
    width: 50px;
    height: 50px;
    border-radius: 50%;
    object-fit: cover;
    margin-right: 1rem;
    border: 2px solid #FF5A57;
}

.mutual-friend-info {
    flex: 1;
}

.mutual-friend-actions {
    margin-top: 0.5rem;
    display: flex;
    gap: 0.5rem;
}

.mutual-friend-actions .btn {
    font-size: 0.75rem;
    padding: 0.25rem 0.5rem;
    border-radius: 15px;
}

.mutual-friends-badge {
    display: inline-block;
    padding: 0.25rem 0.5rem;
    border-radius: 15px;
    background: rgba(255, 90, 87, 0.1);
    transition: all 0.3s ease;
}

.mutual-friends-badge:hover {
    background: rgba(255, 90, 87, 0.2);
    transform: scale(1.05);
}

@media (max-width: 768px) {
    .mutual-friend-item {
        flex-direction: column;
        text-align: center;
    }
    
    .mutual-friend-avatar {
        margin-right: 0;
        margin-bottom: 0.5rem;
    }
}
</style>
`;

// Inject CSS
document.head.insertAdjacentHTML('beforeend', mutualFriendsCSS); 