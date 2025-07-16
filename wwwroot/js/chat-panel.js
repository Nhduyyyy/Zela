/**
 * Meeting Chat Panel JavaScript
 * Xử lý chat real-time trong video call room sử dụng SignalR
 */

class MeetingChatPanel {
    constructor() {
        this.meetingCode = null;
        this.sessionId = null;
        this.currentUserId = null;
        this.connection = null;
        this.isInitialized = false;
        this.typingTimeout = null;
        this.eventsBound = false; // Flag to prevent duplicate event binding
        this.manualDisconnect = false; // Flag to track manual disconnect
        
        this.init();
    }

    init() {
        // Lấy thông tin từ DOM hoặc từ video call system
        const chatPanel = document.querySelector('.meeting-chat-panel');
        if (chatPanel) {
            this.meetingCode = chatPanel.dataset.meetingCode;
            this.sessionId = chatPanel.dataset.sessionId || this.generateSessionId();
            this.currentUserId = parseInt(chatPanel.dataset.currentUserId) || 0;
        } else {
            // Lấy từ video call system nếu có
            if (window.connection && window.connection.connectionId) {
                // Lấy từ SignalR connection của video call
                this.meetingCode = window.currentRoomId || window.meetingCode;
                this.sessionId = window.currentSessionId || this.generateSessionId();
                this.currentUserId = window.currentUserId || 0;
            } else {
                // Fallback: lấy từ video grid
                const videoGrid = document.getElementById('video-grid');
                if (videoGrid) {
                    this.meetingCode = videoGrid.dataset.meetingCode;
                    this.sessionId = videoGrid.dataset.sessionId || this.generateSessionId();
                    this.currentUserId = parseInt(videoGrid.dataset.userId) || window.currentUserId || 0;
                } else {
                    // Final fallback
                    this.meetingCode = null;
                    this.sessionId = this.generateSessionId();
                    this.currentUserId = window.currentUserId || 0;
                }
            }
        }

        console.log('Chat Panel Info:', {
            meetingCode: this.meetingCode,
            sessionId: this.sessionId,
            currentUserId: this.currentUserId
        });

        // Nếu chưa có meeting code, đợi video call system khởi tạo
        if (!this.meetingCode) {
            console.log('⏳ Waiting for video call system to initialize...');
            this.waitForVideoCallSystem();
            return;
        }

        // Khởi tạo SignalR connection
        this.initializeSignalR();
        
        // Bind events (only once)
        if (!this.eventsBound) {
            this.bindEvents();
            this.eventsBound = true;
        }
        
        this.isInitialized = true;
    }

    waitForVideoCallSystem() {
        const checkInterval = setInterval(() => {
            // Kiểm tra xem video call system đã khởi tạo chưa
            if (window.currentRoomId || window.meetingCode) {
                clearInterval(checkInterval);
                
                // Cập nhật thông tin
                this.meetingCode = window.currentRoomId || window.meetingCode;
                this.sessionId = window.currentSessionId || this.generateSessionId();
                this.currentUserId = window.currentUserId || 0;
                
                console.log('✅ Video call system initialized, updating chat panel:', {
                    meetingCode: this.meetingCode,
                    sessionId: this.sessionId,
                    currentUserId: this.currentUserId
                });

                // Khởi tạo SignalR connection
                this.initializeSignalR();
                
                // Bind events (only once)
                if (!this.eventsBound) {
                    this.bindEvents();
                    this.eventsBound = true;
                }
                
                this.isInitialized = true;
            }
        }, 100); // Kiểm tra mỗi 100ms

        // Timeout sau 10 giây
        setTimeout(() => {
            clearInterval(checkInterval);
            if (!this.meetingCode) {
                console.error('❌ Timeout waiting for video call system');
                this.showError('Không thể kết nối với hệ thống video call');
            }
        }, 10000);
    }

    async initializeSignalR() {
        try {
            // Kiểm tra meeting code
            if (!this.meetingCode) {
                console.error('❌ No meeting code available for chat');
                this.showError('Không tìm thấy mã phòng họp');
                return;
            }

            // Kiểm tra xem đã có connection chưa
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                console.log('✅ SignalR connection already exists and connected');
                return;
            }

            // Kiểm tra xem có connection đang connecting không
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connecting) {
                console.log('⏳ SignalR connection is already connecting, waiting...');
                return;
            }

