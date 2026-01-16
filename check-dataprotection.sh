#!/bin/bash

echo "🔍 Kiểm tra DataProtection keys..."
echo ""

# Kiểm tra thư mục DataProtection keys
echo "📁 Kiểm tra thư mục DataProtection keys:"
docker exec zela-app ls -la /root/.aspnet/DataProtection-Keys 2>&1

echo ""
echo "📄 Kiểm tra files trong thư mục:"
docker exec zela-app find /root/.aspnet/DataProtection-Keys -type f 2>&1 | head -10

echo ""
echo "💡 Nếu thư mục không tồn tại hoặc rỗng, cần rebuild container"

