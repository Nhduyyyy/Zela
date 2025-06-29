document.addEventListener('DOMContentLoaded', function() {
    // Khởi tạo sidebar media
    initSidebarMedia();
});

function initSidebarMedia() {
    // Xử lý nút "Xem tất cả" trong sidebar right - cách 1: Event delegation
    document.addEventListener('click', function(e) {
        console.log('Click detected on:', e.target);
        console.log('Closest view-all-link:', e.target.closest('.view-all-link'));

        if (e.target.closest('.view-all-link')) {
            e.preventDefault();
            console.log('View all link clicked!');

            // Kiểm tra xem có phải là nút "Xem tất cả" trong phần Files không
            const viewAllLink = e.target.closest('.view-all-link');
            const fileSection = viewAllLink.closest('.info-section');
            if (fileSection && fileSection.querySelector('h6').textContent.includes('File')) {
                console.log('Opening files tab directly');
                showSidebarMedia('files').catch(error => {
                    console.error('Error showing sidebar media:', error);
                });
            } else {
                showSidebarMedia().catch(error => {
                    console.error('Error showing sidebar media:', error);
                });
            }
        }
    });

    // Cách 2: Direct event listener cho view-all-link
    document.addEventListener('click', function(e) {
        const viewAllLink = e.target.closest('.view-all-link');
        if (viewAllLink) {
            e.preventDefault();
            e.stopPropagation();
            console.log('View all link clicked (method 2)!');

            // Kiểm tra xem có phải là nút "Xem tất cả" trong phần Files không
            const fileSection = viewAllLink.closest('.info-section');
            if (fileSection && fileSection.querySelector('h6').textContent.includes('File')) {
                console.log('Opening files tab directly');
                showSidebarMedia('files').catch(error => {
                    console.error('Error showing sidebar media:', error);
                });
            } else {
                showSidebarMedia().catch(error => {
                    console.error('Error showing sidebar media:', error);
                });
            }
        }
    });

    // Xử lý nút đóng sidebar media
    document.addEventListener('click', function(e) {
        if (e.target.closest('.sidebar-media-close')) {
            hideSidebarMedia();
        }
    });

    // Xử lý filter media
    document.addEventListener('change', function(e) {
        if (e.target.name === 'mediaFilter') {
            filterMedia(e.target.value);
        }
    });

    // Xử lý click vào media item để mở modal
    document.addEventListener('click', function(e) {
        if (e.target.closest('.media-item')) {
            e.preventDefault();
            const mediaItem = e.target.closest('.media-item');
            const mediaType = mediaItem.dataset.type;

            if (mediaType === 'file') {
                const fileThumbnail = mediaItem.querySelector('.file-thumbnail');
                const mediaUrl = fileThumbnail.dataset.url;
                openMediaModal(mediaType, mediaUrl);
            } else {
                const mediaUrl = mediaItem.querySelector('.media-thumbnail').dataset.url;
                openMediaModal(mediaType, mediaUrl);
            }
        }
    });

    // Xử lý đóng modal bằng ESC
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            const modal = document.getElementById('mediaModal');
            if (modal && modal.classList.contains('show')) {
                hideMediaModal();
            }
        }
    });
}

