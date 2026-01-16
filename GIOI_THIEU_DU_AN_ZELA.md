# 📚 GIỚI THIỆU DỰ ÁN ZELA - Nền Tảng Giao Tiếp và Học Tập Trực Tuyến

## 🎯 ZELA LÀ GÌ?

**Zela** là một ứng dụng web hiện đại được xây dựng để phục vụ nhu cầu giao tiếp và học tập trực tuyến. Hãy tưởng tượng Zela như một "trung tâm kỹ thuật số" nơi bạn có thể:

- 💬 **Trò chuyện** với bạn bè và đồng nghiệp
- 📹 **Gọi video** để họp hoặc học nhóm
- ✍️ **Làm bài kiểm tra** trực tuyến
- 🎨 **Vẽ và chia sẻ** ý tưởng trên bảng trắng
- 📁 **Lưu trữ và chia sẻ** tài liệu
- 🤖 **Sử dụng AI** để hỗ trợ học tập

---

## 🛠️ CÔNG NGHỆ ĐƯỢC SỬ DỤNG (Giải thích đơn giản)

### 1. **ASP.NET Core 8.0** - "Bộ khung xây dựng ứng dụng"
   - **Là gì?** Đây là công nghệ của Microsoft dùng để xây dựng các website và ứng dụng web
   - **Vai trò:** Giống như bộ khung của một ngôi nhà, nó cung cấp nền tảng vững chắc để xây dựng toàn bộ ứng dụng
   - **Tại sao dùng?** Rất mạnh mẽ, an toàn, và được Microsoft hỗ trợ tốt

### 2. **SQL Server** - "Kho lưu trữ dữ liệu"
   - **Là gì?** Một hệ thống cơ sở dữ liệu (database) chuyên nghiệp
   - **Vai trò:** Lưu trữ tất cả thông tin như: tài khoản người dùng, tin nhắn, cuộc họp, bài kiểm tra, v.v.
   - **Ví dụ:** Khi bạn gửi tin nhắn, nó được lưu vào SQL Server để sau này có thể xem lại

### 3. **SignalR** - "Công nghệ giao tiếp tức thời"
   - **Là gì?** Công nghệ cho phép máy chủ (server) gửi thông tin đến trình duyệt (browser) ngay lập tức
   - **Vai trò:** Giúp tin nhắn, video call, và các hoạt động khác diễn ra "real-time" (thời gian thực)
   - **Ví dụ:** Khi bạn gửi tin nhắn, người nhận thấy ngay lập tức mà không cần làm mới trang

### 4. **Entity Framework Core** - "Cầu nối với cơ sở dữ liệu"
   - **Là gì?** Công cụ giúp lập trình viên dễ dàng làm việc với cơ sở dữ liệu
   - **Vai trò:** Thay vì viết các câu lệnh phức tạp, lập trình viên có thể dùng code đơn giản hơn để lưu/lấy dữ liệu

### 5. **Cloudinary** - "Kho lưu trữ hình ảnh và video trên mây"
   - **Là gì?** Dịch vụ lưu trữ file (ảnh, video) trên internet
   - **Vai trò:** Lưu trữ các file người dùng upload như ảnh đại diện, video, tài liệu
   - **Lợi ích:** Giúp website chạy nhanh hơn vì file được lưu ở nơi chuyên dụng

### 6. **PayOS** - "Cổng thanh toán"
   - **Là gì?** Hệ thống thanh toán trực tuyến của Việt Nam
   - **Vai trò:** Cho phép người dùng thanh toán để nâng cấp lên tài khoản Premium
   - **Tính năng:** Hỗ trợ thanh toán qua ví điện tử, thẻ ngân hàng

### 7. **OpenAI API** - "Trí tuệ nhân tạo"
   - **Là gì?** Công nghệ AI (trí tuệ nhân tạo) của OpenAI
   - **Vai trò:** Cung cấp các tính năng thông minh như:
     - Tự động tạo phụ đề (subtitle) trong cuộc gọi video
     - Dịch ngôn ngữ tự động
     - Tóm tắt nội dung cuộc họp
     - Trả lời câu hỏi tự động

### 8. **OAuth (Google & Facebook)** - "Đăng nhập bằng tài khoản mạng xã hội"
   - **Là gì?** Công nghệ cho phép đăng nhập bằng tài khoản Google hoặc Facebook
   - **Vai trò:** Người dùng không cần tạo tài khoản mới, chỉ cần dùng tài khoản Google/Facebook có sẵn
   - **Lợi ích:** Tiện lợi và an toàn hơn

