# 🔐 TẠO SSL CERTIFICATE VỚI DNS CHALLENGE

## ✅ ƯU ĐIỂM DNS CHALLENGE

- ✅ **Không cần mở port 80** - Không cần cấu hình firewall
- ✅ **Không cần dừng Nginx** - Có thể chạy song song
- ✅ **Hỗ trợ wildcard certificate** - `*.zelahahaha.site`
- ✅ **An toàn hơn** - Không cần expose port ra ngoài

---

## 🚀 CÁC BƯỚC

### **Bước 1: Cài Certbot trên VPS (nếu chưa có)**

```bash
sudo apt-get update
sudo apt-get install certbot -y
```

---

### **Bước 2: Tạo certificate với DNS challenge**

```bash
sudo certbot certonly \
    --manual \
    --preferred-challenges=dns \
    --email nhatduyyy131104@gmail.com \
    --server https://acme-v02.api.letsencrypt.org/directory \
    --agree-tos \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

**Lưu ý:** 
- Nếu muốn wildcard certificate: thêm `-d *.zelahahaha.site`
- Certbot sẽ yêu cầu bạn tạo TXT record trong DNS

---

### **Bước 3: Tạo TXT record trong DNS**

Certbot sẽ hiển thị thông tin như sau:

```
Please deploy a DNS TXT record under the name
_acme-challenge.zelahahaha.site with the following value:

xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

Before continuing, verify the record is deployed.
```

**Các bước:**
1. Copy giá trị TXT record
2. Vào DNS provider của bạn (nơi quản lý domain)
3. Tạo TXT record:
   - **Name:** `_acme-challenge.zelahahaha.site` (hoặc `_acme-challenge`)
   - **Type:** TXT
   - **Value:** Giá trị mà Certbot cung cấp
   - **TTL:** 300 (hoặc mặc định)

4. Đợi DNS propagate (thường 1-5 phút, có thể lâu hơn)
5. Verify bằng lệnh:
   ```bash
   nslookup -type=TXT _acme-challenge.zelahahaha.site
   ```
   Hoặc:
   ```bash
   dig TXT _acme-challenge.zelahahaha.site
   ```

6. Nhấn Enter trong terminal để tiếp tục

---

### **Bước 4: Lặp lại cho domain thứ 2 (nếu có)**

Nếu bạn có `www.zelahahaha.site`, Certbot sẽ yêu cầu tạo TXT record thứ 2:
- **Name:** `_acme-challenge.www.zelahahaha.site`
- **Value:** Giá trị mới từ Certbot

---

### **Bước 5: Kiểm tra certificate đã được tạo**

```bash
# Xem certificates
sudo certbot certificates

# Kiểm tra file
sudo ls -la /etc/letsencrypt/live/zelahahaha.site/
```

---

### **Bước 6: Copy certificates vào Docker volume**

Sau khi có certificate, copy vào Docker volume để Nginx sử dụng:

```bash
# Tìm đường dẫn volume
docker volume inspect zela_certbot_etc

# Copy certificates (thay VOLUME_PATH bằng path từ lệnh trên)
sudo cp /etc/letsencrypt/live/zelahahaha.site/fullchain.pem /var/lib/docker/volumes/zela_certbot_etc/_data/live/zelahahaha.site/fullchain.pem
sudo cp /etc/letsencrypt/live/zelahahaha.site/privkey.pem /var/lib/docker/volumes/zela_certbot_etc/_data/live/zelahahaha.site/privkey.pem

# Hoặc dùng cách đơn giản hơn - mount trực tiếp
```

**Cách tốt hơn:** Mount trực tiếp từ host vào container

---

## 🔧 CẬP NHẬT DOCKER-COMPOSE.YML

Để Nginx có thể đọc certificates từ host:

```yaml
nginx:
  volumes:
    - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    - ./nginx/conf.d:/etc/nginx/conf.d:ro
    - /etc/letsencrypt:/etc/letsencrypt:ro  # Mount từ host
    - nginx_logs:/var/log/nginx
```

Sau đó restart:
```bash
docker compose restart nginx
```

---

## 🚀 LỆNH NHANH

```bash
# 1. Cài Certbot (nếu chưa có)
sudo apt-get update
sudo apt-get install certbot -y

# 2. Tạo certificate với DNS challenge
sudo certbot certonly \
    --manual \
    --preferred-challenges=dns \
    --email nhatduyyy131104@gmail.com \
    --server https://acme-v02.api.letsencrypt.org/directory \
    --agree-tos \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 3. Tạo TXT record trong DNS (theo hướng dẫn của Certbot)
# 4. Verify TXT record
nslookup -type=TXT _acme-challenge.zelahahaha.site

# 5. Nhấn Enter trong terminal để tiếp tục

# 6. Kiểm tra certificate
sudo certbot certificates

# 7. Cập nhật docker-compose.yml để mount /etc/letsencrypt
# 8. Restart Nginx
docker compose restart nginx
```

---

## 📝 LƯU Ý

1. **DNS TXT record phải được tạo trước khi nhấn Enter**
2. **Đợi DNS propagate** (1-5 phút, có thể lâu hơn)
3. **Verify TXT record** trước khi tiếp tục
4. **Wildcard certificate** cần TXT record ở root domain: `_acme-challenge.zelahahaha.site`

---

## 🔄 GIA HẠN CERTIFICATE

Certificate sẽ tự động được gia hạn bởi certbot container (nếu đã cấu hình). Hoặc gia hạn thủ công:

```bash
sudo certbot renew
```

---

## ✅ SAU KHI CÓ CERTIFICATE

1. Cập nhật `docker-compose.yml` để mount `/etc/letsencrypt`
2. Uncomment HTTPS server block trong `nginx/conf.d/zela.conf`
3. Bật redirect HTTP → HTTPS
4. Reload Nginx

