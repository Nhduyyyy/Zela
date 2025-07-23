/**
 * Quiz Questions Management
 * Quản lý câu hỏi cho quiz
 */

class QuizQuestionsManager {
    constructor() {
        this.currentQuizId = null;
        this.currentQuestionId = null;
        this.isEditMode = false;
        this.isModalOpen = false;
        this.init();
    }

    init() {
        this.bindEvents();
        this.setupModalHandlers();
        this.setupFormValidation();
    }

    bindEvents() {
        // Add question button - sử dụng data attributes
        document.addEventListener('click', (e) => {
            const addButton = e.target.closest('[data-action="add-question"]');
            if (addButton && !this.isModalOpen) {
                e.preventDefault();
                e.stopPropagation();
                this.showAddQuestionModal();
            }
        });

        // Edit question buttons
        document.addEventListener('click', (e) => {
            const editButton = e.target.closest('[data-action="edit-question"]');
            if (editButton && !this.isModalOpen) {
                e.preventDefault();
                e.stopPropagation();
                const questionId = parseInt(editButton.getAttribute('data-question-id'));
                this.editQuestion(questionId);
            }
        });

        // Delete question buttons
        document.addEventListener('click', (e) => {
            const deleteButton = e.target.closest('[data-action="delete-question"]');
            if (deleteButton) {
                e.preventDefault();
                e.stopPropagation();
                const questionId = parseInt(deleteButton.getAttribute('data-question-id'));
                this.deleteQuestion(questionId);
            }
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 'n' && !this.isModalOpen) {
                e.preventDefault();
                this.showAddQuestionModal();
            }
        });

        // Upload image
        document.addEventListener('click', (e) => {
            if (e.target && e.target.id === 'btnUploadImage') {
                e.preventDefault();
                document.getElementById('questionImageInput').click();
            }
        });

        // Upload file
        document.addEventListener('click', (e) => {
            if (e.target && e.target.id === 'btnUploadFile') {
                e.preventDefault();
                document.getElementById('questionFileInput').click();
            }
        });