---

## ✨ CÁC TÍNH NĂNG CHÍNH CỦA ZELA

### 1. 💬 **HỆ THỐNG CHAT (Trò chuyện)**

#### **Chat 1-1 (Trò chuyện riêng tư)**
- Gửi tin nhắn văn bản với bạn bè
- Gửi hình ảnh, video, file đính kèm
- Gửi sticker (nhãn dán) vui nhộn
- Phản ứng với tin nhắn (like, love, haha, v.v.)
- Trả lời tin nhắn cụ thể (reply)
- Xem trạng thái "đang gõ" của người khác
- Lịch sử tin nhắn được lưu trữ an toàn

#### **Chat nhóm (Group Chat)**
- Tạo nhóm trò chuyện với nhiều người
- Quản lý thành viên (thêm, xóa, phân quyền)
- Chia sẻ file trong nhóm
- Chat real-time (thời gian thực) - tin nhắn hiển thị ngay lập tức

**Công nghệ sử dụng:**
- SignalR để gửi/nhận tin nhắn tức thời
- SQL Server để lưu trữ lịch sử tin nhắn
- Cloudinary để lưu trữ file đính kèm

---

### 2. 📹 **HỆ THỐNG VIDEO CALL (Gọi video)**

Đây là một trong những tính năng mạnh mẽ nhất của Zela, tương tự như Zoom hay Google Meet nhưng được tùy chỉnh cho mục đích học tập.

#### **Tính năng cơ bản:**
- **Gọi video 1-1:** Trò chuyện video với một người
- **Gọi video nhóm:** Họp với nhiều người cùng lúc (nhiều người tham gia)
- **Chia sẻ màn hình:** Chia sẻ màn hình máy tính của bạn cho người khác xem
- **Bật/tắt camera và microphone:** Kiểm soát quyền riêng tư

#### **Tính năng nâng cao:**
- **Phòng chờ (Waiting Room):** Người chủ trì có thể kiểm soát ai được vào phòng
- **Quản lý người tham gia:**
  - Mute (tắt tiếng) người khác
  - Remove (xóa) người khỏi phòng
  - Phân quyền Moderator (người điều hành)
- **Ghi âm/Ghi hình:** Lưu lại toàn bộ cuộc họp để xem lại sau
- **Chat trong phòng họp:** Trò chuyện văn bản trong khi đang họp video
- **Phụ đề tự động (Real-time Subtitle):** AI tự động tạo phụ đề từ lời nói
- **Dịch ngôn ngữ:** Tự động dịch phụ đề sang ngôn ngữ khác
- **Breakout Rooms:** Chia phòng họp lớn thành các phòng nhỏ để thảo luận nhóm
- **Polling (Bình chọn):** Tạo câu hỏi khảo sát trong cuộc họp
- **Theo dõi điểm danh:** Tự động ghi nhận ai tham gia, tham gia lúc nào

**Công nghệ sử dụng:**
- SignalR để đồng bộ video/audio real-time
- WebRTC (công nghệ trình duyệt) để truyền video/audio
- OpenAI API để tạo phụ đề và dịch thuật
- SQL Server để lưu trữ thông tin cuộc họp, bản ghi âm

---

### 3. 📝 **HỆ THỐNG QUIZ (Kiểm tra trực tuyến)**

Hệ thống cho phép giáo viên tạo bài kiểm tra và học sinh làm bài trực tuyến.

#### **Tính năng cho giáo viên:**
- **Tạo bài kiểm tra:** 
  - Thêm nhiều câu hỏi
  - Nhiều loại câu hỏi: trắc nghiệm, tự luận, đúng/sai
  - Đặt thời gian làm bài
  - Chấm điểm tự động
- **Quản lý bài kiểm tra:**
  - Xem danh sách học sinh đã làm bài
  - Xem kết quả chi tiết
  - Xuất báo cáo điểm số
- **Phân quyền:** Chỉ người tạo mới có thể chỉnh sửa/xóa bài kiểm tra

#### **Tính năng cho học sinh:**
- **Làm bài kiểm tra:** 
  - Làm bài trực tuyến
  - Xem thời gian còn lại
  - Nộp bài tự động khi hết giờ
