# ZELA PROJECT - WORKFLOW ANALYSIS

## TỔNG QUAN DỰ ÁN

Zela là một ứng dụng web ASP.NET Core MVC tích hợp nhiều tính năng giao tiếp và học tập:
- **Chat 1-1 & Nhóm**: Giao tiếp real-time với bạn bè
- **Video Call**: Họp trực tuyến với nhiều tính năng nâng cao
- **Quiz System**: Hệ thống kiểm tra trực tuyến
- **Whiteboard**: Bảng vẽ tương tác
- **File Management**: Quản lý và chia sẻ file
- **Payment Integration**: Tích hợp thanh toán PayOS

## KIẾN TRÚC TỔNG QUAN

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
├─────────────────────────────────────────────────────────────┤
│  Controllers/         │  Views/        │  wwwroot/          │
│  - ChatController     │  - Chat/       │  - css/            │
│  - MeetingController  │  - Meeting/    │  - js/             │
│  - QuizController     │  - Quiz/       │  - images/         │
│  - GroupChatController│  - Admin/      │  - sticker/        │
└─────────────────────────────────────────────────────────────┘
                              │
┌───────────────────────────────────────────────────────────────────┐
│                    BUSINESS LOGIC LAYER                           │
├───────────────────────────────────────────────────────────────────┤
│  Services/          │  Hubs/          │  Middleware/              │
│  - ChatService      │  - ChatHub      │  - PremiumStatusMiddleware│
│  - MeetingService   │  - MeetingHub   │                           │
│  - QuizService      │  - QuizHub      │                           │
│  - FileUploadService│  - WhiteboardHub│                           │
└───────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    DATA ACCESS LAYER                        │
├─────────────────────────────────────────────────────────────┤
│  Models/          │  DbContext/            │  Migrations/   │
│  - User           │  - ApplicationDbContext│                │
│  - Message        │                        │                │
│  - VideoRoom      │                        │                │
│  - Quiz           │                        │                │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. WORKFLOW CHAT 1-1

### 1.1 Kiến trúc Chat 1-1

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Server    │    │  Database   │
│  (Browser)  │    │ (ASP.NET)   │    │  (SQL Server)│
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │ 1. Load Chat Page │                   │
       │──────────────────▶│                   │
       │                   │ 2. Get Friends    │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 3. Friends List   │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 4. Select Friend  │                   │
       │──────────────────▶│                   │
       │                   │ 5. Get Messages   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 6. Messages       │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 7. Send Message   │                   │
       │──────────────────▶│                   │
       │                   │ 8. Save Message   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 9. SignalR Push   │                   │
       │◀──────────────────│                   │
```

### 1.2 Chi tiết Workflow

#### **Bước 1: Khởi tạo Chat**
```
1. User truy cập /Chat/Index
2. ChatController.Index() được gọi
3. Lấy userId từ Session: HttpContext.Session.GetInt32("UserId")
4. Gọi ChatService.GetFriendListAsync(userId)
5. Trả về View với danh sách bạn bè
```

#### **Bước 2: Chọn bạn chat**
```
1. User click vào một friend
2. JavaScript gọi AJAX: /Chat/GetMessages?friendId={id}
3. ChatController.GetMessages(friendId) được gọi
4. Lấy tin nhắn từ ChatService.GetMessagesAsync(userId, friendId)
5. Trả về PartialView "_ChatMessagesPartial" với lịch sử chat
```

#### **Bước 3: Gửi tin nhắn**
```
1. User nhập tin nhắn và click Send
2. JavaScript gọi AJAX POST: /Chat/SendMessage
3. ChatController.SendMessage() được gọi với:
   - recipientId: ID người nhận
   - content: Nội dung tin nhắn
   - files: Danh sách file (nếu có)
   - replyToMessageId: ID tin nhắn reply (nếu có)

4. ChatService.SendMessageAsync() xử lý:
   - Lưu tin nhắn vào database
   - Upload file (nếu có)
   - Trả về MessageViewModel

5. SignalR Hub gửi real-time:
   - Gửi cho sender: Clients.User(senderId).SendAsync("ReceiveMessage")
   - Gửi cho recipient: Clients.User(recipientId).SendAsync("ReceiveMessage")
```

#### **Bước 4: Nhận tin nhắn real-time**
```
1. SignalR Hub nhận event "ReceiveMessage"
2. JavaScript cập nhật UI:
   - Thêm tin nhắn mới vào chat window
   - Hiển thị notification
   - Cập nhật message status (Sent → Delivered → Seen)