            // Nếu có connection cũ, dừng nó trước
            if (this.connection) {
                console.log('🛑 Stopping existing connection...');
                try {
                    await this.connection.stop();
                } catch (stopError) {
                    console.warn('Warning stopping connection:', stopError);
                }
            }

            console.log('🔄 Creating new SignalR connection...');
            
            // Tạo SignalR connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/meetingChatHub')
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .build();

            // Đăng ký các event handlers
            this.registerSignalREvents();

            // Bắt đầu connection
            console.log('🔄 Starting SignalR connection...');
            await this.connection.start();
            console.log('✅ Meeting Chat SignalR connected');

            // Join vào room
            console.log(`🔄 Joining room ${this.meetingCode}...`);
            await this.connection.invoke('JoinRoom', this.meetingCode, this.sessionId);
            console.log(`✅ Joined room ${this.meetingCode}`);

            // Load existing messages from server
            await this.loadExistingMessages();

        } catch (error) {
            console.error('❌ Failed to connect to Meeting Chat SignalR:', error);
            this.showError('Không thể kết nối chat real-time');
            
            // Reset connection để có thể thử lại
            this.connection = null;
        }
    }

    registerSignalREvents() {
        // Nhận tin nhắn mới
        this.connection.on('ReceiveRoomMessage', (message) => {
            console.log('📨 Received room message:', message);
            this.addMessage(message);
            this.scrollToBottom();
        });

        // Nhận tin nhắn riêng tư
        this.connection.on('ReceivePrivateMessage', (message) => {
            console.log('🔒 Received private message:', message);
            this.addMessage(message);
            this.scrollToBottom();
            this.showNotification('Tin nhắn riêng tư từ ' + message.senderName);
        });

        // Tin nhắn đã được gửi thành công
        this.connection.on('MessageSent', (result) => {
            console.log('✅ Message sent successfully:', result);
            this.setLoadingState(false);
        });

        // Tin nhắn được chỉnh sửa
        this.connection.on('MessageEdited', (message) => {
            console.log('✏️ Message edited:', message);
            this.updateMessage(message);
        });

        // Tin nhắn bị xóa
        this.connection.on('MessageDeleted', (data) => {
            console.log('🗑️ Message deleted:', data);
            this.removeMessage(data.messageId);
        });

        // User typing indicator
        this.connection.on('UserTyping', (data) => {
            console.log('⌨️ User typing:', data);
            this.showTypingIndicator(data.userId, data.isTyping);
        });

        // User joined/left
        this.connection.on('UserJoined', (data) => {
            console.log('👋 User joined:', data);
            this.showSystemMessage(`${data.userId} đã tham gia phòng`);
        });

        this.connection.on('UserLeft', (data) => {
            console.log('👋 User left:', data);
            this.showSystemMessage(`${data.userId} đã rời phòng`);
        });

        // Room participants updated
        this.connection.on('RoomParticipants', (participants) => {
            console.log('👥 Room participants:', participants);
            this.updateParticipantsList(participants);
        });

        // Error handling
        this.connection.on('Error', (error) => {
            console.error('❌ SignalR Error:', error);
            this.showError(error);
        });

        // Connection events
        this.connection.onreconnecting(() => {
            console.log('🔄 Reconnecting to chat...');
            this.showConnectionStatus('Đang kết nối lại...', 'connecting');
        });

        this.connection.onreconnected(() => {
            console.log('✅ Reconnected to chat');
            this.showConnectionStatus('Đã kết nối', 'connected');
            // Rejoin room after reconnection
            this.connection.invoke('JoinRoom', this.meetingCode, this.sessionId);
        });

        this.connection.onclose((error) => {
            console.log('❌ Chat connection closed');
            if (error) {
                console.error('Connection close error:', error);
            }
            this.showConnectionStatus('Mất kết nối', 'disconnected');
            
            // Chỉ tự động reconnect nếu không phải do user disconnect
            if (this.isInitialized && !this.manualDisconnect) {
                console.log('🔄 Auto-reconnecting...');
                setTimeout(() => {
                    this.initializeSignalR();
                }, 2000);
            }
        });
    }

    bindEvents() {
        // Send message button
        const sendBtn = document.getElementById('sendMessage');
        if (sendBtn) {
            sendBtn.addEventListener('click', () => this.sendMessage());
        }

        // Enter key in message input
        const messageInput = document.getElementById('messageInput');
        if (messageInput) {
            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendMessage();
                }
            });

            // Typing indicator
            messageInput.addEventListener('input', () => {
                this.handleTyping();
            });
        }

        // Private message checkbox
        const privateCheckbox = document.getElementById('privateMessage');
        if (privateCheckbox) {
            privateCheckbox.addEventListener('change', () => {
                this.togglePrivateMessage();
            });
        }

        // Message actions (edit, delete)
        this.bindMessageActions();
    }

    bindMessageActions() {
        // Delegate event listeners for message actions
        const messagesContainer = document.getElementById('messagesContainer');
        if (messagesContainer) {
            messagesContainer.addEventListener('click', (e) => {
                const target = e.target;
                
                // Edit message
                if (target.classList.contains('edit-message')) {
                    e.preventDefault();
                    const messageId = target.closest('.message').dataset.messageId;
                    this.editMessage(messageId);
                }
                
                // Delete message
                if (target.classList.contains('delete-message')) {
                    e.preventDefault();
                    const messageId = target.closest('.message').dataset.messageId;
                    this.deleteMessage(messageId);
                }
            });
        }
    }

    async sendMessage() {
        const messageInput = document.getElementById('messageInput');
        const privateCheckbox = document.getElementById('privateMessage');
        const recipientSelect = document.getElementById('recipientSelect');
        
        if (!messageInput || !messageInput.value.trim()) return;

        const messageData = {
            content: messageInput.value.trim(),
            messageType: 0, // Text
            isPrivate: privateCheckbox ? privateCheckbox.checked : false,
            recipientId: privateCheckbox && privateCheckbox.checked && recipientSelect.value ? 
                        parseInt(recipientSelect.value) : null
        };

        try {
            this.setLoadingState(true);
            
            // Gửi qua SignalR
            await this.connection.invoke('SendRoomMessage', 
                this.meetingCode, 
                this.sessionId, 
                messageData.content, 
                messageData.messageType, 
                messageData.isPrivate, 
                messageData.recipientId
            );

            // Clear input
            messageInput.value = '';
            if (privateCheckbox) privateCheckbox.checked = false;
            if (recipientSelect) {
                recipientSelect.style.display = 'none';
                recipientSelect.value = '';
            }
            
            // Stop typing indicator
            this.stopTyping();
            
        } catch (error) {
            console.error('Error sending message:', error);
            this.showError('Không thể gửi tin nhắn');
            this.setLoadingState(false);
        }
    }

    async editMessage(messageId) {
        const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
        if (!messageElement) return;

        const currentContent = messageElement.querySelector('.message-text').textContent;
        const newContent = prompt('Chỉnh sửa tin nhắn:', currentContent);
        
        if (newContent && newContent.trim() && newContent !== currentContent) {
            try {
                await this.connection.invoke('EditMessage', messageId, newContent.trim());
            } catch (error) {
                console.error('Error editing message:', error);
                this.showError('Không thể chỉnh sửa tin nhắn');
            }
        }
    }

    async deleteMessage(messageId) {
        if (!confirm('Bạn có chắc muốn xóa tin nhắn này?')) return;

        try {
            await this.connection.invoke('DeleteMessage', messageId);
        } catch (error) {
            console.error('Error deleting message:', error);
            this.showError('Không thể xóa tin nhắn');
        }
    }

    handleTyping() {
        if (this.typingTimeout) {
            clearTimeout(this.typingTimeout);
        }

        // Kiểm tra connection trước khi gọi
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            // Start typing indicator
            this.connection.invoke('StartTyping', this.meetingCode);
        }

        // Stop typing indicator after 2 seconds of no input
        this.typingTimeout = setTimeout(() => {
            this.stopTyping();
        }, 2000);
    }

    stopTyping() {
        if (this.typingTimeout) {
            clearTimeout(this.typingTimeout);
            this.typingTimeout = null;
        }
        
        // Kiểm tra connection trước khi gọi
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            this.connection.invoke('StopTyping', this.meetingCode);
        }
    }

    togglePrivateMessage() {
        const privateCheckbox = document.getElementById('privateMessage');
        const recipientSelect = document.getElementById('recipientSelect');
        
        if (privateCheckbox && recipientSelect) {
            if (privateCheckbox.checked) {
                recipientSelect.style.display = 'block';
                // Load participants for private message
                this.loadParticipants();
            } else {
                recipientSelect.style.display = 'none';
                recipientSelect.value = '';
            }
        }
    }

    async loadParticipants() {
        try {
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                await this.connection.invoke('GetRoomParticipants', this.meetingCode);
            }
        } catch (error) {
            console.error('Error loading participants:', error);
        }
    }

    addMessage(message) {
        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) return;

        // Kiểm tra xem tin nhắn đã tồn tại chưa (tránh duplicate)
        const existingMessage = document.querySelector(`[data-message-id="${message.messageId}"]`);
        if (existingMessage) {
            console.log('⚠️ Message already exists, skipping:', message.messageId);
            return;
        }

        const messageHtml = this.renderMessage(message);
        messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
        
        // Store message in memory for persistence
        if (!this.messages) this.messages = [];
        this.messages.push(message);
    }

    updateMessage(message) {
        const messageElement = document.querySelector(`[data-message-id="${message.messageId}"]`);
        if (messageElement) {
            const contentElement = messageElement.querySelector('.message-text');
            if (contentElement) {
                contentElement.textContent = message.content;
                contentElement.classList.add('edited');
            }
        }
    }

    removeMessage(messageId) {
        const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
        if (messageElement) {
            messageElement.remove();
        }
    }

    renderMessage(message) {
        const isOwnMessage = message.senderId === this.currentUserId;
        const messageClass = isOwnMessage ? 'own-message' : 'other-message';
        const timeString = new Date(message.sentAt).toLocaleTimeString('vi-VN', { 
            hour: '2-digit', 
            minute: '2-digit' 
        });

        let avatarHtml = '';
        if (message.senderAvatar) {
            avatarHtml = `<img src="${message.senderAvatar}" alt="${message.senderName}" class="avatar-img" />`;
        } else {
            avatarHtml = `<div class="avatar-placeholder">${message.senderName.charAt(0).toUpperCase()}</div>`;
        }

        let privateBadge = '';
        if (message.isPrivate) {
            privateBadge = '<div class="private-badge">🔒 Tin nhắn riêng tư</div>';
        }

        let actionsHtml = '';
        if (message.canEdit) {
            actionsHtml += '<button class="btn btn-link edit-message">✏️</button>';
        }
        if (message.canDelete) {
            actionsHtml += '<button class="btn btn-link text-danger delete-message">🗑️</button>';
        }

        return `
            <div class="message ${messageClass}" data-message-id="${message.messageId}">
                <div class="message-avatar">
                    ${avatarHtml}
                </div>
                <div class="message-content">
                    <div class="message-header">
                        <span class="sender-name">${message.senderName}</span>
                        <span class="message-time">${timeString}</span>
                        ${message.isEdited ? '<span class="edited-badge">(đã chỉnh sửa)</span>' : ''}
                    </div>
                    ${privateBadge}
                    <div class="message-text">${message.displayContent}</div>
                    <div class="message-actions">
                        ${actionsHtml}
                    </div>
                </div>
            </div>
        `;
    }

    showSystemMessage(content) {
        const messagesContainer = document.getElementById('messagesContainer');
        if (messagesContainer) {
            const systemHtml = `
                <div class="system-message">
                    <span class="system-text">${content}</span>
                </div>
            `;
            messagesContainer.insertAdjacentHTML('beforeend', systemHtml);
            this.scrollToBottom();
        }
    }

    showTypingIndicator(userId, isTyping) {
        // Implement typing indicator UI
        const typingElement = document.getElementById('typing-indicator');
        if (isTyping) {
            if (!typingElement) {
                const messagesContainer = document.getElementById('messagesContainer');
                if (messagesContainer) {
                    const typingHtml = `
                        <div id="typing-indicator" class="typing-indicator">
                            <span class="typing-text">${userId} đang nhập tin nhắn...</span>
                        </div>
                    `;
                    messagesContainer.insertAdjacentHTML('beforeend', typingHtml);
                }
            }
        } else {
            if (typingElement) {
                typingElement.remove();
            }
        }
    }

    updateParticipantsList(participants) {
        const recipientSelect = document.getElementById('recipientSelect');
        if (recipientSelect) {
            // Clear existing options except the first one
            while (recipientSelect.children.length > 1) {
                recipientSelect.removeChild(recipientSelect.lastChild);
            }

            // Add new participants
            participants.forEach(participant => {
                if (participant.userId !== this.currentUserId) {
                    const option = document.createElement('option');
                    option.value = participant.userId;
                    option.textContent = participant.fullName;
                    recipientSelect.appendChild(option);
                }
            });
        }
    }

    showConnectionStatus(message, status) {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `connection-status ${status}`;
        }
    }

    showNotification(message) {
        // Simple notification - can be enhanced with toast library
        console.log('🔔 Notification:', message);
    }

    showError(message) {
        const errorElement = document.getElementById('error-notification');
        if (errorElement) {
            const messageElement = errorElement.querySelector('.error-message');
            if (messageElement) {
                messageElement.textContent = message;
            }
            errorElement.style.display = 'block';
            
            // Auto hide after 5 seconds
            setTimeout(() => {
                errorElement.style.display = 'none';
            }, 5000);
        }
    }

    setLoadingState(loading) {
        const sendBtn = document.getElementById('sendMessage');
        const messageInput = document.getElementById('messageInput');
        
        if (sendBtn) {
            sendBtn.disabled = loading;
            sendBtn.innerHTML = loading ? '<i class="fas fa-spinner fa-spin"></i>' : '<i class="fas fa-paper-plane"></i>';
        }
        
        if (messageInput) {
            messageInput.disabled = loading;
        }
    }

    scrollToBottom() {
        const messagesContainer = document.getElementById('messagesContainer');
        if (messagesContainer) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
    }

    generateSessionId() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    // Public method to refresh chat
    async refreshChat() {
        try {
            await this.loadParticipants();
        } catch (error) {
            console.error('Error refreshing chat:', error);
        }
    }

    // Public method to restore messages when reopening chat
    restoreMessages() {
        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer || !this.messages || this.messages.length === 0) return;

        console.log(`🔄 Restoring ${this.messages.length} messages...`);
        
        // Clear existing messages
        messagesContainer.innerHTML = '';
        
        // Restore all messages
        this.messages.forEach(message => {
            const messageHtml = this.renderMessage(message);
            messagesContainer.insertAdjacentHTML('beforeend', messageHtml);
        });
        
        // Scroll to bottom
        this.scrollToBottom();
    }

    // Load existing messages from server
    async loadExistingMessages() {
        try {
            console.log('🔄 Loading existing messages from server...');
            
            // Get room ID from meeting code
            const roomResponse = await fetch(`/Meeting/GetActiveSession?code=${encodeURIComponent(this.meetingCode)}`);
            if (!roomResponse.ok) {
                console.error('Failed to get active session');
                return;
            }
            
            const sessionData = await roomResponse.json();
            console.log('Session data:', sessionData);
            
            if (!sessionData.sessionId) {
                console.error('No session ID found');
                return;
            }

            // Get room ID from database using meeting code
            const roomIdResponse = await fetch(`/Meeting/GetRoomId?code=${encodeURIComponent(this.meetingCode)}`);
            if (!roomIdResponse.ok) {
                console.error('Failed to get room ID');
                return;
            }
            
            const roomData = await roomIdResponse.json();
            console.log('Room data:', roomData);
            
            if (!roomData.roomId) {
                console.error('No room ID found');
                return;
            }

            // Load messages from server
            const response = await fetch(`/Meeting/GetRoomMessages?roomId=${roomData.roomId}&sessionId=${sessionData.sessionId}`);
            if (response.ok) {
                const messages = await response.json();
                console.log('Loaded messages:', messages);
                
                // Clear existing messages
                const messagesContainer = document.getElementById('messagesContainer');
                if (messagesContainer) {
                    messagesContainer.innerHTML = '';
                }
                
                // Initialize messages array
                this.messages = [];
                
                // Add messages
                messages.forEach(message => {
                    this.addMessage(message);
                });
                
                console.log(`✅ Loaded ${messages.length} existing messages`);
                this.scrollToBottom();
            } else {
                console.error('Failed to load messages:', response.status, response.statusText);
            }
        } catch (error) {
            console.error('Error loading existing messages:', error);
        }
    }

    // Public method to disconnect
    async disconnect() {
        this.manualDisconnect = true;
        
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke('LeaveRoom', this.meetingCode, this.sessionId);
                await this.connection.stop();
            } catch (error) {
                console.error('Error disconnecting:', error);
            }
        }
    }
}

// Initialize chat panel when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Only create instance if none exists and chat panel element is present
    if (!window.meetingChatPanel && document.querySelector('.meeting-chat-panel')) {
        window.meetingChatPanel = new MeetingChatPanel();
    }
}); 