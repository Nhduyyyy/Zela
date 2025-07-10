using Microsoft.AspNetCore.SignalR;
using Zela.Services;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Zela.Hubs
{
    public class QuizHub : Hub
    {
        private readonly IQuizService _quizService;
        public QuizHub(IQuizService quizService)
        {
            _quizService = quizService;
        }

        // Học sinh nhập tên, join vào phòng quiz (group)
        public async Task JoinQuizRoom(string roomCode, string displayName)
        {
            // Lưu tên tạm thời vào Context.Items hoặc session nếu cần
            Context.Items["DisplayName"] = displayName;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            // Broadcast thông báo có người mới vào phòng (nếu muốn)
            await Clients.Group(roomCode).SendAsync("UserJoined", displayName);
        }

        // Giáo viên bắt đầu quiz, gửi câu hỏi đầu tiên
        public async Task StartQuiz(string roomCode, int quizId)
        {
            // Lấy câu hỏi đầu tiên từ service
            var question = _quizService.GetFirstQuestion(quizId);
            await Clients.Group(roomCode).SendAsync("ReceiveQuestion", question);
        }

        // Giáo viên chuyển sang câu hỏi tiếp theo
        public async Task NextQuestion(string roomCode, int quizId, int currentQuestionId)
        {
            var question = _quizService.GetNextQuestion(quizId, currentQuestionId);
            await Clients.Group(roomCode).SendAsync("ReceiveQuestion", question);
        }

        // Học sinh gửi đáp án
        public async Task SubmitAnswer(string roomCode, int quizId, int questionId, string answer, string displayName)
        {
            // Gọi service để lưu đáp án, tính điểm, thời gian
            var result = _quizService.SubmitRealtimeAnswer(roomCode, quizId, questionId, answer, displayName);
            // Có thể gửi lại kết quả tạm thời cho học sinh nếu muốn
            await Clients.Caller.SendAsync("AnswerResult", result);
        }

        // Gửi bảng xếp hạng cho tất cả client trong phòng
        public async Task ShowLeaderboard(string roomCode, int quizId)
        {
            var leaderboard = _quizService.GetRealtimeLeaderboard(roomCode, quizId);
            await Clients.Group(roomCode).SendAsync("ReceiveLeaderboard", leaderboard);
        }

        // Kết thúc quiz, gửi kết quả cuối cùng
        public async Task EndQuiz(string roomCode, int quizId)
        {
            _quizService.SaveQuizRoomHistory(roomCode, quizId);
            var leaderboard = _quizService.GetRealtimeLeaderboard(roomCode, quizId);
            await Clients.Group(roomCode).SendAsync("QuizEnded", leaderboard);
        }
    }
} 