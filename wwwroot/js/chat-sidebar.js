/**
 * Chat Sidebar JavaScript
 * Xử lý việc mở/đóng chat sidebar và load chat panel với SignalR
 */

class ChatSidebar {
    constructor() {
        this.isInitialized = false;
        this.isOpen = false;
        this.roomId = null;
        this.sessionId = null;
        this.chatPanel = null;
        
        this.init();
    }

    init() {
        // Lấy thông tin từ video grid
        const videoGrid = document.getElementById('video-grid');
        if (videoGrid) {
            this.roomId = parseInt(videoGrid.dataset.roomId) || 1; // Fallback to 1
            this.sessionId = videoGrid.dataset.sessionId || this.generateSessionId();
        }

        this.bindEvents();
        this.isInitialized = true;
    }

    bindEvents() {
        // Toggle chat button
        const toggleBtn = document.getElementById('toggle-chat');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', () => this.toggleChat());
        }

        // Close chat button
        const closeBtn = document.getElementById('close-chat');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => this.closeChat());
        }

        // Close when clicking overlay (chỉ khi click bên ngoài chat sidebar)
        const overlay = document.getElementById('sidebar-overlay');
        if (overlay) {
            overlay.addEventListener('click', (e) => {
                // Chỉ đóng chat nếu click vào overlay, không phải vào chat sidebar
                if (e.target === overlay) {
                    this.closeChat();
                }
            });
        }

        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isOpen) {
                this.closeChat();
            }
        });
    }

    async toggleChat() {
        if (this.isOpen) {
            this.closeChat();
        } else {
            await this.openChat();
        }
    }

    async openChat() {
        const chatSidebar = document.getElementById('chat-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        const toggleBtn = document.getElementById('toggle-chat');
        const chatContent = document.getElementById('chat-content');

        if (!chatSidebar || !overlay || !toggleBtn || !chatContent) {
            console.error('Chat sidebar elements not found');
            return;
        }

        try {
            // Open sidebar first
            chatSidebar.classList.add('open');
            overlay.classList.add('show');
            toggleBtn.classList.add('active');
            toggleBtn.innerHTML = '✕ Đóng';

            // Check if chat panel already exists
            if (this.chatPanel && this.chatPanel.isInitialized) {
                console.log('✅ Chat panel already exists, reconnecting...');
                
                // Show existing content
                if (chatContent.children.length === 0) {
                    // If content is empty, reload it
                    await this.loadChatPanel();
                } else {
                    // Restore messages and reconnect SignalR if needed
                    if (this.chatPanel.restoreMessages) {
                        this.chatPanel.restoreMessages();
                    }
                    
                    if (this.chatPanel.connection && this.chatPanel.connection.state !== signalR.HubConnectionState.Connected) {
                        console.log('🔄 Reconnecting SignalR...');
                        await this.chatPanel.initializeSignalR();
                    }
                }
            } else {
                console.log('🔄 Loading new chat panel...');
                // Show loading state
                chatContent.innerHTML = `
                    <div class="loading-spinner">
                        <div class="spinner"></div>
                        <p>Đang tải chat...</p>
                    </div>
                `;

                // Load chat panel
                await this.loadChatPanel();
            }

            this.isOpen = true;

        } catch (error) {
            console.error('Error opening chat:', error);
            chatContent.innerHTML = `
                <div class="error-message">
                    <p>❌ Không thể tải chat</p>
                    <button class="btn btn-sm btn-primary" onclick="chatSidebar.retryLoadChat()">Thử lại</button>
                </div>
            `;
        }
    }

    closeChat() {
        const chatSidebar = document.getElementById('chat-sidebar');
        const overlay = document.getElementById('sidebar-overlay');
        const toggleBtn = document.getElementById('toggle-chat');

        if (chatSidebar) chatSidebar.classList.remove('open');
        if (overlay) overlay.classList.remove('show');
        if (toggleBtn) {
            toggleBtn.classList.remove('active');
            toggleBtn.innerHTML = '💬 Chat';
        }

        // Don't disconnect SignalR, just hide the UI
        // This allows messages to still be received in background
        console.log('📱 Chat UI closed, keeping SignalR connection alive');

        this.isOpen = false;
    }

    async loadChatPanel() {
        const chatContent = document.getElementById('chat-content');
        if (!chatContent) return;

        try {
            // Load chat panel via AJAX
            const response = await fetch(`/Meeting/ChatPanel?roomId=${this.roomId}&sessionId=${this.sessionId}`);
            
            if (response.ok) {
                const html = await response.text();
                chatContent.innerHTML = html;

                // Initialize chat panel with SignalR
                this.initializeChatPanel();
            } else {
                throw new Error('Failed to load chat panel');
            }
        } catch (error) {
            console.error('Error loading chat panel:', error);
            throw error;
        }
    }

    initializeChatPanel() {
        // Wait a bit for DOM to be ready
        setTimeout(() => {
            try {
                // Check if global chat panel instance already exists
                if (window.meetingChatPanel) {
                    // Use existing instance
                    this.chatPanel = window.meetingChatPanel;
                    console.log('✅ Using existing chat panel instance');
                } else if (typeof MeetingChatPanel !== 'undefined') {
                    // Initialize new chat panel instance only if none exists
                    this.chatPanel = new MeetingChatPanel();
                    window.meetingChatPanel = this.chatPanel; // Store globally
                    console.log('✅ Chat panel initialized with SignalR');
                } else {
                    console.error('❌ MeetingChatPanel class not found');
                    // Fallback to basic chat without SignalR
                    this.initializeBasicChat();
                }
            } catch (error) {
                console.error('❌ Error initializing chat panel:', error);
                this.initializeBasicChat();
            }
        }, 500); // Tăng thời gian chờ để đảm bảo DOM đã sẵn sàng
    }

    initializeBasicChat() {
        // Fallback basic chat functionality without SignalR
        console.log('⚠️ Using basic chat (no SignalR)');
        
        const sendBtn = document.getElementById('sendMessage');
        const messageInput = document.getElementById('messageInput');
        
        if (sendBtn && messageInput) {
            sendBtn.addEventListener('click', () => {
                this.sendBasicMessage();
            });
            
            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendBasicMessage();
                }
            });
        }
    }

    async sendBasicMessage() {
        const messageInput = document.getElementById('messageInput');
        if (!messageInput || !messageInput.value.trim()) return;

        const messageData = {
            content: messageInput.value.trim(),
            roomId: this.roomId,
            sessionId: this.sessionId,
            messageType: 0, // Text
            isPrivate: false,
            recipientId: null
        };

        try {
            const response = await fetch('/Meeting/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(messageData)
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    messageInput.value = '';
                    // Reload messages
                    await this.loadChatPanel();
                } else {
                    this.showError(result.error || 'Không thể gửi tin nhắn');
                }
            } else {
                this.showError('Lỗi kết nối');
            }
        } catch (error) {
            console.error('Error sending message:', error);
            this.showError('Không thể gửi tin nhắn');
        }
    }

    showError(message) {
        const chatContent = document.getElementById('chat-content');
        if (chatContent) {
            const errorHtml = `
                <div class="error-message">
                    <p>❌ ${message}</p>
                </div>
            `;
            chatContent.innerHTML = errorHtml;
        }
    }

    async retryLoadChat() {
        await this.loadChatPanel();
    }

    generateSessionId() {
        // Generate a temporary session ID if not available
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    // Public method to refresh chat
    async refreshChat() {
        if (this.isOpen) {
            await this.loadChatPanel();
        }
    }

    // Public method to check if chat is open
    isChatOpen() {
        return this.isOpen;
    }

    // Public method to get chat panel instance
    getChatPanel() {
        return this.chatPanel;
    }
}

// Initialize chat sidebar when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.chatSidebar = new ChatSidebar();
});

// Global function to open chat from other scripts
window.openChat = function() {
    if (window.chatSidebar) {
        window.chatSidebar.openChat();
    }
};

// Global function to close chat from other scripts
window.closeChat = function() {
    if (window.chatSidebar) {
        window.chatSidebar.closeChat();
    }
};

// Global function to refresh chat from other scripts
window.refreshChat = function() {
    if (window.chatSidebar) {
        window.chatSidebar.refreshChat();
    }
}; 