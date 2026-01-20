# 🔍 DEBUG CERTBOT - "No renewals were attempted"

## ❌ VẤN ĐỀ

Certbot vẫn báo "No renewals were attempted" - certificate chưa được tạo.

---

## 🔍 CÁC BƯỚC DEBUG

### **Bước 1: Kiểm tra challenge path có accessible không**

```bash
# Test từ localhost
curl http://localhost/.well-known/acme-challenge/test

# Test từ IP
curl http://34.124.228.222/.well-known/acme-challenge/test

# Test từ domain
curl http://zelahahaha.site/.well-known/acme-challenge/test
```

**Kết quả mong đợi:** Phải có response (404 hoặc 403), không phải connection refused

---

### **Bước 2: Kiểm tra volume mount**

```bash
# Kiểm tra volume certbot_www
docker compose exec nginx ls -la /var/www/certbot

# Tạo file test
docker compose run --rm certbot sh -c "echo 'test123' > /var/www/certbot/test.txt"

# Kiểm tra file từ Nginx
docker compose exec nginx cat /var/www/certbot/test.txt

# Test từ browser/curl
curl http://34.124.228.222/.well-known/acme-challenge/test.txt
```

**Kết quả mong đợi:** Phải thấy nội dung "test123"

---

### **Bước 3: Chạy Certbot với verbose để xem lỗi chi tiết**

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

Flag `--verbose` sẽ hiển thị thông tin chi tiết về lỗi.

---

### **Bước 4: Kiểm tra logs Certbot**

```bash
# Xem logs chi tiết
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -50"
```

---

### **Bước 5: Test với standalone mode (nếu webroot không hoạt động)**

Nếu webroot vẫn không hoạt động, thử standalone:

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

## 🚀 LỆNH ĐỂ CHẠY NGAY

```bash
# 1. Test challenge path
curl http://34.124.228.222/.well-known/acme-challenge/test
curl http://zelahahaha.site/.well-known/acme-challenge/test

# 2. Kiểm tra volume
docker compose exec nginx ls -la /var/www/certbot

# 3. Tạo file test
docker compose run --rm certbot sh -c "echo 'test' > /var/www/certbot/test.txt"
curl http://34.124.228.222/.well-known/acme-challenge/test.txt

# 4. Chạy Certbot với verbose
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 5. Xem logs
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -50"
```

---

## 💡 GIẢI PHÁP THAY THẾ: Dùng standalone mode

Nếu webroot vẫn không hoạt động, dùng standalone (đơn giản hơn):

```bash
# 1. Dừng Nginx
docker compose stop nginx

# 2. Tạo certificate
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 3. Khởi động lại Nginx
docker compose start nginx

# 4. Kiểm tra certificate
docker compose exec certbot certbot certificates

# 5. Bật lại HTTPS (uncomment HTTPS server block)
# Sau đó reload Nginx
docker compose exec nginx nginx -s reload
```

