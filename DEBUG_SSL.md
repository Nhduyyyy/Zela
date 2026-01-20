# 🔍 DEBUG SSL CERTIFICATE - XỬ LÝ LỖI

## ❌ VẤN ĐỀ: "No renewals were attempted"

Kết quả này có nghĩa là Certbot không tìm thấy certificate nào để gia hạn, và cũng không tạo certificate mới.

---

## 🔍 CÁC BƯỚC KIỂM TRA

### **Bước 1: Kiểm tra certificate đã tồn tại chưa**

```bash
docker compose exec certbot certbot certificates
```

**Nếu có certificate:** Sẽ hiển thị thông tin certificate  
**Nếu không có:** Sẽ hiển thị "No certificates found"

---

### **Bước 2: Kiểm tra Nginx có serve challenge files không**

```bash
# Test xem Nginx có serve được /.well-known/acme-challenge/ không
curl http://zelahahaha.site/.well-known/acme-challenge/test
```

**Kết quả mong đợi:** 404 Not Found (vì file không tồn tại, nhưng path phải accessible)

**Nếu lỗi 502 hoặc connection refused:** Nginx chưa được cấu hình đúng

---

### **Bước 3: Kiểm tra cấu hình Nginx**

```bash
# Xem cấu hình Nginx
docker compose exec nginx cat /etc/nginx/conf.d/zela.conf | head -20
```

**Đảm bảo có phần này:**
```nginx
location /.well-known/acme-challenge/ {
    root /var/www/certbot;
}
```

---

### **Bước 4: Kiểm tra domain đã trỏ về IP chưa**

```bash
# Kiểm tra DNS
nslookup zelahahaha.site
# hoặc
dig zelahahaha.site
```

**Đảm bảo:** A record trỏ về IP VPS của bạn

---

### **Bước 5: Kiểm tra port 80 đã mở chưa**

```bash
# Test từ bên ngoài
curl -I http://zelahahaha.site
```

**Kết quả mong đợi:** HTTP/1.1 301 hoặc 200

---

## 🔧 GIẢI PHÁP

### **Giải pháp 1: Tạo certificate với --force-renewal**

Nếu certificate đã tồn tại nhưng bạn muốn tạo lại:

```bash
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

### **Giải pháp 2: Kiểm tra và sửa cấu hình Nginx**

Đảm bảo HTTP server block có phần này:

```nginx
server {
    listen 80;
    server_name zelahahaha.site www.zelahahaha.site;

    # Certbot challenge location
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect tất cả HTTP requests sang HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}
```

**Sau đó reload Nginx:**
```bash
docker compose exec nginx nginx -t
docker compose exec nginx nginx -s reload
```

---

### **Giải pháp 3: Tạo certificate với standalone mode (tạm thời dừng Nginx)**

Nếu webroot không hoạt động, thử standalone:

```bash
# Dừng Nginx tạm thời
docker compose stop nginx

# Tạo certificate với standalone
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# Khởi động lại Nginx
docker compose start nginx
```

---

### **Giải pháp 4: Xem logs chi tiết**

```bash
# Xem logs Certbot
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

Flag `--verbose` sẽ hiển thị thông tin chi tiết hơn.

---

## 🚀 THỬ LẠI VỚI CÁC BƯỚC SAU

### **Bước 1: Đảm bảo Nginx đang chạy và cấu hình đúng**

```bash
# Kiểm tra Nginx
docker compose ps nginx

# Test cấu hình
docker compose exec nginx nginx -t

# Reload Nginx
docker compose exec nginx nginx -s reload
```

### **Bước 2: Test webroot path**

```bash
# Tạo file test trong certbot container
docker compose run --rm certbot sh -c "echo 'test' > /var/www/certbot/test.txt"

# Test từ Nginx
curl http://zelahahaha.site/.well-known/acme-challenge/test.txt
```

**Nếu không thấy nội dung file:** Có vấn đề với volume mount

### **Bước 3: Tạo certificate lại với verbose**

```bash
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

## 📋 CHECKLIST

Trước khi tạo certificate, đảm bảo:

- [ ] Domain đã trỏ về IP VPS (kiểm tra với `nslookup`)
- [ ] Port 80 đã mở (test với `curl http://zelahahaha.site`)
- [ ] Nginx đang chạy (`docker compose ps nginx`)
- [ ] Nginx cấu hình đúng (có `/.well-known/acme-challenge/` location)
- [ ] Volumes đã được mount đúng (`certbot_www` volume)

---

## 💡 LỆNH NHANH ĐỂ THỬ LẠI

```bash
# 1. Reload Nginx
docker compose exec nginx nginx -s reload

# 2. Tạo certificate với verbose
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 3. Kiểm tra kết quả
docker compose exec certbot certbot certificates
```

