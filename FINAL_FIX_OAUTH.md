# 🔧 HƯỚNG DẪN CUỐI CÙNG - Sửa lỗi OAuth 403

## ✅ CÁC BƯỚC CẦN LÀM

### Bước 1: Commit và push code mới lên Git

```bash
# Trên máy local
git add .
git commit -m "Fix OAuth correlation failed - configure cookies for HTTP"
git push
```

### Bước 2: Trên VPS - Pull code và rebuild

```bash
# Vào thư mục dự án
cd ~/Zela

# Pull code mới
git pull

# Rebuild container với code mới
docker compose build zela-app

# Recreate container
docker compose up -d --force-recreate zela-app

# Đợi container khởi động
sleep 10

# Kiểm tra logs
docker logs zela-app --tail 30
```

### Bước 3: Xóa cookies cũ trong browser

**Quan trọng:** Phải xóa cookies cũ!

1. Mở Developer Tools (F12)
2. Application > Cookies > `http://zelahahaha.site`
3. Click chuột phải > "Clear" hoặc xóa từng cookie
4. **Hoặc dùng chế độ ẩn danh (Incognito)** - cách này đơn giản nhất

### Bước 4: Kiểm tra cấu hình Google Console

Đảm bảo trong [Google Cloud Console](https://console.cloud.google.com/):
- Vào **APIs & Services** > **Credentials**
- Click vào OAuth 2.0 Client ID của bạn
- Trong **Authorized redirect URIs**, phải có:
  ```
  http://zelahahaha.site/signin-google
  ```
- Click **Save**

### Bước 5: Thử đăng nhập lại

1. Truy cập: `http://zelahahaha.site`
2. Click "Đăng nhập bằng Google"
3. Chọn tài khoản Google
4. Xem kết quả

### Bước 6: Nếu vẫn lỗi - Debug

```bash
# Xem logs real-time
docker logs -f zela-app

# Trong browser, mở Developer Tools > Network tab
# Xem request POST /Account/GoogleLogin
# Kiểm tra response có Set-Cookie với correlation không
```

## 🔍 KIỂM TRA

Sau khi rebuild, kiểm tra cookies trong Developer Tools:
- ✅ SameSite phải là **Lax** (không phải Strict)
- ✅ Secure phải là **empty/false** (không có dấu check)
- ✅ Phải có cookie `.AspNetCore.Correlation.*` khi bắt đầu OAuth

## ⚠️ LƯU Ý QUAN TRỌNG

1. **Phải rebuild container** - không chỉ restart
2. **Phải xóa cookies cũ** - hoặc dùng chế độ ẩn danh
3. **Redirect URI phải đúng** trong Google Console
4. **Đợi vài phút** sau khi thay đổi Google Console

## 🐛 NẾU VẪN KHÔNG ĐƯỢC

Gửi thông tin sau:
1. Logs: `docker logs zela-app --tail 100`
2. Cookies trong Developer Tools (screenshot)
3. Network tab khi click "Đăng nhập bằng Google" (screenshot)
4. Redirect URI trong Google Console có đúng không

