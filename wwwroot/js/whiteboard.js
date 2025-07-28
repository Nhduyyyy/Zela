/**
 * Whiteboard JavaScript - Canvas Drawing System
 * Handles drawing, tools, and real-time collaboration
 */

class Whiteboard {
    constructor(canvasId, options = {}) {
        this.canvas = document.getElementById(canvasId);
        this.ctx = this.canvas.getContext('2d');
        this.isDrawing = false;
        this.currentTool = 'pen';
        this.currentColor = '#000000';
        this.currentSize = 2;
        this.canEdit = options.canEdit || false;
        this.sessionId = options.sessionId || 0;
        this.whiteboardId = options.whiteboardId || 0;
        
        // Drawing state
        this.lastX = 0;
        this.lastY = 0;
        this.paths = [];
        this.currentPath = [];
        
        // Collaboration
        this.connection = null;
        this.collaborators = new Map();
        this.cursorElements = new Map();
        
        // Auto-save
        this.autoSaveInterval = null;
        this.autoSaveDelay = 5000; // 5 seconds
        
        this.init();
    }

    init() {
        this.setupCanvas();
        this.setupEventListeners();
        this.setupTools();
        this.setupAutoSave();
        this.setupCollaboration();
        this.loadCanvasData();
    }

    setupCanvas() {
        // Set canvas size
        this.canvas.width = 1200;
        this.canvas.height = 800;
        
        // Set default styles
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentSize;
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
    }

    setupEventListeners() {
        if (!this.canEdit) return;

        // Mouse events
        this.canvas.addEventListener('mousedown', this.startDrawing.bind(this));
        this.canvas.addEventListener('mousemove', this.draw.bind(this));
        this.canvas.addEventListener('mouseup', this.stopDrawing.bind(this));
        this.canvas.addEventListener('mouseout', this.stopDrawing.bind(this));

        // Touch events for mobile
        this.canvas.addEventListener('touchstart', this.handleTouch.bind(this));
        this.canvas.addEventListener('touchmove', this.handleTouch.bind(this));
        this.canvas.addEventListener('touchend', this.stopDrawing.bind(this));

        // Keyboard shortcuts
        document.addEventListener('keydown', this.handleKeyboard.bind(this));
    }

