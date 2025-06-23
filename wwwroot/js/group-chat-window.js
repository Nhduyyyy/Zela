// Global variables
let currentGroupId = null;
let currentUserId = null;
let selectedFiles = [];

// Initialize current user ID from the page
function initializeCurrentUserId() {
    // Try to get from data attribute or global variable
    const userIdElement = document.querySelector('[data-current-user-id]');
    if (userIdElement) {
        currentUserId = userIdElement.dataset.currentUserId;
        console.log("currentUserId set from data attribute:", currentUserId);
    } else if (typeof window.currentUserId !== 'undefined') {
        currentUserId = window.currentUserId;
        console.log("currentUserId set from window variable:", currentUserId);
    } else {
        console.warn("currentUserId not found!");
    }
}

// Kết nối SignalR với UserId
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", {
        accessTokenFactory: () => currentUserId
    })
    .build();

// Nhận message mới từ nhóm (realtime)
connection.on("ReceiveGroupMessage", function (msg) {
    console.log("SignalR nhận được tin nhắn nhóm:", msg);
    console.log("currentUserId:", currentUserId, "type:", typeof currentUserId);
    console.log("msg.senderId:", msg.senderId, "type:", typeof msg.senderId);

    let currentId = Number(currentGroupId);

    if (currentId && msg.groupId === currentId) {
        // Set isMine based on senderId comparison - ensure both are numbers
        const senderIdNum = Number(msg.senderId);
        const currentUserIdNum = Number(currentUserId);
        msg.isMine = senderIdNum === currentUserIdNum;

        console.log("senderIdNum:", senderIdNum, "currentUserIdNum:", currentUserIdNum, "isMine:", msg.isMine);

        // Check if we're on the details page or index page
        if (document.getElementById('chatMessages')) {
            // Details page - append to chatMessages
            appendMessage(msg);
        } else {
            // Index page - append to chat-content
            $('.chat-content').append(renderGroupMessage(msg));
            scrollToBottom();
        }
    }
});

// Nhận thông báo thành viên mới tham gia
connection.on("MemberAdded", function (userId) {
    console.log("Thành viên mới tham gia:", userId);
    // Có thể cập nhật UI nếu cần
});

// Nhận thông báo thành viên rời nhóm
connection.on("MemberRemoved", function (userId) {
    console.log("Thành viên rời nhóm:", userId);
    // Có thể cập nhật UI nếu cần
});

// Start connection after initializing currentUserId
function startSignalRConnection() {
    initializeCurrentUserId();
    connection.start().catch(err => console.error(err.toString()));
}

// ===== INDEX PAGE FUNCTIONALITY =====

// Khi click chọn một nhóm → load lịch sử chat
$(document).on('click', '.friend-item[data-type="group"]', function () {
    currentGroupId = Number($(this).data('id'));

    $('.friend-item').removeClass('active');
    $(this).addClass('active');

    // Hiện toàn bộ phần thông tin nhóm chat nếu đang bị ẩn
    $('.chat-user-info').removeClass('d-none').show();
    // Ẩn tất cả nhóm trong phần info
    $('.chat-user-info .chat-user').hide();

    // Hiện đúng nhóm được chọn
    $('.chat-user-info .chat-user[data-id="' + currentGroupId + '"]').show();

    // Ẩn placeholder nếu có
    $('.chat-content .no-chat-placeholder').hide();

    // Load thông tin sidebar cho group
    loadGroupSidebar(currentGroupId);

    // Tham gia vào nhóm SignalR
    connection.invoke("JoinGroup", currentGroupId)
        .then(() => {
            console.log("Đã tham gia nhóm:", currentGroupId);
        })
        .catch(err => console.error(err.toString()));

    // Load tin nhắn
    loadGroupMessages();
});

