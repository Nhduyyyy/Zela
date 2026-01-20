#!/bin/bash

# Script để tạo SSL certificate lần đầu với Certbot
# Sử dụng: ./init-ssl.sh

DOMAIN="zelahahaha.site"
EMAIL="your-email@example.com"  # Thay đổi email của bạn

echo "🔐 Khởi tạo SSL certificate cho $DOMAIN..."
echo ""

# Kiểm tra domain đã được cấu hình chưa
if [ -z "$DOMAIN" ] || [ "$DOMAIN" = "your-domain.com" ]; then
    echo "❌ Vui lòng cập nhật DOMAIN trong script này!"
    exit 1
fi

# Kiểm tra email
if [ -z "$EMAIL" ] || [ "$EMAIL" = "your-email@example.com" ]; then
    echo "❌ Vui lòng cập nhật EMAIL trong script này!"
    exit 1
fi

echo "📋 Thông tin:"
echo "   Domain: $DOMAIN"
echo "   Email: $EMAIL"
echo ""

# Đảm bảo Nginx đang chạy
echo "🔄 Kiểm tra Nginx..."
docker compose ps nginx | grep -q "Up" || {
    echo "⚠️  Nginx chưa chạy, đang khởi động..."
    docker compose up -d nginx
    sleep 5
}

# Tạo certificate với Certbot
echo "🔐 Đang tạo SSL certificate..."
docker compose run --rm certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    -d "$DOMAIN" \
    -d "www.$DOMAIN"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ SSL certificate đã được tạo thành công!"
    echo ""
    echo "🔄 Đang reload Nginx..."
    docker compose exec nginx nginx -s reload
    
    echo ""
    echo "✅ Hoàn tất! Website của bạn giờ đã có HTTPS."
    echo "🌐 Truy cập: https://$DOMAIN"
    echo ""
    echo "📝 Lưu ý:"
    echo "   - Certificate sẽ tự động được gia hạn bởi certbot container"
    echo "   - Nginx sẽ tự động reload khi certificate được gia hạn"
else
    echo ""
    echo "❌ Có lỗi khi tạo SSL certificate"
    echo "💡 Kiểm tra:"
    echo "   1. Domain đã trỏ về IP VPS chưa?"
    echo "   2. Port 80 đã mở chưa?"
    echo "   3. Nginx đang chạy chưa?"
    exit 1
fi