```

### 1.3 Các tính năng bổ sung

#### **Sticker System**
```
1. User click vào sticker
2. JavaScript gọi: /Chat/GetStickers
3. Trả về danh sách sticker categories
4. User chọn sticker → gửi qua SignalR
5. ChatHub.SendSticker() xử lý và broadcast
```

#### **File Sharing**
```
1. User chọn file để upload
2. JavaScript upload file qua FormData
3. FileUploadService xử lý:
   - Validate file type/size
   - Save vào wwwroot/uploads/
   - Tạo Media record trong database
4. Gửi tin nhắn với file attachment
```

#### **Message Reactions**
```
1. User hover tin nhắn → hiển thị reaction options
2. Click reaction → AJAX POST: /Chat/AddReaction
3. Lưu reaction vào MessageReaction table
4. SignalR broadcast reaction cho tất cả users
```

---

## 2. WORKFLOW CHAT NHÓM

### 2.1 Kiến trúc Chat Nhóm

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Server    │    │  Database   │
│  (Browser)  │    │ (ASP.NET)   │    │  (SQL Server)│
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │ 1. Load Groups    │                   │
       │──────────────────▶│                   │
       │                   │ 2. Get User Groups│
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 3. Groups List    │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 4. Join Group     │                   │
       │──────────────────▶│                   │
       │                   │ 5. SignalR Join   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 6. Group Messages │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 7. Send Group Msg │                   │
       │──────────────────▶│                   │
       │                   │ 8. Broadcast      │
       │                   │──────────────────▶│
       │ 9. All Members    │                   │
       │◀──────────────────│                   │
```

### 2.2 Chi tiết Workflow

#### **Bước 1: Tạo nhóm chat**
```
1. User click "Create Group"
2. Form submit: /GroupChat/CreateGroup
3. GroupChatController.CreateGroup() xử lý:
   - Validate input (name, description, avatar, password)
   - Upload avatar (nếu có)
   - Parse friendIds từ JSON string
   - Gọi ChatService.CreateGroupWithAvatarAndFriendsAsync()
   - Tạo ChatGroup record
   - Tạo GroupMember records cho creator và friends
   - Trả về JSON response hoặc redirect

4. JavaScript nhận response và cập nhật UI
```

#### **Bước 2: Tham gia nhóm**
```
1. User click vào group
2. JavaScript gọi: /GroupChat/GetGroupMessages?groupId={id}
3. GroupChatController.GetGroupMessages() xử lý:
   - Lấy tin nhắn: ChatService.GetGroupMessagesWithUserContextAsync()
   - Lấy thông tin group: ChatService.GetGroupDetailsAsync()
   - Tạo GroupMessagesViewModel
   - Trả về View hoặc JSON

4. SignalR Join Group:
   - ChatHub.JoinGroup(groupId)
   - Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString())
```

#### **Bước 3: Gửi tin nhắn nhóm**
```
1. User nhập tin nhắn và gửi
2. Form submit: /GroupChat/SendGroupMessage
3. GroupChatController.SendGroupMessage() xử lý:
   - Validate user có trong group không
   - Gọi ChatService.SendGroupMessageWithValidationAsync()
   - Lưu tin nhắn vào database
   - Upload files (nếu có)

4. SignalR Broadcast:
   - ChatHub.SendGroupMessage() được gọi
   - Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage")
   - Tất cả members trong group nhận tin nhắn real-time
```

#### **Bước 4: Quản lý thành viên**
```
1. Add Member:
   - Admin click "Add Member"
   - Modal hiển thị danh sách users
   - AJAX POST: /GroupChat/AddMember
   - Tạo GroupMember record
   - SignalR broadcast: "MemberAdded"

2. Remove Member:
   - Admin click "Remove Member"
   - AJAX POST: /GroupChat/RemoveMember
   - Xóa GroupMember record
   - SignalR broadcast: "MemberRemoved"

3. Ban/Unban Member:
   - Admin có thể ban user tạm thời
   - Lưu ban info vào database
   - User bị ban không thể join group
```

### 2.3 Tính năng nâng cao

#### **Voice to Text**
```
1. User click microphone icon
2. JavaScript record audio
3. Upload audio file: /GroupChat/VoiceToText
4. VoiceToTextService xử lý:
   - Convert audio to text
   - Trả về text content
5. Auto-fill vào message input
```

