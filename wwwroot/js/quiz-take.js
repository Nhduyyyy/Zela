/**
 * Quiz Take Management
 * JavaScript cho trang làm bài quiz
 */

class QuizTakeManager {
    constructor() {
        this.quizId = null;
        this.questions = [];
        this.currentQuestionIndex = 0;
        this.answers = {};
        this.startTime = null;
        this.timeLimit = 0;
        this.timerInterval = null;
        this.isSubmitting = false;
        this.markedQuestions = new Set();
        this.isAutoSubmitting = false; // Flag để tránh submit nhiều lần
        this.hasShownWarning = false; // Flag để tránh hiển thị warning nhiều lần
        this.init();
    }

    init() {
        this.loadQuizData();
        this.bindEvents();
        this.setupKeyboardNavigation();
        this.setupAutoSubmitHandlers();
    }

    async loadQuizData() {
        try {
            // Get quiz data from hidden inputs
            this.quizId = document.getElementById('quizId').value;
            this.timeLimit = parseInt(document.getElementById('timeLimit').value) || 0;
            const totalQuestions = parseInt(document.getElementById('totalQuestionsCount').value) || 0;

            // Load questions from server
            const response = await fetch(`/Quiz/GetQuestions/${this.quizId}`);
            if (response.ok) {
                this.questions = await response.json();
                
                // Khôi phục trạng thái nếu có
                const restored = this.restoreState();
                if (!restored) {
                    this.startTime = new Date();
                } else if (!this.startTime) {
                    // Nếu khôi phục được trạng thái nhưng không có startTime, tạo mới
                    this.startTime = new Date();
                }
                
                this.initializeQuiz();
            } else if (response.status === 401) {
                // Unauthorized - user not logged in
                this.showError('Bạn cần đăng nhập để làm bài. Đang chuyển hướng...');
                setTimeout(() => {
                    window.location.href = '/Account/Login';
                }, 2000);
            } else {
                this.showError('Không thể tải dữ liệu quiz');
            }
        } catch (error) {
            console.error('Error loading quiz:', error);
            this.showError('Lỗi khi tải quiz');
        }
    }

    initializeQuiz() {
        this.generateQuestionIndicators();
        this.showQuestion(this.currentQuestionIndex);
        this.startTimer();
        this.updateProgress();
        
        // Đảm bảo nút "Nộp bài" luôn hiển thị
        const submitBtn = document.getElementById('submitBtn');
        if (submitBtn) {
            submitBtn.style.display = 'inline-flex';
        }
        
        // Ẩn submit section vì không cần thiết nữa
        const submitSection = document.getElementById('submitSection');
        if (submitSection) {
            submitSection.style.display = 'none';
        }
        
        // Lưu trạng thái ban đầu
        this.saveCurrentState();
    }

    generateQuestionIndicators() {
        const indicatorsContainer = document.getElementById('questionIndicators');
        if (!indicatorsContainer) return;

        indicatorsContainer.innerHTML = '';
        
        for (let i = 0; i < this.questions.length; i++) {
            const indicator = document.createElement('div');
            indicator.className = 'question-indicator';
            indicator.textContent = i + 1;
            indicator.onclick = () => this.goToQuestion(i);
            
            if (i === 0) {
                indicator.classList.add('current');
            }
            
            indicatorsContainer.appendChild(indicator);
        }
    }