    setupTools() {
        // Tool buttons
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.setTool(e.target.closest('.tool-btn').dataset.tool);
            });
        });

        // Color presets
        document.querySelectorAll('.color-preset').forEach(preset => {
            preset.addEventListener('click', (e) => {
                this.setColor(e.target.dataset.color);
            });
        });

        // Size slider
        const sizeSlider = document.getElementById('sizeSlider');
        if (sizeSlider) {
            sizeSlider.addEventListener('input', (e) => {
                this.setSize(parseInt(e.target.value));
            });
        }
    }

    setupAutoSave() {
        if (this.sessionId > 0) {
            this.autoSaveInterval = setInterval(() => {
                this.saveCanvasData();
            }, this.autoSaveDelay);
        }
    }

    setupCollaboration() {
        if (this.whiteboardId > 0 && typeof signalR !== 'undefined') {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/whiteboardHub")
                .build();

            this.connection.start()
                .then(() => {
                    console.log("Connected to WhiteboardHub");
                    this.connection.invoke("JoinWhiteboard", this.whiteboardId);
                    this.setupCollaborationEvents();
                })
                .catch(err => console.error("Error connecting to WhiteboardHub:", err));
        }
    }

    setupCollaborationEvents() {
        // Drawing updates from other users
        this.connection.on("DrawingUpdated", (drawingData, userId) => {
            this.paths = JSON.parse(drawingData);
            this.redrawCanvas();
        });

        // Cursor movements from other users
        this.connection.on("CursorMoved", (x, y, tool, userId) => {
            this.updateCollaboratorCursor(userId, x, y, tool);
        });

        // Tool changes from other users
        this.connection.on("ToolChanged", (tool, color, size, userId) => {
            this.updateCollaboratorTool(userId, tool, color, size);
        });

        // Canvas cleared by other users
        this.connection.on("CanvasCleared", (userId) => {
            this.paths = [];
            this.redrawCanvas();
        });

        // User joined/left
        this.connection.on("UserJoined", (userId) => {
            this.addCollaborator(userId);
        });

        this.connection.on("UserLeft", (userId) => {
            this.removeCollaborator(userId);
        });

        // Canvas data received
        this.connection.on("CanvasDataReceived", (canvasData) => {
            this.paths = JSON.parse(canvasData);
            this.redrawCanvas();
        });
    }

    addCollaborator(userId) {
        if (!this.collaborators.has(userId)) {
            this.collaborators.set(userId, {
                id: userId,
                cursor: this.createCursorElement(userId)
            });
        }
    }

    removeCollaborator(userId) {
        const collaborator = this.collaborators.get(userId);
        if (collaborator && collaborator.cursor) {
            collaborator.cursor.remove();
        }
        this.collaborators.delete(userId);
    }

    createCursorElement(userId) {
        const cursor = document.createElement('div');
        cursor.className = 'collaborator-cursor';
        cursor.style.position = 'absolute';
        cursor.style.pointerEvents = 'none';
        cursor.style.zIndex = '1000';
        cursor.style.width = '20px';
        cursor.style.height = '20px';
        cursor.style.borderRadius = '50%';
        cursor.style.backgroundColor = this.getRandomColor();
        cursor.style.border = '2px solid white';
        cursor.style.boxShadow = '0 2px 4px rgba(0,0,0,0.3)';
        cursor.style.display = 'none';
        
        const canvasContainer = this.canvas.parentElement;
        canvasContainer.style.position = 'relative';
        canvasContainer.appendChild(cursor);
        
        return cursor;
    }

    updateCollaboratorCursor(userId, x, y, tool) {
        const collaborator = this.collaborators.get(userId);
        if (collaborator && collaborator.cursor) {
            const rect = this.canvas.getBoundingClientRect();
            collaborator.cursor.style.left = (rect.left + x) + 'px';
            collaborator.cursor.style.top = (rect.top + y) + 'px';
            collaborator.cursor.style.display = 'block';
            
            // Hide cursor after 2 seconds of inactivity
            clearTimeout(collaborator.cursorTimeout);
            collaborator.cursorTimeout = setTimeout(() => {
                collaborator.cursor.style.display = 'none';
            }, 2000);
        }
    }

    updateCollaboratorTool(userId, tool, color, size) {
        const collaborator = this.collaborators.get(userId);
        if (collaborator && collaborator.cursor) {
            collaborator.cursor.style.backgroundColor = color;
            collaborator.cursor.style.width = (size * 2) + 'px';
            collaborator.cursor.style.height = (size * 2) + 'px';
        }
    }

    getRandomColor() {
        const colors = ['#ff0000', '#00ff00', '#0000ff', '#ffff00', '#ff00ff', '#00ffff'];
        return colors[Math.floor(Math.random() * colors.length)];
    }

    // Drawing methods
    startDrawing(e) {
        if (!this.canEdit) return;
        
        this.isDrawing = true;
        const pos = this.getMousePos(e);
        this.lastX = pos.x;
        this.lastY = pos.y;
        
        this.currentPath = [{
            x: pos.x,
            y: pos.y,
            tool: this.currentTool,
            color: this.currentColor,
            size: this.currentSize
        }];
    }

    draw(e) {
        if (!this.isDrawing || !this.canEdit) return;
        
        const pos = this.getMousePos(e);
        
        switch (this.currentTool) {
            case 'pen':
                this.drawPen(pos);
                break;
            case 'eraser':
                this.drawEraser(pos);
                break;
        }
        
        this.lastX = pos.x;
        this.lastY = pos.y;
        
        this.currentPath.push({
            x: pos.x,
            y: pos.y,
            tool: this.currentTool,
            color: this.currentColor,
            size: this.currentSize
        });

        // Broadcast cursor position for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastCursor", this.whiteboardId, pos.x, pos.y, this.currentTool);
        }
    }

    stopDrawing() {
        if (this.isDrawing && this.currentPath.length > 1) {
            this.paths.push([...this.currentPath]);
            this.saveCanvasData();
        }
        this.isDrawing = false;
        this.currentPath = [];
    }

    drawPen(pos) {
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentSize;
        this.ctx.beginPath();
        this.ctx.moveTo(this.lastX, this.lastY);
        this.ctx.lineTo(pos.x, pos.y);
        this.ctx.stroke();
    }

    drawEraser(pos) {
        this.ctx.strokeStyle = '#ffffff';
        this.ctx.lineWidth = this.currentSize * 2;
        this.ctx.beginPath();
        this.ctx.moveTo(this.lastX, this.lastY);
        this.ctx.lineTo(pos.x, pos.y);
        this.ctx.stroke();
    }

    // Tool methods
    setTool(tool) {
        this.currentTool = tool;
        
        // Update UI
        document.querySelectorAll('.tool-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelector(`[data-tool="${tool}"]`).classList.add('active');

        // Broadcast tool change for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastToolChange", this.whiteboardId, tool, this.currentColor, this.currentSize);
        }
    }

    setColor(color) {
        this.currentColor = color;
        
        // Update UI
        document.querySelectorAll('.color-preset').forEach(preset => {
            preset.classList.remove('active');
        });
        document.querySelector(`[data-color="${color}"]`).classList.add('active');

        // Broadcast tool change for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastToolChange", this.whiteboardId, this.currentTool, color, this.currentSize);
        }
    }

    setSize(size) {
        this.currentSize = size;
        
        // Update UI
        const sizeValue = document.getElementById('sizeValue');
        if (sizeValue) {
            sizeValue.textContent = size;
        }

        // Broadcast tool change for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastToolChange", this.whiteboardId, this.currentTool, this.currentColor, size);
        }
    }

    // Utility methods
    getMousePos(e) {
        const rect = this.canvas.getBoundingClientRect();
        return {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };
    }

    handleTouch(e) {
        e.preventDefault();
        const touch = e.touches[0];
        const mouseEvent = new MouseEvent(e.type === 'touchstart' ? 'mousedown' : 
                                        e.type === 'touchmove' ? 'mousemove' : 'mouseup', {
            clientX: touch.clientX,
            clientY: touch.clientY
        });
        this.canvas.dispatchEvent(mouseEvent);
    }

    handleKeyboard(e) {
        switch (e.key) {
            case 'Delete':
            case 'Backspace':
                if (e.ctrlKey) {
                    this.clearCanvas();
                }
                break;
            case 's':
                if (e.ctrlKey) {
                    e.preventDefault();
                    this.saveCanvasData();
                }
                break;
        }
    }

    // Data management
    saveCanvasData() {
        if (this.sessionId <= 0) return;
        
        const canvasData = JSON.stringify(this.paths);
        
        // Save to database
        fetch(`/Whiteboard/UpdateSessionData`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
                sessionId: this.sessionId,
                canvasData: canvasData
            })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                console.log('Canvas data saved successfully');
            } else {
                console.error('Failed to save canvas data:', data.message);
            }
        })
        .catch(error => {
            console.error('Error saving canvas data:', error);
        });

        // Broadcast to other users for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastDrawing", this.whiteboardId, canvasData);
        }
    }

    loadCanvasData() {
        if (this.sessionId <= 0) return;
        
        fetch(`/Whiteboard/GetSession?sessionId=${this.sessionId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success && data.data.canvasData) {
                this.paths = JSON.parse(data.data.canvasData);
                this.redrawCanvas();
            }
        })
        .catch(error => {
            console.error('Error loading canvas data:', error);
        });
    }

    redrawCanvas() {
        // Clear canvas
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Redraw all paths
        this.paths.forEach(path => {
            if (path.length < 2) return;
            
            this.ctx.beginPath();
            this.ctx.moveTo(path[0].x, path[0].y);
            
            for (let i = 1; i < path.length; i++) {
                const point = path[i];
                this.ctx.strokeStyle = point.color;
                this.ctx.lineWidth = point.size;
                this.ctx.lineTo(point.x, point.y);
            }
            
            this.ctx.stroke();
        });
    }

    clearCanvas() {
        this.paths = [];
        this.ctx.fillStyle = '#ffffff';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        this.saveCanvasData();

        // Broadcast clear canvas for collaboration
        if (this.connection && this.whiteboardId > 0) {
            this.connection.invoke("BroadcastClearCanvas", this.whiteboardId);
        }
    }

    // Export methods
    exportAsImage() {
        const link = document.createElement('a');
        link.download = 'whiteboard.png';
        link.href = this.canvas.toDataURL();
        link.click();
    }

    // Public API
    destroy() {
        if (this.autoSaveInterval) {
            clearInterval(this.autoSaveInterval);
        }
        
        // Disconnect from SignalR
        if (this.connection) {
            this.connection.invoke("LeaveWhiteboard", this.whiteboardId);
            this.connection.stop();
        }
        
        // Remove event listeners
        this.canvas.removeEventListener('mousedown', this.startDrawing);
        this.canvas.removeEventListener('mousemove', this.draw);
        this.canvas.removeEventListener('mouseup', this.stopDrawing);
        this.canvas.removeEventListener('mouseout', this.stopDrawing);
    }
}

// Global variables
let whiteboardInstance = null;

// Global functions for external access
function initWhiteboard(canvasData = [], sessionId = 0, canEdit = false, whiteboardId = 0) {
    if (whiteboardInstance) {
        whiteboardInstance.destroy();
    }
    
    whiteboardInstance = new Whiteboard('whiteboardCanvas', {
        sessionId: sessionId,
        canEdit: canEdit,
        whiteboardId: whiteboardId
    });
    
    // Load initial data if provided
    if (canvasData && canvasData.length > 0) {
        whiteboardInstance.paths = canvasData;
        whiteboardInstance.redrawCanvas();
    }
}

function clearWhiteboardCanvas() {
    if (whiteboardInstance) {
        whiteboardInstance.clearCanvas();
    }
}

function exportCanvasAsImage() {
    if (whiteboardInstance) {
        whiteboardInstance.exportAsImage();
    }
}

function loadWhiteboardSession(sessionId) {
    if (whiteboardInstance) {
        whiteboardInstance.sessionId = sessionId;
        whiteboardInstance.loadCanvasData();
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Auto-initialize if canvas exists
    const canvas = document.getElementById('whiteboardCanvas');
    if (canvas && !whiteboardInstance) {
        initWhiteboard();
    }
}); 