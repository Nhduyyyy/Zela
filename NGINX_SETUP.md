# 🚀 HƯỚNG DẪN SỬ DỤNG NGINX VỚI ZELA

## 📋 TỔNG QUAN

Dự án Zela hiện đã được cấu hình với **Nginx** làm reverse proxy. Nginx sẽ:
- Nhận tất cả requests từ internet (port 80, 443)
- Forward requests đến ứng dụng Zela (internal)
- Hỗ trợ WebSocket cho SignalR hubs
- Tối ưu hóa static files
- Sẵn sàng cho SSL/HTTPS

---

## 🏗️ KIẾN TRÚC MỚI

```
Internet
   │
   ▼
┌─────────────────┐
│   Nginx (80/443)│  ← Reverse Proxy
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   zela-app:80   │  ← Application (internal only)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   sqlserver:1433│  ← Database
└─────────────────┘
```

---

## 📁 CẤU TRÚC FILE

### **Thư mục nginx:**

```
nginx/
├── nginx.conf          # Cấu hình chính Nginx
└── conf.d/
    └── zela.conf       # Cấu hình cho ứng dụng Zela
```

### **File cấu hình:**

1. **`nginx/nginx.conf`** - Cấu hình chính:
   - Worker processes
   - Logging
   - Gzip compression
   - MIME types

2. **`nginx/conf.d/zela.conf`** - Cấu hình ứng dụng:
   - Upstream backend (zela-app:80)
   - HTTP server (port 80)
   - HTTPS server (port 443 - commented, sẵn sàng)
   - WebSocket support cho SignalR
   - Static files caching

---

## 🚀 DEPLOY VỚI NGINX

### **Bước 1: Đảm bảo có file cấu hình Nginx**

File đã được tạo sẵn:
- `nginx/nginx.conf`
- `nginx/conf.d/zela.conf`

### **Bước 2: Cập nhật docker-compose.yml**

File `docker-compose.yml` đã được cập nhật với:
- Service `nginx` mới
- `zela-app` không còn expose ports ra ngoài
- Nginx expose ports 80 và 443

### **Bước 3: Build và chạy**

```bash
# Build và chạy tất cả services (bao gồm Nginx)
docker compose up -d --build
```

### **Bước 4: Kiểm tra**

```bash
# Kiểm tra containers
docker compose ps

# Kiểm tra Nginx logs
docker logs zela-nginx

# Test ứng dụng
curl http://localhost:80
# hoặc mở browser: http://your-vps-ip
```

---

## ⚙️ CẤU HÌNH

### **1. Domain Name**

Cập nhật domain trong `nginx/conf.d/zela.conf`:

```nginx
server_name zelahahaha.site www.zelahahaha.site;
```

Thay `zelahahaha.site` bằng domain của bạn.

### **2. Base URL**

Cập nhật `AppSettings__BaseUrl` trong `docker-compose.yml`:

```yaml
- AppSettings__BaseUrl=http://zelahahaha.site
# hoặc
- AppSettings__BaseUrl=https://zelahahaha.site  # nếu có SSL
```

### **3. Ports**

- **Port 80:** HTTP (Nginx)
- **Port 443:** HTTPS (Nginx) - sẵn sàng khi có SSL
- **Port 1433:** SQL Server (có thể đóng nếu không cần truy cập từ ngoài)

---

## 🔒 THIẾT LẬP SSL/HTTPS

### **Cách 1: Sử dụng Let's Encrypt (Certbot)**

```bash
# 1. Cài đặt Certbot trên VPS (không trong container)
sudo apt-get update
sudo apt-get install certbot python3-certbot-nginx -y

# 2. Tạo SSL certificate
sudo certbot --nginx -d zelahahaha.site -d www.zelahahaha.site

# 3. Certbot sẽ tự động cập nhật cấu hình Nginx
```

**Lưu ý:** Nếu Nginx chạy trong Docker, cần:
- Mount certificate vào container
- Hoặc dùng certbot trong container
- Hoặc dùng nginx-proxy với letsencrypt-nginx-proxy-companion

### **Cách 2: Manual SSL (nếu có certificate)**

1. Tạo thư mục SSL:
```bash
mkdir -p nginx/ssl
```

2. Copy certificate vào:
```bash
cp your-cert.pem nginx/ssl/cert.pem
cp your-key.pem nginx/ssl/key.pem
```

3. Uncomment HTTPS server block trong `nginx/conf.d/zela.conf`

4. Cập nhật docker-compose.yml để mount SSL:
```yaml
nginx:
  volumes:
    - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    - ./nginx/conf.d:/etc/nginx/conf.d:ro
    - ./nginx/ssl:/etc/nginx/ssl:ro  # Thêm dòng này
```

5. Restart Nginx:
```bash
docker compose restart nginx
```

---

## 🔍 MONITORING & TROUBLESHOOTING

### **Xem logs:**