#### **Group Management**
```
1. Edit Group:
   - Admin click "Edit Group"
   - Form submit: /GroupChat/EditGroup
   - Update ChatGroup record
   - Upload new avatar (nếu có)

2. Delete Group:
   - Admin click "Delete Group"
   - Soft delete hoặc hard delete
   - Notify all members
```

---

## 3. WORKFLOW VIDEO CALL

### 3.1 Kiến trúc Video Call

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Server    │    │  Database   │
│  (Browser)  │    │ (ASP.NET)   │    │  (SQL Server)│
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │ 1. Create Meeting │                   │
       │──────────────────▶│                   │
       │                   │ 2. Create Room    │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 3. Room Code      │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 4. Join Room      │                   │
       │──────────────────▶│                   │
       │                   │ 5. SignalR Join   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 6. WebRTC Setup   │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 7. Media Stream   │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 8. Chat/Record    │                   │
       │──────────────────▶│                   │
       │                   │ 9. Save Session   │
       │                   │──────────────────▶│
```

### 3.2 Chi tiết Workflow

#### **Bước 1: Tạo cuộc họp**
```
1. User truy cập /Meeting/Create
2. MeetingController.Create() hiển thị form
3. User submit form với thông tin:
   - Title, Description
   - Scheduled time (nếu có)
   - Settings (recording, max participants, etc.)

4. MeetingController.Create(vm) xử lý:
   - Validate input
   - Gọi MeetingService.CreateMeetingAsync(creatorId)
   - Tạo VideoRoom record với:
     * Password: Random code
     * CreatorId: Current user
     * IsOpen: true
     * Settings: JSON configuration
   - Redirect to /Meeting/Room?code={password}
```

#### **Bước 2: Tham gia cuộc họp**
```
1. User có thể:
   - Join từ link được share
   - Enter room code: /Meeting/Join
   - Join từ scheduled meeting

2. MeetingController.Join(vm) xử lý:
   - Validate room code
   - Gọi MeetingService.JoinMeetingAsync(password)
   - Kiểm tra room exists và is open
   - Redirect to room

3. MeetingController.Room(code) xử lý:
   - Lấy room info từ database
   - Kiểm tra user permissions
   - Render room interface với:
     * WebRTC configuration
     * Chat panel
     * Recording controls
     * Participant list
```

#### **Bước 3: Kết nối WebRTC**
```
1. Client JavaScript:
   - Initialize WebRTC
   - Get user media (camera, microphone)
   - Connect to SignalR hub

2. MeetingHub.JoinRoom(password, userId):
   - Add user to room tracking
   - Add connection to SignalR group
   - Set host if first person
   - Start call session tracking
   - Broadcast user joined

3. WebRTC Signaling:
   - Client A tạo offer
   - Gửi offer qua SignalR: Signal(toConnectionId, offer)
   - Client B nhận offer, tạo answer
   - Gửi answer back qua SignalR
   - Establish peer connection
```

#### **Bước 4: Real-time Communication**
```
1. Audio/Video Stream:
   - WebRTC handle media streams
   - Automatic quality adjustment
   - Bandwidth optimization

2. Screen Sharing:
   - User click "Share Screen"
   - JavaScript getDisplayMedia()
   - Replace video track with screen track
   - Broadcast to all participants

3. Chat in Meeting:
   - Real-time chat via MeetingChatHub
   - Messages stored in RoomMessage table
   - Support file sharing, reactions
```

#### **Bước 5: Recording & Transcription**
```
1. Recording:
   - Host click "Start Recording"
   - Client record media streams
   - Upload to server: /Meeting/UploadRecording
   - RecordingService xử lý storage
   - Save metadata to database

2. Live Transcription:
   - AudioTranscriptionService process audio
   - Real-time subtitle generation
   - Support multiple languages
   - Display subtitles overlay
```

#### **Bước 6: Session Management**
```
1. Call Session Tracking:
   - MeetingService.TrackUserJoinAsync()
   - MeetingService.TrackUserLeaveAsync()
   - Store attendance data
   - Calculate session statistics

2. Room Controls:
   - Host can mute/unmute participants
   - Kick/ban participants
   - Lock/unlock room
   - End meeting for all