- **Xem kết quả:** 
  - Xem điểm số ngay sau khi nộp bài
  - Xem đáp án đúng/sai
  - Xem lịch sử các lần làm bài

**Công nghệ sử dụng:**
- SQL Server để lưu trữ câu hỏi, đáp án, kết quả
- SignalR để cập nhật thời gian real-time
- Entity Framework Core để quản lý dữ liệu

---

### 4. 🎨 **HỆ THỐNG WHITEBOARD (Bảng trắng tương tác)**

Giống như một bảng trắng thật, nhưng trên máy tính và nhiều người có thể vẽ cùng lúc.

#### **Tính năng vẽ:**
- **Vẽ tay:** Dùng chuột hoặc bút cảm ứng để vẽ tự do
- **Vẽ hình học:** Vẽ hình tròn, hình vuông, đường thẳng
- **Thêm văn bản:** Viết chữ lên bảng
- **Tẩy:** Xóa các phần đã vẽ
- **Chọn màu:** Nhiều màu sắc để chọn
- **Điều chỉnh độ dày nét vẽ**

#### **Tính năng cộng tác:**
- **Vẽ cùng lúc:** Nhiều người có thể vẽ trên cùng một bảng
- **Theo dõi con trỏ:** Xem người khác đang vẽ ở đâu
- **Lưu tự động:** Bảng tự động lưu mỗi 5 giây
- **Lịch sử:** Xem lại các phiên vẽ trước đó
- **Xuất ảnh:** Tải bảng vẽ về máy dưới dạng hình ảnh

#### **Tích hợp với Video Call:**
- Có thể mở bảng trắng trong phòng họp video
- Vừa họp vừa vẽ để giải thích ý tưởng

**Công nghệ sử dụng:**
- SignalR để đồng bộ nét vẽ real-time giữa các người dùng
- HTML5 Canvas (công nghệ trình duyệt) để vẽ
- SQL Server để lưu trữ dữ liệu bảng vẽ

---

### 5. 📁 **QUẢN LÝ FILE (Lưu trữ và chia sẻ tài liệu)**

#### **Tính năng:**
- **Upload file:** Tải lên các loại file như Word, PDF, Excel, PowerPoint, hình ảnh, video
- **Lưu trữ:** File được lưu an toàn trên Cloudinary (kho lưu trữ đám mây)
- **Chia sẻ:** 
  - Chia sẻ file trong chat
  - Chia sẻ file trong nhóm
  - Tạo link chia sẻ công khai
- **Tóm tắt tự động:** AI tự động đọc và tóm tắt nội dung file (đặc biệt hữu ích cho tài liệu dài)
- **Xem trước:** Xem nội dung file mà không cần tải về

**Công nghệ sử dụng:**
- Cloudinary để lưu trữ file
- OpenAI API để tóm tắt nội dung file
- SQL Server để lưu thông tin về file

---

### 6. 👥 **QUẢN LÝ BẠN BÈ (Friendship System)**

#### **Tính năng:**
- **Gửi lời mời kết bạn:** Tìm và kết bạn với người khác
- **Chấp nhận/Từ chối:** Quyết định có muốn kết bạn hay không
- **Danh sách bạn bè:** Xem tất cả bạn bè của mình
- **Xóa bạn:** Hủy kết bạn nếu muốn

**Công nghệ sử dụng:**
- SQL Server để lưu trữ mối quan hệ bạn bè
- Entity Framework Core để quản lý dữ liệu

---

### 7. 🔔 **HỆ THỐNG THÔNG BÁO (Notifications)**

#### **Tính năng:**
- **Thông báo real-time:** Nhận thông báo ngay khi có sự kiện mới
- **Các loại thông báo:**
  - Có tin nhắn mới
  - Có lời mời kết bạn
  - Có người tham gia cuộc họp
  - Có bài kiểm tra mới
  - Có file mới được chia sẻ
- **Đánh dấu đã đọc:** Quản lý thông báo đã xem/chưa xem

**Công nghệ sử dụng:**
- SignalR để gửi thông báo real-time
- SQL Server để lưu trữ lịch sử thông báo

---

### 8. 💳 **HỆ THỐNG THANH TOÁN (Payment System)**

#### **Tính năng:**
- **Nâng cấp Premium:** Thanh toán để sử dụng các tính năng cao cấp
- **Gói dịch vụ:**
  - Gói tháng: 99,000 VNĐ/tháng
  - Gói năm: 990,000 VNĐ/năm (tiết kiệm hơn)
