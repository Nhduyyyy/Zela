// Global variables
let currentGroupId = null;
let currentUserId = null;
let selectedFiles = [];
let groupStickerPanelVisible = false;
let availableStickers = [];

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

    // Initialize currentGroupId if not set
    if (!currentGroupId) {
        const groupIdElement = document.querySelector('[data-group-id]');
        if (groupIdElement) {
            currentGroupId = groupIdElement.dataset.groupId;
            console.log("currentGroupId set from data attribute:", currentGroupId);
        } else if (typeof window.currentGroupId !== 'undefined') {
            currentGroupId = window.currentGroupId;
            console.log("currentGroupId set from window variable:", currentGroupId);
        }
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
            bindFileSummaryButtons(); // Bind file summary buttons for new message
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

// Nhận sticker mới từ nhóm
connection.on("ReceiveGroupSticker", function (msg) {
    let currentId = Number(currentGroupId);
    if (currentId && msg.groupId === currentId) {
        msg.isMine = Number(msg.senderId) === Number(currentUserId);
        $('.chat-content').append(renderGroupMessage(msg));
        scrollToBottom();
        bindFileSummaryButtons(); // Bind file summary buttons for new sticker
    }
});

// Start connection after initializing currentUserId
function startSignalRConnection() {
    initializeCurrentUserId();

    // Only start if not already connected or connecting
    if (connection.state === signalR.HubConnectionState.Disconnected) {
        connection.start()
            .then(() => {
                console.log('GroupChat SignalR connected successfully');
            })
            .catch(err => console.error('GroupChat SignalR start error:', err.toString()));
    }
}

// Initialize when document is ready
$(document).ready(function() {
    startSignalRConnection();

    // Đăng ký event listener cho nút đóng preview/summary
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('close-preview-btn') || e.target.classList.contains('close-summary-btn')) {
            console.log('Close button clicked:', e.target.className);
            e.preventDefault();
            e.stopPropagation();

            // Tìm container chứa nội dung cần đóng
            const previewBox = e.target.closest('.file-preview-box');
            const summaryBox = e.target.closest('.file-summary-box');

            if (previewBox) {
                console.log('Closing preview box');
                previewBox.style.display = 'none';
            }

            if (summaryBox) {
                console.log('Closing summary box');
                summaryBox.style.display = 'none';
            }

            // Fallback: tìm theo file-summary-block
            if (!previewBox && !summaryBox) {
                const block = e.target.closest('.file-summary-block');
                if (block) {
                    const previewDiv = block.querySelector('.file-preview-content');
                    const summaryDiv = block.querySelector('.file-summary-content');
                    if (previewDiv) previewDiv.style.display = 'none';
                    if (summaryDiv) summaryDiv.style.display = 'none';
                }
            }
        }
    });
});

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

    // Load sidebar media
    try {
        const response = await fetch(`/GroupChat/GetGroupSidebarMedia?groupId=${groupId}`);
        if (response.ok) {
            const html = await response.text();
            const sidebarMediaContainer = document.getElementById('sidebar-media-container');
            if (sidebarMediaContainer) {
                sidebarMediaContainer.innerHTML = html;
            }
        } else {
            console.error('Failed to load group sidebar media');
        }
    } catch (error) {
        console.error('Error loading group sidebar media:', error);
    }
}

// Load tin nhắn nhóm
function loadGroupMessages() {
    console.log('loadGroupMessages called with currentGroupId:', currentGroupId);

    if (!currentGroupId) {
        console.error('currentGroupId is not set!');
        return;
    }

    $.ajax({
        url: '/GroupChat/GetGroupMessages',
        method: 'GET',
        data: { groupId: currentGroupId },
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        },
        success: function(messages) {
            console.log('Received messages:', messages);
            let chatContent = $('.chat-content');
            chatContent.html(''); // Clear existing messages

            if (messages && messages.length > 0) {
                messages.forEach(function(msg) {
                    console.log('Rendering message:', msg);
                    // The currentUserId should be available globally in this script.
                    const messageHtml = renderGroupMessage(msg);
                    chatContent.append(messageHtml);
                });
            } else {
                // Optional: show a message if there are no messages
                chatContent.html('<div class="no-messages">Chưa có tin nhắn nào trong nhóm này.</div>');
            }

            scrollToBottom();
            bindFileSummaryButtons(); // Bind file summary buttons after loading messages

            // Initialize reaction functionality for new messages
            if (window.messageReactions && window.messageReactions.updateAllMessageReactions) {
                window.messageReactions.updateAllMessageReactions();
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading group messages:', error);
            console.error('Response:', xhr.responseText);
            $('.chat-content').html('<div class="error-message">Có lỗi xảy ra khi tải tin nhắn.</div>');
        }
    });
}