    showQuestion(index) {
        if (index < 0 || index >= this.questions.length) return;

        this.currentQuestionIndex = index;
        const question = this.questions[index];

        // Update question display
        document.getElementById('questionNumber').textContent = index + 1;
        document.getElementById('questionText').innerHTML = question.content;
        document.getElementById('questionType').textContent = this.getQuestionTypeName(question.questionType);

        // Update question content based on type
        this.renderQuestionContent(question);

        // Thêm nút đánh dấu
        this.renderMarkButton();

        // Update navigation buttons
        this.updateNavigationButtons();

        // Update indicators
        this.updateQuestionIndicators();

        // Update progress
        this.updateProgress();
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

    renderQuestionContent(question) {
        const contentContainer = document.getElementById('questionContent');
        if (!contentContainer) return;

        switch (question.questionType) {
            case 'MultipleChoice':
                this.renderMultipleChoice(question);
                break;
            case 'TrueFalse':
                this.renderTrueFalse(question);
                break;
            case 'ShortAnswer':
                this.renderShortAnswer(question);
                break;
            case 'Essay':
                this.renderEssay(question);
                break;
            default:
                contentContainer.innerHTML = '<p>Loại câu hỏi không được hỗ trợ</p>';
        }
    }

    renderMultipleChoice(question) {
        const contentContainer = document.getElementById('questionContent');
        let choices = [];
        
        if (question.choices) {
            // Xử lý cả trường hợp \n là chuỗi ký tự và ký tự xuống dòng thực sự
            if (question.choices.includes('\\n')) {
                // Nếu có chuỗi \n, tách theo chuỗi đó
                choices = question.choices.split('\\n').map(c => c.trim()).filter(c => c);
            } else {
                // Nếu không có, tách theo ký tự xuống dòng thực sự
                choices = question.choices.split(/\r?\n/).map(c => c.trim()).filter(c => c);
            }
        }
        
        let html = '';
        const letters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];
        
        choices.forEach((choice, index) => {
            const letter = letters[index] || (index + 1);
            const choiceId = `choice_${this.currentQuestionIndex}_${index}`;
            const isSelected = this.answers[this.currentQuestionIndex] === choice.trim();
            
            html += `
                <div class="choice-item ${isSelected ? 'selected' : ''}" onclick="quizTakeManager.selectChoice('${choiceId}', '${choice.trim()}')">
                    <input type="radio" class="choice-radio" id="${choiceId}" name="question_${this.currentQuestionIndex}" 
                           ${isSelected ? 'checked' : ''} style="display: none;">
                    <span class="choice-letter">${letter}</span>
                    <label class="choice-label" for="${choiceId}">${choice.trim()}</label>
                </div>
            `;
        });
        
        contentContainer.innerHTML = html;
    }

    renderTrueFalse(question) {
        const contentContainer = document.getElementById('questionContent');
        const currentAnswer = this.answers[this.currentQuestionIndex] || '';
        
        let html = `
            <div class="true-false-container">
                <div class="true-false-options">
        `;
        
        const options = [
            { value: 'Đúng', label: 'Đúng', icon: 'bi-check-circle' },
            { value: 'Sai', label: 'Sai', icon: 'bi-x-circle' }
        ];
        
        options.forEach((option, index) => {
            const optionId = `tf_${this.currentQuestionIndex}_${index}`;
            const isSelected = currentAnswer === option.value;
            
            html += `
                <div class="true-false-option ${isSelected ? 'selected' : ''}" onclick="quizTakeManager.selectTrueFalse('${optionId}', '${option.value}')">
                    <input type="radio" class="tf-radio" id="${optionId}" name="tf_question_${this.currentQuestionIndex}" 
                           ${isSelected ? 'checked' : ''} style="display: none;">
                    <i class="bi ${option.icon} tf-icon"></i>
                    <span class="tf-label">${option.label}</span>
                </div>
            `;
        });
        
        html += `
                </div>
            </div>
        `;
        
        contentContainer.innerHTML = html;
    }

    renderShortAnswer(question) {
        const contentContainer = document.getElementById('questionContent');
        const currentAnswer = this.answers[this.currentQuestionIndex] || '';
        
        contentContainer.innerHTML = `
            <div class="short-answer-container">
                <div class="answer-input-group">
                    <textarea class="form-control short-answer-input" id="shortAnswer_${this.currentQuestionIndex}" 
                          rows="3" placeholder="Nhập câu trả lời của bạn..."
                              oninput="quizTakeManager.saveTextAnswer(${this.currentQuestionIndex}, this.value)">${currentAnswer}</textarea>
                    <div class="answer-hint">
                        <i class="bi bi-info-circle"></i>
                        <span>Nhập câu trả lời ngắn gọn và chính xác</span>
                    </div>
                </div>
            </div>
        `;
    }

    renderEssay(question) {
        const contentContainer = document.getElementById('questionContent');
        const currentAnswer = this.answers[this.currentQuestionIndex] || '';
        
        contentContainer.innerHTML = `
            <div class="essay-container">
                <div class="answer-input-group">
                    <div class="essay-notice">
                        <i class="bi bi-info-circle"></i>
                        <span>Câu hỏi này sẽ được chấm điểm thủ công</span>
                    </div>
                    <textarea class="form-control essay-input" id="essay_${this.currentQuestionIndex}" 
                              rows="8" placeholder="Nhập bài viết chi tiết của bạn..."
                              oninput="quizTakeManager.saveTextAnswer(${this.currentQuestionIndex}, this.value)">${currentAnswer}</textarea>
                    <div class="answer-hint">
                        <i class="bi bi-info-circle"></i>
                        <span>Viết bài luận chi tiết với đầy đủ ý kiến và lập luận</span>
                    </div>
                    <div class="word-count">
                        <span id="wordCount_${this.currentQuestionIndex}">0</span> từ
                    </div>
                </div>
            </div>
        `;
        
        // Cập nhật word count
        this.updateWordCount(this.currentQuestionIndex);
    }