// Load thông tin sidebar cho group
async function loadGroupSidebar(groupId) {
    try {
        const response = await fetch(`/GroupChat/GetGroupSidebar?groupId=${groupId}`);
        if (response.ok) {
            const html = await response.text();
            const sidebarRight = document.getElementById('sidebar-right');
            if (sidebarRight) {
                sidebarRight.innerHTML = html;
            }
        } else {
            console.error('Failed to load group sidebar info');
        }
    } catch (error) {
        console.error('Error loading group sidebar:', error);
    }
}

// Load tin nhắn nhóm
function loadGroupMessages() {
    $.get('/GroupChat/GetGroupMessages', { groupId: currentGroupId }, function(html) {
        // Nếu server trả về có chứa <div class="chat-content">, chỉ lấy nội dung bên trong
        let $response = $(html);
        if ($response.hasClass('chat-content')) {
            $('.chat-content').html($response.html());
        } else {
            $('.chat-content').html(html);
        }
        scrollToBottom();
    });
}

// Gửi tin nhắn nhóm (nhấn Enter hoặc bấm icon gửi) - Index page
$(document).on('click', '.bi-send', function() {
    sendGroupMessage();
});

$('.chat-input-bar input').on('keypress', function(e) {
    if (e.which === 13) sendGroupMessage();
});

// ===== DETAILS PAGE FUNCTIONALITY =====

// Initialize details page
function initializeDetailsPage() {
    const groupIdElement = document.querySelector('[data-group-id]');
    if (groupIdElement) {
        currentGroupId = Number(groupIdElement.dataset.groupId);

        // Tham gia vào nhóm
        connection.start().then(function() {
            connection.invoke("JoinGroup", currentGroupId);
        });

        // Load tin nhắn cũ
        loadMessages();

        // Xử lý gửi tin nhắn
        $("#messageForm").submit(function(e) {
            e.preventDefault();
            sendMessageWithFiles();
        });
    }
}

// Load tin nhắn - Details page
function loadMessages() {
    const groupId = currentGroupId;
    if (!groupId) return;

    $.get(`/GroupChat/GetGroupMessages?groupId=${groupId}`, function(data) {
        $("#chatMessages").html(data);
        scrollToBottom();
    });
}

