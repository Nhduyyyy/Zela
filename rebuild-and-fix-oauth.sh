#!/bin/bash

echo "🔧 Rebuild container để sửa lỗi OAuth Correlation failed..."
echo ""

# 1. Pull code mới (nếu có)
echo "📥 Pull code mới..."
git pull

# 2. Rebuild container
echo "🔨 Rebuild container..."
docker compose build zela-app

# 3. Recreate container
echo "🔄 Recreate container..."
docker compose up -d --force-recreate zela-app

# 4. Đợi container khởi động
echo "⏳ Đợi container khởi động..."
sleep 10

# 5. Kiểm tra DataProtection keys
echo "🔍 Kiểm tra DataProtection keys..."
docker exec zela-app ls -la /root/.aspnet/DataProtection-Keys 2>&1 | head -10

# 6. Kiểm tra logs
echo ""
echo "📝 Logs gần đây:"
docker logs zela-app --tail 20

echo ""
echo "✅ Hoàn tất!"
echo ""
echo "💡 Bây giờ thử đăng nhập lại bằng Google"
echo "   Nếu vẫn lỗi, kiểm tra:"
echo "   1. Redirect URI trong Google Console: http://zelahahaha.site/signin-google"
echo "   2. Xem logs chi tiết: docker logs zela-app --tail 50"