```

### 3.3 Tính năng nâng cao

#### **Breakout Rooms**
```
1. Host tạo breakout rooms
2. Participants được assign vào rooms
3. Separate chat và recording cho mỗi room
4. Automatic return to main room
```

#### **Polls & Surveys**
```
1. Host tạo poll trong meeting
2. Participants vote real-time
3. Results displayed instantly
4. Poll data stored for analytics
```

#### **Advanced Analytics**
```
1. Track participant engagement
2. Monitor audio/video quality
3. Generate meeting reports
4. Export attendance data
```

---

## 4. WORKFLOW QUIZ SYSTEM

### 4.1 Kiến trúc Quiz System

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Server    │    │  Database   │
│  (Browser)  │    │ (ASP.NET)   │    │  (SQL Server)│
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │ 1. Create Quiz    │                   │
       │──────────────────▶│                   │
       │                   │ 2. Save Quiz      │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 3. Add Questions  │                   │
       │──────────────────▶│                   │
       │                   │ 4. Save Questions │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 5. Start Quiz     │                   │
       │──────────────────▶│                   │
       │                   │ 6. SignalR Room   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 7. Real-time Quiz │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 8. Submit Answers │                   │
       │──────────────────▶│                   │
       │                   │ 9. Calculate Score│
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 10. Show Results  │                   │
       │◀──────────────────│                   │
```

### 4.2 Chi tiết Workflow

#### **Bước 1: Tạo Quiz**
```
1. Teacher truy cập /Quiz/Create
2. QuizController.Create() hiển thị form
3. Teacher submit quiz info:
   - Title, Description
   - Time limit
   - IsPublic/Password
   - IsHomework (with start/end time)

4. QuizController.Create(quiz) xử lý:
   - Validate input
   - Save Quiz record
   - Redirect to /Quiz/AddQuestions/{quizId}
```

#### **Bước 2: Thêm câu hỏi**
```
1. Teacher truy cập /Quiz/AddQuestions/{quizId}
2. Form cho phép:
   - Add question manually
   - Upload questions file (Excel/CSV)
   - Import from question bank

3. QuizController.AddQuestion() xử lý:
   - Validate question data
   - Save QuizQuestion record
   - Support multiple question types:
     * Multiple choice
     * True/False
     * Essay
     * Matching

4. Question structure:
   - Question text
   - Options (for multiple choice)
   - Correct answer
   - Points/weight
   - Explanation
```

#### **Bước 3: Cấu hình Quiz**
```
1. Quiz Settings:
   - Shuffle questions
   - Shuffle options
   - Show correct answers after submit
   - Allow retake
   - Time limit enforcement
   - Proctoring settings

2. Access Control:
   - Public quiz: Anyone can take
   - Private quiz: Password required
   - Scheduled quiz: Available only in time window
   - Invitation only: Specific users/emails
```

#### **Bước 4: Tham gia Quiz**
```
1. Student truy cập /Quiz/Take/{quizId}
2. QuizController.Take() xử lý:
   - Validate quiz access
   - Check time restrictions
   - Verify password (nếu private)
   - Create QuizAttempt record
   - Load questions

3. Quiz Interface:
   - Timer countdown
   - Question navigation
   - Auto-save answers
   - Submit confirmation
```

#### **Bước 5: Real-time Quiz (Live Mode)**
```
1. Teacher tạo quiz room:
   - /Quiz/QuizRealtime/{quizId}
   - Generate room code
   - Start SignalR connection

2. Students join room:
   - /Quiz/JoinQuizRoom
   - Enter room code
   - Enter display name
   - QuizHub.JoinQuizRoom(roomCode, displayName)

3. Live Quiz Flow:
   - Teacher click "Start Quiz"
   - QuizHub.StartQuiz(roomCode, quizId)
   - Students receive first question
   - Teacher control question progression
   - Real-time leaderboard updates
```

#### **Bước 6: Submit & Grading**
```
1. Student submit answers:
   - QuizController.SubmitAttempt()
   - Save QuizAttemptDetail records
   - Calculate score automatically
   - Store submission time

2. Auto-grading:
   - Multiple choice: Immediate scoring
   - True/False: Immediate scoring
   - Essay: Manual grading required
   - Partial credit for multiple choice

3. Manual grading (nếu cần):
   - Teacher review essay answers
   - Adjust scores
   - Add comments
   - Release results
```

#### **Bước 7: Results & Analytics**
```
1. Individual Results:
   - /Quiz/Result/{attemptId}/{quizId}
   - Show score breakdown
   - Display correct/incorrect answers
   - Show explanations
   - Compare with class average

2. Class Analytics:
   - /Quiz/Statistics/{quizId}
   - Average score
   - Question difficulty analysis
   - Time spent per question
   - Student performance ranking

3. Export Results:
   - Excel/CSV export
   - Detailed reports
   - Grade distribution
   - Question analysis
```

