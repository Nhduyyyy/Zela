#!/bin/bash

# Script để tạo SSL certificate với DNS challenge trong screen
# Sử dụng: ./create-cert-dns-screen.sh

DOMAIN="zelahahaha.site"
EMAIL="nhatduyyy131104@gmail.com"

echo "🔐 Tạo SSL certificate với DNS challenge cho $DOMAIN..."
echo ""

# Kiểm tra screen đã cài chưa
if ! command -v screen &> /dev/null; then
    echo "📦 Đang cài đặt screen..."
    sudo apt-get update
    sudo apt-get install screen -y
fi

# Kiểm tra Certbot đã cài chưa
if ! command -v certbot &> /dev/null; then
    echo "📦 Đang cài đặt Certbot..."
    sudo apt-get install certbot -y
fi

echo "📋 Thông tin:"
echo "   Domain: $DOMAIN"
echo "   Email: $EMAIL"
echo ""

echo "🚀 Đang tạo screen session 'certbot'..."
echo "⚠️  LƯU Ý:"
echo "   1. Bạn sẽ cần tạo TXT record trong DNS"
echo "   2. Session sẽ chạy trong screen (không bị mất khi SSH disconnect)"
echo "   3. Detach: Ctrl + A, D"
echo "   4. Reattach: screen -r certbot"
echo ""

# Tạo screen session và chạy Certbot
screen -dmS certbot bash -c "
    sudo certbot certonly \
        --manual \
        --preferred-challenges=dns \
        --email $EMAIL \
        --server https://acme-v02.api.letsencrypt.org/directory \
        --agree-tos \
        -d $DOMAIN \
        -d www.$DOMAIN
"

echo "✅ Screen session 'certbot' đã được tạo!"
echo ""
echo "📝 Để xem và tương tác với session:"
echo "   screen -r certbot"
echo ""
echo "💡 Khi Certbot yêu cầu TXT record:"
echo "   1. Tạo TXT record trong DNS"
echo "   2. Verify: nslookup -type=TXT _acme-challenge.$DOMAIN"
echo "   3. Nhấn Enter trong screen session"
echo ""

