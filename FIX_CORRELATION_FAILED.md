# 🔧 Sửa lỗi "Correlation failed" trong Google OAuth

## 🔍 Nguyên nhân

Lỗi "Correlation failed" xảy ra khi cookie correlation không được tìm thấy khi Google redirect về lại ứng dụng. Điều này thường do:

1. **Cookie không được lưu giữa các request** - Browser block cookie hoặc domain issues
2. **DataProtection keys thay đổi** - Keys bị reset khi restart container
3. **Reverse proxy issues** - Nếu có nginx/apache phía trước

## ✅ Giải pháp đã áp dụng

1. ✅ **DataProtection keys persistence** - Keys được lưu vào volume
2. ✅ **Cookie settings** - SameSite=Lax, SecurePolicy=None (cho HTTP)
3. ✅ **CorrelationCookie settings** - Cấu hình riêng cho OAuth

## 🔧 Các bước kiểm tra và sửa

### Bước 1: Đảm bảo container đã rebuild với code mới

```bash
# Rebuild container
docker compose build zela-app
docker compose up -d --force-recreate zela-app
```

### Bước 2: Kiểm tra DataProtection keys

```bash
docker exec zela-app ls -la /root/.aspnet/DataProtection-Keys
```

Nếu có file `.xml` trong thư mục này, keys đã được persist.

### Bước 3: Kiểm tra cookies trong browser

1. Mở Developer Tools (F12)
2. Vào tab **Application** > **Cookies** > `http://zelahahaha.site`
3. Kiểm tra xem có cookies nào không:
   - `.AspNetCore.Cookies`
   - `.AspNetCore.Session`
   - `.AspNetCore.Correlation.*`

### Bước 4: Thử đăng nhập trong chế độ ẩn danh

Đôi khi cookies cũ có thể gây vấn đề. Thử:
- Mở cửa sổ ẩn danh
- Truy cập `http://zelahahaha.site`
- Thử đăng nhập

### Bước 5: Kiểm tra có reverse proxy không

Nếu có nginx hoặc apache phía trước, có thể cần cấu hình thêm:

**Nginx:**
```nginx
proxy_cookie_path / "/; SameSite=Lax";
proxy_set_header Host $host;
proxy_set_header X-Real-IP $remote_addr;
proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto $scheme;
```

## 🐛 Debug

Nếu vẫn lỗi, kiểm tra logs chi tiết:

```bash
# Xem logs khi đăng nhập
docker logs -f zela-app

# Trong browser, mở Developer Tools > Network tab
# Xem request đến /signin-google
# Kiểm tra cookies trong request headers
```

## 💡 Giải pháp thay thế (nếu vẫn không được)

Nếu vẫn không được, có thể thử:

1. **Dùng HTTPS** - Một số browser block cookies trên HTTP
2. **Cấu hình domain cụ thể** cho cookies
3. **Kiểm tra firewall/proxy** có block cookies không

## 📝 Lưu ý

- Sau khi rebuild, **xóa cookies cũ** trong browser hoặc dùng chế độ ẩn danh
- Đảm bảo **redirect URI trong Google Console** đúng: `http://zelahahaha.site/signin-google`
- Nếu có **reverse proxy**, cần cấu hình đúng headers