### 4.3 Tính năng nâng cao

#### **Proctoring Features**
```
1. Screen Recording:
   - Record student screen during quiz
   - Detect tab switching
   - Flag suspicious behavior

2. Webcam Monitoring:
   - Face detection
   - Multiple person detection
   - Eye tracking

3. Browser Lockdown:
   - Disable right-click
   - Prevent copy/paste
   - Block external websites
```

#### **Adaptive Testing**
```
1. Question Difficulty:
   - Start with medium difficulty
   - Adjust based on performance
   - Dynamic question selection

2. Personalized Learning:
   - Track student progress
   - Recommend practice questions
   - Identify knowledge gaps
```

#### **Collaborative Features**
```
1. Group Quizzes:
   - Team-based assessment
   - Shared score calculation
   - Peer review system

2. Discussion Forums:
   - Post-quiz discussions
   - Question clarification
   - Peer learning support
```

---

## 5. WORKFLOW WHITEBOARD

### 5.1 Kiến trúc Whiteboard

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Server    │    │  Database   │
│  (Browser)  │    │ (ASP.NET)   │    │  (SQL Server)│
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       │ 1. Create Session │                   │
       │──────────────────▶│                   │
       │                   │ 2. Save Session   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 3. Join Session   │                   │
       │──────────────────▶│                   │
       │                   │ 4. SignalR Join   │
       │                   │──────────────────▶│
       │                   │◀──────────────────│
       │ 5. Canvas Setup   │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 6. Drawing Events │                   │
       │──────────────────▶│                   │
       │                   │ 7. Broadcast      │
       │                   │──────────────────▶│
       │ 8. All Users      │                   │
       │◀──────────────────│                   │
       │                   │                   │
       │ 9. Save Canvas    │                   │
       │──────────────────▶│                   │
       │                   │ 10. Save Image    │
       │                   │──────────────────▶│
```

### 5.2 Chi tiết Workflow

#### **Bước 1: Tạo Whiteboard Session**
```
1. User truy cập /Whiteboard/Create
2. WhiteboardController.Create() hiển thị form
3. User submit:
   - Title, Description
   - Access permissions
   - Canvas size

4. WhiteboardController.Create() xử lý:
   - Validate input
   - Gọi WhiteboardService.CreateSessionAsync()
   - Tạo WhiteboardSession record
   - Generate unique session ID
   - Redirect to /Whiteboard/Editor/{sessionId}
```

#### **Bước 2: Join Session**
```
1. User join via link hoặc session ID
2. WhiteboardController.Editor(sessionId) xử lý:
   - Validate session exists
   - Check access permissions
   - Load existing canvas data
   - Render editor interface

3. SignalR Connection:
   - WhiteboardHub.OnConnectedAsync()
   - Join session group
   - Receive current canvas state
```

#### **Bước 3: Real-time Drawing**
```
1. User draw trên canvas:
   - Mouse events (mousedown, mousemove, mouseup)
   - Touch events (mobile support)
   - Pen pressure (nếu supported)

2. Drawing Tools:
   - Pen/Brush: Freehand drawing
   - Line: Straight lines
   - Rectangle/Circle: Shapes
   - Text: Add text annotations
   - Eraser: Remove content
   - Select: Move/resize objects

3. SignalR Broadcasting:
   - WhiteboardHub.SendDrawingEvent()
   - Broadcast to all users in session
   - Real-time synchronization
```

#### **Bước 4: Collaboration Features**
```
1. Multi-user Support:
   - Multiple users can draw simultaneously
   - Cursor tracking for each user
   - Color coding per user
   - User presence indicators

2. Version Control:
   - Undo/Redo functionality
   - Session history
   - Save checkpoints
   - Export different versions

3. Sharing & Export:
   - Save as image (PNG/JPG)
   - Export as PDF
   - Share via link
   - Embed in documents
```

### 5.3 Tính năng nâng cao

#### **Advanced Tools**
```
1. Shape Recognition:
   - Auto-detect shapes
   - Perfect circles/squares
   - Smart alignment

2. Text Recognition:
   - Handwriting to text
   - OCR for uploaded images
   - Auto-translate text

