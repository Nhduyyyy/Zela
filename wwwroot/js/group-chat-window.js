// Global variables
let currentGroupId = null;
let currentUserId = null;

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
            const message = $("#messageInput").val();
            if (message) {
                connection.invoke("SendGroupMessage", currentGroupId, message);
                $("#messageInput").val("");
            }
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
    const messageHtml = `
        <div class="message ${message.isMine ? 'right' : 'left'}">
            ${!message.isMine ? `<img src="${message.avatarUrl}" class="message-avatar" />` : ''}
            <div class="message-content">
                ${!message.isMine ? `<div class="message-sender">${message.senderName}</div>` : ''}
                <span class="message-bubble">${message.content}</span>
                <span class="message-time">${new Date(message.sentAt).toLocaleTimeString()}</span>
            </div>
        </div>
    `;
    $("#chatMessages").append(messageHtml);
    scrollToBottom();
}

// ===== COMMON FUNCTIONALITY =====

// Gửi tin nhắn nhóm
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
    // Ensure consistent comparison - convert both to numbers
    const senderIdNum = Number(msg.senderId);
    const currentUserIdNum = Number(currentUserId);
    let isMine = senderIdNum === currentUserIdNum;
    let side = isMine ? 'right' : 'left';

    console.log("renderGroupMessage - senderIdNum:", senderIdNum, "currentUserIdNum:", currentUserIdNum, "isMine:", isMine);

    if (isMine) {
        return `<div class="message ${side}">
            <div class="message-content">
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                <span class="message-bubble">${msg.content}</span>
            </div>
        </div>`;
    } else {
        return `<div class="message ${side}">
            <img src="${msg.avatarUrl}" class="message-avatar" />
            <div class="message-content">
                <div class="message-sender">${msg.senderName}</div>
                <span class="message-time">${msg.sentAt.substring(11, 16)}</span>
                <span class="message-bubble">${msg.content}</span>
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
}); 