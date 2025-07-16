// Whiteboard Editor System for Zela
// Standalone whiteboard editor for creating and editing templates

class WhiteboardEditor {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.isDrawing = false;
        this.currentTool = 'pen';
        this.currentColor = '#000000';
        this.currentSize = 5;
        this.sessionId = null;
        this.userId = null;
        this.connection = null;
        this.isConnected = false;
        
        // Drawing state
        this.lastX = 0;
        this.lastY = 0;
        this.path = [];
        
        // Text tool state
        this.textInput = null;
        this.textPosition = null;
        this.isTextMode = false;
        
        // Shape tool state
        this.shapeStart = null;
        this.isDrawingShape = false;
        this.previewCanvas = null;
        
        // Undo/Redo state
        this.actionHistory = [];
        this.currentHistoryIndex = -1;
        this.maxHistorySize = 50;

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
        this.initializeEditor();
    }

    // ======== INITIALIZATION ========

    initializeEditor() {
        this.setupCanvas();
        this.setupEventListeners();
        console.log('🎨 Whiteboard Editor initialized');
    }

    setupCanvas() {
        this.canvas = document.getElementById('whiteboardCanvas');
        if (!this.canvas) {
            console.error('Whiteboard canvas not found');
            return;
        }

        this.ctx = this.canvas.getContext('2d');
        
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
        // Tool selection
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.selectTool(e.target.closest('.tool-btn').dataset.tool);
            });
        });

        // Color selection
        document.querySelectorAll('.color-preset').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.selectColor(e.target.dataset.color);
            });
        });

        // Color picker
        const colorPicker = document.getElementById('colorPicker');
        if (colorPicker) {
            colorPicker.addEventListener('change', (e) => {
                this.selectColor(e.target.value);
            });
        }

        // Size slider
        const sizeSlider = document.getElementById('sizeSlider');
        const sizeValue = document.getElementById('sizeValue');
        if (sizeSlider && sizeValue) {
            sizeSlider.addEventListener('input', (e) => {
                this.currentSize = parseInt(e.target.value);
                sizeValue.textContent = this.currentSize;
                this.ctx.lineWidth = this.currentSize;
            });
        }

        // Undo/Redo buttons
        document.getElementById('undoBtn')?.addEventListener('click', () => {
            this.undo();
        });

        document.getElementById('redoBtn')?.addEventListener('click', () => {
            this.redo();
        });

        // Clear button
        document.getElementById('clearBtn')?.addEventListener('click', () => {
            this.clearCanvas();
        });

        // Canvas events
        if (this.canvas) {
            this.canvas.addEventListener('mousedown', (e) => this.startDrawing(e));
            this.canvas.addEventListener('mousemove', (e) => this.draw(e));
            this.canvas.addEventListener('mouseup', () => this.stopDrawing());
            this.canvas.addEventListener('mouseout', () => this.stopDrawing());
            this.canvas.addEventListener('click', (e) => this.handleCanvasClick(e));

            // Touch events for mobile
            this.canvas.addEventListener('touchstart', (e) => this.startDrawing(e));
            this.canvas.addEventListener('touchmove', (e) => this.draw(e));
            this.canvas.addEventListener('touchend', () => this.stopDrawing());
        }
    }

    initializeSignalR() {
        if (!this.sessionId) {
            console.log('No session ID, skipping SignalR connection');
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/whiteboardHub')
            .build();

        this.setupSignalREvents();
        this.connectSignalR();
    }

    setupSignalREvents() {
        this.connection.on('ReceiveDrawAction', (action) => {
            this.handleRemoteDrawAction(action);
        });

        this.connection.on('ReceiveClear', (userId) => {
            this.handleRemoteClear(userId);
        });

        this.connection.on('UserJoined', (userId, userName) => {
            console.log(`User ${userName} joined the whiteboard`);
        });

        this.connection.on('UserLeft', (userId) => {
            console.log(`User ${userId} left the whiteboard`);
        });

        this.connection.onclose(() => {
            this.isConnected = false;
            document.getElementById('sessionStatus').textContent = 'Disconnected';
            console.log('SignalR connection closed');
        });
    }

    async connectSignalR() {
        try {
            await this.connection.start();
            this.isConnected = true;
            document.getElementById('sessionStatus').textContent = 'Connected';
            console.log('SignalR connected');
            
            // Join the whiteboard session
            if (this.sessionId && this.sessionId !== '00000000-0000-0000-0000-000000000000') {
                await this.connection.invoke('JoinWhiteboardSession', this.sessionId, this.getCurrentUserId());
                console.log('Joined whiteboard session:', this.sessionId);
            } else {
                console.warn('Invalid session ID, cannot join session');
            }
        } catch (err) {
            console.error('SignalR connection failed:', err);
            this.isConnected = false;
        }
    }

    // ======== DRAWING FUNCTIONS ========

    startDrawing(e) {
        if (this.isTextMode) return;
        
        console.log('Start drawing with tool:', this.currentTool);
        this.isDrawing = true;
        const pos = this.getMousePos(e);
        this.lastX = pos.x;
        this.lastY = pos.y;
        this.path = [{ x: pos.x, y: pos.y }];

        if (this.currentTool === 'text') {
            this.activateTextMode();
        } else if (['line', 'rectangle', 'circle'].includes(this.currentTool)) {
            this.startShapeDrawing(pos);
        }
    }

    draw(e) {
        if (!this.isDrawing || this.isTextMode) return;

        const pos = this.getMousePos(e);
        
        if (this.currentTool === 'pen' || this.currentTool === 'brush') {
            this.ctx.beginPath();
            this.ctx.moveTo(this.lastX, this.lastY);
            this.ctx.lineTo(pos.x, pos.y);
            this.ctx.stroke();
            
            this.path.push({ x: pos.x, y: pos.y });
        } else if (this.currentTool === 'eraser') {
            this.ctx.save();
            this.ctx.globalCompositeOperation = 'destination-out';
            this.ctx.beginPath();
            this.ctx.arc(pos.x, pos.y, this.currentSize / 2, 0, Math.PI * 2);
            this.ctx.fill();
            this.ctx.restore();
        } else if (['line', 'rectangle', 'circle'].includes(this.currentTool)) {
            this.updateShapePreview(pos);
        }

        this.lastX = pos.x;
        this.lastY = pos.y;
    }

    stopDrawing() {
        if (!this.isDrawing) return;

        this.isDrawing = false;

        if (this.currentTool === 'pen' || this.currentTool === 'brush') {
            this.finalizePath();
        } else if (['line', 'rectangle', 'circle'].includes(this.currentTool)) {
            this.finalizeShape();
        }
    }

    finalizePath() {
        if (this.path.length < 2) return;

        console.log('Finalizing path with', this.path.length, 'points');

        const action = {
            type: this.currentTool,
            path: this.path,
            color: this.currentColor,
            size: this.currentSize,
            timestamp: Date.now()
        };

        this.addToHistory(action);
        this.sendDrawAction('path', JSON.stringify(action));
    }

    getMousePos(e) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        
        let clientX, clientY;
        
        if (e.type.includes('touch')) {
            clientX = e.touches[0].clientX;
            clientY = e.touches[0].clientY;
        } else {
            clientX = e.clientX;
            clientY = e.clientY;
        }
        
        return {
            x: (clientX - rect.left) * scaleX,
            y: (clientY - rect.top) * scaleY
        };
    }

    // ======== TOOL FUNCTIONS ========

    selectTool(tool) {
        this.currentTool = tool;
        
        // Update UI
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-tool="${tool}"]`).classList.add('active');
        
        // Update cursor
        this.canvas.style.cursor = this.getToolCursor(tool);
        
        // Deactivate text mode if switching tools
        if (tool !== 'text') {
            this.deactivateTextMode();
        }
    }

    selectColor(color) {
        this.currentColor = color;
        this.ctx.strokeStyle = color;
        this.ctx.fillStyle = color;
        
        // Update color picker
        document.getElementById('colorPicker').value = color;
        
        // Update active color preset
        document.querySelectorAll('.color-preset').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-color="${color}"]`).classList.add('active');
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
            select: 'pointer'
        };
        return cursors[tool] || 'default';
    }

    // ======== TEXT TOOL ========

    activateTextMode() {
        this.isTextMode = true;
        this.canvas.style.cursor = 'text';
    }

    deactivateTextMode() {
        this.isTextMode = false;
        this.canvas.style.cursor = this.getToolCursor(this.currentTool);
        if (this.textInput) {
            this.textInput.remove();
            this.textInput = null;
        }
    }

    handleCanvasClick(e) {
        if (this.currentTool === 'text') {
            this.createTextInput(e);
        }
    }

    createTextInput(e) {
        const pos = this.getMousePos(e);
        
        // Remove existing text input
        if (this.textInput) {
            this.textInput.remove();
        }
        
        // Create new text input
        this.textInput = document.createElement('input');
        this.textInput.type = 'text';
        this.textInput.style.position = 'absolute';
        this.textInput.style.left = `${e.clientX}px`;
        this.textInput.style.top = `${e.clientY}px`;
        this.textInput.style.font = `${this.currentSize * 3}px Arial`;
        this.textInput.style.color = this.currentColor;
        this.textInput.style.background = 'transparent';
        this.textInput.style.border = 'none';
        this.textInput.style.outline = 'none';
        this.textInput.style.zIndex = '1000';
        
        document.body.appendChild(this.textInput);
        this.textInput.focus();
        
        this.textPosition = pos;
        
        // Handle text input events
        this.textInput.addEventListener('blur', () => this.finalizeText());
        this.textInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.finalizeText();
            } else if (e.key === 'Escape') {
                this.cancelText();
            }
        });
    }

    finalizeText() {
        if (!this.textInput || !this.textPosition) return;
        
        const text = this.textInput.value.trim();
        if (text) {
            this.drawText(text, this.textPosition.x, this.textPosition.y);
            
            const action = {
                type: 'text',
                text: text,
                x: this.textPosition.x,
                y: this.textPosition.y,
                color: this.currentColor,
                size: this.currentSize,
                timestamp: Date.now()
            };
            
            this.addToHistory(action);
            this.sendDrawAction('text', JSON.stringify(action));
        }
        
        this.cancelText();
    }

    cancelText() {
        this.deactivateTextMode();
    }

    drawText(text, x, y) {
        this.ctx.save();
        this.ctx.font = `${this.currentSize * 3}px Arial`;
        this.ctx.fillStyle = this.currentColor;
        this.ctx.fillText(text, x, y);
        this.ctx.restore();
    }

    // ======== SHAPE TOOLS ========

    startShapeDrawing(pos) {
        this.shapeStart = pos;
        this.isDrawingShape = true;
        this.createPreviewCanvas();
    }

    createPreviewCanvas() {
        if (this.previewCanvas) {
            this.previewCanvas.remove();
        }
        
        this.previewCanvas = document.createElement('canvas');
        this.previewCanvas.width = this.canvas.width;
        this.previewCanvas.height = this.canvas.height;
        this.previewCanvas.style.position = 'absolute';
        this.previewCanvas.style.top = this.canvas.offsetTop + 'px';
        this.previewCanvas.style.left = this.canvas.offsetLeft + 'px';
        this.previewCanvas.style.pointerEvents = 'none';
        this.previewCanvas.style.zIndex = '100';
        
        this.canvas.parentNode.appendChild(this.previewCanvas);
    }

    updateShapePreview(currentPos) {
        if (!this.previewCanvas || !this.shapeStart) return;
        
        const ctx = this.previewCanvas.getContext('2d');
        ctx.clearRect(0, 0, this.previewCanvas.width, this.previewCanvas.height);
        
        ctx.strokeStyle = this.currentColor;
        ctx.lineWidth = this.currentSize;
        ctx.fillStyle = this.currentColor;
        
        if (this.currentTool === 'line') {
            ctx.beginPath();
            ctx.moveTo(this.shapeStart.x, this.shapeStart.y);
            ctx.lineTo(currentPos.x, currentPos.y);
            ctx.stroke();
        } else if (this.currentTool === 'rectangle') {
            const width = currentPos.x - this.shapeStart.x;
            const height = currentPos.y - this.shapeStart.y;
            ctx.strokeRect(this.shapeStart.x, this.shapeStart.y, width, height);
        } else if (this.currentTool === 'circle') {
            const radius = Math.sqrt(
                Math.pow(currentPos.x - this.shapeStart.x, 2) + 
                Math.pow(currentPos.y - this.shapeStart.y, 2)
            );
            ctx.beginPath();
            ctx.arc(this.shapeStart.x, this.shapeStart.y, radius, 0, Math.PI * 2);
            ctx.stroke();
        }
    }

    finalizeShape() {
        if (!this.shapeStart || !this.isDrawingShape) return;
        
        const currentPos = { x: this.lastX, y: this.lastY };
        this.drawShape(this.shapeStart, currentPos);
        
        const action = {
            type: this.currentTool,
            start: this.shapeStart,
            end: currentPos,
            color: this.currentColor,
            size: this.currentSize,
            timestamp: Date.now()
        };
        
        this.addToHistory(action);
        this.sendDrawAction('shape', JSON.stringify(action));
        
        // Clean up
        this.shapeStart = null;
        this.isDrawingShape = false;
        if (this.previewCanvas) {
            this.previewCanvas.remove();
            this.previewCanvas = null;
        }
    }

    drawShape(start, end) {
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentSize;
        this.ctx.fillStyle = this.currentColor;
        
        if (this.currentTool === 'line') {
            this.ctx.beginPath();
            this.ctx.moveTo(start.x, start.y);
            this.ctx.lineTo(end.x, end.y);
            this.ctx.stroke();
        } else if (this.currentTool === 'rectangle') {
            const width = end.x - start.x;
            const height = end.y - start.y;
            this.ctx.strokeRect(start.x, start.y, width, height);
        } else if (this.currentTool === 'circle') {
            const radius = Math.sqrt(
                Math.pow(end.x - start.x, 2) + 
                Math.pow(end.y - start.y, 2)
            );
            this.ctx.beginPath();
            this.ctx.arc(start.x, start.y, radius, 0, Math.PI * 2);
            this.ctx.stroke();
        }
    }

    // ======== UNDO/REDO ========

    addToHistory(action) {
        // Remove any actions after current index
        this.actionHistory = this.actionHistory.slice(0, this.currentHistoryIndex + 1);
        
        // Add new action
        this.actionHistory.push(action);
        
        // Limit history size
        if (this.actionHistory.length > this.maxHistorySize) {
            this.actionHistory.shift();
        } else {
            this.currentHistoryIndex++;
        }
        
        this.updateUndoRedoButtons();
    }

    undo() {
        if (this.currentHistoryIndex >= 0) {
            this.currentHistoryIndex--;
            this.redrawFromHistory();
            this.updateUndoRedoButtons();
        }
    }

    redo() {
        if (this.currentHistoryIndex < this.actionHistory.length - 1) {
            this.currentHistoryIndex++;
            this.redrawFromHistory();
            this.updateUndoRedoButtons();
        }
    }

    redrawFromHistory() {
        // Clear canvas
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Redraw all actions up to current index
        for (let i = 0; i <= this.currentHistoryIndex; i++) {
            this.replayAction(this.actionHistory[i]);
        }
    }

    replayAction(action) {
        this.ctx.strokeStyle = action.color;
        this.ctx.fillStyle = action.color;
        this.ctx.lineWidth = action.size;
        
        if (action.type === 'text') {
            this.ctx.font = `${action.size * 3}px Arial`;
            this.ctx.fillText(action.text, action.x, action.y);
        } else if (action.type === 'path') {
            if (action.path && action.path.length > 1) {
                this.ctx.beginPath();
                this.ctx.moveTo(action.path[0].x, action.path[0].y);
                for (let i = 1; i < action.path.length; i++) {
                    this.ctx.lineTo(action.path[i].x, action.path[i].y);
                }
                this.ctx.stroke();
            }
        } else if (['line', 'rectangle', 'circle'].includes(action.type)) {
            this.drawShape(action.start, action.end);
        }
    }

    updateUndoRedoButtons() {
        const undoBtn = document.getElementById('undoBtn');
        const redoBtn = document.getElementById('redoBtn');
        
        if (undoBtn) {
            undoBtn.disabled = this.currentHistoryIndex < 0;
        }
        if (redoBtn) {
            redoBtn.disabled = this.currentHistoryIndex >= this.actionHistory.length - 1;
        }
    }

    // ======== REMOTE ACTIONS ========

    handleRemoteDrawAction(action) {
        const payload = JSON.parse(action.payload);
        
        if (payload.type === 'text') {
            this.drawRemoteText(payload);
        } else if (['line', 'rectangle', 'circle'].includes(payload.type)) {
            this.drawRemoteShape(payload);
        } else if (payload.type === 'path') {
            this.drawRemotePath(payload);
        }
    }

    drawRemoteText(payload) {
        this.ctx.font = `${payload.size * 3}px Arial`;
        this.ctx.fillStyle = payload.color;
        this.ctx.fillText(payload.text, payload.x, payload.y);
    }

    drawRemoteShape(payload) {
        this.ctx.strokeStyle = payload.color;
        this.ctx.lineWidth = payload.size;
        this.drawShape(payload.start, payload.end);
    }

    drawRemotePath(payload) {
        if (payload.path && payload.path.length > 1) {
            this.ctx.strokeStyle = payload.color;
            this.ctx.lineWidth = payload.size;
            this.ctx.beginPath();
            this.ctx.moveTo(payload.path[0].x, payload.path[0].y);
            for (let i = 1; i < payload.path.length; i++) {
                this.ctx.lineTo(payload.path[i].x, payload.path[i].y);
            }
            this.ctx.stroke();
        }
    }

    handleRemoteClear(userId) {
        this.clearCanvas();
    }

    // ======== UTILITY FUNCTIONS ========

    clearCanvas() {
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Clear history
        this.actionHistory = [];
        this.currentHistoryIndex = -1;
        this.updateUndoRedoButtons();
        
        // Send clear action
        this.sendDrawAction('clear', JSON.stringify({ clearedBy: this.getCurrentUserId() }));
    }

    async sendDrawAction(actionType, payload) {
        if (!this.sessionId || this.sessionId === '00000000-0000-0000-0000-000000000000') {
            console.warn('Invalid session ID, cannot send draw action');
            return;
        }
        
        try {
            console.log('Sending draw action:', actionType, 'for session:', this.sessionId);
            const response = await fetch(`/Whiteboard/AddDrawAction/${this.sessionId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    actionType: actionType,
                    payload: payload
                })
            });

            const result = await response.json();
            if (!result.success) {
                console.error('Failed to send draw action:', result.error);
            } else {
                console.log('Draw action saved successfully:', actionType);
            }
        } catch (error) {
            console.error('Error sending draw action:', error);
        }
    }

    async loadExistingActions() {
        if (!this.sessionId || this.sessionId === '00000000-0000-0000-0000-000000000000') {
            console.warn('Invalid session ID, cannot load existing actions');
            return;
        }
        
        try {
            console.log('Loading existing actions for session:', this.sessionId);
            const response = await fetch(`/Whiteboard/GetDrawActions/${this.sessionId}`);
            const result = await response.json();
            
            if (result.success) {
                // Clear canvas first
                this.ctx.fillStyle = '#FFFFFF';
                this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
                
                // Replay all actions
                result.actions.forEach(action => {
                    const payload = JSON.parse(action.payload);
                    this.replayAction(payload);
                });
                
                // Update action count
                document.getElementById('actionCount').textContent = result.actions.length;
                console.log('Loaded', result.actions.length, 'existing actions');
            }
        } catch (error) {
            console.error('Error loading existing actions:', error);
        }
    }

    exportWhiteboard() {
        if (!this.sessionId) {
            alert('No active session to export');
            return;
        }
        
        // Create download link
        const link = document.createElement('a');
        link.download = `whiteboard-${this.sessionId}.png`;
        link.href = this.canvas.toDataURL();
        link.click();
    }

    async saveAsTemplate(name, description = '', isPublic = false) {
        if (!this.sessionId) {
            alert('No active session to save');
            return;
        }
        
        try {
            const response = await fetch(`/Whiteboard/SaveTemplate?sessionId=${this.sessionId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    templateName: name,
                    description: description,
                    isPublic: isPublic
                })
            });

            const result = await response.json();
            if (result.success) {
                alert('Template saved successfully!');
                return result.templateData;
            } else {
                alert('Error saving template: ' + result.error);
                return null;
            }
        } catch (error) {
            console.error('Error saving template:', error);
            alert('Error saving template: ' + error.message);
            return null;
        }
    }

    getCurrentUserId() {
        // Get user ID from a hidden input or data attribute
        const userIdElement = document.getElementById('currentUserId');
        if (userIdElement) {
            return parseInt(userIdElement.value);
        }
        // Fallback - this should be set from the server
        return this.userId || 1;
    }

    // ======== PUBLIC API ========

    init() {
        console.log('Initializing whiteboard with sessionId:', this.sessionId);
        if (this.sessionId && this.sessionId !== '00000000-0000-0000-0000-000000000000') {
            this.initializeSignalR();
            this.loadExistingActions();
        } else {
            console.warn('No valid sessionId provided for whiteboard initialization');
        }
    }

    destroy() {
        if (this.connection) {
            this.connection.stop();
        }
        if (this.previewCanvas) {
            this.previewCanvas.remove();
        }
        if (this.textInput) {
            this.textInput.remove();
        }
    }
} 