// Gửi tin nhắn nhóm (nhấn Enter hoặc bấm icon gửi) - Index page
// NOTE: This binding is now handled in Index.cshtml to avoid conflicts
// $(document).on('click', '.bi-send', function() {
//     sendGroupMessage();
// });

// $('.chat-input-bar input').on('keypress', function(e) {
//     if (e.which === 13) sendGroupMessage();
// });

// ===== DETAILS PAGE FUNCTIONALITY =====

// Initialize details page
function initializeDetailsPage() {
    const groupIdElement = document.querySelector('[data-group-id]');
    if (groupIdElement) {
        currentGroupId = Number(groupIdElement.dataset.groupId);

        // Tham gia vào nhóm
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            connection.start().then(function() {
                connection.invoke("JoinGroup", currentGroupId);
            }).catch(err => console.error('GroupChat details page SignalR start error:', err));
        } else if (connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("JoinGroup", currentGroupId);
        }

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

    $.get(`/GroupChat/GetGroupMessages?groupId=${groupId}`, function(messages) {
        $("#chatMessages").html(''); // Clear existing messages
        if (messages && messages.length > 0) {
            messages.forEach(function(message) {
                appendMessage(message); // appendMessage will add it to #chatMessages
            });
        }
        scrollToBottom();
        bindFileSummaryButtons(); // Bind file summary buttons after loading messages
    });
}

// Thêm tin nhắn mới - Details page
function appendMessage(message) {
    let chatMessages = $('#chatMessages');
    let messageHtml = renderGroupMessage(message);
    chatMessages.append(messageHtml);
    scrollToBottom();
    bindFileSummaryButtons(); // Bind file summary buttons after appending new message
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

    // Lấy replyToMessageId từ biến toàn cục nếu có
    let replyId = window.replyToMessageId || null;
    connection.invoke("SendGroupMessage", currentGroupId, content, replyId)
        .then(() => {
            window.replyToMessageId = null;
            window.replyToMessageContent = null;
            $('#reply-preview-bar').hide();
            $('.chat-input-bar input').val('');
        })
        .catch(err => console.error(err.toString()));
}

