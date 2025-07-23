let connection = null;
let roomCode = '';
let displayName = '';
let quizId = null;
let currentQuestionId = null;
window.quizStatus = 'waiting'; // 'waiting', 'started', 'ended'

function showSection(id) {
    document.getElementById('joinRoomSection').style.display = id === 'join' ? '' : 'none';
    document.getElementById('quizContentSection').style.display = id === 'quiz' ? '' : 'none';
}

async function startSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/quizHub')
        .build();

    // Khi có người mới vào phòng
    connection.on('UserJoined', name => {
        // Có thể cập nhật UI nếu muốn
        console.log(`${name} đã vào phòng!`);
    });

    // Nhận câu hỏi từ giáo viên
    connection.on('ReceiveQuestion', question => {
        renderQuestion(question);
    });

    // Nhận kết quả gửi đáp án
    connection.on('AnswerResult', result => {
        if (result.Success) {
            alert(result.IsCorrect ? 'Đúng!' : 'Sai!');
        } else {
            alert(result.Message || 'Có lỗi khi gửi đáp án');
        }
    });

    // Nhận bảng xếp hạng
    connection.on('ReceiveLeaderboard', leaderboard => {
        renderLeaderboard(leaderboard);
    });

    // Quiz kết thúc
    connection.on('QuizEnded', leaderboard => {
        renderLeaderboard(leaderboard);
        alert('Quiz đã kết thúc!');
        if (window.isTeacher && document.getElementById('exportResultBtn')) {
            document.getElementById('exportResultBtn').style.display = '';
        }
    });

    await connection.start();
}

function renderQuestion(question) {
    if (!question) return;
    currentQuestionId = question.questionId || question.QuestionId;
    document.getElementById('questionNumber').innerText = '?';
    document.getElementById('questionText').innerText = question.content || question.Content;
    document.getElementById('questionType').innerText = question.questionType || question.QuestionType;
    // Render choices
    const contentDiv = document.getElementById('questionContent');
    contentDiv.innerHTML = '';
    if ((question.questionType || question.QuestionType) === 'MultipleChoice') {
        let choices = question.choices || question.Choices;
        if (typeof choices === 'string') {
            try { choices = JSON.parse(choices); } catch { choices = choices.split('|'); }
        }
        if (Array.isArray(choices)) {
            choices.forEach((c, idx) => {
                const id = 'choice_' + idx;
                contentDiv.innerHTML += `<div class='form-check'><input class='form-check-input' type='radio' name='answer' id='${id}' value='${c}'><label class='form-check-label' for='${id}'>${c}</label></div>`;
            });
        }
    } else if ((question.questionType || question.QuestionType) === 'TrueFalse') {
        contentDiv.innerHTML = `<div class='form-check'><input class='form-check-input' type='radio' name='answer' id='true' value='Đúng'><label class='form-check-label' for='true'>Đúng</label></div><div class='form-check'><input class='form-check-input' type='radio' name='answer' id='false' value='Sai'><label class='form-check-label' for='false'>Sai</label></div>`;
    } else {
        contentDiv.innerHTML = `<input class='form-control' type='text' name='answer' id='answerInput' placeholder='Nhập đáp án...' />`;
    }
    document.getElementById('submitAnswerBtn').style.display = '';
}

function renderLeaderboard(leaderboard) {
    const lbDiv = document.getElementById('leaderboard');
    lbDiv.innerHTML = '';
    if (!leaderboard || leaderboard.length === 0) {
        lbDiv.innerHTML = '<em>Chưa có dữ liệu</em>';
        return;
    }
    lbDiv.innerHTML = `<table class='table table-sm'><thead><tr><th>Hạng</th><th>Tên</th><th>Đúng</th><th>Thời gian</th></tr></thead><tbody>` +
        leaderboard.map((p, i) => `<tr><td>${i+1}</td><td>${p.Name || p.name}</td><td>${p.Correct}</td><td>${(p.TotalTime||0).toFixed(2)}s</td></tr>`).join('') +
        '</tbody></table>';
}

// Sự kiện join phòng
if (document.getElementById('joinRoomBtn')) {
    document.getElementById('joinRoomBtn').onclick = async function() {
        displayName = document.getElementById('displayName').value.trim();
        roomCode = document.getElementById('roomCode').value.trim();
        if (!displayName || !roomCode) {
            alert('Vui lòng nhập tên và mã phòng!');
            return;
        }
        await startSignalR();
        await connection.invoke('JoinQuizRoom', roomCode, displayName);
        showSection('quiz');
    };
}

// Sự kiện gửi đáp án
if (document.getElementById('submitAnswerBtn')) {
    document.getElementById('submitAnswerBtn').onclick = async function() {
        let answer = '';
        const radios = document.querySelectorAll('input[name="answer"]:checked');
        if (radios.length > 0) {
            answer = radios[0].value;
        } else if (document.getElementById('answerInput')) {
            answer = document.getElementById('answerInput').value;
        }
        if (!answer) {
            alert('Vui lòng chọn hoặc nhập đáp án!');
            return;
        }
        await connection.invoke('SubmitAnswer', roomCode, quizId, currentQuestionId, answer, displayName);
        document.getElementById('submitAnswerBtn').style.display = 'none';
    };
}

