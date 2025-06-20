(() => {
    // ID của friend đang chat
    let currentFriendId = null;
    let stickerPanelVisible = false;
    let availableStickers = [];

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
    async function sendMessage() {
        const content = chatInputEl.value.trim();
        const file = fileInputEl ? fileInputEl.files[0] : null;
        const friendId = currentFriendId;
        if (!content && !file) return;

        if (file) {
            // Gửi file qua HTTP POST
            const formData = new FormData();
            formData.append('content', content);
            formData.append('friendId', friendId);
            formData.append('file', file);

            const res = await fetch('/Chat/SendMessage', {
                method: 'POST',
                body: formData
            });
            if (res.ok) {
                chatInputEl.value = '';
                if (fileInputEl) fileInputEl.value = '';
                if (previewEl) {
                    previewEl.innerHTML = '';
                    previewEl.style.display = 'none';
                }
            }
            return;
        } else {
            // Gửi text qua SignalR
            connection.invoke('SendMessage', friendId, content)
                .then(() => {
                    chatInputEl.value = '';
                    console.log("Tin nhắn đã gửi thành công");
                })
                .catch(err => console.error('SendMessage error:', err));
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
                    const fileName = media.url.split('/').pop();
                    mediaHtml += `
                    <div class="chat-file-attachment">
                        <span class="file-icon"><i class="bi bi-file-earmark-text"></i></span>
                        <span class="file-name">${fileName}</span>
                        <a href="${media.url}" download class="file-download-btn" title="Tải về"><i class="bi bi-download"></i></a>
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
            textHtml = `<span class="message-bubble">${msg.content}</span>`;
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
        document.body.insertAdjacentHTML('beforeend', stickerPanelHtml);

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
    });

    // Gửi khi Enter
    chatInputEl.addEventListener('keypress', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
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

    // File input change handler
    if (fileInputEl) {
        fileInputEl.addEventListener('change', function () {
            if (this.files && this.files.length > 0) {
                // Nếu chỉ cho phép 1 file:
                if (this.files[0].size > 50 * 1024 * 1024) {
                    alert("File quá lớn! Vui lòng chọn file nhỏ hơn 50MB.");
                    this.value = "";
                    return;
                }

                if (previewEl) {
                    previewEl.style.display = "flex";
                    previewEl.innerHTML = '';
                    const file = this.files[0];
                    if (file.type.startsWith('image/')) {
                        const img = document.createElement('img');
                        img.src = URL.createObjectURL(file);
                        img.className = 'message-media-img';
                        previewEl.appendChild(img);
                    } else if (file.type.startsWith('video/')) {
                        const video = document.createElement('video');
                        video.className = 'message-media-video';
                        video.src = URL.createObjectURL(file);
                        video.controls = true;
                        previewEl.appendChild(video);
                    } else {
                        const p = document.createElement('p');
                        p.textContent = file.name;
                        previewEl.appendChild(p);
                    }
                }
            } else {
                if (previewEl) {
                    previewEl.style.display = "none";
                    previewEl.innerHTML = "";
                }
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
})();