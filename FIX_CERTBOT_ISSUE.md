# 🔧 SỬA LỖI CERTBOT "No renewals were attempted"

## ❌ VẤN ĐỀ

Certbot vẫn báo "No renewals were attempted" ngay cả với standalone mode.

---

## 🔍 CÁC BƯỚC DEBUG

### **Bước 1: Kiểm tra certificate đã tồn tại chưa**

```bash
# Kiểm tra certificates hiện có
docker compose exec certbot certbot certificates

# Hoặc kiểm tra file trực tiếp
docker compose exec certbot ls -la /etc/letsencrypt/live/
```

**Nếu có certificate:** Certbot sẽ không tạo mới, chỉ gia hạn  
**Nếu không có:** Có vấn đề khác

---

### **Bước 2: Xem logs chi tiết**

```bash
# Xem logs Certbot
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -100"
```

---

### **Bước 3: Thử với --force-renewal**

Nếu certificate đã tồn tại, thử force renewal:

```bash
docker compose stop nginx

docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d zelahahaha.site \
    -d www.zelahahaha.site

docker compose start nginx
```

---

### **Bước 4: Thử với --dry-run (test mode)**

Để test xem có lỗi gì không:

```bash
docker compose stop nginx

docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --dry-run \
    -d zelahahaha.site \
    -d www.zelahahaha.site

docker compose start nginx
```

---

### **Bước 5: Kiểm tra port 80 có bị chặn không**

```bash
# Kiểm tra port 80
sudo netstat -tulpn | grep :80
# hoặc
sudo ss -tulpn | grep :80
```

**Đảm bảo:** Không có process nào đang dùng port 80 (trừ khi Nginx đang chạy)

---

### **Bước 6: Thử với verbose để xem lỗi chi tiết**

```bash
docker compose stop nginx

docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site

docker compose start nginx
```

---

## 🚀 LỆNH ĐỂ CHẠY NGAY

```bash
# 1. Kiểm tra certificates hiện có
docker compose exec certbot certbot certificates

# 2. Xem logs
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -100"

# 3. Thử với force-renewal
docker compose stop nginx
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
docker compose start nginx
```

---

## 💡 GIẢI PHÁP THAY THẾ: Xóa và tạo lại

Nếu certificate đã tồn tại nhưng có vấn đề:

```bash
# Xóa certificate cũ (nếu có)
docker compose run --rm certbot delete --cert-name zelahahaha.site

# Tạo lại từ đầu
docker compose stop nginx
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site
docker compose start nginx
```