// Giáo viên điều khiển quiz
if (window.isTeacher) {
    document.addEventListener('DOMContentLoaded', function() {
        if (document.getElementById('teacherControls')) {
            // Tự động join phòng với tên "Teacher"
            startSignalR().then(() => {
                connection.invoke('JoinQuizRoom', window.roomCode, 'Teacher');
            });
        }
        if (document.getElementById('startQuizBtn')) {
            document.getElementById('startQuizBtn').onclick = async function() {
                updateQuizStatus('started');
                startBtn.style.display = 'none';
                nextBtn.style.display = '';
                endBtn.style.display = '';
                // TODO: Gửi tín hiệu SignalR cho học sinh quiz bắt đầu
            };
        }
        if (document.getElementById('nextQuestionBtn')) {
            document.getElementById('nextQuestionBtn').onclick = async function() {
                await connection.invoke('NextQuestion', window.roomCode, window.quizId, currentQuestionId);
            };
        }
        if (document.getElementById('endQuizBtn')) {
            document.getElementById('endQuizBtn').onclick = async function() {
                updateQuizStatus('ended');
                nextBtn.style.display = 'none';
                endBtn.style.display = 'none';
                // TODO: Gửi tín hiệu SignalR quiz kết thúc
            };
        }
        // Hiển thị trạng thái ban đầu
        updateQuizStatus('waiting');
    });
}
// Ẩn controls giáo viên với học sinh
if (!window.isTeacher && document.getElementById('teacherControls')) {
    document.getElementById('teacherControls').style.display = 'none';
}

// Hàm khởi tạo quizId nếu cần (có thể truyền từ server hoặc lấy từ URL)
// quizId = ...;

function initQuizRealtimePanel() {
    // Gán lại biến toàn cục từ window (nếu có)
    quizId = window.quizId;
    roomCode = window.roomCode;
    // Giáo viên điều khiển quiz
    if (window.isTeacher) {
        if (document.getElementById('teacherControls')) {
            // Tự động join phòng với tên "Teacher"
            startSignalR().then(() => {
                connection.invoke('JoinQuizRoom', window.roomCode, 'Teacher');
            });
        }
        if (document.getElementById('startQuizBtn')) {
            document.getElementById('startQuizBtn').onclick = async function() {
                updateQuizStatus('started');
                startBtn.style.display = 'none';
                nextBtn.style.display = '';
                endBtn.style.display = '';
                // TODO: Gửi tín hiệu SignalR cho học sinh quiz bắt đầu
            };
        }
        if (document.getElementById('nextQuestionBtn')) {
            document.getElementById('nextQuestionBtn').onclick = async function() {
                await connection.invoke('NextQuestion', window.roomCode, window.quizId, currentQuestionId);
            };
        }
        if (document.getElementById('endQuizBtn')) {
            document.getElementById('endQuizBtn').onclick = async function() {
                updateQuizStatus('ended');
                nextBtn.style.display = 'none';
                endBtn.style.display = 'none';
                // TODO: Gửi tín hiệu SignalR quiz kết thúc
            };
        }
    }
    // Ẩn controls giáo viên với học sinh
    if (!window.isTeacher && document.getElementById('teacherControls')) {
        document.getElementById('teacherControls').style.display = 'none';
    }
}

function updateQuizStatus(status) {
    window.quizStatus = status;
    const badge = document.getElementById('quizStatusBadge');
    if (!badge) return;
    if (status === 'waiting') {
        badge.textContent = 'Đang chờ giáo viên bắt đầu';
        badge.className = 'badge badge-warning';
    } else if (status === 'started') {
        badge.textContent = 'Quiz đang diễn ra';
        badge.className = 'badge badge-success';
    } else if (status === 'ended') {
        badge.textContent = 'Quiz đã kết thúc';
        badge.className = 'badge badge-secondary';
    }
}
window.updateQuizStatus = updateQuizStatus;

document.addEventListener('DOMContentLoaded', function() {
    // Room code copy
    const copyBtn = document.getElementById('copyRoomCodeBtn');
    if (copyBtn) {
        copyBtn.onclick = function() {
            const code = document.getElementById('roomCodeDisplay').textContent;
            navigator.clipboard.writeText(code);
            copyBtn.textContent = 'Đã sao chép!';
            setTimeout(() => { copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> Sao chép'; }, 1200);
        };
    }
    // Teacher controls
    if (window.isTeacher) {
        const startBtn = document.getElementById('startQuizBtn');
        const nextBtn = document.getElementById('nextQuestionBtn');
        const endBtn = document.getElementById('endQuizBtn');
        if (startBtn) {
            startBtn.onclick = function() {
                updateQuizStatus('started');
                startBtn.style.display = 'none';
                nextBtn.style.display = '';
                endBtn.style.display = '';
                // TODO: Gửi tín hiệu SignalR cho học sinh quiz bắt đầu
            };
        }
        if (endBtn) {
            endBtn.onclick = function() {
                updateQuizStatus('ended');
                nextBtn.style.display = 'none';
                endBtn.style.display = 'none';
                // TODO: Gửi tín hiệu SignalR quiz kết thúc
            };
        }
    }
    // Hiển thị trạng thái ban đầu
    updateQuizStatus('waiting');
}); 