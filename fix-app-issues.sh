#!/bin/bash

echo "🔧 Đang kiểm tra và sửa các vấn đề phổ biến..."
echo ""

# 1. Kiểm tra thư mục uploads
echo "📁 Kiểm tra thư mục uploads..."
if [ ! -d "wwwroot/uploads" ]; then
    echo "   Tạo thư mục wwwroot/uploads..."
    mkdir -p wwwroot/uploads
    chmod 755 wwwroot/uploads
fi

if [ ! -d "wwwroot/sticker" ]; then
    echo "   Tạo thư mục wwwroot/sticker..."
    mkdir -p wwwroot/sticker
    chmod 755 wwwroot/sticker
fi

# 2. Kiểm tra quyền
echo "🔐 Kiểm tra quyền thư mục..."
chmod -R 755 wwwroot/uploads wwwroot/sticker 2>/dev/null

# 3. Kiểm tra containers
echo "🐳 Kiểm tra containers..."
if ! docker ps | grep -q zela-app; then
    echo "   ❌ Container zela-app không chạy!"
    echo "   Khởi động lại..."
    docker compose up -d
    sleep 10
fi

if ! docker ps | grep -q zela-sqlserver; then
    echo "   ❌ Container zela-sqlserver không chạy!"
    exit 1
fi

# 4. Test kết nối database
echo "🗄️  Test kết nối database..."
docker run --rm \
  --network zela_zela-network \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd \
  -S sqlserver \
  -U SA \
  -P 'Str0ng_Pa$w0rd!' \
  -Q 'SELECT DB_NAME()' > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "   ✅ Database kết nối OK"
else
    echo "   ❌ Không kết nối được database"
fi

# 5. Xem logs lỗi
echo ""
echo "📝 Logs lỗi gần đây:"
echo "=================================="
docker logs zela-app --tail 30 2>&1 | grep -i "error\|exception\|fail" | tail -10

echo ""
echo "✅ Hoàn tất kiểm tra!"
echo ""
echo "💡 Nếu vẫn lỗi, xem logs đầy đủ:"
echo "   docker logs zela-app --tail 100"

