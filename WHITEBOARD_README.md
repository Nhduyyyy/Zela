# 🎨 Whiteboard System - Zela

## 📖 Tổng quan

Hệ thống Whiteboard là một tính năng mạnh mẽ cho phép người dùng tạo, chỉnh sửa và chia sẻ bảng trắng kỹ thuật số. Hệ thống hỗ trợ vẽ tay, văn bản, hình dạng và collaboration thời gian thực.

## ✨ Tính năng chính

### 🎯 **Core Features**
- **Vẽ tay**: Bút vẽ với nhiều màu sắc và độ dày
- **Tẩy**: Xóa các phần đã vẽ
- **Văn bản**: Thêm text vào bảng trắng
- **Hình dạng**: Vẽ hình học cơ bản
- **Chọn và di chuyển**: Chọn và chỉnh sửa đối tượng

### 🤝 **Collaboration**
- **Real-time drawing**: Vẽ đồng thời với nhiều người
- **Cursor tracking**: Theo dõi con trỏ của người khác
- **Tool synchronization**: Đồng bộ công cụ giữa các user
- **Session management**: Quản lý phiên làm việc

### 💾 **Data Management**
- **Auto-save**: Tự động lưu mỗi 5 giây
- **Session history**: Lịch sử các phiên làm việc
- **Template system**: Tạo và sử dụng templates
- **Export/Import**: Xuất ảnh PNG

## 🏗️ Kiến trúc hệ thống

### **Database Schema**
```
Whiteboards
├── WhiteboardId (PK)
├── Title
├── Description
├── CreatorId (FK -> Users)
├── CreatedAt
├── UpdatedAt
├── IsPublic
└── IsTemplate

WhiteboardSessions
├── SessionId (PK)
├── WhiteboardId (FK -> Whiteboards)
├── RoomId (FK -> VideoRooms, nullable)
├── CreatedAt
├── LastModifiedAt
├── IsActive
├── CanvasData (JSON)
└── ThumbnailUrl
```

### **File Structure**
```
Zela/
├── Models/
│   ├── Whiteboard.cs
│   └── WhiteboardSession.cs
├── ViewModels/
│   └── WhiteboardViewModels.cs
├── Services/
│   ├── Interface/
│   │   └── IWhiteboardService.cs
│   └── WhiteboardService.cs
├── Controllers/
│   └── WhiteboardController.cs
├── Hubs/
│   └── WhiteboardHub.cs
├── Views/Whiteboard/
│   ├── Index.cshtml
│   ├── Create.cshtml
│   └── Editor.cshtml
└── wwwroot/
    ├── css/components/whiteboard.css
    └── js/whiteboard.js
```

## 🚀 Cách sử dụng

### **1. Tạo bảng trắng mới**
1. Vào trang **Whiteboard** từ menu chính
2. Click **"Tạo bảng trắng"**
3. Nhập tiêu đề và mô tả
4. Chọn cài đặt công khai/template
5. Click **"Tạo"**

### **2. Chỉnh sửa bảng trắng**
1. Mở bảng trắng từ danh sách
2. Sử dụng các công cụ vẽ:
   - **Bút vẽ**: Vẽ tự do
   - **Tẩy**: Xóa các phần đã vẽ
   - **Văn bản**: Thêm text
   - **Hình dạng**: Vẽ hình học
3. Thay đổi màu sắc và độ dày
4. Lưu tự động hoặc xuất ảnh

### **3. Collaboration**
1. Chia sẻ link bảng trắng với người khác
2. Mọi người có thể vẽ đồng thời
3. Xem con trỏ và thao tác của nhau
4. Đồng bộ thay đổi thời gian thực

### **4. Từ Meeting Room**
1. Trong cuộc họp video, click **"Tạo bảng trắng"**
2. Bảng trắng sẽ được liên kết với phòng họp
3. Mọi người trong phòng có thể truy cập

## 🔧 API Endpoints

### **Whiteboard Management**
- `GET /Whiteboard` - Trang quản lý
- `GET /Whiteboard/Create` - Trang tạo mới
- `POST /Whiteboard/Create` - Tạo bảng trắng
- `GET /Whiteboard/Editor/{id}` - Trang chỉnh sửa
- `POST /Whiteboard/Update` - Cập nhật thông tin
- `POST /Whiteboard/Delete` - Xóa bảng trắng

