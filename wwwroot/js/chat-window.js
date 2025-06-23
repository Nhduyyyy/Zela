(() => {
    // ID của friend đang chat
    let currentFriendId = null;
    let stickerPanelVisible = false;
    let availableStickers = [];
    let selectedFiles = []; // Array để lưu nhiều file

    // Tham chiếu các phần tử DOM
    const chatContentEl = document.querySelector('.chat-content');
    const chatInputEl = document.querySelector('.chat-input-bar input[type="text"]');
    const chatUserInfoEl = document.querySelector('.chat-user-info');
    const fileInputEl = document.getElementById('chat-file-input');
    const sendBtn = document.querySelector('.btn-send');
    const previewEl = document.getElementById('chat-preview');
    const chatFriendNameEl = document.getElementById('chat-friend-name');

    // Khởi tạo kết nối SignalR (signalR đã được load trước qua CDN hoặc script tag)
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/chathub')
        .build();

    // Xử lý khi nhận message mới
    connection.on('ReceiveMessage', msg => {
        console.log('SignalR nhận được:', msg);
        const cid = Number(currentFriendId);
        if (cid && (msg.senderId === cid || msg.recipientId === cid)) {
            chatContentEl.insertAdjacentHTML('beforeend', renderMessage(msg));
            scrollToBottom();
            // Nếu có file đang preview, ẩn luôn
            if (previewEl) {
                previewEl.innerHTML = '';
                previewEl.style.display = 'none';
            }
        }
    });

    // Nhận sticker mới
    connection.on("ReceiveSticker", function (msg) {
        console.log("SignalR nhận được sticker:", msg);

        let currentId = Number(currentFriendId);

        if (currentId && (msg.senderId === currentId || msg.recipientId === currentId)) {
            chatContentEl.insertAdjacentHTML('beforeend', renderMessage(msg));
            scrollToBottom();
        }
    });

    connection.start().catch(err => console.error('SignalR error:', err));

    // Gửi tin nhắn
    // Show loading state
    function showLoadingState() {
        const sendBtn = document.querySelector('.btn-send');
        const chatInput = chatInputEl;

        // Disable input và button
        chatInput.disabled = true;
        chatInput.style.opacity = '0.6';

        if (sendBtn) {
            sendBtn.disabled = true;
            sendBtn.innerHTML = `
                <div class="loading-spinner"></div>
            `;
            sendBtn.style.opacity = '0.6';
            sendBtn.style.transform = 'scale(0.95)';
        }

        // Thêm loading overlay lên preview nếu có
        const previewContainer = previewEl;
        if (previewContainer && previewContainer.style.display !== 'none') {
            const loadingOverlay = document.createElement('div');
            loadingOverlay.className = 'upload-loading-overlay';
            loadingOverlay.innerHTML = `
                <div class="upload-progress">
                    <div class="upload-spinner"></div>
                    <span class="upload-text">Đang tải lên...</span>
                    <div class="upload-progress-bar">
                        <div class="upload-progress-fill"></div>
                    </div>
                </div>
            `;
            previewContainer.appendChild(loadingOverlay);

            // Animate progress bar
            setTimeout(() => {
                const progressFill = loadingOverlay.querySelector('.upload-progress-fill');
                if (progressFill) {
                    progressFill.style.width = '100%';
                }
            }, 100);
        }
    }

    // Show success toast
    function showSuccessToast(message) {
        const toast = document.createElement('div');
        toast.className = 'success-toast';
        toast.innerHTML = `
            <div class="toast-content">
                <i class="bi bi-check-circle-fill"></i>
                <span>${message}</span>
            </div>
        `;

        document.body.appendChild(toast);

        // Animate in
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, 3000);
    }

    // Hide loading state
    function hideLoadingState() {
        const sendBtn = document.querySelector('.btn-send');
        const chatInput = chatInputEl;

        // Enable lại input và button
        chatInput.disabled = false;
        chatInput.style.opacity = '1';

        if (sendBtn) {
            sendBtn.disabled = false;
            sendBtn.innerHTML = `<i class="bi bi-send"></i>`;
            sendBtn.style.opacity = '1';
            sendBtn.style.transform = 'scale(1)';
        }

        // Xóa loading overlay
        const loadingOverlay = document.querySelector('.upload-loading-overlay');
        if (loadingOverlay) {
            loadingOverlay.remove();
        }
    }

    async function sendMessage() {
        const content = chatInputEl.value.trim();
        const files = selectedFiles.length > 0 ? selectedFiles : null;
        const friendId = currentFriendId;
        if (!content && !files) return;

        // Show loading state
        showLoadingState();

        try {
            if (files && files.length > 0) {
                // Gửi nhiều file qua HTTP POST
                const formData = new FormData();
                formData.append('content', content);
                formData.append('friendId', friendId);

                // Thêm tất cả file vào FormData
                files.forEach(file => {
                    formData.append('files', file);
                });

                const res = await fetch('/Chat/SendMessage', {
                    method: 'POST',
                    body: formData
                });

                if (res.ok) {
                    chatInputEl.value = '';
                    selectedFiles = [];
                    if (fileInputEl) {
                        fileInputEl.value = '';
                    }
                    if (previewEl) {
                        previewEl.innerHTML = '';
                        previewEl.style.display = 'none';
                    }
                    updateInputPlaceholder(); // Reset placeholder after sending
                    const fileCount = files.length;
                    showSuccessToast(`${fileCount} file${fileCount > 1 ? 's' : ''} đã được gửi thành công!`);
                    console.log(`${fileCount} file(s) đã được gửi thành công!`);
                } else {
                    console.error('Lỗi khi gửi file:', res.status);
                    alert('Có lỗi xảy ra khi gửi file. Vui lòng thử lại!');
                }
            } else {
                // Gửi text qua SignalR
                await connection.invoke('SendMessage', friendId, content);
                chatInputEl.value = '';
                console.log("Tin nhắn đã gửi thành công");
            }
        } catch (error) {
            console.error('SendMessage error:', error);
            alert('Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại!');
        } finally {
            // Hide loading state
            hideLoadingState();
        }
    }

    // Render 1 tin nhắn
    function renderMessage(msg) {
        const isMine = msg.senderId === currentUserId;
        const side = isMine ? 'right' : 'left';
        const time = msg.sentAt.substring(11, 16);

        // Render media nếu có
        let mediaHtml = '';
        if (msg.media && msg.media.length > 0) {
            for (const media of msg.media) {
                if (media.mediaType && media.mediaType.startsWith('image/')) {
                    mediaHtml += `<img src="${media.url}" class="message-media-img" alt="Ảnh gửi" />`;
                } else if (media.mediaType && media.mediaType.startsWith('video/')) {
                    mediaHtml += `<video src="${media.url}" class="message-media-video" controls></video>`;
                } else {
                    // File thường: icon, tên file, nút tải về
                    const fileName = media.fileName || media.url.split('/').pop();
                    mediaHtml += `
                    <div class="chat-file-attachment">
                        <span class="file-icon"><i class="bi bi-file-earmark-text"></i></span>
                        <span class="file-name">${fileName}</span>
                        <a href="${media.url}" download="${fileName}" class="file-download-btn" title="Tải về"><i class="bi bi-download"></i></a>
                    </div>`;
                }
            }
        }

        // Nếu là Sticker hoặc Text
        let stickerHtml ='';
        let textHtml = '';
        if (msg.stickerUrl && msg.stickerUrl.length > 0) {
            stickerHtml = `<img src="${msg.stickerUrl}" class="sticker-message" alt="Sticker" draggable="false">`;
        } else if (msg.content && msg.content.trim().length > 0) {
            // Xử lý text để đảm bảo tự động xuống dòng đúng cách
            const processedContent = msg.content
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;')
                .replace(/\n/g, '<br>'); // Giữ nguyên line breaks
            textHtml = `<span class="message-bubble">${processedContent}</span>`;
        } else {
            textHtml = ""; // Không render bubble nếu không có nội dung
        }

        if (isMine) {
            return `
        <div class="message ${side}">
          <div class="message-content">
            <span class="message-time">${time}</span>
            ${mediaHtml}
            ${stickerHtml}
            ${textHtml}
          </div>
        </div>`;
        } else {
            return `
        <div class="message ${side}">
          <img src="${msg.avatarUrl}" class="message-avatar" />
          <div class="message-content">
            <span class="message-time">${time}</span>
            ${mediaHtml}
            ${stickerHtml}
            ${textHtml}
          </div>
        </div>`;
        }
    }

    // Cuộn xuống cuối chat
    function scrollToBottom() {
        chatContentEl.scrollTop = chatContentEl.scrollHeight;
    }

    // Load danh sách sticker từ server
    async function loadStickers() {
        try {
            const response = await fetch('/Chat/GetStickers');
            const stickers = await response.json();
            console.log('Loaded stickers:', stickers);
            availableStickers = stickers;
            renderStickerPanel(stickers);
        } catch (error) {
            console.error('Failed to load stickers:', error);
        }
    }

    // Render sticker panel
    function renderStickerPanel(stickers) {
        const grid = document.querySelector('.sticker-grid');
        if (!grid) return;

        grid.innerHTML = '';

        console.log('Rendering sticker panel with', stickers.length, 'stickers');

        stickers.forEach(sticker => {
            console.log('Adding sticker:', sticker);
            const img = document.createElement('img');
            img.src = sticker.stickerUrl;
            img.className = 'sticker-img';
            img.dataset.url = sticker.stickerUrl;
            img.alt = sticker.stickerName;
            img.onerror = function() {
                console.error('Failed to load sticker image:', this.src);
            };
            grid.appendChild(img);
        });
    }

    // Khởi tạo sticker panel khi DOM ready
    function initializeStickerPanel() {
        const stickerPanelHtml = `
            <div class="sticker-panel">
                <div class="sticker-header">
                    <h6>Stickers</h6>
                    <button class="sticker-close">&times;</button>
                </div>
                <div class="sticker-grid"></div>
            </div>
        `;
        // document.body.insertAdjacentHTML('beforeend', stickerPanelHtml);

        const inputBar = document.querySelector('.chat-input-bar');
        if (inputBar) {
            inputBar.insertAdjacentHTML('beforeend', stickerPanelHtml);
        }

        // Load stickers khi khởi tạo
        loadStickers();
    }

    // Event delegation cho click
    document.addEventListener('click', e => {
        // Chọn friend
        const friendItem = e.target.closest('.friend-item');
        if (friendItem) {
            currentFriendId = Number(friendItem.dataset.id);

            // Remove active class from all friend items
            document.querySelectorAll('.friend-item')
                .forEach(el => el.classList.remove('active'));
            friendItem.classList.add('active');

            // Set friend names
            const friendNameEl = friendItem.querySelector('.friend-name');
            if (chatFriendNameEl && friendNameEl) {
                chatFriendNameEl.textContent = friendNameEl.textContent;
            }

            // Hiện/ẩn khung thông tin user
            chatUserInfoEl.classList.remove('d-none');
            chatUserInfoEl.style.display = '';
            chatUserInfoEl.querySelectorAll('.chat-user')
                .forEach(u => u.style.display = 'none');
            const sel = chatUserInfoEl.querySelector(
                `.chat-user[data-id="${currentFriendId}"]`
            );
            if (sel) sel.style.display = '';

            // Ẩn placeholder nếu chưa có tin nhắn
            const ph = chatContentEl.querySelector('.no-chat-placeholder');
            if (ph) ph.style.display = 'none';

            // Load thông tin sidebar cho friend
            loadFriendSidebar(currentFriendId);

            // Load lịch sử chat
            fetch(`/Chat/GetMessages?friendId=${currentFriendId}`)
                .then(res => res.text())
                .then(html => {
                    const tmp = document.createElement('div');
                    tmp.innerHTML = html.trim();
                    const first = tmp.firstElementChild;
                    chatContentEl.innerHTML =
                        (first && first.classList.contains('chat-content'))
                            ? first.innerHTML
                            : html;
                    scrollToBottom();
                })
                .catch(err => console.error('Load history error:', err));
            return;
        }

        // Click nút gửi
        if (e.target.matches('.btn-send, .bi-send')) {
            sendMessage();
            return;
        }

        // Click nút video call
        const videoCallBtn = e.target.closest('.btn-video-call');
        if (videoCallBtn) {
            const peerId = videoCallBtn.dataset.peer;
            if (peerId) {
                window.location.href = '/VideoCall/Room?userId=' + peerId;
            }
            return;
        }

        // Toggle sticker panel
        if (e.target.matches('.sticker-btn')) {
            stickerPanelVisible = !stickerPanelVisible;
            const stickerPanel = document.querySelector('.sticker-panel');
            if (stickerPanel) {
                stickerPanel.style.display = stickerPanelVisible ? 'block' : 'none';
            }
            return;
        }

        // Đóng sticker panel
        if (e.target.matches('.sticker-close')) {
            stickerPanelVisible = false;
            const stickerPanel = document.querySelector('.sticker-panel');
            if (stickerPanel) {
                stickerPanel.style.display = 'none';
            }
            return;
        }

        // Chọn và gửi sticker
        if (e.target.matches('.sticker-img')) {
            if (!currentFriendId) return;

            const stickerUrl = e.target.dataset.url;

            connection.invoke("SendSticker", currentFriendId, stickerUrl)
                .then(() => {
                    console.log("Sticker đã gửi thành công");
                    // Ẩn panel sau khi gửi
                    stickerPanelVisible = false;
                    const stickerPanel = document.querySelector('.sticker-panel');
                    if (stickerPanel) {
                        stickerPanel.style.display = 'none';
                    }
                })
                .catch(err => console.error(err.toString()));
            return;
        }

        const stickerPanel = document.querySelector('.sticker-panel');
        if (stickerPanel && !stickerPanel.contains(e.target) && !e.target.closest('.sticker-btn')) {
            stickerPanel.style.display = 'none';
            stickerPanelVisible = false;
        }
    });

    // Gửi khi Enter
    chatInputEl.addEventListener('keypress', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    // Thêm event listener cho Enter trên toàn bộ chat input area
    document.addEventListener('keydown', function (e) {
        // Chỉ xử lý khi đang focus vào chat input hoặc preview area
        const isInChatInput = e.target.closest('.chat-input-container') ||
            e.target === chatInputEl ||
            document.activeElement === chatInputEl;

        if (e.key === 'Enter' && !e.shiftKey && isInChatInput) {
            // Kiểm tra xem có file hoặc text không
            const hasFiles = selectedFiles.length > 0;
            const hasText = chatInputEl.value.trim();

            if (hasFiles || hasText) {
                e.preventDefault();
                sendMessage();
            }
        }
    });

    // Khi click vào icon ảnh hoặc file, mở input file
    const imageIcon = document.querySelector('.bi-image');
    const paperclipIcon = document.querySelector('.bi-paperclip');

    if (imageIcon) {
        imageIcon.addEventListener('click', function () {
            if (fileInputEl) fileInputEl.click();
        });
    }

    if (paperclipIcon) {
        paperclipIcon.addEventListener('click', function () {
            if (fileInputEl) fileInputEl.click();
        });
    }

    // Load messages function (from jQuery version)
    async function loadMessages(friendId) {
        const res = await fetch(`/Chat/GetMessages?friendId=${friendId}`);
        const html = await res.text();
        chatContentEl.innerHTML = html;
        scrollToBottom();
    }

    // File input change handler - Hỗ trợ nhiều file với tính năng tích lũy
    if (fileInputEl) {
        fileInputEl.addEventListener('change', function () {
            if (this.files && this.files.length > 0) {
                // Validate từng file mới
                const newValidFiles = [];
                for (let i = 0; i < this.files.length; i++) {
                    const file = this.files[i];
                    if (file.size > 50 * 1024 * 1024) {
                        alert(`File "${file.name}" quá lớn! Vui lòng chọn file nhỏ hơn 50MB.`);
                        continue;
                    }

                    // Kiểm tra trùng lặp (dựa trên tên và kích thước)
                    const isDuplicate = selectedFiles.some(existingFile =>
                        existingFile.name === file.name && existingFile.size === file.size
                    );

                    if (isDuplicate) {
                        console.log(`File "${file.name}" đã được chọn rồi, bỏ qua.`);
                        continue;
                    }

                    newValidFiles.push(file);
                }

                if (newValidFiles.length === 0) {
                    this.value = "";
                    if (selectedFiles.length === 0) {
                        // Không có file nào, ẩn preview
                        if (previewEl) {
                            previewEl.style.display = "none";
                            previewEl.innerHTML = "";
                        }
                    }
                    return;
                }

                // Thêm file mới vào danh sách hiện có (tích lũy)
                selectedFiles = [...selectedFiles, ...newValidFiles];
                console.log(`Đã thêm ${newValidFiles.length} file mới. Tổng cộng: ${selectedFiles.length} file(s)`);

                // Hiển thị thông báo khi thêm file mới (nếu đã có file trước đó)
                if (selectedFiles.length > newValidFiles.length) {
                    showSuccessToast(`Đã thêm ${newValidFiles.length} file mới. Tổng cộng: ${selectedFiles.length} file(s)`);
                }

                if (previewEl) {
                    previewEl.style.display = "flex";
                    previewEl.innerHTML = '';

                    // Thêm loading placeholder
                    const loadingItem = document.createElement('div');
                    loadingItem.className = 'preview-loading';
                    loadingItem.innerHTML = `
                        <div class="loading-spinner"></div>
                        <span>Đang tải ${selectedFiles.length} file${selectedFiles.length > 1 ? 's' : ''}...</span>
                    `;
                    previewEl.appendChild(loadingItem);

                    // Simulate loading delay cho smooth effect
                    setTimeout(() => {
                        previewEl.innerHTML = '';
                        loadMultipleFiles();
                    }, 300);

                    function loadMultipleFiles() {
                        // Tạo container cho tất cả preview items
                        const previewContainer = document.createElement('div');
                        previewContainer.className = 'multiple-preview-container';
                        previewContainer.style.cssText = `
                            display: flex;
                            flex-wrap: wrap;
                            gap: 8px;
                            max-height: 120px;
                            overflow-y: auto;
                            width: 100%;
                            padding: 4px;
                        `;

                        selectedFiles.forEach((file, index) => {
                            const previewItem = document.createElement('div');
                            previewItem.className = 'preview-item';
                            previewItem.style.position = 'relative';
                            previewItem.style.flexShrink = '0';
                            previewItem.dataset.fileIndex = index;

                            if (file.type.startsWith('image/')) {
                                const img = document.createElement('img');
                                img.src = URL.createObjectURL(file);
                                img.style.cssText = `
                                    width: 60px;
                                    height: 60px;
                                    object-fit: cover;
                                    border-radius: 8px;
                                    border: 2px solid var(--theme-border-light, rgba(255, 255, 255, 0.1));
                                `;
                                previewItem.appendChild(img);
                            } else if (file.type.startsWith('video/')) {
                                const video = document.createElement('video');
                                video.src = URL.createObjectURL(file);
                                video.style.cssText = `
                                    width: 60px;
                                    height: 60px;
                                    object-fit: cover;
                                    border-radius: 8px;
                                    border: 2px solid var(--theme-border-light, rgba(255, 255, 255, 0.1));
                                `;
                                previewItem.appendChild(video);
                            } else {
                                // File preview container
                                const filePreview = document.createElement('div');
                                filePreview.className = 'file-preview';
                                filePreview.style.cssText = `
                                    width: 60px;
                                    height: 60px;
                                    display: flex;
                                    flex-direction: column;
                                    align-items: center;
                                    justify-content: center;
                                    background: var(--theme-bg-tertiary, rgba(255, 255, 255, 0.05));
                                    border-radius: 8px;
                                    border: 2px solid var(--theme-border-light, rgba(255, 255, 255, 0.1));
                                    font-size: 10px;
                                    text-align: center;
                                    padding: 4px;
                                `;

                                const fileIcon = document.createElement('div');
                                fileIcon.innerHTML = '<i class="bi bi-file-earmark-text"></i>';
                                fileIcon.style.fontSize = '16px';

                                const fileName = document.createElement('div');
                                fileName.textContent = file.name.length > 8 ? file.name.substring(0, 8) + '...' : file.name;
                                fileName.style.cssText = `
                                    font-size: 8px;
                                    margin-top: 2px;
                                    word-break: break-all;
                                    line-height: 1.2;
                                `;

                                filePreview.appendChild(fileIcon);
                                filePreview.appendChild(fileName);
                                previewItem.appendChild(filePreview);
                            }

                            // Thêm nút xóa cho từng file
                            const removeBtn = document.createElement('button');
                            removeBtn.className = 'preview-remove';
                            removeBtn.innerHTML = '✕';
                            removeBtn.title = `Xóa file ${file.name}`;
                            removeBtn.type = 'button';
                            removeBtn.style.cssText = `
                                position: absolute !important;
                                top: -6px !important;
                                right: -6px !important;
                                background: var(--theme-accent-red, #FF5A57) !important;
                                color: var(--theme-text-primary, white) !important;
                                border-radius: 50% !important;
                                width: 18px !important;
                                height: 18px !important;
                                display: flex !important;
                                align-items: center !important;
                                justify-content: center !important;
                                font-size: 10px !important;
                                cursor: pointer !important;
                                border: 1px solid var(--theme-bg-secondary, rgba(27, 32, 98, 0.85)) !important;
                                font-weight: bold !important;
                                z-index: 9999 !important;
                                line-height: 1 !important;
                                box-shadow: var(--theme-shadow-sm, 0 2px 8px rgba(5, 12, 56, 0.2)) !important;
                                transition: all 0.2s ease !important;
                            `;

                            // Thêm hover effects
                            removeBtn.addEventListener('mouseenter', function() {
                                this.style.background = 'var(--theme-accent-green, #E02F75)';
                                this.style.transform = 'scale(1.1)';
                            });

                            removeBtn.addEventListener('mouseleave', function() {
                                this.style.background = 'var(--theme-accent-red, #FF5A57)';
                                this.style.transform = 'scale(1)';
                            });

                            removeBtn.addEventListener('click', function(e) {
                                e.preventDefault();
                                e.stopPropagation();
                                const fileIndex = parseInt(previewItem.dataset.fileIndex);

                                // Xóa file khỏi selectedFiles array
                                selectedFiles.splice(fileIndex, 1);

                                // Nếu không còn file nào, ẩn preview
                                if (selectedFiles.length === 0) {
                                    previewEl.style.display = 'none';
                                    previewEl.innerHTML = '';
                                    fileInputEl.value = '';
                                    updateInputPlaceholder();
                                } else {
                                    // Refresh lại preview để cập nhật nút và indices
                                    previewEl.innerHTML = '';
                                    setTimeout(() => {
                                        loadMultipleFiles();
                                    }, 100);
                                }

                                console.log(`File removed. Remaining: ${selectedFiles.length} file(s)`);
                            });

                            previewItem.appendChild(removeBtn);
                            previewContainer.appendChild(previewItem);
                        });

                        // Thêm nút "Thêm file" để người dùng có thể chọn thêm
                        const addMoreBtn = document.createElement('button');
                        addMoreBtn.className = 'add-more-files-btn';
                        addMoreBtn.innerHTML = '<i class="bi bi-plus"></i>';
                        addMoreBtn.title = 'Thêm file khác';
                        addMoreBtn.type = 'button';

                        addMoreBtn.addEventListener('click', function(e) {
                            e.preventDefault();
                            e.stopPropagation();
                            fileInputEl.click(); // Mở file picker
                        });

                        previewContainer.appendChild(addMoreBtn);

                        // Thêm nút xóa tất cả nếu có nhiều hơn 1 file
                        if (selectedFiles.length > 1) {
                            const clearAllBtn = document.createElement('button');
                            clearAllBtn.className = 'clear-all-btn';
                            clearAllBtn.innerHTML = '<i class="bi bi-trash"></i>';
                            clearAllBtn.title = 'Xóa tất cả file';
                            clearAllBtn.type = 'button';
                            clearAllBtn.style.cssText = `
                                width: 60px;
                                height: 60px;
                                background: var(--theme-bg-tertiary, rgba(255, 255, 255, 0.05));
                                border: 2px dashed var(--theme-accent-red, #FF5A57);
                                border-radius: 8px;
                                color: var(--theme-accent-red, #FF5A57);
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                cursor: pointer;
                                font-size: 16px;
                                transition: all 0.2s ease;
                            `;

                            clearAllBtn.addEventListener('mouseenter', function() {
                                this.style.background = 'var(--theme-accent-red, #FF5A57)';
                                this.style.color = 'white';
                            });

                            clearAllBtn.addEventListener('mouseleave', function() {
                                this.style.background = 'var(--theme-bg-tertiary, rgba(255, 255, 255, 0.05))';
                                this.style.color = 'var(--theme-accent-red, #FF5A57)';
                            });

                            clearAllBtn.addEventListener('click', function(e) {
                                e.preventDefault();
                                e.stopPropagation();
                                selectedFiles = [];
                                previewEl.style.display = 'none';
                                previewEl.innerHTML = '';
                                fileInputEl.value = '';
                                updateInputPlaceholder();
                                console.log('All files cleared');
                            });

                            previewContainer.appendChild(clearAllBtn);
                        }

                        previewEl.appendChild(previewContainer);
                        updateInputPlaceholder();

                        console.log(`Preview created for ${selectedFiles.length} file(s)`);
                    }
                } else {
                    selectedFiles = [];
                    if (previewEl) {
                        previewEl.style.display = "none";
                        previewEl.innerHTML = "";
                    }
                }

                // Reset file input để có thể chọn lại cùng file
                this.value = "";
            }
        });
    }

    // Khởi tạo khi DOM đã sẵn sàng
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeStickerPanel);
    } else {
        initializeStickerPanel();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelector('.chat-content').addEventListener('dragstart', function (e) {
            if (e.target.classList.contains('sticker-message')) {
                e.preventDefault();
            }
        });
    });

    // Update input placeholder based on preview state
    function updateInputPlaceholder() {
        const fileCount = selectedFiles.length;

        if (fileCount > 0) {
            chatInputEl.placeholder = `Nhập tin nhắn kèm ${fileCount} file${fileCount > 1 ? 's' : ''}... (Enter để gửi)`;
            chatInputEl.style.fontStyle = "normal";
        } else {
            chatInputEl.placeholder = "Nhập tin nhắn...";
            chatInputEl.style.fontStyle = "";
        }
    }

    // Load thông tin sidebar cho friend
    async function loadFriendSidebar(friendId) {
        try {
            const response = await fetch(`/Chat/GetFriendSidebar?friendId=${friendId}`);
            if (response.ok) {
                const html = await response.text();
                const sidebarRight = document.getElementById('sidebar-right');
                if (sidebarRight) {
                    sidebarRight.innerHTML = html;
                }
            } else {
                console.error('Failed to load friend sidebar info');
            }
        } catch (error) {
            console.error('Error loading friend sidebar:', error);
        }
    }

    //=======================================================================================
    //                              DRAG & DROP FUNCTIONALITY
    //=======================================================================================

    function initializeDragAndDrop() {
        const dragDropOverlay = document.getElementById('drag-drop-overlay');
        const chatContent = document.querySelector('.chat-content');
        const chatInputContainer = document.querySelector('.chat-input-container');

        if (!dragDropOverlay) return;

        let dragCounter = 0;
        let isDragging = false;

        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            document.addEventListener(eventName, preventDefaults, false);
        });

        function preventDefaults(e) {
            e.preventDefault();
            e.stopPropagation();
        }

        // Handle drag enter - only show overlay when dragging files from outside
        document.addEventListener('dragenter', function(e) {
            // Check if dragging files (not internal elements)
            if (e.dataTransfer && e.dataTransfer.types && e.dataTransfer.types.includes('Files')) {
                dragCounter++;
                if (dragCounter === 1) {
                    isDragging = true;
                    dragDropOverlay.classList.add('active');
                }
            }
        });

        // Handle drag leave
        document.addEventListener('dragleave', function(e) {
            if (isDragging) {
                dragCounter--;
                if (dragCounter <= 0) {
                    dragCounter = 0;
                    isDragging = false;
                    dragDropOverlay.classList.remove('active');
                    if (chatContent) chatContent.classList.remove('drag-over');
                    if (chatInputContainer) chatInputContainer.classList.remove('drag-over');
                }
            }
        });

        // Handle drag over on chat areas
        if (chatContent) {
            chatContent.addEventListener('dragover', function(e) {
                if (isDragging) {
                    chatContent.classList.add('drag-over');
                }
            });

            chatContent.addEventListener('dragleave', function(e) {
                if (!chatContent.contains(e.relatedTarget)) {
                    chatContent.classList.remove('drag-over');
                }
            });
        }

        if (chatInputContainer) {
            chatInputContainer.addEventListener('dragover', function(e) {
                if (isDragging) {
                    chatInputContainer.classList.add('drag-over');
                }
            });

            chatInputContainer.addEventListener('dragleave', function(e) {
                if (!chatInputContainer.contains(e.relatedTarget)) {
                    chatInputContainer.classList.remove('drag-over');
                }
            });
        }

        // Handle drop
        document.addEventListener('drop', function(e) {
            console.log('Drop event triggered, isDragging:', isDragging);
            console.log('DataTransfer files:', e.dataTransfer.files);

            if (isDragging) {
                dragCounter = 0;
                isDragging = false;
                dragDropOverlay.classList.remove('active');
                if (chatContent) chatContent.classList.remove('drag-over');
                if (chatInputContainer) chatInputContainer.classList.remove('drag-over');

                const files = e.dataTransfer.files;
                console.log('Files detected:', files.length);

                if (files.length > 0) {
                    console.log('Calling handleDroppedFiles with', files.length, 'files');
                    handleDroppedFiles(files);
                } else {
                    console.log('❌ No files in drop event');
                }
            } else {
                console.log('❌ Drop event ignored - not in dragging state');
            }
        });

        // Handle window focus/blur to reset state
        window.addEventListener('focus', function() {
            if (isDragging) {
                dragCounter = 0;
                isDragging = false;
                dragDropOverlay.classList.remove('active');
                if (chatContent) chatContent.classList.remove('drag-over');
                if (chatInputContainer) chatInputContainer.classList.remove('drag-over');
            }
        });
    }

    function handleDroppedFiles(files) {
        console.log('handleDroppedFiles called with:', files.length, 'files');
        console.log('selectedFiles before:', selectedFiles ? selectedFiles.length : 'undefined');

        // Initialize selectedFiles if not exists
        if (!window.selectedFiles) {
            window.selectedFiles = [];
            console.log('Initialized selectedFiles array');
        }

        // Convert FileList to Array and add to selectedFiles
        const droppedFiles = Array.from(files);
        console.log('Dropped files:', droppedFiles.map(f => f.name));

        // Find existing file input and simulate file selection
        let fileInput = document.getElementById('file-input');

        if (!fileInput) {
            // Try to find any file input
            fileInput = document.querySelector('input[type="file"]');
        }

        if (!fileInput) {
            console.log('No file input found - creating one');
            // Create file input if not exists
            fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.id = 'file-input';
            fileInput.multiple = true;
            fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
            fileInput.style.display = 'none';
            document.body.appendChild(fileInput);
        }

        // Validate and add files to selectedFiles array
        let newFilesAdded = 0;
        let invalidFiles = 0;
        let duplicatesFound = 0;

        droppedFiles.forEach(file => {
            console.log('Processing file:', file.name, file.type, file.size);
            console.log('Current selectedFiles:', selectedFiles.map(f => f.name));

            // Basic validation
            if (file.size > 50 * 1024 * 1024) { // 50MB limit
                console.log('❌ File too large:', file.name);
                invalidFiles++;
                return;
            }

            // Check for duplicates (more specific check)
            const isDuplicate = selectedFiles.some(existingFile => {
                const nameMatch = existingFile.name === file.name;
                const sizeMatch = existingFile.size === file.size;
                const typeMatch = existingFile.type === file.type;

                console.log(`Checking duplicate: ${file.name} vs ${existingFile.name}`, {
                    nameMatch, sizeMatch, typeMatch
                });

                return nameMatch && sizeMatch && typeMatch;
            });

            if (!isDuplicate) {
                selectedFiles.push(file);
                newFilesAdded++;
                console.log('✅ Added file:', file.name, 'Total files:', selectedFiles.length);
            } else {
                duplicatesFound++;
                console.log('⚠️ Duplicate file:', file.name);
            }
        });

        console.log('Total selected files:', selectedFiles.length);
        console.log('New files added:', newFilesAdded);

        // Trigger the existing file preview system
        if (selectedFiles.length > 0) {
            // Create a fake event to trigger existing file handling
            const fakeEvent = {
                target: {
                    files: selectedFiles,
                    value: ''
                }
            };

            // Try to find and trigger existing file change handler
            if (fileInput && fileInput.onchange) {
                fileInput.onchange(fakeEvent);
            } else {
                // Manually trigger file preview update
                triggerFilePreviewUpdate();
            }
        }

        // Show feedback
        if (newFilesAdded > 0) {
            const message = newFilesAdded === 1
                ? `Đã thêm ${newFilesAdded} file`
                : `Đã thêm ${newFilesAdded} files`;

            if (typeof showToast === 'function') {
                showToast(message, 'success');
            } else {
                console.log('✅', message);
            }
        }

        if (duplicatesFound > 0) {
            const dupMessage = `${duplicatesFound} file đã có trong danh sách`;
            if (typeof showToast === 'function') {
                showToast(dupMessage, 'warning');
            } else {
                console.log('⚠️', dupMessage);
            }
        }

        if (invalidFiles > 0) {
            const errorMessage = `${invalidFiles} file không hợp lệ (quá lớn hoặc không được hỗ trợ)`;
            if (typeof showToast === 'function') {
                showToast(errorMessage, 'error');
            } else {
                console.log('❌', errorMessage);
            }
        }

        // Auto-focus message input
        const messageInput = document.getElementById('message-input');
        if (messageInput) {
            messageInput.focus();
            console.log('Message input focused');
        }
    }

    // Trigger existing file preview system
    function triggerFilePreviewUpdate() {
        console.log('Triggering file preview update for', selectedFiles.length, 'files');

        // Try to find existing preview function
        if (typeof loadMultipleFiles === 'function') {
            console.log('Using existing loadMultipleFiles function');
            const previewEl = document.getElementById('file-preview');
            if (previewEl) {
                previewEl.style.display = 'block';
                loadMultipleFiles();
                console.log('✅ loadMultipleFiles called successfully');
            } else {
                console.log('❌ Preview element not found, creating simple preview');
                createSimplePreview();
            }
        } else {
            console.log('❌ loadMultipleFiles not found, trying alternative methods');

            // Try to trigger file input change event
            const fileInput = document.getElementById('file-input');
            if (fileInput) {
                // Set the files property to trigger existing handlers
                Object.defineProperty(fileInput, 'files', {
                    value: selectedFiles,
                    writable: false
                });

                const event = new Event('change', { bubbles: true });
                fileInput.dispatchEvent(event);
                console.log('Dispatched change event on file input');
            } else {
                console.log('No file input found, creating simple preview');
                createSimplePreview();
            }
        }
    }

    // Create a beautiful preview matching the app's design
    function createSimplePreview() {
        console.log('Creating beautiful preview for', selectedFiles.length, 'files');

        // Remove existing preview
        let previewEl = document.getElementById('file-preview');
        if (previewEl) {
            previewEl.remove();
        }

        // Create new preview element with beautiful styling
        previewEl = document.createElement('div');
        previewEl.id = 'file-preview';
        previewEl.style.cssText = `
            position: fixed;
            bottom: 90px;
            left: 50%;
            transform: translateX(-50%);
            background: linear-gradient(135deg, rgba(27, 32, 98, 0.95) 0%, rgba(45, 52, 120, 0.95) 100%);
            border: 1px solid rgba(255, 255, 255, 0.15);
            border-radius: 16px;
            padding: 16px;
            z-index: 1000;
            max-width: 85%;
            min-width: 300px;
            backdrop-filter: blur(20px);
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 0 1px rgba(255, 255, 255, 0.05);
            animation: slideUp 0.3s ease-out;
        `;

        // Add keyframes for animation
        if (!document.getElementById('preview-styles')) {
            const style = document.createElement('style');
            style.id = 'preview-styles';
            style.textContent = `
                @keyframes slideUp {
                    from { transform: translateX(-50%) translateY(20px); opacity: 0; }
                    to { transform: translateX(-50%) translateY(0); opacity: 1; }
                }
                .file-preview-item:hover {
                    transform: scale(1.02);
                    background: rgba(255, 255, 255, 0.15) !important;
                }
                .file-preview-remove:hover {
                    background: #ff3838 !important;
                    transform: scale(1.1);
                }
            `;
            document.head.appendChild(style);
        }

        // Header with icon and title
        const header = document.createElement('div');
        header.style.cssText = `
            display: flex; 
            align-items: center; 
            justify-content: space-between; 
            margin-bottom: 12px;
            padding-bottom: 8px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        `;

        const titleContainer = document.createElement('div');
        titleContainer.style.cssText = 'display: flex; align-items: center; gap: 8px;';

        const fileIcon = document.createElement('div');
        fileIcon.innerHTML = '📎';
        fileIcon.style.cssText = 'font-size: 16px;';

        const title = document.createElement('span');
        title.textContent = `${selectedFiles.length} file${selectedFiles.length > 1 ? 's' : ''} đã chọn`;
        title.style.cssText = 'color: white; font-size: 14px; font-weight: 600;';

        titleContainer.appendChild(fileIcon);
        titleContainer.appendChild(title);

        // Clear all button
        const clearBtn = document.createElement('button');
        clearBtn.innerHTML = '🗑️';
        clearBtn.title = 'Xóa tất cả';
        clearBtn.style.cssText = `
            background: rgba(255, 69, 87, 0.2);
            color: #ff4557;
            border: 1px solid rgba(255, 69, 87, 0.3);
            border-radius: 8px;
            width: 32px;
            height: 32px;
            cursor: pointer;
            font-size: 14px;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: all 0.2s ease;
        `;
        clearBtn.onmouseover = function() {
            this.style.background = 'rgba(255, 69, 87, 0.3)';
            this.style.transform = 'scale(1.05)';
        };
        clearBtn.onmouseout = function() {
            this.style.background = 'rgba(255, 69, 87, 0.2)';
            this.style.transform = 'scale(1)';
        };
        clearBtn.onclick = function() {
            selectedFiles = [];
            previewEl.style.animation = 'slideUp 0.2s ease-out reverse';
            setTimeout(() => previewEl.remove(), 200);
            console.log('All files cleared');
        };

        header.appendChild(titleContainer);
        header.appendChild(clearBtn);
        previewEl.appendChild(header);

        // File grid
        const fileGrid = document.createElement('div');
        fileGrid.style.cssText = `
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
            gap: 12px;
            max-height: 240px;
            overflow-y: auto;
            padding: 4px;
        `;

        selectedFiles.forEach((file, index) => {
            const fileItem = document.createElement('div');
            fileItem.className = 'file-preview-item';
            fileItem.style.cssText = `
                background: rgba(255, 255, 255, 0.08);
                border: 1px solid rgba(255, 255, 255, 0.1);
                border-radius: 12px;
                padding: 12px 8px;
                color: white;
                text-align: center;
                position: relative;
                cursor: pointer;
                transition: all 0.2s ease;
                min-height: 80px;
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 6px;
            `;

            // File icon/thumbnail
            const iconContainer = document.createElement('div');
            iconContainer.style.cssText = 'font-size: 24px; margin-bottom: 4px;';

            if (file.type.startsWith('image/')) {
                const img = document.createElement('img');
                img.src = URL.createObjectURL(file);
                img.style.cssText = `
                    width: 40px;
                    height: 40px;
                    object-fit: cover;
                    border-radius: 6px;
                    border: 1px solid rgba(255, 255, 255, 0.2);
                `;
                iconContainer.appendChild(img);
            } else {
                let iconHtml = '';
                let iconColor = '';

                if (file.type.includes('pdf')) {
                    iconHtml = '<i class="bi bi-file-pdf"></i>';
                    iconColor = '#ff6b6b';
                } else if (file.type.includes('word')) {
                    iconHtml = '<i class="bi bi-file-word"></i>';
                    iconColor = '#4dabf7';
                } else if (file.type.includes('video')) {
                    iconHtml = '<i class="bi bi-file-play"></i>';
                    iconColor = '#69db7c';
                } else if (file.type.includes('excel') || file.type.includes('sheet')) {
                    iconHtml = '<i class="bi bi-file-excel"></i>';
                    iconColor = '#51cf66';
                } else {
                    iconHtml = '<i class="bi bi-file-earmark"></i>';
                    iconColor = '#ced4da';
                }

                iconContainer.innerHTML = iconHtml;
                iconContainer.style.color = iconColor;
                iconContainer.style.fontSize = '28px';
            }

            // File name
            const fileName = document.createElement('div');
            fileName.textContent = file.name.length > 12 ? file.name.substring(0, 12) + '...' : file.name;
            fileName.title = file.name;
            fileName.style.cssText = `
                font-size: 10px;
                color: rgba(255, 255, 255, 0.8);
                line-height: 1.2;
                max-width: 100%;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            `;

            // File size
            const fileSize = document.createElement('div');
            const sizeInMB = (file.size / (1024 * 1024)).toFixed(1);
            fileSize.textContent = sizeInMB + ' MB';
            fileSize.style.cssText = `
                font-size: 9px;
                color: rgba(255, 255, 255, 0.5);
                margin-top: 2px;
            `;

            // Remove button
            const removeBtn = document.createElement('button');
            removeBtn.innerHTML = '✕';
            removeBtn.className = 'file-preview-remove';
            removeBtn.title = 'Xóa file';
            removeBtn.style.cssText = `
                position: absolute;
                top: -6px;
                right: -6px;
                background: #ff4757;
                color: white;
                border: none;
                border-radius: 50%;
                width: 20px;
                height: 20px;
                font-size: 11px;
                cursor: pointer;
                display: flex;
                align-items: center;
                justify-content: center;
                font-weight: bold;
                box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
                transition: all 0.2s ease;
                z-index: 10;
            `;

            removeBtn.onclick = function(e) {
                e.stopPropagation();
                selectedFiles.splice(index, 1);
                if (selectedFiles.length === 0) {
                    previewEl.style.animation = 'slideUp 0.2s ease-out reverse';
                    setTimeout(() => previewEl.remove(), 200);
                } else {
                    createSimplePreview(); // Refresh preview
                }
                console.log('File removed, remaining:', selectedFiles.length);
            };

            fileItem.appendChild(iconContainer);
            fileItem.appendChild(fileName);
            fileItem.appendChild(fileSize);
            fileItem.appendChild(removeBtn);
            fileGrid.appendChild(fileItem);
        });

        // Add "Add more" button
        const addMoreBtn = document.createElement('div');
        addMoreBtn.style.cssText = `
            background: rgba(255, 255, 255, 0.05);
            border: 2px dashed rgba(255, 255, 255, 0.3);
            border-radius: 12px;
            padding: 12px 8px;
            color: rgba(255, 255, 255, 0.6);
            text-align: center;
            cursor: pointer;
            transition: all 0.2s ease;
            min-height: 80px;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 6px;
        `;

        addMoreBtn.innerHTML = `
            <i class="bi bi-plus" style="font-size: 24px;"></i>
            <div style="font-size: 10px; color: rgba(255, 255, 255, 0.5);">Thêm file</div>
        `;

        addMoreBtn.onmouseover = function() {
            this.style.background = 'rgba(255, 255, 255, 0.1)';
            this.style.borderColor = 'rgba(255, 255, 255, 0.5)';
            this.style.color = 'rgba(255, 255, 255, 0.9)';
        };
        addMoreBtn.onmouseout = function() {
            this.style.background = 'rgba(255, 255, 255, 0.05)';
            this.style.borderColor = 'rgba(255, 255, 255, 0.3)';
            this.style.color = 'rgba(255, 255, 255, 0.6)';
        };

        addMoreBtn.onclick = function() {
            let fileInput = document.querySelector('input[type="file"]');
            if (!fileInput) {
                fileInput = document.createElement('input');
                fileInput.type = 'file';
                fileInput.multiple = true;
                fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
                fileInput.style.display = 'none';
                document.body.appendChild(fileInput);

                fileInput.addEventListener('change', function(e) {
                    if (e.target.files.length > 0) {
                        handleDroppedFiles(e.target.files);
                    }
                });
            }
            fileInput.click();
        };

        fileGrid.appendChild(addMoreBtn);
        previewEl.appendChild(fileGrid);
        document.body.appendChild(previewEl);

        console.log('✅ Beautiful preview created');
    }



    // Initialize drag and drop when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeDragAndDrop);
    } else {
        initializeDragAndDrop();
    }
})();
        
