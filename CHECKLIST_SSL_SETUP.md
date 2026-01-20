# ✅ CHECKLIST KIỂM TRA CẤU HÌNH SSL

## 📋 KIỂM TRA CÁC FILE

### ✅ 1. docker-compose.yml

**Kiểm tra:**
- [x] Nginx mount `/etc/letsencrypt` từ host (dòng 65)
- [x] BaseUrl đã là HTTPS (dòng 36)
- [x] Nginx expose ports 80 và 443 (dòng 60-61)
- [x] Certbot service đã được cấu hình (dòng 76-87)

**Status:** ✅ OK

---

### ✅ 2. nginx/conf.d/zela.conf

**Kiểm tra:**
- [x] HTTP server block có challenge location (dòng 13-15)
- [x] HTTP server tạm thời không redirect (dòng 17-30)
- [x] HTTPS server block đã được comment (dòng 33-135)
- [x] Không có duplicate code

**Status:** ✅ OK (đã sửa duplicate)

---

### ✅ 3. nginx/nginx.conf

**Kiểm tra:**
- [x] Cấu hình chính hợp lệ
- [x] Include conf.d/*.conf (dòng 39)
- [x] Gzip compression đã bật
- [x] Client max body size = 50M

**Status:** ✅ OK

---

### ✅ 4. create-cert-dns.sh

**Kiểm tra:**
- [x] Script có thể thực thi
- [x] Domain và email đã được cấu hình
- [x] Lệnh Certbot đúng với DNS challenge
- [x] Có hướng dẫn bước tiếp theo

**Status:** ✅ OK

---

## 🚀 CÁC BƯỚC TIẾP THEO

### **Bước 1: Tạo Certificate**

```bash
# Chạy script
./create-cert-dns.sh

# Hoặc chạy thủ công
sudo certbot certonly \
    --manual \
    --preferred-challenges=dns \
    --email nhatduyyy131104@gmail.com \
    --server https://acme-v02.api.letsencrypt.org/directory \
    --agree-tos \
    -d zelahahaha.site \
    -d www.zelahahaha.site
```

### **Bước 2: Tạo TXT Record trong DNS**

Khi Certbot yêu cầu, tạo TXT record:
- **Name:** `_acme-challenge.zelahahaha.site`
- **Type:** TXT
- **Value:** (từ Certbot)
- Đợi DNS propagate (1-5 phút)
- Verify: `nslookup -type=TXT _acme-challenge.zelahahaha.site`
- Nhấn Enter để tiếp tục

### **Bước 3: Sau khi có Certificate**

1. **Uncomment HTTPS server block** trong `nginx/conf.d/zela.conf`
   - Xóa các dòng `#` ở đầu mỗi dòng trong HTTPS server block
   - Sửa HTTP redirect: uncomment `return 301` và comment `proxy_pass`

2. **Bật redirect HTTP → HTTPS**
   ```nginx
   location / {
       return 301 https://$server_name$request_uri;
   }
   ```

3. **Test cấu hình Nginx**
   ```bash
   docker compose exec nginx nginx -t
   ```

4. **Reload Nginx**
   ```bash
   docker compose exec nginx nginx -s reload
   ```

5. **Test HTTPS**
   ```bash
   curl -I https://zelahahaha.site
   ```

---

## 📝 TÓM TẮT

### ✅ Đã sẵn sàng:
- [x] docker-compose.yml đã mount /etc/letsencrypt
- [x] nginx/conf.d/zela.conf đã được cấu hình đúng
- [x] Script create-cert-dns.sh sẵn sàng
- [x] BaseUrl đã là HTTPS

### ⏳ Cần làm:
- [ ] Tạo certificate với DNS challenge
- [ ] Tạo TXT record trong DNS
- [ ] Uncomment HTTPS server block
- [ ] Bật redirect HTTP → HTTPS
- [ ] Test HTTPS

---

## 🎯 LỆNH NHANH SAU KHI CÓ CERTIFICATE

```bash
# 1. Uncomment HTTPS server block trong nginx/conf.d/zela.conf
# (Sửa file thủ công)

# 2. Test cấu hình
docker compose exec nginx nginx -t

# 3. Reload Nginx
docker compose exec nginx nginx -s reload

# 4. Test HTTPS
curl -I https://zelahahaha.site
```

---

**Tất cả file đã được kiểm tra và sẵn sàng! 🎉**