async function showSidebarMedia(openTab = 'all') {
    const sidebarRight = document.querySelector('.chat-info-panel:not(.sidebar-media)');
    const sidebarMedia = document.querySelector('.sidebar-media');
    const sidebarMediaContainer = document.getElementById('sidebar-media-container');

    // Nếu sidebar media chưa được load, load nó trước
    if (!sidebarMedia) {
        console.log('Loading sidebar media...');

        // Lấy groupId từ sidebar right hiện tại
        let currentGroupId = sidebarRight?.dataset.groupId;
        if (!currentGroupId) {
            console.error('Group ID not found in sidebar');
            // Thử lấy từ biến global hoặc URL
            const fallbackGroupId = window.currentGroupId || getGroupIdFromUrl();
            if (fallbackGroupId) {
                console.log('Using fallback group ID:', fallbackGroupId);
                currentGroupId = fallbackGroupId;
            } else {
                console.error('No group ID available');
                return;
            }
        }

        // Hiển thị loading indicator
        if (sidebarMediaContainer) {
            sidebarMediaContainer.innerHTML = `
                <div class="chat-info-panel sidebar-media" style="display: flex; align-items: center; justify-content: center;">
                    <div class="text-center">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <p class="mt-2 text-muted">Đang tải media...</p>
                    </div>
                </div>
            `;
            sidebarMediaContainer.classList.remove('d-none');
        }

        try {
            // Load sidebar media từ server
            console.log('Loading sidebar media for group ID:', currentGroupId);
            const response = await fetch(`/GroupChat/GetGroupSidebarMedia?groupId=${currentGroupId}`);
            if (response.ok) {
                const html = await response.text();
                console.log('Sidebar media loaded successfully');
                if (sidebarMediaContainer) {
                    sidebarMediaContainer.innerHTML = html;
                    sidebarMediaContainer.classList.remove('d-none');

                    // Gọi lại hàm sau khi load xong với tab được chỉ định
                    setTimeout(() => showSidebarMedia(openTab), 100);
                    return;
                }
            } else {
                console.error('Failed to load sidebar media, status:', response.status);
                // Hiển thị lỗi
                if (sidebarMediaContainer) {
                    sidebarMediaContainer.innerHTML = `
                        <div class="chat-info-panel sidebar-media" style="display: flex; align-items: center; justify-content: center;">
                            <div class="text-center text-danger">
                                <i class="bi bi-exclamation-triangle fs-1"></i>
                                <p class="mt-2">Không thể tải media</p>
                                <button class="btn btn-outline-primary btn-sm" onclick="showSidebarMedia()">Thử lại</button>
                            </div>
                        </div>
                    `;
                }
                return;
            }
        } catch (error) {
            console.error('Error loading sidebar media:', error);
            // Hiển thị lỗi
            if (sidebarMediaContainer) {
                sidebarMediaContainer.innerHTML = `
                    <div class="chat-info-panel sidebar-media" style="display: flex; align-items: center; justify-content: center;">
                        <div class="text-center text-danger">
                            <i class="bi bi-exclamation-triangle fs-1"></i>
                            <p class="mt-2">Lỗi kết nối</p>
                            <button class="btn btn-outline-primary btn-sm" onclick="showSidebarMedia()">Thử lại</button>
                        </div>
                    </div>
                `;
            }
            return;
        }
    }

    // Ẩn sidebar right
    if (sidebarRight) {
        sidebarRight.style.display = 'none';
    }

    // Hiển thị sidebar media container
    if (sidebarMediaContainer) {
        sidebarMediaContainer.classList.remove('d-none');
    }

    // Hiển thị sidebar media
    sidebarMedia.style.display = 'block';
    sidebarMedia.classList.remove('slide-out');

    // Chọn tab được chỉ định
    if (openTab !== 'all') {
        const filterInput = document.querySelector(`input[name="mediaFilter"][value="${openTab}"]`);
        if (filterInput) {
            filterInput.checked = true;
            filterMedia(openTab);
        }
    }

    // Scroll to top
    sidebarMedia.scrollTop = 0;
}

function hideSidebarMedia() {
    const sidebarRight = document.querySelector('.chat-info-panel:not(.sidebar-media)');
    const sidebarMedia = document.querySelector('.sidebar-media');
    const sidebarMediaContainer = document.getElementById('sidebar-media-container');

    if (!sidebarMedia) {
        return;
    }

    // Thêm animation slide out
    sidebarMedia.classList.add('slide-out');

    // Sau khi animation hoàn thành, ẩn sidebar media và hiển thị sidebar right
    setTimeout(() => {
        sidebarMedia.style.display = 'none';
        sidebarMedia.classList.remove('slide-out');

        // Ẩn sidebar media container
        if (sidebarMediaContainer) {
            sidebarMediaContainer.classList.add('d-none');
        }

        if (sidebarRight) {
            sidebarRight.style.display = 'block';
        }
    }, 300);
}

function filterMedia(filterType) {
    const mediaItems = document.querySelectorAll('.media-item');

    mediaItems.forEach(item => {
        const itemType = item.dataset.type;

        switch (filterType) {
            case 'all':
                item.style.display = 'block';
                break;
            case 'images':
                item.style.display = itemType === 'image' ? 'block' : 'none';
                break;
            case 'videos':
                item.style.display = itemType === 'video' ? 'block' : 'none';
                break;
            case 'files':
                item.style.display = itemType === 'file' ? 'block' : 'none';
                break;
        }
    });
}

function openMediaModal(mediaType, mediaUrl) {
    const modal = document.getElementById('mediaModal');
    const modalImage = document.getElementById('modalImage');
    const modalVideo = document.getElementById('modalVideo');

    if (!modal || !modalImage || !modalVideo) {
        console.error('Modal elements not found');
        return;
    }

    // Ẩn cả image và video
    modalImage.style.display = 'none';
    modalVideo.style.display = 'none';

    // Hiển thị media tương ứng
    if (mediaType === 'image') {
        modalImage.src = mediaUrl;
        modalImage.alt = 'Ảnh chia sẻ trong nhóm';
        modalImage.style.display = 'block';

        // Hiển thị modal
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else if (mediaType === 'video') {
        modalVideo.src = mediaUrl;
        modalVideo.style.display = 'block';

        // Hiển thị modal
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else if (mediaType === 'file') {
        // Tải file trực tiếp
        window.open(mediaUrl, '_blank');
    }
}

function hideMediaModal() {
    const modal = document.getElementById('mediaModal');
    if (modal) {
        const bootstrapModal = bootstrap.Modal.getInstance(modal);
        if (bootstrapModal) {
            bootstrapModal.hide();
        }
    }
}

// Hàm lấy groupId từ URL
function getGroupIdFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('groupId') || urlParams.get('id');
} 