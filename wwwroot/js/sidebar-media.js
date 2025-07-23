document.addEventListener('DOMContentLoaded', function() {
    // Không lắng nghe click trên sidebarRight nữa
    // Chỉ khởi tạo khi có nút 'Xem tất cả' trong _SidebarRight.cshtml
    const sidebarRight = document.querySelector('.chat-info-panel[data-group-id]');
    if (!sidebarRight) return;

    sidebarRight.addEventListener('click', function(e) {
        const viewAllLink = e.target.closest('.view-all-link');
        if (viewAllLink) {
            e.preventDefault();
            e.stopPropagation();
            const fileSection = viewAllLink.closest('.info-section');
            if (fileSection && fileSection.querySelector('h6').textContent.includes('File')) {
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
});

// Đảm bảo không có event listener nào trên sidebarRight hoặc document
// Chỉ export showSidebarMedia ra global để file khác gọi
window.showSidebarMedia = showSidebarMedia;

async function showSidebarMedia(openTab = 'all') {
    const sidebarMediaContainer = document.getElementById('sidebar-media-container');
    let sidebarMedia = document.querySelector('.sidebar-media');

    if (!sidebarMedia) {
        // Lấy groupId từ URL hoặc biến global
        let currentGroupId = window.currentGroupId || getGroupIdFromUrl();
        if (!currentGroupId) return;
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
            const response = await fetch(`/GroupChat/GetGroupSidebarMedia?groupId=${currentGroupId}`);
            if (response.ok) {
                const html = await response.text();
                if (sidebarMediaContainer) {
                    sidebarMediaContainer.innerHTML = html;
                    sidebarMediaContainer.classList.remove('d-none');
                    setTimeout(() => showSidebarMedia(openTab), 100);
                    return;
                }
            } else {
                if (sidebarMediaContainer) {
                    sidebarMediaContainer.innerHTML = `<div class="chat-info-panel sidebar-media" style="display: flex; align-items: center; justify-content: center;"><div class="text-center text-danger"><i class="bi bi-exclamation-triangle fs-1"></i><p class="mt-2">Không thể tải media</p><button class="btn btn-outline-primary btn-sm" onclick="showSidebarMedia()">Thử lại</button></div></div>`;
                }
                return;
            }
        } catch (error) {
            if (sidebarMediaContainer) {
                sidebarMediaContainer.innerHTML = `<div class="chat-info-panel sidebar-media" style="display: flex; align-items: center; justify-content: center;"><div class="text-center text-danger"><i class="bi bi-exclamation-triangle fs-1"></i><p class="mt-2">Lỗi kết nối</p><button class="btn btn-outline-primary btn-sm" onclick="showSidebarMedia()">Thử lại</button></div></div>`;
            }
            return;
        }
    }
    sidebarMedia = document.querySelector('.sidebar-media');
    if (!sidebarMedia) return;
    // Ẩn sidebar right nếu có
    const sidebarRight = document.querySelector('.chat-info-panel[data-group-id]:not(.sidebar-media)');
    if (sidebarRight) sidebarRight.style.display = 'none';
    if (sidebarMediaContainer) sidebarMediaContainer.classList.remove('d-none');
    sidebarMedia.style.display = 'block';
    sidebarMedia.classList.remove('slide-out');
    if (openTab !== 'all') {
        const filterInput = sidebarMedia.querySelector(`input[name="mediaFilter"][value="${openTab}"]`);
        if (filterInput) {
            filterInput.checked = true;
            filterMedia(openTab, sidebarMedia);
        }
    }
    sidebarMedia.scrollTop = 0;
    attachSidebarMediaEvents(sidebarMedia, sidebarMediaContainer, sidebarRight);
}

function attachSidebarMediaEvents(sidebarMedia, sidebarMediaContainer, sidebarRight) {
    if (sidebarMedia.dataset.eventsAttached) return;
    sidebarMedia.dataset.eventsAttached = 'true';
    sidebarMedia.addEventListener('click', function(e) {
        if (e.target.closest('.sidebar-media-close')) {
            hideSidebarMedia(sidebarMedia, sidebarMediaContainer, sidebarRight);
        }
    });
    sidebarMedia.addEventListener('change', function(e) {
        if (e.target.name === 'mediaFilter') {
            filterMedia(e.target.value, sidebarMedia);
        }
    });
    sidebarMedia.addEventListener('click', function(e) {
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
    document.addEventListener('keydown', function escHandler(e) {
        if (e.key === 'Escape') {
            const modal = document.getElementById('mediaModal');
            if (modal && modal.classList.contains('show')) {
                hideMediaModal();
            }
        }
    });
}

function hideSidebarMedia(sidebarMedia, sidebarMediaContainer, sidebarRight) {
    if (!sidebarMedia) return;
    sidebarMedia.classList.add('slide-out');
    setTimeout(() => {
        sidebarMedia.style.display = 'none';
        sidebarMedia.classList.remove('slide-out');
        if (sidebarMediaContainer) sidebarMediaContainer.classList.add('d-none');
        if (sidebarRight) sidebarRight.style.display = 'block';
    }, 300);
}

function filterMedia(filterType, sidebarMedia) {
    const context = sidebarMedia || document;
    const mediaItems = context.querySelectorAll('.media-item');
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
    if (!modal || !modalImage || !modalVideo) return;
    modalImage.style.display = 'none';
    modalVideo.style.display = 'none';
    if (mediaType === 'image') {
        modalImage.src = mediaUrl;
        modalImage.alt = 'Ảnh chia sẻ trong nhóm';
        modalImage.style.display = 'block';
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else if (mediaType === 'video') {
        modalVideo.src = mediaUrl;
        modalVideo.style.display = 'block';
        const bootstrapModal = new bootstrap.Modal(modal);
        bootstrapModal.show();
    } else if (mediaType === 'file') {
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

function getGroupIdFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('groupId') || urlParams.get('id');
} 