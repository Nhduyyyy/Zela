/**
 * RecordingsManager - Quản lý recordings và giao diện
 */
class RecordingsManager {
    constructor(userId) {
        this.userId = userId;
        this.recordings = [];
        this.filteredRecordings = [];
        this.currentPage = 1;
        this.recordingsPerPage = 12;
        this.currentFilters = {
            meeting: '',
            date: '',
            type: ''
        };
        this.currentRecording = null;
        this.selectedRecordings = new Set();
        this.searchQuery = '';
        
        // Upload related
        this.selectedFile = null;
        this.isUploading = false;
        
        this.initializeEventListeners();
    }

    // ======== INITIALIZATION ========
    initialize() {
        console.log('🎬 Initializing RecordingsManager for user:', this.userId);
        this.showLoading();
        this.loadRecordings();
    }

    initializeEventListeners() {
        // Filter events
        document.getElementById('meeting-filter').addEventListener('change', (e) => {
            this.currentFilters.meeting = e.target.value;
            this.applyFilters();
        });

        document.getElementById('date-filter').addEventListener('change', (e) => {
            this.currentFilters.date = e.target.value;
            this.applyFilters();
        });

        document.getElementById('type-filter').addEventListener('change', (e) => {
            this.currentFilters.type = e.target.value;
            this.applyFilters();
        });

        // Refresh button
        document.getElementById('refresh-recordings').addEventListener('click', () => {
            this.refreshRecordings();
        });

        // Modal events
        document.getElementById('download-recording').addEventListener('click', () => {
            this.downloadCurrentRecording();
        });

        document.getElementById('share-recording').addEventListener('click', () => {
            this.shareCurrentRecording();
        });

        document.getElementById('delete-recording').addEventListener('click', () => {
            this.deleteCurrentRecording();
        });

        document.getElementById('save-recording-changes').addEventListener('click', () => {
            this.saveRecordingChanges();
        });

        // Initialize modals
        this.initializeUploadModal();
        this.initializeBulkOperationsModal();
        this.initializeStatisticsModal();

        // Search input
        const searchInput = document.getElementById('search-input');
        searchInput.addEventListener('input', (e) => {
            this.searchQuery = e.target.value.toLowerCase();
            this.applyFilters();
        });

        // Upload recording
        document.getElementById('upload-recording').addEventListener('click', () => {
            this.showUploadModal();
        });

        // Bulk operations
        document.getElementById('bulk-delete').addEventListener('click', () => {
            this.showBulkDeleteConfirmation();
        });

        // Statistics
        document.getElementById('show-statistics').addEventListener('click', () => {
            this.showStatistics();
        });

        // Upload modal events
        this.initializeUploadModal();

        // Statistics modal events
        this.initializeStatisticsModal();

        // Bulk operations modal
        this.initializeBulkOperationsModal();
    }

