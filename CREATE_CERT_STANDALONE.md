# 🔐 TẠO SSL CERTIFICATE VỚI STANDALONE MODE

## ✅ ƯU ĐIỂM

Standalone mode đơn giản hơn webroot:
- ✅ Không cần cấu hình webroot path
- ✅ Không cần volume mount phức tạp
- ✅ Certbot tự động serve challenge files

---

## 🚀 CÁC BƯỚC

### **Bước 1: Dừng Nginx tạm thời**

```bash
docker compose stop nginx
```

**Lý do:** Certbot standalone cần dùng port 80 để verify domain

---

### **Bước 2: Tạo certificate với standalone**

```bash
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

**Giải thích:**
- `--standalone`: Certbot tự động tạo web server để verify
- `--preferred-challenges http`: Dùng HTTP challenge (port 80)
- `-d`: Domain names

**Kết quả mong đợi:**
```
Successfully received certificate.
Certificate is saved at: /etc/letsencrypt/live/zelahahaha.site/fullchain.pem
Key is saved at:         /etc/letsencrypt/live/zelahahaha.site/privkey.pem
```

---

### **Bước 3: Khởi động lại Nginx**

```bash
docker compose start nginx
```

---

### **Bước 4: Kiểm tra certificate**

```bash
docker compose exec certbot certbot certificates
```

**Kết quả mong đợi:** Hiển thị thông tin certificate vừa tạo

---

### **Bước 5: Bật lại HTTPS trong Nginx**

Sau khi có certificate, cần uncomment HTTPS server block trong `nginx/conf.d/zela.conf`

**Cách 1: Sửa thủ công**
- Mở file `nginx/conf.d/zela.conf`
- Uncomment phần HTTPS server block (dòng 43-137)
- Sửa HTTP redirect (uncomment `return 301` và comment `proxy_pass`)

**Cách 2: Dùng script** (nếu có)

---

### **Bước 6: Test cấu hình và reload Nginx**

```bash
# Test cấu hình
docker compose exec nginx nginx -t

# Reload Nginx
docker compose exec nginx nginx -s reload
```

---

### **Bước 7: Test HTTPS**

```bash
# Test HTTPS
curl -I https://zelahahaha.site

# Test redirect HTTP → HTTPS
curl -I http://zelahahaha.site
```

---

## 📝 TÓM TẮT CÁC LỆNH

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

# 5. Bật lại HTTPS (sửa file nginx/conf.d/zela.conf)
# - Uncomment HTTPS server block
# - Bật redirect HTTP → HTTPS

# 6. Test và reload Nginx
docker compose exec nginx nginx -t
docker compose exec nginx nginx -s reload

# 7. Test HTTPS
curl -I https://zelahahaha.site
```

---

## ⚠️ LƯU Ý

1. **Port 80 phải mở** trên firewall
2. **Domain phải trỏ về IP VPS** (đã kiểm tra ✅)
3. **Nginx phải dừng** khi tạo certificate (standalone mode)
4. **Sau khi có certificate**, nhớ bật lại HTTPS server block

---

## 🎯 SAU KHI CÓ CERTIFICATE

Cần sửa file `nginx/conf.d/zela.conf`:
1. Uncomment HTTPS server block
2. Bật redirect HTTP → HTTPS (uncomment `return 301`)

Sau đó reload Nginx.

