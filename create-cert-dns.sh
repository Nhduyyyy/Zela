#!/bin/bash

# Script để tạo SSL certificate với DNS challenge
# Sử dụng: ./create-cert-dns.sh

DOMAIN="zelahahaha.site"
EMAIL="nhatduyyy131104@gmail.com"

echo "🔐 Tạo SSL certificate với DNS challenge cho $DOMAIN..."
echo ""

# Kiểm tra Certbot đã cài chưa
if ! command -v certbot &> /dev/null; then
    echo "📦 Đang cài đặt Certbot..."
    sudo apt-get update
    sudo apt-get install certbot -y
fi

echo "📋 Thông tin:"
echo "   Domain: $DOMAIN"
echo "   Email: $EMAIL"
echo ""

echo "🚀 Đang tạo certificate với DNS challenge..."
echo "⚠️  LƯU Ý: Bạn sẽ cần tạo TXT record trong DNS!"
echo ""

# Tạo certificate
sudo certbot certonly \
    --manual \
    --preferred-challenges=dns \
    --email "$EMAIL" \
    --server https://acme-v02.api.letsencrypt.org/directory \
    --agree-tos \
    -d "$DOMAIN" \
    -d "www.$DOMAIN"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Certificate đã được tạo thành công!"
    echo ""
    echo "📁 Certificate location:"
    echo "   /etc/letsencrypt/live/$DOMAIN/fullchain.pem"
    echo "   /etc/letsencrypt/live/$DOMAIN/privkey.pem"
    echo ""
    echo "🔄 Bước tiếp theo:"
    echo "   1. Cập nhật docker-compose.yml (đã mount /etc/letsencrypt)"
    echo "   2. Uncomment HTTPS server block trong nginx/conf.d/zela.conf"
    echo "   3. Restart Nginx: docker compose restart nginx"
else
    echo ""
    echo "❌ Có lỗi khi tạo certificate"
    exit 1
fi