    // ======== DATA LOADING ========
    async loadRecordings() {
        try {
            const response = await fetch(`/Meeting/GetRecordingHistory?userId=${this.userId}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            if (data.success) {
                this.recordings = data.recordings || [];
                this.populateMeetingFilter();
                this.applyFilters();
                console.log('📥 Loaded recordings:', this.recordings.length);
            } else {
                throw new Error(data.error || 'Failed to load recordings');
            }
            
        } catch (error) {
            console.error('Failed to load recordings:', error);
            this.showError('Không thể tải danh sách recordings: ' + error.message);
        }
    }

    async refreshRecordings() {
        console.log('🔄 Refreshing recordings...');
        this.showLoading();
        await this.loadRecordings();
    }

    // ======== FILTERING ========
    populateMeetingFilter() {
        const meetingSelect = document.getElementById('meeting-filter');
        const meetings = [...new Set(this.recordings.map(r => r.meetingCode))];
        
        // Clear existing options except first one
        meetingSelect.innerHTML = '<option value="">Tất cả cuộc họp</option>';
        
        meetings.forEach(meeting => {
            const option = document.createElement('option');
            option.value = meeting;
            option.textContent = `Meeting ${meeting}`;
            meetingSelect.appendChild(option);
        });
    }

    applyFilters() {
        this.filteredRecordings = this.recordings.filter(recording => {
            // Search filter
            if (this.searchQuery) {
                const searchableFields = [
                    recording.fileName?.toLowerCase() || '',
                    recording.originalFileName?.toLowerCase() || '',
                    recording.description?.toLowerCase() || '',
                    recording.tags?.toLowerCase() || '',
                    recording.meetingCode?.toLowerCase() || ''
                ];
                
                const matchesSearch = searchableFields.some(field => 
                    field.includes(this.searchQuery)
                );
                
                if (!matchesSearch) return false;
            }

            // Meeting filter
            if (this.currentFilters.meeting && recording.meetingCode !== this.currentFilters.meeting) {
                return false;
            }

            // Date filter
            if (this.currentFilters.date) {
                const recordingDate = new Date(recording.createdAt);
                const now = new Date();
                
                switch (this.currentFilters.date) {
                    case 'today':
                        if (!this.isSameDay(recordingDate, now)) return false;
                        break;
                    case 'week':
                        const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
                        if (recordingDate < weekAgo) return false;
                        break;
                    case 'month':
                        const monthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
                        if (recordingDate < monthAgo) return false;
                        break;
                }
            }

            // Type filter
            if (this.currentFilters.type && recording.recordingType !== this.currentFilters.type) {
                return false;
            }

            return true;
        });

        this.currentPage = 1;
        this.renderRecordings();
    }

    // ======== RENDERING ========
    renderRecordings() {
        const grid = document.getElementById('recordings-grid');
        const loading = document.getElementById('recordings-loading');
        const empty = document.getElementById('recordings-empty');
        const pagination = document.getElementById('recordings-pagination');

        // Hide loading
        loading.style.display = 'none';

        if (this.filteredRecordings.length === 0) {
            grid.style.display = 'none';
            pagination.style.display = 'none';
            empty.style.display = 'block';
            return;
        }

        // Show grid
        empty.style.display = 'none';
        grid.style.display = 'grid';

        // Calculate pagination
        const startIndex = (this.currentPage - 1) * this.recordingsPerPage;
        const endIndex = startIndex + this.recordingsPerPage;
        const pageRecordings = this.filteredRecordings.slice(startIndex, endIndex);

        // Render recording cards
        grid.innerHTML = pageRecordings.map(recording => this.createRecordingCard(recording)).join('');

        // Render pagination
        this.renderPagination();

        // Add click events to cards
        this.attachCardEvents();
    }

    createRecordingCard(recording) {
        const thumbnailUrl = recording.thumbnailUrl || (recording.recordingType === 'screenshot' ? recording.fileUrl : '/images/default-video-thumbnail.jpg');
        const duration = recording.duration ? this.formatDuration(recording.duration) : '';
        const quality = this.getQualityFromMetadata(recording.metadata);
        
        return `
            <div class="recording-card" data-recording-id="${recording.id}">
                <div class="bulk-checkbox">
                    <input type="checkbox" class="recording-checkbox" data-recording-id="${recording.id}">
                </div>
                <div class="recording-thumbnail">
                    <img src="${thumbnailUrl}" alt="Recording thumbnail" loading="lazy">
                    
                    ${recording.recordingType === 'recording' ? `<div class="recording-duration">${duration}</div>` : ''}
                    
                    <div class="recording-type-badge ${recording.recordingType}">
                        ${recording.recordingType === 'recording' ? 'Video' : 'Screenshot'}
                    </div>
                    
                    ${quality ? `<div class="quality-indicator ${quality.level}">${quality.label}</div>` : ''}
                </div>
                
                <div class="recording-content">
                    <div class="recording-title">
                        ${recording.fileName || `${recording.recordingType} - ${recording.meetingCode}`}
                    </div>
                    
                    <div class="recording-meta">
                        <div class="recording-meeting">
                            <i class="bi bi-camera-video"></i>
                            <span>Meeting ${recording.meetingCode}</span>
                        </div>
                        
                        <div class="recording-date">
                            <i class="bi bi-calendar"></i>
                            <span>${this.formatDate(recording.createdAt)}</span>
                        </div>
                        
                        <div class="recording-size">
                            <i class="bi bi-file-earmark"></i>
                            <span>${this.formatFileSize(recording.fileSize)}</span>
                        </div>
                    </div>
                    
                    ${recording.tags ? this.renderTags(recording.tags) : ''}
                    
                    <div class="recording-actions">
                        <button class="btn btn-outline-light btn-sm play-recording">
                            <i class="bi bi-play-circle"></i> Xem
                        </button>
                        
                        <div>
                            <button class="btn btn-outline-light btn-sm edit-recording" data-bs-toggle="tooltip" title="Chỉnh sửa">
                                <i class="bi bi-pencil"></i>
                            </button>
                            
                            <button class="btn btn-outline-danger btn-sm delete-recording" data-bs-toggle="tooltip" title="Xóa">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    renderTags(tagsString) {
        if (!tagsString) return '';
        
        const tags = tagsString.split(',').map(tag => tag.trim()).filter(tag => tag);
        if (tags.length === 0) return '';
        
        return `
            <div class="recording-tags">
                ${tags.map(tag => `<span class="recording-tag">${tag}</span>`).join('')}
            </div>
        `;
    }

    // ======== EVENT HANDLING ========
    attachCardEvents() {
        // Checkbox events for bulk selection
        document.querySelectorAll('.recording-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => {
                e.stopPropagation();
                this.handleCheckboxChange(checkbox);
            });
        });

        // Play recording events
        document.querySelectorAll('.play-recording').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const card = e.target.closest('.recording-card');
                const recordingId = card.dataset.recordingId;
                this.playRecording(recordingId);
            });
        });

        // Edit recording events
        document.querySelectorAll('.edit-recording').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const card = e.target.closest('.recording-card');
                const recordingId = card.dataset.recordingId;
                this.editRecording(recordingId);
            });
        });

        // Delete recording events
        document.querySelectorAll('.delete-recording').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const card = e.target.closest('.recording-card');
                const recordingId = card.dataset.recordingId;
                this.deleteRecording(recordingId);
            });
        });

        // Card click events (play) - avoid checkbox area
        document.querySelectorAll('.recording-card').forEach(card => {
            card.addEventListener('click', (e) => {
                // Don't trigger if clicking on checkbox area
                if (e.target.closest('.bulk-checkbox')) return;
                
                const recordingId = card.dataset.recordingId;
                this.playRecording(recordingId);
            });
        });
    }

    // ======== RECORDING ACTIONS ========
    playRecording(recordingId) {
        const recording = this.recordings.find(r => r.id === recordingId);
        if (!recording) return;

        this.currentRecording = recording;

        // Update access time
        this.updateLastAccessed(recordingId);

        if (recording.recordingType === 'screenshot') {
            // Open screenshot in new tab
            window.open(recording.fileUrl, '_blank');
        } else {
            // Open video in modal
            this.showVideoModal(recording);
        }
    }

    showVideoModal(recording) {
        const modal = new bootstrap.Modal(document.getElementById('recordingDetailModal'));
        const player = document.getElementById('recording-player');
        const details = document.getElementById('recording-details');

        // Set video source
        player.src = recording.fileUrl;

        // Populate recording details
        this.populateRecordingDetails(recording, details);

        modal.show();
    }

    populateRecordingDetails(recording, container) {
        const metadata = recording.metadata ? JSON.parse(recording.metadata) : {};
        
        const details = [
            { label: 'Tên file', value: recording.fileName, icon: 'bi-file-earmark-text' },
            { label: 'Meeting Code', value: recording.meetingCode, icon: 'bi-key' },
            { label: 'Kích thước', value: this.formatFileSize(recording.fileSize), icon: 'bi-hdd' },
            { label: 'Thời lượng', value: recording.duration ? this.formatDuration(recording.duration) : 'N/A', icon: 'bi-clock' },
            { label: 'Ngày tạo', value: this.formatDate(recording.createdAt), icon: 'bi-calendar-event' },
            { label: 'Lượt tải', value: recording.downloadCount || 0, icon: 'bi-download' }
        ];

        if (metadata.resolution) {
            details.push({ label: 'Độ phân giải', value: metadata.resolution, icon: 'bi-display' });
        }

        if (metadata.quality) {
            details.push({ label: 'Chất lượng', value: metadata.quality, icon: 'bi-badge-hd' });
        }

        container.innerHTML = details.map((item, index) => {
            let valueClass = '';
            if (item.label === 'Kích thước') valueClass = 'file-size';
            else if (item.label === 'Thời lượng') valueClass = 'duration';
            else if (item.label === 'Lượt tải') valueClass = 'downloads';
            
            return `
                <div class="recording-details-item">
                    <span class="label">
                        <i class="bi ${item.icon}"></i>
                        ${item.label}:
                    </span>
                    <span class="value ${valueClass}">${item.value}</span>
                </div>
            `;
        }).join('');
    }

    async downloadCurrentRecording() {
        if (!this.currentRecording) return;
        
        try {
            // Try to use our backend endpoint for forced download instead
            const downloadUrl = `/Meeting/DownloadRecording?recordingId=${this.currentRecording.id}`;
            
            // Start monitoring downloads automatically
            this.startDownloadMonitoring(this.currentRecording.id);
            
            // Create download using our backend endpoint
            const downloadLink = document.createElement('a');
            downloadLink.href = downloadUrl;
            downloadLink.download = this.currentRecording.fileName || 'recording';
            
            // Force download without opening new tab
            document.body.appendChild(downloadLink);
            downloadLink.click();
            document.body.removeChild(downloadLink);
            
            this.showNotification('Bắt đầu tải xuống...', 'success');
            
        } catch (error) {
            console.error('Download failed:', error);
            this.showNotification('Lỗi tải xuống: ' + error.message, 'error');
        }
    }

    startDownloadMonitoring(recordingId) {
        // Track download optimistically
        this.trackDownload(recordingId).catch(err => console.warn('Failed to track download:', err));
        
        // Monitor page visibility and focus to detect if download started
        let downloadDetected = false;
        let timeoutId = null;
        
        const cleanup = () => {
            if (timeoutId) clearTimeout(timeoutId);
            document.removeEventListener('visibilitychange', visibilityHandler);
            window.removeEventListener('focus', focusHandler);
            window.removeEventListener('blur', blurHandler);
        };
        
        const detectSuccess = () => {
            if (!downloadDetected) {
                downloadDetected = true;
                cleanup();
                this.showNotification('Tải xuống thành công!', 'success');
            }
        };
        
        const detectFailure = () => {
            if (!downloadDetected) {
                downloadDetected = true;
                cleanup();
                this.rollbackDownload(recordingId).catch(err => console.warn('Failed to rollback:', err));
                this.showNotification('Tải xuống thất bại - đã điều chỉnh thống kê', 'warning');
            }
        };
        
        // Monitor visibility changes (user might switch to Downloads folder)
        const visibilityHandler = () => {
            if (document.hidden) {
                // User might be checking downloads - assume success
                setTimeout(detectSuccess, 2000);
            }
        };
        
        // Monitor window focus (download dialog might steal focus)
        const blurHandler = () => {
            // Window lost focus - might be download dialog
            setTimeout(() => {
                if (document.hasFocus()) {
                    detectSuccess();
                }
            }, 1000);
        };
        
        const focusHandler = () => {
            // Window regained focus after download dialog
            setTimeout(detectSuccess, 500);
        };
        
        document.addEventListener('visibilitychange', visibilityHandler);
        window.addEventListener('focus', focusHandler);
        window.addEventListener('blur', blurHandler);
        
        // Fallback: assume failure if no indicators after 15 seconds
        timeoutId = setTimeout(detectFailure, 15000);
    }



    async rollbackDownload(recordingId) {
        try {
            await fetch('/Meeting/RollbackDownload', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ recordingId })
            });
        } catch (error) {
            console.warn('Failed to rollback download:', error);
        }
    }

    shareCurrentRecording() {
        if (!this.currentRecording) return;

        const url = window.location.origin + '/Meeting/ViewRecording/' + this.currentRecording.id;
        
        if (navigator.share) {
            navigator.share({
                title: this.currentRecording.fileName,
                text: 'Xem recording từ cuộc họp',
                url: url
            });
        } else {
            // Fallback - copy to clipboard
            navigator.clipboard.writeText(url).then(() => {
                this.showNotification('Đã copy link chia sẻ!', 'success');
            });
        }
    }

    deleteCurrentRecording() {
        if (!this.currentRecording) return;
        
        // Hide modal first
        bootstrap.Modal.getInstance(document.getElementById('recordingDetailModal')).hide();
        
        // Delete recording
        this.deleteRecording(this.currentRecording.id);
    }

    async deleteRecording(recordingId) {
        const recording = this.recordings.find(r => r.id === recordingId);
        if (!recording) return;

        const confirmed = confirm(`Bạn có chắc chắn muốn xóa recording "${recording.fileName}"?`);
        if (!confirmed) return;

        try {
            const response = await fetch(`/Meeting/DeleteRecording?recordingId=${recordingId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            
            if (result.success) {
                // Remove from local data
                this.recordings = this.recordings.filter(r => r.id !== recordingId);
                this.applyFilters();
                
                this.showNotification('Xóa recording thành công!', 'success');
            } else {
                throw new Error(result.error || 'Failed to delete recording');
            }

        } catch (error) {
            console.error('Failed to delete recording:', error);
            this.showNotification('Không thể xóa recording: ' + error.message, 'error');
        }
    }

    // ======== UTILITY FUNCTIONS ========
    async updateLastAccessed(recordingId) {
        try {
            await fetch('/Meeting/UpdateLastAccessed', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ recordingId })
            });
        } catch (error) {
            console.warn('Failed to update last accessed:', error);
        }
    }

    async trackDownload(recordingId) {
        try {
            await fetch('/Meeting/TrackDownload', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ recordingId })
            });
        } catch (error) {
            console.warn('Failed to track download:', error);
        }
    }

    getQualityFromMetadata(metadataString) {
        try {
            if (!metadataString) return null;
            
            const metadata = JSON.parse(metadataString);
            const quality = metadata.quality;
            
            if (!quality) return null;
            
            const levels = {
                'High': { level: 'high', label: 'HD' },
                'Medium': { level: 'medium', label: 'SD' },
                'Low': { level: 'low', label: 'LD' },
                'Minimal': { level: 'low', label: 'LD' }
            };
            
            return levels[quality] || null;
        } catch {
            return null;
        }
    }

    formatDuration(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        
        if (hours > 0) {
            return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        } else {
            return `${minutes}:${secs.toString().padStart(2, '0')}`;
        }
    }

    formatDate(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diffTime = Math.abs(now - date);
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        
        if (diffDays === 1) {
            return 'Hôm nay';
        } else if (diffDays === 2) {
            return 'Hôm qua';
        } else if (diffDays <= 7) {
            return `${diffDays - 1} ngày trước`;
        } else {
            return date.toLocaleDateString('vi-VN');
        }
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    isSameDay(date1, date2) {
        return date1.getDate() === date2.getDate() &&
               date1.getMonth() === date2.getMonth() &&
               date1.getFullYear() === date2.getFullYear();
    }

    // ======== UI STATE MANAGEMENT ========
    showLoading() {
        document.getElementById('recordings-loading').style.display = 'block';
        document.getElementById('recordings-grid').style.display = 'none';
        document.getElementById('recordings-empty').style.display = 'none';
        document.getElementById('recordings-pagination').style.display = 'none';
    }

    showError(message) {
        document.getElementById('recordings-loading').style.display = 'none';
        document.getElementById('recordings-grid').style.display = 'none';
        document.getElementById('recordings-pagination').style.display = 'none';
        
        const empty = document.getElementById('recordings-empty');
        empty.style.display = 'block';
        empty.innerHTML = `
            <i class="bi bi-exclamation-triangle display-1 text-warning"></i>
            <h3>Có lỗi xảy ra</h3>
            <p class="text-muted">${message}</p>
            <button class="btn btn-primary" onclick="window.recordingsManager.refreshRecordings()">
                <i class="bi bi-arrow-clockwise"></i> Thử lại
            </button>
        `;
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'} alert-dismissible fade show`;
        notification.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999; max-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto remove after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 3000);
    }

    renderPagination() {
        const pagination = document.getElementById('recordings-pagination');
        const totalPages = Math.ceil(this.filteredRecordings.length / this.recordingsPerPage);
        
        if (totalPages <= 1) {
            pagination.style.display = 'none';
            return;
        }
        
        pagination.style.display = 'block';
        
        const paginationHtml = `
            <nav>
                <ul class="pagination">
                    <li class="page-item ${this.currentPage === 1 ? 'disabled' : ''}">
                        <a class="page-link" href="#" data-page="${this.currentPage - 1}">
                            <i class="bi bi-chevron-left"></i>
                        </a>
                    </li>
                    
                    ${this.generatePageNumbers(totalPages)}
                    
                    <li class="page-item ${this.currentPage === totalPages ? 'disabled' : ''}">
                        <a class="page-link" href="#" data-page="${this.currentPage + 1}">
                            <i class="bi bi-chevron-right"></i>
                        </a>
                    </li>
                </ul>
            </nav>
        `;
        
        pagination.innerHTML = paginationHtml;
        
        // Add pagination click events
        pagination.querySelectorAll('.page-link').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.target.closest('.page-link').dataset.page);
                if (page && page !== this.currentPage) {
                    this.currentPage = page;
                    this.renderRecordings();
                }
            });
        });
    }

    generatePageNumbers(totalPages) {
        let pages = [];
        const current = this.currentPage;
        
        // Always show first page
        if (current > 3) {
            pages.push(`<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>`);
            if (current > 4) {
                pages.push(`<li class="page-item disabled"><span class="page-link">...</span></li>`);
            }
        }
        
        // Show pages around current
        for (let i = Math.max(1, current - 2); i <= Math.min(totalPages, current + 2); i++) {
            pages.push(`
                <li class="page-item ${i === current ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `);
        }
        
        // Always show last page
        if (current < totalPages - 2) {
            if (current < totalPages - 3) {
                pages.push(`<li class="page-item disabled"><span class="page-link">...</span></li>`);
            }
            pages.push(`<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a></li>`);
        }
        
        return pages.join('');
    }

    editRecording(recordingId) {
        // Find recording and populate edit modal
        const recording = this.recordings.find(r => r.id === recordingId);
        if (!recording) return;

        // Populate edit form
        document.getElementById('recording-description').value = recording.description || '';
        document.getElementById('recording-tags').value = recording.tags || '';

        // Store current recording id
        this.currentRecording = recording;

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('editRecordingModal'));
        modal.show();
    }

    async saveRecordingChanges() {
        if (!this.currentRecording) return;
        
        const description = document.getElementById('recording-description').value.trim();
        const tags = document.getElementById('recording-tags').value.trim();
        
        try {
            const response = await fetch('/Meeting/UpdateRecording', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    recordingId: this.currentRecording.id,
                    description: description,
                    tags: tags
                })
            });
            
            const result = await response.json();
            if (result.success) {
                this.showNotification('Cập nhật thành công!', 'success');
                bootstrap.Modal.getInstance(document.getElementById('editRecordingModal')).hide();
                this.refreshRecordings();
            } else {
                throw new Error(result.error || 'Update failed');
            }
        } catch (error) {
            console.error('Error updating recording:', error);
            this.showNotification('Lỗi cập nhật: ' + error.message, 'error');
        }
    }

    // ======== BULK OPERATIONS ========
    handleCheckboxChange(checkbox) {
        const recordingId = checkbox.dataset.recordingId;
        const card = checkbox.closest('.recording-card');
        
        if (checkbox.checked) {
            this.selectedRecordings.add(recordingId);
            card.classList.add('selected');
        } else {
            this.selectedRecordings.delete(recordingId);
            card.classList.remove('selected');
        }
        
        this.updateBulkActionsUI();
    }

    updateBulkActionsUI() {
        const bulkDeleteBtn = document.getElementById('bulk-delete');
        const selectedCount = document.getElementById('selected-count');
        
        if (this.selectedRecordings.size > 0) {
            bulkDeleteBtn.style.display = 'inline-block';
            selectedCount.textContent = this.selectedRecordings.size;
        } else {
            bulkDeleteBtn.style.display = 'none';
        }
    }

    showBulkDeleteConfirmation() {
        if (this.selectedRecordings.size === 0) return;
        
        document.getElementById('bulk-count').textContent = this.selectedRecordings.size;
        const modal = new bootstrap.Modal(document.getElementById('bulkOperationModal'));
        modal.show();
    }

    async executeBulkDelete() {
        const recordingIds = Array.from(this.selectedRecordings);
        let successCount = 0;
        
        for (const recordingId of recordingIds) {
            try {
                const response = await fetch(`/Meeting/DeleteRecording/${recordingId}`, {
                    method: 'DELETE'
                });
                
                const result = await response.json();
                if (result.success) {
                    successCount++;
                    this.selectedRecordings.delete(recordingId);
                }
            } catch (error) {
                console.error('Error deleting recording:', recordingId, error);
            }
        }
        
        this.showNotification(`Đã xóa ${successCount} recordings`, 'success');
        this.selectedRecordings.clear();
        this.updateBulkActionsUI();
        this.refreshRecordings();
    }

    initializeBulkOperationsModal() {
        document.getElementById('confirm-bulk-delete').addEventListener('click', () => {
            bootstrap.Modal.getInstance(document.getElementById('bulkOperationModal')).hide();
            this.executeBulkDelete();
        });
    }

    // ======== UPLOAD FUNCTIONALITY ========
    initializeUploadModal() {
        const dropZone = document.getElementById('upload-drop-zone');
        const fileInput = document.getElementById('upload-file-input');
        const startUploadBtn = document.getElementById('start-upload');

        if (!dropZone || !fileInput || !startUploadBtn) return;

        // Click to select file
        dropZone.addEventListener('click', () => {
            fileInput.click();
        });

        // Drag and drop
        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('dragover');
        });

        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('dragover');
        });

        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('dragover');
            
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                this.handleFileSelection(files[0]);
            }
        });

        // File input change
        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                this.handleFileSelection(e.target.files[0]);
            }
        });

        // Start upload
        startUploadBtn.addEventListener('click', () => {
            this.startUpload();
        });

        // Remove file
        document.addEventListener('click', (e) => {
            if (e.target.closest('.remove-file')) {
                this.clearSelectedFile();
            }
        });
    }

    showUploadModal() {
        this.clearSelectedFile();
        const modal = new bootstrap.Modal(document.getElementById('uploadRecordingModal'));
        modal.show();
    }

    handleFileSelection(file) {
        this.selectedFile = file;
        
        // Update UI
        const fileInfo = document.getElementById('selected-file-info');
        const fileName = fileInfo.querySelector('.file-name');
        const fileMeta = fileInfo.querySelector('.file-meta');
        
        fileName.textContent = file.name;
        fileMeta.textContent = `${this.formatFileSize(file.size)} • ${file.type}`;
        
        fileInfo.style.display = 'block';
        
        // Auto-detect file type
        const typeSelect = document.getElementById('upload-type');
        if (file.type.startsWith('video/')) {
            typeSelect.value = 'recording';
        } else if (file.type.startsWith('image/')) {
            typeSelect.value = 'screenshot';
        }
    }

    clearSelectedFile() {
        this.selectedFile = null;
        document.getElementById('selected-file-info').style.display = 'none';
        document.getElementById('upload-file-input').value = '';
        document.getElementById('upload-progress').style.display = 'none';
    }

    async startUpload() {
        if (!this.selectedFile || this.isUploading) return;
        
        const meetingCode = document.getElementById('upload-meeting-code').value.trim();
        const type = document.getElementById('upload-type').value;
        const description = document.getElementById('upload-description').value.trim();
        const tags = document.getElementById('upload-tags').value.trim();
        
        if (!meetingCode) {
            this.showNotification('Vui lòng nhập mã cuộc họp', 'error');
            return;
        }
        
        this.isUploading = true;
        
        // Show progress
        const progressContainer = document.getElementById('upload-progress');
        const progressBar = progressContainer.querySelector('.progress-bar');
        const progressText = progressContainer.querySelector('.upload-percentage');
        progressContainer.style.display = 'block';
        
        try {
            const formData = new FormData();
            formData.append('file', this.selectedFile);
            formData.append('type', type);
            formData.append('meetingCode', meetingCode);
            
            if (description || tags) {
                const metadata = { description, tags };
                formData.append('metadata', JSON.stringify(metadata));
            }
            
            const xhr = new XMLHttpRequest();
            
            // Upload progress
            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    const percentComplete = (e.loaded / e.total) * 100;
                    progressBar.style.width = percentComplete + '%';
                    progressText.textContent = Math.round(percentComplete) + '%';
                }
            });
            
            xhr.addEventListener('load', () => {
                if (xhr.status === 200) {
                    const result = JSON.parse(xhr.responseText);
                    if (result.success) {
                        this.showNotification('Upload thành công!', 'success');
                        bootstrap.Modal.getInstance(document.getElementById('uploadRecordingModal')).hide();
                        this.refreshRecordings();
                    } else {
                        throw new Error(result.error || 'Upload failed');
                    }
                } else {
                    throw new Error('Upload failed');
                }
                this.isUploading = false;
            });
            
            xhr.addEventListener('error', () => {
                this.showNotification('Lỗi upload', 'error');
                this.isUploading = false;
            });
            
            xhr.open('POST', '/Meeting/UploadRecording');
            xhr.send(formData);
            
        } catch (error) {
            console.error('Upload error:', error);
            this.showNotification('Lỗi upload: ' + error.message, 'error');
            this.isUploading = false;
        }
    }

    // ======== ADVANCED STATISTICS ========
    initializeStatisticsModal() {
        // Time range selector
        this.currentTimeRange = 'today';
        this.customDateRange = { start: null, end: null };
        
        // Time range buttons
        document.querySelectorAll('.time-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.time-btn').forEach(b => b.classList.remove('active'));
                e.target.classList.add('active');
                this.currentTimeRange = e.target.dataset.range;
                this.refreshStatistics();
            });
        });
        
        // Custom date range
        document.getElementById('apply-custom-range').addEventListener('click', () => {
            const startDate = document.getElementById('start-date').value;
            const endDate = document.getElementById('end-date').value;
            
            if (startDate && endDate) {
                this.customDateRange = { start: new Date(startDate), end: new Date(endDate) };
                this.currentTimeRange = 'custom';
                document.querySelectorAll('.time-btn').forEach(b => b.classList.remove('active'));
                this.refreshStatistics();
            }
        });
        
        // Timeline view selector
        const timelineSelect = document.getElementById('timeline-view');
        if (timelineSelect) {
            timelineSelect.addEventListener('change', () => {
                this.refreshStatistics();
            });
        }
        
        // Initialize cleanup and compress buttons
        const cleanupBtn = document.getElementById('cleanup-old-files');
        const compressBtn = document.getElementById('compress-recordings');
        
        if (cleanupBtn) {
            cleanupBtn.addEventListener('click', () => {
                this.cleanupOldFiles();
            });
        }
        
        if (compressBtn) {
            compressBtn.addEventListener('click', () => {
                this.compressRecordings();
            });
        }
    }

    async showStatistics() {
        const modal = new bootstrap.Modal(document.getElementById('statisticsModal'));
        modal.show();
        
        // Show loading
        document.getElementById('stats-loading').style.display = 'block';
        document.getElementById('stats-content').style.display = 'none';
        
        try {
            await this.loadStatistics();
        } catch (error) {
            console.error('Error loading statistics:', error);
            this.showNotification('Không thể tải thống kê', 'error');
        }
    }

    async refreshStatistics() {
        if (document.getElementById('stats-content').style.display !== 'none') {
            await this.loadStatistics();
        }
    }

    async loadStatistics() {
        // Filter recordings based on time range
        const filteredRecordings = this.getFilteredRecordings();
        
        // Calculate statistics
        const stats = this.calculateAdvancedStatistics(filteredRecordings);
        
        // Update metric cards with animations
        this.updateMetricCards(stats);
        
        // Create advanced charts
        this.createAdvancedTimelineChart(stats.timelineData);
        this.createEnhancedFileTypeChart(stats.fileTypeData);
        this.createQualityDistributionChart(stats.qualityData);
        this.createTopMeetingsChart(stats.meetingData);
        this.createActivityHeatmap(stats.activityData);
        
        // Generate smart insights
        this.generateSmartInsights(stats);
        
        // Update storage breakdown
        this.updateStorageBreakdown(stats);
        
        // Show content with animation
        setTimeout(() => {
            document.getElementById('stats-loading').style.display = 'none';
            document.getElementById('stats-content').style.display = 'block';
            this.animateMetrics();
        }, 1000);
    }

    getFilteredRecordings() {
        const now = new Date();
        let startDate, endDate;
        
        switch (this.currentTimeRange) {
            case 'today':
                startDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
                endDate = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
                break;
            case 'week':
                const startOfWeek = new Date(now);
                startOfWeek.setDate(now.getDate() - now.getDay());
                startDate = new Date(startOfWeek.getFullYear(), startOfWeek.getMonth(), startOfWeek.getDate());
                endDate = new Date(startDate.getTime() + 7 * 24 * 60 * 60 * 1000);
                break;
            case 'month':
                startDate = new Date(now.getFullYear(), now.getMonth(), 1);
                endDate = new Date(now.getFullYear(), now.getMonth() + 1, 1);
                break;
            case 'quarter':
                const quarter = Math.floor(now.getMonth() / 3);
                startDate = new Date(now.getFullYear(), quarter * 3, 1);
                endDate = new Date(now.getFullYear(), (quarter + 1) * 3, 1);
                break;
            case 'year':
                startDate = new Date(now.getFullYear(), 0, 1);
                endDate = new Date(now.getFullYear() + 1, 0, 1);
                break;
            case 'custom':
                startDate = this.customDateRange.start;
                endDate = this.customDateRange.end;
                break;
            default:
                return this.recordings;
        }
        
        return this.recordings.filter(recording => {
            const recordingDate = new Date(recording.createdAt);
            return recordingDate >= startDate && recordingDate < endDate;
        });
    }

    calculateAdvancedStatistics(recordings) {
        const stats = {
            totalRecordings: recordings.length,
            totalStorage: 0,
            totalDuration: 0,
            totalDownloads: 0,
            timelineData: {},
            fileTypeData: { recording: 0, screenshot: 0 },
            qualityData: { high: 0, medium: 0, low: 0 },
            meetingData: {},
            activityData: {},
            peakDay: null,
            avgRecordings: 0,
            storageGrowth: 0,
            downloadTrend: []
        };
        
        const timelineView = document.getElementById('timeline-view')?.value || 'monthly';
        
        recordings.forEach(recording => {
            stats.totalStorage += recording.fileSize || 0;
            stats.totalDuration += recording.duration || 0;
            stats.totalDownloads += recording.downloadCount || 0;
            
            const date = new Date(recording.createdAt);
            
            // Timeline data based on view
            let timeKey;
            switch (timelineView) {
                case 'daily':
                    timeKey = date.toISOString().slice(0, 10);
                    break;
                case 'weekly':
                    const weekStart = new Date(date);
                    weekStart.setDate(date.getDate() - date.getDay());
                    timeKey = weekStart.toISOString().slice(0, 10);
                    break;
                case 'yearly':
                    timeKey = date.getFullYear().toString();
                    break;
                default: // monthly
                    timeKey = date.toISOString().slice(0, 7);
            }
            
            stats.timelineData[timeKey] = (stats.timelineData[timeKey] || 0) + 1;
            
            // File type data
            const type = recording.recordingType || 'recording';
            stats.fileTypeData[type] = (stats.fileTypeData[type] || 0) + 1;
            
            // Quality data
            const metadata = recording.metadata ? JSON.parse(recording.metadata) : {};
            const quality = metadata.quality || 'medium';
            stats.qualityData[quality] = (stats.qualityData[quality] || 0) + 1;
            
            // Meeting data
            const meetingCode = recording.meetingCode;
            if (!stats.meetingData[meetingCode]) {
                stats.meetingData[meetingCode] = {
                    count: 0,
                    totalDuration: 0,
                    totalSize: 0
                };
            }
            stats.meetingData[meetingCode].count++;
            stats.meetingData[meetingCode].totalDuration += recording.duration || 0;
            stats.meetingData[meetingCode].totalSize += recording.fileSize || 0;
            
            // Activity data (hour of day)
            const hour = date.getHours();
            const day = date.getDay();
            const activityKey = `${day}-${hour}`;
            stats.activityData[activityKey] = (stats.activityData[activityKey] || 0) + 1;
        });
        
        // Calculate derived statistics
        const timelineCounts = Object.values(stats.timelineData);
        if (timelineCounts.length > 0) {
            stats.avgRecordings = timelineCounts.reduce((a, b) => a + b, 0) / timelineCounts.length;
            const maxCount = Math.max(...timelineCounts);
            stats.peakDay = Object.keys(stats.timelineData).find(key => stats.timelineData[key] === maxCount);
        }
        
        return stats;
    }

    updateMetricCards(stats) {
        // Update values
        document.getElementById('total-recordings').textContent = stats.totalRecordings;
        document.getElementById('total-storage').textContent = this.formatFileSize(stats.totalStorage);
        document.getElementById('total-duration').textContent = this.formatDuration(stats.totalDuration);
        document.getElementById('total-downloads').textContent = stats.totalDownloads;
        
        // Update progress ring for storage
        const storageLimit = 5 * 1024 * 1024 * 1024; // 5GB
        const storagePercent = Math.min((stats.totalStorage / storageLimit) * 100, 100);
        const progressRing = document.getElementById('storage-progress-ring');
        const progressPercent = document.getElementById('storage-percent');
        
        if (progressRing && progressPercent) {
            const offset = 163 - (163 * storagePercent) / 100;
            progressRing.style.strokeDashoffset = offset;
            progressPercent.textContent = Math.round(storagePercent) + '%';
        }
        
        // Update change indicators (placeholder - you can implement comparison with previous period)
        this.updateChangeIndicators(stats);
        
        // Update visual elements
        this.updateDurationBars(stats);
        this.updateDownloadTrend(stats);
    }

    updateChangeIndicators(stats) {
        // Placeholder implementation - in real app, compare with previous period
        const changes = [
            { id: 'recordings-change', value: '+12%', type: 'positive' },
            { id: 'storage-change', value: '+8%', type: 'positive' },
            { id: 'duration-change', value: '+5%', type: 'positive' },
            { id: 'downloads-change', value: '+15%', type: 'positive' }
        ];
        
        changes.forEach(change => {
            const element = document.getElementById(change.id);
            if (element) {
                element.textContent = change.value;
                element.className = `metric-change ${change.type}`;
            }
        });
    }

    updateDurationBars(stats) {
        const barsContainer = document.getElementById('duration-bars');
        if (!barsContainer) return;
        
        const bars = barsContainer.querySelectorAll('.bar');
        const maxHeight = 40;
        
        // Generate random heights for demo - in real app, use actual data
        bars.forEach((bar, index) => {
            const height = Math.random() * maxHeight;
            bar.style.setProperty('--bar-height', height + 'px');
        });
    }

    updateDownloadTrend(stats) {
        const trendContainer = document.getElementById('download-trend');
        if (!trendContainer) return;
        
        // Create mini line chart using CSS
        const trendHTML = `
            <svg width="80" height="40" style="overflow: visible;">
                <polyline fill="none" stroke="#667eea" stroke-width="2" 
                          points="0,30 20,25 40,20 60,15 80,10"/>
                <circle cx="80" cy="10" r="3" fill="#667eea"/>
            </svg>
        `;
        trendContainer.innerHTML = trendHTML;
    }

    animateMetrics() {
        // Add staggered animation to metric cards
        const metricCards = document.querySelectorAll('.metric-card');
        metricCards.forEach((card, index) => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';
            
            setTimeout(() => {
                card.style.transition = 'all 0.6s ease';
                card.style.opacity = '1';
                card.style.transform = 'translateY(0)';
            }, index * 200);
        });
    }

    createAdvancedTimelineChart(timelineData) {
        const canvas = document.getElementById('recordings-timeline-chart');
        if (!canvas) return;
        
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        const entries = Object.entries(timelineData).sort();
        if (entries.length === 0) {
            this.drawNoDataMessage(ctx, canvas);
            return;
        }
        
        const values = entries.map(([_, value]) => value);
        const maxValue = Math.max(...values) || 1;
        
        const padding = 60;
        const chartWidth = canvas.width - 2 * padding;
        const chartHeight = canvas.height - 2 * padding;
        const barWidth = Math.max(20, chartWidth / entries.length - 10);
        
        // Draw gradient background
        const gradient = ctx.createLinearGradient(0, padding, 0, canvas.height - padding);
        gradient.addColorStop(0, 'rgba(102, 126, 234, 0.1)');
        gradient.addColorStop(1, 'rgba(118, 75, 162, 0.1)');
        ctx.fillStyle = gradient;
        ctx.fillRect(padding, padding, chartWidth, chartHeight);
        
        // Draw bars with gradient
        entries.forEach(([label, value], index) => {
            const barHeight = (value / maxValue) * chartHeight;
            const x = padding + index * (barWidth + 10);
            const y = canvas.height - padding - barHeight;
            
            // Create bar gradient
            const barGradient = ctx.createLinearGradient(0, y, 0, y + barHeight);
            barGradient.addColorStop(0, '#667eea');
            barGradient.addColorStop(1, '#764ba2');
            
            // Draw bar with rounded corners
            ctx.fillStyle = barGradient;
            this.drawRoundedRect(ctx, x, y, barWidth, barHeight, 4);
            
            // Draw value on top of bar
            ctx.fillStyle = '#333';
            ctx.font = 'bold 12px Arial';
            ctx.textAlign = 'center';
            ctx.fillText(value.toString(), x + barWidth / 2, y - 5);
            
            // Draw label
            ctx.fillStyle = '#666';
            ctx.font = '10px Arial';
            ctx.save();
            ctx.translate(x + barWidth / 2, canvas.height - padding + 15);
            ctx.rotate(-Math.PI / 4);
            ctx.fillText(label, 0, 0);
            ctx.restore();
        });
        
        // Update insights
        this.updateTimelineInsights(timelineData);
    }

    drawRoundedRect(ctx, x, y, width, height, radius) {
        ctx.beginPath();
        ctx.moveTo(x + radius, y);
        ctx.lineTo(x + width - radius, y);
        ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
        ctx.lineTo(x + width, y + height);
        ctx.lineTo(x, y + height);
        ctx.lineTo(x, y + radius);
        ctx.quadraticCurveTo(x, y, x + radius, y);
        ctx.closePath();
        ctx.fill();
    }

    updateTimelineInsights(timelineData) {
        const entries = Object.entries(timelineData);
        if (entries.length === 0) return;
        
        const values = entries.map(([_, value]) => value);
        const maxValue = Math.max(...values);
        const avgValue = values.reduce((a, b) => a + b, 0) / values.length;
        const peakLabel = entries.find(([_, value]) => value === maxValue)?.[0];
        
        const peakElement = document.getElementById('peak-day');
        const avgElement = document.getElementById('avg-recordings');
        
        if (peakElement) peakElement.textContent = peakLabel || '-';
        if (avgElement) avgElement.textContent = avgValue.toFixed(1);
    }

    createEnhancedFileTypeChart(fileTypeData) {
        const canvas = document.getElementById('file-type-chart');
        if (!canvas) return;
        
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        const total = Object.values(fileTypeData).reduce((a, b) => a + b, 0);
        if (total === 0) {
            this.drawNoDataMessage(ctx, canvas);
            return;
        }
        
        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2 - 20;
        const radius = Math.min(centerX, centerY) - 40;
        
        const colors = {
            recording: '#667eea',
            screenshot: '#28a745'
        };
        
        let currentAngle = -Math.PI / 2; // Start from top
        const legendItems = [];
        
        Object.entries(fileTypeData).forEach(([type, count]) => {
            if (count === 0) return;
            
            const sliceAngle = (count / total) * 2 * Math.PI;
            
            // Create gradient for slice
            const gradient = ctx.createRadialGradient(centerX, centerY, 0, centerX, centerY, radius);
            gradient.addColorStop(0, colors[type]);
            gradient.addColorStop(1, this.darkenColor(colors[type], 0.3));
            
            // Draw slice
            ctx.beginPath();
            ctx.moveTo(centerX, centerY);
            ctx.arc(centerX, centerY, radius, currentAngle, currentAngle + sliceAngle);
            ctx.closePath();
            ctx.fillStyle = gradient;
            ctx.fill();
            
            // Draw border
            ctx.strokeStyle = '#fff';
            ctx.lineWidth = 3;
            ctx.stroke();
            
            // Add to legend
            legendItems.push({
                color: colors[type],
                label: type === 'recording' ? 'Video' : 'Screenshot',
                count: count,
                percentage: ((count / total) * 100).toFixed(1)
            });
            
            currentAngle += sliceAngle;
        });
        
        // Draw legend
        this.drawChartLegend('file-type-legend', legendItems);
    }

    createQualityDistributionChart(qualityData) {
        const canvas = document.getElementById('quality-distribution-chart');
        if (!canvas) return;
        
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        const total = Object.values(qualityData).reduce((a, b) => a + b, 0);
        if (total === 0) {
            this.drawNoDataMessage(ctx, canvas);
            return;
        }
        
        // Create donut chart
        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2 - 20;
        const outerRadius = Math.min(centerX, centerY) - 40;
        const innerRadius = outerRadius * 0.6;
        
        const colors = {
            high: '#28a745',
            medium: '#ffc107',
            low: '#dc3545'
        };
        
        let currentAngle = -Math.PI / 2;
        
        Object.entries(qualityData).forEach(([quality, count]) => {
            if (count === 0) return;
            
            const sliceAngle = (count / total) * 2 * Math.PI;
            
            // Draw outer arc
            ctx.beginPath();
            ctx.arc(centerX, centerY, outerRadius, currentAngle, currentAngle + sliceAngle);
            ctx.arc(centerX, centerY, innerRadius, currentAngle + sliceAngle, currentAngle, true);
            ctx.closePath();
            ctx.fillStyle = colors[quality];
            ctx.fill();
            
            currentAngle += sliceAngle;
        });
        
        // Draw center text
        ctx.fillStyle = '#333';
        ctx.font = 'bold 16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Quality', centerX, centerY - 5);
        ctx.font = '12px Arial';
        ctx.fillText('Distribution', centerX, centerY + 10);
        
        // Update quality stats
        this.updateQualityStats(qualityData, total);
    }

    updateQualityStats(qualityData, total) {
        const container = document.getElementById('quality-stats');
        if (!container) return;
        
        const qualityLabels = {
            high: 'Cao',
            medium: 'Trung bình',
            low: 'Thấp'
        };
        
        const html = Object.entries(qualityData)
            .filter(([_, count]) => count > 0)
            .map(([quality, count]) => {
                const percentage = ((count / total) * 100).toFixed(1);
                return `
                    <div class="quality-item ${quality}">
                        <span>${qualityLabels[quality]}</span>
                        <span>${count} (${percentage}%)</span>
                    </div>
                `;
            }).join('');
        
        container.innerHTML = html;
    }

    createTopMeetingsChart(meetingData) {
        const container = document.getElementById('top-meetings');
        if (!container) return;
        
        const sortedMeetings = Object.entries(meetingData)
            .sort(([,a], [,b]) => b.count - a.count)
            .slice(0, 5);
        
        if (sortedMeetings.length === 0) {
            container.innerHTML = '<p class="text-muted text-center">Không có dữ liệu</p>';
            return;
        }
        
        const html = sortedMeetings.map(([meetingCode, data]) => {
            return `
                <div class="meeting-item">
                    <div class="meeting-info">
                        <div class="meeting-code">${meetingCode}</div>
                        <div class="meeting-meta">
                            ${this.formatDuration(data.totalDuration)} • ${this.formatFileSize(data.totalSize)}
                        </div>
                    </div>
                    <div class="meeting-count">${data.count}</div>
                </div>
            `;
        }).join('');
        
        container.innerHTML = html;
    }

    createActivityHeatmap(activityData) {
        const container = document.getElementById('activity-heatmap');
        if (!container) return;
        
        const maxActivity = Math.max(...Object.values(activityData), 1);
        const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
        
        let html = '';
        
        // Add hour labels
        for (let hour = 0; hour < 24; hour++) {
            html += `<div style="grid-column: ${hour + 1}; grid-row: 1; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; color: #666;">${hour}</div>`;
        }
        
        // Add day labels and cells
        for (let day = 0; day < 7; day++) {
            html += `<div style="grid-column: 1; grid-row: ${day + 2}; display: flex; align-items: center; justify-content: center; font-size: 0.8rem; color: #666; margin-right: 10px;">${days[day]}</div>`;
            
            for (let hour = 0; hour < 24; hour++) {
                const activityKey = `${day}-${hour}`;
                const activity = activityData[activityKey] || 0;
                const level = Math.min(Math.ceil((activity / maxActivity) * 5), 5);
                
                html += `
                    <div class="heatmap-cell level-${level}" 
                         style="grid-column: ${hour + 2}; grid-row: ${day + 2};"
                         title="${days[day]} ${hour}:00 - ${activity} recordings">
                    </div>
                `;
            }
        }
        
        container.innerHTML = html;
        container.style.gridTemplateColumns = 'auto repeat(24, 1fr)';
        container.style.gridTemplateRows = 'auto repeat(7, 1fr)';
    }

    generateSmartInsights(stats) {
        const container = document.getElementById('smart-insights');
        if (!container) return;
        
        const insights = [];
        
        // Peak activity insight
        if (stats.peakDay) {
            insights.push({
                title: '📅 Ngày hoạt động cao nhất',
                description: `Ngày ${stats.peakDay} có nhiều recordings nhất với ${stats.timelineData[stats.peakDay]} recordings được tạo.`
            });
        }
        
        // Storage insight
        const storagePercent = (stats.totalStorage / (5 * 1024 * 1024 * 1024)) * 100;
        if (storagePercent > 80) {
            insights.push({
                title: '⚠️ Dung lượng sắp hết',
                description: `Bạn đã sử dụng ${storagePercent.toFixed(1)}% dung lượng. Hãy xem xét xóa các recordings cũ hoặc nâng cấp gói lưu trữ.`
            });
        } else if (storagePercent < 20) {
            insights.push({
                title: '💾 Dung lượng còn nhiều',
                description: `Bạn mới sử dụng ${storagePercent.toFixed(1)}% dung lượng. Có thể tăng chất lượng recording để có trải nghiệm tốt hơn.`
            });
        }
        
        // Quality insight
        const totalQuality = Object.values(stats.qualityData).reduce((a, b) => a + b, 0);
        const highQualityPercent = (stats.qualityData.high / totalQuality) * 100 || 0;
        if (highQualityPercent < 30) {
            insights.push({
                title: '🎬 Chất lượng recording',
                description: `Chỉ ${highQualityPercent.toFixed(1)}% recordings ở chất lượng cao. Hãy xem xét nâng cấp cài đặt để có trải nghiệm tốt hơn.`
            });
        }
        
        // Meeting activity insight
        const meetingCount = Object.keys(stats.meetingData).length;
        if (meetingCount > 10) {
            insights.push({
                title: '🏆 Người dùng tích cực',
                description: `Bạn đã tham gia ${meetingCount} meeting khác nhau, cho thấy mức độ hoạt động cao.`
            });
        }
        
        const html = insights.map(insight => `
            <div class="insight-card">
                <div class="insight-title">${insight.title}</div>
                <div class="insight-description">${insight.description}</div>
            </div>
        `).join('');
        
        container.innerHTML = html || '<p class="text-muted">Không có insights nào được tạo.</p>';
    }

    updateStorageBreakdown(stats) {
        const container = document.getElementById('storage-details');
        if (!container) return;
        
        const breakdownItems = [
            { label: 'Video Recordings', value: this.formatFileSize(stats.totalStorage * 0.8) },
            { label: 'Screenshots', value: this.formatFileSize(stats.totalStorage * 0.15) },
            { label: 'Thumbnails', value: this.formatFileSize(stats.totalStorage * 0.05) },
            { label: 'Tổng cộng', value: this.formatFileSize(stats.totalStorage) }
        ];
        
        const html = breakdownItems.map(item => `
            <div class="storage-item">
                <span class="storage-label">${item.label}</span>
                <span class="storage-value">${item.value}</span>
            </div>
        `).join('');
        
        container.innerHTML = html;
    }

    // Helper methods
    drawNoDataMessage(ctx, canvas) {
        ctx.fillStyle = '#6c757d';
        ctx.font = '16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Không có dữ liệu', canvas.width / 2, canvas.height / 2);
    }

    drawChartLegend(containerId, items) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        const html = items.map(item => `
            <div class="legend-item">
                <div class="legend-color" style="background-color: ${item.color}"></div>
                <span>${item.label}: ${item.count} (${item.percentage}%)</span>
            </div>
        `).join('');
        
        container.innerHTML = html;
    }

    darkenColor(color, factor) {
        // Simple color darkening function
        return color; // Placeholder
    }

    // ======== STORAGE MANAGEMENT ========
    async cleanupOldFiles() {
        this.showNotification('Tính năng dọn dẹp đang được phát triển', 'info');
    }

    async compressRecordings() {
        this.showNotification('Tính năng nén đang được phát triển', 'info');
    }
} 