        // Handle image input change
        document.addEventListener('change', (e) => {
            if (e.target && e.target.id === 'questionImageInput') {
                const file = e.target.files[0];
                if (!file) return;
                if (file.size > 3 * 1024 * 1024) {
                    this.showNotification('Ảnh không được vượt quá 3MB', 'error');
                    return;
                }
                this.uploadQuestionMedia(file, 'image');
            }
            if (e.target && e.target.id === 'questionFileInput') {
                const file = e.target.files[0];
                if (!file) return;
                if (file.size > 5 * 1024 * 1024) {
                    this.showNotification('File đính kèm không được vượt quá 5MB', 'error');
                    return;
                }
                this.uploadQuestionMedia(file, 'media');
            }
        });
    }

    setupModalHandlers() {
        // Modal backdrop click
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal') && this.isModalOpen) {
                this.hideModal();
            }
        });

        // Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isModalOpen) {
                this.hideModal();
            }
        });

        // Bootstrap modal events
        document.addEventListener('hidden.bs.modal', (e) => {
            if (e.target.id === 'questionModal') {
                this.isModalOpen = false;
                this.cleanupModal();
            }
        });

        document.addEventListener('shown.bs.modal', (e) => {
            if (e.target.id === 'questionModal') {
                this.isModalOpen = true;
            }
        });
    }

    setupFormValidation() {
        // Question type change
        document.addEventListener('change', (e) => {
            if (e.target.name === 'QuestionType') {
                this.handleQuestionTypeChange(e.target.value);
            }
        });
    }

    showAddQuestionModal() {
        if (this.isModalOpen) {
            console.log('Modal is already open, ignoring request');
            return;
        }

        this.isEditMode = false;
        this.currentQuestionId = null;
        this.createQuestionModal();
        this.showModal();
    }

    editQuestion(questionId) {
        console.log('Edit question called with ID:', questionId);
        this.cleanupModal();
        this.isEditMode = true;
        this.currentQuestionId = questionId;
        
        // Load data first, then create modal
        this.loadQuestionData(questionId, (question) => {
            console.log('Question data loaded:', question);
            this.createQuestionModal();
            
            // Wait for modal to be fully rendered
            const checkModal = setInterval(() => {
                const form = document.getElementById('questionForm');
                if (form) {
                    clearInterval(checkModal);
                    console.log('Form found, populating data...');
                    this.populateForm(question);
                    this.showModal();
                }
            }, 50);
            
            // Timeout after 2 seconds
            setTimeout(() => {
                clearInterval(checkModal);
                console.log('Modal creation timeout');
            }, 2000);
        });
    }

    async loadQuestionData(questionId, callback) {
        console.log('Loading question data for ID:', questionId);
        try {
            const response = await fetch(`/Quiz/GetQuestion/${questionId}`);
            console.log('Response status:', response.status);
            
            if (response.ok) {
                const question = await response.json();
                console.log('Question data received:', question);
                if (callback) callback(question);
            } else {
                console.error('Failed to load question data:', response.status, response.statusText);
                this.showNotification('Không thể tải dữ liệu câu hỏi', 'error');
            }
        } catch (error) {
            console.error('Error loading question:', error);
            this.showNotification('Lỗi khi tải dữ liệu', 'error');
        }
    }

    populateForm(question) {
        console.log('Populating form with question data:', question);
        const form = document.getElementById('questionForm');
        if (!form) {
            console.error('Form not found!');
            return;
        }
        
        try {
            // Hỗ trợ cả camelCase và PascalCase
            const content = question.content || question.Content || '';
            const questionType = question.questionType || question.QuestionType || 'MultipleChoice';
            const choices = question.choices || question.Choices || '';
            const answerKey = question.answerKey || question.AnswerKey || '';
            
            console.log('Setting form values:', { content, questionType, choices, answerKey });
            
            const contentField = form.querySelector('[name="Content"]');
            const questionTypeField = form.querySelector('[name="QuestionType"]');
            const choicesField = form.querySelector('[name="Choices"]');
            const answerKeyField = form.querySelector('[name="AnswerKey"]');
            
            if (contentField) contentField.value = content;
            if (questionTypeField) questionTypeField.value = questionType;
            if (choicesField) choicesField.value = choices;
            if (answerKeyField) answerKeyField.value = answerKey;
            
            // Update question type sections
            this.handleQuestionTypeChange(questionType);
            
            console.log('Form populated successfully');
        } catch (error) {
            console.error('Error populating form:', error);
        }
    }

    createQuestionModal() {
        // Remove existing modal first
        this.cleanupModal();

        const modalHtml = `
            <div class="modal fade" id="questionModal" tabindex="-1" aria-labelledby="questionModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content" style="background: var(--quiz-bg-card); border: 1px solid var(--quiz-border-primary);">
                        <div class="modal-header" style="background: var(--quiz-bg-card); border-bottom: 1px solid var(--quiz-border-primary);">
                            <h5 class="modal-title" id="questionModalLabel" style="color: var(--quiz-text-primary);">
                                <i class="bi bi-question-circle me-2"></i>
                                ${this.isEditMode ? 'Chỉnh sửa câu hỏi' : 'Thêm câu hỏi mới'}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <form id="questionForm">
                                <div class="row">
                                    <div class="col-12 mb-3">
                                        <label for="Content" class="form-label" style="color: var(--quiz-text-primary);">
                                            <i class="bi bi-chat-text me-1"></i>
                                            Nội dung câu hỏi <span class="text-danger">*</span>
                                        </label>
                                        <textarea 
                                            class="form-control" 
                                            id="Content" 
                                            name="Content" 
                                            rows="3" 
                                            required
                                            style="background: var(--quiz-bg-input); border: 1px solid var(--quiz-border-primary); color: var(--quiz-text-primary);"
                                            placeholder="Nhập nội dung câu hỏi..."></textarea>
                                        <div class="mt-2 d-flex gap-2 align-items-center">
                                            <input type="file" id="questionImageInput" accept="image/*" style="display:none;">
                                            <button type="button" class="btn btn-outline-primary btn-sm" id="btnUploadImage"><i class="bi bi-image"></i> Thêm ảnh (≤3MB)</button>
                                            <input type="file" id="questionFileInput" accept="application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,video/*,audio/*" style="display:none;">
                                            <button type="button" class="btn btn-outline-secondary btn-sm" id="btnUploadFile"><i class="bi bi-paperclip"></i> Đính kèm file (≤5MB)</button>
                                            <span id="uploadStatus" class="ms-2" style="color: var(--quiz-text-secondary);"></span>
                                        </div>
                                        <div id="questionMediaPreview" class="mt-2"></div>
                                    </div>
                                    
                                    <div class="col-md-6 mb-3">
                                        <label for="QuestionType" class="form-label" style="color: var(--quiz-text-primary);">
                                            <i class="bi bi-list-check me-1"></i>
                                            Loại câu hỏi
                                        </label>
                                        <select 
                                            class="form-select" 
                                            id="QuestionType" 
                                            name="QuestionType"
                                            style="background: var(--quiz-bg-input); border: 1px solid var(--quiz-border-primary); color: var(--quiz-text-primary);">
                                            <option value="MultipleChoice">Trắc nghiệm</option>
                                            <option value="TrueFalse">Đúng/Sai</option>
                                            <option value="ShortAnswer">Câu trả lời ngắn</option>
                                            <option value="Essay">Tự luận</option>
                                        </select>
                                    </div>
                                    
                                    <div class="col-md-6 mb-3">
                                        <label for="AnswerKey" class="form-label" style="color: var(--quiz-text-primary);">
                                            <i class="bi bi-check-circle me-1"></i>
                                            Đáp án <span class="text-danger answer-required">*</span>
                                        </label>
                                        <input 
                                            type="text" 
                                            class="form-control" 
                                            id="AnswerKey" 
                                            name="AnswerKey" 
                                            style="background: var(--quiz-bg-input); border: 1px solid var(--quiz-border-primary); color: var(--quiz-text-primary);"
                                            placeholder="Nhập đáp án chính xác...">
                                    </div>
                                    </div>
                                    
                                <!-- Multiple Choice Options -->
                                <div id="multipleChoiceSection" class="question-type-section">
                                    <div class="row">
                                        <div class="col-12 mb-3">
                                        <label for="Choices" class="form-label" style="color: var(--quiz-text-primary);">
                                            <i class="bi bi-list-ul me-1"></i>
                                                Các lựa chọn (mỗi lựa chọn một dòng)
                                        </label>
                                        <textarea 
                                            class="form-control" 
                                            id="Choices" 
                                            name="Choices" 
                                            rows="4"
                                            style="background: var(--quiz-bg-input); border: 1px solid var(--quiz-border-primary); color: var(--quiz-text-primary);"
                                                placeholder="A. Lựa chọn 1&#10;B. Lựa chọn 2&#10;C. Lựa chọn 3&#10;D. Lựa chọn 4"></textarea>
                                            <div class="form-text" style="color: var(--quiz-text-secondary);">
                                                <i class="bi bi-info-circle me-1"></i>
                                                Mỗi lựa chọn trên một dòng riêng biệt
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <!-- True/False Section -->
                                <div id="trueFalseSection" class="question-type-section" style="display: none;">
                                    <div class="row">
                                        <div class="col-12 mb-3">
                                            <div class="alert alert-info" style="background: var(--quiz-info); border: none; color: white;">
                                                <i class="bi bi-info-circle me-2"></i>
                                                <strong>Đúng/Sai:</strong> Học viên sẽ chọn "Đúng" hoặc "Sai". 
                                                Đáp án phải là "Đúng" hoặc "Sai" (chính xác).
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <!-- Short Answer Section -->
                                <div id="shortAnswerSection" class="question-type-section" style="display: none;">
                                    <div class="row">
                                        <div class="col-12 mb-3">
                                            <div class="alert alert-info" style="background: var(--quiz-info); border: none; color: white;">
                                                <i class="bi bi-info-circle me-2"></i>
                                                <strong>Câu trả lời ngắn:</strong> Học viên sẽ nhập câu trả lời ngắn gọn. 
                                                Đáp án phải chính xác từng từ.
                                            </div>
                                        </div>
                                    </div>
                                </div>
                                
                                <!-- Essay Section -->
                                <div id="essaySection" class="question-type-section" style="display: none;">
                                    <div class="row">
                                        <div class="col-12 mb-3">
                                            <div class="alert alert-info" style="background: var(--quiz-info); border: none; color: white;">
                                                <i class="bi bi-info-circle me-2"></i>
                                                <strong>Tự luận:</strong> Học viên sẽ viết bài luận chi tiết. 
                                                Câu hỏi này sẽ được chấm điểm thủ công và không cần đáp án tự động.
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer" style="background: var(--quiz-bg-card); border-top: 1px solid var(--quiz-border-primary);">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                <i class="bi bi-x-circle me-1"></i>
                                Hủy
                            </button>
                            <button type="button" class="btn btn-primary" onclick="quizQuestionsManager.saveQuestion()">
                                <i class="bi bi-check-circle me-1"></i>
                                ${this.isEditMode ? 'Cập nhật' : 'Thêm'}
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);

        setTimeout(() => {
            const form = document.getElementById('questionForm');
            if (form && !this.isEditMode) form.reset(); // Chỉ reset khi thêm mới
            // Reset các section hiển thị theo loại câu hỏi
            document.getElementById('multipleChoiceSection').style.display = '';
            document.getElementById('trueFalseSection').style.display = 'none';
            document.getElementById('shortAnswerSection').style.display = 'none';
            document.getElementById('essaySection').style.display = 'none';
        }, 100);
    }

    cleanupModal() {
        const existingModal = document.getElementById('questionModal');
        if (existingModal) {
            const bootstrapModal = bootstrap.Modal.getInstance(existingModal);
            if (bootstrapModal) {
                bootstrapModal.dispose();
            }
            existingModal.remove();
        }
    }

    handleQuestionTypeChange(questionType) {
        // Hide all sections first
        document.querySelectorAll('.question-type-section').forEach(section => {
            section.style.display = 'none';
        });

        const answerKeyInput = document.getElementById('AnswerKey');
        const answerRequired = document.querySelector('.answer-required');
        
        // Show relevant section
            switch (questionType) {
                case 'MultipleChoice':
                document.getElementById('multipleChoiceSection').style.display = 'block';
                document.getElementById('Choices').required = true;
                answerKeyInput.required = true;
                answerRequired.style.display = 'inline';
                answerKeyInput.placeholder = 'Nhập đáp án chính xác (ví dụ: A, B, C, D)...';
                    break;
                case 'TrueFalse':
                document.getElementById('trueFalseSection').style.display = 'block';
                document.getElementById('Choices').required = false;
                answerKeyInput.required = true;
                answerRequired.style.display = 'inline';
                answerKeyInput.placeholder = 'Nhập "Đúng" hoặc "Sai"...';
                    break;
                case 'ShortAnswer':
                document.getElementById('shortAnswerSection').style.display = 'block';
                document.getElementById('Choices').required = false;
                answerKeyInput.required = true;
                answerRequired.style.display = 'inline';
                answerKeyInput.placeholder = 'Nhập đáp án chính xác...';
                break;
                case 'Essay':
                document.getElementById('essaySection').style.display = 'block';
                document.getElementById('Choices').required = false;
                answerKeyInput.required = false;
                answerRequired.style.display = 'none';
                answerKeyInput.placeholder = 'Để trống cho câu hỏi tự luận (sẽ chấm thủ công)...';
                    break;
        }
    }

    async saveQuestion() {
        const form = document.getElementById('questionForm');
        if (!form) return;

        const formData = new FormData(form);
        const quizId = this.getCurrentQuizId();
        
        if (!quizId) {
            this.showNotification('Không tìm thấy ID quiz', 'error');
            return;
        }

        const questionType = formData.get('QuestionType');
        const content = formData.get('Content');
        const choices = formData.get('Choices');
        const answerKey = formData.get('AnswerKey');

        // Validate từng loại câu hỏi
        if (!content || content.trim() === '') {
            this.showNotification('Nội dung câu hỏi không được để trống', 'error');
            return;
        }
        if (questionType === 'MultipleChoice') {
            if (!choices || choices.trim() === '') {
                this.showNotification('Bạn phải nhập các lựa chọn cho câu hỏi trắc nghiệm', 'error');
                return;
            }
            if (!answerKey || answerKey.trim() === '') {
                this.showNotification('Bạn phải nhập đáp án cho câu hỏi trắc nghiệm', 'error');
                return;
            }
        }
        if (questionType === 'TrueFalse') {
            if (!['Đúng', 'Sai'].includes(answerKey)) {
                this.showNotification('Đáp án Đúng/Sai chỉ được là "Đúng" hoặc "Sai"', 'error');
                return;
            }
        }
        if (questionType === 'ShortAnswer') {
            if (!answerKey || answerKey.trim() === '') {
                this.showNotification('Bạn phải nhập đáp án cho câu hỏi trả lời ngắn', 'error');
                return;
            }
        }
        // Tự luận không cần answerKey

        const questionData = {
            quizId: parseInt(quizId),
            content: content,
            questionType: questionType,
            choices: choices,
            answerKey: answerKey
        };

        try {
            const url = this.isEditMode 
                ? `/Quiz/UpdateQuestion/${this.currentQuestionId}`
                : '/Quiz/AddQuestion';
            const method = this.isEditMode ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(questionData)
            });

            if (response.ok) {
                this.showNotification(
                    this.isEditMode ? 'Cập nhật câu hỏi thành công!' : 'Thêm câu hỏi thành công!',
                    'success'
                );
                this.hideModal();
                this.refreshQuestionsList();
            } else {
                const error = await response.json();
                this.showNotification(error.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error saving question:', error);
            this.showNotification('Lỗi khi lưu câu hỏi', 'error');
        }
    }

    async deleteQuestion(questionId) {
        if (!confirm('Bạn có chắc chắn muốn xóa câu hỏi này?')) {
            return;
        }

        try {
            const response = await fetch(`/Quiz/DeleteQuestion/${questionId}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (response.ok) {
                this.showNotification('Xóa câu hỏi thành công!', 'success');
                this.refreshQuestionsList();
            } else {
                const error = await response.json();
                this.showNotification(error.message || 'Có lỗi xảy ra', 'error');
            }
        } catch (error) {
            console.error('Error deleting question:', error);
            this.showNotification('Lỗi khi xóa câu hỏi', 'error');
        }
    }

    async refreshQuestionsList() {
        const quizId = this.getCurrentQuizId();
        if (!quizId) return;

        try {
            const response = await fetch(`/Quiz/GetQuestions/${quizId}`);
            if (response.ok) {
                const questions = await response.json();
                this.updateQuestionsDisplay(questions);
            }
        } catch (error) {
            console.error('Error refreshing questions:', error);
        }
    }

    updateQuestionsDisplay(questions) {
        const questionsList = document.getElementById('questionsList');
        if (!questionsList) return;

        if (!questions || questions.length === 0) {
            questionsList.innerHTML = `
                <div class="questions-empty-state">
                    <i class="bi bi-question-circle questions-empty-icon"></i>
                    <h5 class="questions-empty-title">Chưa có câu hỏi nào</h5>
                    <p class="questions-empty-description">Bắt đầu thêm câu hỏi cho quiz này</p>
                    <button class="btn btn-primary" data-action="add-question">
                        <i class="bi bi-plus-circle"></i>
                        Thêm câu hỏi đầu tiên
                    </button>
                </div>
            `;
            return;
        }

        const questionsHtml = questions.map((question, index) => {
            const questionTypeName = this.getQuestionTypeName(question.questionType);
            const questionTypeClass = this.getQuestionTypeClass(question.questionType);
            const questionNumber = index + 1; // Số thứ tự từ 1
            
            let choicesDisplay = '';
            if (question.choices && question.questionType === 'MultipleChoice') {
                const choices = question.choices.split('\n').filter(c => c.trim());
                choicesDisplay = choices.map(choice => `<div class="choice-preview">${choice.trim()}</div>`).join('');
            }

            let answerDisplay = '';
            if (question.questionType === 'Essay') {
                answerDisplay = `
                    <div class="question-detail-value question-answer essay-answer">
                        <i class="bi bi-info-circle me-1"></i>
                        <span>Câu hỏi tự luận - chấm điểm thủ công</span>
                    </div>
                `;
            } else if (question.answerKey) {
                answerDisplay = `
                    <div class="question-detail-value question-answer">${question.answerKey}</div>
            `;
        } else {
                answerDisplay = `
                    <div class="question-detail-value question-answer no-answer">
                        <i class="bi bi-exclamation-triangle me-1"></i>
                        <span>Chưa có đáp án</span>
                            </div>
                `;
            }

            return `
                <div class="question-item">
                    <div class="question-header">
                        <div class="question-content">
                            <h6 class="question-title">Câu ${questionNumber}: ${question.content}</h6>
                            <span class="question-type-badge ${questionTypeClass}">${questionTypeName}</span>
                        </div>
                        <div class="question-actions">
                            <button class="question-action-btn edit" data-action="edit-question" data-question-id="${question.questionId}" title="Chỉnh sửa">
                                <i class="bi bi-pencil"></i>
                            </button>
                            <button class="question-action-btn delete" data-action="delete-question" data-question-id="${question.questionId}" title="Xóa">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    </div>
                    <div class="question-details">
                        ${choicesDisplay ? `
                            <div class="question-detail-item">
                                <span class="question-detail-label">Lựa chọn</span>
                                <div class="question-detail-value choices-preview">
                                    ${choicesDisplay}
                                </div>
                            </div>
                        ` : ''}
                        <div class="question-detail-item">
                            <span class="question-detail-label">Đáp án</span>
                            ${answerDisplay}
                        </div>
                    </div>
                </div>
            `;
        }).join('');
            
            questionsList.innerHTML = questionsHtml;
        }

    getQuestionTypeName(type) {
        const typeNames = {
            'MultipleChoice': 'Trắc nghiệm',
            'TrueFalse': 'Đúng/Sai',
            'ShortAnswer': 'Câu trả lời ngắn',
            'Essay': 'Tự luận'
        };
        return typeNames[type] || type;
    }

    getQuestionTypeClass(type) {
        const typeClasses = {
            'MultipleChoice': 'badge-primary',
            'TrueFalse': 'badge-success',
            'ShortAnswer': 'badge-warning',
            'Essay': 'badge-info'
        };
        return typeClasses[type] || 'badge-secondary';
    }

    getCurrentQuizId() {
        // Extract quiz ID from URL path
        const pathParts = window.location.pathname.split('/');
        const quizIdIndex = pathParts.indexOf('AddQuestions') + 1;
        if (quizIdIndex < pathParts.length) {
            return pathParts[quizIdIndex];
        }
        return this.currentQuizId;
    }

    getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    showModal() {
        const modal = document.getElementById('questionModal');
        if (modal) {
            const bootstrapModal = new bootstrap.Modal(modal);
            bootstrapModal.show();
        }
    }

    hideModal() {
        const modal = document.getElementById('questionModal');
        if (modal) {
            const bootstrapModal = bootstrap.Modal.getInstance(modal);
            if (bootstrapModal) {
                bootstrapModal.hide();
            }
        }
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show position-fixed`;
        notification.style.cssText = `
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            background: var(--quiz-bg-card);
            border: 1px solid var(--quiz-border-primary);
            color: var(--quiz-text-primary);
        `;
        
        notification.innerHTML = `
            <i class="bi bi-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    async uploadQuestionMedia(file, type) {
        const status = document.getElementById('uploadStatus');
        status.textContent = 'Đang upload...';
        const formData = new FormData();
        formData.append('file', file);
        formData.append('type', type);
        try {
            const response = await fetch('/Quiz/UploadQuestionMedia', {
                method: 'POST',
                body: formData
            });
            const result = await response.json();
            if (response.ok && result.url) {
                status.textContent = 'Upload thành công!';
                this.insertMediaToContent(result.url, type);
                this.showMediaPreview(result.url, type);
            } else {
                status.textContent = '';
                this.showNotification(result.message || 'Upload thất bại', 'error');
            }
        } catch (error) {
            status.textContent = '';
            this.showNotification('Lỗi upload file', 'error');
        }
    }

    insertMediaToContent(url, type) {
        const textarea = document.getElementById('Content');
        if (!textarea) return;
        if (type === 'image') {
            textarea.value += `\n<img src=\"${url}\" alt=\"Hình minh họa\" style=\"max-width:100%;\">\n`;
        } else {
            textarea.value += `\n<a href=\"${url}\" target=\"_blank\">File đính kèm</a>\n`;
        }
    }

    showMediaPreview(url, type) {
        const preview = document.getElementById('questionMediaPreview');
        if (!preview) return;
        if (type === 'image') {
            preview.innerHTML = `<img src="${url}" alt="Hình minh họa" style="max-width:200px;max-height:150px;border-radius:8px;box-shadow:0 2px 8px #0002;">`;
        } else {
            preview.innerHTML = `<a href="${url}" target="_blank" class="btn btn-outline-secondary btn-sm"><i class="bi bi-paperclip"></i> Xem file đính kèm</a>`;
        }
    }
}

// Initialize the manager
const quizQuestionsManager = new QuizQuestionsManager();