- **Thanh toán an toàn:** Tích hợp PayOS (cổng thanh toán uy tín của Việt Nam)
- **Lịch sử giao dịch:** Xem tất cả các giao dịch đã thực hiện

#### **Tính năng Premium:**
- Lưu trữ không giới hạn
- Ghi âm cuộc họp không giới hạn
- Ưu tiên hỗ trợ
- Tính năng nâng cao khác

**Công nghệ sử dụng:**
- PayOS SDK để xử lý thanh toán
- SQL Server để lưu trữ thông tin giao dịch
- Webhook để nhận thông báo thanh toán thành công

---

### 9. 👨‍💼 **HỆ THỐNG QUẢN TRỊ (Admin Panel)**

#### **Tính năng:**
- **Quản lý người dùng:**
  - Xem danh sách tất cả người dùng
  - Xem thông tin chi tiết từng người
  - Khóa/Mở khóa tài khoản
- **Quản lý cuộc họp:**
  - Xem tất cả cuộc họp đang diễn ra
  - Xem lịch sử cuộc họp
  - Quản lý phòng họp
- **Quản lý nhóm chat:**
  - Xem tất cả nhóm chat
  - Quản lý thành viên nhóm
- **Quản lý bài kiểm tra:**
  - Xem tất cả bài kiểm tra
  - Xem thống kê kết quả

**Công nghệ sử dụng:**
- SQL Server để truy vấn dữ liệu
- Entity Framework Core để quản lý dữ liệu

---

### 10. 🔐 **BẢO MẬT VÀ XÁC THỰC (Security & Authentication)**

#### **Tính năng:**
- **Đăng nhập an toàn:**
  - Đăng nhập bằng Google
  - Đăng nhập bằng Facebook
  - Cookie-based authentication (lưu phiên đăng nhập)
- **Phân quyền:**
  - Chỉ người dùng đã đăng nhập mới sử dụng được
  - Phân quyền Admin/User
  - Kiểm soát quyền truy cập các tính năng
- **Bảo vệ dữ liệu:**
  - Mã hóa thông tin nhạy cảm
  - Bảo vệ chống tấn công

**Công nghệ sử dụng:**
- ASP.NET Core Authentication
- OAuth 2.0 (Google, Facebook)
- Cookie Authentication

---

## 🔄 ZELA HOẠT ĐỘNG NHƯ THẾ NÀO?

### **Luồng hoạt động cơ bản:**

1. **Người dùng truy cập website Zela**
   - Mở trình duyệt (Chrome, Firefox, Safari, v.v.)
   - Gõ địa chỉ website

2. **Đăng nhập**
   - Chọn đăng nhập bằng Google hoặc Facebook
   - Hệ thống xác thực và tạo phiên đăng nhập

3. **Sử dụng tính năng**
   - Chọn tính năng muốn dùng (Chat, Video Call, Quiz, v.v.)
   - Hệ thống xử lý yêu cầu và trả về kết quả

4. **Lưu trữ dữ liệu**
   - Tất cả dữ liệu (tin nhắn, cuộc họp, bài kiểm tra) được lưu vào SQL Server
   - File (ảnh, video, tài liệu) được lưu vào Cloudinary

5. **Giao tiếp real-time**
   - Khi có sự kiện mới (tin nhắn mới, người tham gia cuộc họp), SignalR tự động gửi thông báo đến trình duyệt
   - Người dùng thấy thay đổi ngay lập tức mà không cần làm mới trang

---

## 📊 KIẾN TRÚC TỔNG QUAN CỦA ZELA

Zela được xây dựng theo mô hình **3 tầng (3-layer architecture):**

### **Tầng 1: Presentation Layer (Tầng hiển thị)**
- **Controllers:** Xử lý các yêu cầu từ người dùng (ví dụ: khi bạn click "Gửi tin nhắn")
- **Views:** Giao diện người dùng (các trang web mà bạn nhìn thấy)
- **wwwroot:** Các file tĩnh (CSS, JavaScript, hình ảnh) để làm đẹp và tương tác

### **Tầng 2: Business Logic Layer (Tầng xử lý nghiệp vụ)**
- **Services:** Chứa logic xử lý (ví dụ: cách gửi tin nhắn, cách tạo cuộc họp)
- **Hubs (SignalR):** Xử lý giao tiếp real-time
- **Middleware:** Xử lý các tác vụ chung (ví dụ: kiểm tra quyền truy cập)

