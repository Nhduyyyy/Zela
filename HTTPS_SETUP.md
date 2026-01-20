# 🔐 HƯỚNG DẪN THIẾT LẬP HTTPS VỚI CERTBOT

## 📋 TỔNG QUAN

Dự án Zela đã được cấu hình để sử dụng **Let's Encrypt SSL certificates** với **Certbot** chạy trong Docker container. Certbot sẽ tự động:
- Tạo SSL certificate lần đầu
- Gia hạn certificate tự động (mỗi 12 giờ kiểm tra)
- Reload Nginx khi certificate được gia hạn

---

## 🚀 CÁC BƯỚC THIẾT LẬP

### **Bước 1: Chuẩn bị**

**Yêu cầu:**
- ✅ Domain đã trỏ về IP VPS (A record)
- ✅ Port 80 và 443 đã mở trên firewall
- ✅ Nginx container đang chạy

**Kiểm tra DNS:**
```bash
# Kiểm tra domain đã trỏ về IP chưa
nslookup zelahahaha.site
# hoặc
dig zelahahaha.site
```

### **Bước 2: Cập nhật thông tin trong script**

Mở file `init-ssl.sh` và cập nhật:
```bash
DOMAIN="zelahahaha.site"  # Domain của bạn
EMAIL="your-email@example.com"  # Email của bạn (để nhận thông báo)
```

### **Bước 3: Tạo SSL certificate lần đầu**

```bash
# Cấp quyền thực thi
chmod +x init-ssl.sh

# Chạy script
./init-ssl.sh
```

Script sẽ:
1. Kiểm tra Nginx đang chạy
2. Tạo SSL certificate với Certbot
3. Tự động reload Nginx
4. Thông báo kết quả

### **Bước 4: Kiểm tra HTTPS**

```bash
# Test HTTPS
curl -I https://zelahahaha.site

# Hoặc mở browser
# https://zelahahaha.site
```

---

## 🔄 TỰ ĐỘNG GIA HẠN CERTIFICATE

Certbot container đã được cấu hình để tự động:
- Kiểm tra certificate mỗi 12 giờ
- Gia hạn nếu còn < 30 ngày
- Nginx sẽ tự động reload khi certificate được gia hạn

**Không cần làm gì thêm!** Certificate sẽ tự động được gia hạn.

---

## 📁 CẤU TRÚC

### **Volumes:**
- `certbot_etc` - Lưu certificates và keys
- `certbot_www` - Webroot cho challenge validation

### **Containers:**
- `zela-certbot` - Certbot container (tự động renew)
- `zela-nginx` - Nginx với SSL support

---

## 🛠️ CÁC LỆNH HỮU ÍCH

### **Xem trạng thái Certbot:**
```bash
docker logs zela-certbot
```

### **Kiểm tra certificate:**
```bash
# Xem thông tin certificate
docker compose exec certbot certbot certificates

# Kiểm tra ngày hết hạn
docker compose exec certbot certbot certificates | grep "Expiry"
```

### **Gia hạn thủ công (nếu cần):**
```bash
docker compose exec certbot certbot renew --force-renewal
docker compose exec nginx nginx -s reload
```

### **Test cấu hình Nginx:**
```bash
docker compose exec nginx nginx -t
```

### **Reload Nginx:**
```bash
docker compose exec nginx nginx -s reload
```

---

## 🐛 XỬ LÝ LỖI

### **Lỗi: Domain không trỏ về IP**

**Triệu chứng:**
```
Failed to verify domain ownership
```

**Giải pháp:**
1. Kiểm tra DNS: `nslookup zelahahaha.site`
2. Đảm bảo A record trỏ về IP VPS
3. Đợi DNS propagate (có thể mất vài phút đến vài giờ)

### **Lỗi: Port 80 bị chặn**

**Triệu chứng:**
```
Connection refused on port 80
```

**Giải pháp:**
```bash
# Kiểm tra firewall
sudo ufw status

# Mở port 80
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### **Lỗi: Certificate không được tạo**

**Triệu chứng:**
```
Error creating certificate
```

**Giải pháp:**
1. Kiểm tra logs: `docker logs zela-certbot`
2. Đảm bảo Nginx đang chạy: `docker compose ps`
3. Kiểm tra domain đã trỏ về IP
4. Thử lại: `./init-ssl.sh`

### **Lỗi: 502 Bad Gateway sau khi có HTTPS**

**Nguyên nhân:** Nginx không tìm thấy certificate

**Giải pháp:**
```bash
# Kiểm tra certificate đã tồn tại
docker compose exec certbot ls -la /etc/letsencrypt/live/

# Kiểm tra Nginx có mount đúng volume không
docker compose exec nginx ls -la /etc/letsencrypt/live/

# Restart Nginx
docker compose restart nginx
```

---

## 📝 CẤU HÌNH CHI TIẾT

### **docker-compose.yml:**

```yaml
certbot:
  image: certbot/certbot:latest
  volumes:
    - certbot_etc:/etc/letsencrypt
    - certbot_www:/var/www/certbot
  entrypoint: "/bin/sh -c 'trap exit TERM; while :; do certbot renew; sleep 12h & wait $${!}; done;'"
```

### **nginx/conf.d/zela.conf:**

- HTTP server: Redirect sang HTTPS + hỗ trợ Certbot challenge
- HTTPS server: SSL configuration với Let's Encrypt certificates

---

## 🔒 BẢO MẬT

### **SSL Configuration:**
- ✅ TLS 1.2 và 1.3 only
- ✅ Modern cipher suites
- ✅ HSTS header
- ✅ Security headers (X-Frame-Options, X-Content-Type-Options, etc.)

### **Auto-renewal:**
- ✅ Tự động kiểm tra mỗi 12 giờ
- ✅ Tự động gia hạn khi còn < 30 ngày
- ✅ Nginx tự động reload

---

## ✅ KIỂM TRA SAU KHI THIẾT LẬP

1. **Test HTTPS:**
   ```bash
   curl -I https://zelahahaha.site
   ```

2. **Kiểm tra certificate:**
   ```bash
   openssl s_client -connect zelahahaha.site:443 -servername zelahahaha.site
   ```

3. **Test redirect HTTP → HTTPS:**
   ```bash
   curl -I http://zelahahaha.site
   # Phải thấy: HTTP/1.1 301 Moved Permanently
   ```

4. **Kiểm tra trong browser:**
   - Mở https://zelahahaha.site
   - Click vào icon khóa để xem certificate
   - Đảm bảo không có cảnh báo

---

## 🎯 TÓM TẮT

1. ✅ Cập nhật `DOMAIN` và `EMAIL` trong `init-ssl.sh`
2. ✅ Chạy `./init-ssl.sh` để tạo certificate
3. ✅ Kiểm tra HTTPS hoạt động
4. ✅ Certificate sẽ tự động được gia hạn

**Xong! Website của bạn giờ đã có HTTPS! 🔒**

---

**Tạo bởi:** Setup HTTPS với Certbot  
**Ngày:** 2025-01-14  
**Version:** 1.0

