# 🔧 SỬA LỖI CERTIFICATE PATH

## ❌ VẤN ĐỀ

Nginx container không tìm thấy certificate:
```
cannot load certificate "/etc/letsencrypt/live/zelahahaha.site/fullchain.pem"
```

---

## 🔍 CÁC BƯỚC KIỂM TRA

### **Bước 1: Kiểm tra certificate trên host**

```bash
# Kiểm tra certificate có tồn tại trên host không
sudo ls -la /etc/letsencrypt/live/zelahahaha.site/
```

**Kết quả mong đợi:** Phải thấy `fullchain.pem` và `privkey.pem`

---

### **Bước 2: Kiểm tra Nginx container có thấy certificate không**

```bash
# Kiểm tra từ trong Nginx container
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/
```

**Nếu không thấy:** Volume mount chưa đúng hoặc cần restart container

---

### **Bước 3: Kiểm tra volume mount trong docker-compose.yml**

Đảm bảo có dòng:
```yaml
volumes:
  - /etc/letsencrypt:/etc/letsencrypt:ro
```

---

## ✅ GIẢI PHÁP

### **Giải pháp 1: Restart Nginx container**

```bash
# Restart Nginx để mount lại volume
docker compose restart nginx

# Kiểm tra lại
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/
```

---

### **Giải pháp 2: Kiểm tra quyền truy cập**

```bash
# Kiểm tra quyền trên host
sudo ls -la /etc/letsencrypt/live/zelahahaha.site/

# Nếu cần, cấp quyền đọc
sudo chmod 644 /etc/letsencrypt/live/zelahahaha.site/fullchain.pem
sudo chmod 600 /etc/letsencrypt/live/zelahahaha.site/privkey.pem
```

---

### **Giải pháp 3: Copy certificate vào volume (nếu cần)**

Nếu volume mount không hoạt động, có thể copy certificate vào Docker volume:

```bash
# Tìm path của volume
docker volume inspect zela_certbot_etc

# Copy certificate (thay VOLUME_PATH bằng path từ lệnh trên)
sudo cp /etc/letsencrypt/live/zelahahaha.site/fullchain.pem /var/lib/docker/volumes/zela_certbot_etc/_data/live/zelahahaha.site/fullchain.pem
sudo cp /etc/letsencrypt/live/zelahahaha.site/privkey.pem /var/lib/docker/volumes/zela_certbot_etc/_data/live/zelahahaha.site/privkey.pem
```

Nhưng cách tốt nhất là sửa volume mount trong docker-compose.yml.

---

## 🚀 LỆNH NHANH

```bash
# 1. Kiểm tra certificate trên host
sudo ls -la /etc/letsencrypt/live/zelahahaha.site/

# 2. Kiểm tra từ Nginx container
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/

# 3. Restart Nginx
docker compose restart nginx

# 4. Kiểm tra lại
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/

# 5. Test cấu hình
docker compose exec nginx nginx -t

# 6. Reload Nginx
docker compose exec nginx nginx -s reload
```

