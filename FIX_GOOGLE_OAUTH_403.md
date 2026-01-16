# 🔧 Sửa lỗi Google OAuth 403

## 🔍 Nguyên nhân

Lỗi `Login?error=403` khi đăng nhập Google thường do:
1. **Redirect URI không khớp** với cấu hình trong Google Console
2. **BaseUrl chưa được cập nhật** với domain mới
3. **Callback path** không được cấu hình đúng

## ✅ Các bước sửa

### Bước 1: Cập nhật BaseUrl với domain của bạn

Cập nhật `appsettings.json` hoặc environment variable trong `docker-compose.yml`:

```json
"AppSettings": {
  "BaseUrl": "https://yourdomain.com"
}
```

Hoặc trong `docker-compose.yml`:
```yaml
- AppSettings__BaseUrl=https://yourdomain.com
```

**Lưu ý:** Dùng `https://` nếu có SSL, hoặc `http://` nếu chưa có.

### Bước 2: Cấu hình Redirect URI trong Google Console

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Chọn project của bạn
3. Vào **APIs & Services** > **Credentials**
4. Click vào OAuth 2.0 Client ID của bạn
5. Trong phần **Authorized redirect URIs**, thêm các URI sau:

```
https://yourdomain.com/signin-google
http://yourdomain.com/signin-google  (nếu chưa có SSL)
```

**Lưu ý:** 
- Thay `yourdomain.com` bằng domain thực tế của bạn
- Callback path mặc định của ASP.NET Core Google OAuth là `/signin-google`
- Nếu dùng cả HTTP và HTTPS, thêm cả 2

### Bước 3: Restart container để áp dụng thay đổi

```bash
# Cập nhật docker-compose.yml với BaseUrl mới
# Sau đó restart
docker compose restart zela-app

# Hoặc nếu thay đổi environment variable
docker compose up -d --force-recreate zela-app
```

### Bước 4: Kiểm tra

1. Truy cập `https://yourdomain.com`
2. Click "Đăng nhập bằng Google"
3. Nếu vẫn lỗi, kiểm tra logs:
   ```bash
   docker logs zela-app --tail 50
   ```

## 🔍 Kiểm tra Callback Path

Callback path mặc định của ASP.NET Core:
- Google: `/signin-google`
- Facebook: `/signin-facebook`

Nếu bạn muốn tùy chỉnh, có thể thêm vào `Program.cs`:

```csharp
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/Account/GoogleResponse"; // Tùy chỉnh nếu cần
})
```

Nhưng phải đảm bảo redirect URI trong Google Console khớp với path này.

## 📝 Ví dụ cấu hình đầy đủ

### appsettings.json hoặc docker-compose.yml:
```json
{
  "AppSettings": {
    "BaseUrl": "https://yourdomain.com"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-client-secret"
    }
  }
}
```

### Google Console - Authorized redirect URIs:
```
https://yourdomain.com/signin-google
```

## ⚠️ Lưu ý quan trọng

1. **Domain phải khớp chính xác** - không có trailing slash
2. **Protocol phải khớp** - nếu dùng HTTPS thì redirect URI cũng phải HTTPS
3. **Sau khi thay đổi trong Google Console**, có thể mất vài phút để có hiệu lực
4. **Kiểm tra domain có đúng không** - không dùng IP, phải dùng domain

## 🐛 Debug

Nếu vẫn lỗi, kiểm tra:

```bash
# Xem logs chi tiết
docker logs zela-app --tail 100 | grep -i "google\|oauth\|redirect"

# Kiểm tra environment variables
docker exec zela-app printenv | grep -i "AppSettings\|Authentication"
```

