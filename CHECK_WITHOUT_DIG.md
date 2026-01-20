# 🔍 KIỂM TRA KHÔNG CẦN DIG/HOST

## 📋 CÁC CÁCH KIỂM TRA KHÁC

### **Cách 1: Dùng curl để test DNS**

```bash
# Test domain có resolve được không
curl -I http://zelahahaha.site

# So sánh với IP trực tiếp
curl -I http://34.124.228.222
```

**Nếu cả 2 đều có response:** Domain đã trỏ về IP  
**Nếu chỉ IP có response:** Domain chưa trỏ về IP

---

### **Cách 2: Test trực tiếp với IP**

```bash
# Test challenge path với IP
curl http://34.124.228.222/.well-known/acme-challenge/test
```

**Nếu có response (404 hoặc 403):** Nginx đang serve, vấn đề là DNS  
**Nếu không có response:** Vấn đề là cấu hình Nginx

---

### **Cách 3: Kiểm tra cấu hình Nginx**

```bash
# Test cấu hình
docker compose exec nginx nginx -t

# Xem cấu hình HTTP server
docker compose exec nginx cat /etc/nginx/conf.d/zela.conf | head -25
```

---

### **Cách 4: Kiểm tra volume mount**

```bash
# Kiểm tra volume certbot_www
docker compose exec nginx ls -la /var/www/certbot

# Tạo file test
docker compose run --rm certbot sh -c "echo 'test123' > /var/www/certbot/test.txt"

# Test file từ container
docker compose exec nginx cat /var/www/certbot/test.txt

# Test từ localhost
curl http://localhost/.well-known/acme-challenge/test.txt
```

---

### **Cách 5: Kiểm tra logs Nginx**

```bash
# Xem logs Nginx
docker logs zela-nginx --tail 50

# Xem error logs
docker compose exec nginx cat /var/log/nginx/error.log | tail -20
```

---

## 🚀 LỆNH ĐỂ CHẠY NGAY

```bash
# 1. Test domain vs IP
curl -I http://zelahahaha.site
curl -I http://34.124.228.222

# 2. Test challenge path với IP
curl http://34.124.228.222/.well-known/acme-challenge/test

# 3. Test cấu hình Nginx
docker compose exec nginx nginx -t

# 4. Kiểm tra volume
docker compose exec nginx ls -la /var/www/certbot

# 5. Tạo file test
docker compose run --rm certbot sh -c "echo 'test' > /var/www/certbot/test.txt"

# 6. Test file
curl http://localhost/.well-known/acme-challenge/test.txt
curl http://34.124.228.222/.well-known/acme-challenge/test.txt

# 7. Xem logs
docker logs zela-nginx --tail 20
```

