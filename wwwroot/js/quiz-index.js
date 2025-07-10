/**
 * Quiz Index JavaScript
 * Handles quiz management functionality
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize the page
    initializeQuizIndex();
});

function initializeQuizIndex() {
    // Add event listeners
    addEventListeners();
    
    // Initialize search functionality
    initializeSearch();
    
    // Add smooth animations
    addAnimations();
}

function addEventListeners() {
    // Search functionality
    const searchInput = document.getElementById('quiz-search');
    if (searchInput) {
        searchInput.addEventListener('input', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            filterQuizCards(searchTerm);
        });
    }
    
    // Quiz cards
    const quizCards = document.querySelectorAll('.quiz-card:not(.empty-card)');
    quizCards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-4px)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });
}

function initializeSearch() {
    // Search functionality is handled by the input event listener
    console.log('Search functionality initialized');
}

function filterQuizCards(searchTerm) {
    const cards = document.querySelectorAll('.quiz-card:not(.empty-card)');
    
    cards.forEach(card => {
        const title = card.querySelector('.quiz-card-title').textContent.toLowerCase();
        const desc = card.querySelector('.quiz-card-desc').textContent.toLowerCase();
        const author = card.querySelector('.quiz-author').textContent.toLowerCase();
        
        if (title.includes(searchTerm) || desc.includes(searchTerm) || author.includes(searchTerm)) {
            card.style.display = 'block';
            card.style.animation = 'fadeIn 0.3s ease-out';
        } else {
            card.style.display = 'none';
        }
    });
}

function addAnimations() {
    // Add CSS animations
    const style = document.createElement('style');
    style.textContent = `
        @keyframes fadeIn {
            from {
                opacity: 0;
            }
            to {
                opacity: 1;
            }
        }
    `;
    document.head.appendChild(style);
}

/**
 * Initialize navigation functionality
 */
function initializeNavigation() {
    let currentPage = 1;
    const totalPages = 3; // This should be dynamic based on actual data
    
    // Update page indicator
    function updatePageIndicator() {
        const indicator = document.querySelector('.page-indicator');
        if (indicator) {
            indicator.textContent = `${currentPage}/${totalPages}`;
        }
    }
    
    // Previous page
    window.previousPage = function() {
        if (currentPage > 1) {
            currentPage--;
            updatePageIndicator();
            // Here you would typically load new data or show different cards
            console.log('Previous page:', currentPage);
        }
    };
    
    // Next page
    window.nextPage = function() {
        if (currentPage < totalPages) {
            currentPage++;
            updatePageIndicator();
            // Here you would typically load new data or show different cards
            console.log('Next page:', currentPage);
        }
    };
}

/**
 * Initialize tooltips and popovers
 */
function initializeTooltips() {
    // Add tooltips to action buttons
    const actionButtons = document.querySelectorAll('.btn-edit, .btn-delete');
    actionButtons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            const tooltip = document.createElement('div');
            tooltip.className = 'tooltip';
            tooltip.textContent = this.classList.contains('btn-edit') ? 'Chỉnh sửa' : 'Xóa';
            tooltip.style.cssText = `
                position: absolute;
                background: rgba(0, 0, 0, 0.8);
                color: white;
                padding: 4px 8px;
                border-radius: 4px;
                font-size: 12px;
                z-index: 1000;
                pointer-events: none;
                white-space: nowrap;
            `;
            
            const rect = this.getBoundingClientRect();
            tooltip.style.left = rect.left + 'px';
            tooltip.style.top = (rect.top - 30) + 'px';
            
            document.body.appendChild(tooltip);
            this._tooltip = tooltip;
        });
        
        button.addEventListener('mouseleave', function() {
            if (this._tooltip) {
                this._tooltip.remove();
                this._tooltip = null;
            }
        });
    });
}

/**
 * Show upgrade modal
 */
window.showUpgradeModal = function() {
    // Create modal for upgrade
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.innerHTML = `
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">
                        <i class="bi bi-crown me-2"></i>Nâng cấp tài khoản
                    </h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <div class="text-center">
                        <i class="bi bi-crown fs-1 mb-3" style="color: var(--quiz-warning);"></i>
                        <h4>Tận hưởng tất cả tính năng</h4>
                        <p class="text-muted">Dùng thử miễn phí 7 ngày!</p>
                        <ul class="list-unstyled text-start">
                            <li><i class="bi bi-check-circle me-2" style="color: var(--quiz-success);"></i>Quiz không giới hạn</li>
                            <li><i class="bi bi-check-circle me-2" style="color: var(--quiz-success);"></i>Xuất báo cáo chi tiết</li>
                            <li><i class="bi bi-check-circle me-2" style="color: var(--quiz-success);"></i>Chia sẻ quiz với cộng đồng</li>
                            <li><i class="bi bi-check-circle me-2" style="color: var(--quiz-success);"></i>Hỗ trợ ưu tiên</li>
                        </ul>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Để sau</button>
                    <button type="button" class="btn btn-warning" onclick="startUpgrade()">
                        <i class="bi bi-crown me-1"></i>
                        Bắt đầu dùng thử
                    </button>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Initialize Bootstrap modal
    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
    
    // Remove modal from DOM after it's hidden
    modal.addEventListener('hidden.bs.modal', function() {
        modal.remove();
    });
};

function startUpgrade() {
    // Redirect to upgrade page or show payment form
    alert('Chức năng nâng cấp sẽ được phát triển sau');
}

/**
 * Edit quiz function
 */
window.editQuiz = function(quizId) {
    window.location.href = `/Quiz/Edit/${quizId}`;
};

/**
 * Delete quiz function
 */
window.deleteQuiz = function(quizId) {
    if (confirm('Bạn có chắc chắn muốn xóa quiz này?')) {
        fetch(`/Quiz/Delete/${quizId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (response.ok) {
                // Remove quiz card from DOM with animation
                const quizCard = document.querySelector(`[onclick*="editQuiz(${quizId})"]`)?.closest('.quiz-card');
                if (quizCard) {
                    quizCard.style.animation = 'fadeOut 0.3s ease-out';
                    setTimeout(() => {
                        quizCard.remove();
                    }, 300);
                } else {
                    window.location.reload();
                }
            } else {
                alert('Có lỗi xảy ra khi xóa quiz');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Có lỗi xảy ra khi xóa quiz');
        });
    }
};

/**
 * Start quiz function
 */
window.startQuiz = function(quizId) {
    window.location.href = `/Quiz/Details/${quizId}`;
};

/**
 * View quiz details function
 */
window.viewDetails = function(quizId) {
    window.location.href = `/Quiz/Details/${quizId}`;
};

/**
 * Add questions function
 */
window.addQuestions = function(quizId) {
    window.location.href = `/Quiz/AddQuestions/${quizId}`;
};

// Navigation functions
function previousPage() {
    // Implement pagination logic
    console.log('Previous page');
}

function nextPage() {
    // Implement pagination logic
    console.log('Next page');
}

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl/Cmd + N for new quiz
    if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        window.location.href = '/Quiz/Create';
    }
    
    // Ctrl/Cmd + K for search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        document.getElementById('quiz-search').focus();
    }
});

// Add fadeOut animation for delete
const fadeOutStyle = document.createElement('style');
fadeOutStyle.textContent = `
    @keyframes fadeOut {
        from {
            opacity: 1;
            transform: translateY(0);
        }
        to {
            opacity: 0;
            transform: translateY(-20px);
        }
    }
`;
document.head.appendChild(fadeOutStyle); 