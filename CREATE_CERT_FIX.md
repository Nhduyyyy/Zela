# 🔧 SỬA LỖI CERTBOT BỊ ĐỨNG

## ❌ VẤN ĐỀ

Certbot bị đứng (hang) khi chạy standalone mode. Có thể do:
- Certbot container không thể bind vào port 80
- Firewall chặn (nhưng không có ufw)
- Network issue trong Docker

---

## ✅ GIẢI PHÁP: Expose ports cho Certbot

Đã cập nhật `docker-compose.yml` để expose ports 80 và 443 cho Certbot container.

---

## 🚀 CÁC BƯỚC

### **Bước 1: Đảm bảo Nginx đã dừng**

```bash
docker compose stop nginx
docker compose ps nginx
```

---

### **Bước 2: Restart Certbot service để áp dụng cấu hình mới**

```bash
# Không cần restart vì certbot chỉ chạy khi gọi docker compose run
# Nhưng cần đảm bảo volumes đã được tạo
docker compose up -d certbot
docker compose stop certbot
```

---

### **Bước 3: Tạo certificate với verbose**

```bash
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

### **Bước 4: Nếu vẫn bị đứng, thử với network mode host**

Nếu expose ports vẫn không hoạt động, thử dùng network mode host:

```bash
# Dừng Nginx
docker compose stop nginx

# Chạy Certbot với network host
docker run --rm \
    --network host \
    -v zela_certbot_etc:/etc/letsencrypt \
    -v zela_certbot_www:/var/www/certbot \
    certbot/certbot:latest certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

## 🚀 LỆNH NHANH

```bash
# 1. Đảm bảo Nginx đã dừng
docker compose stop nginx

# 2. Tạo certificate (với ports đã được expose)
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 3. Nếu vẫn bị đứng, thử với network host
docker run --rm \
    --network host \
    -v zela_certbot_etc:/etc/letsencrypt \
    -v zela_certbot_www:/var/www/certbot \
    certbot/certbot:latest certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

---

## 💡 GIẢI PHÁP THAY THẾ: Dùng certbot trên host

Nếu Docker vẫn có vấn đề, có thể cài Certbot trực tiếp trên VPS:

```bash
# Cài Certbot
sudo apt-get update
sudo apt-get install certbot -y

# Dừng Nginx
docker compose stop nginx

# Tạo certificate
sudo certbot certonly --standalone \
    --preferred-challenges http \
    -d zelahahaha.site \
    -d www.zelahahaha.site \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email

# Copy certificates vào Docker volume
sudo cp /etc/letsencrypt/live/zelahahaha.site/fullchain.pem $(docker volume inspect zela_certbot_etc -f '{{.Mountpoint}}')/live/zelahahaha.site/fullchain.pem
sudo cp /etc/letsencrypt/live/zelahahaha.site/privkey.pem $(docker volume inspect zela_certbot_etc -f '{{.Mountpoint}}')/live/zelahahaha.site/privkey.pem

# Khởi động lại Nginx
docker compose start nginx
```