// Render tin nhắn nhóm mới - Index page
function renderGroupMessage(msg) {
    const senderIdNum = Number(msg.senderId);
    const currentUserIdNum = Number(currentUserId);
    const isMine = senderIdNum === currentUserIdNum;

    let sentTime = '';
    if (msg.sentAt) {
        const date = new Date(msg.sentAt);
        sentTime = date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
    }

    let mediaHtml = '';
    if (msg.media && msg.media.length > 0) {
        for (const media of msg.media) {
            if (media.mediaType && media.mediaType.startsWith('image/')) {
                mediaHtml += `<img src="${media.url}" class="message-media-img" alt="Ảnh gửi"/>`;
            } else if (media.mediaType && media.mediaType.startsWith('video/')) {
                mediaHtml += `<video src="${media.url}" class="message-media-video" controls></video>`;
            } else {
                const fileName = media.fileName || media.url.split('/').pop();
                const isTextDoc = fileName && (fileName.endsWith('.txt') || fileName.endsWith('.doc') || fileName.endsWith('.docx'));

                if (isTextDoc) {
                    mediaHtml += `
                        <div class="file-block file-summary-block" data-media-url="${media.url}" data-filename="${fileName}">
                            <div class="chat-file-attachment-group">
                                <div class="chat-file-attachment">
                                    <span class="file-icon"><i class="bi bi-file-earmark-text"></i></span>
                                    <span class="file-name">${fileName}</span>
                                    <a href="${media.url}" download="${fileName}" class="file-download-btn" title="Tải về"><i class="bi bi-download"></i></a>
                                </div>
                                <div class="chat-file-attachment">
                                    <button type="button" class="btn btn-primary btn-preview-file" data-media-url="${media.url}" data-filename="${fileName}">
                                        Xem trước
                                    </button>
                                    <button type="button" class="btn btn-primary btn-summarize-file" data-media-url="${media.url}" data-filename="${fileName}">
                                        <span class="btn-summarize-text">Tóm tắt file</span>
                                        <span class="btn-summarize-loading" style="display:none;">
                                            <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang tóm tắt...
                                        </span>
                                    </button>
                                </div>
                            </div>
                            <div class="file-preview-content" style="display:none;"></div>
                            <div class="file-summary-content" style="display:none;"></div>
                        </div>`;
                } else {
                    mediaHtml += `
                        <div class="chat-file-attachment">
                            <span class="file-icon"><i class="bi bi-file-earmark"></i></span>
                            <span class="file-name">${fileName}</span>
                            <a href="${media.url}" download="${fileName}" class="file-download-btn" title="Tải về"><i class="bi bi-download"></i></a>
                        </div>`;
                }
            }
        }
    }

    let contentHtml = '';
    if (msg.stickerUrl && msg.stickerUrl.length > 0) {
        contentHtml = `<img src="${msg.stickerUrl}" class="sticker-message" alt="Sticker" draggable="false" style="max-width:120px;max-height:120px;"/>`;
    } else if (msg.content && msg.content !== "[Đã gửi file]") {
        let replyHtml = '';
        if (msg.replyToMessageId && msg.replyToMessageContent) {
            const replySenderText = isMine
                ? `Bạn đã trả lời '${msg.replyToMessageSenderName || "ai đó"}'`
                : `Đã trả lời '${msg.replyToMessageSenderName || "ai đó"}'`;

            replyHtml = `
                <div class="reply-preview-in-bubble" style="align-self: stretch; background: #f0f0f0; border-left: 3px solid #007bff; padding: 4px 8px; margin-bottom: 2px; border-radius: 6px 6px 0 0;">
                    <div style="font-size: 12px; color: #007bff; font-weight: 500; margin-bottom: 2px;">
                    ${replySenderText}
                    </div>
                    <span class="reply-label">Reply:</span>
                    <span class="reply-content">${msg.replyToMessageContent}</span>
                </div>`;
        }

        const escapedContent = (msg.content || '').replace(/'/g, "&#39;");
        let bubbleContentHtml;
        if (isMine) {
            bubbleContentHtml = `
                <div style="display: flex; align-items: center; gap: 6px; width: 100%;justify-content: flex-end;">
                    <button class="btn-reply" onclick="replyToMessage(${msg.messageId}, '${escapedContent}')" title="Reply" style="background: none; border: none; cursor: pointer; color: #007bff; visibility: hidden;">↩️</button>
                    <div class="message-reaction-btn" onclick="showReactionMenu(${msg.messageId})">😀</div>
                    <span class="message-bubble">${msg.content}</span>
                </div>`;
        } else {
            bubbleContentHtml = `
                <div style="display: flex; align-items: center; gap: 6px; width: 100%;justify-content: flex-start;">
                    <span class="message-bubble">${msg.content}</span>
                    <div class="message-reaction-btn" onclick="showReactionMenu(${msg.messageId})">😀</div>
                    <button class="btn-reply" onclick="replyToMessage(${msg.messageId}, '${escapedContent}')" title="Reply" style="background: none; border: none; cursor: pointer; color: #007bff; visibility: hidden;">↩️</button>
                </div>`;
        }

        const bubbleRowAlign = isMine ? 'align-items: flex-end;' : 'align-items: flex-start;';
        contentHtml = `
            <div class="bubble-row" style="display: flex; align-items: center; gap: 6px; flex-direction: column; ${bubbleRowAlign}">
                ${replyHtml}
                ${bubbleContentHtml}
            </div>`;
    }

    let reactionsHtml = '';
    if (msg.reactions && msg.reactions.length > 0) {
        reactionsHtml += `<div class="message-reactions">`;
        for (const reaction of msg.reactions) {
            const userClass = reaction.hasUserReaction ? "user-reaction" : "";
            const emoji = getReactionEmoji(reaction.reactionType);
            const userNames = (reaction.userNames || []).join(', ');
            reactionsHtml += `
                <span class="reaction-badge ${userClass}" data-reaction-type="${reaction.reactionType}" title="${userNames}">
                    ${emoji} ${reaction.count}
                </span>`;
        }
        reactionsHtml += `</div>`;
    }

    if (isMine) {
        return `
            <div class="message right" data-message-id="${msg.messageId}">
                <div class="message-content">
                    <span class="message-time">${sentTime}</span>
                    ${mediaHtml}
                    ${contentHtml}
                    ${reactionsHtml}
                </div>
            </div>`;
    } else {
        return `
            <div class="message left" data-message-id="${msg.messageId}">
                <img src="${msg.avatarUrl}" class="message-avatar" />
                <div class="message-content">
                    <div class="message-sender">${msg.senderName}</div>
                    <span class="message-time">${sentTime}</span>
                    ${mediaHtml}
                    ${contentHtml}
                    ${reactionsHtml}
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
    const sidebarMediaContainer = document.getElementById("sidebar-media-container");

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener("click", function () {
            // Nếu sidebar media đang hiển thị, ẩn nó trước
            if (sidebarMediaContainer && !sidebarMediaContainer.classList.contains('d-none')) {
                hideSidebarMedia();
                return;
            }

            // Toggle sidebar right
            sidebar.classList.toggle("d-none");
            sidebar.classList.toggle("show");
        });
    }

    // Initialize drag and drop
    initializeDragAndDrop();

    $(document).on('mouseenter', '.message-content', function () {
        $(this).find('.btn-reply').css('visibility', 'visible');
    }).on('mouseleave', '.message-content', function () {
        $(this).find('.btn-reply').css('visibility', 'hidden');
    });
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

// ===== STICKER PANEL FOR GROUP CHAT =====

// Load danh sách sticker từ server
async function loadStickers() {
    try {
        const response = await fetch('/Chat/GetStickers');
        const stickers = await response.json();
        availableStickers = stickers;
        renderStickerPanel(stickers);

    } catch (error) {
        console.error('Failed to load stickers:', error);
    }
}

// Render sticker panel
function renderStickerPanel(stickers) {
    let panel = document.querySelector('.sticker-panel');
    if (!panel) {
        const inputBar = document.querySelector('.chat-input-bar .input-actions');
        if (!inputBar) return;
        inputBar.insertAdjacentHTML('beforeend', `
            <div class="sticker-panel">
                <div class="sticker-header" style="display:flex; justify-content:space-between; align-items:center; padding:8px 12px; border-bottom:1px solid #eee;">
                    <h6 style="margin:0; font-size:16px;">Stickers</h6>
                    <button class="sticker-close" style="background:none; border:none; font-size:20px; cursor:pointer;">&times;</button>
                </div>
                <div class="sticker-grid" style="flex-wrap:wrap; gap:8px; padding:12px;"></div>
            </div>
        `);
        panel = document.querySelector('.sticker-panel');
    }
    const grid = panel.querySelector('.sticker-grid');
    grid.innerHTML = '';
    stickers.forEach(sticker => {
        const img = document.createElement('img');
        img.src = sticker.stickerUrl;
        img.className = 'sticker-img';
        img.dataset.url = sticker.stickerUrl;
        img.alt = sticker.stickerName;
        img.style.width = '64px';
        img.style.height = '64px';
        img.style.cursor = 'pointer';
        img.onerror = function() { this.style.display = 'none'; };
        grid.appendChild(img);
    });
}

// Khởi tạo sticker panel khi DOM ready
function initializeStickerPanel() {
    loadStickers();
}
document.addEventListener('DOMContentLoaded', initializeStickerPanel);

// Toggle sticker panel
$(document).on('click', '.chat-input-bar .bi-emoji-smile', function () {
    groupStickerPanelVisible = !groupStickerPanelVisible;
    const stickerPanel = document.querySelector('.sticker-panel');
    if (stickerPanel) {
        stickerPanel.style.display = groupStickerPanelVisible ? 'block' : 'none';
    }
});
// Đóng sticker panel
$(document).on('click', '.sticker-close', function () {
    groupStickerPanelVisible = false;
    const stickerPanel = document.querySelector('.sticker-panel');
    if (stickerPanel) {
        stickerPanel.style.display = 'none';
    }
});
// Chọn và gửi sticker
$(document).on('click', '.sticker-img', function () {
    const stickerUrl = this.dataset.url;
    if (typeof connection !== 'undefined' && connection.invoke) {
        connection.invoke("SendGroupSticker", currentGroupId, stickerUrl)
            .then(() => {
                groupStickerPanelVisible = false;
                const stickerPanel = document.querySelector('.sticker-panel');
                if (stickerPanel) stickerPanel.style.display = 'none';
            })
            .catch(err => alert('Lỗi gửi sticker!'));
    }
});
// Ẩn panel khi click ra ngoài
$(document).on('mousedown', function (e) {
    const stickerPanel = document.querySelector('.sticker-panel');
    if (stickerPanel && !stickerPanel.contains(e.target) && !e.target.closest('.bi-emoji-smile')) {
        stickerPanel.style.display = 'none';
        groupStickerPanelVisible = false;
    }
});

// Hàm ẩn sidebar media
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

// Toggle member list visibility in group chat sidebar
$(document).on('click', '#toggle-member-list', function (event) {
    event.stopPropagation();
    console.log('Toggle member list clicked!');
    var $memberList = $('#member-list-box');
    var $btn = $(this);
    var expanded = $btn.attr('aria-expanded') === 'true';
    if (expanded) {
        $memberList.addClass('collapsed');
        $btn.attr('aria-expanded', 'false');
        $btn.find('span').removeClass('bi-eye').addClass('bi-eye-slash');
        $btn.contents().last()[0].textContent = '';
    } else {
        $memberList.removeClass('collapsed');
        $btn.attr('aria-expanded', 'true');
        $btn.find('span').removeClass('bi-eye-slash').addClass('bi-eye');
        $btn.contents().last()[0].textContent = '';
    }
});

// ===== FILE SUMMARY FUNCTIONALITY FOR GROUP CHAT =====

// Xử lý preview và tóm tắt file văn bản
function bindFileSummaryButtons() {
    // Event delegation cho preview/tóm tắt file
    document.addEventListener('click', function(e) {
        // Preview
        if (e.target.classList.contains('btn-preview-file')) {
            e.preventDefault();
            e.stopPropagation();
            const btn = e.target;
            const block = btn.closest('.file-summary-block');
            const url = btn.getAttribute('data-media-url');
            const filename = btn.getAttribute('data-filename');
            const previewDiv = block.querySelector('.file-preview-content');
            const summaryDiv = block.querySelector('.file-summary-content');
            if (summaryDiv) summaryDiv.style.display = 'none';
            if (getComputedStyle(previewDiv).display === 'block') {
                previewDiv.style.display = 'none';
            } else {
                previewDiv.innerHTML = '<em>Đang tải xem trước...</em>';
                previewDiv.style.display = 'block';
                fetch(`/File/Preview?url=${encodeURIComponent(url)}&filename=${encodeURIComponent(filename)}`)
                    .then(res => res.text())
                    .then(html => {
                        previewDiv.innerHTML = html;
                        previewDiv.style.display = 'block';
                    })
                    .catch(() => {
                        previewDiv.innerHTML = '<div class="file-preview-box"><div class="text-danger">Không thể xem trước.</div></div>';
                        previewDiv.style.display = 'block';
                    });
            }
        }
        // Summarize
        if (e.target.classList.contains('btn-summarize-file')) {
            e.preventDefault();
            e.stopPropagation();
            const btn = e.target;
            const block = btn.closest('.file-summary-block');
            const url = btn.getAttribute('data-media-url');
            const filename = btn.getAttribute('data-filename');
            const summaryDiv = block.querySelector('.file-summary-content');
            const previewDiv = block.querySelector('.file-preview-content');
            const btnText = btn.querySelector('.btn-summarize-text');
            const btnLoading = btn.querySelector('.btn-summarize-loading');
            if (previewDiv) previewDiv.style.display = 'none';
            if (getComputedStyle(summaryDiv).display === 'block') {
                summaryDiv.style.display = 'none';
            } else {
                btn.disabled = true;
                btnText.style.display = 'none';
                btnLoading.style.display = 'inline-block';
                summaryDiv.innerHTML = '';
                summaryDiv.style.display = 'block';
                const formData = new FormData();
                formData.append('url', url);
                formData.append('filename', filename);
                fetch('/File/Summarize', {
                    method: 'POST',
                    body: formData
                })
                    .then(res => res.text())
                    .then(html => {
                        summaryDiv.innerHTML = html;
                        summaryDiv.style.display = 'block';
                    })
                    .catch(() => {
                        summaryDiv.innerHTML = '<span class="text-danger">Có lỗi xảy ra khi tóm tắt file.</span>';
                        summaryDiv.style.display = 'block';
                    })
                    .finally(() => {
                        btn.disabled = false;
                        btnText.style.display = 'inline';
                        btnLoading.style.display = 'none';
                    });
            }
        }
    });
}

 