# 🔍 TẠO CERTIFICATE VỚI VERBOSE - XEM LỖI CHI TIẾT

## 📋 TÌNH TRẠNG

- ✅ Không có certificate nào (đã kiểm tra)
- ❌ Certbot không tạo được certificate
- ❌ Báo "No renewals were attempted"

---

## 🔍 DEBUG VỚI VERBOSE

### **Bước 1: Đảm bảo Nginx đã dừng**

```bash
# Kiểm tra Nginx đã dừng chưa
docker compose ps nginx

# Nếu vẫn chạy, dừng lại
docker compose stop nginx
```

**QUAN TRỌNG:** Nginx phải dừng để Certbot standalone có thể dùng port 80

---

### **Bước 2: Kiểm tra port 80 có bị chiếm không**

```bash
# Kiểm tra port 80
sudo netstat -tulpn | grep :80
# hoặc
sudo ss -tulpn | grep :80
```

**Đảm bảo:** Không có process nào đang dùng port 80

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

Flag `--verbose` sẽ hiển thị thông tin chi tiết về lỗi.

---

### **Bước 4: Xem logs chi tiết**

```bash
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -200"
```

---

## 🚀 LỆNH ĐỂ CHẠY NGAY

```bash
# 1. Đảm bảo Nginx đã dừng
docker compose stop nginx
docker compose ps nginx

# 2. Kiểm tra port 80
sudo netstat -tulpn | grep :80

# 3. Tạo certificate với verbose
docker compose run --rm certbot certonly \
    --standalone \
    --preferred-challenges http \
    --email nhatduyyy131104@gmail.com \
    --agree-tos \
    --no-eff-email \
    --verbose \
    -d zelahahaha.site \
    -d www.zelahahaha.site

# 4. Xem logs nếu vẫn lỗi
docker compose run --rm certbot sh -c "cat /var/log/letsencrypt/letsencrypt.log | tail -200"
```

---

## 💡 CÁC LỖI THƯỜNG GẶP

### **Lỗi: Port 80 already in use**

**Nguyên nhân:** Nginx hoặc process khác đang dùng port 80

**Giải pháp:**
```bash
# Dừng tất cả containers
docker compose stop

# Hoặc kill process đang dùng port 80
sudo fuser -k 80/tcp
```

### **Lỗi: Connection refused**

**Nguyên nhân:** Firewall chặn port 80

**Giải pháp:**
```bash
# Mở port 80
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### **Lỗi: Domain not accessible**

**Nguyên nhân:** Domain chưa trỏ về IP hoặc DNS chưa propagate

**Giải pháp:**
- Kiểm tra DNS: `curl -I http://zelahahaha.site`
- Đợi DNS propagate (có thể mất vài giờ)

---

## ⚠️ LƯU Ý

1. **Nginx PHẢI dừng** khi dùng standalone mode
2. **Port 80 PHẢI mở** và không bị chiếm
3. **Domain PHẢI trỏ về IP VPS** (đã kiểm tra ✅)
4. **Firewall PHẢI cho phép** port 80

