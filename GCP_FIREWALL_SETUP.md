# 🔥 THIẾT LẬP FIREWALL TRÊN GOOGLE CLOUD PLATFORM

## 📋 VẤN ĐỀ

Trên GCP, có 2 loại firewall:
1. **VPC Firewall Rules** (trong GCP Console) - Quan trọng nhất!
2. **OS-level firewall** (iptables trên VM)

Certbot cần port 80 và 443 mở để Let's Encrypt có thể verify domain.

---

## 🚀 CÁCH 1: MỞ FIREWALL QUA GCP CONSOLE (KHUYẾN NGHỊ)

### **Bước 1: Vào GCP Console**

1. Truy cập: https://console.cloud.google.com
2. Chọn project của bạn
3. Vào **VPC network** → **Firewall**

### **Bước 2: Tạo Firewall Rule cho HTTP (Port 80)**

1. Click **Create Firewall Rule**
2. Điền thông tin:
   - **Name:** `allow-http`
   - **Description:** Allow HTTP traffic for Let's Encrypt
   - **Direction of traffic:** Ingress
   - **Action on match:** Allow
   - **Targets:** All instances in the network
   - **Source IP ranges:** `0.0.0.0/0` (cho phép từ mọi nơi)
   - **Protocols and ports:** 
     - ✅ TCP
     - Port: `80`

3. Click **Create**

### **Bước 3: Tạo Firewall Rule cho HTTPS (Port 443)**

1. Click **Create Firewall Rule**
2. Điền thông tin:
   - **Name:** `allow-https`
   - **Description:** Allow HTTPS traffic
   - **Direction of traffic:** Ingress
   - **Action on match:** Allow
   - **Targets:** All instances in the network
   - **Source IP ranges:** `0.0.0.0/0`
   - **Protocols and ports:** 
     - ✅ TCP
     - Port: `443`

3. Click **Create**

### **Bước 4: Kiểm tra Rules đã được tạo**

Trong danh sách Firewall Rules, bạn sẽ thấy:
- `allow-http` (port 80)
- `allow-https` (port 443)

---

## 🚀 CÁCH 2: MỞ FIREWALL QUA GCLOUD CLI

Nếu bạn có gcloud CLI cài đặt:

```bash
# Mở port 80 (HTTP)
gcloud compute firewall-rules create allow-http \
    --allow tcp:80 \
    --source-ranges 0.0.0.0/0 \
    --description "Allow HTTP traffic"

# Mở port 443 (HTTPS)
gcloud compute firewall-rules create allow-https \
    --allow tcp:443 \
    --source-ranges 0.0.0.0/0 \
    --description "Allow HTTPS traffic"
```

---

## 🚀 CÁCH 3: MỞ FIREWALL TRÊN VM (OS-level)

Nếu cần mở firewall trên VM (thường không cần nếu đã mở trên GCP):

```bash
# Kiểm tra iptables
sudo iptables -L -n | grep 80
sudo iptables -L -n | grep 443

# Mở port 80 và 443 (nếu cần)
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT

# Lưu rules (nếu dùng iptables-persistent)
sudo apt-get install iptables-persistent -y
sudo netfilter-persistent save
```

---

## ✅ KIỂM TRA SAU KHI MỞ FIREWALL

### **Test từ bên ngoài:**

```bash
# Test port 80
curl -I http://34.124.228.222

# Test port 443
curl -I https://34.124.228.222
```

### **Test từ VPS:**

```bash
# Test port 80 local
curl -I http://localhost

# Test với domain
curl -I http://zelahahaha.site
```

---

## 🎯 SAU KHI MỞ FIREWALL

Sau khi mở firewall trên GCP, thử lại tạo certificate:

```bash
# Dừng Nginx
docker compose stop nginx

# Tạo certificate với network host
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

# Khởi động lại Nginx
docker compose start nginx
```

---

## 📝 TÓM TẮT

1. ✅ Vào GCP Console → VPC network → Firewall
2. ✅ Tạo rule `allow-http` (port 80)
3. ✅ Tạo rule `allow-https` (port 443)
4. ✅ Test lại tạo certificate

---

## 🔗 LINK HỮU ÍCH

- GCP Console: https://console.cloud.google.com
- VPC Firewall: https://console.cloud.google.com/networking/firewalls
- GCP Firewall Docs: https://cloud.google.com/vpc/docs/firewalls

