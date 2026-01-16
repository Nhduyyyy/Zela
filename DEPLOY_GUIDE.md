# 🚀 HƯỚNG DẪN DEPLOY ZELA LÊN VPS

## 📋 YÊU CẦU

- VPS đã cài đặt:
  - ✅ Git
  - ✅ Docker
  - ✅ Docker Compose

## 📝 CÁC BƯỚC DEPLOY

### Bước 1: Clone hoặc Pull code từ Git

```bash
# Nếu chưa clone, clone repository
git clone <your-repository-url>
cd Zelafinal

# Nếu đã clone, chỉ cần pull
git pull
```

### Bước 2: Cấu hình appsettings.json

**QUAN TRỌNG:** Bạn cần cập nhật file `appsettings.json` với các thông tin phù hợp:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=YourStrong@Password123!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;"
  },
  "AppSettings": {
    "BaseUrl": "http://your-vps-ip:80"
    // hoặc nếu có domain: "BaseUrl": "https://yourdomain.com"
  }
}
```

**Lưu ý:**
- `Server=sqlserver` - Tên service trong docker-compose.yml
- `Password=YourStrong@Password123!` - Phải khớp với password trong docker-compose.yml
- `BaseUrl` - Thay bằng IP hoặc domain của VPS

### Bước 3: Cập nhật docker-compose.yml (nếu cần)

Mở file `docker-compose.yml` và kiểm tra:

1. **SQL Server Password:**
   ```yaml
   environment:
     - SA_PASSWORD=YourStrong@Password123!  # Đổi password mạnh hơn
   ```

2. **Ports:**
   ```yaml
   ports:
     - "80:80"      # Port HTTP
     - "443:443"    # Port HTTPS (nếu cần)
   ```

3. **Connection String trong zela-app:**
   ```yaml
   environment:
     - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=YourStrong@Password123!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;
   ```
   **Lưu ý:** Password phải khớp với SA_PASSWORD ở trên

### Bước 4: Build và chạy Docker containers

```bash
# Build và chạy containers
docker compose up -d --build
```

Lệnh này sẽ:
- Build Docker image cho ứng dụng Zela
- Tạo và chạy SQL Server container
- Tạo và chạy Zela application container
- Tự động tạo network và volumes

### Bước 5: Chạy Migrations (Tạo database)

Sau khi containers đã chạy, bạn cần chạy migrations để tạo database:

```bash
# Vào trong container zela-app
docker exec -it zela-app bash

# Chạy migrations
dotnet ef database update

# Hoặc nếu không có ef tools trong container, chạy từ bên ngoài:
docker exec -it zela-app dotnet ef database update
```

**Nếu gặp lỗi:** Có thể cần cài đặt EF Core tools trong container hoặc chạy migrations từ máy local trước khi deploy.

### Bước 6: Kiểm tra logs

```bash
# Xem logs của ứng dụng
docker logs zela-app

# Xem logs của SQL Server
docker logs zela-sqlserver

# Xem logs real-time
docker logs -f zela-app
```

### Bước 7: Kiểm tra ứng dụng

Mở trình duyệt và truy cập:
- `http://your-vps-ip:80`
- Hoặc `http://yourdomain.com` (nếu đã cấu hình domain)

---

## 🔄 CẬP NHẬT ỨNG DỤNG (Khi có code mới)

Khi có code mới, chỉ cần chạy 2 lệnh:

```bash
git pull
docker compose up -d --build
```

Docker sẽ tự động:
- Pull code mới
- Build lại image
- Restart containers với code mới

---

## 🛠️ CÁC LỆNH HỮU ÍCH

### Xem trạng thái containers
```bash
docker compose ps
```

### Dừng containers
```bash
docker compose down
```

### Dừng và xóa volumes (⚠️ Xóa cả database)
```bash
docker compose down -v
```

### Restart containers
```bash
docker compose restart
```

### Xem logs
```bash
# Logs của tất cả services
docker compose logs

# Logs của một service cụ thể
docker compose logs zela-app
docker compose logs sqlserver

# Logs real-time
docker compose logs -f zela-app
```

### Vào trong container
```bash
# Vào container ứng dụng
docker exec -it zela-app bash

# Vào container SQL Server
docker exec -it zela-sqlserver bash
```

### Backup database
```bash
# Backup database
docker exec zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "YourStrong@Password123!" -Q "BACKUP DATABASE [Zela_FinalV2.0] TO DISK = '/var/opt/mssql/backup/Zela.bak'"

# Copy backup ra ngoài
docker cp zela-sqlserver:/var/opt/mssql/backup/Zela.bak ./backup/
```

---

## ⚠️ LƯU Ý QUAN TRỌNG

### 1. Bảo mật Password
- **ĐỔI PASSWORD SQL SERVER** trong docker-compose.yml thành password mạnh
- Không commit password vào Git
- Có thể dùng environment variables hoặc secrets

### 2. Firewall
Đảm bảo mở các ports cần thiết:
```bash
# Ubuntu/Debian
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 1433/tcp  # Nếu cần truy cập SQL từ bên ngoài
```

### 3. Domain và SSL
- Nếu có domain, cấu hình DNS trỏ về IP VPS
- Cài đặt SSL certificate (Let's Encrypt) cho HTTPS
- Cập nhật `BaseUrl` trong appsettings.json

### 4. File Uploads
- Thư mục `wwwroot/uploads` được mount vào container
- Đảm bảo có quyền ghi vào thư mục này

### 5. Migrations
- Chạy migrations lần đầu sau khi containers đã chạy
- Nếu có migrations mới, chạy lại: `docker exec -it zela-app dotnet ef database update`

---

## 🐛 XỬ LÝ LỖI THƯỜNG GẶP

### Lỗi: Cannot connect to SQL Server
```bash
# Kiểm tra SQL Server đã chạy chưa
docker logs zela-sqlserver

# Kiểm tra connection string trong appsettings.json
# Đảm bảo Server=sqlserver (tên service, không phải localhost)
```

### Lỗi: Port already in use
```bash
# Kiểm tra port nào đang dùng
sudo netstat -tulpn | grep :80

# Hoặc đổi port trong docker-compose.yml
ports:
  - "8080:80"  # Dùng port 8080 thay vì 80
```

### Lỗi: Permission denied
```bash
# Cấp quyền cho thư mục uploads
sudo chmod -R 755 wwwroot/uploads
sudo chown -R $USER:$USER wwwroot/uploads
```

### Lỗi: Database migration failed
```bash
# Kiểm tra SQL Server đã sẵn sàng chưa
docker exec -it zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "YourStrong@Password123!" -Q "SELECT 1"

# Chạy migration lại
docker exec -it zela-app dotnet ef database update
```

---

## 📞 HỖ TRỢ

Nếu gặp vấn đề, kiểm tra:
1. Logs: `docker compose logs`
2. Trạng thái containers: `docker compose ps`
3. Network: `docker network ls`
4. Volumes: `docker volume ls`

---

**Chúc bạn deploy thành công! 🎉**