### **Tầng 3: Data Access Layer (Tầng truy cập dữ liệu)**
- **Models:** Định nghĩa cấu trúc dữ liệu (ví dụ: User có tên, email, v.v.)
- **DbContext:** Kết nối với cơ sở dữ liệu
- **Migrations:** Quản lý thay đổi cấu trúc cơ sở dữ liệu

---

## 🎓 ZELA PHÙ HỢP VỚI AI?

### **Giáo dục:**
- Giáo viên tạo bài kiểm tra trực tuyến
- Học sinh làm bài và nhận kết quả ngay
- Họp video để giảng dạy từ xa
- Sử dụng bảng trắng để giải thích bài học

### **Doanh nghiệp:**
- Họp video với đồng nghiệp
- Chia sẻ tài liệu và cộng tác
- Ghi lại cuộc họp để xem lại sau

### **Cá nhân:**
- Trò chuyện với bạn bè
- Tổ chức nhóm học tập
- Lưu trữ và chia sẻ tài liệu

---

## 🚀 TÍNH NĂNG ĐẶC BIỆT - AI HỖ TRỢ

Zela tích hợp trí tuệ nhân tạo (AI) để nâng cao trải nghiệm:

### **1. Phụ đề tự động (Real-time Subtitle)**
- Trong cuộc gọi video, AI tự động nghe và tạo phụ đề
- Phụ đề hiển thị ngay dưới video
- Hữu ích cho người khiếm thính hoặc khi môi trường ồn

### **2. Dịch ngôn ngữ tự động**
- Phụ đề có thể được dịch sang nhiều ngôn ngữ
- Hữu ích cho cuộc họp quốc tế

### **3. Tóm tắt nội dung**
- AI tự động tóm tắt nội dung cuộc họp sau khi kết thúc
- Tóm tắt file tài liệu dài
- Giúp tiết kiệm thời gian đọc

### **4. Trả lời câu hỏi tự động**
- AI có thể trả lời câu hỏi trong chat
- Hỗ trợ học tập 24/7

---

## 📈 HIỆU SUẤT VÀ KHẢ NĂNG MỞ RỘNG

### **Tối ưu hóa:**
- **Caching:** Lưu tạm dữ liệu thường dùng để tải nhanh hơn
- **Indexing:** Tối ưu cơ sở dữ liệu để truy vấn nhanh
- **CDN:** Sử dụng Cloudinary để phân phối file nhanh trên toàn cầu

### **Khả năng mở rộng:**
- Có thể thêm nhiều máy chủ để phục vụ nhiều người dùng hơn
- Cơ sở dữ liệu có thể mở rộng để lưu trữ nhiều dữ liệu hơn

---

## 🔒 BẢO MẬT

Zela được xây dựng với các biện pháp bảo mật:

- **Xác thực an toàn:** Sử dụng OAuth (Google, Facebook) - công nghệ bảo mật cao
- **Mã hóa dữ liệu:** Thông tin nhạy cảm được mã hóa
- **Kiểm soát truy cập:** Chỉ người có quyền mới truy cập được
- **Bảo vệ chống tấn công:** Có các biện pháp chống tấn công phổ biến

---

## 📝 TÓM TẮT

**Zela** là một nền tảng web toàn diện kết hợp:

✅ **Giao tiếp:** Chat, Video Call, Nhóm chat  
✅ **Học tập:** Quiz, Whiteboard, File sharing  
✅ **AI:** Phụ đề tự động, Dịch thuật, Tóm tắt  
✅ **Thanh toán:** Tích hợp PayOS cho Premium  
✅ **Bảo mật:** Xác thực OAuth, mã hóa dữ liệu  

**Công nghệ chính:**
- ASP.NET Core 8.0 (Framework)
- SQL Server (Database)
- SignalR (Real-time)
- Cloudinary (File storage)
- OpenAI API (AI)
- PayOS (Payment)

**Mục đích:** Tạo ra một môi trường học tập và làm việc trực tuyến hiện đại, tiện lợi và thông minh.

---

*Tài liệu này được tạo để giúp những người không có kiến thức về lập trình hiểu rõ về dự án Zela. Nếu có câu hỏi, vui lòng liên hệ đội phát triển.*