    selectChoice(choiceId, choiceText) {
        // Remove previous selection
        const questionContainer = document.getElementById('questionContent');
        questionContainer.querySelectorAll('.choice-item').forEach(item => {
            item.classList.remove('selected');
        });
        
        // Select new choice
        const selectedItem = document.getElementById(choiceId).closest('.choice-item');
        if (selectedItem) {
            selectedItem.classList.add('selected');
            document.getElementById(choiceId).checked = true;
        }
        
        // Save answer
        this.answers[this.currentQuestionIndex] = choiceText;
        this.updateQuestionIndicators();
        
        // Lưu trạng thái khi người dùng chọn câu trả lời
        this.saveCurrentState();
    }

    selectTrueFalse(optionId, optionValue) {
        // Remove previous selection
        const questionContainer = document.getElementById('questionContent');
        questionContainer.querySelectorAll('.true-false-option').forEach(item => {
            item.classList.remove('selected');
        });
        
        // Select new option
        const selectedItem = document.getElementById(optionId).closest('.true-false-option');
        if (selectedItem) {
            selectedItem.classList.add('selected');
            document.getElementById(optionId).checked = true;
        }
        
        // Save answer
        this.answers[this.currentQuestionIndex] = optionValue;
        this.updateQuestionIndicators();
        
        // Lưu trạng thái khi người dùng chọn câu trả lời
        this.saveCurrentState();
    }

    saveTextAnswer(questionIndex, value) {
        this.answers[questionIndex] = value;
        this.updateQuestionIndicators();
        
        // Update word count for essay questions
        if (this.questions[questionIndex]?.questionType === 'Essay') {
            this.updateWordCount(questionIndex);
        }
        
        // Lưu trạng thái khi người dùng thay đổi câu trả lời
        this.saveCurrentState();
    }

    updateWordCount(questionIndex) {
        const textarea = document.getElementById(`essay_${questionIndex}`);
        const wordCountElement = document.getElementById(`wordCount_${questionIndex}`);
        
        if (textarea && wordCountElement) {
            const text = textarea.value.trim();
            const wordCount = text === '' ? 0 : text.split(/\s+/).length;
            wordCountElement.textContent = wordCount;
            
            // Change color based on word count
            if (wordCount < 10) {
                wordCountElement.style.color = '#dc3545'; // Red
            } else if (wordCount < 50) {
                wordCountElement.style.color = '#ffc107'; // Yellow
            } else {
                wordCountElement.style.color = '#28a745'; // Green
            }
        }
    }

    updateNavigationButtons() {
        const prevBtn = document.getElementById('prevBtn');
        const nextBtn = document.getElementById('nextBtn');
        const submitBtn = document.getElementById('submitBtn');
        const submitSection = document.getElementById('submitSection');

        if (prevBtn) {
            prevBtn.disabled = this.currentQuestionIndex === 0;
        }

        if (nextBtn) {
            if (this.currentQuestionIndex === this.questions.length - 1) {
                nextBtn.style.display = 'none';
            } else {
                nextBtn.style.display = 'inline-flex';
            }
        }

        // Luôn hiển thị nút "Nộp bài" và ẩn submit section
        if (submitBtn) {
            submitBtn.style.display = 'inline-flex';
        }
        
        if (submitSection) {
            submitSection.style.display = 'none';
        }
    }

    updateQuestionIndicators() {
        const indicatorsContainer = document.getElementById('questionIndicators');
        if (!indicatorsContainer) return;
        const children = indicatorsContainer.children;
        for (let i = 0; i < children.length; i++) {
            children[i].classList.remove('current', 'marked', 'answered');
            if (i === this.currentQuestionIndex) {
                children[i].classList.add('current');
            }
            if (this.markedQuestions.has(i)) {
                children[i].classList.add('marked');
                children[i].title = 'Câu này đã được đánh dấu (chưa chắc chắn)';
            } else if (this.answers[i] && String(this.answers[i]).trim() !== '') {
                children[i].classList.add('answered');
                children[i].title = 'Câu này đã trả lời';
            } else {
                children[i].title = '';
            }
        }
    }

