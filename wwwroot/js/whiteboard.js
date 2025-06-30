// Whiteboard System for Zela Video Call
// Real-time collaborative drawing with multiple tools and features

class WhiteboardSystem {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.isDrawing = false;
        this.currentTool = 'pen';
        this.currentColor = '#000000';
        this.currentSize = 2;
        this.sessionId = null;
        this.userId = null;
        this.connection = null;
        this.isConnected = false;
        this.lastActionTime = 0;
        this.actionThrottle = 16; // ~60fps
        
        // Drawing state
        this.lastX = 0;
        this.lastY = 0;
        this.path = [];
        
        // Tools configuration
        this.tools = {
            pen: { name: 'Pen', icon: '✏️' },
            brush: { name: 'Brush', icon: '🖌️' },
            eraser: { name: 'Eraser', icon: '🧽' },
            line: { name: 'Line', icon: '📏' },
            rectangle: { name: 'Rectangle', icon: '⬜' },
            circle: { name: 'Circle', icon: '⭕' },
            text: { name: 'Text', icon: '📝' },
            select: { name: 'Select', icon: '👆' }
        };
        
        // Colors palette
        this.colors = [
            '#000000', '#FFFFFF', '#FF0000', '#00FF00', '#0000FF',
            '#FFFF00', '#FF00FF', '#00FFFF', '#FFA500', '#800080',
            '#008000', '#800000', '#000080', '#808080', '#C0C0C0'
        ];
        