3. Templates & Stencils:
   - Pre-made templates
   - Custom stencils
   - Import SVG graphics
```

#### **Integration Features**
```
1. Meeting Integration:
   - Whiteboard in video calls
   - Screen sharing whiteboard
   - Recording whiteboard sessions

2. File Import/Export:
   - Import images/documents
   - Export to various formats
   - Cloud storage integration
```

---

## 6. WORKFLOW FILE MANAGEMENT

### 6.1 File Upload & Processing

```
1. File Selection:
   - User chọn file từ input
   - JavaScript validate file type/size
   - Show upload progress

2. Upload Process:
   - FormData với file và metadata
   - AJAX POST to FileController
   - FileUploadService xử lý:
     * Validate file
     * Generate unique filename
     * Save to wwwroot/uploads/
     * Create Media record

3. File Processing:
   - Image compression
   - Video thumbnail generation
   - Document preview generation
   - Virus scanning
```

### 6.2 File Sharing & Access

```
1. Share Files:
   - Generate share link
   - Set access permissions
   - Password protection
   - Expiration date

2. Access Control:
   - Public files
   - Private files (user only)
   - Shared files (specific users)
   - Group files (group members)
```

---

## 7. WORKFLOW PAYMENT SYSTEM

### 7.1 PayOS Integration

```
1. Subscription Plans:
   - Free tier limitations
   - Premium features
   - Payment plans (monthly/yearly)

2. Payment Flow:
   - User select plan
   - Redirect to PayOS
   - Payment processing
   - Webhook notification
   - Update user subscription

3. Premium Features:
   - Unlimited storage
   - Advanced analytics
   - Priority support
   - Custom branding
```

---

## 8. SECURITY & AUTHENTICATION

### 8.1 Authentication Flow

```
1. Login Options:
   - Google OAuth
   - Facebook OAuth
   - Email/Password (nếu có)

2. Session Management:
   - Cookie-based authentication
   - Session timeout
   - Remember me functionality

3. Authorization:
   - Role-based access control
   - Resource-level permissions
   - API rate limiting
```

### 8.2 Data Protection

```
1. Data Encryption:
   - HTTPS for all communications
   - Database encryption
   - File encryption at rest

2. Privacy Controls:
   - GDPR compliance
   - Data retention policies
   - User data export/deletion
```

---

## 9. PERFORMANCE & SCALABILITY

### 9.1 Optimization Strategies

```
1. Caching:
   - Redis for session storage
   - CDN for static files
   - Database query caching

2. Database Optimization:
   - Indexed queries
   - Connection pooling
   - Query optimization

3. Real-time Performance:
   - SignalR scaling
   - WebRTC optimization
   - Bandwidth management
```

### 9.2 Monitoring & Analytics

```
1. Application Monitoring:
   - Error tracking
   - Performance metrics
   - User behavior analytics

2. Infrastructure Monitoring:
   - Server health
   - Database performance
   - Network latency
```

---

## 10. DEPLOYMENT & MAINTENANCE

### 10.1 Deployment Process

```
1. Environment Setup:
   - Development
   - Staging
   - Production

2. CI/CD Pipeline:
   - Automated testing
   - Build automation
   - Deployment automation

3. Database Migrations:
   - Schema updates
   - Data migrations
   - Rollback procedures
```

### 10.2 Maintenance Tasks

```
1. Regular Maintenance:
   - Database cleanup
   - Log rotation
   - Security updates

2. Backup Strategy:
   - Automated backups
   - Disaster recovery
   - Data retention
```

---

## KẾT LUẬN

Dự án Zela là một ứng dụng web toàn diện với kiến trúc MVC well-structured, tích hợp nhiều tính năng hiện đại:

**Điểm mạnh:**
- Kiến trúc rõ ràng, tách biệt các layer
- Sử dụng SignalR cho real-time communication
- Tích hợp nhiều tính năng nâng cao
- Code organization tốt với dependency injection

**Cải thiện có thể:**
- Thêm unit tests
- Implement caching strategy
- Optimize database queries
- Add comprehensive error handling
- Implement logging framework

**Công nghệ sử dụng:**
- ASP.NET Core MVC
- Entity Framework Core
- SignalR
- WebRTC
- PayOS Payment
- Google/Facebook OAuth
- SQL Server
- JavaScript/jQuery
- Bootstrap CSS

Dự án thể hiện khả năng phát triển ứng dụng web enterprise-level với nhiều tính năng phức tạp và real-time capabilities. 