    updateProgress() {
        const currentQuestionElement = document.getElementById('currentQuestion');
        const totalQuestionsElement = document.getElementById('totalQuestions');
        
        if (currentQuestionElement) {
            currentQuestionElement.textContent = this.currentQuestionIndex + 1;
        }
        
        if (totalQuestionsElement) {
            totalQuestionsElement.textContent = this.questions.length;
        }
    }

    startTimer() {
        if (this.timeLimit <= 0) return;

        const updateTimer = () => {
            const now = new Date();
            const elapsed = Math.floor((now - this.startTime) / 1000);
            const remaining = this.timeLimit * 60 - elapsed;

            if (remaining <= 0) {
                clearInterval(this.timerInterval);
                this.showError('Hết thời gian làm bài!');
                this.submitQuiz();
                return;
            }

            const minutes = Math.floor(remaining / 60);
            const seconds = remaining % 60;
            const timeDisplay = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            const timeRemainingElement = document.getElementById('timeRemaining');
            if (timeRemainingElement) {
                timeRemainingElement.textContent = timeDisplay;
            }
        };

        updateTimer();
        this.timerInterval = setInterval(updateTimer, 1000);
    }

    bindEvents() {
        // Navigation events - sử dụng data attributes thay vì onclick
        document.addEventListener('click', (e) => {
            const prevButton = e.target.closest('[data-action="previous-question"]');
            if (prevButton) {
                e.preventDefault();
                e.stopPropagation();
                this.previousQuestion();
            }
            
            const nextButton = e.target.closest('[data-action="next-question"]');
            if (nextButton) {
                e.preventDefault();
                e.stopPropagation();
                this.nextQuestion();
            }
            
            const submitButton = e.target.closest('[data-action="submit-quiz"]');
            if (submitButton && !this.isSubmitting) {
                e.preventDefault();
                e.stopPropagation();
                this.submitQuiz();
            }
            
            const exitAndSubmitButton = e.target.closest('[data-action="exit-and-submit"]');
            if (exitAndSubmitButton && !this.isSubmitting) {
                e.preventDefault();
                e.stopPropagation();
                this.exitAndSubmit();
            }
            
            const exitButton = e.target.closest('[data-action="exit-quiz"]');
            if (exitButton) {
                e.preventDefault();
                e.stopPropagation();
                this.exitQuiz();
            }
        });
    }

    setupKeyboardNavigation() {
        document.addEventListener('keydown', (e) => {
            switch (e.key) {
                case 'ArrowLeft':
                    e.preventDefault();
                    this.previousQuestion();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    this.nextQuestion();
                    break;
                case 'Enter':
                    e.preventDefault();
                    // Cho phép nộp bài từ bất kỳ câu hỏi nào
                    this.submitQuiz();
                    break;
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                    e.preventDefault();
                    const choiceIndex = parseInt(e.key) - 1;
                    const choices = document.querySelectorAll('.choice-item');
                    if (choices[choiceIndex]) {
                        choices[choiceIndex].click();
                    }
                    break;
            }
        });
    }

    previousQuestion() {
        if (this.currentQuestionIndex > 0) {
            this.showQuestion(this.currentQuestionIndex - 1);
        }
    }

    nextQuestion() {
        if (this.currentQuestionIndex < this.questions.length - 1) {
            this.showQuestion(this.currentQuestionIndex + 1);
        }
    }

    goToQuestion(index) {
        this.showQuestion(index);
    }