### **Session Management**
- `GET /Whiteboard/GetSession` - Lấy dữ liệu session
- `POST /Whiteboard/UpdateSessionData` - Cập nhật canvas
- `POST /Whiteboard/SaveThumbnail` - Lưu thumbnail
- `POST /Whiteboard/CreateSession` - Tạo session mới

### **SignalR Hub**
- `/whiteboardHub` - Real-time collaboration
- `JoinWhiteboard` - Tham gia phòng
- `BroadcastDrawing` - Gửi dữ liệu vẽ
- `BroadcastCursor` - Gửi vị trí con trỏ
- `BroadcastToolChange` - Gửi thay đổi công cụ

## 🎨 Canvas Drawing System

### **Drawing Tools**
```javascript
// Khởi tạo whiteboard
initWhiteboard(canvasData, sessionId, canEdit, whiteboardId);

// Công cụ vẽ
setTool('pen');        // Bút vẽ
setTool('eraser');     // Tẩy
setTool('text');       // Văn bản
setTool('shape');      // Hình dạng
setTool('select');     // Chọn

// Thuộc tính
setColor('#ff0000');   // Màu đỏ
setSize(5);           // Độ dày 5px
```

### **Data Format**
```json
{
  "paths": [
    [
      {
        "x": 100,
        "y": 100,
        "tool": "pen",
        "color": "#000000",
        "size": 2
      },
      {
        "x": 150,
        "y": 150,
        "tool": "pen",
        "color": "#000000",
        "size": 2
      }
    ]
  ]
}
```

## 🔒 Bảo mật

### **Access Control**
- Chỉ chủ sở hữu có thể chỉnh sửa
- Bảng trắng công khai có thể xem
- Template có thể được sao chép
- Session-based authentication

### **Data Protection**
- Canvas data được lưu dưới dạng JSON
- Auto-save với validation
- Backup và recovery
- Rate limiting cho API calls

## 📱 Responsive Design

### **Desktop**
- Full canvas với sidebar tools
- Keyboard shortcuts
- Mouse và touchpad support
- Multi-monitor support

### **Mobile**
- Touch-optimized interface
- Gesture support
- Responsive canvas size
- Mobile-friendly tools

## 🚀 Performance

### **Optimization**
- Canvas rendering optimization
- Debounced auto-save
- Efficient data serialization
- Lazy loading cho sessions

### **Scalability**
- SignalR connection pooling
- Database indexing
- Caching strategies
- Load balancing ready

## 🧪 Testing

### **Manual Testing**
1. Tạo bảng trắng mới
2. Vẽ và test các công cụ
3. Test collaboration với 2+ users
4. Test auto-save và recovery
5. Test export functionality

### **Automated Testing**
```bash
# Run tests
dotnet test

# Test specific features
dotnet test --filter "Whiteboard"
```

## 📈 Monitoring

### **Metrics**
- Active sessions count
- Drawing operations per minute
- Collaboration events
- Error rates
- Performance metrics

### **Logging**
- User actions
- System events
- Error tracking
- Performance monitoring

## 🔮 Roadmap

### **Phase 1** ✅
- [x] Basic drawing tools
- [x] Real-time collaboration
- [x] Session management
- [x] Template system

### **Phase 2** 🚧
- [ ] Advanced shapes
- [ ] Image import
- [ ] Layer system
- [ ] Undo/Redo

### **Phase 3** 📋
- [ ] AI-powered tools
- [ ] Voice commands
- [ ] 3D drawing
- [ ] VR support

## 🤝 Contributing

### **Development Setup**
1. Clone repository
2. Install dependencies
3. Run migrations
4. Start development server

### **Code Standards**
- Follow C# conventions
- Use async/await patterns
- Implement proper error handling
- Write unit tests

## 📞 Support

### **Documentation**
- API documentation
- User guides
- Video tutorials
- FAQ

### **Contact**
- Email: support@zela.com
- Discord: Zela Community
- GitHub Issues

---

**Whiteboard System** - Một phần của nền tảng Zela 🎨 