```bash
# Nginx access logs
docker exec zela-nginx tail -f /var/log/nginx/zela_access.log

# Nginx error logs
docker exec zela-nginx tail -f /var/log/nginx/zela_error.log

# Nginx general logs
docker logs zela-nginx -f

# Application logs
docker logs zela-app -f
```

### **Test cấu hình Nginx:**

```bash
# Kiểm tra cấu hình Nginx
docker exec zela-nginx nginx -t

# Reload Nginx (không restart)
docker exec zela-nginx nginx -s reload
```

### **Kiểm tra kết nối:**

```bash
# Test từ bên ngoài
curl -I http://your-vps-ip

# Test WebSocket (SignalR)
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: test" \
  http://your-vps-ip/chathub
```

---

## 🐛 XỬ LÝ LỖI

### **Lỗi: 502 Bad Gateway**

**Nguyên nhân:** Nginx không kết nối được đến zela-app

**Giải pháp:**
```bash
# 1. Kiểm tra zela-app đã chạy chưa
docker compose ps

# 2. Kiểm tra network
docker network inspect zela_zela-network

# 3. Test kết nối từ Nginx đến app
docker exec zela-nginx wget -O- http://zela-app:80

# 4. Kiểm tra logs
docker logs zela-app
docker logs zela-nginx
```

### **Lỗi: WebSocket không hoạt động**

**Nguyên nhân:** Cấu hình WebSocket chưa đúng

**Giải pháp:**
1. Kiểm tra cấu hình SignalR hubs trong `nginx/conf.d/zela.conf`
2. Đảm bảo có headers:
   - `Upgrade: websocket`
   - `Connection: upgrade`
3. Restart Nginx:
```bash
docker compose restart nginx
```

### **Lỗi: Static files không load**

**Nguyên nhân:** Path hoặc permissions

**Giải pháp:**
- Static files được serve bởi ứng dụng (không phải Nginx)
- Kiểm tra volume mount trong docker-compose.yml
- Kiểm tra permissions: `chmod -R 755 wwwroot/`

### **Lỗi: 413 Request Entity Too Large**

**Nguyên nhân:** File upload quá lớn

**Giải pháp:**
Cập nhật `client_max_body_size` trong `nginx/conf.d/zela.conf`:
```nginx
client_max_body_size 100M;  # Tăng lên nếu cần
```

Sau đó reload:
```bash
docker exec zela-nginx nginx -s reload
```

---

## 📊 TỐI ƯU HÓA

### **1. Static Files Caching**

Nginx đã được cấu hình cache static files 30 ngày:
```nginx
location ~* \.(jpg|jpeg|png|gif|ico|css|js|woff|woff2|ttf|svg|webp|mp4|mp3)$ {
    expires 30d;
    add_header Cache-Control "public, immutable";
}
```

### **2. Gzip Compression**

Đã bật Gzip trong `nginx/nginx.conf` để giảm bandwidth.

### **3. Keep-Alive Connections**

Upstream đã được cấu hình với `keepalive 32` để tái sử dụng connections.

---

## 🔄 CẬP NHẬT

### **Cập nhật cấu hình Nginx:**

1. Sửa file cấu hình:
```bash
nano nginx/conf.d/zela.conf
```

2. Test cấu hình:
```bash
docker exec zela-nginx nginx -t
```

3. Reload Nginx:
```bash
docker compose restart nginx
# hoặc
docker exec zela-nginx nginx -s reload
```

### **Cập nhật ứng dụng:**

```bash
git pull
docker compose up -d --build
```

---

## 📝 LƯU Ý QUAN TRỌNG

1. ✅ **Nginx expose ports 80/443** - zela-app không expose ra ngoài
2. ✅ **WebSocket support** - Đã cấu hình cho SignalR hubs
3. ✅ **Static files** - Vẫn được serve bởi ứng dụng (có thể tối ưu sau)
4. ✅ **SSL ready** - Cấu hình HTTPS đã sẵn sàng, chỉ cần uncomment
5. ✅ **Logs** - Nginx logs được lưu trong volume `nginx_logs`

---

## 🎯 SO SÁNH TRƯỚC VÀ SAU

### **Trước (không có Nginx):**
```
Internet → zela-app:80 (expose trực tiếp)
```

### **Sau (có Nginx):**
```
Internet → nginx:80 → zela-app:80 (internal)
```

**Lợi ích:**
- ✅ Bảo mật tốt hơn (app không expose trực tiếp)
- ✅ Dễ dàng thêm SSL/HTTPS
- ✅ Load balancing (nếu cần scale)
- ✅ Static files optimization
- ✅ Better logging và monitoring

---

## 📚 TÀI LIỆU THAM KHẢO

- [Nginx Documentation](https://nginx.org/en/docs/)
- [Nginx Reverse Proxy](https://docs.nginx.com/nginx/admin-guide/web-server/reverse-proxy/)
- [Nginx WebSocket Support](https://nginx.org/en/docs/http/websocket.html)
- [Let's Encrypt](https://letsencrypt.org/)

---

**Tạo bởi:** Setup Nginx cho Zela  
**Ngày:** 2025-01-14  
**Version:** 1.0