    async submitQuiz() {
        if (this.isSubmitting) {
            console.log('Already submitting, ignoring request');
            return;
        }

        // Kiểm tra xem có câu hỏi nào chưa trả lời không
        const unansweredCount = this.questions.length - Object.keys(this.answers).length;
        let confirmMessage = 'Bạn có chắc chắn muốn nộp bài?';
        
        if (unansweredCount > 0) {
            confirmMessage = `Bạn còn ${unansweredCount} câu hỏi chưa trả lời. Bạn có chắc chắn muốn nộp bài?`;
        }

        // Kiểm tra có câu hỏi tự luận không
        const essayQuestions = this.questions.filter(q => q.questionType === 'Essay').length;
        if (essayQuestions > 0) {
            confirmMessage += `\n\nLưu ý: Quiz này có ${essayQuestions} câu hỏi tự luận sẽ được chấm điểm thủ công.`;
        }

        if (!confirm(confirmMessage)) {
            return;
        }

        this.isSubmitting = true;

        try {
            // Stop timer
            if (this.timerInterval) {
                clearInterval(this.timerInterval);
            }

            const endTime = new Date();
            console.log('Submit - startTime:', this.startTime);
            console.log('Submit - endTime:', endTime);
            
            // Đảm bảo startTime có giá trị hợp lệ
            if (!this.startTime || isNaN(this.startTime.getTime())) {
                console.warn('Invalid startTime, using current time - 60 seconds');
                this.startTime = new Date(endTime.getTime() - 60000); // Giả sử làm bài 1 phút
            }
            
            const duration = Math.floor((endTime - this.startTime) / 1000);
            console.log('Submit - calculated duration:', duration);

            // Calculate score
            const score = this.calculateScore();
            const essayQuestions = this.questions.filter(q => q.questionType === 'Essay').length;

            // Prepare submission data - chuyển đổi index thành QuestionId
            const answersWithQuestionId = {};
            for (let i = 0; i < this.questions.length; i++) {
                if (this.answers[i] && String(this.answers[i]).trim() !== '') {
                    const questionId = this.questions[i].questionId;
                    answersWithQuestionId[questionId] = this.answers[i];
                }
            }
            
            const submissionData = {
                quizId: parseInt(this.quizId),
                answers: answersWithQuestionId,
                duration: duration,
                score: score
            };

            // Submit to server
            const response = await fetch('/Quiz/SubmitAttempt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(submissionData)
            });

            if (response.ok) {
                const result = await response.json();
                console.log('Submit result:', result); // Debug log
                console.log('Response status:', response.status); // Debug log
                console.log('Response headers:', response.headers); // Debug log
                
                let successMessage = 'Nộp bài thành công!';
                
                if (typeof showScoreAfterSubmit !== 'undefined' && showScoreAfterSubmit) {
                    if (essayQuestions > 0) {
                        successMessage += ` Điểm tự động: ${score.toFixed(1)}% (${essayQuestions} câu tự luận sẽ được chấm thủ công)`;
                    } else {
                        successMessage += ` Điểm của bạn: ${score.toFixed(1)}%`;
                    }
                }
                
                // Xóa trạng thái đã lưu
                sessionStorage.removeItem('quizState');
                
                // Không hiển thị thông báo để tránh chặn chuyển hướng
                console.log('Submit successful, redirecting immediately...');
                console.log('Current URL:', window.location.href);
                console.log('Result object:', result);
                
                // Chuyển hướng ngay lập tức đến trang kết quả
                if (result.attemptId) {
                    console.log('Redirecting to result page...'); // Debug log
                    console.log('AttemptId:', result.attemptId); // Debug log
                    console.log('QuizId:', this.quizId); // Debug log
                    
                    // Chuyển hướng đơn giản
                    const resultUrl = `/Quiz/Result?attemptId=${result.attemptId}&quizId=${this.quizId}`;
                    console.log('Result URL:', resultUrl); // Debug log
                    
                    // Hiển thị thông báo và chuyển hướng
                    this.showSuccess('Nộp bài thành công! Đang chuyển hướng đến trang kết quả...');
                    
                    // Chuyển hướng sau 1 giây
                    setTimeout(() => {
                        console.log('Executing redirect to:', resultUrl);
                        try {
                            window.location.href = resultUrl;
                        } catch (error) {
                            console.error('Redirect error:', error);
                            // Fallback: tạo link và click
                            const link = document.createElement('a');
                            link.href = resultUrl;
                            link.style.display = 'none';
                            document.body.appendChild(link);
                            link.click();
                        }
                    }, 1000);
                } else {
                    console.error('No attemptId in result:', result); // Debug log
                    this.showError('Có lỗi xảy ra khi nộp bài');
                }
            } else if (response.status === 401) {
                // Unauthorized - user not logged in
                this.showError('Bạn cần đăng nhập để nộp bài. Đang chuyển hướng...');
                setTimeout(() => {
                    window.location.href = '/Account/Login';
                }, 2000);
            } else {
                const error = await response.json();
                this.showError(error.message || 'Có lỗi xảy ra khi nộp bài');
            }
        } catch (error) {
            console.error('Error submitting quiz:', error);
            this.showError('Lỗi khi nộp bài');
        } finally {
            this.isSubmitting = false;
        }
    }

    calculateScore() {
        let correctAnswers = 0;
        let totalQuestions = this.questions.length;
        let essayQuestions = 0;

        this.questions.forEach((question, index) => {
            const userAnswer = this.answers[index];
            
            // Skip essay questions from automatic scoring
            if (question.questionType === 'Essay') {
                essayQuestions++;
                return;
            }
            
            if (!userAnswer || !userAnswer.trim()) {
                return; // Skip unanswered questions
            }

            const isCorrect = this.checkAnswer(question, userAnswer.trim());
            if (isCorrect) {
                correctAnswers++;
            }
        });

        // Calculate score only for non-essay questions
        const nonEssayQuestions = totalQuestions - essayQuestions;
        if (nonEssayQuestions === 0) {
            return 0; // All questions are essays
        }
        
        return (correctAnswers / nonEssayQuestions) * 100;
    }

    checkAnswer(question, userAnswer) {
        const correctAnswer = question.answerKey.trim();
        
        // If no answer key, treat as essay question
        if (!correctAnswer) {
            return false;
        }
        
        switch (question.questionType) {
            case 'MultipleChoice':
                // For multiple choice, compare exact text
                return userAnswer.toLowerCase() === correctAnswer.toLowerCase();
                
            case 'TrueFalse':
                // For true/false, normalize and compare
                const normalizedUser = this.normalizeTrueFalse(userAnswer);
                const normalizedCorrect = this.normalizeTrueFalse(correctAnswer);
                return normalizedUser === normalizedCorrect;
                
            case 'ShortAnswer':
                // For short answer, compare exact text (case-insensitive)
                return userAnswer.toLowerCase() === correctAnswer.toLowerCase();
                
            case 'Essay':
                // Essay questions are not automatically scored
                return false;
                
            default:
                return userAnswer.toLowerCase() === correctAnswer.toLowerCase();
        }
    }

    normalizeTrueFalse(answer) {
        const normalized = answer.toLowerCase().trim();
        if (normalized === 'true' || normalized === 'đúng' || normalized === 'yes' || normalized === '1') {
            return 'đúng';
        }
        if (normalized === 'false' || normalized === 'sai' || normalized === 'no' || normalized === '0') {
            return 'sai';
        }
        return normalized;
    }

    getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    showError(message) {
        this.showNotification(message, 'error');
    }

    showNotification(message, type = 'info') {
        // Remove existing notifications first
        const existingNotifications = document.querySelectorAll('.alert.position-fixed');
        existingNotifications.forEach(notification => notification.remove());

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

    renderMarkButton() {
        let markBtn = document.getElementById('markQuestionBtn');
        if (!markBtn) {
            markBtn = document.createElement('button');
            markBtn.id = 'markQuestionBtn';
            markBtn.className = 'btn btn-warning btn-mark-question';
            markBtn.style = 'margin-top: 1rem; margin-bottom: 1rem;';
            markBtn.onclick = () => this.toggleMarkQuestion();
            document.getElementById('questionContent').parentElement.appendChild(markBtn);
        }
        markBtn.textContent = this.markedQuestions.has(this.currentQuestionIndex) ? 'Bỏ đánh dấu (chưa chắc chắn)' : 'Đánh dấu (chưa chắc chắn)';
    }

    toggleMarkQuestion() {
        if (this.markedQuestions.has(this.currentQuestionIndex)) {
            this.markedQuestions.delete(this.currentQuestionIndex);
        } else {
            this.markedQuestions.add(this.currentQuestionIndex);
        }
        this.renderMarkButton();
        this.updateQuestionIndicators();
    }

    // Thêm method để setup auto-submit handlers
    setupAutoSubmitHandlers() {
        // Beforeunload event - cảnh báo khi người dùng cố gắng thoát
        window.addEventListener('beforeunload', (e) => {
            if (!this.isSubmitting && !this.isAutoSubmitting && Object.keys(this.answers).length > 0) {
                e.preventDefault();
                e.returnValue = 'Bạn có chắc muốn thoát? Bài làm của bạn sẽ được tự động nộp.';
                return 'Bạn có chắc muốn thoát? Bài làm của bạn sẽ được tự động nộp.';
            }
        });

        // Visibility change event - phát hiện khi tab bị ẩn
        document.addEventListener('visibilitychange', () => {
            if (document.hidden && !this.isSubmitting && !this.isAutoSubmitting) {
                this.handlePageHidden();
            }
        });

        // Page unload event - tự động nộp bài khi trang bị đóng
        window.addEventListener('unload', () => {
            if (!this.isSubmitting && !this.isAutoSubmitting && Object.keys(this.answers).length > 0) {
                this.autoSubmitQuiz();
            }
        });

        // Focus event - phát hiện khi tab được focus lại
        window.addEventListener('focus', () => {
            if (this.hasShownWarning) {
                this.showNotification('Bạn đã quay lại trang làm bài. Hãy tiếp tục làm bài hoặc nộp bài.', 'info');
                this.hasShownWarning = false;
            }
        });
    }

    // Xử lý khi trang bị ẩn
    handlePageHidden() {
        if (Object.keys(this.answers).length > 0 && !this.hasShownWarning) {
            this.hasShownWarning = true;
            // Lưu trạng thái hiện tại
            this.saveCurrentState();
        }
    }

    // Lưu trạng thái hiện tại
    saveCurrentState() {
        try {
            const state = {
                quizId: this.quizId,
                answers: this.answers,
                currentQuestionIndex: this.currentQuestionIndex,
                startTime: this.startTime ? this.startTime.toISOString() : null,
                timeLimit: this.timeLimit,
                markedQuestions: Array.from(this.markedQuestions)
            };
            sessionStorage.setItem('quizState', JSON.stringify(state));
        } catch (error) {
            console.error('Error saving quiz state:', error);
        }
    }

    // Khôi phục trạng thái từ session storage
    restoreState() {
        try {
            const savedState = sessionStorage.getItem('quizState');
            if (savedState) {
                const state = JSON.parse(savedState);
                if (state.quizId == this.quizId) {
                    this.answers = state.answers || {};
                    this.currentQuestionIndex = state.currentQuestionIndex || 0;
                    this.startTime = state.startTime ? new Date(state.startTime) : new Date();
                    this.markedQuestions = new Set(state.markedQuestions || []);
                    
                    // Cập nhật UI
                    this.showQuestion(this.currentQuestionIndex);
                    this.updateQuestionIndicators();
                    this.updateProgress();
                    
                    this.showNotification('Đã khôi phục trạng thái làm bài trước đó.', 'info');
                    return true;
                }
            }
        } catch (error) {
            console.error('Error restoring quiz state:', error);
        }
        return false;
    }

    // Tự động nộp bài
    async autoSubmitQuiz() {
        if (this.isAutoSubmitting || this.isSubmitting) return;
        
        this.isAutoSubmitting = true;
        
        try {
            // Gửi request nộp bài
            console.log('Auto-submit - startTime:', this.startTime);
            const currentTime = new Date();
            console.log('Auto-submit - current time:', currentTime);
            
            // Đảm bảo startTime có giá trị hợp lệ
            if (!this.startTime || isNaN(this.startTime.getTime())) {
                console.warn('Auto-submit: Invalid startTime, using current time - 60 seconds');
                this.startTime = new Date(currentTime.getTime() - 60000); // Giả sử làm bài 1 phút
            }
            
            const duration = Math.floor((currentTime - this.startTime) / 1000);
            console.log('Auto-submit - calculated duration:', duration);
            const score = this.calculateScore();
            
            // Chuyển đổi index thành QuestionId cho auto-submit
            const answersWithQuestionId = {};
            for (let i = 0; i < this.questions.length; i++) {
                if (this.answers[i] && String(this.answers[i]).trim() !== '') {
                    const questionId = this.questions[i].questionId;
                    answersWithQuestionId[questionId] = this.answers[i];
                }
            }
            
            const submission = {
                quizId: this.quizId,
                answers: answersWithQuestionId,
                duration: duration,
                score: score
            };

            const response = await fetch('/Quiz/SubmitAttempt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(submission)
            });

            if (response.ok) {
                const result = await response.json();
                console.log('Auto-submitted quiz successfully:', result);
                
                // Xóa trạng thái đã lưu
                sessionStorage.removeItem('quizState');
                
                // Chuyển hướng đến trang kết quả
                const resultUrl = `/Quiz/Result?attemptId=${result.attemptId}&quizId=${this.quizId}`;
                console.log('Auto-submit redirecting to:', resultUrl);
                
                // Hiển thị thông báo và chuyển hướng
                this.showSuccess('Tự động nộp bài thành công! Đang chuyển hướng đến trang kết quả...');
                
                // Chuyển hướng sau 1 giây
                setTimeout(() => {
                    window.location.href = resultUrl;
                }, 1000);
            } else {
                console.error('Auto-submit failed:', response.status);
            }
        } catch (error) {
            console.error('Error auto-submitting quiz:', error);
        } finally {
            this.isAutoSubmitting = false;
        }
    }

    // Hiển thị dialog xác nhận nộp bài
    showSubmitConfirmationDialog() {
        const answeredCount = Object.keys(this.answers).length;
        const totalQuestions = this.questions.length;
        
        // Tạo modal dialog
        const modal = document.createElement('div');
        modal.className = 'quiz-confirmation-modal';
        modal.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.5);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
            backdrop-filter: blur(5px);
        `;
        
        const modalContent = document.createElement('div');
        modalContent.style.cssText = `
            background: var(--quiz-bg-card);
            border: 1px solid var(--quiz-border-primary);
            border-radius: var(--quiz-radius-lg);
            padding: var(--quiz-spacing-xl);
            max-width: 500px;
            width: 90%;
            text-align: center;
            box-shadow: var(--quiz-shadow-xl);
        `;
        
        modalContent.innerHTML = `
            <div style="margin-bottom: var(--quiz-spacing-lg);">
                <i class="bi bi-question-circle" style="font-size: 3rem; color: var(--quiz-warning);"></i>
            </div>
            <h3 style="margin-bottom: var(--quiz-spacing-md); color: var(--quiz-text-primary);">
                Xác nhận hành động
            </h3>
            <p style="margin-bottom: var(--quiz-spacing-lg); color: var(--quiz-text-secondary);">
                Bạn đã trả lời <strong>${answeredCount}/${totalQuestions}</strong> câu hỏi.
            </p>
            <div style="display: flex; gap: var(--quiz-spacing-md); justify-content: center; flex-wrap: wrap;">
                <button class="btn btn-outline-secondary" id="continueBtn">
                    <i class="bi bi-arrow-left"></i>
                    Tiếp tục làm bài
                </button>
                <button class="btn btn-success" id="submitBtn">
                    <i class="bi bi-check-circle"></i>
                    Nộp bài ngay
                </button>
            </div>
        `;
        
        modal.appendChild(modalContent);
        document.body.appendChild(modal);
        
        // Xử lý các nút
        modal.querySelector('#continueBtn').onclick = () => {
            document.body.removeChild(modal);
        };
        
        modal.querySelector('#submitBtn').onclick = () => {
            document.body.removeChild(modal);
            this.submitQuiz();
        };
        
        // Đóng modal khi click bên ngoài
        modal.onclick = (e) => {
            if (e.target === modal) {
                document.body.removeChild(modal);
            }
        };
        
        // Đóng modal khi nhấn ESC
        const handleEsc = (e) => {
            if (e.key === 'Escape') {
                document.body.removeChild(modal);
                document.removeEventListener('keydown', handleEsc);
            }
        };
        document.addEventListener('keydown', handleEsc);
    }

    // Thêm method để thoát và nộp bài
    async exitAndSubmit() {
        const answeredCount = Object.keys(this.answers).length;
        const totalQuestions = this.questions.length;
        
        const confirmMessage = `Bạn đã trả lời ${answeredCount}/${totalQuestions} câu hỏi.\n\nBạn có chắc muốn thoát và nộp bài ngay bây giờ?`;
        
        if (confirm(confirmMessage)) {
            await this.submitQuiz();
            // Sau khi nộp bài thành công, sẽ được chuyển hướng trong submitQuiz
        }
    }

    // Thêm method để thoát mà không nộp bài
    exitQuiz() {
        const answeredCount = Object.keys(this.answers).length;
        const totalQuestions = this.questions.length;
        
        let confirmMessage = 'Bạn có chắc muốn thoát?';
        if (answeredCount > 0) {
            confirmMessage = `Bạn đã trả lời ${answeredCount}/${totalQuestions} câu hỏi.\n\nBạn có chắc muốn thoát mà không nộp bài?`;
        }
        
        if (confirm(confirmMessage)) {
            // Xóa trạng thái đã lưu
            sessionStorage.removeItem('quizState');
            
            // Chuyển hướng về Quiz Index
            window.location.href = '/Quiz/Index';
        }
    }
}

// Initialize the manager
const quizTakeManager = new QuizTakeManager();

// Global functions for onclick handlers
function previousQuestion() {
    quizTakeManager.previousQuestion();
}

function nextQuestion() {
    quizTakeManager.nextQuestion();
}

function submitQuiz() {
    quizTakeManager.submitQuiz();
} 