// Thêm tin nhắn mới - Details page
function appendMessage(message) {
    let mediaHtml = '';

    // Render media nếu có
    if (message.media && message.media.length > 0) {
        for (const media of message.media) {
            if (media.mediaType && media.mediaType.startsWith('image/')) {
                mediaHtml += `<img src="${media.url}" class="message-media-img" alt="Ảnh gửi" />`;
            } else if (media.mediaType && media.mediaType.startsWith('video/')) {
                mediaHtml += `<video src="${media.url}" class="message-media-video" controls></video>`;
            } else {
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

    const messageHtml = `
        <div class="message ${message.isMine ? 'right' : 'left'}">
            ${!message.isMine ? `<img src="${message.avatarUrl}" class="message-avatar" />` : ''}
            <div class="message-content">
                ${!message.isMine ? `<div class="message-sender">${message.senderName}</div>` : ''}
                <span class="message-time">${new Date(message.sentAt).toLocaleTimeString()}</span>
                ${mediaHtml}
                ${message.content && message.content !== "[Đã gửi file]" ? `<span class="message-bubble">${message.content}</span>` : ''}
            </div>
        </div>
    `;
    $("#chatMessages").append(messageHtml);
    scrollToBottom();
}

// ===== COMMON FUNCTIONALITY =====

// Gửi tin nhắn nhóm với file support - Details page
async function sendMessageWithFiles() {
    const content = $("#messageInput").val().trim();
    const files = selectedFiles.length > 0 ? selectedFiles : null;

    if (!content && !files) return;

    try {
        if (files && files.length > 0) {
            // Gửi files qua HTTP POST
            const formData = new FormData();
            formData.append('content', content);
            formData.append('groupId', currentGroupId);

            files.forEach(file => {
                formData.append('files', file);
            });

            const response = await fetch('/GroupChat/SendGroupMessage', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                $("#messageInput").val('');
                selectedFiles = [];
                clearFilePreview();
                console.log("Files sent successfully");
            } else {
                console.error('Error sending files:', response.status);
                alert('Có lỗi xảy ra khi gửi file!');
            }
        } else {
            // Gửi text qua SignalR
            await connection.invoke("SendGroupMessage", currentGroupId, content);
            $("#messageInput").val("");
        }
    } catch (error) {
        console.error('Send message error:', error);
        alert('Có lỗi xảy ra khi gửi tin nhắn!');
    }
}

// Gửi tin nhắn nhóm - Index page
function sendGroupMessage() {
    let content = $('.chat-input-bar input').val();
    if (!content.trim() || !currentGroupId) return;

    connection.invoke("SendGroupMessage", currentGroupId, content)
        .then(() => {
            console.log("Tin nhắn nhóm đã gửi thành công");
        })
        .catch(err => console.error(err.toString()));

    $('.chat-input-bar input').val('');
}

// Render tin nhắn nhóm mới - Index page
function renderGroupMessage(msg) {
    const senderIdNum = Number(msg.senderId);
    const currentUserIdNum = Number(currentUserId);
    let isMine = senderIdNum === currentUserIdNum;
    let side = isMine ? 'right' : 'left';

    let mediaHtml = '';

    // Render media nếu có
    if (msg.media && msg.media.length > 0) {
        for (const media of msg.media) {
            if (media.mediaType && media.mediaType.startsWith('image/')) {
                mediaHtml += `<img src="${media.url}" class="message-media-img" alt="Ảnh gửi" />`;
            } else if (media.mediaType && media.mediaType.startsWith('video/')) {
                mediaHtml += `<video src="${media.url}" class="message-media-video" controls></video>`;
            } else {
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

    if (isMine) {
        return `<div class="message ${side}">
            <div class="message-content">
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                ${mediaHtml}
                ${msg.content && msg.content !== "[Đã gửi file]" ? `<span class="message-bubble">${msg.content}</span>` : ''}
            </div>
        </div>`;
    } else {
        return `<div class="message ${side}">
            <img src="${msg.avatarUrl}" class="message-avatar" />
            <div class="message-content">
                <div class="message-sender">${msg.senderName}</div>
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                ${mediaHtml}
                ${msg.content && msg.content !== "[Đã gửi file]" ? `<span class="message-bubble">${msg.content}</span>` : ''}
            </div>
        </div>`;
    }
}

// Cuộn xuống tin nhắn mới nhất
function scrollToBottom() {
    const chatMessages = document.getElementById('chatMessages');
    if (chatMessages) {
        // Details page
        chatMessages.scrollTop = chatMessages.scrollHeight;
    } else {
        // Index page
        let chatContent = $('.chat-content');
        chatContent.scrollTop(chatContent[0].scrollHeight);
    }
}

// File handling functions
function clearFilePreview() {
    const previewEl = document.getElementById('file-preview');
    if (previewEl) {
        previewEl.innerHTML = '';
        previewEl.style.display = 'none';
    }
}

// Global function for HTML onclick
function openFileDialog(type) {
    console.log("openFileDialog called with type:", type);

    const fileInput = document.getElementById('groupFileInput');
    if (!fileInput) {
        console.error("File input not found!");
        alert("File input not found! Please refresh the page.");
        return;
    }

    if (type === 'image') {
        fileInput.accept = 'image/*';
        console.log("Opening file dialog for images only");
    } else {
        fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
        console.log("Opening file dialog for all file types");
    }

    fileInput.click();
}

// Make function globally available
window.openFileDialog = openFileDialog;

// Debug function to check buttons
function debugButtons() {
    console.log("=== Debugging buttons ===");

    // Check all possible selectors
    const selectors = [
        '.btn-file',
        '.bi-image',
        '.bi-paperclip',
        '.btn-file.bi-image',
        '.btn-file.bi-paperclip',
        'button[title="Chọn ảnh"]',
        'button[title="Chọn file"]'
    ];

    selectors.forEach(selector => {
        const elements = document.querySelectorAll(selector);
        console.log(`Selector "${selector}": found ${elements.length} elements`);
        elements.forEach((el, index) => {
            console.log(`  Element ${index}:`, el.className, el.outerHTML);
        });
    });

    // Check messageForm
    const messageForm = document.getElementById('messageForm');
    console.log('messageForm:', messageForm);
    if (messageForm) {
        console.log('messageForm HTML:', messageForm.outerHTML);
    }

    console.log("=== End debugging ===");
}

function setupFileInput() {
    console.log("Setting up file input...");

    // Debug buttons first
    debugButtons();

    // Add file input if not exists
    if (!document.getElementById('groupFileInput')) {
        console.log("Creating file input element...");
        const fileInput = document.createElement('input');
        fileInput.type = 'file';
        fileInput.id = 'groupFileInput';
        fileInput.multiple = true;
        fileInput.style.display = 'none';
        fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
        document.body.appendChild(fileInput);

        fileInput.addEventListener('change', function(e) {
            console.log("File input changed, files:", e.target.files.length);
            if (e.target.files.length > 0) {
                selectedFiles = Array.from(e.target.files);
                showFilePreview();
            }
        });
        console.log("File input created and added to body");
    } else {
        console.log("File input already exists");
    }

    // Add test button handler
    const testBtn = document.getElementById('testFileBtn');
    if (testBtn) {
        testBtn.addEventListener('click', function() {
            console.log("Test button clicked!");
            alert('Test button works!');
            const fileInput = document.getElementById('groupFileInput');
            if (fileInput) {
                fileInput.click();
            } else {
                alert('File input not found!');
            }
        });
        console.log("Test button handler added");
    }

    // Remove old event handlers and add new ones
    $(document).off('click', '.btn-file');

    // Try using different approaches

    // Approach 1: Event delegation
    $(document).on('click', '.btn-file', function(e) {
        e.preventDefault();
        e.stopPropagation();
        console.log("Button file clicked via delegation");

        const fileInput = document.getElementById('groupFileInput');
        if (fileInput) {
            if ($(this).hasClass('bi-image')) {
                fileInput.accept = 'image/*';
                console.log("Setting accept to images only");
            } else {
                fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
                console.log("Setting accept to all file types");
            }
            fileInput.click();
        } else {
            console.error("File input not found!");
        }
    });

    // Approach 2: Wait for DOM and try direct binding
    setTimeout(() => {
        const imageBtn = document.querySelector('.btn-file.bi-image');
        const paperclipBtn = document.querySelector('.btn-file.bi-paperclip');

        if (imageBtn) {
            console.log("Found image button after timeout, adding click handler");
            imageBtn.onclick = function(e) {
                e.preventDefault();
                console.log("Direct image button clicked (onclick)");
                const fileInput = document.getElementById('groupFileInput');
                if (fileInput) {
                    fileInput.accept = 'image/*';
                    fileInput.click();
                }
            };
        } else {
            console.warn("Image button still not found after timeout");
        }

        if (paperclipBtn) {
            console.log("Found paperclip button after timeout, adding click handler");
            paperclipBtn.onclick = function(e) {
                e.preventDefault();
                console.log("Direct paperclip button clicked (onclick)");
                const fileInput = document.getElementById('groupFileInput');
                if (fileInput) {
                    fileInput.accept = 'image/*,video/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt';
                    fileInput.click();
                }
            };
        } else {
            console.warn("Paperclip button still not found after timeout");
        }
    }, 1000);

    console.log("File input setup completed");
}

function showFilePreview() {
    let previewEl = document.getElementById('file-preview');
    if (!previewEl) {
        previewEl = document.createElement('div');
        previewEl.id = 'file-preview';
        previewEl.className = 'file-preview';
        const messageForm = document.getElementById('messageForm');
        if (messageForm) {
            messageForm.insertBefore(previewEl, messageForm.firstChild);
        }
    }

    previewEl.innerHTML = '';
    previewEl.style.display = 'flex';

    selectedFiles.forEach((file, index) => {
        const previewItem = document.createElement('div');
        previewItem.className = 'file-preview-item';

        const removeBtn = document.createElement('button');
        removeBtn.innerHTML = '×';
        removeBtn.className = 'file-remove-btn';
        removeBtn.onclick = () => removeFile(index);

        const fileName = document.createElement('div');
        fileName.className = 'file-name';
        fileName.textContent = file.name;

        if (file.type.startsWith('image/')) {
            const img = document.createElement('img');
            img.src = URL.createObjectURL(file);
            img.style.cssText = `width: 60px; height: 60px; object-fit: cover; border-radius: 8px;`;
            previewItem.appendChild(img);
        } else if (file.type.startsWith('video/')) {
            const video = document.createElement('video');
            video.src = URL.createObjectURL(file);
            video.style.cssText = `width: 60px; height: 60px; object-fit: cover; border-radius: 8px;`;
            previewItem.appendChild(video);
        } else {
            const fileIcon = document.createElement('div');
            fileIcon.innerHTML = '<i class="bi bi-file-earmark-text"></i>';
            fileIcon.style.cssText = `width: 60px; height: 60px; display: flex; align-items: center; justify-content: center; background: rgba(255,255,255,0.1); border-radius: 8px; font-size: 24px;`;
            previewItem.appendChild(fileIcon);
        }

        previewItem.appendChild(removeBtn);
        previewItem.appendChild(fileName);
        previewEl.appendChild(previewItem);
    });
}

function removeFile(index) {
    selectedFiles.splice(index, 1);
    if (selectedFiles.length === 0) {
        clearFilePreview();
    } else {
        showFilePreview();
    }
}

// Khi rời khỏi trang, rời khỏi nhóm SignalR
$(window).on('beforeunload', function() {
    if (currentGroupId) {
        connection.invoke("LeaveGroup", currentGroupId)
            .catch(err => console.error(err.toString()));
    }
});

// Video call functionality
$(document).on('click', '.btn-video-call', function() {
    let peerId = $(this).data('peer');
    window.location.href = '/VideoCall/Room?userId=' + peerId;
});

// Sidebar toggle functionality
$(document).ready(function() {
    // Start SignalR connection
    startSignalRConnection();

    // Check if we're on details page
    if (document.getElementById('chatMessages')) {
        initializeDetailsPage();
        setupFileInput();
    }

    // Sidebar toggle
    const toggleBtn = document.querySelector(".toggle-sidebar-btn");
    const sidebar = document.getElementById("sidebar-right");

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("d-none");
            sidebar.classList.toggle("show");
        });
    }

    // Initialize drag and drop
    initializeDragAndDrop();
});

// ===== DRAG & DROP FUNCTIONALITY =====

function initializeDragAndDrop() {
    const dragDropOverlay = document.getElementById('drag-drop-overlay');

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
            }
        }
    });

    // Handle drop
    document.addEventListener('drop', function(e) {
        if (isDragging) {
            dragCounter = 0;
            isDragging = false;
            dragDropOverlay.classList.remove('active');

            const files = e.dataTransfer.files;
            if (files.length > 0 && document.getElementById('chatMessages')) {
                // Only handle file drop in Details page
                selectedFiles = Array.from(files);
                showFilePreview();
            }
        }
    });

    // Handle window focus to reset state
    window.addEventListener('focus', function() {
        if (isDragging) {
            dragCounter = 0;
            isDragging = false;
            dragDropOverlay.classList.remove('active');
        }
    });
} 