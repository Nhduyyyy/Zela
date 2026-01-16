# 🐳 HƯỚNG DẪN NHANH - DEPLOY ZELA VỚI DOCKER

## ⚡ CÁCH SỬ DỤNG NHANH

### Lần đầu tiên deploy:

```bash
# 1. Clone hoặc pull code
git pull

# 2. Cập nhật cấu hình (QUAN TRỌNG!)
# Mở file docker-compose.yml và đổi password SQL Server:
# - SA_PASSWORD=YourStrong@Password123!  (đổi thành password mạnh)
# - ConnectionStrings__DefaultConnection (đổi password cho khớp)

# 3. Build và chạy
docker compose up -d --build

# 4. Chạy migrations (tạo database)
# Đợi containers chạy xong (khoảng 1-2 phút), sau đó:
docker exec -it zela-app dotnet ef database update
```

### Cập nhật code mới:

```bash
git pull
docker compose up -d --build
```

**Xong!** 🎉

---

## 📋 CÁC FILE QUAN TRỌNG

- `Dockerfile` - File build Docker image
- `docker-compose.yml` - Chạy cả app + SQL Server
- `docker-compose.standalone.yml` - Chỉ chạy app (SQL Server riêng)
- `.dockerignore` - Loại bỏ file không cần thiết khi build

---

## ⚙️ CẤU HÌNH

### 1. Đổi Password SQL Server

Mở `docker-compose.yml`, tìm và đổi:
```yaml
environment:
  - SA_PASSWORD=YourStrong@Password123!  # ĐỔI PASSWORD NÀY
```

Và cập nhật connection string:
```yaml
- ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=YourStrong@Password123!;...
```

### 2. Cập nhật BaseUrl

Mở `appsettings.json`, cập nhật:
```json
"AppSettings": {
  "BaseUrl": "http://your-vps-ip:80"
}
```

---

## 🔍 KIỂM TRA

```bash
# Xem containers đang chạy
docker compose ps

# Xem logs
docker compose logs -f zela-app

# Kiểm tra ứng dụng
curl http://localhost:80
```

---

## 🆘 LỖI THƯỜNG GẶP

### Lỗi: Cannot connect to database
- Đợi SQL Server khởi động xong (khoảng 30-60 giây)
- Kiểm tra password đã khớp chưa
- Xem logs: `docker logs zela-sqlserver`

### Lỗi: Port 80 already in use
- Đổi port trong docker-compose.yml: `"8080:80"`
- Hoặc dừng service đang dùng port 80

### Lỗi: Migration failed
- Đảm bảo SQL Server đã chạy: `docker logs zela-sqlserver`
- Chạy lại: `docker exec -it zela-app dotnet ef database update`

---

## 📚 XEM THÊM

Chi tiết đầy đủ: `DEPLOY_GUIDE.md`