        // Initialize
        this.initializeWhiteboard();
    }

    // ======== INITIALIZATION ========
    
    initializeWhiteboard() {
        this.createWhiteboardUI();
        this.setupEventListeners();
        this.initializeSignalR();
        console.log('🎨 Whiteboard System initialized');
    }

    createWhiteboardUI() {
        const whiteboardContainer = document.getElementById('whiteboard-container');
        if (!whiteboardContainer) {
            console.error('Whiteboard container not found');
            return;
        }

        whiteboardContainer.innerHTML = `
            <div class="whiteboard-header">
                <h3>🎨 Whiteboard</h3>
                <div class="whiteboard-controls">
                    <button id="toggle-whiteboard" class="btn btn-primary">📋 Mở Whiteboard</button>
                    <button id="clear-whiteboard" class="btn btn-danger">🗑️ Xóa</button>
                    <button id="save-template" class="btn btn-success">💾 Lưu Template</button>
                    <button id="export-whiteboard" class="btn btn-info">📤 Xuất</button>
                </div>
            </div>
            
            <div class="whiteboard-main">
                <div class="whiteboard-toolbar">
                    <div class="tool-group">
                        <label>🛠️ Công cụ:</label>
                        <div class="tool-buttons">
                            ${Object.entries(this.tools).map(([key, tool]) => `
                                <button class="tool-btn ${key === 'pen' ? 'active' : ''}" data-tool="${key}" title="${tool.name}">
                                    ${tool.icon}
                                </button>
                            `).join('')}
                        </div>
                    </div>
                    
                    <div class="tool-group">
                        <label>🎨 Màu sắc:</label>
                        <div class="color-palette">
                            ${this.colors.map(color => `
                                <button class="color-btn ${color === '#000000' ? 'active' : ''}" 
                                        style="background-color: ${color}" data-color="${color}"></button>
                            `).join('')}
                        </div>
                    </div>
                    
                    <div class="tool-group">
                        <label>📏 Kích thước:</label>
                        <input type="range" id="size-slider" min="1" max="20" value="2" class="size-slider">
                        <span id="size-value">2px</span>
                    </div>
                </div>
                
                <canvas id="whiteboard-canvas" width="800" height="600"></canvas>
                
                <div class="whiteboard-sidebar">
                    <div class="sidebar-section">
                        <h4>👥 Người tham gia</h4>
                        <div id="participants-list"></div>
                    </div>
                    
                    <div class="sidebar-section">
                        <h4>💬 Ghi chú</h4>
                        <div class="annotations-list" id="annotations-list"></div>
                        <div class="annotation-input">
                            <input type="text" id="annotation-input" placeholder="Thêm ghi chú...">
                            <button id="add-annotation">➕</button>
                        </div>
                    </div>
                    
                    <div class="sidebar-section">
                        <h4>📊 Thống kê</h4>
                        <div id="whiteboard-stats">
                            <div>Hành động: <span id="action-count">0</span></div>
                            <div>Người dùng: <span id="user-count">0</span></div>
                            <div>Thời gian: <span id="session-time">00:00</span></div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Initialize canvas
        this.canvas = document.getElementById('whiteboard-canvas');
        this.ctx = this.canvas.getContext('2d');
        this.setupCanvas();
    }

    setupCanvas() {
        // Set canvas background
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Set default styles
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentSize;
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
    }

    setupEventListeners() {
        // Toggle whiteboard
        document.getElementById('toggle-whiteboard')?.addEventListener('click', () => {
            console.log('Nút Whiteboard đã được bấm');
            const container = document.getElementById('whiteboard-container');
            container.style.display = 'block';
            // Nếu cần, gọi lại initializeWhiteboard() ở đây
        });

        // Clear whiteboard
        document.getElementById('clear-whiteboard')?.addEventListener('click', () => {
            this.clearWhiteboard();
        });

        // Save template
        document.getElementById('save-template')?.addEventListener('click', () => {
            this.saveAsTemplate();
        });

        // Export whiteboard
        document.getElementById('export-whiteboard')?.addEventListener('click', () => {
            this.exportWhiteboard();
        });

        // Tool selection
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.selectTool(e.target.dataset.tool);
            });
        });

        // Color selection
        document.querySelectorAll('.color-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.selectColor(e.target.dataset.color);
            });
        });

        // Size slider
        const sizeSlider = document.getElementById('size-slider');
        const sizeValue = document.getElementById('size-value');
        if (sizeSlider && sizeValue) {
            sizeSlider.addEventListener('input', (e) => {
                this.currentSize = parseInt(e.target.value);
                sizeValue.textContent = `${this.currentSize}px`;
                this.ctx.lineWidth = this.currentSize;
            });
        }

        // Canvas events
        if (this.canvas) {
            this.canvas.addEventListener('mousedown', (e) => this.startDrawing(e));
            this.canvas.addEventListener('mousemove', (e) => this.draw(e));
            this.canvas.addEventListener('mouseup', () => this.stopDrawing());
            this.canvas.addEventListener('mouseout', () => this.stopDrawing());
            
            // Touch events for mobile
            this.canvas.addEventListener('touchstart', (e) => this.startDrawing(e));
            this.canvas.addEventListener('touchmove', (e) => this.draw(e));
            this.canvas.addEventListener('touchend', () => this.stopDrawing());
        }

        // Annotation input
        document.getElementById('add-annotation')?.addEventListener('click', () => {
            this.addAnnotation();
        });

        document.getElementById('annotation-input')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.addAnnotation();
            }
        });
    }

    // ======== SIGNALR CONNECTION ========
    
    initializeSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/whiteboardHub')
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .build();

        this.setupSignalREvents();
        this.connectSignalR();
    }

    setupSignalREvents() {
        // Connection events
        this.connection.onreconnecting(() => {
            console.log('🔄 Whiteboard reconnecting...');
            this.isConnected = false;
        });

        this.connection.onreconnected(() => {
            console.log('✅ Whiteboard reconnected');
            this.isConnected = true;
            if (this.sessionId) {
                this.joinSession();
            }
        });

        this.connection.onclose(() => {
            console.log('❌ Whiteboard connection closed');
            this.isConnected = false;
        });

        // Drawing events
        this.connection.on('DrawActionReceived', (action) => {
            this.handleRemoteDrawAction(action);
        });

        this.connection.on('WhiteboardCleared', (userId) => {
            this.handleRemoteClear(userId);
        });

        // User events
        this.connection.on('UserJoinedWhiteboard', (userId) => {
            this.handleUserJoined(userId);
        });

        this.connection.on('UserLeftWhiteboard', (userId) => {
            this.handleUserLeft(userId);
        });

        // Cursor events
        this.connection.on('CursorUpdated', (data) => {
            this.updateRemoteCursor(data);
        });

        // Template events
        this.connection.on('TemplateShared', (data) => {
            this.handleTemplateShared(data);
        });

        // Annotation events
        this.connection.on('AnnotationAdded', (data) => {
            this.handleAnnotationAdded(data);
        });
    }

    async connectSignalR() {
        try {
            await this.connection.start();
            this.isConnected = true;
            console.log('✅ Whiteboard SignalR connected');
        } catch (error) {
            console.error('❌ Whiteboard SignalR connection failed:', error);
            this.isConnected = false;
        }
    }

    // ======== SESSION MANAGEMENT ========
    
    async createSession(roomId) {
        try {
            const response = await fetch('/api/whiteboard/session/create', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ roomId: roomId })
            });

            const result = await response.json();
            if (result.success) {
                this.sessionId = result.sessionId;
                this.userId = getCurrentUserId();
                await this.joinSession();
                return result.sessionId;
            } else {
                throw new Error(result.error);
            }
        } catch (error) {
            console.error('Error creating whiteboard session:', error);
            throw error;
        }
    }

    async joinSession() {
        if (!this.connection || !this.isConnected || !this.sessionId || !this.userId) {
            return;
        }

        try {
            await this.connection.invoke('JoinWhiteboardSession', this.sessionId, this.userId);
            console.log('✅ Joined whiteboard session:', this.sessionId);
            
            // Load existing actions
            await this.loadExistingActions();
        } catch (error) {
            console.error('Error joining whiteboard session:', error);
        }
    }

    async loadExistingActions() {
        try {
            const response = await fetch(`/api/whiteboard/session/${this.sessionId}/actions`);
            const result = await response.json();
            
            if (result.success && result.actions) {
                result.actions.forEach(action => {
                    this.handleRemoteDrawAction(action);
                });
            }
        } catch (error) {
            console.error('Error loading existing actions:', error);
        }
    }

    // ======== DRAWING FUNCTIONS ========
    
    startDrawing(e) {
        this.isDrawing = true;
        const pos = this.getMousePos(e);
        this.lastX = pos.x;
        this.lastY = pos.y;
        this.path = [{ x: pos.x, y: pos.y }];
        
        // Update cursor for other users
        this.updateCursor(pos.x, pos.y);
    }

    draw(e) {
        if (!this.isDrawing) return;

        const pos = this.getMousePos(e);
        this.path.push({ x: pos.x, y: pos.y });

        // Throttle drawing actions
        const now = Date.now();
        if (now - this.lastActionTime < this.actionThrottle) {
            return;
        }
        this.lastActionTime = now;

        // Draw locally
        this.drawPath();

        // Send to server
        this.sendDrawAction('draw', {
            path: this.path,
            color: this.currentColor,
            size: this.currentSize,
            tool: this.currentTool
        });

        this.lastX = pos.x;
        this.lastY = pos.y;
    }

    stopDrawing() {
        if (!this.isDrawing) return;
        
        this.isDrawing = false;
        this.path = [];
    }

    drawPath() {
        if (this.path.length < 2) return;

        this.ctx.beginPath();
        this.ctx.moveTo(this.path[0].x, this.path[0].y);

        for (let i = 1; i < this.path.length; i++) {
            this.ctx.lineTo(this.path[i].x, this.path[i].y);
        }

        this.ctx.stroke();
    }

    getMousePos(e) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;

        if (e.touches && e.touches[0]) {
            return {
                x: (e.touches[0].clientX - rect.left) * scaleX,
                y: (e.touches[0].clientY - rect.top) * scaleY
            };
        } else {
            return {
                x: (e.clientX - rect.left) * scaleX,
                y: (e.clientY - rect.top) * scaleY
            };
        }
    }

    // ======== TOOL FUNCTIONS ========
    
    selectTool(tool) {
        this.currentTool = tool;
        
        // Update UI
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-tool="${tool}"]`)?.classList.add('active');
        
        // Update cursor
        this.canvas.style.cursor = this.getToolCursor(tool);
    }

    selectColor(color) {
        this.currentColor = color;
        this.ctx.strokeStyle = color;
        
        // Update UI
        document.querySelectorAll('.color-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-color="${color}"]`)?.classList.add('active');
    }

    getToolCursor(tool) {
        const cursors = {
            pen: 'crosshair',
            brush: 'crosshair',
            eraser: 'crosshair',
            line: 'crosshair',
            rectangle: 'crosshair',
            circle: 'crosshair',
            text: 'text',
            select: 'default'
        };
        return cursors[tool] || 'default';
    }

    // ======== REMOTE DRAWING HANDLERS ========
    
    handleRemoteDrawAction(action) {
        try {
            const payload = JSON.parse(action.payload);
            
            switch (action.actionType) {
                case 'draw':
                    this.drawRemotePath(payload);
                    break;
                case 'clear':
                    this.clearCanvas();
                    break;
                case 'text':
                    this.drawRemoteText(payload);
                    break;
                case 'shape':
                    this.drawRemoteShape(payload);
                    break;
            }
        } catch (error) {
            console.error('Error handling remote draw action:', error);
        }
    }

    drawRemotePath(payload) {
        const { path, color, size, tool } = payload;
        
        this.ctx.save();
        this.ctx.strokeStyle = color;
        this.ctx.lineWidth = size;
        
        if (tool === 'eraser') {
            this.ctx.globalCompositeOperation = 'destination-out';
        }
        
        this.ctx.beginPath();
        this.ctx.moveTo(path[0].x, path[0].y);
        
        for (let i = 1; i < path.length; i++) {
            this.ctx.lineTo(path[i].x, path[i].y);
        }
        
        this.ctx.stroke();
        this.ctx.restore();
    }

    handleRemoteClear(userId) {
        this.clearCanvas();
        this.showNotification(`Whiteboard cleared by user ${userId}`, 'info');
    }

    clearCanvas() {
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    }

    // ======== COMMUNICATION ========
    
    async sendDrawAction(actionType, payload) {
        if (!this.connection || !this.isConnected || !this.sessionId) {
            return;
        }

        try {
            await this.connection.invoke('DrawAction', this.sessionId, actionType, JSON.stringify(payload), this.userId);
        } catch (error) {
            console.error('Error sending draw action:', error);
        }
    }

    async clearWhiteboard() {
        if (!this.connection || !this.isConnected || !this.sessionId) {
            return;
        }

        try {
            await this.connection.invoke('ClearWhiteboard', this.sessionId, this.userId);
            this.clearCanvas();
        } catch (error) {
            console.error('Error clearing whiteboard:', error);
        }
    }

    updateCursor(x, y) {
        if (!this.connection || !this.isConnected || !this.sessionId) {
            return;
        }

        this.connection.invoke('UpdateCursor', this.sessionId, this.userId, x, y).catch(error => {
            console.error('Error updating cursor:', error);
        });
    }

    updateRemoteCursor(data) {
        // Show remote cursor indicator
        this.showRemoteCursor(data.userId, data.x, data.y);
    }

    showRemoteCursor(userId, x, y) {
        // Implementation for showing remote cursors
        // This would create visual indicators for other users' cursors
    }

    // ======== TEMPLATE & EXPORT ========
    
    async saveAsTemplate() {
        const templateName = prompt('Nhập tên template:');
        if (!templateName) return;

        try {
            const response = await fetch(`/api/whiteboard/session/${this.sessionId}/save-template`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ templateName: templateName })
            });

            const result = await response.json();
            if (result.success) {
                this.showNotification('Template saved successfully!', 'success');
            } else {
                throw new Error(result.error);
            }
        } catch (error) {
            console.error('Error saving template:', error);
            this.showNotification('Failed to save template', 'error');
        }
    }

    async exportWhiteboard() {
        try {
            const response = await fetch(`/api/whiteboard/session/${this.sessionId}/export/image?format=png`);
            const blob = await response.blob();
            
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `whiteboard-${this.sessionId}.png`;
            a.click();
            
            window.URL.revokeObjectURL(url);
        } catch (error) {
            console.error('Error exporting whiteboard:', error);
            this.showNotification('Failed to export whiteboard', 'error');
        }
    }

    // ======== ANNOTATIONS ========
    
    async addAnnotation() {
        const input = document.getElementById('annotation-input');
        const text = input?.value?.trim();
        
        if (!text) return;

        try {
            await this.connection.invoke('AddAnnotation', this.sessionId, this.userId, text);
            input.value = '';
        } catch (error) {
            console.error('Error adding annotation:', error);
        }
    }

    handleAnnotationAdded(data) {
        const annotationsList = document.getElementById('annotations-list');
        if (annotationsList) {
            const annotationDiv = document.createElement('div');
            annotationDiv.className = 'annotation-item';
            annotationDiv.innerHTML = `
                <div class="annotation-header">
                    <span class="annotation-user">User ${data.userId}</span>
                    <span class="annotation-time">${new Date(data.timestamp).toLocaleTimeString()}</span>
                </div>
                <div class="annotation-text">${data.annotation}</div>
            `;
            annotationsList.appendChild(annotationDiv);
            annotationsList.scrollTop = annotationsList.scrollHeight;
        }
    }

    // ======== UI FUNCTIONS ========
    
    toggleWhiteboard() {
        const main = document.querySelector('.whiteboard-main');
        const toggleBtn = document.getElementById('toggle-whiteboard');
        
        if (main.style.display === 'none' || !main.style.display) {
            main.style.display = 'flex';
            toggleBtn.textContent = '📋 Đóng Whiteboard';
            this.resizeCanvas();
        } else {
            main.style.display = 'none';
            toggleBtn.textContent = '📋 Mở Whiteboard';
        }
    }

    resizeCanvas() {
        const container = this.canvas.parentElement;
        const rect = container.getBoundingClientRect();
        
        this.canvas.width = rect.width - 20;
        this.canvas.height = rect.height - 20;
        
        this.setupCanvas();
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // ======== UTILITY FUNCTIONS ========
    
    getCurrentUserId() {
        // This should match the method used in other parts of the app
        const userIdElement = document.querySelector('[data-user-id]');
        if (userIdElement) {
            return parseInt(userIdElement.getAttribute('data-user-id'));
        }
        return Math.floor(Math.random() * 1000000);
    }

    destroy() {
        if (this.connection) {
            this.connection.stop();
        }
        console.log('🎨 Whiteboard System destroyed');
    }
}

// Global function to initialize whiteboard
function initializeWhiteboard() {
    if (!window.whiteboardSystem) {
        window.whiteboardSystem = new WhiteboardSystem();
    }
    return window.whiteboardSystem;
}

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Initialize whiteboard if we're in a meeting room
    if (window.location.pathname.includes('/Meeting/Room')) {
        setTimeout(() => {
            initializeWhiteboard();
        }, 1000); // Wait for other systems to initialize
    }
}); 