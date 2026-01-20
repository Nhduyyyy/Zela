# 🔐 HƯỚNG DẪN TẠO SSL CERTIFICATE - TỪNG LỆNH

## 📋 CHUẨN BỊ

Trước khi bắt đầu, đảm bảo:
- ✅ Domain đã trỏ về IP VPS (A record)
- ✅ Port 80 và 443 đã mở trên firewall
- ✅ Docker Compose đã được cập nhật với Certbot service

---

## 🚀 CÁC BƯỚC THỰC HIỆN

### **Bước 1: Kiểm tra Nginx đang chạy**

```bash
docker compose ps nginx
```

**Kết quả mong đợi:** Container `zela-nginx` có status `Up`

**Nếu chưa chạy, khởi động:**
```bash
docker compose up -d nginx
```

**Đợi 5 giây để Nginx khởi động xong:**
```bash
sleep 5
```

---

### **Bước 2: Kiểm tra cấu hình Nginx**

```bash
docker compose exec nginx nginx -t
```

**Kết quả mong đợi:** 
```
nginx: configuration file /etc/nginx/nginx.conf test is successful
```

---

### **Bước 3: Tạo SSL Certificate với Certbot**

**Thay đổi các thông tin sau:**
- `your-email@example.com` → Email của bạn
- `zelahahaha.site` → Domain của bạn

**Lệnh tạo certificate:**

```bash
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email your-email@example.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

**Giải thích các tham số:**
- `--webroot`: Sử dụng webroot method (không cần dừng Nginx)
- `--webroot-path=/var/www/certbot`: Thư mục để Certbot đặt challenge files
- `--email`: Email của bạn (để nhận thông báo từ Let's Encrypt)
- `--agree-tos`: Đồng ý với điều khoản
- `--no-eff-email`: Không đăng ký nhận email từ EFF
- `-d`: Domain name (có thể thêm nhiều domain)

**Kết quả mong đợi:**
```
Successfully received certificate.
Certificate is saved at: /etc/letsencrypt/live/zelahahaha.site/fullchain.pem
Key is saved at:         /etc/letsencrypt/live/zelahahaha.site/privkey.pem
```

---

### **Bước 4: Kiểm tra certificate đã được tạo**

```bash
docker compose exec certbot certbot certificates
```

**Kết quả mong đợi:** Hiển thị thông tin certificate vừa tạo

**Hoặc kiểm tra file trực tiếp:**
```bash
docker compose exec nginx ls -la /etc/letsencrypt/live/
```

**Kết quả mong đợi:** Thấy thư mục `zelahahaha.site` với các file:
- `fullchain.pem` (certificate)
- `privkey.pem` (private key)

---

### **Bước 5: Reload Nginx để áp dụng SSL**

```bash
docker compose exec nginx nginx -s reload
```

**Kết quả mong đợi:** Không có lỗi (exit code 0)

---

### **Bước 6: Kiểm tra HTTPS hoạt động**

**Test từ command line:**
```bash
curl -I https://zelahahaha.site
```

**Kết quả mong đợi:**
```
HTTP/2 200 
server: nginx/1.29.4
...
```

**Test redirect HTTP → HTTPS:**
```bash
curl -I http://zelahahaha.site
```

**Kết quả mong đợi:**
```
HTTP/1.1 301 Moved Permanently
Location: https://zelahahaha.site/...
```

**Kiểm tra trong browser:**
- Mở: `https://zelahahaha.site`
- Click vào icon khóa để xem certificate
- Đảm bảo không có cảnh báo

---

## 🔍 KIỂM TRA CHI TIẾT

### **Xem thông tin certificate:**

```bash
docker compose exec certbot certbot certificates
```

**Kết quả sẽ hiển thị:**
- Domain name
- Expiry date (ngày hết hạn)
- Certificate path

### **Kiểm tra ngày hết hạn:**

```bash
docker compose exec certbot certbot certificates | grep "Expiry"
```

### **Xem logs Certbot:**

```bash
docker logs zela-certbot
```

### **Xem logs Nginx:**

```bash
docker logs zela-nginx
```

---

## 🐛 XỬ LÝ LỖI

### **Lỗi: Domain không trỏ về IP**

**Triệu chứng:**
```
Failed to verify domain ownership
```

**Giải pháp:**
```bash
# Kiểm tra DNS
nslookup zelahahaha.site
# hoặc
dig zelahahaha.site

# Đảm bảo A record trỏ về IP VPS
# Đợi DNS propagate (có thể mất vài phút đến vài giờ)
```

### **Lỗi: Port 80 bị chặn**

**Triệu chứng:**
```
Connection refused on port 80
```

**Giải pháp:**
```bash
# Kiểm tra firewall
sudo ufw status

# Mở port 80 và 443
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### **Lỗi: Nginx không tìm thấy certificate**

**Triệu chứng:**
```
502 Bad Gateway hoặc SSL error
```

**Giải pháp:**
```bash
# Kiểm tra certificate đã tồn tại
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/

# Kiểm tra Nginx có mount đúng volume không
docker compose exec nginx ls -la /etc/letsencrypt/live/

# Restart Nginx
docker compose restart nginx
```

### **Lỗi: Certificate đã tồn tại**

**Triệu chứng:**
```
Certificate already exists
```

**Giải pháp:**
```bash
# Xem certificates hiện có
docker compose exec certbot certbot certificates

# Nếu muốn tạo lại, dùng --force-renewal
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email your-email@example.com \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

## 🔄 GIA HẠN CERTIFICATE (Nếu cần thủ công)

Certificate sẽ tự động được gia hạn bởi certbot container, nhưng nếu muốn gia hạn thủ công:

```bash
# Gia hạn certificate
docker compose exec certbot certbot renew

# Reload Nginx sau khi gia hạn
docker compose exec nginx nginx -s reload
```

**Hoặc force renewal:**
```bash
# Force renewal (tạo lại ngay lập tức)
docker compose exec certbot certbot renew --force-renewal

# Reload Nginx
docker compose exec nginx nginx -s reload
```

---

## 📝 TÓM TẮT CÁC LỆNH

```bash
# 1. Kiểm tra Nginx
docker compose ps nginx

# 2. Test cấu hình Nginx
docker compose exec nginx nginx -t

# 3. Tạo certificate (THAY ĐỔI EMAIL VÀ DOMAIN)
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email your-email@example.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 4. Kiểm tra certificate
docker compose exec certbot certbot certificates

# 5. Reload Nginx
docker compose exec nginx nginx -s reload

# 6. Test HTTPS
curl -I https://zelahahaha.site
```

---

## ✅ CHECKLIST

- [ ] Domain đã trỏ về IP VPS
- [ ] Port 80 và 443 đã mở
- [ ] Nginx đang chạy
- [ ] Đã chạy lệnh tạo certificate
- [ ] Certificate đã được tạo thành công
- [ ] Đã reload Nginx
- [ ] HTTPS hoạt động (test trong browser)
- [ ] HTTP redirect sang HTTPS

---

**Sau khi hoàn tất, website của bạn sẽ có HTTPS! 🔒**

