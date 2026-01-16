#!/bin/bash

echo "🔧 Script sửa lỗi OAuth hoàn chỉnh..."
echo ""

# 1. Pull code mới
echo "📥 Pull code mới..."
git pull

if [ $? -ne 0 ]; then
    echo "⚠️  Git pull failed. Đảm bảo đã commit và push code từ máy local."
    exit 1
fi

# 2. Rebuild container
echo "🔨 Rebuild container..."
docker compose build zela-app

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

# 3. Recreate container
echo "🔄 Recreate container..."
docker compose up -d --force-recreate zela-app

# 4. Đợi container khởi động
echo "⏳ Đợi container khởi động (15 giây)..."
sleep 15

# 5. Kiểm tra container đang chạy
echo "🔍 Kiểm tra containers:"
docker compose ps

# 6. Kiểm tra DataProtection keys
echo ""
echo "🔑 Kiểm tra DataProtection keys:"
docker exec zela-app ls -la /root/.aspnet/DataProtection-Keys 2>&1 | head -5

# 7. Kiểm tra logs
echo ""
echo "📝 Logs gần đây:"
docker logs zela-app --tail 20

echo ""
echo "✅ Hoàn tất rebuild!"
echo ""
echo "📋 CÁC BƯỚC TIẾP THEO:"
echo "1. Xóa cookies trong browser (hoặc dùng chế độ ẩn danh)"
echo "2. Kiểm tra Google Console có redirect URI: http://zelahahaha.site/signin-google"
echo "3. Truy cập: http://zelahahaha.site"
echo "4. Click 'Đăng nhập bằng Google'"
echo ""
echo "💡 Nếu vẫn lỗi, xem logs: docker logs -f zela-app"

